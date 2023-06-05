using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using supper_searchbot.Entity;
using System.Text;
using Telegram.Bot;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace supper_searchbot.Executor
{
    public abstract class ExecutorBase
    {
        private readonly DataContext dataContext;

        public IWebDriver WebDriver { get; }
        public ExecutorSetting ExecutorSetting { get; }
        public Setting setting { get; set; }
        public StringBuilder LogsMessageBuilder { get; set; }
        public Random RandomSleepEngine { get; set; }

        public ExecutorBase(ExecutorSetting executorSetting, DataContext dataContext)
        {
            ExecutorSetting = executorSetting ?? throw new ArgumentNullException(nameof(executorSetting));
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            WebDriver = SetupDriverInstance();
            setting = Setting.FromString(executorSetting.SettingJson);
            LogsMessageBuilder = new StringBuilder();
            RandomSleepEngine = new Random();
        }

        public async Task UpdateExecutorLastRunTime()
        {
            ExecutorSetting.LastExcuted = DateTime.UtcNow;
            await dataContext.SaveChangesAsync();
        }

        public async Task RandomSleep()
        {
            var from = int.Parse(Environment.GetEnvironmentVariable("SLEEP_INTERVAL_FROM") ?? "5");
            var to = int.Parse(Environment.GetEnvironmentVariable("SLEEP_INTERVAL_TO") ?? "7");
            var sleep = RandomSleepEngine.Next(from, to);
            Console.WriteLine($"Sleep {sleep}");
            await Task.Delay(TimeSpan.FromSeconds(sleep));
        }

        public async Task ProceedSendMessageAsync(string description, string title, string phone = "", string name = "", string urlToscan = "")
        {
            var webUrl = WebDriver.Url;
            var message = GenerateMessage(description, title, phone, name, webUrl);
            var lowerDescription = description.ToLower().Trim();
            var lowerTitle = title.ToLower().Trim();

            var containAllMustHaveKeywords = setting.MustHaveKeywords
                .Select(k => k.ToLower())
                .All(musthaveKeyword => lowerDescription.Contains(musthaveKeyword) || lowerTitle.Contains(musthaveKeyword));
            if (!containAllMustHaveKeywords)
            {
                LogsMessageBuilder.AppendLine($"Does not contain all must have keywords: {string.Join(",", setting.MustHaveKeywords)}, skip");
                return;
            }
            var shouldIgnored = setting.ExcludeKeywords
                .Select(k => k.ToLower())
                .Any(lowkey => lowerDescription.Contains(lowkey) || lowerTitle.Contains(lowkey));
            if (shouldIgnored)
            {
                LogsMessageBuilder.AppendLine("Found an exculude keywords in title or description, skip");
                return;
            }
            if (!setting.Keywords.Any())
            {
                var phoneText = string.IsNullOrWhiteSpace(phone) ? $" phone:{phone}" : "";
                await SendMessage(message);
                LogsMessageBuilder.AppendLine("There is no keyword define, proceed send notification");
            }

            var listMatcheKeywords = new List<string>();
            foreach (var lowkeyword in setting.Keywords.Select(k => k.ToLower()))
            {
                if (!lowerDescription.Contains(lowkeyword) && !lowerTitle.Contains(lowkeyword))
                {
                    Console.WriteLine($"There is no keyword \"{lowkeyword}\" on title or description");
                    LogsMessageBuilder.AppendLine($"There is no keyword \"{lowkeyword}\" on title or description");
                    continue;
                }

                if (lowerTitle.Contains(lowkeyword) || lowerDescription.Contains(lowkeyword))
                {
                    LogsMessageBuilder.AppendLine("Found ad matched, add to send list");
                    listMatcheKeywords.Add(lowkeyword);
                    continue;
                }
            }

            if (listMatcheKeywords.Any())
            {
                var foundMessage = $"{string.Join(", ", listMatcheKeywords.Select(s => s.ToUpper()))} in:" +
                       $"{Environment.NewLine}>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>" +
                       $"{Environment.NewLine}{message}";
                await SendMessage(foundMessage);
            }
        }

        public async Task SendMessage(string text)
        {
            try
            {
                var bot = new TelegramBotClient(ExecutorSetting.User.TelegramBot.Token);
                foreach (var telegramId in setting.TelegramIds)
                {
                    await bot.SendTextMessageAsync(telegramId, text);
                    Console.WriteLine($"Sent success");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"err:{e.Message} | {e.StackTrace}");
            }
        }

        public async Task SendLogMessage(StringBuilder logsMessageBuilder)
        {
            try
            {
                var text = logsMessageBuilder.ToString();
                var truncateText = text.Substring(0, Math.Min(4000, text.Length - 1));
                var bot = new TelegramBotClient(ExecutorSetting.User.TelegramBot.Token);
                foreach (var telegramId in setting.DumbTelegramIds)
                {
                    await bot.SendTextMessageAsync(telegramId, truncateText);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"err:{e.Message} | {e.StackTrace}");
            }
        }

        public void Switch(string url)
        {
            if (WebDriver.Url.Equals(url))
            {
                WebDriver.Navigate().Refresh();
                return;
            }
            WebDriver.Navigate().GoToUrl(url);
        }

        private string GenerateMessage(string description = "", string title = "", string phone = "", string name = "", string webUrl = "")
        {
            var stringBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(name))
            {
                stringBuilder.AppendLine($"Name: {name}");
                stringBuilder.AppendLine("------------------------------------------------------------------------");
            }
            if (!string.IsNullOrWhiteSpace(phone))
            {
                stringBuilder.AppendLine($"Phone: {phone}");
                stringBuilder.AppendLine("------------------------------------------------------------------------");
            }
            if (!string.IsNullOrWhiteSpace(title))
            {
                stringBuilder.AppendLine($"Title: {title}");
                stringBuilder.AppendLine("------------------------------------------------------------------------");
            }
            if (!string.IsNullOrWhiteSpace(description))
            {
                stringBuilder.AppendLine($"Description: {description}");
                stringBuilder.AppendLine("------------------------------------------------------------------------");
            }

            stringBuilder.AppendLine($"URL: {webUrl} ");
            return stringBuilder.ToString();
        }

        public int GetStartPosition()
        {
            return setting.MinAdsPositionOnEachPage;
        }

        public int GetStopPosition(int urlsCount)
        {
            return Math.Min(urlsCount, setting.MinAdsPositionOnEachPage + setting.MaximumAdsOnEachPage);
        }
        public Task TryHandleErrorUnreachableException(Exception inputException)
        {
            try
            {
                Console.WriteLine($"Try to handle error unreachable Exception, {inputException}");
                WebDriver.Navigate().GoToUrl("chrome://net-internals/#dns");
                WebDriver.FindElements(By.CssSelector("button[value='Clear host cache']"))
                    .FirstOrDefault()?.Click();
                Console.WriteLine("Cleaned host cache, proceed to quit and reload");
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine($"There was an error to handle the UnreachableException, {e.Message}");
                throw;
            }
        }

        public async Task<bool> HasTitleAlreadySaved(string verifyingTitle)
        {
            var result = await dataContext.Histories
                .AnyAsync(h => h.ExecutorSettingId == ExecutorSetting.ID
                    && h.Created >= DateTime.UtcNow.AddDays(-2)
                    && h.Title.Trim().ToLower().Equals(verifyingTitle.Trim().ToLower()));
            return result;
        }

        public async Task SaveTitle(string title)
        {
            dataContext.Histories.Add(
                new History
                {
                    Id = 0,
                    Title = title,
                    Created = DateTime.UtcNow,
                    ExecutorSettingId = ExecutorSetting.ID,
                }
                );
            await dataContext.SaveChangesAsync();
        }

        private IWebDriver SetupDriverInstance()
        {
            try
            {
                new DriverManager().SetUpDriver(new ChromeConfig(), "MatchingBrowser");
            }
            catch (Exception)
            {
            }
            var chromeArguments = GetGeneralSetting();
            var options = new ChromeOptions();
            options.AddExcludedArgument("enable-automation");
            options.AddArguments(chromeArguments);
            options.PageLoadStrategy = PageLoadStrategy.Eager;
            var remoteDriver = new ChromeDriver(options);
            return remoteDriver ?? throw new ArgumentNullException($"Could not init web diver");
        }

        private IEnumerable<string> GetGeneralSetting()
        {
            var chromeArguments = new List<string> {
                "--disable-extensions",
                "--headless",
                "--incognito",
                "--disable-notifications",
                "--start-maximized",
                "--ignore-ssl-errors",
                "--ignore-certificate-errors",
                "--no-sandbox",
                "--disable-gpu",
                "--disable-logging",
                "--disable-popup-blocking",
                "disable-blink-features=AutomationControlled",
                "--disable-dev-shm-usage",
                "--log-level=3",
                "enable-features=NetworkServiceInProcess",
                "--disable-features=NetworkService",
            };
            return chromeArguments;
        }

        public void Dispose()
        {
            WebDriver?.Quit();
            Clean();
        }

        public void Clean()
        {
            Process[] chromeDriverProcesses = Process.GetProcessesByName("chromedriver");

            foreach (var chromeDriverProcess in chromeDriverProcesses)
            {
                chromeDriverProcess.Kill();
            }

            Process[] chromeExes = Process.GetProcessesByName("chrome");

            foreach (var chromeExe in chromeExes)
            {
                chromeExe.Kill();
            }
        }
    }
}
