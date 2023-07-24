using System;
namespace HelloWorld
{

  public class Strings
  {
      public static void Parse()
      {
          (string dateAsString, string description)[] dateInfo = {
              // ("08/18/2018 07:22:16", "String with a date and time component"),
              // ("08/18/2018", "String with a date component only"),
              // ("8/2018", "String with a month and year component only"),
              // ("8/18", "String with a month and day component only"),
              // ("07:22:16", "String with a time component only"),
              // ("7 PM", "String with an hour and AM/PM designator only"),
              // ("2018-08-18T07:22:16.0000000Z", "UTC string that conforms to ISO 8601"),
              // ("2018-08-18T07:22:16.0000000-07:00", "Non-UTC string that conforms to ISO 8601"),
              // ("Sat, 18 Aug 2018 07:22:16 GMT", "String that conforms to RFC 1123"),
              ("08/18/2018 07:22:16 -5:00", "String with date, time, and time zone information" )
          };

          Console.WriteLine($"Today is {DateTime.Now:d}\n");

          foreach ((string,string) item in dateInfo)
          {
              Console.WriteLine($"{item.Item2 + ":",-52} '{item.Item1}' --> {DateTime.Parse(item.Item1)}");
          }
      }
  }
  class Program
  {
    static int Main()
    {
      Strings.Parse();
      Console.WriteLine("Hello World, Hello RISC-V ;)");
      return 0;
    }
  }
}
