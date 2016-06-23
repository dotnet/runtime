using ModuleCore;

namespace XmlTests
{
    internal class Program
    {
        public static int Main()
        {
            string args = @"DocType:[DOCTYPE] trace:false Host:[Host]";
            return 100 + TestRunner.Execute(args);
        }
    }
}