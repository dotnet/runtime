using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AlphaVantageDataParser;
using Helper.ReadAsAsync;
using System.Linq;
using System.Collections;

namespace NetCore.Sample.StockTicker
{

    public class Stock
    {
        static HttpClient client = new HttpClient();
        static readonly string URL_FORMAT=@"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={0}&apikey=YLAA0KGL2VNVRZZJ";

        static TimeSeriesDaily stockTimeSeriesDaily = null;
        static async Task<TimeSeriesDaily> GetTimeSeriesDailyAsync(string path)
        {
            TimeSeriesDaily data = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
               data = await response.Content.ReadAsJsonAsync<TimeSeriesDaily>();
            }
            return data;
        }

        public static void ShowStockDetails(string symbol)
        {
            
            if(stockTimeSeriesDaily == null | stockTimeSeriesDaily.MetaData == null)
            {
             Console.WriteLine($"Invalid Symbol : '{symbol}' used . Please try MSFT for exmaple ");
             return;
            }
            Console.WriteLine("---------------------------------");
            Console.WriteLine($"Symbol: {stockTimeSeriesDaily?.MetaData?.Symbol}");
            Console.WriteLine($"Information: {stockTimeSeriesDaily?.MetaData?.Information}");
            Console.WriteLine($"Last Refreshed: {stockTimeSeriesDaily?.MetaData?.LastRefreshed}");
            Console.WriteLine($"TimeZone : {stockTimeSeriesDaily?.MetaData?.TimeZone}");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("");

            Console.WriteLine("Daily details for last 5 days");
            Console.WriteLine("---------------------------------");

            int counter=0;
            foreach(var item in stockTimeSeriesDaily?.TimeSeriesDailyItem)
            {
                if(counter++ < 5)
                {
                    Console.WriteLine($"Date: {item.Key}");

                    Console.WriteLine($"High:{item.Value?.High}");
                    Console.WriteLine($"Low:{item.Value?.Low}");
                    Console.WriteLine($"Open: {item.Value?.Open}");
                    Console.WriteLine($"Volume: {item.Value?.Volume}");

                    Console.WriteLine();
                }
            }

        }

        static async Task RunAsync(string symbol)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                string url = string.Format(URL_FORMAT,symbol);
                stockTimeSeriesDaily = await GetTimeSeriesDailyAsync(url);

                ShowStockDetails(symbol);

            }
            catch (Exception e)
            {
                Console.WriteLine("Error Logged :" + e.Message);
            }
        }
        
        static void Main(string[] args)
        {
            string inputStock= (args.Length!=1)?"MSFT":args[0];
       
            RunAsync(inputStock).GetAwaiter().GetResult();
            ShowUsage();

        }

        static void ShowUsage()
        {
            Console.WriteLine(" Usage : stockapp <<SymbolName>> .For example run 'stockapp MSFT'" );
        }

    }
}