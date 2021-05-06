using System;
using System.Collections.Generic;
using System.Text;

namespace CanvasRabbitMQSender
{
    class Event
    {
        public Event(int id, string title, string description, string locationName, string locationAddress, DateTime startAt, DateTime endAt, int contextId, string contextType, DateTime createdAt, DateTime updatedAt, bool deleted) 
        {
            Id = id;
            Title = title;
            Description = description;
            LocationName = locationName;
            LocationAddress = locationAddress;
            StartAt = startAt;
            EndAt = endAt;
            ContextId = contextId;
            ContextType = contextType;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Deleted = deleted;
        }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string LocationName { get; set; }
        public string LocationAddress { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int ContextId { get; set; }
        public string ContextType { get; set; }
        public string OrganiserId { get; set; }
        public string UUID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }

    }
}