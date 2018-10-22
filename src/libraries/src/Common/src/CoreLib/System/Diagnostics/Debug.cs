// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Do not remove this, it is needed to retain calls to these conditional methods in release builds
#define DEBUG
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// Provides a set of properties and methods for debugging code.
    /// </summary>
    public static partial class Debug
    {
        private static DebugProvider s_provider = new DebugProvider();

        public static DebugProvider SetProvider(DebugProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return Interlocked.Exchange(ref s_provider, provider);
        }

        public static bool AutoFlush { get { return true; } set { } }

        public static int IndentLevel
        {
            get
            {
                return DebugProvider.IndentLevel;
            }
            set
            {
                DebugProvider.IndentLevel = value;
            }
        }

        public static int IndentSize
        {
            get
            {
                return DebugProvider.IndentSize;
            }
            set
            {
                DebugProvider.IndentSize = value;
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Close() { }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Flush() { }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Indent()
        {
            IndentLevel++;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Unindent()
        {
            IndentLevel--;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Print(string message)
        {
            Write(message);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Print(string format, params object[] args)
        {
            Write(string.Format(null, format, args));
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            Assert(condition, string.Empty, string.Empty);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            Assert(condition, message, string.Empty);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert(bool condition, string message, string detailMessage)
        {
            if (!condition)
            {
                string stackTrace;
                try
                {
                    stackTrace = new StackTrace(0, true).ToString(System.Diagnostics.StackTrace.TraceFormat.Normal);
                }
                catch
                {
                    stackTrace = "";
                }
                WriteLine(FormatAssert(stackTrace, message, detailMessage));
                s_provider.ShowDialog(stackTrace, message, detailMessage, "Assertion Failed");
            }
        }

        internal static void ContractFailure(bool condition, string message, string detailMessage, string failureKindMessage)
        {
            if (!condition)
            {
                string stackTrace;
                try
                {
                    stackTrace = new StackTrace(2, true).ToString(System.Diagnostics.StackTrace.TraceFormat.Normal);
                }
                catch
                {
                    stackTrace = "";
                }
                WriteLine(FormatAssert(stackTrace, message, detailMessage));
                s_provider.ShowDialog(stackTrace, message, detailMessage, SR.GetResourceString(failureKindMessage));
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Fail(string message)
        {
            Assert(false, message, string.Empty);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Fail(string message, string detailMessage)
        {
            Assert(false, message, detailMessage);
        }

        private static string FormatAssert(string stackTrace, string message, string detailMessage)
        {
            string newLine = DebugProvider.GetIndentString() + Environment.NewLine;
            return SR.DebugAssertBanner + newLine
                   + SR.DebugAssertShortMessage + newLine
                   + message + newLine
                   + SR.DebugAssertLongMessage + newLine
                   + detailMessage + newLine
                   + stackTrace;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert(bool condition, string message, string detailMessageFormat, params object[] args)
        {
            Assert(condition, message, string.Format(detailMessageFormat, args));
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Write(string message)
        {
            s_provider.Write(message);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(object value)
        {
            WriteLine(value?.ToString());
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(object value, string category)
        {
            WriteLine(value?.ToString(), category);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(null, format, args));
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(string message, string category)
        {
            if (category == null)
            {
                WriteLine(message);
            }
            else
            {
                WriteLine(category + ":" + message);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Write(object value)
        {
            Write(value?.ToString());
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Write(string message, string category)
        {
            if (category == null)
            {
                Write(message);
            }
            else
            {
                Write(category + ":" + message);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Write(object value, string category)
        {
            Write(value?.ToString(), category);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteIf(bool condition, string message)
        {
            if (condition)
            {
                Write(message);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteIf(bool condition, object value)
        {
            if (condition)
            {
                Write(value);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteIf(bool condition, string message, string category)
        {
            if (condition)
            {
                Write(message, category);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteIf(bool condition, object value, string category)
        {
            if (condition)
            {
                Write(value, category);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, object value)
        {
            if (condition)
            {
                WriteLine(value);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, object value, string category)
        {
            if (condition)
            {
                WriteLine(value, category);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, string message)
        {
            if (condition)
            {
                WriteLine(message);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, string message, string category)
        {
            if (condition)
            {
                WriteLine(message, category);
            }
        }
    }
}
