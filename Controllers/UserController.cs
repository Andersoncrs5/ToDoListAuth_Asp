using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoListJwt.DTOs.user;
using TodoListJwt.entities;
using TodoListJwt.SetUnitOfWork;
using TodoListJwt.utils;

namespace TodoListJwt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public UserController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("me")]
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
        [HttpDelete]
        public async Task<ActionResult> Delete() 
        {
            string? id = User.FindFirst(ClaimTypes.Sid)?.Value;

            await this._uow.UserRepository.Delete(id);

            return Ok(new Response<ApplicationUser> 
            {
                Message = "User deleted with successfully!",
                Code = 200,
                Status = "success"
            });
        }

        [HttpPut]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> Update([FromBody] UpdateUserDto userDto) 
        {
            if (!ModelState.IsValid)
                    return BadRequest(ModelState);

            string? id = User.FindFirst(ClaimTypes.Sid)?.Value;
            ApplicationUser user = await this._uow.UserRepository.Update(id, userDto);

            return Ok(
                new UserResponse 
                {   
                    Id = user.Id,
                    Name = user.UserName!,
                    Email = user.Email!
                }
            );

        }

    }
}