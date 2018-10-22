// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Do not remove this, it is needed to retain calls to these conditional methods in release builds
#define DEBUG

namespace System.Diagnostics
{
    /// <summary>
    /// Provides default implementation for Write and ShowDialog methods in Debug class.
    /// </summary>
    public partial class DebugProvider
    {
        public virtual void Write(string message)
        {
            lock (s_lock)
            {
                if (message == null)
                {
                    s_WriteCore(string.Empty);
                    return;
                }
                if (s_needIndent)
                {
                    message = GetIndentString() + message;
                    s_needIndent = false;
                }
                s_WriteCore(message);
                if (message.EndsWith(Environment.NewLine))
                {
                    s_needIndent = true;
                }
            }
        }

        private static readonly object s_lock = new object();

        private sealed class DebugAssertException : Exception
        {
            internal DebugAssertException(string stackTrace) :
                base(Environment.NewLine + stackTrace)
            {
            }

            internal DebugAssertException(string message, string stackTrace) :
                base(message + Environment.NewLine + Environment.NewLine + stackTrace)
            {
            }

            internal DebugAssertException(string message, string detailMessage, string stackTrace) :
                base(message + Environment.NewLine + detailMessage + Environment.NewLine + Environment.NewLine + stackTrace)
            {
            }
        }

        [ThreadStatic]
        private static int s_indentLevel;
        internal static int IndentLevel
        {
            get
            {
                return s_indentLevel;
            }
            set
            {
                s_indentLevel = value < 0 ? 0 : value;
            }
        }

        private static int s_indentSize = 4;
        internal static int IndentSize
        {
            get
            {
                return s_indentSize;
            }
            set
            {
                s_indentSize = value < 0 ? 0 : value;
            }
        }

        private static bool s_needIndent;

        private static string s_indentString;

        internal static string GetIndentString()
        {
            int indentCount = IndentSize * IndentLevel;
            if (s_indentString?.Length == indentCount)
            {
                return s_indentString;
            }
            return s_indentString = new string(' ', indentCount);
        }

        // internal and not readonly so that the tests can swap this out.
        internal static Action<string> s_WriteCore = WriteCore;
    }
}
