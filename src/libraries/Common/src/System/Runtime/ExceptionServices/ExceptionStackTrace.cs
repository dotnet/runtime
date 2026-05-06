// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace System.Runtime.ExceptionServices
{
    internal static class ExceptionStackTrace
    {
        /// <summary>In debug builds, appends the current stack trace to the exception.</summary>
        [Conditional("DEBUG")]
        public static void AddCurrentStack(Exception exception)
        {
            Debug.Assert(exception != null, "Expected non-null Exception");

            const string ExceptionRemoteStackTraceStringName = "_remoteStackTraceString";
            FieldInfo? fi = typeof(Exception).GetField(ExceptionRemoteStackTraceStringName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (fi != null)
            {
                string text =
                    (string?)fi.GetValue(exception) +
                    Environment.StackTrace + Environment.NewLine +
                    "--- End of stack trace from AddCurrentStack ---" + Environment.NewLine;
                fi.SetValue(exception, text);
            }
        }
    }
}
