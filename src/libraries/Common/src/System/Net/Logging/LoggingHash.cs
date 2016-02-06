// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace System.Net
{
    internal static class LoggingHash
    {
        // Converts an object to a normalized string that can be printed
        // takes System.Net.ObjectNamedFoo and coverts to ObjectNamedFoo, 
        // except IPAddress, IPEndPoint, and Uri, which return ToString().
        internal static string GetObjectName(object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            if (obj is Uri || obj is System.Net.IPAddress || obj is System.Net.IPEndPoint || obj is string)
            {
                return obj.ToString();
            }
            else
            {
                return obj.GetType().ToString();
            }
        }
        internal static int HashInt(object objectValue)
        {
            if (objectValue == null)
            {
                return 0;
            }
            else
            {
                return objectValue.GetHashCode();
            }
        }

        internal static string ObjectToString(object objectValue)
        {
            if (objectValue == null)
            {
                return "(null)";
            }
            else if (objectValue is string && ((string)objectValue).Length == 0)
            {
                return "(string.empty)";
            }
            else if (objectValue is Exception)
            {
                return ExceptionMessage(objectValue as Exception);
            }
            else if (objectValue is IntPtr)
            {
                return "0x" + ((IntPtr)objectValue).ToString("x");
            }
            else
            {
                return objectValue.ToString();
            }
        }

        private static string ExceptionMessage(Exception exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            if (exception.InnerException == null)
            {
                return exception.Message;
            }

            return exception.Message + " (" + ExceptionMessage(exception.InnerException) + ")";
        }

        internal static string HashString(object objectValue)
        {
            if (objectValue == null)
            {
                return "(null)";
            }
            else if (objectValue is string && ((string)objectValue).Length == 0)
            {
                return "(string.empty)";
            }
            else
            {
                return objectValue.GetHashCode().ToString(NumberFormatInfo.InvariantInfo);
            }
        }

        internal static object[] GetObjectLogHash(object obj)
        {
            object[] hashObject = new object[2];
            hashObject[0] = GetObjectName(obj);
            hashObject[1] = HashInt(obj);
            return hashObject;
        }
    }
}
