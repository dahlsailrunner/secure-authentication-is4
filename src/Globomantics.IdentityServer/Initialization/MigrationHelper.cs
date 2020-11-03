using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Globomantics.IdentityServer.Initialization
{
    public static class MigrationHelper
    {
        public static void ApplyDatabaseSchema(this IApplicationBuilder app)
        {
            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()?.CreateScope();

            serviceScope?.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
            serviceScope?.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();
        }
    }
}
