#nullable enable
using Xunit.Abstractions;

namespace System.Net.NameResolution.Tests
{
    internal static class NameResolutionTestHelper
    {
        public static unsafe bool EnsureNameToAddressWorks(string hostName, ITestOutputHelper? testOutput, bool throwOnFailure = true)
        {
            // NOP, since we do not expect DNS failures on Windows.
            return true;
        }
    }
}
