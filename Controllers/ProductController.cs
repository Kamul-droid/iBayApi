using Ibay.Models;
using Ibay.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace iBayApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {

        private readonly IBasicRepository<Product> _productRepository;
        private readonly UserManager<User> _userManagRepository;

        public ProductController(IBasicRepository<Product> productRepository, UserManager<User> userManager)
        {
            _productRepository = productRepository;
            _userManagRepository = userManager;
        }


        // GET: api/<ProductController>/
        [HttpGet]
        public IActionResult Get( int? count)
        {
            if (count == null)
            {
                return Ok(_productRepository.GetAll().Take(10));

            }
                return Ok(_productRepository.GetAll().Take((int)count));
            
        }
        
        // GET: api/<ProductController>/date
        [HttpGet("sort-by-date")]
        public IActionResult GetByDate( int? count, bool? ascending =false)
        {
            if (count == null)
            {
                if(ascending == false) return Ok(_productRepository.GetAll().OrderByDescending(p =>p.added_time).Take(10));
                if(ascending == true) return Ok(_productRepository.GetAll().OrderBy(p =>p.added_time).Take(10));

            }
             if(ascending == true) return Ok(_productRepository.GetAll().OrderBy(p =>p.added_time).Take((int)count));
            return Ok(_productRepository.GetAll().OrderByDescending(p =>p.added_time).Take((int)count));
            
        }
         // GET: api/<ProductController>/type
        [HttpGet("sort-by-categ")]
        public IActionResult GetCateg( int? count , bool? ascending = false)
        {
            if (count == null)
            {
                if(ascending == false) return Ok(_productRepository.GetAll().OrderByDescending(p =>p.CategoryName).Take(10));
                if(ascending == true) return Ok(_productRepository.GetAll().OrderBy(p =>p.CategoryName).Take(10));

            }

            if (ascending == true) return Ok(_productRepository.GetAll().OrderBy(p =>p.CategoryName).Take((int)count));
            return Ok(_productRepository.GetAll().OrderByDescending(p =>p.CategoryName).Take((int)count));
            
        }

         // GET: api/<ProductController>/name
        [HttpGet("sort-by-name")]
        public IActionResult GetName( int? count, bool? ascending = false)
        {
            if (count == null)
            {
                if (ascending == true) return Ok(_productRepository.GetAll().OrderBy(p =>p.Name).Take(10));
                if (ascending == false) return Ok(_productRepository.GetAll().OrderByDescending(p =>p.Name).Take(10));

            }

            if (ascending == true) return Ok(_productRepository.GetAll().OrderBy(p =>p.Name).Take((int)count));
            return Ok(_productRepository.GetAll().OrderByDescending(p =>p.Name).Take((int)count));
            
        }

          // GET: api/<ProductController>/price
        [HttpGet("sort-by-price")]
        public IActionResult GetPrice( int? count, bool? ascending = false)
        {
            if (count == null)
            {
                if (ascending == true) return Ok(_productRepository.GetAll().OrderBy(p =>p.Price).Take(10));
                if (ascending == false) return Ok(_productRepository.GetAll().OrderByDescending(p =>p.Price).Take(10));

            }
            if (ascending == true) return Ok(_productRepository.GetAll().OrderBy(p =>p.Price).Take((int)count));
            return Ok(_productRepository.GetAll().OrderByDescending(p =>p.Price).Take((int)count));
            
        }

        // GET: api/<ProductController>/search
        [HttpGet("search")]
        public IActionResult Search(DateTime? date, string? type, string? name, decimal? price)
        {
         
            if ( name != null && price != null)
            {
                return Ok(_productRepository.GetAll().Where(p => p.Name == name && p.Price == price  || p.CategoryName == type  || p.added_time ==date));

            }

            if (date != null && type != null && name != null && price != null) { 
            
            return Ok(_productRepository.GetAll().Where(p => p.added_time == date && p.CategoryName == type && p.Name == name && p.Price ==price));
            
            }

            return Ok(_productRepository.GetAll().Where(p => p.added_time == date  || p.CategoryName == type || p.Name == name || p.Price ==price));

           
            
        }




        // GET api/<ProductController>/5
        [HttpGet("{id}")]
        public IActionResult Get(Guid id)
        {
            var product = _productRepository.Get(id);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            return Ok(product);
        }

        // POST api/<ProductController>
        [Authorize(Roles = "Seller")]
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] ProductForm product)
        {
            if (product.Image != null && product.Image.Length > 0)
            {
                
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "upload", "images", $"{product.Image.FileName}");
                
                using (var stream = new FileStream(@filePath, FileMode.Create))
                {
                    await product.Image.CopyToAsync(stream);
                }
                var myProduct = new Product();
                myProduct.Name = product.Name;
                myProduct.Price = product.Price;
                myProduct.added_time = DateTime.Now;
                myProduct.CategoryName = product.CategoryName;
                myProduct.Description = product.Description;
                myProduct.ImagePath = filePath;
                myProduct.IsAvailable= product.IsAvailable;

                var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
                var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                 return BadRequest("You are not connected");

                }

                var currentUser = await _userManagRepository.FindByEmailAsync(emailClaim.Value);

                if (currentUser == null)
                {
                    return NotFound("No user found for the relations mapping");
                }

                myProduct.User = currentUser;
                if (currentUser.Products == null) {
                    currentUser.Products = new List<Product> { };
                }
                currentUser.Products.Add(myProduct);
               var res= await _userManagRepository.UpdateAsync(currentUser);
             



                

                return Ok($"{myProduct.Name} is added,");

            }
            return BadRequest("Image is required");
        }

        // PUT api/<ProductController>/5
        [Authorize(Roles = "Seller")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromForm] ProductForm product)
        {
            // check if the prod is created by connected user
            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);

            if (emailClaim.Value == null)
            {

                return Unauthorized("You are not connected to your account");

            }
            var user = await _userManagRepository.FindByEmailAsync(emailClaim.Value);

            if (user == null)
            {
                return Unauthorized("You dont have an account");
            }

            //#### get userId from product
            //check if this userid is the same as current user and approve
            var prod = _productRepository.Get(id);

            if(prod == null) { return NotFound("No product with this Id exist"); }
            
            if (prod.UserId == user.Id) {

                prod.Name = product.Name;
                prod.Description = product.Description;
                prod.Price = product.Price;
                prod.IsAvailable = product.IsAvailable;
                prod.CategoryName = product.CategoryName;

                if (product.Image != null && product.Image.Length > 0)
                {

                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "upload", "images", $"{product.Image.FileName}");

                    using (var stream = new FileStream(@filePath, FileMode.Create))
                    {
                        await product.Image.CopyToAsync(stream);
                    }

                    prod.ImagePath = filePath;
                }
                _productRepository.Update(prod);
                _productRepository.SaveChange();
                return Ok(product);
            }
            
            return Unauthorized("You can only update a product you have added !");

        }

        // DELETE api/<ProductController>/5
        [Authorize(Roles = "Seller")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);

            if (emailClaim.Value == null)
            {

                return Unauthorized("You are not connected to your account");

            }
            var user = await _userManagRepository.FindByEmailAsync(emailClaim.Value);

            if (user == null)
            {
                return Unauthorized("You dont have an account");
            }

            // check if the prod is created by connected user
            var prd = _productRepository.Get(id);
            if (prd == null) { return NotFound("Product doesn't exist"); }
            if (user.Id == prd.UserId)
            {
                _productRepository.Delete(prd.ProductId);
                _productRepository.SaveChange();
                return Ok("Product deleted successfully");

            }
            return Unauthorized("You can only deleted a product you have added !");


        }
    }
}
