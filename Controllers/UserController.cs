using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TodoListJwt.DTOs.user;
using TodoListJwt.entities;
using TodoListJwt.SetUnitOfWork;
using TodoListJwt.utils;

namespace TodoListJwt.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public UserController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [HttpGet("me")]
        [EnableRateLimiting("SlidingWindowLimiterPolicy")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> Me() 
        {
            string? id = User.FindFirst(ClaimTypes.Sid)?.Value;

            ApplicationUser user = await _uow.UserRepository.Get(id);

            return Ok(
                new UserResponse 
                {   
                    Id = user.Id,
                    Name = user.UserName!,
                    Email = user.Email!
                }
            );
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [EnableRateLimiting("fixedWindowLimiterPolicy")]
        [HttpDelete]
        public async Task<ActionResult> Delete() 
        {
            string? id = User.FindFirst(ClaimTypes.Sid)?.Value;
            ApplicationUser user = await _uow.UserRepository.Get(id);

            await this._uow.UserRepository.Delete(user);

            return Ok(new Response<ApplicationUser> 
            {
                Message = "User deleted with successfully!",
                Code = 200,
                Status = "success"
            });
        }

        [HttpPut]
        [EnableRateLimiting("fixedWindowLimiterPolicy")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> Update([FromBody] UpdateUserDto userDto) 
        {
            if (!ModelState.IsValid)
                    return BadRequest(ModelState);

            string? id = User.FindFirst(ClaimTypes.Sid)?.Value;
            ApplicationUser user = await _uow.UserRepository.Get(id);
            ApplicationUser data = await _uow.UserRepository.Update(user, userDto);

            return Ok(
                new UserResponse 
                {   
                    Id = data.Id,
                    Name = data.UserName!,
                    Email = data.Email!
                }
            );

        }

    }
}