using System;
using System.Collections.Generic;

namespace DBMSProject.Models;

public partial class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Isbn { get; set; }

    public int? PublishedYear { get; set; }

    public bool IsBorrowed { get; set; }

    public int? StudentId { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Student? Student { get; set; }

    public virtual ICollection<Author> Authors { get; set; } = new List<Author>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();
}
