using System;
using System.Collections.Generic;

namespace DBMSProject.Models;

public partial class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public virtual Librarian? Librarian { get; set; }

    public virtual Student? Student { get; set; }
}
