// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace System
{
    public static class Console
    {
        // Unlike many other buffer sizes throughout .NET, which often only affect performance, this buffer size has a
        // functional impact on interactive console apps, where the size of the buffer passed to ReadFile/Console impacts
        // how many characters the cmd window will allow to be typed as part of a single line. It also does affect perf,
        // in particular when input is redirected and data may be consumed from a larger source. This 4K default size is the
        // same as is currently used by most other environments/languages tried.
        internal const int ReadBufferSize = 4096;
        // There's no visible functional impact to the write buffer size, and as we auto flush on every write,
        // there's little benefit to having a large buffer.  So we use a smaller buffer size to reduce working set.
        private const int WriteBufferSize = 256;

        private static readonly object s_syncObject = new object();
        private static TextReader? s_in;
        private static TextWriter? s_out, s_error;
        private static Encoding? s_inputEncoding;
        private static Encoding? s_outputEncoding;
        private static bool s_isOutTextWriterRedirected;
        private static bool s_isErrorTextWriterRedirected;

        private static ConsoleCancelEventHandler? s_cancelCallbacks;
        private static ConsolePal.ControlCHandlerRegistrar? s_registrar;

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static TextReader In
        {
            get
            {
                return Volatile.Read(ref s_in) ?? EnsureInitialized();

                static TextReader EnsureInitialized()
                {
                    // Must be placed outside s_syncObject lock. See Out getter.
                    ConsolePal.EnsureConsoleInitialized();

                    lock (s_syncObject) // Ensures In and InputEncoding are synchronized.
                    {
                        if (s_in == null)
                        {
                            Volatile.Write(ref s_in, ConsolePal.GetOrCreateReader());
                        }
                        return s_in;
                    }
                }
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static Encoding InputEncoding
        {
            get
            {
                Encoding? encoding = Volatile.Read(ref s_inputEncoding);
                if (encoding == null)
                {
                    lock (s_syncObject)
                    {
                        if (s_inputEncoding == null)
                        {
                            Volatile.Write(ref s_inputEncoding, ConsolePal.InputEncoding);
                        }
                        encoding = s_inputEncoding;
                    }
                }
                return encoding;
            }
            set
            {
                CheckNonNull(value, nameof(value));

                lock (s_syncObject)
                {
                    // Set the terminal console encoding.
                    ConsolePal.SetConsoleInputEncoding(value);

                    Volatile.Write(ref s_inputEncoding, (Encoding)value.Clone());

                    // We need to reinitialize 'Console.In' in the next call to s_in
                    // This will discard the current StreamReader, potentially
                    // losing buffered data.
                    Volatile.Write(ref s_in, null);
                }
            }
        }

        public static Encoding OutputEncoding
        {
            get
            {
                Encoding? encoding = Volatile.Read(ref s_outputEncoding);
                if (encoding == null)
                {
                    lock (s_syncObject)
                    {
                        if (s_outputEncoding == null)
                        {
                            Volatile.Write(ref s_outputEncoding, ConsolePal.OutputEncoding);
                        }
                        encoding = s_outputEncoding;
                    }
                }
                return encoding;
            }
            [UnsupportedOSPlatform("android")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            set
            {
                CheckNonNull(value, nameof(value));

                lock (s_syncObject)
                {
                    // Set the terminal console encoding.
                    ConsolePal.SetConsoleOutputEncoding(value);

                    // Before changing the code page we need to flush the data
                    // if Out hasn't been redirected. Also, have the next call to
                    // s_out reinitialize the console code page.
                    if (s_out != null && !s_isOutTextWriterRedirected)
                    {
                        s_out.Flush();
                        Volatile.Write(ref s_out, null!);
                    }
                    if (s_error != null && !s_isErrorTextWriterRedirected)
                    {
                        s_error.Flush();
                        Volatile.Write(ref s_error, null!);
                    }

                    Volatile.Write(ref s_outputEncoding, (Encoding)value.Clone());
                }
            }
        }

        public static bool KeyAvailable
        {
            get
            {
                if (IsInputRedirected)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ConsoleKeyAvailableOnFile);
                }

                return ConsolePal.KeyAvailable;
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static ConsoleKeyInfo ReadKey()
        {
            return ConsolePal.ReadKey(false);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            return ConsolePal.ReadKey(intercept);
        }

        public static TextWriter Out
        {
            get
            {
                // Console.Out shouldn't be locked while holding a lock on s_syncObject.
                // Otherwise there can be a deadlock when another thread locks these
                // objects in opposite order.
                //
                // Some functionality requires the console to be initialized.
                // On Linux, this initialization requires a lock on Console.Out.
                // The EnsureConsoleInitialized call must be placed outside the s_syncObject lock.
                Debug.Assert(!Monitor.IsEntered(s_syncObject));

                return Volatile.Read(ref s_out) ?? EnsureInitialized();

                static TextWriter EnsureInitialized()
                {
                    lock (s_syncObject) // Ensures Out and OutputEncoding are synchronized.
                    {
                        if (s_out == null)
                        {
                            Volatile.Write(ref s_out, CreateOutputWriter(ConsolePal.OpenStandardOutput()));
                        }
                        return s_out;
                    }
                }
            }
        }

        public static TextWriter Error
        {
            get
            {
                return Volatile.Read(ref s_error) ?? EnsureInitialized();

                static TextWriter EnsureInitialized()
                {
                    lock (s_syncObject) // Ensures Error and OutputEncoding are synchronized.
                    {
                        if (s_error == null)
                        {
                            Volatile.Write(ref s_error, CreateOutputWriter(ConsolePal.OpenStandardError()));
                        }
                        return s_error;
                    }
                }
            }
        }

        private static TextWriter CreateOutputWriter(Stream outputStream)
        {
            return TextWriter.Synchronized(outputStream == Stream.Null ?
                StreamWriter.Null :
                new StreamWriter(
                    stream: outputStream,
                    encoding: OutputEncoding.RemovePreamble(), // This ensures no prefix is written to the stream.
                    bufferSize: WriteBufferSize,
                    leaveOpen: true)
                {
                    AutoFlush = true
                });
        }

        private static StrongBox<bool>? _isStdInRedirected;
        private static StrongBox<bool>? _isStdOutRedirected;
        private static StrongBox<bool>? _isStdErrRedirected;

        public static bool IsInputRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdInRedirected) ?? EnsureInitialized();
                return redirected.Value;

                static StrongBox<bool> EnsureInitialized()
                {
                    Volatile.Write(ref _isStdInRedirected, new StrongBox<bool>(ConsolePal.IsInputRedirectedCore()));
                    return _isStdInRedirected;
                }
            }
        }

        public static bool IsOutputRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdOutRedirected) ?? EnsureInitialized();
                return redirected.Value;

                static StrongBox<bool> EnsureInitialized()
                {
                    Volatile.Write(ref _isStdOutRedirected, new StrongBox<bool>(ConsolePal.IsOutputRedirectedCore()));
                    return _isStdOutRedirected;
                }
            }
        }

        public static bool IsErrorRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdErrRedirected) ?? EnsureInitialized();
                return redirected.Value;

                static StrongBox<bool> EnsureInitialized()
                {
                    Volatile.Write(ref _isStdErrRedirected, new StrongBox<bool>(ConsolePal.IsErrorRedirectedCore()));
                    return _isStdErrRedirected;
                }
            }
        }

        public static int CursorSize
        {
            [UnsupportedOSPlatform("android")]
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            get { return ConsolePal.CursorSize; }
            [SupportedOSPlatform("windows")]
            set { ConsolePal.CursorSize = value; }
        }

        [SupportedOSPlatform("windows")]
        public static bool NumberLock
        {
            get { return ConsolePal.NumberLock; }
        }

        [SupportedOSPlatform("windows")]
        public static bool CapsLock
        {
            get { return ConsolePal.CapsLock; }
        }

        internal const ConsoleColor UnknownColor = (ConsoleColor)(-1);

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static ConsoleColor BackgroundColor
        {
            get { return ConsolePal.BackgroundColor; }
            set { ConsolePal.BackgroundColor = value; }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static ConsoleColor ForegroundColor
        {
            get { return ConsolePal.ForegroundColor; }
            set { ConsolePal.ForegroundColor = value; }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static void ResetColor()
        {
            ConsolePal.ResetColor();
        }

        public static int BufferWidth
        {
            [UnsupportedOSPlatform("android")]
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            get { return ConsolePal.BufferWidth; }
            [SupportedOSPlatform("windows")]
            set { ConsolePal.BufferWidth = value; }
        }

        public static int BufferHeight
        {
            [UnsupportedOSPlatform("android")]
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            get { return ConsolePal.BufferHeight; }
            [SupportedOSPlatform("windows")]
            set { ConsolePal.BufferHeight = value; }
        }

        [SupportedOSPlatform("windows")]
        public static void SetBufferSize(int width, int height)
        {
            ConsolePal.SetBufferSize(width, height);
        }

        public static int WindowLeft
        {
            get { return ConsolePal.WindowLeft; }
            [SupportedOSPlatform("windows")]
            set { ConsolePal.WindowLeft = value; }
        }

        public static int WindowTop
        {
            get { return ConsolePal.WindowTop; }
            [SupportedOSPlatform("windows")]
            set { ConsolePal.WindowTop = value; }
        }

        public static int WindowWidth
        {
            [UnsupportedOSPlatform("android")]
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            get { return ConsolePal.WindowWidth; }
            [SupportedOSPlatform("windows")]
            set { ConsolePal.WindowWidth = value; }
        }

        public static int WindowHeight
        {
            [UnsupportedOSPlatform("android")]
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            get { return ConsolePal.WindowHeight; }
            [SupportedOSPlatform("windows")]
            set { ConsolePal.WindowHeight = value; }
        }

        [SupportedOSPlatform("windows")]
        public static void SetWindowPosition(int left, int top)
        {
            ConsolePal.SetWindowPosition(left, top);
        }

        [SupportedOSPlatform("windows")]
        public static void SetWindowSize(int width, int height)
        {
            ConsolePal.SetWindowSize(width, height);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static int LargestWindowWidth
        {
            get { return ConsolePal.LargestWindowWidth; }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static int LargestWindowHeight
        {
            get { return ConsolePal.LargestWindowHeight; }
        }

        public static bool CursorVisible
        {
            [SupportedOSPlatform("windows")]
            get { return ConsolePal.CursorVisible; }
            [UnsupportedOSPlatform("android")]
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            set { ConsolePal.CursorVisible = value; }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static int CursorLeft
        {
            get { return ConsolePal.GetCursorPosition().Left; }
            set { SetCursorPosition(value, CursorTop); }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static int CursorTop
        {
            get { return ConsolePal.GetCursorPosition().Top; }
            set { SetCursorPosition(CursorLeft, value); }
        }

        /// <summary>Gets the position of the cursor.</summary>
        /// <returns>The column and row position of the cursor.</returns>
        /// <remarks>
        /// Columns are numbered from left to right starting at 0. Rows are numbered from top to bottom starting at 0.
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static (int Left, int Top) GetCursorPosition()
        {
            return ConsolePal.GetCursorPosition();
        }

        public static string Title
        {
            [SupportedOSPlatform("windows")]
            get { return ConsolePal.Title; }
            [UnsupportedOSPlatform("android")]
            [UnsupportedOSPlatform("browser")]
            [UnsupportedOSPlatform("ios")]
            [UnsupportedOSPlatform("tvos")]
            set
            {
                ConsolePal.Title = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static void Beep()
        {
            ConsolePal.Beep();
        }

        [SupportedOSPlatform("windows")]
        public static void Beep(int frequency, int duration)
        {
            ConsolePal.Beep(frequency, duration);
        }

        [SupportedOSPlatform("windows")]
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
        {
            ConsolePal.MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop, ' ', ConsoleColor.Black, BackgroundColor);
        }

        [SupportedOSPlatform("windows")]
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
        {
            ConsolePal.MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop, sourceChar, sourceForeColor, sourceBackColor);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static void Clear()
        {
            ConsolePal.Clear();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static void SetCursorPosition(int left, int top)
        {
            // Basic argument validation.  The PAL implementation may provide further validation.
            if (left < 0 || left >= short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(left), left, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            if (top < 0 || top >= short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(top), top, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);

            ConsolePal.SetCursorPosition(left, top);
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static event ConsoleCancelEventHandler? CancelKeyPress
        {
            add
            {
                // Must be placed outside s_syncObject lock. See Out getter.
                ConsolePal.EnsureConsoleInitialized();

                lock (s_syncObject)
                {
                    s_cancelCallbacks += value;

                    // If we haven't registered our control-C handler, do it.
                    if (s_registrar == null)
                    {
                        s_registrar = new ConsolePal.ControlCHandlerRegistrar();
                        s_registrar.Register();
                    }
                }
            }
            remove
            {
                lock (s_syncObject)
                {
                    s_cancelCallbacks -= value;
                    if (s_registrar != null && s_cancelCallbacks == null)
                    {
                        s_registrar.Unregister();
                        s_registrar = null;
                    }
                }
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static bool TreatControlCAsInput
        {
            get { return ConsolePal.TreatControlCAsInput; }
            set { ConsolePal.TreatControlCAsInput = value; }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static Stream OpenStandardInput()
        {
            return ConsolePal.OpenStandardInput();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        public static Stream OpenStandardInput(int bufferSize)
        {
            // bufferSize is ignored, other than in argument validation, even in the .NET Framework
            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            return ConsolePal.OpenStandardInput();
        }

        public static Stream OpenStandardOutput()
        {
            return ConsolePal.OpenStandardOutput();
        }

        public static Stream OpenStandardOutput(int bufferSize)
        {
            // bufferSize is ignored, other than in argument validation, even in the .NET Framework
            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            return ConsolePal.OpenStandardOutput();
        }

        public static Stream OpenStandardError()
        {
            return ConsolePal.OpenStandardError();
        }

        public static Stream OpenStandardError(int bufferSize)
        {
            // bufferSize is ignored, other than in argument validation, even in the .NET Framework
            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            return ConsolePal.OpenStandardError();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public static void SetIn(TextReader newIn)
        {
            CheckNonNull(newIn, nameof(newIn));
            newIn = SyncTextReader.GetSynchronizedTextReader(newIn);
            lock (s_syncObject)
            {
                Volatile.Write(ref s_in, newIn);
            }
        }

        public static void SetOut(TextWriter newOut)
        {
            CheckNonNull(newOut, nameof(newOut));
            newOut = TextWriter.Synchronized(newOut);
            lock (s_syncObject)
            {
                s_isOutTextWriterRedirected = true;
                Volatile.Write(ref s_out, newOut);
            }
        }

        public static void SetError(TextWriter newError)
        {
            CheckNonNull(newError, nameof(newError));
            newError = TextWriter.Synchronized(newError);
            lock (s_syncObject)
            {
                s_isErrorTextWriterRedirected = true;
                Volatile.Write(ref s_error, newError);
            }
        }

        private static void CheckNonNull(object obj, string paramName)
        {
            if (obj == null)
                throw new ArgumentNullException(paramName);
        }

        //
        // Give a hint to the code generator to not inline the common console methods. The console methods are
        // not performance critical. It is unnecessary code bloat to have them inlined.
        //
        // Moreover, simple repros for codegen bugs are often console-based. It is tedious to manually filter out
        // the inlined console writelines from them.
        //
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        public static int Read()
        {
            return In.Read();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        public static string? ReadLine()
        {
            return In.ReadLine();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine()
        {
            Out.WriteLine();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(bool value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(char value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(char[]? buffer)
        {
            Out.WriteLine(buffer);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(char[] buffer, int index, int count)
        {
            Out.WriteLine(buffer, index, count);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(decimal value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(double value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(float value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(int value)
        {
            Out.WriteLine(value);
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(uint value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(long value)
        {
            Out.WriteLine(value);
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(ulong value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(object? value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string? value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string format, object? arg0)
        {
            Out.WriteLine(format, arg0);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string format, object? arg0, object? arg1)
        {
            Out.WriteLine(format, arg0, arg1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string format, object? arg0, object? arg1, object? arg2)
        {
            Out.WriteLine(format, arg0, arg1, arg2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string format, params object?[]? arg)
        {
            if (arg == null)                       // avoid ArgumentNullException from String.Format
                Out.WriteLine(format, null, null); // faster than Out.WriteLine(format, (Object)arg);
            else
                Out.WriteLine(format, arg);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string format, object? arg0)
        {
            Out.Write(format, arg0);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string format, object? arg0, object? arg1)
        {
            Out.Write(format, arg0, arg1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string format, object? arg0, object? arg1, object? arg2)
        {
            Out.Write(format, arg0, arg1, arg2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string format, params object?[]? arg)
        {
            if (arg == null)                   // avoid ArgumentNullException from String.Format
                Out.Write(format, null, null); // faster than Out.Write(format, (Object)arg);
            else
                Out.Write(format, arg);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(bool value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(char value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(char[]? buffer)
        {
            Out.Write(buffer);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(char[] buffer, int index, int count)
        {
            Out.Write(buffer, index, count);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(double value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(decimal value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(float value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(int value)
        {
            Out.Write(value);
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(uint value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(long value)
        {
            Out.Write(value);
        }

        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(ulong value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(object? value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string? value)
        {
            Out.Write(value);
        }

        internal static bool HandleBreakEvent(ConsoleSpecialKey controlKey)
        {
            ConsoleCancelEventHandler? handler = s_cancelCallbacks;
            if (handler == null)
            {
                return false;
            }

            var args = new ConsoleCancelEventArgs(controlKey);
            handler(null, args);
            return args.Cancel;
        }
    }
}
