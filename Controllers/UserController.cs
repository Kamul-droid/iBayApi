using Ibay.Models;
using Ibay.Repositories;
using Microsoft.AspNetCore.Mvc;
using NuGet.DependencyResolver;
using NuGet.Protocol;
using System.Text.Json.Nodes;
using CheckPasswordStrength;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using Azure;
using System.Security;
using Microsoft.AspNetCore.Authorization;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Authentication;
using System.Data;
using Microsoft.EntityFrameworkCore;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace iBayApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        
        private readonly UserManager<User> _userManagRepository;

        private readonly IServiceProvider _serviceProvider;
        private readonly IBasicRepository<Cart> _cartRepository;
       
        //private readonly ILogger _logger;
        //private readonly IConfiguration _configuration;
        private readonly RoleController _roleController;
        public UserController(IServiceProvider serviceProvider, UserManager<User> userManager, IBasicRepository<Cart> cartRepository) 
        {
            //_userRepository= userRepository;
            _serviceProvider= serviceProvider;
            _roleController= new RoleController(serviceProvider);
            _userManagRepository = userManager; 
            _cartRepository= cartRepository;

            

        }


        // GET: api/<UserController>
        [Authorize(Roles ="Admin")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            
            //@sT1ring

           
            return Ok( _userManagRepository.Users);
          
           
        }


        // GET api/<UserController>/5
        [Authorize(Roles = "User")]
        [HttpGet("{id}")]
        public async Task <IActionResult> Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Id not provided");

            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);


            var user = await _userManagRepository.FindByIdAsync(id);
            if (user == null) return NotFound(" User not found");
            var myUser = _userManagRepository.Users.Include(u => u.Products).Where(u => u.Id == user.Id).Include(u => u.Cart).OrderBy(u => user.UserName);

            var currentUser = await _userManagRepository.FindByEmailAsync(emailClaim.Value);
            if (currentUser == null) return BadRequest("error for identity");
            if (currentUser.Id != user.Id) return Unauthorized("You can only have access to your personnal data");
            
            return Ok(myUser);
        }

        // POST api/<UserController>/register
        [HttpPost("register")]
        public async Task<IActionResult> Post([FromBody] UserForm user)
        {
            if (user == null)
            {
                return BadRequest("No body");
            }


            try
            {
                var passwordStrenghLevel = user.Password.PasswordStrength().Id;

                if (passwordStrenghLevel == 0)
                {
                    return BadRequest("Your password is too weak !");
                }

                string regex = @"^[^@\s]+@[^@\s]+\.(com|net|org|gov)$";

                 var isEmailValid = Regex.IsMatch(user.Email,
                                     regex,
                                     RegexOptions.IgnoreCase);
                if (!isEmailValid)
                {
                    return BadRequest("Your email is not valid !");
                }

                // Create user with Entity framework identity usermanager

                var myUser = new User
                {
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = user.UserName,
                    Email = user.Email,
                    PasswordHash = user.Password,
                    Products = new List<Product> { }
                };

                //User cart 
                Cart cart = new Cart
                {
                    Products = new List<Product>(),
                    User = myUser,
                };

                _cartRepository.Create(cart);

                myUser.Cart = cart;

                var result = await _userManagRepository.CreateAsync(myUser, myUser.PasswordHash);

                if (result.Succeeded)
                {
                    // Add role user 
                    string role = "User";
                    await RoleController.InitializeAsync(_serviceProvider);

                    try
                    {
                        var roleResult = await _roleController.AddUserToRole(myUser.Email, role);
                        if (roleResult.Succeeded)
                        {
                            return Created($" New User / {myUser.Id}", myUser );

                        }


                    }
                    catch (Exception)
                    {

                       
                        return BadRequest("Can 't add the role, Contact your administrator to join you with User privilege");


                    }


                }
                else
                {
                    return BadRequest(result.ToString());

                }

            }
            catch (Exception)
            {

                return BadRequest("Something went wrong when adding user with the server, please try again");
            }

            
                return BadRequest("Something went wrong with the server, please try again");

        }
        
        // POST api/<UserController>/register-admin
        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] UserForm user)
        {
            if (user == null)
            {
                return BadRequest("No body");
            }
           

            try
            {
                var passwordStrenghLevel = user.Password.PasswordStrength().Id;

                if (passwordStrenghLevel == 0)
                {
                    return BadRequest("Your password is too weak !");
                }

                string regex = @"^[^@\s]+@[^@\s]+\.(com|net|org|gov)$";

                 var isEmailValid = Regex.IsMatch(user.Email,
                                     regex,
                                     RegexOptions.IgnoreCase);
                if (!isEmailValid)
                {
                    return BadRequest("Your email is not valid !");
                }

                // Create user with Entity framework identity usermanager

                var myUser = new User
                {
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = user.UserName,
                    Email = user.Email,
                    PasswordHash = user.Password,
                    Products = new List<Product> { }
                };

                var result = await _userManagRepository.CreateAsync(myUser, myUser.PasswordHash);

                if (!result.Succeeded)
                    return BadRequest(result.ToString());

                // Add role user 
                await RoleController.InitializeAsync(_serviceProvider);
                string role = "Admin";
                var roleResult = await _roleController.AddUserToRole(myUser.Email, role);

                if (roleResult.Succeeded)
                {
                    return Created($" New {myUser}", "admin is added");

                }

            }
            catch (Exception)
            {

                return BadRequest("User is created but erreur occurs when adding role");
            }

            return Created($" Something went wrong with the addind of this {user}","check the data");
           

        }
        
        // POST api/<UserController/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManagRepository.FindByEmailAsync(email);
            
            // Authentification
            if (user != null || await _userManagRepository.CheckPasswordAsync(user,password))
            {
                
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    
                };
                var userRoles = await _userManagRepository.GetRolesAsync(user);

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var claimsIdentity = new ClaimsIdentity(authClaims, "MyAuthType");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                 HttpContext.SignInAsync("MyAuthScheme", claimsPrincipal);


                //Authorization
                var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationManager.AppSetting["JWT:Secret"]));
                var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
                var tokeOptions = new JwtSecurityToken(issuer: ConfigurationManager.AppSetting["JWT:ValidIssuer"], audience: ConfigurationManager.AppSetting["JWT:ValidAudience"], claims: authClaims, expires: DateTime.Now.AddMinutes(120), signingCredentials: signinCredentials);
                var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);
                return Ok(new JWTTokenResponse
                {
                    Token = tokenString
                }) ;
            }
            return Unauthorized();


          

        }

        // POST api/<UserController/addtoroleseller
        [Authorize(Roles = "User")]
        [HttpPost("add-to-role-seller")]
        public async Task<IActionResult> AddRole(string email, string role)
        {
            if (string.IsNullOrWhiteSpace(email)) { throw new ArgumentNullException("email"); };

            if (string.IsNullOrWhiteSpace(role)) { return BadRequest("Role cant be null"); };

            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);

            if (emailClaim.Value != email) { 
            
                return Unauthorized("Cant add role to another one account") ;
            
            }
            var user = await _userManagRepository.FindByEmailAsync(email);
            var userRoles = await _userManagRepository.GetRolesAsync(user);

            if(userRoles.Contains("Seller") && role == "Seller") {
                return BadRequest("Role already exist");
            }

            if(!userRoles.Contains("Seller") && role == "Seller") { 

                 var result = await _roleController.AddUserToRole(email, role);
                
                if (result.Succeeded)
                {
                    // update authorization roles
                    var authClaims = new List<Claim>
                                    {
                                        new Claim(ClaimTypes.Email, user.Email),

                                    };
                    var userRol = await _userManagRepository.GetRolesAsync(user);

                    foreach (var userRole in userRol)
                            {
                                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                            }

                    var claimsId = new ClaimsIdentity(authClaims, "MyAuthType");
                    var claimsPrincipal = new ClaimsPrincipal(claimsId);

                     HttpContext.SignInAsync("MyAuthScheme", claimsPrincipal);

                    return Ok( $"Role {role} added to user!");
                }
                
                return BadRequest(result.Errors);
            }


            return BadRequest($"Dear user : {user.Email} you can only add role Seller to your account");

        }
        
        // POST api/<UserController/addtoroleadmin
        [Authorize(Roles = "Admin")]
        [HttpPost("add-to-role")]
        public async Task<IActionResult> AddRoleAdmin(string email, string role)
        {
            if (string.IsNullOrWhiteSpace(email)) { throw new ArgumentNullException("email"); };

            if (string.IsNullOrWhiteSpace(role)) { return BadRequest("Role can't be null"); };

           
            var user = await _userManagRepository.FindByEmailAsync(email);

            if (user != null)
            {
                var userRoles = await _userManagRepository.GetRolesAsync(user);

                if (userRoles.Contains($"{role}"))
                {
                    return BadRequest("Role already exist");
                }

                if (!userRoles.Contains($"{role}"))
                {

                    var result = await _roleController.AddUserToRole(email, role);

                    if (result.Succeeded)
                    {
                        return Ok($"Role {role} added to user!");
                    }

                    return BadRequest(result.Errors);
                }


            }
            return NotFound("No user with this email address exist");

            


        }


        // POST api/<UserController/createrole
        [Authorize(Roles = "Admin")]
        [HttpPost("createrole")]
        public async Task<IActionResult> CreateRoles(string role)
        {
            await _roleController.CreateRole( role);
            return Ok($"Role {role} created !");
        }


        // PUT api/<UserController>/5
        [Authorize(Roles = "User")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] UserUpdateForm userQ)
        {
            if (userQ == null || id == default || id == null) return BadRequest("Erreur de mise à jour");

            var user = await _userManagRepository.FindByIdAsync(id);
            if (user == null) return BadRequest("No User with this Id exist");

           
            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);
                       

            var currentUser = await _userManagRepository.FindByEmailAsync(emailClaim.Value);
            if (currentUser.Id != user.Id) return Unauthorized("You can only update your personnal data");

           
            user.UserName = userQ.UserName;
            user.PhoneNumber = userQ.PhoneNumber;

            var result = await _userManagRepository.UpdateAsync(user);
            //var resultRole = await _roleController .UpdateAsync(user);
            if (result.Succeeded)
            {
                
                return Ok("Mise à jour réussi");
            }

            return BadRequest(result.Errors);
            
        }

        // DELETE api/<UserController>/5
        [Authorize(Roles = "User")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == default || id == null) return BadRequest("Id not provided");

            var user = await _userManagRepository.FindByIdAsync(id); 
            
            if (user == null) return BadRequest("User not find");

            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);
            var currentUser = await _userManagRepository.FindByEmailAsync(emailClaim.Value);
            if (currentUser.Id != user.Id) return Unauthorized("You can only delete your personnal account");

            var result =  await _userManagRepository.DeleteAsync(user);
            
            return Ok("User deleted successfully");
        }



       



    }
}
