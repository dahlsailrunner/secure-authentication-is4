using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Globomantics.Core.Authorization;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Globomantics.Core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDbConnection, SqlConnection>(db =>
                new SqlConnection(Configuration.GetConnectionString("GlobomanticsDb")));

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "mvccode";
                    options.AccessDeniedPath = "/AccessDenied";
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = "https://id-local.globomantics.com:44395";

                    options.ClientId = "interactive.confidential";
                    options.ClientSecret = "secret";

                    options.ResponseType = "code";
                    options.UsePkce = true;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("glob_profile");
                    options.Scope.Add("api");
                    options.Scope.Add("offline_access");

                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    options.ClaimActions.MapJsonKey("MfaEnabled", "MfaEnabled");
                    options.ClaimActions.MapJsonKey("CompanyId", "CompanyId");
                    options.ClaimActions.MapJsonKey("role", "role");

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.Name,
                        RoleClaimType = JwtClaimTypes.Role
                    };
                    options.Events = new OpenIdConnectEvents
                    {
                        OnTicketReceived = e =>
                        {
                            e.Principal = DoClaimsTransformation(e.Principal);
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddSingleton<IAuthorizationPolicyProvider, CustomPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, RightRequirementHandler>();
            //services.AddScoped<IAuthorizationHandler, MfaChallengeRequirementHandler>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("MfaRequired",
                    p =>
                    {
                        p.RequireClaim("CompanyId");
                        p.RequireClaim("MfaEnabled", "True");
                    });
            });

            services.AddRazorPages();
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseExceptionHandler("/Error");

            app.UseHsts();
            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseSerilogRequestLogging(opts =>
            {
                opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
                {
                    diagCtx.Set("ClientIP", httpCtx.Connection.RemoteIpAddress);
                    diagCtx.Set("UserAgent", httpCtx.Request.Headers["User-Agent"]);
                    if (httpCtx.User.Identity.IsAuthenticated)
                    {
                        diagCtx.Set("UserName", httpCtx.User.Identity?.Name);
                    }
                };
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages().RequireAuthorization();
            });
        }

        private ClaimsPrincipal DoClaimsTransformation(ClaimsPrincipal argPrincipal)
        {
            var claims = argPrincipal.Claims.ToList();
            claims.Add(new Claim("somenewclaim", "something"));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, argPrincipal.Identity.AuthenticationType,
                JwtClaimTypes.Name, JwtClaimTypes.Role));
        }
    }
}
