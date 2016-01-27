// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.IO;
using System.Security;
using System.Text;

namespace TestLibrary
{
    [SecuritySafeCritical]
    public static class Logging
    {
        public static void WriteLine()
        {
            Console.WriteLine();
        }

        public static void WriteLine(bool value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(char value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(char[] buffer)
        {
            Console.WriteLine(buffer);
        }

        public static void WriteLine(char[] buffer, int index, int count)
        {
            Console.WriteLine(new string(buffer, index, count));
        }

        public static void WriteLine(decimal value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(double value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(float value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(int value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(uint value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(long value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(ulong value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(Object value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(String value)
        {
            Console.WriteLine(value);
        }

        public static void WriteLine(String format, Object arg0)
        {
            Console.WriteLine(format, arg0);
        }

        public static void WriteLine(String format, Object arg0, Object arg1)
        {
            Console.WriteLine(format, arg0, arg1);
        }

        public static void WriteLine(String format, Object arg0, Object arg1, Object arg2)
        {
            Console.WriteLine(format, arg0, arg1, arg2);
        }

        public static void WriteLine(String format, params Object[] arg)
        {
            Console.WriteLine(format, arg);
        }

        public static void Write(bool value)
        {
            Console.Write(value.ToString());
        }

        public static void Write(char value)
        {
            Console.Write(value.ToString());
        }

        public static void Write(char[] buffer)
        {
            Console.Write(buffer.ToString());
        }

        public static void Write(String value)
        {
            Console.Write(value);
        }

        public static void Write(Object value)
        {
            Console.Write(String.Format("{0}", value));
        }
    }
}
