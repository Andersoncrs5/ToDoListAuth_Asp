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
        Task<TaskEntity> Create(TaskDto task , ApplicationUser user);
        Task<TaskEntity> Update(TaskDto task , long? taskId);
        Task<TaskEntity> Get(long? taskId);
        Task Delete(TaskEntity task);
        Task<bool> ChangeStatusDone(TaskEntity task);
        Task<PaginatedList<TaskEntity>> GetAllByUser(ApplicationUser user, int pageNumber = 1, int pageSize = 10 );
    }
}