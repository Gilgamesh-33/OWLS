using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OWLS.Models;

namespace OWLS.Data
{
    public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            var fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                identity.AddClaim(new Claim("FullName", fullName));
            }
            if (!string.IsNullOrWhiteSpace(user.IdNumber))
            {
                identity.AddClaim(new Claim("IdNumber", user.IdNumber));
            }
            return identity;
        }
    }
}