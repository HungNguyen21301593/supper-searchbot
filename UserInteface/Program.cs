using Microsoft.Extensions.Hosting;
using supper_searchbot.Entity;
using supper_searchbot.Executor;
using Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using supper_searchbot.Executor;
using supper_searchbot.Entity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace UserInteface
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton<ExecutorManager>();
            var environmentConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            Console.WriteLine($"CONNECTION_STRING: {environmentConnectionString}");
            var appSettingconnection = builder.Configuration.GetConnectionString("WebApiDatabase");
            Console.WriteLine($"WebApiDatabase: {appSettingconnection}");
            var connection = environmentConnectionString ?? appSettingconnection;
            Console.WriteLine($"connection: {connection}");
            builder.Services.AddDbContext<DataContext>(opt =>
            opt.UseSqlite(connection));
            IHost host = builder.Build();
            Application.Run(new Main(host.Services));
        }
    }
}