// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
////////////////////////////////////////////////////////////////////////////////

#pragma warning disable 0420 // turn off 'a reference to a volatile field will not be treated as volatile' during CAS.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Diagnostics.Contracts;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading
{
    /// <summary>
    /// Propagates notification that operations should be canceled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="CancellationToken"/> may be created directly in an unchangeable canceled or non-canceled state
    /// using the CancellationToken's constructors. However, to have a CancellationToken that can change 
    /// from a non-canceled to a canceled state, 
    /// <see cref="System.Threading.CancellationTokenSource">CancellationTokenSource</see> must be used.
    /// CancellationTokenSource exposes the associated CancellationToken that may be canceled by the source through its 
    /// <see cref="System.Threading.CancellationTokenSource.Token">Token</see> property. 
    /// </para>
    /// <para>
    /// Once canceled, a token may not transition to a non-canceled state, and a token whose 
    /// <see cref="CanBeCanceled"/> is false will never change to one that can be canceled.
    /// </para>
    /// <para>
    /// All members of this struct are thread-safe and may be used concurrently from multiple threads.
    /// </para>
    /// </remarks>
    [ComVisible(false)]
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    [DebuggerDisplay("IsCancellationRequested = {IsCancellationRequested}")]
    public struct CancellationToken
    {
        // The backing TokenSource.  
        // if null, it implicitly represents the same thing as new CancellationToken(false).
        // When required, it will be instantiated to reflect this.
        private CancellationTokenSource m_source;
        //!! warning. If more fields are added, the assumptions in CreateLinkedToken may no longer be valid

        /* Properties */

        /// <summary>
        /// Returns an empty CancellationToken value.
        /// </summary>
        /// <remarks>
        /// The <see cref="CancellationToken"/> value returned by this property will be non-cancelable by default.
        /// </remarks>
        public static CancellationToken None
        {
            get { return default(CancellationToken); }
        }

        /// <summary>
        /// Gets whether cancellation has been requested for this token.
        /// </summary>
        /// <value>Whether cancellation has been requested for this token.</value>
        /// <remarks>
        /// <para>
        /// This property indicates whether cancellation has been requested for this token, 
        /// either through the token initially being construted in a canceled state, or through
        /// calling <see cref="System.Threading.CancellationTokenSource.Cancel()">Cancel</see> 
        /// on the token's associated <see cref="CancellationTokenSource"/>.
        /// </para>
        /// <para>
        /// If this property is true, it only guarantees that cancellation has been requested.  
        /// It does not guarantee that every registered handler
        /// has finished executing, nor that cancellation requests have finished propagating
        /// to all registered handlers.  Additional synchronization may be required,
        /// particularly in situations where related objects are being canceled concurrently.
        /// </para>
        /// </remarks>
        public bool IsCancellationRequested 
        {
            get
            {
                return m_source != null && m_source.IsCancellationRequested;
            }
        }
        
        /// <summary>
        /// Gets whether this token is capable of being in the canceled state.
        /// </summary>
        /// <remarks>
        /// If CanBeCanceled returns false, it is guaranteed that the token will never transition
        /// into a canceled state, meaning that <see cref="IsCancellationRequested"/> will never
        /// return true.
        /// </remarks>
        public bool CanBeCanceled
        {
            get
            {
                return m_source != null && m_source.CanBeCanceled;
            }
        }

        /// <summary>
        /// Gets a <see cref="T:System.Threading.WaitHandle"/> that is signaled when the token is canceled.</summary>
        /// <remarks>
        /// Accessing this property causes a <see cref="T:System.Threading.WaitHandle">WaitHandle</see>
        /// to be instantiated.  It is preferable to only use this property when necessary, and to then
        /// dispose the associated <see cref="CancellationTokenSource"/> instance at the earliest opportunity (disposing
        /// the source will dispose of this allocated handle).  The handle should not be closed or disposed directly.
        /// </remarks>
        /// <exception cref="T:System.ObjectDisposedException">The associated <see
        /// cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> has been disposed.</exception>
        public WaitHandle WaitHandle
        {
            get
            {
                if (m_source == null)
                {
                    InitializeDefaultSource();
                }

                return m_source.WaitHandle;
            }
        }

        // public CancellationToken()
        // this constructor is implicit for structs
        //   -> this should behaves exactly as for new CancellationToken(false)

        /// <summary>
        /// Internal constructor only a CancellationTokenSource should create a CancellationToken
        /// </summary>
        internal CancellationToken(CancellationTokenSource source)
        {
            m_source = source;
        }

        /// <summary>
        /// Initializes the <see cref="T:System.Threading.CancellationToken">CancellationToken</see>.
        /// </summary>
        /// <param name="canceled">
        /// The canceled state for the token.
        /// </param>
        /// <remarks>
        /// Tokens created with this constructor will remain in the canceled state specified
        /// by the <paramref name="canceled"/> parameter.  If <paramref name="canceled"/> is false,
        /// both <see cref="CanBeCanceled"/> and <see cref="IsCancellationRequested"/> will be false.
        /// If <paramref name="canceled"/> is true,
        /// both <see cref="CanBeCanceled"/> and <see cref="IsCancellationRequested"/> will be true. 
        /// </remarks>
        public CancellationToken(bool canceled) :
            this()
        {
            if(canceled)
                m_source = CancellationTokenSource.InternalGetStaticSource(canceled);
        }

        /* Methods */
        

        private readonly static Action<Object> s_ActionToActionObjShunt = new Action<Object>(ActionToActionObjShunt);
        private static void ActionToActionObjShunt(object obj)
        {
            Action action = obj as Action;
            Contract.Assert(action != null, "Expected an Action here");
            action();
        }

        /// <summary>
        /// Registers a delegate that will be called when this <see cref="T:System.Threading.CancellationToken">CancellationToken</see> is canceled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this token is already in the canceled state, the
        /// delegate will be run immediately and synchronously. Any exception the delegate generates will be
        /// propagated out of this method call.
        /// </para>
        /// <para>
        /// The current <see cref="System.Threading.ExecutionContext">ExecutionContext</see>, if one exists, will be captured
        /// along with the delegate and will be used when executing it.
        /// </para>
        /// </remarks>
        /// <param name="callback">The delegate to be executed when the <see cref="T:System.Threading.CancellationToken">CancellationToken</see> is canceled.</param>
        /// <returns>The <see cref="T:System.Threading.CancellationTokenRegistration"/> instance that can 
        /// be used to deregister the callback.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="callback"/> is null.</exception>
        public CancellationTokenRegistration Register(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            
            return Register(
                s_ActionToActionObjShunt,
                callback,
                false, // useSync=false
                true   // useExecutionContext=true
             );
        }

        /// <summary>
        /// Registers a delegate that will be called when this 
        /// <see cref="T:System.Threading.CancellationToken">CancellationToken</see> is canceled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this token is already in the canceled state, the
        /// delegate will be run immediately and synchronously. Any exception the delegate generates will be
        /// propagated out of this method call.
        /// </para>
        /// <para>
        /// The current <see cref="System.Threading.ExecutionContext">ExecutionContext</see>, if one exists, will be captured
        /// along with the delegate and will be used when executing it.
        /// </para>
        /// </remarks>
        /// <param name="callback">The delegate to be executed when the <see cref="T:System.Threading.CancellationToken">CancellationToken</see> is canceled.</param>
        /// <param name="useSynchronizationContext">A Boolean value that indicates whether to capture
        /// the current <see cref="T:System.Threading.SynchronizationContext">SynchronizationContext</see> and use it
        /// when invoking the <paramref name="callback"/>.</param>
        /// <returns>The <see cref="T:System.Threading.CancellationTokenRegistration"/> instance that can 
        /// be used to deregister the callback.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="callback"/> is null.</exception>
        public CancellationTokenRegistration Register(Action callback, bool useSynchronizationContext)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");
            
            return Register(
                s_ActionToActionObjShunt,
                callback,
                useSynchronizationContext,
                true   // useExecutionContext=true
             );
        }

        /// <summary>
        /// Registers a delegate that will be called when this 
        /// <see cref="T:System.Threading.CancellationToken">CancellationToken</see> is canceled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this token is already in the canceled state, the
        /// delegate will be run immediately and synchronously. Any exception the delegate generates will be
        /// propagated out of this method call.
        /// </para>
        /// <para>
        /// The current <see cref="System.Threading.ExecutionContext">ExecutionContext</see>, if one exists, will be captured
        /// along with the delegate and will be used when executing it.
        /// </para>
        /// </remarks>
        /// <param name="callback">The delegate to be executed when the <see cref="T:System.Threading.CancellationToken">CancellationToken</see> is canceled.</param>
        /// <param name="state">The state to pass to the <paramref name="callback"/> when the delegate is invoked.  This may be null.</param>
        /// <returns>The <see cref="T:System.Threading.CancellationTokenRegistration"/> instance that can 
        /// be used to deregister the callback.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="callback"/> is null.</exception>
        public CancellationTokenRegistration Register(Action<Object> callback, Object state)
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            return Register(
                callback,
                state,
                false, // useSync=false
                true   // useExecutionContext=true
             );
        }

        /// <summary>
        /// Registers a delegate that will be called when this 
        /// <see cref="T:System.Threading.CancellationToken">CancellationToken</see> is canceled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this token is already in the canceled state, the
        /// delegate will be run immediately and synchronously. Any exception the delegate generates will be
        /// propagated out of this method call.
        /// </para>
        /// <para>
        /// The current <see cref="System.Threading.ExecutionContext">ExecutionContext</see>, if one exists, 
        /// will be captured along with the delegate and will be used when executing it.
        /// </para>
        /// </remarks>
        /// <param name="callback">The delegate to be executed when the <see cref="T:System.Threading.CancellationToken">CancellationToken</see> is canceled.</param>
        /// <param name="state">The state to pass to the <paramref name="callback"/> when the delegate is invoked.  This may be null.</param>
        /// <param name="useSynchronizationContext">A Boolean value that indicates whether to capture
        /// the current <see cref="T:System.Threading.SynchronizationContext">SynchronizationContext</see> and use it
        /// when invoking the <paramref name="callback"/>.</param>
        /// <returns>The <see cref="T:System.Threading.CancellationTokenRegistration"/> instance that can 
        /// be used to deregister the callback.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="callback"/> is null.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The associated <see
        /// cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> has been disposed.</exception>
        public CancellationTokenRegistration Register(Action<Object> callback, Object state, bool useSynchronizationContext)
        {
            return Register(
                callback,
                state,
                useSynchronizationContext,
                true   // useExecutionContext=true
             );
        }
        
        // helper for internal registration needs that don't require an EC capture (e.g. creating linked token sources, or registering unstarted TPL tasks)
        // has a handy signature, and skips capturing execution context.
        internal CancellationTokenRegistration InternalRegisterWithoutEC(Action<object> callback, Object state)
        {
            return Register(
                callback,
                state,
                false, // useSyncContext=false
                false  // useExecutionContext=false
             );
        }

        // the real work..
        [SecuritySafeCritical]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private CancellationTokenRegistration Register(Action<Object> callback, Object state, bool useSynchronizationContext, bool useExecutionContext)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (CanBeCanceled == false)
            {
                return new CancellationTokenRegistration(); // nothing to do for tokens than can never reach the canceled state. Give them a dummy registration.
            }

            // Capture sync/execution contexts if required.
            // Note: Only capture sync/execution contexts if IsCancellationRequested = false
            // as we know that if it is true that the callback will just be called synchronously.

            SynchronizationContext capturedSyncContext = null;
            ExecutionContext capturedExecutionContext = null;
            if (!IsCancellationRequested)
            {
                if (useSynchronizationContext)
                    capturedSyncContext = SynchronizationContext.Current;
                if (useExecutionContext)
                    capturedExecutionContext = ExecutionContext.Capture(
                        ref stackMark, ExecutionContext.CaptureOptions.OptimizeDefaultCase); // ideally we'd also use IgnoreSyncCtx, but that could break compat
            }

            // Register the callback with the source.
            return m_source.InternalRegister(callback, state, capturedSyncContext, capturedExecutionContext);
        }

        /// <summary>
        /// Determines whether the current <see cref="T:System.Threading.CancellationToken">CancellationToken</see> instance is equal to the 
        /// specified token.
        /// </summary>
        /// <param name="other">The other <see cref="T:System.Threading.CancellationToken">CancellationToken</see> to which to compare this
        /// instance.</param>
        /// <returns>True if the instances are equal; otherwise, false. Two tokens are equal if they are associated
        /// with the same <see cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> or if they were both constructed 
        /// from public CancellationToken constructors and their <see cref="IsCancellationRequested"/> values are equal.</returns>
        public bool Equals(CancellationToken other)
        {
            //if both sources are null, then both tokens represent the Empty token.
            if (m_source == null && other.m_source == null)
            {
                return true;
            }

            // one is null but other has inflated the default source
            // these are only equal if the inflated one is the staticSource(false)
            if (m_source == null)
            {
                return other.m_source == CancellationTokenSource.InternalGetStaticSource(false);
            }
            
            if (other.m_source == null)
            {
                return m_source == CancellationTokenSource.InternalGetStaticSource(false);
            }

            // general case, we check if the sources are identical
            
            return m_source == other.m_source;
        }

        /// <summary>
        /// Determines whether the current <see cref="T:System.Threading.CancellationToken">CancellationToken</see> instance is equal to the 
        /// specified <see cref="T:System.Object"/>.
        /// </summary>
        /// <param name="other">The other object to which to compare this instance.</param>
        /// <returns>True if <paramref name="other"/> is a <see cref="T:System.Threading.CancellationToken">CancellationToken</see>
        /// and if the two instances are equal; otherwise, false. Two tokens are equal if they are associated
        /// with the same <see cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> or if they were both constructed 
        /// from public CancellationToken constructors and their <see cref="IsCancellationRequested"/> values are equal.</returns>
        /// <exception cref="T:System.ObjectDisposedException">An associated <see
        /// cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> has been disposed.</exception>
        public override bool Equals(Object other)
        {
            if (other is CancellationToken)
            {
                return Equals((CancellationToken) other);
            }

            return false;
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="T:System.Threading.CancellationToken">CancellationToken</see>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="T:System.Threading.CancellationToken">CancellationToken</see> instance.</returns>
        public override Int32 GetHashCode()
        {
            if (m_source == null)
            {
                // link to the common source so that we have a source to interrogate.
                return CancellationTokenSource.InternalGetStaticSource(false).GetHashCode();
            }

            return m_source.GetHashCode(); 
        }
        
        /// <summary>
        /// Determines whether two <see cref="T:System.Threading.CancellationToken">CancellationToken</see> instances are equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True if the instances are equal; otherwise, false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">An associated <see
        /// cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> has been disposed.</exception>
        public static bool operator ==(CancellationToken left, CancellationToken right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="T:System.Threading.CancellationToken">CancellationToken</see> instances are not equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True if the instances are not equal; otherwise, false.</returns>
        /// <exception cref="T:System.ObjectDisposedException">An associated <see
        /// cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> has been disposed.</exception>
        public static bool operator !=(CancellationToken left, CancellationToken right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Throws a <see cref="T:System.OperationCanceledException">OperationCanceledException</see> if
        /// this token has had cancellation requested.
        /// </summary>
        /// <remarks>
        /// This method provides functionality equivalent to:
        /// <code>
        /// if (token.IsCancellationRequested) 
        ///    throw new OperationCanceledException(token);
        /// </code>
        /// </remarks>
        /// <exception cref="System.OperationCanceledException">The token has had cancellation requested.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The associated <see
        /// cref="T:System.Threading.CancellationTokenSource">CancellationTokenSource</see> has been disposed.</exception>
        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested) 
                ThrowOperationCanceledException();
        }

        // Throw an ODE if this CancellationToken's source is disposed.
        internal void ThrowIfSourceDisposed()
        {
            if ((m_source != null) && m_source.IsDisposed)
                ThrowObjectDisposedException();
        }

        // Throws an OCE; separated out to enable better inlining of ThrowIfCancellationRequested
        private void ThrowOperationCanceledException()
        {
            throw new OperationCanceledException(Environment.GetResourceString("OperationCanceled"), this);
        }

        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(null, Environment.GetResourceString("CancellationToken_SourceDisposed"));
        }

        // -----------------------------------
        // Private helpers
        
        private void InitializeDefaultSource()
        {
            // Lazy is slower, and although multiple threads may try and set m_source repeatedly, the race condition is benign.
            // Alternative: LazyInititalizer.EnsureInitialized(ref m_source, ()=>CancellationTokenSource.InternalGetStaticSource(false));

            m_source = CancellationTokenSource.InternalGetStaticSource(false);
        }
    }
}
