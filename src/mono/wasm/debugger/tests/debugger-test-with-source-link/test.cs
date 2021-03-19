using System;

namespace ExampleLibrary
{
    public static class Greeter
    {
        public static string Greet(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            return $"Hello {name}!";
        }
    }
}
