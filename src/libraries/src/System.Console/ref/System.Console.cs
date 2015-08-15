// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    public static partial class Console
    {
        public static System.ConsoleColor BackgroundColor { get { return default(System.ConsoleColor); } set { } }
        public static System.IO.TextWriter Error { get { return default(System.IO.TextWriter); } }
        public static System.ConsoleColor ForegroundColor { get { return default(System.ConsoleColor); } set { } }
        public static System.IO.TextReader In { get { return default(System.IO.TextReader); } }
        public static System.IO.TextWriter Out { get { return default(System.IO.TextWriter); } }
        public static event System.ConsoleCancelEventHandler CancelKeyPress { add { } remove { } }
        public static System.IO.Stream OpenStandardError() { return default(System.IO.Stream); }
        public static System.IO.Stream OpenStandardInput() { return default(System.IO.Stream); }
        public static System.IO.Stream OpenStandardOutput() { return default(System.IO.Stream); }
        public static int Read() { return default(int); }
        public static string ReadLine() { return default(string); }
        public static void ResetColor() { }
        public static void SetError(System.IO.TextWriter newError) { }
        public static void SetIn(System.IO.TextReader newIn) { }
        public static void SetOut(System.IO.TextWriter newOut) { }
        public static void Write(bool value) { }
        public static void Write(char value) { }
        public static void Write(char[] buffer) { }
        public static void Write(char[] buffer, int index, int count) { }
        public static void Write(decimal value) { }
        public static void Write(double value) { }
        public static void Write(int value) { }
        public static void Write(long value) { }
        public static void Write(object value) { }
        public static void Write(float value) { }
        public static void Write(string value) { }
        public static void Write(string format, object arg0) { }
        public static void Write(string format, object arg0, object arg1) { }
        public static void Write(string format, object arg0, object arg1, object arg2) { }
        public static void Write(string format, params object[] arg) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ulong value) { }
        public static void WriteLine() { }
        public static void WriteLine(bool value) { }
        public static void WriteLine(char value) { }
        public static void WriteLine(char[] buffer) { }
        public static void WriteLine(char[] buffer, int index, int count) { }
        public static void WriteLine(decimal value) { }
        public static void WriteLine(double value) { }
        public static void WriteLine(int value) { }
        public static void WriteLine(long value) { }
        public static void WriteLine(object value) { }
        public static void WriteLine(float value) { }
        public static void WriteLine(string value) { }
        public static void WriteLine(string format, object arg0) { }
        public static void WriteLine(string format, object arg0, object arg1) { }
        public static void WriteLine(string format, object arg0, object arg1, object arg2) { }
        public static void WriteLine(string format, params object[] arg) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteLine(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteLine(ulong value) { }
    }
    public sealed partial class ConsoleCancelEventArgs : System.EventArgs
    {
        internal ConsoleCancelEventArgs() { }
        public bool Cancel { get { return default(bool); } set { } }
        public System.ConsoleSpecialKey SpecialKey { get { return default(System.ConsoleSpecialKey); } }
    }
    public delegate void ConsoleCancelEventHandler(object sender, System.ConsoleCancelEventArgs e);
    public enum ConsoleColor
    {
        Black = 0,
        Blue = 9,
        Cyan = 11,
        DarkBlue = 1,
        DarkCyan = 3,
        DarkGray = 8,
        DarkGreen = 2,
        DarkMagenta = 5,
        DarkRed = 4,
        DarkYellow = 6,
        Gray = 7,
        Green = 10,
        Magenta = 13,
        Red = 12,
        White = 15,
        Yellow = 14,
    }
    public enum ConsoleSpecialKey
    {
        ControlBreak = 1,
        ControlC = 0,
    }
}
