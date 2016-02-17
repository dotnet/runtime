// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    public static partial class Console
    {
        public static System.ConsoleColor BackgroundColor { get { return default(System.ConsoleColor); } set { } }
        public static void Beep() { }
        public static void Beep(int frequency, int duration) { }
        public static int BufferHeight { get { return default(int); } set { } }
        public static int BufferWidth { get { return default(int); } set { } }
        public static event System.ConsoleCancelEventHandler CancelKeyPress { add { } remove { } }
        public static void Clear() { }
        public static int CursorLeft { get { return default(int); } set { } }
        public static int CursorSize { get { return default(int); } set { } }
        public static int CursorTop { get { return default(int); } set { } }
        public static bool CursorVisible { get { return default(bool); } set { } }
        public static System.IO.TextWriter Error { get { return default(System.IO.TextWriter); } }
        public static System.ConsoleColor ForegroundColor { get { return default(System.ConsoleColor); } set { } }
        public static bool IsErrorRedirected { get { return false; } }
        public static bool IsInputRedirected { get { return false; } }
        public static bool IsOutputRedirected { get { return false; } }
        public static System.IO.TextReader In { get { return default(System.IO.TextReader); } }
        public static bool KeyAvailable { get { return default(bool); }}
        public static int LargestWindowWidth { get { return default(int); } }
        public static int LargestWindowHeight { get { return default(int); }}
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop) { }
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor) { }
        public static System.IO.Stream OpenStandardError() { return default(System.IO.Stream); }
        public static System.IO.Stream OpenStandardInput() { return default(System.IO.Stream); }
        public static System.IO.Stream OpenStandardOutput() { return default(System.IO.Stream); }
        public static System.IO.TextWriter Out { get { return default(System.IO.TextWriter); } }
        public static int Read() { return default(int); }
        public static ConsoleKeyInfo ReadKey() { return default(ConsoleKeyInfo); }
        public static ConsoleKeyInfo ReadKey(bool intercept) { return default(ConsoleKeyInfo); }
        public static string ReadLine() { return default(string); }
        public static void ResetColor() { }
        public static void SetBufferSize(int width, int height) { }
        public static void SetCursorPosition(int left, int top) { }
        public static void SetError(System.IO.TextWriter newError) { }
        public static void SetIn(System.IO.TextReader newIn) { }
        public static void SetOut(System.IO.TextWriter newOut) { }
        public static void SetWindowPosition(int left, int top) { }
        public static void SetWindowSize(int width, int height) { }
        public static string Title { get { return default(string); } set { } }
        public static int WindowHeight { get { return default(int); } set { } }
        public static int WindowWidth { get { return default(int); } set { } }
        public static int WindowLeft { get { return default(int); } set { } }
        public static int WindowTop { get { return default(int); } set { } }
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
    public partial struct ConsoleKeyInfo
    {
        public ConsoleKeyInfo(char keyChar, ConsoleKey key, bool shift, bool alt, bool control) { }
        public char KeyChar { get { return default(char); } }
        public ConsoleKey Key { get { return default(ConsoleKey); } }
        public ConsoleModifiers Modifiers { get { return default(ConsoleModifiers); ; } }
    }
    public enum ConsoleKey
    {
        Backspace = 0x8,
        Tab = 0x9,
        Clear = 0xC,
        Enter = 0xD,
        Pause = 0x13,
        Escape = 0x1B,
        Spacebar = 0x20,
        PageUp = 0x21,
        PageDown = 0x22,
        End = 0x23,
        Home = 0x24,
        LeftArrow = 0x25,
        UpArrow = 0x26,
        RightArrow = 0x27,
        DownArrow = 0x28,
        Select = 0x29,
        Print = 0x2A,
        Execute = 0x2B,
        PrintScreen = 0x2C,
        Insert = 0x2D,
        Delete = 0x2E,
        Help = 0x2F,
        D0 = 0x30,  // 0 through 9
        D1 = 0x31,
        D2 = 0x32,
        D3 = 0x33,
        D4 = 0x34,
        D5 = 0x35,
        D6 = 0x36,
        D7 = 0x37,
        D8 = 0x38,
        D9 = 0x39,
        A = 0x41,
        B = 0x42,
        C = 0x43,
        D = 0x44,
        E = 0x45,
        F = 0x46,
        G = 0x47,
        H = 0x48,
        I = 0x49,
        J = 0x4A,
        K = 0x4B,
        L = 0x4C,
        M = 0x4D,
        N = 0x4E,
        O = 0x4F,
        P = 0x50,
        Q = 0x51,
        R = 0x52,
        S = 0x53,
        T = 0x54,
        U = 0x55,
        V = 0x56,
        W = 0x57,
        X = 0x58,
        Y = 0x59,
        Z = 0x5A,
        Sleep = 0x5F,
        NumPad0 = 0x60,
        NumPad1 = 0x61,
        NumPad2 = 0x62,
        NumPad3 = 0x63,
        NumPad4 = 0x64,
        NumPad5 = 0x65,
        NumPad6 = 0x66,
        NumPad7 = 0x67,
        NumPad8 = 0x68,
        NumPad9 = 0x69,
        Multiply = 0x6A,
        Add = 0x6B,
        Separator = 0x6C,
        Subtract = 0x6D,
        Decimal = 0x6E,
        Divide = 0x6F,
        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,
        F13 = 0x7C,
        F14 = 0x7D,
        F15 = 0x7E,
        F16 = 0x7F,
        F17 = 0x80,
        F18 = 0x81,
        F19 = 0x82,
        F20 = 0x83,
        F21 = 0x84,
        F22 = 0x85,
        F23 = 0x86,
        F24 = 0x87,
        Oem1 = 0xBA,
        OemPlus = 0xBB,
        OemComma = 0xBC,
        OemMinus = 0xBD,
        OemPeriod = 0xBE,
        Oem2 = 0xBF,
        Oem3 = 0xC0,
        Oem4 = 0xDB,
        Oem5 = 0xDC,
        Oem6 = 0xDD,
        Oem7 = 0xDE,
        Oem8 = 0xDF,
        OemClear = 0xFE,
    }
    [Flags]
    public enum ConsoleModifiers
    {
        Alt = 1,
        Shift = 2,
        Control = 4
    }
    public enum ConsoleSpecialKey
    {
        ControlBreak = 1,
        ControlC = 0,
    }
}
