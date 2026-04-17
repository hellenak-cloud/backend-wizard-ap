using Microsoft.EntityFrameworkCore;
using BackendWizardAPI.Models;
namespace BackendWizardAPI.Data;
public class AppDbContext : DbContext
{
 public AppDbContext(DbContextOptions<AppDbContext> options) 
 : base(options)
  {
  }
  public DbSet<Profile> Profiles => Set<Profile>();
} 