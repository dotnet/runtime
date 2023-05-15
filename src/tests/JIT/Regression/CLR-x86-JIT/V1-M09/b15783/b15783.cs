// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace DefaultNamespace
{
    using System;
    public class jitbug
    {
        public static UInt16 f()
        {
            return UInt16.MaxValue;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Object v = ((UInt16)65535);
            Console.WriteLine("v.ToUInt16: " + v);
            Console.WriteLine("UInt16.MaxValue: " + UInt16.MaxValue);

            // This worked...  I couldn't simplify it.
            if (f() != UInt16.MaxValue)
                Console.WriteLine("Ack!  f() didn't return the correct value!");
            else
                Console.WriteLine("ushort comparison looked good...");

            if (((UInt16)v) != UInt16.MaxValue)
                throw new Exception("UInt16.MaxValue from Object as UInt16 wasn't right!  " + (UInt16)v);
            Console.WriteLine("pass");
            return 100;
        }
    }
}
