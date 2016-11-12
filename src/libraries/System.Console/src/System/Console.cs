// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    public static class Console
    {
        private const int DefaultConsoleBufferSize = 256; // default size of buffer used in stream readers/writers
        private static readonly object InternalSyncObject = new object(); // for synchronizing changing of Console's static fields
        private static TextReader s_in;
        private static TextWriter s_out, s_error;
        private static Encoding s_inputEncoding;
        private static Encoding s_outputEncoding;
        private static bool s_isOutTextWriterRedirected = false;
        private static bool s_isErrorTextWriterRedirected = false;

        private static ConsoleCancelEventHandler _cancelCallbacks;
        private static ConsolePal.ControlCHandlerRegistrar _registrar;

        internal static T EnsureInitialized<T>(ref T field, Func<T> initializer) where T : class
        {
            lock (InternalSyncObject)
            {
                T result = Volatile.Read(ref field);
                if (result == null)
                {
                    result = initializer();
                    Volatile.Write(ref field, result);
                }
                return result;
            }
        }

        public static TextReader In
        {
            get
            {
                return Volatile.Read(ref s_in) ?? EnsureInitialized(ref s_in, () => ConsolePal.GetOrCreateReader());
            }
        }

        public static Encoding InputEncoding
        {
            get
            {
                return Volatile.Read(ref s_inputEncoding) ?? EnsureInitialized(ref s_inputEncoding, () => ConsolePal.InputEncoding);
            }
            set
            {
                CheckNonNull(value, nameof(value));
                lock (InternalSyncObject)
                {
                    // Set the terminal console encoding.
                    ConsolePal.SetConsoleInputEncoding(value);

                    Volatile.Write(ref s_inputEncoding, (Encoding)value.Clone());

                    // We need to reinitialize Console.In in the next call to s_in
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
                return Volatile.Read(ref s_outputEncoding) ?? EnsureInitialized(ref s_outputEncoding, () => ConsolePal.OutputEncoding);
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

        public static TextWriter Out
        {
            get { return Volatile.Read(ref s_out) ?? EnsureInitialized(ref s_out, () => CreateOutputWriter(OpenStandardOutput())); }
        }

        public static TextWriter Error
        {
            get { return Volatile.Read(ref s_error) ?? EnsureInitialized(ref s_error, () => CreateOutputWriter(OpenStandardError())); }
        }

        private static TextWriter CreateOutputWriter(Stream outputStream)
        {
            return SyncTextWriter.GetSynchronizedTextWriter(outputStream == Stream.Null ?
                StreamWriter.Null :
                new StreamWriter(
                    stream: outputStream,
                    encoding: new ConsoleEncoding(OutputEncoding), // This ensures no prefix is written to the stream.
                    bufferSize: DefaultConsoleBufferSize,
                    leaveOpen: true) { AutoFlush = true });
        }

        private static StrongBox<bool> _isStdInRedirected;
        private static StrongBox<bool> _isStdOutRedirected;
        private static StrongBox<bool> _isStdErrRedirected;

        public static bool IsInputRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdInRedirected) ??
                    EnsureInitialized(ref _isStdInRedirected, () => new StrongBox<bool>(ConsolePal.IsInputRedirectedCore()));
                return redirected.Value;
            }
        }

        public static bool IsOutputRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdOutRedirected) ??
                    EnsureInitialized(ref _isStdOutRedirected, () => new StrongBox<bool>(ConsolePal.IsOutputRedirectedCore()));
                return redirected.Value;
            }
        }

        public static bool IsErrorRedirected
        {
            get
            {
                StrongBox<bool> redirected = Volatile.Read(ref _isStdErrRedirected) ??
                    EnsureInitialized(ref _isStdErrRedirected, () => new StrongBox<bool>(ConsolePal.IsErrorRedirectedCore()));
                return redirected.Value;
            }
        }

        public static int CursorSize
        {
            get { return ConsolePal.CursorSize; }
            set { ConsolePal.CursorSize = value; }
        }

        public static bool NumberLock { get { return ConsolePal.NumberLock; } }

        public static bool CapsLock { get { return ConsolePal.CapsLock; } }

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
            get
            {
                return ConsolePal.WindowWidth;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_NeedPosNum);
                }
                ConsolePal.WindowWidth = value;
            }
        }

        public static int WindowHeight
        {
            get
            {
                return ConsolePal.WindowHeight;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_NeedPosNum);
                }
                ConsolePal.WindowHeight = value;
            }
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

        private const int MaxConsoleTitleLength = 24500; // same value as in .NET Framework

        public static string Title
        {
            get { return ConsolePal.Title; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.Length > MaxConsoleTitleLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_ConsoleTitleTooLong);
                }

                ConsolePal.Title = value;
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
                    _cancelCallbacks += value;

                    // If we haven't registered our control-C handler, do it.
                    if (_registrar == null)
                    {
                        _registrar = new ConsolePal.ControlCHandlerRegistrar();
                        _registrar.Register();
                    }
                }
            }
            remove
            {
                lock (InternalSyncObject)
                {
                    _cancelCallbacks -= value;
                    if (_registrar != null && _cancelCallbacks == null)
                    {
                        _registrar.Unregister();
                        _registrar = null;
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
            newOut = SyncTextWriter.GetSynchronizedTextWriter(newOut);
            Volatile.Write(ref s_isOutTextWriterRedirected, true);

            lock (InternalSyncObject)
            {
                Volatile.Write(ref s_out, newOut);
            }
        }

        public static void SetError(TextWriter newError)
        {
            CheckNonNull(newError, nameof(newError));
            newError = SyncTextWriter.GetSynchronizedTextWriter(newError);
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
        public static String ReadLine()
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
        public static void WriteLine(Object value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String value)
        {
            Out.WriteLine(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String format, Object arg0)
        {
            Out.WriteLine(format, arg0);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String format, Object arg0, Object arg1)
        {
            Out.WriteLine(format, arg0, arg1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String format, Object arg0, Object arg1, Object arg2)
        {
            Out.WriteLine(format, arg0, arg1, arg2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(String format, params Object[] arg)
        {
            if (arg == null)                       // avoid ArgumentNullException from String.Format
                Out.WriteLine(format, null, null); // faster than Out.WriteLine(format, (Object)arg);
            else
                Out.WriteLine(format, arg);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String format, Object arg0)
        {
            Out.Write(format, arg0);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String format, Object arg0, Object arg1)
        {
            Out.Write(format, arg0, arg1);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String format, Object arg0, Object arg1, Object arg2)
        {
            Out.Write(format, arg0, arg1, arg2);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String format, params Object[] arg)
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
        public static void Write(Object value)
        {
            Out.Write(value);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void Write(String value)
        {
            Out.Write(value);
        }

        // TODO: Uncomment when ArgIterator and friends are available in System.Runtime.dll
        //
        //[CLSCompliant(false)]
        //[MethodImplAttribute(MethodImplOptions.NoInlining)]
        //public static void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, __arglist)
        //{
        //    Out.WriteLine(format, ToObjectArgs(arg0, arg1, arg2, arg3, new ArgIterator(__arglist));
        //}
        //
        //[CLSCompliant(false)]
        //[MethodImplAttribute(MethodImplOptions.NoInlining)]
        //public static void Write(string format, object arg0, object arg1, object arg2, object arg3, __arglist)
        //{
        //    Out.Write(format, ToObjectArgs(arg0, arg1, arg2, arg3, new ArgIterator(__arglist));
        //}
        //
        //private static object[] ToObjectArgs(object arg0, object arg1, object arg2, object arg3, ArgIterator args)
        //{
        //    var objArgs = new object[4 + args.GetRemainingCount()] { arg0, arg1, arg2, arg3 };
        //    for (int i = 4; i < objArgs.Length; i++)
        //    {
        //        objArgs[i] = TypedReference.ToObject(args.GetNextArg());
        //    }
        //    return objArgs;
        //}

        private sealed class ControlCDelegateData
        {
            private readonly ConsoleSpecialKey _controlKey;
            private readonly ConsoleCancelEventHandler _cancelCallbacks;

            internal bool Cancel;
            internal bool DelegateStarted;

            internal ControlCDelegateData(ConsoleSpecialKey controlKey, ConsoleCancelEventHandler cancelCallbacks)
            {
                _controlKey = controlKey;
                _cancelCallbacks = cancelCallbacks;
            }

            // This is the worker delegate that is called on the Threadpool thread to fire the actual events. It sets the DelegateStarted flag so
            // the thread that queued the work to the threadpool knows it has started (since it does not want to block indefinitely on the task
            // to start).
            internal void HandleBreakEvent()
            {
                DelegateStarted = true;
                var args = new ConsoleCancelEventArgs(_controlKey);
                _cancelCallbacks(null, args);
                Cancel = args.Cancel;
            }
        }

        internal static bool HandleBreakEvent(ConsoleSpecialKey controlKey)
        {
            // The thread that this gets called back on has a very small stack on some systems. There is
            // not enough space to handle a managed exception being caught and thrown. So, run a task
            // on the threadpool for the actual event callback.

            // To avoid the race condition between remove handler and raising the event
            ConsoleCancelEventHandler cancelCallbacks = Console._cancelCallbacks;
            if (cancelCallbacks == null)
            {
                return false;
            }

            var delegateData = new ControlCDelegateData(controlKey, cancelCallbacks);
            Task callBackTask = Task.Factory.StartNew(
                d => ((ControlCDelegateData)d).HandleBreakEvent(),
                delegateData,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

            // Block until the delegate is done. We need to be robust in the face of the task not executing
            // but we also want to get control back immediately after it is done and we don't want to give the
            // handler a fixed time limit in case it needs to display UI. Wait on the task twice, once with a
            // timeout and a second time without if we are sure that the handler actually started.
            TimeSpan controlCWaitTime = new TimeSpan(0, 0, 30); // 30 seconds
            callBackTask.Wait(controlCWaitTime);
            
            if (!delegateData.DelegateStarted)
            {
                Debug.Assert(false, "The task to execute the handler did not start within 30 seconds.");
                return false;
            }

            callBackTask.Wait();
            return delegateData.Cancel;
        }
    }
}
