using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PnP.Core.QueryModel;
using PnP.Core.Services;
using PnP.Core.Services.Builder.Configuration;
using PnP.Core.Auth.Services.Builder.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PnP.Core.Auth;
using System.Security;

namespace MyConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(
                    (hostingContext, logging) =>
                    {
                        logging.AddConsole();
                    }
                )
                .ConfigureServices(
                    (hostingContext, services) =>
                    {
                        services.AddPnPCore();
                        /*services.Configure<PnPCoreOptions>(
                            hostingContext.Configuration.GetSection("PnPCore")
                        );
                        services.AddPnPCoreAuthentication();
                        services.Configure<PnPCoreAuthenticationOptions>(
                            hostingContext.Configuration.GetSection("PnPCore")
                        );*/

                        SecureString sec = new SecureString();
                        string pwd = ""; /* Not Secure! */
                        pwd.ToCharArray().ToList().ForEach(sec.AppendChar);
                        /* and now : seal the deal */
                        sec.MakeReadOnly();

                        //Create an instance of the Authentication Provider that uses Credential Manager
                        var authenticationProvider = new UsernamePasswordAuthenticationProvider(
                            "",
                            "",
                            "",
                            sec
                        );
                        services.AddPnPCore(options =>
                        {
                            options.DefaultAuthenticationProvider = authenticationProvider;
                        });
                        services.AddPnPCore(options =>
                        {
                            options.DefaultAuthenticationProvider = authenticationProvider;
                            options.Sites.Add(
                                "Contoso",
                                new PnP.Core.Services.Builder.Configuration.PnPCoreSiteOptions
                                {
                                    SiteUrl = "https://contoso.sharepoint.com/",
                                    AuthenticationProvider = authenticationProvider
                                }
                            );
                        });
                    }
                )
                // Let the builder know we're running in a console
                .UseConsoleLifetime()
                // Add services to the container
                .Build();

            await host.StartAsync();

            using (var scope = host.Services.CreateScope())
            {
                var pnpContextFactory =
                    scope.ServiceProvider.GetRequiredService<IPnPContextFactory>();

                using (var context = await pnpContextFactory.CreateAsync("Contoso"))
                {
                    var web = await context.Web.GetAsync(
                        p => p.Title,
                        p => p.Lists,
                        p => p.MasterUrl
                    );

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("===Web (REST)===");
                    Console.WriteLine($"Title: {web.Title}");
                    Console.WriteLine($"# Lists: {web.Lists.Length}");
                    Console.WriteLine($"Master page url: {web.MasterUrl}");
                    Console.ResetColor();

                    

                    // We can retrieve the whole list of lists
                    // and their items in the context web
                    var listsQuery =
                        from l in context.Web.Lists.QueryProperties(
                            l => l.Id,
                            l => l.Title,
                            l => l.Description
                        )
                        orderby l.Title descending
                        select l;

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("===LINQ: Retrieve list and list items===");
                    foreach (var list in listsQuery.ToList())
                    {
                        Console.WriteLine(
                            $"{list.Id} - {list.Title} - Items count: {list.Items.Length}"
                        );
                    }
                    Console.ResetColor();
                }
            }
        }
    }
}
