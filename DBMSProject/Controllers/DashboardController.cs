using DBMSProject.Data;
using DBMSProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static System.Reflection.Metadata.BlobBuilder;

namespace DBMSProject.Controllers
{
    public class DashboardController : Controller
    {
        private LibraryDbContext db = new LibraryDbContext();
        string _connection = "Host=localhost;Port=5432;Database=LibraryDb;Username=postgres;Password=Manisa45";


        public IActionResult StudentDashboard()
        {
            return View();
        }
        public IActionResult LibrarianDashboard()
        {
            return View();
        }

        public User GetLoggedInUser()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userName = HttpContext.Session.GetString("UserName");
            var userType = HttpContext.Session.GetString("UserType");

            if (userId == null || userType == null)
            {
                return null;  // No user is logged in
            }

            // Assuming you have a method to retrieve the full user details from the DB or memory
            var user = db.Users.FirstOrDefault(x => x.Id == userId);  
            user.UserType = userType;  // Set the UserType fetched from session
            user.UserName = userName;
            return user;
        }

        public IActionResult SearchBooks(string searchTerm)
        {
            var loggedInUser = GetLoggedInUser();


            ViewBag.UserType = loggedInUser?.UserType;
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return View("ListBooks", new List<Book>()); // Pass an empty list of books
            }

            string sqlQuery = @"
                                SELECT * FROM SearchBooks(@SearchTerm);
                            ";

            var booksWithDetails = new List<Book>();

