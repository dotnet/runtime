// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
/*
set DOTNET_JITStress=1
*/
namespace Test
{
    using System;

    public class AA
    {
        internal static void Method1()
        {
            uint L = 0;
#pragma warning disable 1718
            while (L == L)
#pragma warning restore 1718
            {
                L = 1;
                try
                {
                    throw new NullReferenceException();
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                AA.Method1();
                Console.WriteLine("failed");
                return 1;
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("passed");
                return 100;
            }
        }
    }
}
