// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public partial interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
namespace System.Collections.Generic
{
    public partial interface IAsyncEnumerable<out T>
    {
        System.Collections.Generic.IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
    }
    public partial interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        T Current { get; }
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
    }
}
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Method, Inherited=false, AllowMultiple=false)]
    public sealed partial class AsyncIteratorStateMachineAttribute : System.Runtime.CompilerServices.StateMachineAttribute
    {
        public AsyncIteratorStateMachineAttribute(System.Type stateMachineType) : base (default(System.Type)) { }
    }
    public readonly partial struct ConfiguredAsyncDisposable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable DisposeAsync() { throw null; }
    }
    public readonly partial struct ConfiguredCancelableAsyncEnumerable<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> ConfigureAwait(bool continueOnCapturedContext) { throw null; }
        public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T>.Enumerator GetAsyncEnumerator() { throw null; }
        public System.Runtime.CompilerServices.ConfiguredCancelableAsyncEnumerable<T> WithCancellation(System.Threading.CancellationToken cancellationToken) { throw null; }
        public readonly partial struct Enumerator
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public T Current { get { throw null; } }
            public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable DisposeAsync() { throw null; }
            public System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<bool> MoveNextAsync() { throw null; }
        }
    }
}
