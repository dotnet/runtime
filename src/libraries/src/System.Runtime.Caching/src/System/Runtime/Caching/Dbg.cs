// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Runtime.Caching
{
    internal static class Dbg
    {
#if DEBUG
        private static readonly bool s_tracingEnabled = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_RUNTIME_CACHING_TRACING") == "true";
#endif

        [Conditional("DEBUG")]
        internal static void Trace(string tagName, string message, Exception e = null)
        {
#if DEBUG
            string exceptionMessage =
                e is null ? null :
                e is ExternalException ee ? "Exception " + e + Environment.NewLine + "_hr=0x" + ee.ErrorCode.ToString("x", CultureInfo.InvariantCulture) :
                "Exception " + e;

            if (string.IsNullOrEmpty(message) & exceptionMessage != null)
            {
                message = exceptionMessage;
                exceptionMessage = null;
            }

            string output = string.Format(CultureInfo.InvariantCulture,
                 "[{0}] {1} {2}{3}{4}",
                 Thread.CurrentThread.ManagedThreadId,
                 tagName,
                 message,
                 Environment.NewLine,
                 exceptionMessage != null ? exceptionMessage + Environment.NewLine : "");
            Debug.WriteLine(output);
#endif
        }
    }
}
