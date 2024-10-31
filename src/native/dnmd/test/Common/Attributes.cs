using System.Runtime.InteropServices;

using Xunit;

namespace Common
{
    public sealed class WindowsOnlyTheoryAttribute : TheoryAttribute
    {
        public WindowsOnlyTheoryAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "Only run on Windows";
            }
        }
    }
}