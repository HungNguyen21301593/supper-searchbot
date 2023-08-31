using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using supper_searchbot.Entity;
using System.Collections.ObjectModel;

namespace supper_searchbot.Executor
{
    public class FacebookMarketPlace : ExecutorBase, IDisposable
    {
        private By locator = By.CssSelector("a[href*='/marketplace/item']");
        public FacebookMarketPlace(ExecutorSetting executorSetting, DataContext dataContext) :
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
                        var longSleep = TimeSpan.FromMinutes(5);
                        Console.WriteLine($"Wait {longSleep}");
                        await Task.Delay(longSleep);
                    }
                }
            }
        }

        public async Task ScanPage(string homeUrl)
        {
            var newTitles = await GetAllNewUrlTitles(homeUrl);
            if (newTitles is null)
            {
                Console.WriteLine($"Scanned and found nothing");
                return;
            }
            LogsMessageBuilder.Clear();
            LogsMessageBuilder.AppendLine($"Found {newTitles.Count} new ads, scanning all");
            LogsMessageBuilder.AppendLine($"--------------------------------------------------------------------");
            await SendLogMessage(LogsMessageBuilder);
            newTitles.Reverse();
            foreach (var newTitle in newTitles.Select((value, index) => (value, index)))
            {
                try
                {
                    LogsMessageBuilder.Clear();
                    Console.WriteLine($"Scanning {newTitle.index}, title: {newTitle.value}");
                    Console.WriteLine("--------------------------------------------------------------------");
                    LogsMessageBuilder.AppendLine($"Scanning {newTitle.index}, title: {newTitle.value}");
                    LogsMessageBuilder.AppendLine("--------------------------------------------------------------------");
                    Switch(homeUrl);
                    Console.WriteLine($"Original: {newTitle.value}");
                    var titleToSearch = NormalizeText(newTitle.value);
                    Console.WriteLine($"Formatted: {titleToSearch}");

                    Switch(homeUrl);
                    var AllElements = await GetAllUrlElements(locator);
                    var titleElementIndex = GetElementIndexByText(AllElements, titleToSearch);
                    if (titleElementIndex == -1)
                    {
                        LogsMessageBuilder.AppendLine($"Listing is advertisement, skip");
                        Console.WriteLine($"Listing is advertisement, skip");
                        continue;
                    }
                    AllElements.ElementAt(titleElementIndex).Click();
                    var description = await GetDescription();
                    Console.WriteLine($"Description: {description}");
                    await ProceedSendMessageAsync(description, newTitle.value);
                    await SaveTitle(newTitle.value);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"There was an error reading listing {newTitle} with home url {homeUrl}, skipped, e:{e.Message}");
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
            var element = WebDriver.FindElements(By.TagName("body"));
            var more = WebDriver.FindElements(By.XPath("//*[contains(text(), 'Xem thêm')]"));
            more.FirstOrDefault()?.Click();
            var rawDescription =  element?.FirstOrDefault()?.GetAttribute("innerText") ?? "";
            return rawDescription;
        }

        public async Task<List<string>> GetAllNewUrlTitles(string homeUrl)
        {
            
            Switch(homeUrl);
            await RandomSleep();

            var noResult = WebDriver.FindElements(By.XPath("//*[contains(text(),  'No results found')]"));
            if (noResult.Any())
            {
                return null;
            }
            Switch(homeUrl);
            var urls = await GetAllUrlElements(locator);

            var allTitles = new List<string>();
            foreach (var url in urls.Where(target => urls.IndexOf(target) < setting.MaximumAdsOnEachPage))
            {
                allTitles.Add(NormalizeText(url.GetAttribute("innerText")));
            }
            Console.WriteLine($"---------------------------Top {setting.MaximumAdsOnEachPage} Listings-----------------------------------------");
            Console.WriteLine(string.Join(Environment.NewLine, allTitles));
            Console.WriteLine($"---------------------------New Listing-----------------------------------------");
            var newTitleResults = await FilterOut(allTitles);
            Console.WriteLine(string.Join(Environment.NewLine, newTitleResults));
            Console.WriteLine("--------------------------------------------------------------------");
            return newTitleResults;
        }

        public async Task<ReadOnlyCollection<IWebElement>> GetAllUrlElements(By locator, ReadOnlyCollection<IWebElement>? previousUrls = null)
        {
            if (previousUrls is not null && !previousUrls.Any())
            {
                new Actions(WebDriver).MoveToElement(previousUrls.Last()).Perform();
            }
            await RandomSleep();
            var urls = WebDriver.FindElements(locator);
            return urls;
        }

        public int GetElementIndexByText(IReadOnlyCollection<IWebElement> sourceElements, string searchText)
        {
            for (int index = sourceElements.Count - 1; index >= 0; index--)
            {
                var sourceElementText = sourceElements.ElementAt(index).GetAttribute("innerText");
                var formatedSourceElementText = NormalizeText(sourceElementText);
                var formatedsearchText = NormalizeText(searchText);

                if (formatedSourceElementText.Contains(formatedsearchText))
                {
                    return index;
                }
            }
            return -1;
        }
    }
}