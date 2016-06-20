using ModuleCore;

namespace XslCompiledTransformTests
{
    internal class Program
    {
        public static int Main()
        {
            var args = @"DocType:XmlDocument  trace:false Host:None";
            return 100 + TestRunner.Execute(args);
        }
    }
}