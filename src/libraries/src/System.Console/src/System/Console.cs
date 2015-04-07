// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    public static class Console
    {
        private const int DefaultConsoleBufferSize = 256; // default size of buffer used in stream readers/writers
        private static readonly object InternalSyncObject = new object(); // for synchronizing changing of Console's static fields
        private static TextReader _in;
        private static TextWriter _out, _error;

        private static ConsoleCancelEventHandler _cancelCallbacks;
        private static ConsolePal.ControlCHandlerRegistrar _registrar;

        private static T EnsureInitialized<T>(ref T field, Func<T> initializer) where T : class
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
                return Volatile.Read(ref _in) ?? EnsureInitialized(ref _in, () =>
                {
                    Stream inputStream = OpenStandardInput();
                    return SyncTextReader.GetSynchronizedTextReader(inputStream == Stream.Null ?
                        StreamReader.Null :
                        new StreamReader(
                            stream: inputStream,
                            encoding: ConsolePal.InputEncoding,
                            detectEncodingFromByteOrderMarks: false,
                            bufferSize: DefaultConsoleBufferSize,
                            leaveOpen: true));
                });
            }
        }

        public static TextWriter Out
        {
            get { return Volatile.Read(ref _out) ?? EnsureInitialized(ref _out, () => CreateOutputWriter(OpenStandardOutput())); }
        }

        public static TextWriter Error
        {
            get { return Volatile.Read(ref _error) ?? EnsureInitialized(ref _error, () => CreateOutputWriter(OpenStandardError())); }
        }

        private static TextWriter CreateOutputWriter(Stream outputStream)
        {
            return SyncTextWriter.GetSynchronizedTextWriter(outputStream == Stream.Null ?
                StreamWriter.Null :
                new StreamWriter(
                    stream: outputStream,
                    encoding: ConsolePal.OutputEncoding,
                    bufferSize: DefaultConsoleBufferSize,
                    leaveOpen: true) { AutoFlush = true });
        }

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

        // This is the worker delegate that is called on the Threadpool thread to fire the actual events. It must guarantee that it
        // signals the caller on the ControlC thread so that it does not block indefinitely.
        private static void ControlCDelegate(object data)
        {
            ControlCDelegateData controlCData = (ControlCDelegateData)data;
            try
            {
                controlCData.DelegateStarted = true;
                ConsoleCancelEventArgs args = new ConsoleCancelEventArgs(controlCData.ControlKey);
                controlCData.CancelCallbacks(null, args);
                controlCData.Cancel = args.Cancel;
            }
            finally
            {
                controlCData.CompletionEvent.Set();
            }
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

        public static Stream OpenStandardInput()
        {
            return ConsolePal.OpenStandardInput();
        }

        public static Stream OpenStandardOutput()
        {
            return ConsolePal.OpenStandardOutput();
        }

        public static Stream OpenStandardError()
        {
            return ConsolePal.OpenStandardError();
        }

        public static void SetIn(TextReader newIn)
        {
            CheckNonNull(newIn, "newIn");
            newIn = SyncTextReader.GetSynchronizedTextReader(newIn);
            lock (InternalSyncObject) { _in = newIn; }
        }

        public static void SetOut(TextWriter newOut)
        {
            CheckNonNull(newOut, "newOut");
            newOut = SyncTextWriter.GetSynchronizedTextWriter(newOut);
            lock (InternalSyncObject) { _out = newOut; }
        }

        public static void SetError(TextWriter newError)
        {
            CheckNonNull(newError, "newError");
            newError = SyncTextWriter.GetSynchronizedTextWriter(newError);
            lock (InternalSyncObject) { _error = newError; }
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

        private sealed class ControlCDelegateData
        {
            internal readonly ConsoleSpecialKey ControlKey;
            internal readonly ManualResetEvent CompletionEvent;
            internal readonly ConsoleCancelEventHandler CancelCallbacks;

            internal bool Cancel;
            internal bool DelegateStarted;

            internal ControlCDelegateData(ConsoleSpecialKey controlKey, ConsoleCancelEventHandler cancelCallbacks)
            {
                this.ControlKey = controlKey;
                this.CancelCallbacks = cancelCallbacks;
                this.CompletionEvent = new ManualResetEvent(false);
            }
        }

        internal static bool HandleBreakEvent(ConsoleSpecialKey controlKey)
        {
            // The thread that this gets called back on has a very small stack on some systems. There is
            // not enough space to handle a managed exception being caught and thrown. So, queue up a work
            // item on another thread for the actual event callback.

            // To avoid the race condition between remove handler and raising the event
            ConsoleCancelEventHandler cancelCallbacks = Console._cancelCallbacks;
            if (cancelCallbacks == null)
            {
                return false;
            }

            // Create the delegate
            ControlCDelegateData delegateData = new ControlCDelegateData(controlKey, cancelCallbacks);
            WaitCallback controlCCallback = new WaitCallback(ControlCDelegate);

            // Queue the delegate
            ThreadPool.QueueUserWorkItem(controlCCallback, delegateData);

            // Block until the delegate is done. We need to be robust in the face of the work item not executing
            // but we also want to get control back immediately after it is done and we don't want to give the
            // handler a fixed time limit in case it needs to display UI. Wait on the event twice, once with a
            // timout and a second time without if we are sure that the handler actually started.
            TimeSpan controlCWaitTime = new TimeSpan(0, 0, 30); // 30 seconds
            delegateData.CompletionEvent.WaitOne(controlCWaitTime);

            if (!delegateData.DelegateStarted)
            {
                Debug.Assert(false, "ThreadPool.QueueUserWorkItem did not execute the handler within 30 seconds.");
                return false;
            }

            delegateData.CompletionEvent.WaitOne();
            delegateData.CompletionEvent.Dispose();
            return delegateData.Cancel;
        }
    }
}
