using System;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace ResourcesTest
{
    public class TestClass
    {
        public static int Main(string[] args)
        {
            string expected;
            if (args.Length == 1)
            {
                string cultureToTest = args[0];
                var newCulture = new CultureInfo(cultureToTest);
                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;

                if (cultureToTest == "es-ES")
                    expected = "hola";
                else if (cultureToTest == "ja-JP")
                    expected = "\u3053\u3093\u306B\u3061\u306F";
                else
                    throw new Exception($"Cannot determine the expected output for {cultureToTest}");

            } else {
                expected = "hello";
            }

            var currentCultureName = Thread.CurrentThread.CurrentCulture.Name;

            var rm = new ResourceManager("##RESOURCE_NAME##", typeof(##TYPE_NAME##).Assembly);
            Console.WriteLine($"For '{currentCultureName}' got: {rm.GetString("hello")}");

            return rm.GetString("hello") == expected ? 42 : -1;
        }
    }
}
