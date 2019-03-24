// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;
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

        private static object InternalSyncObject = new object(); // for synchronizing changing of Console's static fields
        private static TextReader s_in;
        private static TextWriter s_out, s_error;
        private static Encoding s_inputEncoding;
        private static Encoding s_outputEncoding;
        private static bool s_isOutTextWriterRedirected = false;
        private static bool s_isErrorTextWriterRedirected = false;

        private static ConsoleCancelEventHandler s_cancelCallbacks;
        private static ConsolePal.ControlCHandlerRegistrar s_registrar;

        internal static T EnsureInitialized<T>(ref T field, Func<T> initializer) where T : class =>
            LazyInitializer.EnsureInitialized(ref field, ref InternalSyncObject, initializer);

        public static TextReader In => EnsureInitialized(ref s_in, () => ConsolePal.GetOrCreateReader());

        public static Encoding InputEncoding
        {
            get
            {
                return EnsureInitialized(ref s_inputEncoding, () => ConsolePal.InputEncoding);
            }
            set
            {
                CheckNonNull(value, nameof(value));
                lock (InternalSyncObject)
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
                return EnsureInitialized(ref s_outputEncoding, () => ConsolePal.OutputEncoding);
            }
            set
            {
                CheckNonNull(value, nameof(value));

                lock (InternalSyncObject)
                {
                    // Set the terminal console encoding.
                    ConsolePal.SetConsoleOutputEncoding(value);

                    // Before changing the code page we need to flush the data 
                    // if Out hasn't been redirected. Also, have the next call to  
                    // s_out reinitialize the console code page.
                    if (Volatile.Read(ref s_out) != null && !s_isOutTextWriterRedirected)
                    {
                        s_out.Flush();
                        Volatile.Write(ref s_out, null);
                    }
                    if (Volatile.Read(ref s_error) != null && !s_isErrorTextWriterRedirected)
                    {
                        s_error.Flush();
                        Volatile.Write(ref s_error, null);
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

        public static ConsoleKeyInfo ReadKey()
        {
            return ConsolePal.ReadKey(false);
        }

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            return ConsolePal.ReadKey(intercept);
        }

        public static TextWriter Out => EnsureInitialized(ref s_out, () => CreateOutputWriter(OpenStandardOutput()));

        public static TextWriter Error => EnsureInitialized(ref s_error, () => CreateOutputWriter(OpenStandardError()));

        private static TextWriter CreateOutputWriter(Stream outputStream)
        {
            return TextWriter.Synchronized(outputStream == Stream.Null ?
                StreamWriter.Null :
                new StreamWriter(
                    stream: outputStream,
                    encoding: OutputEncoding.RemovePreamble(), // This ensures no prefix is written to the stream.
                    bufferSize: WriteBufferSize,
                    leaveOpen: true) { AutoFlush = true });
        }

        private static StrongBox<bool> _isStdInRedirected;
        private static StrongBox<bool> _isStdOutRedirected;
        private static StrongBox<bool> _isStdErrRedirected;

        public static bool IsInputRedirected
        {
            get
            {
                StrongBox<bool> redirected = EnsureInitialized(ref _isStdInRedirected, () => new StrongBox<bool>(ConsolePal.IsInputRedirectedCore()));
                return redirected.Value;
            }
        }

        public static bool IsOutputRedirected
        {
            get
            {
                StrongBox<bool> redirected = EnsureInitialized(ref _isStdOutRedirected, () => new StrongBox<bool>(ConsolePal.IsOutputRedirectedCore()));
                return redirected.Value;
            }
        }

        public static bool IsErrorRedirected
        {
            get
            {
                StrongBox<bool> redirected = EnsureInitialized(ref _isStdErrRedirected, () => new StrongBox<bool>(ConsolePal.IsErrorRedirectedCore()));
                return redirected.Value;
            }
        }

        public static int CursorSize
        {
            get { return ConsolePal.CursorSize; }
            set { ConsolePal.CursorSize = value; }
        }

        public static bool NumberLock
        {
            get { return ConsolePal.NumberLock; }
        }

        public static bool CapsLock
        {
            get { return ConsolePal.CapsLock; }
        }

        internal const ConsoleColor UnknownColor = (ConsoleColor)(-1);

        public static ConsoleColor BackgroundColor
        {
            get { return ConsolePal.BackgroundColor; }
            set { ConsolePal.BackgroundColor = value; }
        }

        public static ConsoleColor ForegroundColor
        {
            get { return ConsolePal.ForegroundColor; }
            set { ConsolePal.ForegroundColor = value; }
        }

        public static void ResetColor()
        {
            ConsolePal.ResetColor();
        }

        public static int BufferWidth
        {
            get { return ConsolePal.BufferWidth; }
            set { ConsolePal.BufferWidth = value; }
        }

        public static int BufferHeight
        {
            get { return ConsolePal.BufferHeight; }
            set { ConsolePal.BufferHeight = value; }
        }

        public static void SetBufferSize(int width, int height)
        {
            ConsolePal.SetBufferSize(width, height);
        }

        public static int WindowLeft
        {
            get { return ConsolePal.WindowLeft; }
            set { ConsolePal.WindowLeft = value; }
        }

        public static int WindowTop
        {
            get { return ConsolePal.WindowTop; }
            set { ConsolePal.WindowTop = value; }
        }

        public static int WindowWidth
        {
            get { return ConsolePal.WindowWidth; }
            set { ConsolePal.WindowWidth = value; }
        }

        public static int WindowHeight
        {
            get { return ConsolePal.WindowHeight; }
            set { ConsolePal.WindowHeight = value; }
        }

        public static void SetWindowPosition(int left, int top)
        {
            ConsolePal.SetWindowPosition(left, top);
        }

        public static void SetWindowSize(int width, int height)
        {
            ConsolePal.SetWindowSize(width, height);
        }

        public static int LargestWindowWidth
        {
            get { return ConsolePal.LargestWindowWidth; }
        }

        public static int LargestWindowHeight
        {
            get { return ConsolePal.LargestWindowHeight; }
        }

        public static bool CursorVisible
        {
            get { return ConsolePal.CursorVisible; }
            set { ConsolePal.CursorVisible = value; }
        }

        public static int CursorLeft
        {
            get { return ConsolePal.CursorLeft; }
            set { SetCursorPosition(value, CursorTop); }
        }

        public static int CursorTop
        {
            get { return ConsolePal.CursorTop; }
            set { SetCursorPosition(CursorLeft, value); }
        }

        public static string Title
        {
            get { return ConsolePal.Title; }
            set
            {
                ConsolePal.Title = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public static void Beep()
        {
            ConsolePal.Beep();
        }

        public static void Beep(int frequency, int duration)
        {
            ConsolePal.Beep(frequency, duration);
        }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
        {
            ConsolePal.MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop, ' ', ConsoleColor.Black, BackgroundColor);
        }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
        {
            ConsolePal.MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop, sourceChar, sourceForeColor, sourceBackColor);
        }

        public static void Clear()
        {
            ConsolePal.Clear();
        }

        public static void SetCursorPosition(int left, int top)
        {
            // Basic argument validation.  The PAL implementation may provide further validation.
            if (left < 0 || left >= short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(left), left, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            if (top < 0 || top >= short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(top), top, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);

            ConsolePal.SetCursorPosition(left, top);
        }

        public static event ConsoleCancelEventHandler CancelKeyPress
        {
            add
            {
                lock (InternalSyncObject)
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
                lock (InternalSyncObject)
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

        public static bool TreatControlCAsInput
        {
            get { return ConsolePal.TreatControlCAsInput; }
            set { ConsolePal.TreatControlCAsInput = value; }
        }

        public static Stream OpenStandardInput()
        {
            return ConsolePal.OpenStandardInput();
        }

        public static Stream OpenStandardInput(int bufferSize)
        {
            // bufferSize is ignored, other than in argument validation, even in the .NET Framework
            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            return OpenStandardInput();
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
            return OpenStandardOutput();
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
            return OpenStandardError();
        }

        public static void SetIn(TextReader newIn)
        {
            CheckNonNull(newIn, nameof(newIn));
            newIn = SyncTextReader.GetSynchronizedTextReader(newIn);
            lock (InternalSyncObject)
            {
                Volatile.Write(ref s_in, newIn);
            }
        }

        public static void SetOut(TextWriter newOut)
        {
            CheckNonNull(newOut, nameof(newOut));
            newOut = TextWriter.Synchronized(newOut);
            Volatile.Write(ref s_isOutTextWriterRedirected, true);

            lock (InternalSyncObject)
            {
                Volatile.Write(ref s_out, newOut);
            }
        }

        public static void SetError(TextWriter newError)
        {
            CheckNonNull(newError, nameof(newError));
            newError = TextWriter.Synchronized(newError);
            Volatile.Write(ref s_isErrorTextWriterRedirected, true);

            lock (InternalSyncObject)
            {
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
        public static int Read()
        {
            return In.Read();
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static string ReadLine()
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
        public static void WriteLine(char[] buffer)
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
        public static void WriteLine(object value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string format, object arg0)
        {
            Out.WriteLine(format, arg0);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string format, object arg0, object arg1)
        {
            Out.WriteLine(format, arg0, arg1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            Out.WriteLine(format, arg0, arg1, arg2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string format, params object[] arg)
        {
            if (arg == null)                       // avoid ArgumentNullException from String.Format
                Out.WriteLine(format, null, null); // faster than Out.WriteLine(format, (Object)arg);
            else
                Out.WriteLine(format, arg);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string format, object arg0)
        {
            Out.Write(format, arg0);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string format, object arg0, object arg1)
        {
            Out.Write(format, arg0, arg1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string format, object arg0, object arg1, object arg2)
        {
            Out.Write(format, arg0, arg1, arg2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string format, params object[] arg)
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
        public static void Write(char[] buffer)
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
        public static void Write(object value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(string value)
        {
            Out.Write(value);
        }

        internal static bool HandleBreakEvent(ConsoleSpecialKey controlKey)
        {
            ConsoleCancelEventHandler handler = s_cancelCallbacks;
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
