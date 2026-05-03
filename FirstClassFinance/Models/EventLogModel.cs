using System;
using System.ComponentModel.DataAnnotations;

namespace FirstClassFinance.Models
{
    public class EventLogModel
    {
        public int EventLogID { get; set; } // PK for this model's table
    
        [Required]
        public int EntityID { get; set; } // FK from the respective DB table
    
        [Required]
        public string EntityName { get; set; } // eg an account
    
        [Required]
        public string ActionType { get; set; } // was this an activation, deactivation, deletion etc
    
        [Required]
        public int UserID { get; set; } // who changed the Entity, FK from the respective DB table
    
        public string EntityBeforeState  { get; set; } // JSON snapshot of the entity before it's changed
        public string EntityAfterState { get; set; } // JSON snapshot of the entity after it's been changed
    
        [Required]
        public DateTime ChangeTimeStamp { get; set; } = DateTime.UtcNow; // receipt for when change happened
    }
}