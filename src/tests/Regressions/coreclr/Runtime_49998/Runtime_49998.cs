using System;
using Xunit;
using TestLibrary;

namespace InterfaceMain
{
    public interface Program
    {
        [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
        [Fact]
        public static void TestEntryPoint()
        {
        }
    }
}
