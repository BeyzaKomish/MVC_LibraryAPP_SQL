using System;
using System.Collections.Generic;

namespace DBMSProject.Models;

public partial class Librarian
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = null!;

    public string Department { get; set; } = null!;

    public virtual User IdNavigation { get; set; } = null!;
}
