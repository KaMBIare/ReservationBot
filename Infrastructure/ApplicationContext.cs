using Domain;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure;

public class ApplicationContext : DbContext
{
    public DbSet<Reservation> Reservations { get; set; }

    public ApplicationContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //подключаемся к бд
        //ConnectToPostgres(optionsBuilder);
        ConnectToSqlite(optionsBuilder);


       
    }
    private void ConnectToSqlite(DbContextOptionsBuilder optionsBuilder)
    {
        string stringConnection;
        using (StreamReader streamReader = new StreamReader("TextConnectionSqlite.txt"))
        {
            stringConnection = streamReader.ReadToEnd();
        }

        optionsBuilder.UseSqlite(stringConnection);
    }

    private void ConnectToPostgres(DbContextOptionsBuilder optionsBuilder)
    {
        string stringConnection;
        using (StreamReader streamReader = new StreamReader("TextConnectionPostgres.txt"))
        {
            stringConnection = streamReader.ReadToEnd();
        }

        optionsBuilder.UseNpgsql(stringConnection);
    }
}