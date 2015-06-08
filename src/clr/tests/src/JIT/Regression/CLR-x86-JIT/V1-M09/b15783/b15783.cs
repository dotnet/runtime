// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace DefaultNamespace
{
    using System;
    public class jitbug
    {
        public static UInt16 f()
        {
            return UInt16.MaxValue;
        }

        public static int Main(String[] args)
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