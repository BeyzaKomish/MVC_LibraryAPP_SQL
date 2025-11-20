CREATE TABLE "User" (
    "Id" SERIAL,
    "UserName" VARCHAR(100) NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "Email" VARCHAR(150) NOT NULL UNIQUE,
    "UserType" VARCHAR(50) NOT NULL CHECK ("UserType" IN ('Student', 'Librarian')),
    CONSTRAINT "UserPK" PRIMARY KEY ("Id")

);
CREATE TABLE "Student" (
    "Id" INT,
    "StudentID" VARCHAR(50) NOT NULL UNIQUE,
    "GPA" NUMERIC(3, 2) NOT NULL
    CONSTRAINT "StudentPK" PRIMARY KEY ("Id"),
    CONSTRAINT "StudentUser" FOREIGN KEY ("Id") REFERENCES "User" ("Id")
	ON DELETE CASCADE ON UPDATE CASCADE
);
CREATE TABLE "Librarian" (
    "Id" INT,
    "EmployeeID" VARCHAR(50) NOT NULL UNIQUE,
    "Department" VARCHAR(100) NOT NULL
    CONSTRAINT "LibrarianPK" PRIMARY KEY ("Id"),
    CONSTRAINT "LibrarianUser" FOREIGN KEY ("Id") REFERENCES "User" ("Id")
	ON DELETE CASCADE ON UPDATE CASCADE
    
);

CREATE TABLE "Books" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "ISBN" VARCHAR(20) UNIQUE,
    "PublishedYear" INT,
    "IsBorrowed" BOOLEAN NOT NULL DEFAULT FALSE
);
CREATE TABLE Authors (
    "Id" SERIAL,
    "Name" VARCHAR(50),
    CONSTRAINT "AuthorsPK" PRIMARY KEY ("Id")
);
CREATE TABLE Genres (
    "Id" SERIAL,
    "Name" VARCHAR(50),
    CONSTRAINT "GenresPK" PRIMARY KEY ("Id")
);


CREATE TABLE Categories (
    "Id" SERIAL,
    "Name" VARCHAR(50),
    CONSTRAINT "CategoriesPK" PRIMARY KEY ("Id")
);

CREATE TABLE BookGenre (
    "GenreId" INT,
    "BookId" INT,
    CONSTRAINT "BookGenrePK" PRIMARY KEY ("GenreId", "BookId"),
    CONSTRAINT "BookGenre_GenreFK" FOREIGN KEY ("GenreId") REFERENCES "Genres"("Id") ON DELETE CASCADE,
    CONSTRAINT "BookGenre_BookFK" FOREIGN KEY ("BookId") REFERENCES "Books"("Id") ON DELETE CASCADE
);


CREATE TABLE BookAuthor (
    "AuthorId" INT,
    "BookId" INT,
    CONSTRAINT "BookAuthorPK" PRIMARY KEY ("AuthorId", "BookId"),
    CONSTRAINT "BookAuthor_AuthorFK" FOREIGN KEY ("AuthorId") REFERENCES "Authors"("Id") ON DELETE CASCADE,
    CONSTRAINT "BookAuthor_BookFK" FOREIGN KEY ("BookId") REFERENCES "Books"("Id") ON DELETE CASCADE
);



CREATE TABLE BookCategory (
    "CategoryId" INT,
    "BookId" INT,
    CONSTRAINT "BookCategoryPK" PRIMARY KEY ("CategoryId", "BookId"),
    CONSTRAINT "BookCategory_CategoryFK" FOREIGN KEY ("CategoryId") REFERENCES "Categories"("Id") ON DELETE CASCADE,
    CONSTRAINT "BookCategory_BookFK" FOREIGN KEY ("BookId") REFERENCES "Books"("Id") ON DELETE CASCADE
);

