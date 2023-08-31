using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using supper_searchbot.Entity;

namespace supper_searchbot.Executor
{
    public class KijijiExecutor : ExecutorBase
    {
        public KijijiExecutor(ExecutorSetting executorSetting, DataContext dataContext) :
            base(executorSetting, dataContext)
        {
        }

        public override async Task RunAsync()
        {
            foreach (var criteriaUrl in setting.BaseUrlSetting.CriteriaUrls)
            {
                for (var pageIndex = setting.StartPage; pageIndex < setting.EndPage; pageIndex++)
                {
                    var allParams = setting.BaseUrlSetting.StaticParams;
                    allParams.TryAdd(setting.BaseUrlSetting.DynamicParams.Page, pageIndex.ToString());
                    var homeUrl = allParams.Apply(criteriaUrl);
                    try
                    {
                        LogsMessageBuilder.Clear();
                        Console.WriteLine($"********************* Start page {pageIndex} *********************");
                        LogsMessageBuilder.AppendLine($"------------------------------------------------------------------");
                        LogsMessageBuilder.AppendLine($"Proceed scan page {homeUrl} with keywords: {string.Join(", ", setting.Keywords)}");
                        await SendLogMessage(LogsMessageBuilder);
                        await ScanPage(homeUrl);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Warning, there was an error {e.Message} {e.GetType()}");
                        await TryHandleErrorUnreachableException(e);
                    }
                    finally
                    {
                        LogsMessageBuilder.Clear();
                        Console.WriteLine($"********************* End page {pageIndex} *********************");
                        LogsMessageBuilder.AppendLine($"Done scan page {homeUrl}");
                        LogsMessageBuilder.AppendLine($"------------------------------------------------------------------");
                        await SendLogMessage(LogsMessageBuilder);
                    }
                }
            }
        }

        public async Task ScanPage(string homeUrl)
        {
            var titles = await GetAllUrlElements(homeUrl);
            LogsMessageBuilder.Clear();
            LogsMessageBuilder.AppendLine($"Page {homeUrl}, found {titles.Count} new ads, scanning all");
            LogsMessageBuilder.AppendLine($"--------------------------------------------------------------------");
            await SendLogMessage(LogsMessageBuilder);
            foreach (var title in titles.Select((value,index)=> (value, index)))
            {
                try
                {
                    LogsMessageBuilder.Clear();
                    Console.WriteLine($"Scanning {title.index}, title: {title.value}, url: {homeUrl}");
                    Console.WriteLine("--------------------------------------------------------------------");
                    LogsMessageBuilder.AppendLine($"Scanning {title.index}, title: {title.value}");
                    LogsMessageBuilder.AppendLine("--------------------------------------------------------------------");
                    Switch(homeUrl);
                    Console.WriteLine($"Original: {title.value}");
                    var titleToSearch = title.value.TrimEnd('\"');
                    Console.WriteLine($"Formatted: {titleToSearch}");
                    var searchXpath = $"//a[contains(normalize-space(),\"{titleToSearch}\")]";
                    Console.WriteLine($"Xpath: {searchXpath}");
                    var titleElements = WebDriver.FindElements(By.XPath(searchXpath));
                    if (!titleElements.Any())
                    {
                        LogsMessageBuilder.AppendLine($"Listing is advertisement, skip");
                        Console.WriteLine($"Listing is advertisement, skip");
                        continue;
                    }
                    await RandomSleep();
                    var url = titleElements.First().GetAttribute("href");
                    Console.WriteLine($"URL: {url}");
                    WebDriver.Navigate().GoToUrl(url);
                    var description = await GetDescription();
                    Console.WriteLine($"Description: {description}");
                    await ProceedSendMessageAsync(description, title.value);
                    await SaveTitle(title.value);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"There was an error reading listing {title} with home url {homeUrl}, skipped, e:{e.Message}");
                }
                finally
                {
                    Console.WriteLine("--------------------------------------------------------------------");
                    await SendLogMessage(LogsMessageBuilder);
                    await UpdateExecutorLastRunTime();
                }
            }
        }

        private async Task<string> GetDescription()
        {
            await RandomSleep();
            var element = WebDriver.FindElements(By.CssSelector("div[class*='descriptionContainer']"));
            return element?.FirstOrDefault()?.GetAttribute("innerText") ?? "";
        }

        public async Task<List<string>> GetAllUrlElements(string homeUrl)
        {
            Switch(homeUrl);
            await RandomSleep();
            var locator = By.XPath("//a[@class='title ' or @data-testid='listing-link']");
            var urls = WebDriver.FindElements(locator);
            if (!urls.Any())
            {
                return new List<string>();
            }
            new Actions(WebDriver).MoveToElement(urls.Last()).Perform();
            urls = WebDriver.FindElements(locator);
            var titles = new List<string>();
            foreach (var url in urls.Where(item => setting.MinAdsPositionOnEachPage < urls.IndexOf(item) && urls.IndexOf(item) <= setting.MinAdsPositionOnEachPage + setting.MaximumAdsOnEachPage))
            {
                titles.Add(url.GetAttribute("innerText").Trim());
            }
            Console.WriteLine("---------------------------All Titles-----------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, titles));
            Console.WriteLine("---------------------------New Titles-----------------------------------------");
            var resultTitles = await FilterOut(titles);
            Console.WriteLine(string.Join(Environment.NewLine, resultTitles));
            Console.WriteLine("--------------------------------------------------------------------");
            return resultTitles;
        }
    }
}