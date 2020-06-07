// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    struct BB
    {
        float[] m_afField1;

        BB Method3(float param2, ulong[] param3)
        { return new BB(); }

        static bool Static1(float[] param1) { return false; }

        static int Main()
        {
            try
            {
                throw new Exception();
            }
            catch (Exception)
            {
                Static1(new BB().Method3(0.0f, null).m_afField1);
                Console.WriteLine("PASSED");
                return 100;
            }
            return -1;
        }
    }
}
