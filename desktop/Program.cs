using Microsoft.Extensions.Hosting;
using supper_searchbot.Entity;
using supper_searchbot.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Scheduling;

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
        var db = host.Services.GetRequiredService<DataContext>();
        string settingJson = File.ReadAllText(@"./setting.json");
        var nextExecutingSetting = await db.ExecutorSettings
                .Include(s => s.User)
                .ThenInclude(u => u.TelegramBot)
                .Where(x => x.Type == Setting.FromString(settingJson).Type)
                .FirstOrDefaultAsync();
        if (nextExecutingSetting == null)
        {
            return;
        }
        nextExecutingSetting.SettingJson = settingJson;
        await manager.ExecuteBaseSetting(nextExecutingSetting);
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
var interval = 30;
Schedule.Every(interval).Seconds().Run(action);
host.Run();