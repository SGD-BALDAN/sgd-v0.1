using Microsoft.EntityFrameworkCore;

namespace SgdBaldan.Data;

public class SgdDbContext : DbContext
{
    public SgdDbContext(DbContextOptions<SgdDbContext> opcoes)
        : base(opcoes)
    {
    }
}