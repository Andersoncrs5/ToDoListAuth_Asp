using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace TodoListJwt.entities
{
    public class ApplicationUser: IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }

        // [Required]
        [JsonIgnore]
        public virtual ICollection<TaskEntity> Tasks { get; set; } = new List<TaskEntity>();

    }
}