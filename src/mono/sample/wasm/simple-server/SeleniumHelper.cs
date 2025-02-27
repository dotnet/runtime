using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Text.RegularExpressions;

namespace HttpServer
{
    public class SeleniumHelper : IDisposable
    {
        private IWebDriver driver;

        public void Dispose()
        {
            StopChromeDriver();
        }

        public SeleniumHelper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            driver = new ChromeDriver(options);

            if (Program.Verbose)
            {
                Console.WriteLine("ChromeDriver started.");
            }
        }

        public void OpenUrl(string url)
        {
            if (Program.Verbose)
            {
                Console.WriteLine($"Navigating chromedriver to url: {url}");
            }

            driver.Navigate().GoToUrl(url);

            if (Program.Verbose)
            {
                Regex? exitRegex = null;
                if (!string.IsNullOrEmpty(Program.ChromeDriverExitLine))
                {
                    Console.WriteLine($"Using exit regex: {Program.ChromeDriverExitLine}");
                    exitRegex = new Regex(Program.ChromeDriverExitLine);
                }

                Console.WriteLine($"Log for url: {url}");

                while (true)
                {
                    foreach (var entry in driver.Manage().Logs.GetLog(LogType.Browser))
                    {
                        Console.WriteLine($"  chromedriver: {entry}");
                        if (exitRegex != null && exitRegex.IsMatch(entry.Message))
                        {
                            Console.WriteLine("  chromedriver: Found exit line. Exiting.");
                            StopChromeDriver();
                            Environment.Exit(0);
                            return;
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            }
        }

        public void StopChromeDriver()
        {
            driver.Quit();
        }
    }
}
