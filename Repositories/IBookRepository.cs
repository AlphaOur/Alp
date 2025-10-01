using Microsoft.EntityFrameworkCore;
// Repositories/IBookRepository.cs
public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();
    Task<Book?> GetByIdAsync(int id);
    Task AddAsync(Book book);
    Task UpdateAsync(Book book);
    Task DeleteAsync(int id);
}

// Repositories/BookRepository.cs
public class BookRepository : IBookRepository
{
    private readonly AppDbContext _context;
    public BookRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Book>> GetAllAsync() => await _context.Books.Include(b => b.Category).Include(b => b.Seller).ToListAsync();
    public async Task<Book?> GetByIdAsync(int id) => await _context.Books.Include(b => b.Category).Include(b => b.Seller).FirstOrDefaultAsync(b => b.Id == id);
    public async Task AddAsync(Book book) { _context.Books.Add(book); await _context.SaveChangesAsync(); }
    public async Task UpdateAsync(Book book) { _context.Books.Update(book); await _context.SaveChangesAsync(); }
    public async Task DeleteAsync(int id) { var book = await _context.Books.FindAsync(id); if (book != null) { _context.Books.Remove(book); await _context.SaveChangesAsync(); } }
}
