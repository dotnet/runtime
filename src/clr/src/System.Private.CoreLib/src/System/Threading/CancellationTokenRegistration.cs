// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    /// <summary>
    /// Represents a callback delegate that has been registered with a <see cref="T:System.Threading.CancellationToken">CancellationToken</see>.
    /// </summary>
    /// <remarks>
    /// To unregister a callback, dispose the corresponding Registration instance.
    /// </remarks>
    public readonly struct CancellationTokenRegistration : IEquatable<CancellationTokenRegistration>, IDisposable
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
        /// <see cref="T:System.Threading.CancellationToken">CancellationToken</see>.
        /// If the target callback is currently executing, this method will wait until it completes, except
        /// in the degenerate cases where a callback method unregisters itself.
        /// </summary>
        public void Dispose()
        {
            CancellationTokenSource.CallbackNode node = _node;
            if (node != null && !node.Partition.Unregister(_id, node))
            {
                WaitForCallbackIfNecessary();
            }
        }

        /// <summary>
        /// Gets the <see cref="CancellationToken"/> with which this registration is associated.  If the
        /// registration isn't associated with a token (such as after the registration has been disposed),
        /// this will return a default token.
        /// </summary>
        public CancellationToken Token => _node?.Partition.Source.Token ?? default;

        /// <summary>
        /// Disposes of the registration and unregisters the target callback from the associated 
        /// <see cref="T:System.Threading.CancellationToken">CancellationToken</see>.
        /// </summary>
        internal bool Unregister() // corefx currently has an InternalsVisibleTo dependency on this
        {
            CancellationTokenSource.CallbackNode node = _node;
            return node != null && node.Partition.Unregister(_id, node);
        }

        private void WaitForCallbackIfNecessary()
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
            CancellationTokenSource source = _node.Partition.Source;
            if (source.IsCancellationRequested && // Running callbacks has commenced.
                !source.IsCancellationCompleted && // Running callbacks hasn't finished.
                source.ThreadIDExecutingCallbacks != Thread.CurrentThread.ManagedThreadId) // The executing thread ID is not this thread's ID.
            {
                // Callback execution is in progress, the executing thread is different to us and has taken the callback for execution
                // so observe and wait until this target callback is no longer the executing callback.
                source.WaitForCallbackToComplete(_id);
            }
        }

        /// <summary>
        /// Determines whether two <see
        /// cref="T:System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see>
        /// instances are equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True if the instances are equal; otherwise, false.</returns>
        public static bool operator ==(CancellationTokenRegistration left, CancellationTokenRegistration right) => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="T:System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instances are not equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True if the instances are not equal; otherwise, false.</returns>
        public static bool operator !=(CancellationTokenRegistration left, CancellationTokenRegistration right) => !left.Equals(right);

        /// <summary>
        /// Determines whether the current <see cref="T:System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instance is equal to the 
        /// specified <see cref="T:System.Object"/>.
        /// </summary> 
        /// <param name="obj">The other object to which to compare this instance.</param>
        /// <returns>True, if both this and <paramref name="obj"/> are equal. False, otherwise.
        /// Two <see cref="T:System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instances are equal if
        /// they both refer to the output of a single call to the same Register method of a 
        /// <see cref="T:System.Threading.CancellationToken">CancellationToken</see>. 
        /// </returns>
        public override bool Equals(object obj) => obj is CancellationTokenRegistration && Equals((CancellationTokenRegistration)obj);

        /// <summary>
        /// Determines whether the current <see cref="T:System.Threading.CancellationToken">CancellationToken</see> instance is equal to the 
        /// specified <see cref="T:System.Object"/>.
        /// </summary> 
        /// <param name="other">The other <see cref="T:System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> to which to compare this instance.</param>
        /// <returns>True, if both this and <paramref name="other"/> are equal. False, otherwise.
        /// Two <see cref="T:System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instances are equal if
        /// they both refer to the output of a single call to the same Register method of a 
        /// <see cref="T:System.Threading.CancellationToken">CancellationToken</see>. 
        /// </returns>
        public bool Equals(CancellationTokenRegistration other) => _node == other._node && _id == other._id;

        /// <summary>
        /// Serves as a hash function for a <see cref="T:System.Threading.CancellationTokenRegistration">CancellationTokenRegistration.</see>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="T:System.Threading.CancellationTokenRegistration">CancellationTokenRegistration</see> instance.</returns>
        public override int GetHashCode() => _node != null ? _node.GetHashCode() ^ _id.GetHashCode()  : _id.GetHashCode();
    }
}
