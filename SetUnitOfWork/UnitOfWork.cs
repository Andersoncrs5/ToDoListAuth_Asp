using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using TodoListJwt.Context;
using TodoListJwt.entities;
using TodoListJwt.SetRepositories.IRepositories;
using TodoListJwt.SetRepositories.Repositories;

namespace TodoListJwt.SetUnitOfWork
{
    public class UnitOfWork: IUnitOfWork, IDisposable
    {
        private readonly  AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private IUserRepository _userRepository;
        private ITaskRepository _taskRepository;

        public UnitOfWork(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public IUserRepository UserRepository
            => _userRepository ??= new UserRepository(_context, _userManager);

        public ITaskRepository TaskRepository 
            => _taskRepository ??= new TaskRepository(_context);

        public async Task Commit()
        {
            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}