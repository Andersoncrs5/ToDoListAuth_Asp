using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInApi.Services.IServices;
using TodoListJwt.entities;
using TodoListJwt.DTOs.user;
using TodoListJwt.utils;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace TodoListJwt.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [EnableRateLimiting("AdmSystemPolicy")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            ITokenService tokenService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                    return BadRequest(ModelState);

            string email = model.Email.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(model.Password))
                return BadRequest("Email and Password must be provided.");

            ApplicationUser? user = await _userManager.FindByEmailAsync(email);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                IList<string>? userRoles = await _userManager.GetRolesAsync(user);

                if (string.IsNullOrEmpty(user.Email) || string.IsNullOrWhiteSpace(user.Email))
                    return BadRequest("Email is null");

                List<Claim>? authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.Sid, user.Id!),
                    new Claim(ClaimTypes.Email, user.Email.Trim()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                JwtSecurityToken? token = _tokenService.GenerateAccessToken(authClaims, _configuration);
                string? refreshToken = _tokenService.GenerateRefreshToken();

                _ = int.TryParse(_configuration["jwt:RefreshTokenValidityInMinutes"], out int refreshTokenValidity);

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidity);
                await _userManager.UpdateAsync(user);

                return Ok(new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken,
                    Expiration = token.ValidTo
                });
            }

            return Unauthorized("Invalid email or password.");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto model)
        {
            try 
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                string email = model.Email.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(model.Password))
                    return BadRequest("Email and Password must be provided.");

                ApplicationUser? userExists = await _userManager.FindByEmailAsync(email);

                if (userExists != null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest,
                        new Response<string> { Status = "Error", Message = "User already exists.", Code = 400 });
                }

                ApplicationUser? user = new ApplicationUser
                {
                    Email = email,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = model.Name
                };

                IdentityResult? result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description);
                    return StatusCode(StatusCodes.Status400BadRequest, new
                    {
                        Status = "Error",
                        Message = "User creation failed.",
                        Errors = errors,
                        Code = 400
                    });
                }

                return Ok(new Response<string> { Code = 201, Status = "Success", Message = "User created successfully." });
            }
            catch (Exception e)
            {
                throw new Exception($"Error: \n{e}");
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenDto tokenModel)
        {
            if (!ModelState.IsValid)
                    return BadRequest(ModelState);

            if (tokenModel is null || string.IsNullOrWhiteSpace(tokenModel.AcessToken) || string.IsNullOrWhiteSpace(tokenModel.RefreshToken))
                return BadRequest("Invalid client request");

            ClaimsPrincipal? principal = _tokenService.GetPrincipalFromExpiredToken(tokenModel.AcessToken, _configuration);

            if (principal == null)
                return BadRequest("Invalid access token or refresh token");

            string? username = principal.Identity?.Name;

            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Invalid token data");

            ApplicationUser? user = await _userManager.FindByNameAsync(username);

            if (user == null || user.RefreshToken != tokenModel.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                return BadRequest("Invalid access token or refresh token");

            JwtSecurityToken? newAccessToken = _tokenService.GenerateAccessToken(principal.Claims.ToList(), _configuration);
            string? newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken
            });
        }

        [Authorize]
        [HttpPost]
        [Route("revoke/{email}")]
        public async Task<IActionResult> Revoke(string email)
        {
                    
            ApplicationUser? user = await this._userManager.FindByEmailAsync(email);

            if (user == null) return BadRequest("Invalid email");

            user.RefreshToken = null;

            await _userManager.UpdateAsync(user);

            return NoContent();

        }

    }
}