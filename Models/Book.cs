// Models/Book.cs
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int SellerId { get; set; }
    public User? Seller { get; set; }
}

// Models/User.cs
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public decimal Budget { get; set; }
    public bool IsSeller { get; set; }
    public ICollection<Book>? Books { get; set; }
    public ICollection<Order>? Orders { get; set; }
}

// Models/Order.cs
public class Order
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime OrderDate { get; set; }
}

// Models/Category.cs
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<Book>? Books { get; set; }
}

// Models/SellerProfile.cs
public class SellerProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string StoreName { get; set; } = "";
}
