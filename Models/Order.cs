using System.ComponentModel.DataAnnotations;

namespace MiniShop.Models;

public class Order
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public string CustomerEmail { get; set; } = "";

    [Required(ErrorMessage = "Укажите ФИО")]
    [StringLength(150)]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Укажите телефон")]
    [Phone(ErrorMessage = "Введите корректный телефон")]
    [StringLength(30)]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Укажите адрес доставки")]
    [StringLength(300)]
    public string Address { get; set; } = "";

    [Required(ErrorMessage = "Выберите способ оплаты")]
    public string PaymentMethod { get; set; } = "";

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Новый";

    public decimal Total { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
