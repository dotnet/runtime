using System;
using System.Globalization;
using System.Threading;

public class Cx
{
    static void Main()
    {
        Type.GetType("System.Console, System.Console").GetMethod("WriteLine", new[] {typeof(string)})
            .Invoke(null, new[] {"hello"});
        
        var cculture = new CultureInfo("ru-RU");
        Thread.CurrentThread.CurrentCulture = cculture;
        Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;
        
        Console.WriteLine("\nTime:");
        Console.WriteLine(DateTime.Now.ToLongDateString());
        Console.WriteLine(DateTime.Now.ToLongTimeString());
        
        Console.WriteLine("\nCulture:");
        Console.WriteLine(cculture.DisplayName);
        Console.WriteLine(cculture.EnglishName);
        Console.WriteLine(cculture.NativeName);
        Console.WriteLine(cculture.ThreeLetterWindowsLanguageName);
        Console.WriteLine(cculture.ThreeLetterISOLanguageName);
        Console.WriteLine(cculture.TwoLetterISOLanguageName);
        Console.WriteLine(cculture.IetfLanguageTag);
        Console.WriteLine(string.Join(";", cculture.DateTimeFormat.DayNames));
        Console.WriteLine(string.Join(";", cculture.DateTimeFormat.MonthNames));
        Console.WriteLine(string.Join(";", cculture.DateTimeFormat.MonthGenitiveNames));
        Console.WriteLine(string.Join(";", cculture.DateTimeFormat.ShortestDayNames));
        Console.WriteLine(string.Join(";", cculture.DateTimeFormat.GetAllDateTimePatterns()));
        Console.WriteLine(cculture.DateTimeFormat.TimeSeparator);
        Console.WriteLine(cculture.DateTimeFormat.AbbreviatedDayNames);
        Console.WriteLine(cculture.DateTimeFormat.AbbreviatedMonthNames);
        Console.WriteLine(cculture.DateTimeFormat.AMDesignator);
        Console.WriteLine(cculture.DateTimeFormat.LongDatePattern);
        Console.WriteLine(cculture.DateTimeFormat.LongTimePattern);
        Console.WriteLine(cculture.DateTimeFormat.MonthDayPattern);
        Console.WriteLine(cculture.IetfLanguageTag);
        
        Console.WriteLine("\nCurrency:");
        Console.WriteLine(cculture.NumberFormat.CurrencySymbol);
        Console.WriteLine(cculture.NumberFormat.CurrencyDecimalSeparator);
        Console.WriteLine(cculture.NumberFormat.CurrencyGroupSeparator);
        Console.WriteLine(cculture.NumberFormat.CurrencyDecimalDigits);
        Console.WriteLine(string.Join(";", cculture.NumberFormat.CurrencyGroupSizes));
        Console.WriteLine(cculture.NumberFormat.CurrencyNegativePattern);
        Console.WriteLine(cculture.NumberFormat.CurrencyPositivePattern);
        
        
        Console.WriteLine("\nRegionInfo:");
        var zone = new RegionInfo(cculture.LCID);
        Console.WriteLine(zone.NativeName);
        Console.WriteLine(zone.DisplayName);
        Console.WriteLine(zone.CurrencyEnglishName);
        Console.WriteLine(zone.CurrencyNativeName);
        
        Console.WriteLine(100000000.ToString("C", new CultureInfo("ru-RU")));
        Console.WriteLine(3.14.ToString(cculture));
        
        
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
        {
            var str = "\"" + culture.Name + "\",";
            str = str.Replace('-', '_');
            if (!str.Contains("_"))
                continue;

            //Console.WriteLine(str.PadRight(16, ' '));// + "// " + culture.EnglishName);
        }
    }
}