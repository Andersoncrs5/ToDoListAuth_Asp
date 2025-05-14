using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoListJwt.DTOs.user;
using TodoListJwt.entities;

namespace TodoListJwt.SetRepositories.IRepositories
{
    public interface IUserRepository
    {
        Task<ApplicationUser> Get(string? id);
        Task Delete(string? id);
        Task<ApplicationUser> Update(string? Id, UpdateUserDto userDto);
    }
}