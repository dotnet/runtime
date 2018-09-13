using System;
using System.Reflection;

namespace LightupLib
{
    public class Greet
    {
        public static string Hello(string name)
        {
            // Load a dependency of LightupLib
            var t = typeof(Newtonsoft.Json.JsonReader);
            if (t != null)
                return "Hello "+name;
            else 
                return "Failed to load LibDependency";
        }
    }
}
