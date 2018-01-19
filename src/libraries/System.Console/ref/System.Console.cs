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
        public static System.ConsoleColor BackgroundColor { get { throw null; } set { } }
        public static int BufferHeight { get { throw null; } set { } }
        public static int BufferWidth { get { throw null; } set { } }
        public static bool CapsLock { get { throw null; } }
        public static int CursorLeft { get { throw null; } set { } }
        public static int CursorSize { get { throw null; } set { } }
        public static int CursorTop { get { throw null; } set { } }
        public static bool CursorVisible { get { throw null; } set { } }
        public static System.IO.TextWriter Error { get { throw null; } }
        public static System.ConsoleColor ForegroundColor { get { throw null; } set { } }
        public static System.IO.TextReader In { get { throw null; } }
        public static System.Text.Encoding InputEncoding { get { throw null; } set { } }
        public static bool IsErrorRedirected { get { throw null; } }
        public static bool IsInputRedirected { get { throw null; } }
        public static bool IsOutputRedirected { get { throw null; } }
        public static bool KeyAvailable { get { throw null; } }
        public static int LargestWindowHeight { get { throw null; } }
        public static int LargestWindowWidth { get { throw null; } }
        public static bool NumberLock { get { throw null; } }
        public static System.IO.TextWriter Out { get { throw null; } }
        public static System.Text.Encoding OutputEncoding { get { throw null; } set { } }
        public static string Title { get { throw null; } set { } }
        public static bool TreatControlCAsInput { get { throw null; } set { } }
        public static int WindowHeight { get { throw null; } set { } }
        public static int WindowLeft { get { throw null; } set { } }
        public static int WindowTop { get { throw null; } set { } }
        public static int WindowWidth { get { throw null; } set { } }
        public static event System.ConsoleCancelEventHandler CancelKeyPress { add { } remove { } }
        public static void Beep() { }
        public static void Beep(int frequency, int duration) { }
        public static void Clear() { }
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop) { }
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, System.ConsoleColor sourceForeColor, System.ConsoleColor sourceBackColor) { }
        public static System.IO.Stream OpenStandardError() { throw null; }
        public static System.IO.Stream OpenStandardError(int bufferSize) { throw null; }
        public static System.IO.Stream OpenStandardInput() { throw null; }
        public static System.IO.Stream OpenStandardInput(int bufferSize) { throw null; }
        public static System.IO.Stream OpenStandardOutput() { throw null; }
        public static System.IO.Stream OpenStandardOutput(int bufferSize) { throw null; }
        public static int Read() { throw null; }
        public static System.ConsoleKeyInfo ReadKey() { throw null; }
        public static System.ConsoleKeyInfo ReadKey(bool intercept) { throw null; }
        public static string ReadLine() { throw null; }
        public static void ResetColor() { }
        public static void SetBufferSize(int width, int height) { }
        public static void SetCursorPosition(int left, int top) { }
        public static void SetError(System.IO.TextWriter newError) { }
        public static void SetIn(System.IO.TextReader newIn) { }
        public static void SetOut(System.IO.TextWriter newOut) { }
        public static void SetWindowPosition(int left, int top) { }
        public static void SetWindowSize(int width, int height) { }
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
        public bool Cancel { get { throw null; } set { } }
        public System.ConsoleSpecialKey SpecialKey { get { throw null; } }
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
    public enum ConsoleKey
    {
        A = 65,
        Add = 107,
        Applications = 93,
        Attention = 246,
        B = 66,
        Backspace = 8,
        BrowserBack = 166,
        BrowserFavorites = 171,
        BrowserForward = 167,
        BrowserHome = 172,
        BrowserRefresh = 168,
        BrowserSearch = 170,
        BrowserStop = 169,
        C = 67,
        Clear = 12,
        CrSel = 247,
        D = 68,
        D0 = 48,
        D1 = 49,
        D2 = 50,
        D3 = 51,
        D4 = 52,
        D5 = 53,
        D6 = 54,
        D7 = 55,
        D8 = 56,
        D9 = 57,
        Decimal = 110,
        Delete = 46,
        Divide = 111,
        DownArrow = 40,
        E = 69,
        End = 35,
        Enter = 13,
        EraseEndOfFile = 249,
        Escape = 27,
        Execute = 43,
        ExSel = 248,
        F = 70,
        F1 = 112,
        F10 = 121,
        F11 = 122,
        F12 = 123,
        F13 = 124,
        F14 = 125,
        F15 = 126,
        F16 = 127,
        F17 = 128,
        F18 = 129,
        F19 = 130,
        F2 = 113,
        F20 = 131,
        F21 = 132,
        F22 = 133,
        F23 = 134,
        F24 = 135,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        G = 71,
        H = 72,
        Help = 47,
        Home = 36,
        I = 73,
        Insert = 45,
        J = 74,
        K = 75,
        L = 76,
        LaunchApp1 = 182,
        LaunchApp2 = 183,
        LaunchMail = 180,
        LaunchMediaSelect = 181,
        LeftArrow = 37,
        LeftWindows = 91,
        M = 77,
        MediaNext = 176,
        MediaPlay = 179,
        MediaPrevious = 177,
        MediaStop = 178,
        Multiply = 106,
        N = 78,
        NoName = 252,
        NumPad0 = 96,
        NumPad1 = 97,
        NumPad2 = 98,
        NumPad3 = 99,
        NumPad4 = 100,
        NumPad5 = 101,
        NumPad6 = 102,
        NumPad7 = 103,
        NumPad8 = 104,
        NumPad9 = 105,
        O = 79,
        Oem1 = 186,
        Oem102 = 226,
        Oem2 = 191,
        Oem3 = 192,
        Oem4 = 219,
        Oem5 = 220,
        Oem6 = 221,
        Oem7 = 222,
        Oem8 = 223,
        OemClear = 254,
        OemComma = 188,
        OemMinus = 189,
        OemPeriod = 190,
        OemPlus = 187,
        P = 80,
        Pa1 = 253,
        Packet = 231,
        PageDown = 34,
        PageUp = 33,
        Pause = 19,
        Play = 250,
        Print = 42,
        PrintScreen = 44,
        Process = 229,
        Q = 81,
        R = 82,
        RightArrow = 39,
        RightWindows = 92,
        S = 83,
        Select = 41,
        Separator = 108,
        Sleep = 95,
        Spacebar = 32,
        Subtract = 109,
        T = 84,
        Tab = 9,
        U = 85,
        UpArrow = 38,
        V = 86,
        VolumeDown = 174,
        VolumeMute = 173,
        VolumeUp = 175,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,
        Zoom = 251,
    }
    public readonly partial struct ConsoleKeyInfo
    {
        private readonly int _dummy;
        public ConsoleKeyInfo(char keyChar, System.ConsoleKey key, bool shift, bool alt, bool control) { throw null; }
        public System.ConsoleKey Key { get { throw null; } }
        public char KeyChar { get { throw null; } }
        public System.ConsoleModifiers Modifiers { get { throw null; } }
        public bool Equals(System.ConsoleKeyInfo obj) { throw null; }
        public override bool Equals(object value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ConsoleKeyInfo a, System.ConsoleKeyInfo b) { throw null; }
        public static bool operator !=(System.ConsoleKeyInfo a, System.ConsoleKeyInfo b) { throw null; }
    }
    [System.FlagsAttribute]
    public enum ConsoleModifiers
    {
        Alt = 1,
        Control = 4,
        Shift = 2,
    }
    public enum ConsoleSpecialKey
    {
        ControlBreak = 1,
        ControlC = 0,
    }
}
