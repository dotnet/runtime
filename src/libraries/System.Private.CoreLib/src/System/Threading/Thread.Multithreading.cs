// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System.Threading
{
    public sealed partial class Thread
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeImports.RuntimeLibrary, "RhpCurrentThreadIsFinalizerThread")]
#endif
        internal static extern bool CurrentThreadIsFinalizerThread();

        // State associated with starting new thread
        private sealed class StartHelper
        {
            internal int _maxStackSize;
            internal Delegate _start;
            internal object? _startArg;
            internal CultureInfo? _culture;
            internal CultureInfo? _uiCulture;
            internal ExecutionContext? _executionContext;

            internal StartHelper(Delegate start)
            {
                _start = start;
            }

            internal static readonly ContextCallback s_threadStartContextCallback = new ContextCallback(Callback);

            private static void Callback(object? state)
            {
                Debug.Assert(state is not null);
                ((StartHelper)state).RunWorker();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)] // avoid long-lived stack frame in many threads
            internal void Run()
            {
                if (_executionContext is not null && !_executionContext.IsDefault)
                {
                    ExecutionContext.RunInternal(_executionContext, s_threadStartContextCallback, this);
                }
                else
                {
                    RunWorker();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)] // avoid long-lived stack frame in many threads
            private void RunWorker()
            {
                InitializeCulture();

                Delegate start = _start;
                _start = null!;

#if FEATURE_OBJCMARSHAL
                if (AutoreleasePool.EnableAutoreleasePool)
                    AutoreleasePool.CreateAutoreleasePool();
#endif

                try
                {
#if TARGET_APPLE || NATIVEAOT
                    // On other platforms, when the underlying native thread is created,
                    // the thread name is set to the name of the managed thread by another thread.
                    // However, on Apple platforms and NativeAOT (across all OSes), only the thread itself can set its name.
                    // Therefore, by this point the native thread is still unnamed as it has not started yet.
                    Thread thread = Thread.CurrentThread;
                    if (!string.IsNullOrEmpty(thread.Name))
                    {
                        // Name the underlying native thread to match the managed thread name.
                        thread.ThreadNameChanged(thread.Name);
                    }
#endif
                    if (start is ThreadStart threadStart)
                    {
                        threadStart();
                    }
                    else
                    {
                        ParameterizedThreadStart parameterizedThreadStart = (ParameterizedThreadStart)start;

                        object? startArg = _startArg;
                        _startArg = null;

                        parameterizedThreadStart(startArg);
                    }
                }
                catch (Exception ex) when (ExceptionHandling.IsHandledByGlobalHandler(ex))
                {
                    // the handler returned "true" means the exception is now "handled" and we should gracefully exit.
                }

#if FEATURE_OBJCMARSHAL
                // There is no need to wrap this "clean up" code in a finally block since
                // if an exception is thrown above, the process is going to terminate.
                // Optimize for the most common case - no exceptions escape a thread.
                if (AutoreleasePool.EnableAutoreleasePool)
                    AutoreleasePool.DrainAutoreleasePool();
#endif
            }

            private void InitializeCulture()
            {
                if (_culture is not null)
                {
                    CultureInfo.CurrentCulture = _culture;
                    _culture = null;
                }

                if (_uiCulture is not null)
                {
                    CultureInfo.CurrentUICulture = _uiCulture;
                    _uiCulture = null;
                }
            }
        }

        private void InitializeStartHelper(Delegate start, int maxStackSize)
        {
            _startHelper = new StartHelper(start) { _maxStackSize = maxStackSize };
        }

        private void Start(object? parameter, bool captureContext)
        {
            RuntimeFeature.ThrowIfMultithreadingIsNotSupported();

            StartHelper? startHelper = _startHelper;

            // In the case of a null startHelper (second call to start on same thread)
            // StartCore method will take care of the error reporting.
            if (startHelper is not null)
            {
                if (startHelper._start is ThreadStart)
                {
                    // We expect the thread to be setup with a ParameterizedThreadStart if this Start is called.
                    throw new InvalidOperationException(SR.InvalidOperation_ThreadWrongThreadStart);
                }

                startHelper._startArg = parameter;
                startHelper._executionContext = captureContext ? ExecutionContext.Capture() : null;
            }

            StartCore();
        }

        private void Start(bool captureContext)
        {
            RuntimeFeature.ThrowIfMultithreadingIsNotSupported();
            StartHelper? startHelper = _startHelper;

            // In the case of a null startHelper (second call to start on same thread)
            // StartCore method will take care of the error reporting.
            if (startHelper is not null)
            {
                startHelper._startArg = null;
                startHelper._executionContext = captureContext ? ExecutionContext.Capture() : null;
            }

            StartCore();
        }

        private void SetCultureOnUnstartedThread(CultureInfo value, bool uiCulture)
        {
            ArgumentNullException.ThrowIfNull(value);

            StartHelper? startHelper = _startHelper;

            // This check is best effort to catch common user errors only. It won't catch all possible race
            // conditions between setting culture on unstarted thread and starting the thread.
            if ((ThreadState & ThreadState.Unstarted) == 0)
            {
                throw new InvalidOperationException(SR.Thread_Operation_RequiresCurrentThread);
            }

            Debug.Assert(startHelper is not null);

            if (uiCulture)
            {
                startHelper._uiCulture = value;
            }
            else
            {
                startHelper._culture = value;
            }
        }
    }
}
