using System;
using System.Runtime.CompilerServices;

namespace Runtime_40607
{
    class Program
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool WillBeInlined(out bool shouldBeFalse)
        {
            shouldBeFalse = false;
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SkipLocalsInit]
        static int DependsOnUnInitValue()
        {
            int retVal = 1;
            bool shouldBeFalse;

            while (WillBeInlined(out shouldBeFalse))
            {
                if (shouldBeFalse)
                {
                    retVal = 0;
                }
                break;
            }

            return retVal;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe int PoisonStackWith(uint fillValue)
        {
            int retVal = 1;
            bool shouldBeFalse;

            *(uint*)&shouldBeFalse = fillValue;

            while (WillBeInlined(out shouldBeFalse))
            {
                if (shouldBeFalse)
                {
                    retVal = 0;
                }
                break;
            }

            return retVal;
        }

        static int Main(string[] args)
        {
            PoisonStackWith(0xdeadbeef);

            const int expected = 1;
            int actual = DependsOnUnInitValue();

            if (expected != actual)
            {
                return 0;
            }

            return 100;
        }
    }
}
