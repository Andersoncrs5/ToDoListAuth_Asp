using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoListJwt.SetRepositories.IRepositories;

namespace TodoListJwt.SetUnitOfWork
{
    public interface IUnitOfWork: IDisposable
    {
        IUserRepository UserRepository {get;}

        ITaskRepository TaskRepository { get; } 

        Task Commit();
    }
}