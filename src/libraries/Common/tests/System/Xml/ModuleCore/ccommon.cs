// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;		//Assert

[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // Common
    //
    ////////////////////////////////////////////////////////////////
    internal class Common
    {
        static public void Assert(bool condition)
        {
            Assert(condition, "Assertion Failed!", null);
        }

        static public void Assert(bool condition, string strCondition)
        {
            Assert(condition, strCondition, null);
        }

        static public void Assert(bool condition, string strCondition, string message)
        {
            Debug.Assert(condition, strCondition + message /*+ new StackTrace()*/);
        }

        static public void Trace(string message)
        {
            Console.WriteLine(message);
            Debug.WriteLine(message);
        }

        static public string ToString(object value)
        {
            if (value == null)
                return null;
            return value.ToString();
        }

        static public string Format(object value)
        {
            if (value == null)
                return "(null)";
            if (value is string)
                return "\"" + value + "\"";

            return ToString(value);
        }
    }
}
