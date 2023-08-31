using supper_searchbot.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace supper_searchbot.Executor
{
    public class ExecutorManager
    {
        private readonly DataContext dataContext;
        private readonly ILogger<ExecutorManager> logger;

        public ExecutorManager(DataContext dataContext, ILogger<ExecutorManager> logger)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Execute()
        {

            var nextExecutingSetting = await dataContext.ExecutorSettings
                .Include(s => s.User)
                      .ThenInclude(u => u.TelegramBot)
                .Where(s => s.LastExcuted.AddMinutes(5) < DateTime.UtcNow)
                .OrderBy(s => s.LastExcuted)
                .FirstOrDefaultAsync();

            if (nextExecutingSetting is null)
            {
                logger.LogInformation("There is no executor remanning");
                return;
            }
            logger.LogInformation($"Executing {JsonConvert.SerializeObject(nextExecutingSetting.SettingJson)}");
            try
            {
                await dataContext.Entry(nextExecutingSetting).ReloadAsync();
                nextExecutingSetting.LastExcuted = DateTime.UtcNow;
                await dataContext.SaveChangesAsync();
                await ExecuteBaseSetting(nextExecutingSetting);
            }
            catch (Exception e)
            {
                logger.LogError($"Executed failed, {e.Message}");
                await dataContext.Entry(nextExecutingSetting).ReloadAsync();
                nextExecutingSetting.LastExcuted = DateTime.UtcNow.AddDays(-1);
                await dataContext.SaveChangesAsync();
            }
            finally
            {
                await dataContext.Entry(nextExecutingSetting).ReloadAsync();
                nextExecutingSetting.LastExcuted = DateTime.UtcNow;
                await dataContext.SaveChangesAsync();
            }
        }

        public async Task ExecuteBaseSetting(ExecutorSetting nextExecutingSetting)
        {
            using var executor = BuildExecutor(nextExecutingSetting);
            if (executor is null)
            {
                return;
            }
            await executor.RunAsync();
        }

        private ExecutorBase BuildExecutor(ExecutorSetting nextExecutingSetting)
        {
            return nextExecutingSetting.Type switch
            {
                "Kijiji" => new KijijiExecutor(nextExecutingSetting, dataContext),
                "KijijiF" => new KijijiExecutorFast(nextExecutingSetting, dataContext),
                "FBM" => new FacebookMarketPlace(nextExecutingSetting, dataContext),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
