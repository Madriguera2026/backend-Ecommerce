using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrbisaAsp.Data.Configuration;
using OrbisaAsp.Data.Services;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURACIÓN

var region =
    builder.Configuration["AWS_REGION"]
    ?? throw new Exception("AWS_REGION no configurado.");

var userPoolId =
    builder.Configuration["COGNITO_USER_POOL_ID"]
    ?? throw new Exception("COGNITO_USER_POOL_ID no configurado.");

var clientId =
    builder.Configuration["COGNITO_CLIENT_ID"]
    ?? throw new Exception("COGNITO_CLIENT_ID no configurado.");

var authority =
    $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

// AWS

builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
    new AmazonDynamoDBClient(
        Amazon.RegionEndpoint.USEast2
    )
);

builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
    new AmazonCognitoIdentityProviderClient(
        Amazon.RegionEndpoint.USEast2
    )
);

// SERVICIOS

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProductService>();

builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminSettings")
);

builder.Services.AddControllers();

// SWAGGER

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingresa el Access Token de Cognito."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

// COGNITO

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authority,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                ValidateAudience = false
            };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                Console.WriteLine("TOKEN VALIDADO");

                foreach (var claim in context.Principal!.Claims)
                {
                    Console.WriteLine(
                        $"CLAIM: {claim.Type} = {claim.Value}"
                    );
                }

                var tokenClientId =
                    context.Principal
                        .FindFirst("client_id")?.Value;

                var tokenUse =
                    context.Principal
                        .FindFirst("token_use")?.Value;

                if (tokenClientId != clientId)
                {
                    context.Fail(
                        "El token pertenece a otro App Client."
                    );

                    return Task.CompletedTask;
                }

                if (tokenUse != "access")
                {
                    context.Fail(
                        "Se requiere un Access Token."
                    );

                    return Task.CompletedTask;
                }

                return Task.CompletedTask;
            },

            OnAuthenticationFailed = context =>
            {
                Console.WriteLine(
                    $"AUTH ERROR: {context.Exception.Message}"
                );

                return Task.CompletedTask;
            }
        };
    });

// AUTHORIZATION

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var username =
                context.User
                    .FindFirst("username")?.Value;

            var adminUsernames =
                builder.Configuration
                    .GetSection(
                        "AdminSettings:AdminUsers"
                    )
                    .Get<List<string>>()
                    ?? new List<string>();

            return !string.IsNullOrWhiteSpace(username)
                && adminUsernames.Any(
                    adminUsername =>
                        string.Equals(
                            adminUsername.Trim(),
                            username.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                );
        });
    });
});

// CORS

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// BUILD

var app = builder.Build();

// MIDDLEWARE

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("ReactFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();