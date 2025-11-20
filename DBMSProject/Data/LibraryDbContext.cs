using System;
using System.Collections.Generic;
using DBMSProject.Models;
using Microsoft.EntityFrameworkCore;

namespace DBMSProject.Data;

public partial class LibraryDbContext : DbContext
{
    public LibraryDbContext()
    {
    }

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Librarian> Librarians { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=LibraryDb;Username=postgres;Password=Manisa45");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("AuthorsPK");

            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasMany(d => d.Books).WithMany(p => p.Authors)
                .UsingEntity<Dictionary<string, object>>(
                    "BookAuthor",
                    r => r.HasOne<Book>().WithMany()
                        .HasForeignKey("BooksId")
                        .HasConstraintName("BookAuthor_BookFK"),
                    l => l.HasOne<Author>().WithMany()
                        .HasForeignKey("AuthorsId")
                        .HasConstraintName("BookAuthor_AuthorFK"),
                    j =>
                    {
                        j.HasKey("AuthorsId", "BooksId").HasName("BookAuthorPK");
                        j.ToTable("BookAuthor");
                    });
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Books_pkey");

            entity.HasIndex(e => e.Isbn, "Books_ISBN_key").IsUnique();

            entity.Property(e => e.IsBorrowed).HasDefaultValue(false);
            entity.Property(e => e.Isbn)
                .HasMaxLength(20)
                .HasColumnName("ISBN");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Student).WithMany(p => p.Books)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("StudentId_FK");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CategoriesPK");

            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasMany(d => d.Books).WithMany(p => p.Categories)
                .UsingEntity<Dictionary<string, object>>(
                    "BookCategory",
                    r => r.HasOne<Book>().WithMany()
                        .HasForeignKey("BooksId")
                        .HasConstraintName("BookCategory_BookFK"),
                    l => l.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoriesId")
                        .HasConstraintName("BookCategory_CategoryFK"),
                    j =>
                    {
                        j.HasKey("CategoriesId", "BooksId").HasName("BookCategoryPK");
                        j.ToTable("BookCategory");
                    });
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GenresPK");

            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasMany(d => d.Books).WithMany(p => p.Genres)
                .UsingEntity<Dictionary<string, object>>(
                    "BookGenre",
                    r => r.HasOne<Book>().WithMany()
                        .HasForeignKey("BooksId")
                        .HasConstraintName("BookGenre_BookFK"),
                    l => l.HasOne<Genre>().WithMany()
                        .HasForeignKey("GenresId")
                        .HasConstraintName("BookGenre_GenreFK"),
                    j =>
                    {
                        j.HasKey("GenresId", "BooksId").HasName("BookGenrePK");
                        j.ToTable("BookGenre");
                    });
        });

        modelBuilder.Entity<Librarian>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("LibrarianPK");

            entity.ToTable("Librarian");

            entity.HasIndex(e => e.EmployeeId, "Librarian_EmployeeID_key").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(50)
                .HasColumnName("EmployeeID");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Librarian)
                .HasForeignKey<Librarian>(d => d.Id)
                .HasConstraintName("LibrarianUser");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ReviewsPK");

            entity.Property(e => e.Description).HasMaxLength(250);
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.Book).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Reviews_BookFK");

            entity.HasOne(d => d.Student).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("Reviews_StudentFK");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("StudentPK");

            entity.ToTable("Student");

            entity.HasIndex(e => e.StudentId, "Student_StudentID_key").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Gpa)
                .HasPrecision(3, 2)
                .HasColumnName("GPA");
            entity.Property(e => e.StudentId)
                .HasMaxLength(50)
                .HasColumnName("StudentID");

            entity.HasOne(d => d.IdNavigation).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.Id)
                .HasConstraintName("StudentUser");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserPK");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "User_Email_key").IsUnique();

            entity.HasIndex(e => e.UserName, "User_UserName_key").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.UserName).HasMaxLength(100);
            entity.Property(e => e.UserType).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
