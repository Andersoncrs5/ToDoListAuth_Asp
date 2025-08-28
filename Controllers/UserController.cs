using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using TodoListJwt.DTOs.user;
using TodoListJwt.entities;
using TodoListJwt.SetUnitOfWork;
using TodoListJwt.utils;
using TodoListJwt.utils.response;

namespace TodoListJwt.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(IUnitOfWork uow, UserManager<ApplicationUser> userManager)
        {
            _uow = uow;
            _userManager = userManager;
        }

        [HttpGet("me")]
        [EnableRateLimiting("SlidingWindowLimiterPolicy")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ResponseBody<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Me() 
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

            ApplicationUser? user = await _uow.UserRepository.Get(id);

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

            return Ok(new ResponseBody<UserResponse>{
                Body = new UserResponse 
                    {   
                        Id = user.Id,
                        Name = user.UserName!,
                        Email = user.Email!
                    },
                Message = "User found successfully!",
                Success = true,
                Timestamp = DateTimeOffset.Now,
                StatusCode = 200,
            });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [EnableRateLimiting("fixedWindowLimiterPolicy")]
        [HttpDelete]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult> Delete() 
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

            ApplicationUser? user = await _uow.UserRepository.Get(id);

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

            await _uow.UserRepository.Delete(user);

            return Ok(new ResponseBody<string>{
                Body = null,
                Message = "User deleted successfully!",
                Success = true,
                Timestamp = DateTimeOffset.Now,
                StatusCode = 200,
            });
        }

        [HttpPut]
        [EnableRateLimiting("fixedWindowLimiterPolicy")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(ResponseBody<ValidationErrors>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseBody<string>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ResponseBody<UserResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult> Update([FromBody] UpdateUserDto userDto) 
        {
            if (!ModelState.IsValid)
            {
                ResponseBody<ValidationErrors> errorResponse = CreateErrorResponse(ModelState);
                return BadRequest(errorResponse);
            }

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
    
            ApplicationUser? user = await _uow.UserRepository.Get(id);

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

            ApplicationUser? nameCheck = await _userManager.FindByNameAsync(userDto.Name);

            if (nameCheck != null && nameCheck.Id != user.Id)
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

            ApplicationUser data = await _uow.UserRepository.Update(user, userDto);

            return Ok(new ResponseBody<UserResponse>{
                Body = new UserResponse 
                    {   
                        Id = user.Id,
                        Name = user.UserName!,
                        Email = user.Email!
                    },
                Message = "User updated successfully!",
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