CREATE TABLE Reviews (
    "Id" SERIAL,
    "Title" VARCHAR(100),
    "Description" VARCHAR(250),
    "StudentId" INT,
    "BookId" INT,
    CONSTRAINT "ReviewsPK" PRIMARY KEY ("Id"),
    CONSTRAINT "Reviews_StudentFK" FOREIGN KEY ("StudentId") REFERENCES "Student"("Id") ON DELETE CASCADE,
    CONSTRAINT "Reviews_BookFK" FOREIGN KEY ("BookId") REFERENCES "Books"("Id") ON DELETE CASCADE
);

CREATE TABLE BorrowLog (
    LogID SERIAL PRIMARY KEY,
    BookID INT NOT NULL,
    Action TEXT NOT NULL,
    ActionTimestamp TIMESTAMP NOT NULL
);

CREATE TABLE IF NOT EXISTS BookToggles (
    ToggleID SERIAL PRIMARY KEY,
    BookID INT NOT NULL,
    LastToggled TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (BookID) REFERENCES "Books" ("Id")
);


FUNCTİONS:
CREATE OR REPLACE FUNCTION public.create_book(title text, isbn text, published_year integer, is_borrowed boolean DEFAULT false, genre_ids integer[] DEFAULT '{}'::integer[], author_ids integer[] DEFAULT '{}'::integer[], category_ids integer[] DEFAULT '{}'::integer[])
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE 
    book_id integer;
    genre_id integer;
    author_id integer;
    category_id integer;
BEGIN

    INSERT INTO "Books" ("Title", "ISBN", "PublishedYear", "IsBorrowed")
    VALUES (title, isbn, published_year, is_borrowed)
    RETURNING "Id" INTO book_id;

    IF genre_ids IS NOT NULL THEN
        FOREACH genre_id IN ARRAY genre_ids
        LOOP
            INSERT INTO "BookGenre" ("BooksId", "GenresId")
            VALUES (book_id, genre_id);
        END LOOP;
    END IF;

    IF author_ids IS NOT NULL THEN
        FOREACH author_id IN ARRAY author_ids
        LOOP
            INSERT INTO "BookAuthor" ("BooksId", "AuthorsId")
            VALUES (book_id, author_id);
        END LOOP;
    END IF;

    IF category_ids IS NOT NULL THEN
        FOREACH category_id IN ARRAY category_ids
        LOOP
            INSERT INTO "BookCategory" ("BooksId", "CategoriesId")
            VALUES (book_id, category_id);
        END LOOP;
    END IF;
END;
$function$

