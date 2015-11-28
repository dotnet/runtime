namespace System.Runtime.Tests.Common
{
    internal static class CompareHelper
    {
        public static int NormalizeCompare(int i)
        {
            return
                i == 0 ? 0 :
                i > 0 ? 1 :
                -1;
        }
    }
}
