using Ibay.Models;
using Ibay.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace iBayApi
{
    public  class RoleController
    {
        private  IServiceProvider _serviceProvider;
        private IBasicRepository<UserManager<User>>? _basicRepository;
        

        public RoleController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
           
        }

        public  async Task CreateRole( string role)
        {
            var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            //var userManager = _serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            IdentityResult roleResult;

            var roleExist = await roleManager.RoleExistsAsync(role);

            if (!roleExist)
            {
                roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            }

        }



        public async Task<IdentityResult> AddUserToRole( string email, string role)
        {
            var userManager = _serviceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "UserNotFound",
                    Description = $"User with email '{email}' was not found."
                });
            }

            var result = await userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                IdentityResult roleResult;

                var roleExist = await roleManager.RoleExistsAsync(role);

                if (!roleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(role));

                    if (roleResult.Succeeded)
                    {
                        var res = await userManager.AddToRoleAsync(user, role);

                        if (res.Succeeded)
                        {
                            return res;
                        }
                    }
                }


            }
            return result;

        }


        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            //var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roleNames = { "Admin", "Seller" ,"User"};
            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

    }
}
