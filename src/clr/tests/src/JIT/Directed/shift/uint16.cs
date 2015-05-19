// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace ShiftTest
{
    public class CL
    {
        public ushort clm_data = 0xFFFF;
    }
    public struct VT
    {
        public ushort vtm_data;
    }
    public class ushort32Test
    {
        private static ushort s_data = 0xFFFF;
        public static ushort f1(ushort arg_data)
        {
            arg_data >>= 4;
            return arg_data;
        }
        public static ushort f2(ushort arg_data)
        {
            arg_data <<= 4;
            return arg_data;
        }
        public static int Main()
        {
            ushort loc_data = 0xFFFF;

            ushort[] arr_data = new ushort[1];

            CL cl = new CL();
            VT vt;

            s_data = 0xFFFF;
            loc_data = 0xFFFF;
            arr_data[0] = 0xFFFF;
            cl.clm_data = 0xFFFF;
            vt.vtm_data = 0xFFFF;

            // Test >>

            Console.WriteLine("The expected result of (0xFFFF>>4) is: {0}", (0xFFFF >> 4));
            Console.WriteLine();

            Console.WriteLine("The actual result for funciton argument is: {0}", f1(0xFFFF));
            loc_data >>= 4;
            Console.WriteLine("The actual result for local variable is: {0}", loc_data);
            s_data >>= 4;
            Console.WriteLine("The actual result for static field is: {0}", s_data);
            arr_data[0] >>= 4;
            Console.WriteLine("The actual result for array element is: {0}", arr_data[0]);
            cl.clm_data >>= 4;
            Console.WriteLine("The actual result for class member is: {0}", cl.clm_data);
            vt.vtm_data >>= 4;
            Console.WriteLine("The actual result for valuestruct member is: {0}", vt.vtm_data);

            Console.WriteLine();

            if (loc_data != (0xFFFF >> 4))
            {
                Console.WriteLine("FAILED for local variable");
                return -1;
            }
            if (f1(0xFFFF) != (0xFFFF >> 4))
            {
                Console.WriteLine("FAILED for function argument");
                return -1;
            }
            if (s_data != (0xFFFF >> 4))
            {
                Console.WriteLine("FAILED for static field");
                return -1;
            }
            if (arr_data[0] != (0xFFFF >> 4))
            {
                Console.WriteLine("FAILED for array element");
                return -1;
            }
            if (cl.clm_data != (0xFFFF >> 4))
            {
                Console.WriteLine("FAILED for class member");
                return -1;
            }
            if (vt.vtm_data != (0xFFFF >> 4))
            {
                Console.WriteLine("FAILED for valuestruct member");
                return -1;
            }

            // Test <<

            s_data = 0x1;
            loc_data = 0x1;
            arr_data[0] = 0x1;
            cl.clm_data = 0x1;
            vt.vtm_data = 0x1;

            Console.WriteLine("The expected result of (0x1<<4) is: {0}", ((ushort)0x1 << 4));
            Console.WriteLine();

            Console.WriteLine("The actual result for funciton argument is: {0}", f2(0x1));
            loc_data <<= 4;
            Console.WriteLine("The actual result for local variable is: {0}", loc_data);
            s_data <<= 4;
            Console.WriteLine("The actual result for static field is: {0}", s_data);
            arr_data[0] <<= 4;
            Console.WriteLine("The actual result for array element is: {0}", arr_data[0]);
            cl.clm_data <<= 4;
            Console.WriteLine("The actual result for class member is: {0}", cl.clm_data);
            vt.vtm_data <<= 4;
            Console.WriteLine("The actual result for valuestruct member is: {0}", vt.vtm_data);

            Console.WriteLine();

            if (loc_data != (0x1 << 4))
            {
                Console.WriteLine("FAILED for local variable");
                return -1;
            }
            if (f2(0x1) != (0x1 << 4))
            {
                Console.WriteLine("FAILED for function argument");
                return -1;
            }
            if (s_data != (0x1 << 4))
            {
                Console.WriteLine("FAILED for static field");
                return -1;
            }
            if (arr_data[0] != (0x1 << 4))
            {
                Console.WriteLine("FAILED for array element");
                return -1;
            }
            if (cl.clm_data != (0x1 << 4))
            {
                Console.WriteLine("FAILED for class member");
                return -1;
            }
            if (vt.vtm_data != (0x1 << 4))
            {
                Console.WriteLine("FAILED for valuestruct member");
                return -1;
            }

            Console.WriteLine("PASSED");
            return 100;
        }
    }
}
