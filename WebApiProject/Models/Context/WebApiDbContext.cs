using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using WebApiProject.Models;
using Microsoft.AspNetCore.Identity;

namespace WebApiProject.Data;

public partial class WebApiDbContext : IdentityDbContext<User, IdentityRole, string>
{
    public WebApiDbContext(DbContextOptions<WebApiDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<app_api_client>(entity =>
        {
            entity.HasKey(e => e.client_id);
            entity.Property(e => e.client_name).HasMaxLength(100);
            entity.Property(e => e.client_id).HasMaxLength(50);
            entity.Property(e => e.api_key_hash).HasMaxLength(255);
            entity.Property(e => e.created_at).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.updated_at).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");
        });
    }
}