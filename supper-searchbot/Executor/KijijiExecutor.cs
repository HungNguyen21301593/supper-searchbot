using AngleSharp.Common;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using supper_searchbot.Entity;

namespace supper_searchbot.Executor;

public class KijijiExecutor : ExecutorBase, IDisposable
{
    public KijijiExecutor(ExecutorSetting executorSetting, DataContext dataContext) :
        base(executorSetting, dataContext)
    {
    }

    public async Task RunAsync()
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
        var urls = await GetAllUrlElements(homeUrl);
        var startPosition = GetStartPosition();
        var stopPosition = GetStopPosition(urls.Count);
        LogsMessageBuilder.AppendLine($"Found {urls.Count}, scanning from position {startPosition} to position {stopPosition}");
        if (stopPosition == 0 || startPosition >= stopPosition)
        {
            LogsMessageBuilder.AppendLine($"Found no result");
            Console.WriteLine($"Found no result");
            await SendLogMessage(LogsMessageBuilder);
            return;
        }
        await SendLogMessage(LogsMessageBuilder);
        for (var urlIndex = startPosition; urlIndex < stopPosition; urlIndex++)
        {
            try
            {
                LogsMessageBuilder.Clear();
                Console.WriteLine($"Page {homeUrl} - Ad {urlIndex}");
                Console.WriteLine("--------------------------------------------------------------------");
                LogsMessageBuilder.AppendLine($"--------------------------------------------------------------------");
                LogsMessageBuilder.AppendLine($"Ad {urlIndex}");
                var titleElement = urls.GetItemByIndex(urlIndex).FindElement(By.ClassName("title"));
                var title = titleElement.GetAttribute("innerText").ToLower();
                LogsMessageBuilder.AppendLine($"Found title: {title}");

                if (await HasTitleAlreadySaved(title))
                {
                    Console.WriteLine($"title: {title} is already saved, savedTitles");
                    LogsMessageBuilder.AppendLine($"{title} is already saved, so skip");
                    continue;
                }

                titleElement.Click();
                await RandomSleep();
                Console.WriteLine($"Title: {title}");
                LogsMessageBuilder.AppendLine($"Ad URL: {WebDriver.Url}");
                var description = (await GetDescription()).ToLower();
                Console.WriteLine($"Description: {description}");
                await ProceedSendMessageAsync(description, title);
                await SaveTitle(title);
            }
            catch (Exception e)
            {
                Console.WriteLine($"There was an error reading ad {urlIndex} with home url {homeUrl}, skipped, e:{e.Message}");
            }
            finally
            {
                urls = await GetAllUrlElements(homeUrl);
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
        return element?.FirstOrDefault().GetAttribute("innerText");
    }

    public async Task<IReadOnlyCollection<IWebElement>> GetAllUrlElements(string homeUrl)
    {
        Switch(homeUrl);
        await RandomSleep();
        var elements = WebDriver.FindElements(By.ClassName("info-container"));
        new Actions(WebDriver).MoveToElement(elements.Last()).Perform();
        return WebDriver.FindElements(By.ClassName("info-container"));
    }
}
