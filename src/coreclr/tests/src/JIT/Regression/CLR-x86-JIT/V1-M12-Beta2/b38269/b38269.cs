// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class AA
    {
        private AA m_buddy = null;

        public AA(int reclevel) { if (reclevel < 1000) m_buddy = new AA(reclevel + 1); }

        ~AA() { }
    }

    class App
    {
        static AA s_aa = new AA(0);

        static int Main()
        {
            s_aa = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("If you see this, test passed.");
            return 100;
        }
    }
}
