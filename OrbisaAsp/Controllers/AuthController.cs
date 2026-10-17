using Amazon.CognitoIdentityProvider.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrbisaAsp.Data.Models;
using OrbisaAsp.Data.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OrbisaApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly UserService _userService;

    public AuthController(AuthService authService, UserService userService)
    {
        _authService = authService;
        _userService = userService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var response = await _authService.Register(request);

            return Ok(new
            {
                message = "Usuario registrado correctamente",
                userSub = response.UserSub,
                confirmed = response.UserConfirmed
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var response = await _authService.Login(request);

            return Ok(new
            {
                accessToken = response.AuthenticationResult.AccessToken,
                idToken = response.AuthenticationResult.IdToken,
                refreshToken = response.AuthenticationResult.RefreshToken,
                expiresIn = response.AuthenticationResult.ExpiresIn,
                tokenType = response.AuthenticationResult.TokenType
            });
        }
        catch (Exception ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("confirm")]
    [AllowAnonymous]
    public async Task<IActionResult> Confirm(
    ConfirmRegisterRequest request)
    {
        try
        {
            await _authService.ConfirmRegister(request);

            return Ok(new
            {
                message = "Cuenta confirmada correctamente."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        Console.WriteLine("User: " + User.FindFirstValue);
        var username = User.FindFirst("username")?.Value
            ?? User.FindFirst("username")?.Value;

        if (string.IsNullOrWhiteSpace(username))
            return Unauthorized();

        var user = await _userService.GetByUsername(username);

        if (user is null)
            return NotFound();

        return Ok(user);
    }
}