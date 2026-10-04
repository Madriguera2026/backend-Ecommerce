using Amazon.CognitoIdentityProvider;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrbisaAsp.Data.Configuration;
using OrbisaAsp.Data.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
//TODO: Poner en variables de entorno
var region = builder.Configuration["AWS_REGION"];
var userPoolId = builder.Configuration["COGNITO_USER_POOL_ID"];
var key = builder.Configuration["JWT_KEY"];
// Add services to the container.
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

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminSettings")
);
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var authority =
    $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,

            ValidateAudience = false
        };
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireAssertion(context =>
        {
            var username = context.User.FindFirst("username")?.Value;

            var adminUsernames =
                builder.Configuration
                    .GetSection("AdminSettings:AdminUsers")
                    .Get<List<string>>() ?? new List<string>();

            return adminUsernames.Any(
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

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
