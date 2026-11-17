using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Partilhae_Net.Data;

namespace Partilhae_Net.Tests.Helpers;

public static class TestDbContextFactory
{
    public static (AppDbContext Context, SqliteConnection Connection)
        Criar()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);

        context.Database.EnsureCreated();

        return (context, connection);
    }
}