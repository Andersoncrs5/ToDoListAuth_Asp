using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoListJwt.Context;
using TodoListJwt.DTOs.task;
using TodoListJwt.entities;
using TodoListJwt.SetRepositories.IRepositories;
using TodoListJwt.SetUnitOfWork;
using TodoListJwt.utils;

namespace TodoListJwt.SetRepositories.Repositories
{
    public class TaskRepository: ITaskRepository
    {
        private readonly AppDbContext _context;
        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskEntity?> Get(long taskId)
        {
            if (long.IsNegative(taskId) || taskId == 0 )
                throw new ArgumentNullException(nameof(taskId));

            TaskEntity? task = await _context.Tasks
                .AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return null;

            return task;
        }

        public async Task<TaskEntity> Create(TaskDto taskDto , ApplicationUser user) 
        {
            TaskEntity task = taskDto.toTaskEntity();

            task.User = user;
            task.CreatedAt = DateTime.UtcNow;
            task.UserId = user.Id!;

            var created = await _context.AddAsync(task);
            await _context.SaveChangesAsync();

            return created.Entity;
        }

        public async Task<TaskEntity> Update(TaskDto taskDto , TaskEntity task) 
        {
            task.Title = taskDto.Title;
            task.Description = taskDto.Description;
            task.Done = taskDto.Done;
            task.UpdatedAt = DateTime.UtcNow;

            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return task;
        }

        public async Task Delete(TaskEntity task)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task<TaskEntity> ChangeStatusDone(TaskEntity task)
        {
            task.Done = !task.Done;

            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return task;
        }

        public async Task<PaginatedList<TaskEntity>> GetAllByUser(
            ApplicationUser user, 
            DateTime? createAtBefore, 
            DateTime? createAtAfter, 
            string? title,
            bool? done,
            int pageNumber = 1, 
            int pageSize = 10
        )
        {
            IQueryable<TaskEntity> query = _context.Tasks
                .AsNoTracking()
                .Where(t => t.UserId == user.Id!);

            if (createAtBefore.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= createAtBefore.Value);
            }

            if (createAtAfter.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= createAtAfter.Value);
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                query = query.Where(t => t.Title.Contains(title));
            }

            if (done.HasValue)
            {
                query = query.Where(t => t.Done == done.Value);
            }

            return await PaginatedList<TaskEntity>.CreateAsync(query, pageNumber, pageSize);
        }


    }
}