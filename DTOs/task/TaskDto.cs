using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TodoListJwt.entities;

namespace TodoListJwt.DTOs.task
{
    public class TaskDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; } = string.Empty;

        [Required]
        public bool Done { get; set; } = false;

        public TaskEntity toTaskEntity() 
        {
            return new TaskEntity
            {
                Title = Title,
                Description = Description,
                Done = Done,
            };
        }

    }
}