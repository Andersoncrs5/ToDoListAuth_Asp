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
        Task Delete(ApplicationUser user);
        Task<ApplicationUser> Update(ApplicationUser user, UpdateUserDto userDto);
    }
}