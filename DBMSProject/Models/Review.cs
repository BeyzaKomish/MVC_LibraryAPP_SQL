using System;
using System.Collections.Generic;

namespace DBMSProject.Models;

public partial class Review
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? StudentId { get; set; }

    public int? BookId { get; set; }

    public virtual Book? Book { get; set; }

    public virtual Student? Student { get; set; }
}
