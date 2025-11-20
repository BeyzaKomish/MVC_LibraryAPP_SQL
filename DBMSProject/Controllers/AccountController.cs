using DBMSProject.Data;
using DBMSProject.Models;
using Microsoft.AspNetCore.Mvc;

namespace DBMSProject.Controllers
{
    public class AccountController : Controller
    {
        private LibraryDbContext db = new LibraryDbContext();

        [HttpGet]   
        public IActionResult Login()
        {
            return View();  
        }

        [HttpPost]  
        public IActionResult Login(string username,string password)
        {
            var user = db.Users.FirstOrDefault(u => u.UserName == username && u.PasswordHash == password);

            if (user != null) 
            {


                if (user.UserType == "Student")
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserType", user.UserType);

                    return RedirectToAction("StudentDashboard", "Dashboard");
                }

                if (user.UserType == "Librarian")
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserType", user.UserType);


                    return RedirectToAction("LibrarianDashboard", "Dashboard");
                }



                return RedirectToAction("Index", "Home"); }

            ViewBag.ErrorMessage = "Invalid Username or Password";
            return View();

        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string password,string email)
        {


            try
            {
                var user = new User()
                {
                    UserName = username,
                    PasswordHash = password,
                    Email = email ,
                    UserType = "Student"
                    

                };

                db.Users.Add(user);
                db.SaveChanges();




                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex) 
            {
                ViewBag.ErrorMessage = $"An error occurred while saving the user: {ex.Message}";

                // Optionally, log the stack trace or inner exception details for further troubleshooting
                if (ex.InnerException != null)
                {
                    Console.WriteLine($" Inner Exception: {ex.InnerException.Message}");
                }

                // Return the registration view with the error message
                return View();
            }

        }


        public IActionResult Logout()
        {
            return RedirectToAction("Login","Account");
        }







   



         
         



    }
}
