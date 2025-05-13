using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoListJwt.DTOs.user
{
    public class TokenDto
    {
        public string? AcessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}