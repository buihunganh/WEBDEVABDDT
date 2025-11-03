using BTL_WEBDEV2025.Models;
using Microsoft.AspNetCore.Mvc;
using BTL_WEBDEV2025.Data;

namespace BTL_WEBDEV2025.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        // Trang đăng nhập (bước 1: nhập email)
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // Xử lý đăng nhập và luồng đăng ký nhanh trong cùng form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            // Bước 1: chỉ có email => quyết định hiển thị ô mật khẩu hay chuyển sang đăng ký
            if (!model.ShowPassword && !model.IsNewUser)
            {
                if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
                {
                    if (!ModelState.IsValid)
                    {
                        return View(model);
                    }
                }
                var existingUser = _db.Users.FirstOrDefault(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    // Tìm thấy email: yêu cầu nhập mật khẩu
                    ModelState.Clear();
                    model.ShowPassword = true;
                    model.IsNewUser = false;
                    return View(model);
                }
                else
                {
                    // Không có email: chuyển sang chế độ đăng ký nhanh
                    ModelState.Clear();
                    model.IsNewUser = true;
                    model.ShowPassword = true;
                    return View(model);
                }
            }

            // Bước 2: đang ở bước mật khẩu => bỏ qua validate email
            if (model.ShowPassword && !model.IsNewUser)
            {
                ModelState.Remove(nameof(LoginViewModel.Email));
                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    return RedirectToAction("Login");
                }
            }
            else if (!ModelState.IsValid && !model.IsNewUser)
            {
                return View(model);
            }
            // Đăng nhập theo email + mật khẩu
            var user = _db.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user != null)
            {
                if (string.IsNullOrWhiteSpace(model.Password))
                {
                    ModelState.Clear();
                    model.ShowPassword = true;
                    model.IsNewUser = false;
                    return View(model);
                }
                if (user.PasswordHash == model.Password)
                {
                    // Thiết lập session và điều hướng theo vai trò
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    if (!string.IsNullOrWhiteSpace(user.FullName))
                    {
                        HttpContext.Session.SetString("UserName", user.FullName);
                    }
                    if (user.RoleId == 1)
                    {
                        return RedirectToAction("Index", "Admin");
                    }
                    return RedirectToAction("Index", "User");
                }

                ModelState.Clear();
                ModelState.AddModelError("Password", "Password incorrect");
                model.ShowPassword = true;
                model.IsNewUser = false;
                return View(model);
            }

            // Không tìm thấy email: kích hoạt luồng đăng ký inline
            if (!model.IsNewUser)
            {
                ModelState.Clear();
                model.IsNewUser = true;
                model.ShowPassword = true;
                return View(model);
            }

            // Validate dữ liệu đăng ký inline
            ModelState.Remove(nameof(LoginViewModel.Email));
            if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
            {
                ModelState.AddModelError("Email", "Valid email is required");
            }
            
            if (string.IsNullOrWhiteSpace(model.FirstName) || model.FirstName.Length < 2 || model.FirstName.Length > 50)
                ModelState.AddModelError("FirstName", "First name must be between 2 and 50 characters");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.FirstName, @"^[a-zA-Z\s'-]+$"))
                ModelState.AddModelError("FirstName", "First name can only contain letters, spaces, hyphens, and apostrophes");
            
            if (string.IsNullOrWhiteSpace(model.LastName) || model.LastName.Length < 2 || model.LastName.Length > 50)
                ModelState.AddModelError("LastName", "Last name must be between 2 and 50 characters");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.LastName, @"^[a-zA-Z\s'-]+$"))
                ModelState.AddModelError("LastName", "Last name can only contain letters, spaces, hyphens, and apostrophes");
            
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError("Password", "Password is required");
            else if (model.Password.Length < 6 || model.Password.Length > 50)
                ModelState.AddModelError("Password", "Password must be between 6 and 50 characters");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$"))
                ModelState.AddModelError("Password", "Password must contain at least one uppercase letter, one lowercase letter, and one number");
            
            if (string.IsNullOrWhiteSpace(model.Preference))
                ModelState.AddModelError("Preference", "Shopping preference is required");
            
            if (!model.AcceptPolicy)
                ModelState.AddModelError("AcceptPolicy", "You must agree to continue");

            // Tuổi phải > 13; nếu điền một phần DOB thì yêu cầu đủ DD/MM/YYYY
            if (model.BirthDay.HasValue || model.BirthMonth.HasValue || model.BirthYear.HasValue)
            {
                if (!(model.BirthDay.HasValue && model.BirthMonth.HasValue && model.BirthYear.HasValue))
                {
                    ModelState.AddModelError("BirthDay", "Please complete date of birth (DD/MM/YYYY)");
                }
                else
                {
                    try
                    {
                        var testDob = new DateTime(model.BirthYear!.Value, model.BirthMonth!.Value, model.BirthDay!.Value);
                        var today = DateTime.Today;
                        var age = today.Year - testDob.Year;
                        if (testDob.Date > today.AddYears(-age)) age--;
                        if (age <= 13) ModelState.AddModelError("BirthDay", "You must be over 13 years old.");
                    }
                    catch
                    {
                        ModelState.AddModelError("BirthDay", "Invalid date of birth");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                model.IsNewUser = true;
                model.ShowPassword = true;
                return View(model);
            }

            // Parse DOB nếu đủ thông tin
            DateTime? dob = null;
            if (model.BirthDay.HasValue && model.BirthMonth.HasValue && model.BirthYear.HasValue)
            {
                try
                {
                    dob = new DateTime(model.BirthYear!.Value, model.BirthMonth!.Value, model.BirthDay!.Value);
                }
                catch { }
            }

            try
            {
                // Đảm bảo có RoleId=2 (Customer) để tránh lỗi FK ở DB mới
                if (!_db.Roles.Any(r => r.Id == 2))
                {
                    _db.Roles.Add(new Role { Id = 2, Name = "Customer" });
                    _db.SaveChanges();
                }
                var newUser = new User
                {
                    Email = model.Email,
                    PasswordHash = model.Password!,
                    FullName = ($"{model.FirstName} {model.LastName}").Trim(),
                    ShoppingPreference = model.Preference,
                    DateOfBirth = dob,
                    RoleId = 2
                };
                _db.Users.Add(newUser);
                _db.SaveChanges();
                var successMsg = "Account created successfully! You can now sign in.";
                TempData["AuthMessage"] = successMsg;
                return RedirectToAction("Login", new { success = 1, msg = successMsg });
            }
            catch (Exception ex)
            {
                // Thông báo lỗi lưu tài khoản (demo giữ plain password theo yêu cầu dự án)
                ModelState.AddModelError(string.Empty, "Không thể lưu tài khoản. Vui lòng kiểm tra kết nối CSDL và thử lại.");
                ModelState.AddModelError(string.Empty, ex.GetType().Name + ": " + (ex.InnerException?.Message ?? ex.Message));
                model.IsNewUser = true;
                model.ShowPassword = true;
                return View(model);
            }
        }

        // Trang Register chuyển hướng dùng chung luồng Sign In, có thể prefill email
        [HttpGet]
        public IActionResult Register(string? email)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                return View("Login", new LoginViewModel { Email = email, IsNewUser = true, ShowPassword = true });
            }
            return RedirectToAction("Login");
        }

        // Xử lý đăng ký tài khoản chuẩn (không qua luồng inline)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            // Kiểm tra tuổi > 13, yêu cầu đủ DD/MM/YYYY nếu có nhập DOB
            if (model.BirthDay.HasValue || model.BirthMonth.HasValue || model.BirthYear.HasValue)
            {
                if (!(model.BirthDay.HasValue && model.BirthMonth.HasValue && model.BirthYear.HasValue))
                {
                    ModelState.AddModelError("BirthDay", "Please complete date of birth (DD/MM/YYYY)");
                }
                else
                {
                    try
                    {
                        var dateOfBirth = new DateTime(model.BirthYear!.Value, model.BirthMonth!.Value, model.BirthDay!.Value);
                        var today = DateTime.Today;
                        var age = today.Year - dateOfBirth.Year;
                        if (dateOfBirth.Date > today.AddYears(-age)) age--;
                        if (age <= 13)
                        {
                            ModelState.AddModelError("BirthDay", "You must be over 13 years old.");
                        }
                    }
                    catch
                    {
                        ModelState.AddModelError("BirthDay", "Invalid date of birth");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Không cho trùng email
            var exists = _db.Users.Any(u => u.Email == model.Email);
            if (exists)
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            var user = new User
            {
                Email = model.Email,
                PasswordHash = model.Password,
                FullName = ($"{model.FirstName} {model.LastName}").Trim(),
                PhoneNumber = null,
                RoleId = 2
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email);
            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                HttpContext.Session.SetString("UserName", user.FullName);
            }

            TempData["AuthMessage"] = "Account created.";
            return RedirectToAction("Index", "Home");
        }

        // Đăng xuất: xóa session và quay lại đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserEmail");
            return RedirectToAction("Login");
        }
    }
}