// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;		//Assert

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAssembly]

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // Common
    //
    ////////////////////////////////////////////////////////////////
    internal class Common
    {
        public static void Assert(bool condition)
        {
            Assert(condition, "Assertion Failed!", null);
        }

        public static void Assert(bool condition, string strCondition)
        {
            Assert(condition, strCondition, null);
        }

        public static void Assert(bool condition, string strCondition, string message)
        {
            Debug.Assert(condition, strCondition + message /*+ new StackTrace()*/);
        }

        public static void Trace(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);
        }

        public static string ToString(object value)
        {
            if (value == null)
                return null;
            return value.ToString();
        }

        public static string Format(object value)
        {
            if (value == null)
                return "(null)";
            if (value is string)
                return "\"" + value + "\"";

            return ToString(value);
        }
    }
}
