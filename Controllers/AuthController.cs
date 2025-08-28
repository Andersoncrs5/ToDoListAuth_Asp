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
using TodoListJwt.utils.response;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TodoListJwt.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
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
        [ProducesResponseType(typeof(ResponseBody<ValidationErrors>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseBody<Tokens>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                ResponseBody<ValidationErrors> errorResponse = CreateErrorResponse(ModelState);
                return BadRequest(errorResponse);
            }

            string email = model.Email.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new ResponseBody<string>
                {
                    Body = null,
                    Message = "Email and Password must be provided.",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 400,
                });
            }

            ApplicationUser? user = await _userManager.FindByEmailAsync(email);

            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                IList<string>? userRoles = await _userManager.GetRolesAsync(user);

                if (string.IsNullOrEmpty(user.Email) || string.IsNullOrWhiteSpace(user.Email))
                {
                    return BadRequest(new ResponseBody<string>
                    {
                        Body = null,
                        Message = "Email is null",
                        Success = false,
                        Timestamp = DateTimeOffset.Now,
                        StatusCode = 400,
                    });
                }

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

                return Ok(new Tokens
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken,
                    ExpirationToken = token.ValidTo
                });
            }

            return Unauthorized(new ResponseBody<string>{
                Body = null,
                Message = "Invalid email or password.",
                Success = false,
                Timestamp = DateTimeOffset.Now,
                StatusCode = 401,
            });
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ResponseBody<ValidationErrors>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseBody<IEnumerable<string>>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] CreateUserDto model)
        {
            try 
            {
                if (!ModelState.IsValid)
                {
                    ResponseBody<ValidationErrors> errorResponse = CreateErrorResponse(ModelState);
                    return BadRequest(errorResponse);
                }

                string email = model.Email.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return BadRequest(new ResponseBody<string>
                    {
                        Body = null,
                        Message = "Email and Password must be provided.",
                        Success = false,
                        Timestamp = DateTimeOffset.Now,
                        StatusCode = 400,
                    });
                }

                ApplicationUser? nameCheck = await _userManager.FindByNameAsync(model.Name);

                if (nameCheck != null)
                {
                    return StatusCode(StatusCodes.Status409Conflict, new ResponseBody<string>
                    {
                        Body = null,
                        Message = "Name already exists.",
                        Success = false,
                        Timestamp = DateTimeOffset.Now,
                        StatusCode = StatusCodes.Status409Conflict,
                    });
                }

                ApplicationUser? userExists = await _userManager.FindByEmailAsync(email);

                if (userExists != null)
                {
                    return StatusCode(StatusCodes.Status409Conflict, new ResponseBody<string>
                    {
                        Body = null,
                        Message = "Email already exists.",
                        Success = false,
                        Timestamp = DateTimeOffset.Now,
                        StatusCode = StatusCodes.Status409Conflict,
                    });
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
                    IEnumerable<string> errors = result.Errors.Select(e => e.Description);
                    return StatusCode(StatusCodes.Status400BadRequest, new ResponseBody<IEnumerable<string>>
                    {
                        StatusCode = 400,
                        Message = "User creation failed.",
                        Body = errors,
                        Success = false,
                        Timestamp = DateTimeOffset.Now
                    });
                }

                return StatusCode(StatusCodes.Status201Created, new ResponseBody<string>
                    {
                        Body = null,
                        Message = "User created successfully.",
                        Success = true,
                        Timestamp = DateTimeOffset.Now,
                        StatusCode = 201,
                    }
                );
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseBody<string>
                {
                    Body = e.Message,
                    Message = "Error the create user! Please try again later",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = StatusCodes.Status500InternalServerError,
                });
            }
        }

        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ResponseBody<ValidationErrors>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseBody<Tokens>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] TokenDto tokenModel)
        {
            if (!ModelState.IsValid)
            {
                ResponseBody<ValidationErrors> errorResponse = CreateErrorResponse(ModelState);
                return BadRequest(errorResponse);
            }

            if (tokenModel is null || string.IsNullOrWhiteSpace(tokenModel.AcessToken) || string.IsNullOrWhiteSpace(tokenModel.RefreshToken))
            {    
                return BadRequest(new ResponseBody<string>{
                    Body = null,
                    Message = "Invalid client request",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 400,
                });
            }

            ClaimsPrincipal? principal = _tokenService.GetPrincipalFromExpiredToken(tokenModel.AcessToken, _configuration);

            if (principal == null)
            {
                return BadRequest(new ResponseBody<string>{
                    Body = null,
                    Message = "Invalid access token or refresh token",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 400,
                });
            }

            string? username = principal.Identity?.Name;

            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new ResponseBody<string>{
                    Body = null,
                    Message = "Invalid token data",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 400,
                });
            }

            ApplicationUser? user = await _userManager.FindByNameAsync(username);

            if (user == null || user.RefreshToken != tokenModel.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                return BadRequest(new ResponseBody<string>{
                    Body = null,
                    Message = "Invalid access token or refresh token",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 400,
                });
            }

            JwtSecurityToken? newAccessToken = _tokenService.GenerateAccessToken(principal.Claims.ToList(), _configuration);
            string? newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            await _userManager.UpdateAsync(user);

            return Ok(new ResponseBody<Tokens>
            {
                Body = new Tokens
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                    RefreshToken = newRefreshToken,
                    ExpirationToken = newAccessToken.ValidTo
                },
                Message = "Token created",
                Success = true,
                Timestamp = DateTimeOffset.Now,
                StatusCode = 200,
            });
        }

        [HttpPost]
        [Route("revoke")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Revoke()
        {
            string? id = User.FindFirst(ClaimTypes.Sid)?.Value;

            if (string.IsNullOrWhiteSpace(id)) 
            {
                return Unauthorized(new ResponseBody<string>{
                    Body = null,
                    Message = "you are not logged in",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 401,
                });
            }

            ApplicationUser? user = await _userManager.FindByIdAsync(id);

            if (user == null) 
            {
                return NotFound(new ResponseBody<string>{
                    Body = null,
                    Message = "User not found",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            user.RefreshToken = null;

            await _userManager.UpdateAsync(user);

            return StatusCode(StatusCodes.Status200OK, new ResponseBody<string>{
                Body = null,
                Message = "Bye bye",
                Success = true,
                Timestamp = DateTimeOffset.Now,
                StatusCode = 200,
            });
        }

        private ResponseBody<ValidationErrors> CreateErrorResponse(ModelStateDictionary modelState)
        {
            ValidationErrors errorDict = new ValidationErrors();

            foreach (string key in modelState.Keys)
            {
                if (modelState[key] is ModelStateEntry state && state.Errors.Any())
                {
                    var errorMessages = state.Errors.Select(e => e.ErrorMessage).ToList();
                    errorDict.Errors.Add(key, errorMessages);
                }
            }

            return new ResponseBody<ValidationErrors>
            {
                Message = "Validation failed. Check the response body for errors.",
                Success = false,
                StatusCode = StatusCodes.Status400BadRequest, 
                Timestamp = DateTimeOffset.UtcNow,
                Body = errorDict, 
                Url = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.Path}"
            };
        }


    }
}