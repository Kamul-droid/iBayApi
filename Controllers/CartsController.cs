using Ibay.Models;
using Ibay.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace iBayApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly IBasicRepository<Product> _productRepository;
        private readonly IBasicRepository<Cart> _cartRepository;
        private readonly UserManager<User> _userManagRepository;

        public CartsController (IBasicRepository<Product> productRepository, IBasicRepository<Cart> cartRepository, UserManager<User> userManagRepository)
        {
            _productRepository = productRepository;
            _cartRepository = cartRepository;
            _userManagRepository = userManagRepository;
        }



        // GET: api/<CartsController>
        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);

            if (emailClaim == null)
            {

                return Unauthorized("You need to be connected to your account");

            }
            var user = await _userManagRepository.FindByEmailAsync(emailClaim.Value);

            if (user == null)
            {
                return Unauthorized("You are not connected");
            }

            var userWithCart = _userManagRepository.Users.Include(u => u.Cart).SingleOrDefault(u => u.Id == user.Id);
            if (userWithCart.Cart == null)
            {
                Cart newCart = new Cart
                {
                    Products = new List<Product>(),
                    User = user,
                };

                _cartRepository.Create(newCart);
                user.Cart = newCart;
               _cartRepository.SaveChange();
                return NotFound("No cart found");
            }

           
            var CartWithProduct = _cartRepository.Get(userWithCart.Cart.CartId);
            return Ok(CartWithProduct.Products.ToList());
        }

        // GET api/<CartsController>/5
        [Authorize(Roles = "User")]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
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

            var cart = _userManagRepository.Users.Include(u=> u.Cart).FirstOrDefault(u =>u.Cart.CartId == id);

            if (cart == null) {
                return NotFound("You don't have any cart");
            }

            if (user.Id == cart.Cart.UserId )
            {
                return Ok(_cartRepository.Get(id).Products);
            }
            return Unauthorized("You can't have acces to this cart");
        }

        // GET api/<CartsController>/addproduct
        [Authorize(Roles = "User")]
        [HttpGet("addproduct-to-cart-with{id}")]
        public async Task<IActionResult> AddProduct(Guid id)
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

            var product = _productRepository.Get(id);
            if (product == null)
            {
                return NotFound("this product doesn't exist");
            }

            var userCart = _userManagRepository.Users.Include(_u => _u.Cart).FirstOrDefault(u=> u.Cart.UserId == user.Id);
            
            if (userCart.Cart.Products == null)
            {

                userCart.Cart.Products = new List<Product>();
                   
                

                //_cartRepository.Create(newCart);
                userCart.Cart.Products.Add(product);
                

               


                //await _userManagRepository.UpdateAsync(userCart);
                //_cartRepository.Create(newCart);
                _cartRepository.SaveChange();




                return Ok($"{product.Name} add succefully ");



                

            }
            else
            {

                userCart.Cart.Products.Add(product);

                _cartRepository.SaveChange();
                //await _userManagRepository.UpdateAsync(user);

                return Ok($"{product.Name} add succefully again ");
            }

            return Unauthorized("You can't have acces to this cart");
        }


        // GET api/<CartsController>/delete-product-from-cart
        [Authorize(Roles = "User")]
        [HttpGet("delete-from-cart{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
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
                return Unauthorized("We dont have this current user account");
            }

            var userWithCart =  _userManagRepository.Users.Include(u => u.Cart).FirstOrDefault(u => u.Id == user.Id);
            var product = _productRepository.Get(id);

            if (product != null)
            {
                userWithCart.Cart.Products.Remove(product);

                _cartRepository.SaveChange() ;

                return Ok($"Product deleted succefully {product}");
            }
            else
            {
                return NotFound("this product doesn't exist");
            }


         

            
            return Unauthorized("You can't have acces to this cart");
        }


        // GET api/<CartsController>/get-my-cart-total
        [Authorize(Roles = "User")]
        [HttpGet("get-my-cart-total")]
        public async Task<IActionResult> TotalProduct()
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
                return Unauthorized("We dont have any user with this account");
            }

            var userWithCart = _userManagRepository.Users.Include(u => u.Cart).FirstOrDefault(u => u.Cart.UserId == user.Id);

            var cartWithProd = _cartRepository.Get(userWithCart.Cart.CartId);
            decimal total = 0;

            if (cartWithProd.Products != null) {
                foreach (var item in cartWithProd.Products)
                {
                    
                    total += item.Price;

                }
                return Ok($"You have a total of {total} Euro in your Cart");
            }


            return Unauthorized("You can't have acces to this cart");
        }

        // POST api/<CartsController>
        [Authorize(Roles = "User")]
        [HttpPost("pay-my-product")]
        public async Task<IActionResult> Post([FromBody] decimal fees)
        {
            var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
            var emailClaim = claimsIdentity.FindFirst(ClaimTypes.Email);

            if (emailClaim.Value == null)
            {

                return Unauthorized("You are not connected to your account");

            }
            var user = await _userManagRepository.FindByEmailAsync(emailClaim.Value);

            var userWithCart = _userManagRepository.Users.Include(_ => _.Cart).FirstOrDefault(u => u.Id == user.Id);

            var cartWithProduct = _cartRepository.Get(userWithCart.Cart.CartId);

            if (cartWithProduct.Products == null)
            {
                return NotFound("You don't have any product in your cart");
            }

            if (user.Id == userWithCart.Id)
            {
                decimal total = 0;

                foreach (var item in userWithCart.Products)
                {
                    total += item.Price;

                }

                if (fees != total)
                {
                return BadRequest($"Error in your credit card! You need to pay a total of {total} Euro");

                }

                 _cartRepository.Delete(user.Cart.CartId);

                _cartRepository.SaveChange();

            }
            return Ok("Thanks for your payment");
        }



        // DELETE api/<CartsController>/5
        [Authorize(Roles = "User")]
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
            var userWithCart = _userManagRepository.Users.Include(_ =>_.Cart).FirstOrDefault(u => u.Id == user.Id);
            var cartWithProd = _cartRepository.Get(userWithCart.Cart.CartId);
            if (cartWithProd.Products == null)
            {
                return NotFound("You don't have any product in your cart");
            }

            if (cartWithProd.CartId == id)
            {
                _cartRepository.Delete(id);
                _cartRepository.SaveChange();
                return Ok("Cart deleted");

            }

            return BadRequest("You can't find any cart with this Id in your account");


        }
    }
}
