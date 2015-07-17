// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net
{
    // Issue 2500: Event logging for System.Net.*
    //
    // Event logging is currently stubbed out; we need to design and implement a solution.
    internal static class Logging
    {
        public static object Http { get { return null; } }

        public static bool On { get { return false; } }

        public static object Web { get { return null; } }

        public static object WebSockets { get { return null; } }

        public static void Associate(object traceSource, object objA, object objB)
        {
        }

        public static void Enter(object traceSource, object obj, string method, object retObject)
        {
        }

        public static void Exception(object traceSource, object obj, string method, Exception e)
        {
        }

        public static void Exit(object traceSource, object obj, string method, object retObject)
        {
        }

        internal static object[] GetObjectLogHash(object obj)
        {
            return default(object[]);
        }

        internal static void PrintError(object traceSource, string errorMessage)
        {
        }

        internal static void PrintError(object traceSource, object obj, string method, string errorMessage)
        {
        }

        internal static void PrintInfo(object traceSource, object obj, string message)
        {
        }

        internal static void PrintWarning(object traceSource, string warning)
        {
        }
    }
}