            using (var connection = db.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand() )
                {
                    command.CommandText = sqlQuery;
                    command.Parameters.Add(new NpgsqlParameter("@SearchTerm", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = searchTerm });

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var bookId = reader.GetInt32(reader.GetOrdinal("Id"));
                            var existingBook = booksWithDetails.Find(b => b.Id == bookId);

                            if (existingBook == null)
                            {
                                var newBook = new Book
                                {
                                    Id = bookId,
                                    Title = reader.GetString(reader.GetOrdinal("Title")),
                                    Isbn = reader.GetString(reader.GetOrdinal("ISBN")),
                                    PublishedYear = reader.GetInt32(reader.GetOrdinal("PublishedYear")),
                                    IsBorrowed = reader.GetBoolean(reader.GetOrdinal("IsBorrowed")),
                                    StudentId = reader.IsDBNull(reader.GetOrdinal("StudentId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Authors = new List<Author>(),
                                    Genres = new List<Genre>(),
                                    Categories = new List<Category>()
                                };

                                booksWithDetails.Add(newBook);
                                existingBook = newBook;
                            }

                            // Add related authors, genres, categories, and student (same logic as before)
                            var authorName = reader.IsDBNull(reader.GetOrdinal("AuthorName")) ? null : reader.GetString(reader.GetOrdinal("AuthorName"));
                            if (authorName != null && !existingBook.Authors.Any(a => a.Name == authorName))
                            {
                                existingBook.Authors.Add(new Author
                                {
                                    Name = authorName
                                });
                            }

                            var genreName = reader.IsDBNull(reader.GetOrdinal("GenreName")) ? null : reader.GetString(reader.GetOrdinal("GenreName"));
                            if (genreName != null && !existingBook.Genres.Any(g => g.Name == genreName))
                            {
                                existingBook.Genres.Add(new Genre
                                {
                                    Name = genreName
                                });
                            }

                            var categoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName"));
                            if (categoryName != null && !existingBook.Categories.Any(c => c.Name == categoryName))
                            {
                                existingBook.Categories.Add(new Category
                                {
                                    Name = categoryName
                                });
                            }

                            // Add Student info
                            if (!reader.IsDBNull(reader.GetOrdinal("StudentName")))
                            {
                                existingBook.Student = new Student
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    IdNavigation = new User
                                    {
                                        UserName = reader.GetString(reader.GetOrdinal("StudentName")),
                                        UserType = reader.GetString(reader.GetOrdinal("UserType"))
                                    }
                                };
                            }
                        }
                    }
                }
            }
            ViewBag.SearchTerm = searchTerm;    

            return View("ListBooks",booksWithDetails); // Return filtered books to the view
        }

        public IActionResult ListBooks()
        {
            var loggedInUser = GetLoggedInUser();   

            ViewBag.UserType = loggedInUser?.UserType;

            // Define the query
            string sqlQuery = @"
                SELECT 
                    b.""Id"",
                    b.""Title"",
                    b.""ISBN"",
                    b.""PublishedYear"",
                    b.""IsBorrowed"",
                    b.""StudentId"",
                    g.""Name"" AS ""GenreName"",
                    a.""Name"" AS ""AuthorName"",
                    c.""Name"" AS ""CategoryName"",
                    u.""UserName"" AS ""StudentName"",
                    u.""UserType""
                FROM ""Books"" b
                LEFT JOIN ""BookGenre"" bg ON b.""Id"" = bg.""BooksId""
                LEFT JOIN ""Genres"" g ON bg.""GenresId"" = g.""Id""
                LEFT JOIN ""BookAuthor"" ba ON b.""Id"" = ba.""BooksId""
                LEFT JOIN ""Authors"" a ON ba.""AuthorsId"" = a.""Id""
                LEFT JOIN ""BookCategory"" bc ON b.""Id"" = bc.""BooksId""
                LEFT JOIN ""Categories"" c ON bc.""CategoriesId"" = c.""Id""
                LEFT JOIN ""Student"" s ON b.""StudentId"" = s.""Id""
                LEFT JOIN ""User"" u ON s.""Id"" = u.""Id""
";

            // Execute the query and fetch the raw data
            var booksWithDetails = new List<Book>();

            using (var connection = db.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Map the results to the Book model
                            var bookId = reader.GetInt32(reader.GetOrdinal("Id"));

                            // Check if this book is already in the list
                            var existingBook = booksWithDetails.Find(b => b.Id == bookId);

                            if (existingBook == null)
                            {
                                // Add a new book
                                var newBook = new Book
                                {
                                    Id = bookId,
                                    Title = reader.GetString(reader.GetOrdinal("Title")),
                                    Isbn = reader.GetString(reader.GetOrdinal("ISBN")),
                                    PublishedYear = reader.GetInt32(reader.GetOrdinal("PublishedYear")),
                                    IsBorrowed = reader.GetBoolean(reader.GetOrdinal("IsBorrowed")),
                                    StudentId = reader.IsDBNull(reader.GetOrdinal("StudentId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    Authors = new List<Author>(),
                                    Genres = new List<Genre>(),
                                    Categories = new List<Category>()
                                };

                                booksWithDetails.Add(newBook);
                                existingBook = newBook;
                            }

                            // Add related genres
                            if (!reader.IsDBNull(reader.GetOrdinal("StudentId")))
                            {
                                existingBook.Student = new Student
                                {

                                    Id = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    IdNavigation = new User
                                    {
                                        UserName = reader.GetString(reader.GetOrdinal("StudentName")),
                                        UserType = reader.GetString(reader.GetOrdinal("UserType"))

                                    }
                                };
                             }

                            var authorName = reader.IsDBNull(reader.GetOrdinal("AuthorName")) ? null : reader.GetString(reader.GetOrdinal("AuthorName"));
                            if (authorName != null && !existingBook.Authors.Any(a => a.Name == authorName))
                            {
                                existingBook.Authors.Add(new Author
                                {
                                    Name = authorName
                                });
                            }

                            var genreName = reader.IsDBNull(reader.GetOrdinal("GenreName")) ? null : reader.GetString(reader.GetOrdinal("GenreName"));
                            if (genreName != null && !existingBook.Genres.Any(g => g.Name == genreName))
                            {
                                existingBook.Genres.Add(new Genre
                                {
                                    Name = genreName
                                });
                            }

                  
                            var categoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName"));
                            if (categoryName != null && !existingBook.Categories.Any(c => c.Name == categoryName))
                            {
                                existingBook.Categories.Add(new Category
                                {
                                    Name = categoryName
                                });
                            }

                         


                        }
                    }
                }
            }

            return View(booksWithDetails); // Pass the final list to the view
        }

        // GET: CreateBook
        // GET: CreateBook
        public IActionResult CreateBook()
        {
            var book = new Book();
            var allAuthors = new List<Author>();
            var allGenres = new List<Genre>();
            var allCategories = new List<Category>();

            string bookQuery = @"
       

                     SELECT 
                  b.""Id"",
                  b.""Title"",
                  b.""ISBN"",
                  b.""PublishedYear"",
                  b.""IsBorrowed"",
                  array_agg(a.""Id"") AS ""AuthorIds"",
                  array_agg(g.""Id"") AS ""GenreIds"",
                  array_agg(c.""Id"") AS ""CategoryIds"",
                  array_agg(a.""Name"") AS ""AuthorNames"",
                  array_agg(g.""Name"") AS ""GenreNames"",
                  array_agg(c.""Name"") AS ""CategoryNames""
              FROM ""Books"" b
              LEFT JOIN ""BookAuthor"" ba ON b.""Id"" = ba.""BooksId""
              LEFT JOIN ""Authors"" a ON ba.""AuthorsId"" = a.""Id""
              LEFT JOIN ""BookGenre"" bg ON b.""Id"" = bg.""BooksId""
              LEFT JOIN ""Genres"" g ON bg.""GenresId"" = g.""Id""
              LEFT JOIN ""BookCategory"" bc ON b.""Id"" = bc.""BooksId""
              LEFT JOIN ""Categories"" c ON bc.""CategoriesId"" = c.""Id""
              GROUP BY b.""Id""
             
       ";

            string authorsQuery = @"SELECT ""Id"", ""Name"" FROM ""Authors""";

            // Query to fetch all genres
            string genresQuery = @"SELECT ""Id"", ""Name"" FROM ""Genres""";

            // Query to fetch all categories
            string categoriesQuery = @"SELECT ""Id"", ""Name"" FROM ""Categories""";

            using (var connection = db.Database.GetDbConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = bookQuery;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            book.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                            book.Title = reader.GetString(reader.GetOrdinal("Title"));
                            book.Isbn = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? null : reader.GetString(reader.GetOrdinal("ISBN"));
                            book.PublishedYear = reader.IsDBNull(reader.GetOrdinal("PublishedYear")) ? null : reader.GetInt32(reader.GetOrdinal("PublishedYear"));
                            book.IsBorrowed = reader.GetBoolean(reader.GetOrdinal("IsBorrowed"));
                            book.Genres = reader.IsDBNull(reader.GetOrdinal("GenreIds")) || reader.IsDBNull(reader.GetOrdinal("GenreNames"))
                                  ? new List<Genre>()
                                  : reader.GetFieldValue<int[]>(reader.GetOrdinal("GenreIds"))
                                      .Zip(reader.GetFieldValue<string[]>(reader.GetOrdinal("GenreNames")), (id, name) => new Genre { Id = id, Name = name })
                                      .ToList();

                            book.Authors = reader.IsDBNull(reader.GetOrdinal("AuthorIds")) || reader.IsDBNull(reader.GetOrdinal("AuthorNames"))
                                ? new List<Author>()
                                : reader.GetFieldValue<int[]>(reader.GetOrdinal("AuthorIds"))
                                    .Zip(reader.GetFieldValue<string[]>(reader.GetOrdinal("AuthorNames")), (id, name) => new Author { Id = id, Name = name })
                                    .ToList();

                            book.Categories = reader.IsDBNull(reader.GetOrdinal("CategoryIds")) || reader.IsDBNull(reader.GetOrdinal("CategoryNames"))
                                ? new List<Category>()
                                : reader.GetFieldValue<int[]>(reader.GetOrdinal("CategoryIds"))
                                    .Zip(reader.GetFieldValue<string[]>(reader.GetOrdinal("CategoryNames")), (id, name) => new Category { Id = id, Name = name })
                                    .ToList();
                        }
                    }
                }


                using (var command = connection.CreateCommand())
                {
                    command.CommandText = authorsQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allAuthors.Add(new Author
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }

                // Fetch all genres
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = genresQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allGenres.Add(new Genre
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }

                // Fetch all categories
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = categoriesQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allCategories.Add(new Category
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }
            }

            ViewBag.AllAuthors = allAuthors;
            ViewBag.AllGenres = allGenres;
            ViewBag.AllCategories = allCategories;
            return View(book);
        }

        // POST: CreateBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateBook(Book book, int[] AuthorIds, int[] GenreIds, int[] CategoryIds)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string authorQuery = @"
                                            SELECT ""Id"", ""Name"" 
                                            FROM ""Authors"" 
                                            WHERE ""Id"" = ANY(@AuthorIds)";

                    string genreQuery = @"
                                            SELECT ""Id"", ""Name"" 
                                            FROM ""Genres"" 
                                            WHERE ""Id"" = ANY(@GenreIds)"
                    ;


                    string categoryQuery = @"
                                                SELECT ""Id"", ""Name"" 
                                                FROM ""Categories"" 
                                                WHERE ""Id"" = ANY(@CategoryIds)";
                    using (var connection = db.Database.GetDbConnection())
                    {
                        connection.Open();

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = authorQuery;
                            command.Parameters.Add(new NpgsqlParameter("@AuthorIds", AuthorIds.ToArray()));

                            using (var reader = command.ExecuteReader())
                            {
                                book.Authors = new List<Author>();

                                while(reader.Read())
                                {
                                    book.Authors.Add(new Author
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                        Name = reader.GetString(reader.GetOrdinal("Name"))
                                    });
                                }
                            }
                        }

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = genreQuery;
                            command.Parameters.Add(new NpgsqlParameter("@GenreIds", GenreIds.ToArray()));

                            using (var reader = command.ExecuteReader())
                            {
                                book.Genres = new List<Genre>();

                                while (reader.Read())
                                {
                                    book.Genres.Add(new Genre
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                        Name = reader.GetString(reader.GetOrdinal("Name"))
                                    });
                                }
                            }
                        }
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = categoryQuery;
                            command.Parameters.Add(new NpgsqlParameter("@CategoryIds", CategoryIds.ToArray()));

                            using (var reader = command.ExecuteReader())
                            {
                                book.Categories = new List<Category>();

                                while (reader.Read())
                                {
                                    book.Categories.Add(new Category
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                        Name = reader.GetString(reader.GetOrdinal("Name"))
                                    });
                                }
                            }
                        }

                    }

                    CreateBookInDatabase(book); // Create the book in the database
                    TempData["SuccessMessage"] = "Book created successfully!";
                    return RedirectToAction("StudentDashboard","Dashboard"); // Redirect to a list of books, for example
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Error: {ex.Message}";
                    return View(book); // Return the form with the error message
                }
            }

            return View(book); // Return the form if validation failed
        }


        // This method calls the database function to create the book
        private void CreateBookInDatabase(Book book)
        {
            using (var connection = new NpgsqlConnection(_connection))
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand("SELECT public.create_book(@Title, @ISBN, @PublishedYear, @IsBorrowed, @GenreIds,@AuthorIds, @CategoryIds)", connection))
                {
                    cmd.CommandType = CommandType.Text;

                    cmd.Parameters.AddWithValue("Title", book.Title);
                    cmd.Parameters.AddWithValue("ISBN", book.Isbn ?? (object)DBNull.Value); // Handle null ISBN
                    cmd.Parameters.AddWithValue("PublishedYear", book.PublishedYear ?? (object)DBNull.Value); // Handle null PublishedYear
                    cmd.Parameters.AddWithValue("IsBorrowed", book.IsBorrowed);

                    // Pass the IDs of the related Authors, Genres, and Categories as arrays
                    cmd.Parameters.AddWithValue("GenreIds", book.Genres != null ? book.Genres.Select(g => g.Id).ToArray() : new int[0]);
                    cmd.Parameters.AddWithValue("AuthorIds", book.Authors != null ? book.Authors.Select(a => a.Id).ToArray() : new int[0]);
                    cmd.Parameters.AddWithValue("CategoryIds", book.Categories != null ? book.Categories.Select(c => c.Id).ToArray() : new int[0]);

                    cmd.ExecuteNonQuery(); // Execute the function
                }
            }
        }



        // GET: UpdateBooHttpGetk
        [HttpGet]
        public IActionResult UpdateBook(int id)
        {
            var book = new Book();
            var allAuthors = new List<Author>();
            var allGenres = new List<Genre>();
            var allCategories = new List<Category>();

            // Define the query to fetch the book with related authors, genres, and categories
            string bookQuery = @"
                                SELECT 
                                    b.""Id"",
                                    b.""Title"",
                                    b.""ISBN"",
                                    b.""PublishedYear"",
                                    b.""IsBorrowed"",
                                    array_agg(a.""Id"") AS ""AuthorIds"",
                                    array_agg(g.""Id"") AS ""GenreIds"",
                                    array_agg(c.""Id"") AS ""CategoryIds"",
                                    array_agg(a.""Name"") AS ""AuthorNames"",
                                    array_agg(g.""Name"") AS ""GenreNames"",
                                    array_agg(c.""Name"") AS ""CategoryNames""
                                FROM ""Books"" b
                                LEFT JOIN ""BookAuthor"" ba ON b.""Id"" = ba.""BooksId""
                                LEFT JOIN ""Authors"" a ON ba.""AuthorsId"" = a.""Id""
                                LEFT JOIN ""BookGenre"" bg ON b.""Id"" = bg.""BooksId""
                                LEFT JOIN ""Genres"" g ON bg.""GenresId"" = g.""Id""
                                LEFT JOIN ""BookCategory"" bc ON b.""Id"" = bc.""BooksId""
                                LEFT JOIN ""Categories"" c ON bc.""CategoriesId"" = c.""Id""
                                WHERE b.""Id"" = @BookId
                                GROUP BY b.""Id""";

            // Query to fetch all authors
            string authorsQuery = @"SELECT ""Id"", ""Name"" FROM ""Authors""";

            // Query to fetch all genres
            string genresQuery = @"SELECT ""Id"", ""Name"" FROM ""Genres""";

            // Query to fetch all categories
            string categoriesQuery = @"SELECT ""Id"", ""Name"" FROM ""Categories""";

            using (var connection = db.Database.GetDbConnection())
            {
                connection.Open();

                // Fetch the selected book
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = bookQuery;
                    command.Parameters.Add(new NpgsqlParameter("@BookId", id));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            book.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                            book.Title = reader.GetString(reader.GetOrdinal("Title"));
                            book.Isbn = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? null : reader.GetString(reader.GetOrdinal("ISBN"));
                            book.PublishedYear = reader.IsDBNull(reader.GetOrdinal("PublishedYear")) ? null : reader.GetInt32(reader.GetOrdinal("PublishedYear"));
                            book.IsBorrowed = reader.GetBoolean(reader.GetOrdinal("IsBorrowed"));
                            book.Genres = reader.IsDBNull(reader.GetOrdinal("GenreIds")) || reader.IsDBNull(reader.GetOrdinal("GenreNames"))
                                  ? new List<Genre>()
                                  : reader.GetFieldValue<int[]>(reader.GetOrdinal("GenreIds"))
                                      .Zip(reader.GetFieldValue<string[]>(reader.GetOrdinal("GenreNames")), (id, name) => new Genre { Id = id, Name = name })
                                      .ToList();

                            book.Authors = reader.IsDBNull(reader.GetOrdinal("AuthorIds")) || reader.IsDBNull(reader.GetOrdinal("AuthorNames"))
                                ? new List<Author>()
                                : reader.GetFieldValue<int[]>(reader.GetOrdinal("AuthorIds"))
                                    .Zip(reader.GetFieldValue<string[]>(reader.GetOrdinal("AuthorNames")), (id, name) => new Author { Id = id, Name = name })
                                    .ToList();

                            book.Categories = reader.IsDBNull(reader.GetOrdinal("CategoryIds")) || reader.IsDBNull(reader.GetOrdinal("CategoryNames"))
                                ? new List<Category>()
                                : reader.GetFieldValue<int[]>(reader.GetOrdinal("CategoryIds"))
                                    .Zip(reader.GetFieldValue<string[]>(reader.GetOrdinal("CategoryNames")), (id, name) => new Category { Id = id, Name = name })
                                    .ToList();

                        }
                    }
                }

                // Fetch all authors
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = authorsQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allAuthors.Add(new Author
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }

                // Fetch all genres
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = genresQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allGenres.Add(new Genre
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }

                // Fetch all categories
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = categoriesQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allCategories.Add(new Category
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            });
                        }
                    }
                }
            }

            // Pass data to the view
            ViewBag.AllAuthors = allAuthors;
            ViewBag.AllGenres = allGenres;
            ViewBag.AllCategories = allCategories;

            return View(book);
        }


        // POST: UpdateBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateBook(int id,Book book, int[] AuthorIds, int[] GenreIds, int[] CategoryIds)
        {
            var allAuthors = new List<Author>();
            var allGenres = new List<Genre>();
            var allCategories = new List<Category>();

            if (ModelState.IsValid)
            {
              

                string authorQuery = @"
                                            SELECT ""Id"", ""Name"" 
                                            FROM ""Authors"" 
                                            WHERE ""Id"" = ANY(@AuthorIds)";

                string genreQuery = @"
                                            SELECT ""Id"", ""Name"" 
                                            FROM ""Genres"" 
                                            WHERE ""Id"" = ANY(@GenreIds)"
                ;


                string categoryQuery = @"
                                                SELECT ""Id"", ""Name"" 
                                                FROM ""Categories"" 
                                                WHERE ""Id"" = ANY(@CategoryIds)";
                using (var connection = db.Database.GetDbConnection())
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = authorQuery;
                        command.Parameters.Add(new NpgsqlParameter("@AuthorIds", AuthorIds.ToArray()));

                        using (var reader = command.ExecuteReader())
                        {
                            book.Authors = new List<Author>();

                            while (reader.Read())
                            {
                                book.Authors.Add(new Author
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Name = reader.GetString(reader.GetOrdinal("Name"))
                                });
                            }
                        }
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = genreQuery;
                        command.Parameters.Add(new NpgsqlParameter("@GenreIds", GenreIds.ToArray()));

                        using (var reader = command.ExecuteReader())
                        {
                            book.Genres = new List<Genre>();

                            while (reader.Read())
                            {
                                book.Genres.Add(new Genre
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Name = reader.GetString(reader.GetOrdinal("Name"))
                                });
                            }
                        }
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = categoryQuery;
                        command.Parameters.Add(new NpgsqlParameter("@CategoryIds", CategoryIds.ToArray()));

                        using (var reader = command.ExecuteReader())
                        {
                            book.Categories = new List<Category>();

                            while (reader.Read())
                            {
                                book.Categories.Add(new Category
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Name = reader.GetString(reader.GetOrdinal("Name"))
                                });
                            }
                        }
                    }

                }

                // Call the update function to update the book details in the database
                UpdateBookInDatabase(book);

                // Redirect to the list of books after the update
                return RedirectToAction("ListBooks");
            }

            // If model is not valid, return to the update view with the existing data
            ViewBag.Authors = allAuthors;
            ViewBag.Genres = allGenres;
            ViewBag.Categories = allCategories;
            return View(book);
        }

        // Function to update book in the database
        private void UpdateBookInDatabase(Book book)
        {
            using (var connection = new NpgsqlConnection(_connection))
            {
                connection.Open();

                // Call the update function in PostgreSQL (using raw SQL)
                using (var cmd = new NpgsqlCommand("SELECT public.update_book(@BookId, @Title, @ISBN, @PublishedYear, @IsBorrowed, @GenreIds,@AuthorIds, @CategoryIds)", connection))
                {
                    cmd.CommandType = CommandType.Text;

                    // Pass the updated book details as parameters
                    cmd.Parameters.AddWithValue("BookId", book.Id);
                    cmd.Parameters.AddWithValue("Title", book.Title);
                    cmd.Parameters.AddWithValue("ISBN", book.Isbn ?? (object)DBNull.Value); // Handle null ISBN
                    cmd.Parameters.AddWithValue("PublishedYear", book.PublishedYear ?? (object)DBNull.Value); // Handle null PublishedYear
                    cmd.Parameters.AddWithValue("IsBorrowed", book.IsBorrowed);
                    cmd.Parameters.AddWithValue("GenreIds", book.Genres?.Select(g => g.Id).ToArray() ?? Array.Empty<int>());
                    cmd.Parameters.AddWithValue("AuthorIds", book.Authors?.Select(a => a.Id).ToArray() ?? Array.Empty<int>());
                    cmd.Parameters.AddWithValue("CategoryIds", book.Categories?.Select(c => c.Id).ToArray() ?? Array.Empty<int>());

                    // Set NpgsqlDbType explicitly for array parameters
                    cmd.Parameters["GenreIds"].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer;
                    cmd.Parameters["AuthorIds"].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer;
                    cmd.Parameters["CategoryIds"].NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer;

                    cmd.ExecuteNonQuery(); // Execute the update function
                }
            }
        }


        // GET: DeleteBook
        [HttpGet]
        public IActionResult DeleteBook(int id)
        {
            var book = new Book();

            string bookQuery = @"
                        SELECT 
                            b.""Id"",
                            b.""Title"",
                            b.""ISBN"",
                            b.""PublishedYear"",
                            b.""IsBorrowed"",
                            array_agg(a.""Id"") AS ""AuthorIds"",
                            array_agg(g.""Id"") AS ""GenreIds"",
                            array_agg(c.""Id"") AS ""CategoryIds"",
                            array_agg(a.""Name"") AS ""AuthorNames"",
                            array_agg(g.""Name"") AS ""GenreNames"",
                            array_agg(c.""Name"") AS ""CategoryNames""
                        FROM ""Books"" b
                        LEFT JOIN ""BookAuthor"" ba ON b.""Id"" = ba.""BooksId""
                        LEFT JOIN ""Authors"" a ON ba.""AuthorsId"" = a.""Id""
                        LEFT JOIN ""BookGenre"" bg ON b.""Id"" = bg.""BooksId""
                        LEFT JOIN ""Genres"" g ON bg.""GenresId"" = g.""Id""
                        LEFT JOIN ""BookCategory"" bc ON b.""Id"" = bc.""BooksId""
                        LEFT JOIN ""Categories"" c ON bc.""CategoriesId"" = c.""Id""
                        WHERE b.""Id"" = @BookId
                        GROUP BY b.""Id""";

            using (var connection = db.Database.GetDbConnection())
            {
                connection.Open();

                // Fetch the selected book
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = bookQuery;
                    command.Parameters.Add(new NpgsqlParameter("@BookId", id));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            book.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                            book.Title = reader.GetString(reader.GetOrdinal("Title"));
                            book.Isbn = reader.IsDBNull(reader.GetOrdinal("ISBN")) ? null : reader.GetString(reader.GetOrdinal("ISBN"));
                            book.PublishedYear = reader.IsDBNull(reader.GetOrdinal("PublishedYear")) ? null : reader.GetInt32(reader.GetOrdinal("PublishedYear"));
                            book.IsBorrowed = reader.GetBoolean(reader.GetOrdinal("IsBorrowed"));

                            book.Genres = reader["GenreIds"] is DBNull || reader["GenreNames"] is DBNull
                                ? new List<Genre>()
                                : ((int[])reader["GenreIds"])
                                    .Zip(((string[])reader["GenreNames"]), (id, name) => new Genre { Id = id, Name = name })
                                    .ToList();
                            book.Authors = reader["AuthorIds"] is DBNull || reader["AuthorNames"] is DBNull
                                ? new List<Author>()
                                : ((int[])reader["AuthorIds"])
                                    .Zip(((string[])reader["AuthorNames"]), (id, name) => new Author { Id = id, Name = name })
                                    .ToList();


                            book.Categories = reader["CategoryIds"] is DBNull || reader["CategoryNames"] is DBNull
                                ? new List<Category>()
                                : ((int[])reader["CategoryIds"])
                                    .Zip(((string[])reader["CategoryNames"]), (id, name) => new Category { Id = id, Name = name })
                                    .ToList();
                        }
                    }
                }
            }

            return View(book);
        }



        // POST: DeleteBook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBookConfirmed(int id)
        {
            // Call the delete function to delete the book from the database
            DeleteBookFromDatabase(id);

            // Redirect to the list of books after the deletion
            return RedirectToAction("ListBooks");
        }

        // Function to delete book from the database
        private void DeleteBookFromDatabase(int bookId)
        {
            using (var connection = new NpgsqlConnection(_connection))
            {
                connection.Open();

                // Call the delete function in PostgreSQL (using raw SQL)
                using (var cmd = new NpgsqlCommand("SELECT public.delete_book(@BookId)", connection))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("BookId", bookId);
                    cmd.ExecuteNonQuery(); // Execute the delete function
                }
            }
        }


        public IActionResult ListReview()
        {
          
            string sqlQuery = @"
                        SELECT r.""Id"", r.""Title"", r.""Description"", r.""StudentId"" AS ""StudentId"", 
                               r.""BookId"" AS ""BookId"", u.""UserName"" AS ""StudentName"", 
                               b.""Title"" AS ""BookTitle""
                        FROM ""Reviews"" r
                        LEFT JOIN ""Student"" s ON r.""StudentId"" = s.""Id""
                        LEFT JOIN ""User"" u ON s.""Id"" = u.""Id""
                        LEFT JOIN ""Books"" b ON r.""BookId"" = b.""Id""
                       ";

            using (var connection = db.Database.GetDbConnection())
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    using (var reader = command.ExecuteReader())
                    {
                        var reviews = new List<Review>();

                        while (reader.Read())
                        {
                            reviews.Add(new Review
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Title = reader.GetString(reader.GetOrdinal("Title")),
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                StudentId = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                BookId = reader.GetInt32(reader.GetOrdinal("BookId")),
                                Book = new Book
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("BookId")),
                                    Title = reader.GetString(reader.GetOrdinal("BookTitle"))
                                },
                                Student = new Student
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("StudentId")),
                                    IdNavigation = new User
                                    {
                                        UserName = reader.GetString(reader.GetOrdinal("StudentName"))
                                    }
                                }
                            });
                        }

                        return View(reviews);
                    }
                }
            }
        }


        [HttpGet]
        // GET: Display the form to add a review
        public IActionResult AddReview(int id)
        {
            var loggedInUser = GetLoggedInUser();

            ViewBag.StudentId = loggedInUser?.Id;   
            ViewBag.UserType = loggedInUser?.UserType;
            ViewBag.UserName = loggedInUser?.UserName;

            Console.WriteLine($"HttpGet - UserId: {loggedInUser.Id}, UserName: {loggedInUser.UserName}, BookId: {id}");

            // Pass the BookId and StudentId to the view
            ViewBag.BookId = id;
            return View();
        }



        [HttpPost]
        public IActionResult AddReview(int bookId,int studentId, string title, string description)
        {
            var loggedInUser = GetLoggedInUser();

            if (loggedInUser == null)
            {
                throw new Exception("Logged-in user is null. Cannot retrieve StudentId.");
            }

            

            Console.WriteLine($"Received: student:{studentId} bookId={bookId}, title={title}, description={description}");


            string insertQuery = @"INSERT INTO ""Reviews"" (""Title"", ""Description"", ""StudentId"", ""BookId"")
                               VALUES (@Title, @Description, @StudentId, @BookId)";

            using (var connection = db.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = insertQuery;

                    command.Parameters.Add(new NpgsqlParameter("@Title", title));
                    command.Parameters.Add(new NpgsqlParameter("@Description", description));
                    command.Parameters.Add(new NpgsqlParameter("@StudentId", studentId));
                    command.Parameters.Add(new NpgsqlParameter("@BookId", bookId));

                    command.ExecuteNonQuery();
                }
            }

            // After saving the review, redirect to the ListReview page
            return RedirectToAction("ListReview");
        }


        public IActionResult BorrowedBooks()
        {
            var borrowedBooks = new List<Book>();

            try
            {
                using (var connection = new NpgsqlConnection(_connection))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand(@"SELECT * FROM ""Books"" WHERE ""IsBorrowed"" = TRUE;", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Assuming your Books table has these columns
                                borrowedBooks.Add(new Book
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Title = reader.GetString(reader.GetOrdinal("Title")),
                                    IsBorrowed = reader.GetBoolean(reader.GetOrdinal("IsBorrowed"))
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching borrowed books: {ex.Message}");
            }

            // Pass the list to the View
            return View(borrowedBooks);
        }
        public IActionResult BorrowBook(int bookId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connection))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand("SELECT toggle_is_borrowed(@BookId);", connection))
                    {
                        command.CommandType = CommandType.Text;
                        command.Parameters.AddWithValue("@BookId", bookId);

                        var result = command.ExecuteScalar(); // Executes the function

                        if (result != null && Convert.ToBoolean(result))
                        {
                            Console.WriteLine($"Book with ID {bookId} borrow status toggled successfully.");
                           

                        }
                        else
                        {
                            Console.WriteLine($"Failed to toggle borrow status for Book ID {bookId}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return RedirectToAction("BorrowedBooks");
        }

    }


}

