using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoListJwt.entities
{
    public class TaskEntity
    {
        public long Id { get; set; }
        public string title { get; set; } = string.Empty;

        public string description { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; }

    }
}