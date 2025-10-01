// DTOs/BookDto.cs
public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
    public string Seller { get; set; } = "";
}

// DTOs/UserDto.cs
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public decimal Budget { get; set; }
    public bool IsSeller { get; set; }
}

// DTOs/OrderDto.cs
public class OrderDto
{
    public int Id { get; set; }
    public string BookTitle { get; set; } = "";
    public string Buyer { get; set; } = "";
    public DateTime OrderDate { get; set; }
}
