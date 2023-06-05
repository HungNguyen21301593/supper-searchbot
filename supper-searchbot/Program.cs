// See https://aka.ms/new-console-template for more information
using Scheduling;
using supper_searchbot.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using supper_searchbot.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ExecutorManager>();
var environmentConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
Console.WriteLine($"CONNECTION_STRING: {environmentConnectionString}");
var appSettingconnection = builder.Configuration.GetConnectionString("WebApiDatabase");
Console.WriteLine($"WebApiDatabase: {appSettingconnection}");
var connection = environmentConnectionString ?? appSettingconnection;
Console.WriteLine($"connection: {connection}");
builder.Services.AddDbContext<DataContext>(opt =>
opt.UseNpgsql(connection));
IHost host = builder.Build();
var interval = 60;
var random = new Random();
var delay = random.Next(interval/2, interval);
Console.WriteLine($"Wait {delay} before start");
await Task.Delay(TimeSpan.FromSeconds(delay));
try
{
    host.Services.GetRequiredService<DataContext>().Database.EnsureCreated();
}
catch (Exception)
{
}

var manager = host.Services.GetRequiredService<ExecutorManager>();
SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

Action action = async () =>
{
    if (!await semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(10)))
    {
        return;
    }
    try
    {
        await manager.Execute();
    }
    catch (Exception e)
    {
        Console.WriteLine(e.ToString());
    }
    finally
    {
        //When the task is ready, release the semaphore. It is vital to ALWAYS release the semaphore when we are ready, or else we will end up with a Semaphore that is forever locked.
        //This is why it is important to do the Release within a try...finally clause; program execution may crash or take a different path, this way you are guaranteed execution
        semaphoreSlim.Release();
    }
};
Schedule.Every(interval).Seconds().Run(action);
host.Run();
