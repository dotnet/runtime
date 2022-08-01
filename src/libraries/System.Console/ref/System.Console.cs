// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public static partial class Console
    {
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.ConsoleColor BackgroundColor { get { throw null; } set { } }
        public static int BufferHeight { [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")] get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public static int BufferWidth { [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")] get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool CapsLock { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static int CursorLeft { get { throw null; } set { } }
        public static int CursorSize { [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")] get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static int CursorTop { get { throw null; } set { } }
        public static bool CursorVisible { [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] get { throw null; } [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")] set { } }
        public static System.IO.TextWriter Error { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.ConsoleColor ForegroundColor { get { throw null; } set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.IO.TextReader In { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.Text.Encoding InputEncoding { get { throw null; } set { } }
        public static bool IsErrorRedirected { get { throw null; } }
        public static bool IsInputRedirected { get { throw null; } }
        public static bool IsOutputRedirected { get { throw null; } }
        public static bool KeyAvailable { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static int LargestWindowHeight { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static int LargestWindowWidth { get { throw null; } }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static bool NumberLock { get { throw null; } }
        public static System.IO.TextWriter Out { get { throw null; } }
        public static System.Text.Encoding OutputEncoding { get { throw null; } [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")] set { } }
        public static string Title { [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] get { throw null; } [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")] set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static bool TreatControlCAsInput { get { throw null; } set { } }
        public static int WindowHeight { [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")] get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public static int WindowLeft { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public static int WindowTop { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public static int WindowWidth { [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios"), System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")] get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static event System.ConsoleCancelEventHandler? CancelKeyPress { add { } remove { } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static void Beep() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void Beep(int frequency, int duration) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static void Clear() { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static (int Left, int Top) GetCursorPosition() { throw null; }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, System.ConsoleColor sourceForeColor, System.ConsoleColor sourceBackColor) { }
        public static System.IO.Stream OpenStandardError() { throw null; }
        public static System.IO.Stream OpenStandardError(int bufferSize) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.IO.Stream OpenStandardInput() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static System.IO.Stream OpenStandardInput(int bufferSize) { throw null; }
        public static System.IO.Stream OpenStandardOutput() { throw null; }
        public static System.IO.Stream OpenStandardOutput(int bufferSize) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static int Read() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.ConsoleKeyInfo ReadKey() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static System.ConsoleKeyInfo ReadKey(bool intercept) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static string? ReadLine() { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static void ResetColor() { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void SetBufferSize(int width, int height) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static void SetCursorPosition(int left, int top) { }
        public static void SetError(System.IO.TextWriter newError) { }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("android")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("ios")]
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("tvos")]
        public static void SetIn(System.IO.TextReader newIn) { }
        public static void SetOut(System.IO.TextWriter newOut) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void SetWindowPosition(int left, int top) { }
        [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")]
        public static void SetWindowSize(int width, int height) { }
        public static void Write(bool value) { }
        public static void Write(char value) { }
        public static void Write(char[]? buffer) { }
        public static void Write(char[] buffer, int index, int count) { }
        public static void Write(decimal value) { }
        public static void Write(double value) { }
        public static void Write(int value) { }
        public static void Write(long value) { }
        public static void Write(object? value) { }
        public static void Write(float value) { }
        public static void Write(string? value) { }
        public static void Write([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, object? arg0) { }
        public static void Write([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, object? arg0, object? arg1) { }
        public static void Write([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2) { }
        public static void Write([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, params object?[]? arg) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void Write(ulong value) { }
        public static void WriteLine() { }
        public static void WriteLine(bool value) { }
        public static void WriteLine(char value) { }
        public static void WriteLine(char[]? buffer) { }
        public static void WriteLine(char[] buffer, int index, int count) { }
        public static void WriteLine(decimal value) { }
        public static void WriteLine(double value) { }
        public static void WriteLine(int value) { }
        public static void WriteLine(long value) { }
        public static void WriteLine(object? value) { }
        public static void WriteLine(float value) { }
        public static void WriteLine(string? value) { }
        public static void WriteLine([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, object? arg0) { }
        public static void WriteLine([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, object? arg0, object? arg1) { }
        public static void WriteLine([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, object? arg0, object? arg1, object? arg2) { }
        public static void WriteLine([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, params object?[]? arg) { }
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
    public delegate void ConsoleCancelEventHandler(object? sender, System.ConsoleCancelEventArgs e);
    public enum ConsoleColor
    {
        Black = 0,
        DarkBlue = 1,
        DarkGreen = 2,
        DarkCyan = 3,
        DarkRed = 4,
        DarkMagenta = 5,
        DarkYellow = 6,
        Gray = 7,
        DarkGray = 8,
        Blue = 9,
        Green = 10,
        Cyan = 11,
        Red = 12,
        Magenta = 13,
        Yellow = 14,
        White = 15,
    }
    public enum ConsoleKey
    {
        Backspace = 8,
        Tab = 9,
        Clear = 12,
        Enter = 13,
        Pause = 19,
        Escape = 27,
        Spacebar = 32,
        PageUp = 33,
        PageDown = 34,
        End = 35,
        Home = 36,
        LeftArrow = 37,
        UpArrow = 38,
        RightArrow = 39,
        DownArrow = 40,
        Select = 41,
        Print = 42,
        Execute = 43,
        PrintScreen = 44,
        Insert = 45,
        Delete = 46,
        Help = 47,
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
        A = 65,
        B = 66,
        C = 67,
        D = 68,
        E = 69,
        F = 70,
        G = 71,
        H = 72,
        I = 73,
        J = 74,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        R = 82,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,
        LeftWindows = 91,
        RightWindows = 92,
        Applications = 93,
        Sleep = 95,
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
        Multiply = 106,
        Add = 107,
        Separator = 108,
        Subtract = 109,
        Decimal = 110,
        Divide = 111,
        F1 = 112,
        F2 = 113,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
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
        F20 = 131,
        F21 = 132,
        F22 = 133,
        F23 = 134,
        F24 = 135,
        BrowserBack = 166,
        BrowserForward = 167,
        BrowserRefresh = 168,
        BrowserStop = 169,
        BrowserSearch = 170,
        BrowserFavorites = 171,
        BrowserHome = 172,
        VolumeMute = 173,
        VolumeDown = 174,
        VolumeUp = 175,
        MediaNext = 176,
        MediaPrevious = 177,
        MediaStop = 178,
        MediaPlay = 179,
        LaunchMail = 180,
        LaunchMediaSelect = 181,
        LaunchApp1 = 182,
        LaunchApp2 = 183,
        Oem1 = 186,
        OemPlus = 187,
        OemComma = 188,
        OemMinus = 189,
        OemPeriod = 190,
        Oem2 = 191,
        Oem3 = 192,
        Oem4 = 219,
        Oem5 = 220,
        Oem6 = 221,
        Oem7 = 222,
        Oem8 = 223,
        Oem102 = 226,
        Process = 229,
        Packet = 231,
        Attention = 246,
        CrSel = 247,
        ExSel = 248,
        EraseEndOfFile = 249,
        Play = 250,
        Zoom = 251,
        NoName = 252,
        Pa1 = 253,
        OemClear = 254,
    }
    public readonly partial struct ConsoleKeyInfo : System.IEquatable<System.ConsoleKeyInfo>
    {
        private readonly int _dummyPrimitive;
        public ConsoleKeyInfo(char keyChar, System.ConsoleKey key, bool shift, bool alt, bool control) { throw null; }
        public System.ConsoleKey Key { get { throw null; } }
        public char KeyChar { get { throw null; } }
        public System.ConsoleModifiers Modifiers { get { throw null; } }
        public bool Equals(System.ConsoleKeyInfo obj) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ConsoleKeyInfo a, System.ConsoleKeyInfo b) { throw null; }
        public static bool operator !=(System.ConsoleKeyInfo a, System.ConsoleKeyInfo b) { throw null; }
    }
    [System.FlagsAttribute]
    public enum ConsoleModifiers
    {
        Alt = 1,
        Shift = 2,
        Control = 4,
    }
    public enum ConsoleSpecialKey
    {
        ControlC = 0,
        ControlBreak = 1,
    }
}
