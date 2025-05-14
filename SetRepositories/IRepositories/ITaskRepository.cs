using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoListJwt.DTOs.task;
using TodoListJwt.entities;
using TodoListJwt.utils;

namespace TodoListJwt.SetRepositories.IRepositories
{
    public interface ITaskRepository
    {
        Task<TaskEntity> Create(TaskDto task , string? userId);
        Task<TaskEntity> Update(TaskDto task , long? taskId);
        Task<TaskEntity> Get(long? taskId);
        Task Delete(long? taskId);
        Task<bool> ChangeStatusDone(long? taskId);
        Task<PaginatedList<TaskEntity>> GetAllByUser(string? userId, int pageNumber = 1, int pageSize = 10 );
    }
}