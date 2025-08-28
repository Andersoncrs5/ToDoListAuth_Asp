using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoListJwt.utils.response
{
    public class Tokens
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpirationToken  { get; set; }
        public DateTime? ExpirationRefreshToken  { get; set; }
    }
}