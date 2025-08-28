using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using TodoListJwt.DTOs.task;
using TodoListJwt.entities;
using TodoListJwt.SetUnitOfWork;
using TodoListJwt.utils;
using TodoListJwt.utils.response;

namespace TodoListJwt.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class TaskController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public TaskController(IUnitOfWork uow) 
        {
            _uow = uow;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost]
        [EnableRateLimiting("fixedWindowLimiterPolicy")]
        public async Task<IActionResult> Create([FromBody] TaskDto taskDto)
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

            TaskEntity task = await _uow.TaskRepository.Create(taskDto, user);

            return StatusCode(StatusCodes.Status201Created, new ResponseBody<TaskEntity>
            {
                Body = task,
                Message = "Task created with successfully",
                Success = true,
                Timestamp = DateTimeOffset.Now,
                StatusCode = 200,
            });    
        }
        
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("{Id:long}")]
        [EnableRateLimiting("fixedWindowLimiterPolicy")]
        public async Task<IActionResult> Update(long Id, [FromBody] TaskDto taskDto)
        {
            if (!ModelState.IsValid)
            {
                ResponseBody<ValidationErrors> errorResponse = CreateErrorResponse(ModelState);
                return BadRequest(errorResponse);
            }

            if (Id <= 0) 
            {
                return BadRequest(new ResponseBody<string>{
                    Body = null,
                    Message = "Id is required",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            TaskEntity? task = await _uow.TaskRepository.Get(Id);

            if (task == null) 
            {
                return NotFound(new ResponseBody<string>{
                    Body = null,
                    Message = "Task not found",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            TaskEntity taskUpdated = await _uow.TaskRepository.Update(taskDto, task);

            return Ok(new ResponseBody<TaskEntity>{
                Body = taskUpdated,
                Message = "Task updated with successfully",
                Success = true,
                Timestamp = DateTimeOffset.Now,
                StatusCode = 200,
            });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{Id:long}")]
        [EnableRateLimiting("SlidingWindowLimiterPolicy")]
        public async Task<ActionResult> Get(long Id) 
        {
            if (Id <= 0) 
            {
                return BadRequest(new ResponseBody<string>{
                    Body = null,
                    Message = "Id is required",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            TaskEntity? task = await _uow.TaskRepository.Get(Id);

            if (task == null) 
            {
                return NotFound(new ResponseBody<string>{
                    Body = null,
                    Message = "Task not found",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            return Ok(new ResponseBody<TaskEntity>
                {
                    Body = task,
                    Message = "Task found with successfully",
                    Success = true,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 200,
                }
            );
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [EnableRateLimiting("fixedWindowLimiterPolicy")]
        [HttpDelete("{Id:long}")]
        public async Task<ActionResult> Delete(long Id) 
        {
            if (Id <= 0) 
            {
                return BadRequest(new ResponseBody<string>{
                    Body = null,
                    Message = "Id is required",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            TaskEntity? task = await _uow.TaskRepository.Get(Id);

            if (task == null) 
            {
                return NotFound(new ResponseBody<string>{
                    Body = null,
                    Message = "Task not found",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            await _uow.TaskRepository.Delete(task);

            return Ok(new ResponseBody<string>
                {
                    Body = null,
                    Message = "Task deleted with successfully",
                    Success = true,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 200,
                }
            );
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("/ChangeStatusDone/{Id:long}")]
        [EnableRateLimiting("fixedWindowLimiterPolicy")]
        public async Task<ActionResult> ChangeStatusDone(long Id) 
        {
            if (Id <= 0) 
            {
                return BadRequest(new ResponseBody<string>{
                    Body = null,
                    Message = "Id is required",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            TaskEntity? task = await _uow.TaskRepository.Get(Id);

            if (task == null) 
            {
                return NotFound(new ResponseBody<string>{
                    Body = null,
                    Message = "Task not found",
                    Success = false,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 404,
                });
            }

            TaskEntity taskChanged = await _uow.TaskRepository.ChangeStatusDone(task);

            return Ok(new ResponseBody<TaskEntity>
                {
                    Body = taskChanged,
                    Message = "Task status changed with successfully",
                    Success = true,
                    Timestamp = DateTimeOffset.Now,
                    StatusCode = 200,
                }
            );
        }

        [HttpGet]
        [EnableRateLimiting("SlidingWindowLimiterPolicy")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> GetAllByUser(
            [FromQuery] PaginationQuery query,
            [FromQuery] string? Title,
            [FromQuery] bool? Done,
            [FromQuery] DateTime? createAtBefore,
            [FromQuery] DateTime? createAtAfter
            )
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

            PaginatedList<TaskEntity> tasks = await _uow.TaskRepository.GetAllByUser(user, createAtBefore, createAtAfter, Title, Done,query.PageNumber, query.PageSize);

            return Ok(tasks);
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