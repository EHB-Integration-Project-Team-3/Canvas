using System;
using System.Collections.Generic;
using System.Text;

namespace CanvasRabbitMQSender.UserRepo
{
    class User
    {
        public User() { }

        public User(int id, string sortable_name, string email, string role, DateTime createdAt, DateTime updatedAt, bool deleted)
        {
            Id = id;
            Sortable_name = sortable_name;
            Email = email;
            Role = role;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Deleted = deleted;
        }

        public int Id { get; set; }
        public string Sortable_name { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string UUID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
    }
}

