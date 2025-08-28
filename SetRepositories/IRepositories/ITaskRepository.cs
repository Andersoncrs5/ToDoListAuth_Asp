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
        Task<TaskEntity> Create(TaskDto taskDto, ApplicationUser user);
        Task<TaskEntity> Update(TaskDto taskDto , TaskEntity task);
        Task<TaskEntity?> Get(long taskId);
        Task Delete(TaskEntity task);
        Task<TaskEntity> ChangeStatusDone(TaskEntity task);
        Task<PaginatedList<TaskEntity>> GetAllByUser(ApplicationUser user, DateTime? createAtBefore, DateTime? createAtAfter, string? Title, bool? Done,int pageNumber = 1, int pageSize = 10 );
    }
}