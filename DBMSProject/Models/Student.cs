using System;
using System.Collections.Generic;

namespace DBMSProject.Models;

public partial class Student
{
    public int Id { get; set; }

    public string StudentId { get; set; } = null!;

    public decimal Gpa { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();

    public virtual User IdNavigation { get; set; } = null!;

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
