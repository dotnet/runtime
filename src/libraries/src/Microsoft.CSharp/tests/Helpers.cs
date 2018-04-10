using System;
using System.IO;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public static class Helpers
    {
        public static void OverrideConsoleAndRun(Action<StringWriter> command)
        {
            TextWriter savedOut = Console.Out;
            try
            {
                using (var stringWriter = new StringWriter())
                {
                    Console.SetOut(stringWriter);
                    command(stringWriter);
                }
            }
            finally
            {
                Console.SetOut(savedOut);
            }
        }
    }
}
