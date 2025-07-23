﻿using Ecom_api.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Ecom_api.Models
{

    public class Order
    {
        public int Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = SD.OrderStatusPending;

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }


        [Required]
        public string paymentStatus { get; set; } = SD.OrderStatusPending;


        [StringLength(200)]
        public string? PaymentSessionId { get; set; }
    }
}
