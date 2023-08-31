using Microsoft.Extensions.Hosting;
using supper_searchbot.Entity;
using supper_searchbot.Executor;
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace UI
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public IServiceProvider ServiceProvider { get; set; }
        public Main()
        {
            InitializeComponent();
            ServiceProvider = InitHost();
        }

        private IServiceProvider InitHost()
        {
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
            try
            {
                host.Services.GetRequiredService<DataContext>().Database.EnsureCreated();
            }
            catch (Exception)
            {
            }
            return host.Services;
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            await ServiceProvider.GetRequiredService<ExecutorManager>().Execute();
        }
    }
}
