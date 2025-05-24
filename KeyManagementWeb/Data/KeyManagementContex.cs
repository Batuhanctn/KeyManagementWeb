using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KeyManagementWeb.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace KeyManagementWeb.Data
{
    public class KeyManagementContext : DbContext
    {
        public KeyManagementContext(DbContextOptions<KeyManagementContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Key> Keys { get; set; }
        public DbSet<KeyHistory> KeyHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Key>().ToTable("keys");
            modelBuilder.Entity<KeyHistory>().ToTable("keyhistory");
        }
    }
}
