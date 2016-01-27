// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace Test
{
    using System;

    class AA
    {
        static void DoAnything() { throw new Exception(); }
        static int Main()
        {
            bool FALSE = true;
            try
            {
#pragma warning disable 1718
                while (FALSE == FALSE)
                {	//Stupid loop to optimize
#pragma warning restore 1718
                    try { DoAnything(); }
                    catch (DivideByZeroException) { }
                }
                try { DoAnything(); }
                catch (Exception) { return 1; }
                try { DoAnything(); }
                catch (Exception) { return 2; }
            }
            catch (Exception) { return 100; }
            return 3;
        }

    }
}
