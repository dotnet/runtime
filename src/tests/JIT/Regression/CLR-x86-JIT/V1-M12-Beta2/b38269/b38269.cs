// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    class AA
    {
        private AA m_buddy = null;

        public AA(int reclevel) { if (reclevel < 1000) m_buddy = new AA(reclevel + 1); }

        ~AA() { }
    }

    public class App
    {
        static AA s_aa = new AA(0);

        [Fact]
        public static int TestEntryPoint()
        {
            s_aa = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("If you see this, test passed.");
            return 100;
        }
    }
}
