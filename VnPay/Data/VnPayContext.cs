using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VnPay.Models;

namespace VnPay.Data
{
    public class VnPayContext : DbContext
    {
        public VnPayContext (DbContextOptions<VnPayContext> options)
            : base(options)
        {
        }
        public override int SaveChanges()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                ((BaseEntity)entityEntry.Entity).UpdatedDate = DateTime.Now;

                ((BaseEntity)entityEntry.Entity).CreatedDate= ((BaseEntity)entityEntry.Entity).CreatedDate ?? DateTime.Now;

            }

            return base.SaveChanges();
        }
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                ((BaseEntity)entityEntry.Entity).UpdatedDate = DateTime.Now;

                ((BaseEntity)entityEntry.Entity).CreatedDate= ((BaseEntity)entityEntry.Entity).CreatedDate ?? DateTime.Now;

            }
            return await base.SaveChangesAsync(true, cancellationToken).ConfigureAwait(false);
        }

        public DbSet<VnPay.Models.Order> Order { get; set; } = default!;
    }
}
