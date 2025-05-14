using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoListJwt.DTOs.task;
using TodoListJwt.entities;
using TodoListJwt.SetUnitOfWork;
using TodoListJwt.utils;

namespace TodoListJwt.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public TaskController(IUnitOfWork uow) 
        {
            _uow = uow;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskDto taskDto)
        {
            string? id = User.FindFirst(ClaimTypes.Sid)?.Value;
            TaskEntity task = await this._uow.TaskRepository.Create(taskDto, id);

            return Ok(new Response<TaskEntity>
            {
                Message = "Task created with successfully",
                Code = 201,
                Status = "success",
                data = task
            }
            );
        }
        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("{Id:long}")]
        public async Task<IActionResult> Update(long Id, [FromBody] TaskDto taskDto)
        {
            if (!ModelState.IsValid)
                    return BadRequest(ModelState);

            TaskEntity task = await this._uow.TaskRepository.Update(taskDto, Id);

            return Ok(new Response<TaskEntity>
            {
                Message = "Task updated with successfully",
                Code = 201,
                Status = "success",
                data = task
            }
            );
            
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult> Get(long id) 
        {
            TaskEntity task = await _uow.TaskRepository.Get(id);

            return Ok( new Response<TaskEntity>
                {
                    Code = 302,
                    data = task,
                    Message = "Task found",
                    Status = "success"
                }
            );
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult> Delete(long id) 
        {
            await _uow.TaskRepository.Delete(id);

            return Ok( new Response<string>
                {
                    Code = 200,
                    data = "NONE",
                    Message = "Task deleted with successfully",
                    Status = "success"
                }
            );
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("/ChangeStatusDone/{id:long}")]
        public async Task<ActionResult> ChangeStatusDone(long id) 
        {
            bool status = await _uow.TaskRepository.ChangeStatusDone(id);

            return Ok( new Response<string>
                {
                    Code = 302,
                    data = $"status : {status}",
                    Message = "Task changed with successfully",
                    Status = "success"
                }
            );
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> GetAllByUser([FromQuery] PaginationQuery query)
        {
            string? id = User.FindFirst(ClaimTypes.Sid)?.Value;

            var tasks = await this._uow.TaskRepository.GetAllByUser(id, query.PageNumber, query.PageSize);

            return Ok(tasks);
        }

    }
}