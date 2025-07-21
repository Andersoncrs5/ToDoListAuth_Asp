using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoListJwt.Context;
using TodoListJwt.DTOs.user;
using TodoListJwt.entities;
using TodoListJwt.SetRepositories.IRepositories;
using TodoListJwt.utils;

namespace TodoListJwt.SetRepositories.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRepository(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ApplicationUser> Get(string? id) {
            if (string.IsNullOrEmpty(id) || id == null ) 
                throw new ResponseException("Id is required", 400, "failed");
            
            ApplicationUser? user = await _userManager.FindByIdAsync(id);

            if (user == null) 
                throw new ResponseException("User not found", 404, "failed");

            return user;
        }

        public async Task Delete(ApplicationUser user) 
        {
            if (string.IsNullOrEmpty(user.Id))
                throw new ResponseException("Id is required", 400, "failed");

            await _userManager.DeleteAsync(user);
        }

        public async Task<ApplicationUser> Update(ApplicationUser user, UpdateUserDto userDto) 
        {
            user.UserName = userDto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(userDto.Password))
            {
                PasswordHasher<ApplicationUser>?  passwordHasher = new PasswordHasher<ApplicationUser>();
                user.PasswordHash = passwordHasher.HashPassword(user, userDto.Password);
            }

            IdentityResult? result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new ResponseException("Update failed", 500, "failed");
            }

            return user;
        }


    }
}