CREATE OR REPLACE FUNCTION public.delete_book(book_id integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Delete associated records from the many-to-many tables first (optional)
    DELETE FROM "BookAuthor" WHERE "BooksId" = book_id;
    DELETE FROM "BookGenre" WHERE "BooksId" = book_id;
    DELETE FROM "BookCategory" WHERE "BooksId" = book_id;

    -- Finally, delete the book itself
    DELETE FROM "Books" WHERE "Id" = book_id;
END;
$function$

CREATE OR REPLACE FUNCTION public.log_book_deletion()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Insert the deleted book’s data into the log table
    INSERT INTO deleted_books_log (book_id, title, isbn, published_year, is_borrowed)
    VALUES (OLD."Id", OLD."Title", OLD."ISBN", OLD."PublishedYear", OLD."IsBorrowed");
    
    -- Return the deleted record
    RETURN OLD;
END;
$function$

CREATE OR REPLACE FUNCTION public.log_book_update()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Insert a record into the BookUpdateLog table with before and after values
    INSERT INTO public."BookUpdateLog" (
        "BookId", "OldTitle", "OldISBN", "OldPublishedYear", "OldIsBorrowed",
        "NewTitle", "NewISBN", "NewPublishedYear", "NewIsBorrowed"
    )
    VALUES (
        OLD."Id",               -- Old Book ID
        OLD."Title",            -- Old Title
        OLD."ISBN",             -- Old ISBN
        OLD."PublishedYear",    -- Old PublishedYear
        OLD."IsBorrowed",       -- Old IsBorrowed
        NEW."Title",            -- New Title
        NEW."ISBN",             -- New ISBN
        NEW."PublishedYear",    -- New PublishedYear
        NEW."IsBorrowed"        -- New IsBorrowed
    );

    RETURN NEW;  -- Return the new row after the update
END;
$function$


CREATE OR REPLACE FUNCTION public.update_book(book_id integer, new_title text, new_isbn text, new_published_year integer, new_is_borrowed boolean DEFAULT false, new_genre_ids integer[] DEFAULT '{}'::integer[], new_author_ids integer[] DEFAULT '{}'::integer[], new_category_ids integer[] DEFAULT '{}'::integer[])
 RETURNS void
 LANGUAGE plpgsql
AS $function$
DECLARE
    genre_id integer;
    author_id integer;
    category_id integer;
BEGIN
    -- Validate Title (ensure it's not empty or NULL)
    IF new_title IS NULL OR new_title = '' THEN
        RAISE EXCEPTION 'Book title cannot be empty or null';
    END IF;

    -- Validate ISBN format (ISBN-10 or ISBN-13)
    IF NOT new_isbn ~ '^(\d{13}|\d{10})$' THEN
        RAISE EXCEPTION 'Invalid ISBN. Must be either a 10-digit or 13-digit number';
    END IF;

    -- Update the Book table with the new values
    UPDATE "Books"
    SET 
        "Title" = new_title,
        "ISBN" = new_isbn,
        "PublishedYear" = new_published_year,
        "IsBorrowed" = new_is_borrowed
    WHERE "Id" = book_id;

    -- If there are new genres, update the BookGenre table
    IF array_length(new_genre_ids, 1) > 0 THEN
        DELETE FROM "BookGenre" WHERE "BooksId" = book_id;
        FOREACH genre_id IN ARRAY new_genre_ids
        LOOP
            INSERT INTO "BookGenre" ("BooksId", "GenresId")
            VALUES (book_id, genre_id);
        END LOOP;
    END IF;

    -- If there are new authors, update the BookAuthor table
    IF array_length(new_author_ids, 1) > 0 THEN
        DELETE FROM "BookAuthor" WHERE "BooksId" = book_id;
        FOREACH author_id IN ARRAY new_author_ids
        LOOP
            INSERT INTO "BookAuthor" ("BooksId", "AuthorsId")
            VALUES (book_id, author_id);
        END LOOP;
    END IF;

    -- If there are new categories, update the BookCategory table
    IF array_length(new_category_ids, 1) > 0 THEN
        DELETE FROM "BookCategory" WHERE "BooksId" = book_id;
        FOREACH category_id IN ARRAY new_category_ids
        LOOP
            INSERT INTO "BookCategory" ("BooksId", "CategoriesId")
            VALUES (book_id, category_id);
        END LOOP;
    END IF;
END;
$function$

CREATE OR REPLACE FUNCTION public.validate_book_insert()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN

    IF NEW."Title" IS NULL OR NEW."Title" = '' THEN
        RAISE EXCEPTION 'Book title cannot be empty or null';
    END IF;

    -- Validate the ISBN format (either ISBN-10 or ISBN-13)
    IF NOT NEW."ISBN" ~ '^(\d{13}|\d{10})$' THEN
        RAISE EXCEPTION 'Invalid ISBN. Must be either a 10-digit or 13-digit number';
    END IF;

    -- Return the NEW row if all validations pass
    RETURN NEW;
END;
$function$
CREATE OR REPLACE FUNCTION public.log_borrow_action()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Ensure the column name is correct and matches the table schema
    IF NEW."IsBorrowed" THEN
        INSERT INTO BorrowLog (BookId, Action, ActionTimestamp)
        VALUES (NEW."Id", 'Borrowed', NOW());
    ELSE
        INSERT INTO BorrowLog (BookId, Action, ActionTimestamp)
        VALUES (NEW."Id", 'Returned', NOW());
    END IF;

    RETURN NEW;  -- Return the updated row
END;
$function$




CREATE OR REPLACE FUNCTION SearchBooks(searchTerm VARCHAR)
RETURNS TABLE (
    Id INT,
    Title VARCHAR,
    ISBN VARCHAR,
    PublishedYear INT,
    IsBorrowed BOOLEAN,
    StudentId INT,
    GenreName VARCHAR,
    AuthorName VARCHAR,
    CategoryName VARCHAR,
    StudentName VARCHAR,
    UserType VARCHAR
)
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        b."Id",
        b."Title",
        b."ISBN",
        b."PublishedYear",
        b."IsBorrowed",
        b."StudentId",
        g."Name" AS "GenreName",
        a."Name" AS "AuthorName",
        c."Name" AS "CategoryName",
        u."UserName" AS "StudentName",
        u."UserType"
    FROM "Books" b
    LEFT JOIN "BookGenre" bg ON b."Id" = bg."BooksId"
    LEFT JOIN "Genres" g ON bg."GenresId" = g."Id"
    LEFT JOIN "BookAuthor" ba ON b."Id" = ba."BooksId"
    LEFT JOIN "Authors" a ON ba."AuthorsId" = a."Id"
    LEFT JOIN "BookCategory" bc ON b."Id" = bc."BooksId"
    LEFT JOIN "Categories" c ON bc."CategoriesId" = c."Id"
    LEFT JOIN "Student" s ON b."StudentId" = s."Id"
    LEFT JOIN "User" u ON s."Id" = u."Id"
    WHERE
        b."Title" ILIKE '%' || searchTerm || '%' OR
        a."Name" ILIKE '%' || searchTerm || '%' OR
        g."Name" ILIKE '%' || searchTerm || '%' OR
        c."Name" ILIKE '%' || searchTerm || '%';
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION public.toggle_is_borrowed(book_id integer)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Toggle the IsBorrowed status in the Books table
    UPDATE "Books"
    SET "IsBorrowed" = NOT "IsBorrowed"
    WHERE "Id" = book_id;

    -- Ensure the update was successful
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Book with ID % does not exist', book_id;
    END IF;

    -- Insert or update the LastToggled timestamp in the BookToggles table
    INSERT INTO BookToggles (BookId, LastToggled)
    VALUES (book_id, NOW())
    ON CONFLICT (BookId)
    DO UPDATE SET LastToggled = NOW();
END;
$function$


TRIGGERS:
CREATE TRIGGER before_book_insert BEFORE INSERT ON public."Books" FOR EACH ROW EXECUTE FUNCTION validate_book_insert()

CREATE TRIGGER book_deletion_trigger AFTER DELETE ON public."Books" FOR EACH ROW EXECUTE FUNCTION log_book_deletion()

CREATE TRIGGER book_update_trigger BEFORE UPDATE ON public."Books" FOR EACH ROW EXECUTE FUNCTION log_book_update()

CREATE TRIGGER borrow_action_trigger AFTER UPDATE OF "IsBorrowed" ON public."Books" FOR EACH ROW EXECUTE FUNCTION log_borrow_action()



Inserting data:
INSERT INTO "Books" ("Title", "ISBN", "PublishedYear", "IsBorrowed", "StudentId")
VALUES
('The Lord of the Rings', '9780618640157', 1954, TRUE, 2),
('The Alchemist', '9780061122415', 1988, FALSE, NULL),
('Crime and Punishment', '9780140449136', 1866, TRUE, 3),
('The Brothers Karamazov', '9780374528379', 1880, FALSE, NULL),
('Anna Karenina', '9780143035008', 1877, TRUE, 1),
('The Grapes of Wrath', '9780143039433', 1939, FALSE, NULL),
('Wuthering Heights', '9780141439556', 1847, TRUE, 2),
('Jane Eyre', '9780141441146', 1847, FALSE, NULL),
('Les Misérables', '9780451419439', 1862, TRUE, 3),
('Don Quixote', '9780060934347', 1605, FALSE, NULL),
('The Divine Comedy', '9780142437223', 1320, FALSE, NULL),
('The Iliad', '9780140445923', -750, TRUE, 1),
('The Odyssey', '9780140268867', -700, FALSE, NULL),
('Meditations', '9780812968255', 180, FALSE, NULL),
('The Art of War', '9781590302255', -500, TRUE, 2),
('Dracula', '9780141439846', 1897, FALSE, NULL),
('Frankenstein', '9780141439471', 1818, TRUE, 3),
('The Scarlet Letter', '9780142437261', 1850, FALSE, NULL),
('Heart of Darkness', '9780141441672', 1899, FALSE, NULL),
('One Hundred Years of Solitude', '9780060883287', 1967, TRUE, 1);

INSERT INTO "Genres" ("Name")
VALUES
('Fiction'),
('Science'),
('Non-Fiction');

INSERT INTO "Authors" ("Name")
VALUES
('F. Scott Fitzgerald'),
('George Orwell'),
('Herman Melville');

INSERT INTO "Categories" ("Name")
VALUES
('Literature'),
('Dystopian'),
('Adventure');




INSERT INTO "BookGenre" ("GenresId", "BooksId")
VALUES
-- Matching books to genres
(6, 1),   -- Book Thief -> Fiction
(6, 2),   -- The Catcher in the Rye -> Fiction
(6, 3),   -- To Kill a Mockingbird -> Fiction
(8, 4),   -- Pride and Prejudice -> Novel
(4, 5),   -- The Hobbit -> Mythic
(6, 6),   -- Brave New World -> Fiction
(6, 7),   -- Fahrenheit 451 -> Fiction
(2, 8),   -- 1984 -> Science
(6, 9),   -- The Great Gatsby -> Fiction
(4, 10),  -- Moby Dick -> Mythic
(10, 11), -- War and Peace -> History
(4, 12),  -- The Lord of the Rings -> Mythic
(8, 13),  -- The Alchemist -> Novel
(6, 14),  -- Crime and Punishment -> Fiction
(8, 15),  -- The Brothers Karamazov -> Novel
(8, 16),  -- Anna Karenina -> Novel
(10, 17), -- The Grapes of Wrath -> History
(6, 18),  -- Wuthering Heights -> Fiction
(1, 19),  -- Jane Eyre -> Romance
(5, 20),  -- Les Misérables -> Drama
(8, 21),  -- Don Quixote -> Novel
(5, 22),  -- The Divine Comedy -> Drama
(4, 23),  -- The Iliad -> Mythic
(4, 24),  -- The Odyssey -> Mythic
(11, 25), -- Meditations -> Literature
(11, 26), -- The Art of War -> Literature
(6, 27),  -- Dracula -> Fiction
(6, 28),  -- Frankenstein -> Fiction
(6, 29),  -- The Scarlet Letter -> Fiction
(6, 30),  -- Heart of Darkness -> Fiction
(8, 31);  -- One Hundred Years of Solitude -> Novel


INSERT INTO "Reviews" ("Title", "Description", "StudentId", "BookId")
VALUES
('Amazing Read!', 'This book was truly captivating and kept me engaged from start to finish.', 1, 2),
('Classic Masterpiece', 'A must-read for anyone who enjoys timeless literature.', 3, 7),
('Inspirational Story', 'I loved the message and the character development in this book.', 2, 10),
('Too Slow for My Taste', 'While the story was interesting, it felt a bit slow-paced.', 1, 5),
('A Timeless Tale', 'The themes and characters are as relevant today as they were when it was written.', 3, 9),
('Not What I Expected', 'I had high expectations, but the book didn’t quite live up to them.', 2, 6),
('Great for Learning', 'This book offers incredible insights and is very thought-provoking.', 1, 3),
('Fascinating Characters', 'The characters felt so real, and their struggles were relatable.', 3, 8),
('Beautifully Written', 'The prose is poetic and vivid, creating an immersive reading experience.', 2, 4),
('Highly Recommend', 'This is one of the best books I’ve read this year!', 1, 1);


INSERT INTO "Reviews" ("Title", "Description", "StudentId", "BookId")
                               VALUES ('Harika', 'Çok beğendim', 5, 22)
                               ;




 