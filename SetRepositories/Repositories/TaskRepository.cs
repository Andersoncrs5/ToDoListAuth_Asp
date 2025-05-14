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
        private readonly IUserRepository _userRepository;
        private readonly AppDbContext _context;
        public TaskRepository(IUserRepository userRepository, AppDbContext context)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public async Task<TaskEntity> Get(long? taskId)
        {
            if (taskId == null || taskId <= 0) 
                throw new ResponseException("Id of task is required", 400, "failed");

            TaskEntity? task = await this._context.Tasks
                .AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) 
                throw new ResponseException("User not found", 404, "failed");

            return task;
        }

        public async Task<TaskEntity> Create(TaskDto taskDto , string? userId) 
        {
            ApplicationUser user = await this._userRepository.Get(userId);

            TaskEntity task = taskDto.toTaskEntity();

            task.User = user;
            task.CreatedAt = DateTime.UtcNow;
            task.UserId = userId!;

            var created = await this._context.AddAsync(task);
            await this._context.SaveChangesAsync();

            return created.Entity;
        }

        public async Task<TaskEntity> Update(TaskDto taskDto , long? taskId) 
        {
            TaskEntity task = await this.Get(taskId);

            task.Title = taskDto.Title;
            task.Description = taskDto.Description;
            task.Done = taskDto.Done;
            task.UpdatedAt = DateTime.UtcNow;

            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return task;
        }

        public async Task Delete(long? taskId)
        {
            TaskEntity task = await this.Get(taskId);

            this._context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ChangeStatusDone(long? taskId)
        {
            TaskEntity task = await this.Get(taskId);

            task.Done = !task.Done;

            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return task.Done;
        }

        public async Task<PaginatedList<TaskEntity>> GetAllByUser(
            string? userId, int pageNumber = 1, int pageSize = 10 
        )
        {
            ApplicationUser user = await this._userRepository.Get(userId);

            IQueryable<TaskEntity> query = _context.Tasks
                .AsNoTracking()
                .Where(t => t.UserId == userId!);

            return await PaginatedList<TaskEntity>.CreateAsync(query, pageNumber, pageSize);
        }

    }
}