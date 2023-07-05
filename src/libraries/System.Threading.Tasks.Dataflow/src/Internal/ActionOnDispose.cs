// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ActionOnDispose.cs
//
//
// Implementation of IDisposable that runs a delegate on Dispose.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Provider of disposables that run actions.</summary>
    internal static class Disposables
    {
        /// <summary>An IDisposable that does nothing.</summary>
        internal static readonly IDisposable Nop = new NopDisposable();

        /// <summary>Creates an IDisposable that runs an action when disposed.</summary>
        /// <typeparam name="T1">Specifies the type of the first argument.</typeparam>
        /// <typeparam name="T2">Specifies the type of the second argument.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The created disposable.</returns>
        internal static IDisposable Create<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            Debug.Assert(action != null, "Non-null disposer action required.");
            return new Disposable<T1, T2>(action, arg1, arg2);
        }

        /// <summary>Creates an IDisposable that runs an action when disposed.</summary>
        /// <typeparam name="T1">Specifies the type of the first argument.</typeparam>
        /// <typeparam name="T2">Specifies the type of the second argument.</typeparam>
        /// <typeparam name="T3">Specifies the type of the third argument.</typeparam>
        /// <param name="action">The action to invoke.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns>The created disposable.</returns>
        internal static IDisposable Create<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            Debug.Assert(action != null, "Non-null disposer action required.");
            return new Disposable<T1, T2, T3>(action, arg1, arg2, arg3);
        }

        /// <summary>A disposable that's a nop.</summary>
        [DebuggerDisplay("Disposed = true")]
        private sealed class NopDisposable : IDisposable
        {
            void IDisposable.Dispose() { }
        }

        /// <summary>An IDisposable that will run a delegate when disposed.</summary>
        [DebuggerDisplay("Disposed = {Disposed}")]
        private sealed class Disposable<T1, T2> : IDisposable
        {
            /// <summary>First state argument.</summary>
            private readonly T1 _arg1;
            /// <summary>Second state argument.</summary>
            private readonly T2 _arg2;
            /// <summary>The action to run when disposed. Null if disposed.</summary>
            private Action<T1, T2>? _action;

            /// <summary>Initializes the ActionOnDispose.</summary>
            /// <param name="action">The action to run when disposed.</param>
            /// <param name="arg1">The first argument.</param>
            /// <param name="arg2">The second argument.</param>
            internal Disposable(Action<T1, T2> action, T1 arg1, T2 arg2)
            {
                Debug.Assert(action != null, "Non-null action needed for disposable");
                _action = action;
                _arg1 = arg1;
                _arg2 = arg2;
            }

            /// <summary>Gets whether the IDisposable has been disposed.</summary>
            private bool Disposed { get { return _action == null; } }

            /// <summary>Invoke the action.</summary>
            void IDisposable.Dispose()
            {
                Action<T1, T2>? toRun = _action;
                if (toRun != null &&
                    Interlocked.CompareExchange(ref _action, null, toRun) == toRun)
                {
                    toRun(_arg1, _arg2);
                }
            }
        }

        /// <summary>An IDisposable that will run a delegate when disposed.</summary>
        [DebuggerDisplay("Disposed = {Disposed}")]
        private sealed class Disposable<T1, T2, T3> : IDisposable
        {
            /// <summary>First state argument.</summary>
            private readonly T1 _arg1;
            /// <summary>Second state argument.</summary>
            private readonly T2 _arg2;
            /// <summary>Third state argument.</summary>
            private readonly T3 _arg3;
            /// <summary>The action to run when disposed. Null if disposed.</summary>
            private Action<T1, T2, T3>? _action;

            /// <summary>Initializes the ActionOnDispose.</summary>
            /// <param name="action">The action to run when disposed.</param>
            /// <param name="arg1">The first argument.</param>
            /// <param name="arg2">The second argument.</param>
            /// <param name="arg3">The third argument.</param>
            internal Disposable(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
            {
                Debug.Assert(action != null, "Non-null action needed for disposable");
                _action = action;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = arg3;
            }

            /// <summary>Gets whether the IDisposable has been disposed.</summary>
            private bool Disposed { get { return _action == null; } }

            /// <summary>Invoke the action.</summary>
            void IDisposable.Dispose()
            {
                Action<T1, T2, T3>? toRun = _action;
                if (toRun != null &&
                    Interlocked.CompareExchange(ref _action, null, toRun) == toRun)
                {
                    toRun(_arg1, _arg2, _arg3);
                }
            }
        }
    }
}
