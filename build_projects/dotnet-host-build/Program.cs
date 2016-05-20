using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Host.Build
{
    public class Program
    {
        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            return BuildSetup.Create(".NET Core Host")
                .UseStandardGoals()
                .UseAllTargetsFromAssembly<Program>()
                .Run(args);
        } 
    }
}
