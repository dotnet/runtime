// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

namespace ShiftTest
{
    public class CL
    {
        public byte clm_data = 0xFF;
    }
    public struct VT
    {
        public byte vtm_data;
    }
    public class byte32Test
    {
        private static byte s_data = 0xFF;
        public static byte f1(byte arg_data)
        {
            arg_data >>= 4;
            return arg_data;
        }
        public static byte f2(byte arg_data)
        {
            arg_data <<= 4;
            return arg_data;
        }
        public static int Main()
        {
            byte loc_data = 0xFF;

            byte[] arr_data = new byte[1];

            CL cl = new CL();
            VT vt;

            s_data = 0xFF;
            loc_data = 0xFF;
            arr_data[0] = 0xFF;
            cl.clm_data = 0xFF;
            vt.vtm_data = 0xFF;

            // Test >>

            Console.WriteLine("The expected result of (0xFF>>4) is: {0}", (0xFF >> 4));
            Console.WriteLine();

            Console.WriteLine("The actual result for function argument is: {0}", f1(0xFF));
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

            if (loc_data != (0xFF >> 4))
            {
                Console.WriteLine("FAILED for local variable");
                return -1;
            }
            if (f1(0xFF) != (0xFF >> 4))
            {
                Console.WriteLine("FAILED for function argument");
                return -1;
            }
            if (s_data != (0xFF >> 4))
            {
                Console.WriteLine("FAILED for static field");
                return -1;
            }
            if (arr_data[0] != (0xFF >> 4))
            {
                Console.WriteLine("FAILED for array element");
                return -1;
            }
            if (cl.clm_data != (0xFF >> 4))
            {
                Console.WriteLine("FAILED for class member");
                return -1;
            }
            if (vt.vtm_data != (0xFF >> 4))
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

            Console.WriteLine("The expected result of (0x1<<4) is: {0}", ((byte)0x1 << 4));
            Console.WriteLine();

            Console.WriteLine("The actual result for function argument is: {0}", f2(0x1));
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
