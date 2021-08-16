// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading
{
    /// <summary>
    /// Represents a callback delegate that has been registered with a <see cref="System.Threading.CancellationToken">CancellationToken</see>.
    /// </summary>
    /// <remarks>
    /// To unregister a callback, dispose the corresponding Registration instance.
    /// </remarks>
    public readonly struct CancellationTokenRegistration : IEquatable<CancellationTokenRegistration>, IDisposable, IAsyncDisposable
    {
        private readonly long _id;
        private readonly CancellationTokenSource.CallbackNode _node;

        internal CancellationTokenRegistration(long id, CancellationTokenSource.CallbackNode node)
        {
            _id = id;
            _node = node;
        }

        /// <summary>
        /// Disposes of the registration and unregisters the target callback from the associated
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see>.
        /// If the target callback is currently executing, this method will wait until it completes, except
        /// in the degenerate cases where a callback method unregisters itself.
        /// </summary>
        public void Dispose()
        {
            if (_node is CancellationTokenSource.CallbackNode node && !node.Registrations.Unregister(_id, node))
            {
                WaitForCallbackIfNecessary(_id, node);

                static void WaitForCallbackIfNecessary(long id, CancellationTokenSource.CallbackNode node)
                {
                    // We're a valid registration but we were unable to unregister, which means the callback wasn't in the list,
                    // which means either it already executed or it's currently executing. We guarantee that we will not return
                    // if the callback is being executed (assuming we are not currently called by the callback itself)
                    // We achieve this by the following rules:
                    //    1. If we are called in the context of an executing callback, no need to wait (determined by tracking callback-executor threadID)
                    //       - if the currently executing callback is this CTR, then waiting would deadlock. (We choose to return rather than deadlock)
                    //       - if not, then this CTR cannot be the one executing, hence no need to wait
                    //    2. If unregistration failed, and we are on a different thread, then the callback may be running under control of cts.Cancel()
                    //       => poll until cts.ExecutingCallback is not the one we are trying to unregister.
                    CancellationTokenSource source = node.Registrations.Source;
                    if (source.IsCancellationRequested && // Running callbacks has commenced.
                        !source.IsCancellationCompleted && // Running callbacks hasn't finished.
                        node.Registrations.ThreadIDExecutingCallbacks != Environment.CurrentManagedThreadId) // The executing thread ID is not this thread's ID.
                    {
                        // Callback execution is in progress, the executing thread is different from this thread and has taken the callback for execution
                        // so observe and wait until this target callback is no longer the executing callback.
                        node.Registrations.WaitForCallbackToComplete(id);
                    }
                }
            }
        }

        /// <summary>
        /// Disposes of the registration and unregisters the target callback from the associated
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see>.
        /// The returned <see cref="ValueTask"/> will complete once the associated callback
        /// is unregistered without having executed or once it's finished executing, except
        /// in the degenerate case where the callback itself is unregistering itself.
        /// </summary>
        public ValueTask DisposeAsync()
        {
            return _node is CancellationTokenSource.CallbackNode node && !node.Registrations.Unregister(_id, node) ?
                WaitForCallbackIfNecessaryAsync(_id, node) :
                default;

            static ValueTask WaitForCallbackIfNecessaryAsync(long id, CancellationTokenSource.CallbackNode node)
            {
                // Same as WaitForCallbackIfNecessary, except returning a task that'll be completed when callbacks complete.

                CancellationTokenSource source = node.Registrations.Source;
                if (source.IsCancellationRequested && // Running callbacks has commenced.
                    !source.IsCancellationCompleted && // Running callbacks hasn't finished.
                    node.Registrations.ThreadIDExecutingCallbacks != Environment.CurrentManagedThreadId) // The executing thread ID is not this thread's ID.
                {
                    // Callback execution is in progress, the executing thread is different from this thread and has taken the callback for execution
                    // so get a task that'll complete when this target callback is no longer the executing callback.
                    return node.Registrations.WaitForCallbackToCompleteAsync(id);
                }

                // Callback is either already completed, won't execute, or the callback itself is calling this.
                return default;
            }
        }

        /// <summary>Gets the <see cref="CancellationToken"/> with which this registration is associated.</summary>
        /// <remarks>
        /// If the registration isn't associated with a token (such as for a registration returned from a call
        /// to <see cref="CancellationToken.Register"/> on a token that already had cancellation requested),
        /// this will return a default token.
        /// </remarks>
        public CancellationToken Token =>
            _node is CancellationTokenSource.CallbackNode node ?
                new CancellationToken(node.Registrations.Source) : // avoid CTS.Token, which throws after disposal
                default;

        /// <summary>
        /// Disposes of the registration and unregisters the target callback from the associated
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see>.
        /// </summary>
        public bool Unregister() =>
            _node is CancellationTokenSource.CallbackNode node && node.Registrations.Unregister(_id, node);

        /// <summary>
        /// Determines whether two <see
        /// cref="System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see>
        /// instances are equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True if the instances are equal; otherwise, false.</returns>
        public static bool operator ==(CancellationTokenRegistration left, CancellationTokenRegistration right) => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instances are not equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True if the instances are not equal; otherwise, false.</returns>
        public static bool operator !=(CancellationTokenRegistration left, CancellationTokenRegistration right) => !left.Equals(right);

        /// <summary>
        /// Determines whether the current <see cref="System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instance is equal to the
        /// specified <see cref="object"/>.
        /// </summary>
        /// <param name="obj">The other object to which to compare this instance.</param>
        /// <returns>True, if both this and <paramref name="obj"/> are equal. False, otherwise.
        /// Two <see cref="System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instances are equal if
        /// they both refer to the output of a single call to the same Register method of a
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see>.
        /// </returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is CancellationTokenRegistration other && Equals(other);

        /// <summary>
        /// Determines whether the current <see cref="System.Threading.CancellationToken">CancellationToken</see> instance is equal to the
        /// specified <see cref="object"/>.
        /// </summary>
        /// <param name="other">The other <see cref="System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> to which to compare this instance.</param>
        /// <returns>True, if both this and <paramref name="other"/> are equal. False, otherwise.
        /// Two <see cref="System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instances are equal if
        /// they both refer to the output of a single call to the same Register method of a
        /// <see cref="System.Threading.CancellationToken">CancellationToken</see>.
        /// </returns>
        public bool Equals(CancellationTokenRegistration other) => _node == other._node && _id == other._id;

        /// <summary>
        /// Serves as a hash function for a <see cref="System.Threading.CancellationTokenRegistration">CancellationTokenRegistration.</see>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instance.</returns>
        public override int GetHashCode() => _node != null ? _node.GetHashCode() ^ _id.GetHashCode() : _id.GetHashCode();
    }
}
