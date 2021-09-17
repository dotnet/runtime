using System;
using System.Reflection;

namespace hammer
{
    class Program
    {
        static void Main(string[] args)
        {
            var asm = Assembly.Load("Location, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            var location = asm.GetType("GPS.Location");
            var city = location.GetProperty("City");
            var cityName = city.GetValue(null);
            Console.WriteLine($"Hi {cityName}!");
        }
    }
}
