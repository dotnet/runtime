// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma warning disable 0420

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// --------------------------------------------------------------------------------------
//
// A class that provides a simple, lightweight implementation of lazy initialization, 
// obviating the need for a developer to implement a custom, thread-safe lazy initialization 
// solution.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Runtime;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Runtime.ExceptionServices;

namespace System
{
    // Lazy<T> is generic, but not all of its state needs to be generic.  Avoid creating duplicate
    // objects per instantiation by putting them here.
    internal static class LazyHelpers
    {
        // Dummy object used as the value of m_threadSafeObj if in PublicationOnly mode.
        internal static readonly object PUBLICATION_ONLY_SENTINEL = new object();
    }

    /// <summary>
    /// Provides support for lazy initialization.
    /// </summary>
    /// <typeparam name="T">Specifies the type of element being lazily initialized.</typeparam>
    /// <remarks>
    /// <para>
    /// By default, all public and protected members of <see cref="Lazy{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.  These thread-safety guarantees may be removed optionally and per instance
    /// using parameters to the type's constructors.
    /// </para>
    /// </remarks>
    [Serializable]
    [ComVisible(false)]
#if !FEATURE_CORECLR
    [HostProtection(Synchronization = true, ExternalThreading = true)]
#endif
    [DebuggerTypeProxy(typeof(System_LazyDebugView<>))]
    [DebuggerDisplay("ThreadSafetyMode={Mode}, IsValueCreated={IsValueCreated}, IsValueFaulted={IsValueFaulted}, Value={ValueForDebugDisplay}")]
    public class Lazy<T>
    {

#region Inner classes
        /// <summary>
        /// wrapper class to box the initialized value, this is mainly created to avoid boxing/unboxing the value each time the value is called in case T is 
        /// a value type
        /// </summary>
        [Serializable]
        class Boxed
        {
            internal Boxed(T value)
            {
                m_value = value;
            }
            internal T m_value;
        }


        /// <summary>
        /// Wrapper class to wrap the excpetion thrown by the value factory
        /// </summary>
        class LazyInternalExceptionHolder
        {
            internal ExceptionDispatchInfo m_edi;
            internal LazyInternalExceptionHolder(Exception ex)
            {
                m_edi = ExceptionDispatchInfo.Capture(ex);
            }
        }
#endregion

        // A dummy delegate used as a  :
        // 1- Flag to avoid recursive call to Value in None and ExecutionAndPublication modes in m_valueFactory
        // 2- Flag to m_threadSafeObj if ExecutionAndPublication mode and the value is known to be initialized
        static readonly Func<T> ALREADY_INVOKED_SENTINEL = delegate 
        {
            Contract.Assert(false, "ALREADY_INVOKED_SENTINEL should never be invoked.");
            return default(T);
        };

        //null --> value is not created
        //m_value is Boxed --> the value is created, and m_value holds the value
        //m_value is LazyExceptionHolder --> it holds an exception
        private object m_boxed;

        // The factory delegate that returns the value.
        // In None and ExecutionAndPublication modes, this will be set to ALREADY_INVOKED_SENTINEL as a flag to avoid recursive calls
        [NonSerialized]
        private Func<T> m_valueFactory;

        // null if it is not thread safe mode
        // LazyHelpers.PUBLICATION_ONLY_SENTINEL if PublicationOnly mode
        // object if ExecutionAndPublication mode (may be ALREADY_INVOKED_SENTINEL if the value is already initialized)
        [NonSerialized]
        private object m_threadSafeObj;


        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/> class that 
        /// uses <typeparamref name="T"/>'s default constructor for lazy initialization.
        /// </summary>
        /// <remarks>
        /// An instance created with this constructor may be used concurrently from multiple threads.
        /// </remarks>
        public Lazy()
            : this(LazyThreadSafetyMode.ExecutionAndPublication)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/> class that
        /// uses a pre-initialized specified value.
        /// </summary>
        /// <remarks>
        /// An instance created with this constructor should be usable by multiple threads
        //  concurrently.
        /// </remarks>
        public Lazy(T value)
        {
            m_boxed = new Boxed(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/> class that uses a
        /// specified initialization function.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="T:System.Func{T}"/> invoked to produce the lazily-initialized value when it is
        /// needed.
        /// </param>
        /// <exception cref="System.ArgumentNullException"><paramref name="valueFactory"/> is a null
        /// reference (Nothing in Visual Basic).</exception>
        /// <remarks>
        /// An instance created with this constructor may be used concurrently from multiple threads.
        /// </remarks>
        public Lazy(Func<T> valueFactory)
            : this(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/>
        /// class that uses <typeparamref name="T"/>'s default constructor and a specified thread-safety mode.
        /// </summary>
        /// <param name="isThreadSafe">true if this instance should be usable by multiple threads concurrently; false if the instance will only be used by one thread at a time.
        /// </param>
        public Lazy(bool isThreadSafe) : 
            this(isThreadSafe? LazyThreadSafetyMode.ExecutionAndPublication : LazyThreadSafetyMode.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/>
        /// class that uses <typeparamref name="T"/>'s default constructor and a specified thread-safety mode.
        /// </summary>
        /// <param name="mode">The lazy thread-safety mode mode</param>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="mode"/> mode contains an invalid valuee</exception>
        public Lazy(LazyThreadSafetyMode mode)
        {
            m_threadSafeObj = GetObjectFromMode(mode);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/> class
        /// that uses a specified initialization function and a specified thread-safety mode.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="T:System.Func{T}"/> invoked to produce the lazily-initialized value when it is needed.
        /// </param>
        /// <param name="isThreadSafe">true if this instance should be usable by multiple threads concurrently; false if the instance will only be used by one thread at a time.
        /// </param>
        /// <exception cref="System.ArgumentNullException"><paramref name="valueFactory"/> is
        /// a null reference (Nothing in Visual Basic).</exception>
        public Lazy(Func<T> valueFactory, bool isThreadSafe)
            : this(valueFactory, isThreadSafe ? LazyThreadSafetyMode.ExecutionAndPublication : LazyThreadSafetyMode.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/> class
        /// that uses a specified initialization function and a specified thread-safety mode.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="T:System.Func{T}"/> invoked to produce the lazily-initialized value when it is needed.
        /// </param>
        /// <param name="mode">The lazy thread-safety mode.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="valueFactory"/> is
        /// a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="mode"/> mode contains an invalid value.</exception>
        public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
        {
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");

            m_threadSafeObj = GetObjectFromMode(mode);
            m_valueFactory = valueFactory;
        }

        /// <summary>
        /// Static helper function that returns an object based on the given mode. it also throws an exception if the mode is invalid
        /// </summary>
        private static object GetObjectFromMode(LazyThreadSafetyMode mode)
        {
            if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
                return new object();
            else if (mode == LazyThreadSafetyMode.PublicationOnly)
                return LazyHelpers.PUBLICATION_ONLY_SENTINEL;
            else if (mode != LazyThreadSafetyMode.None)
                throw new ArgumentOutOfRangeException("mode", Environment.GetResourceString("Lazy_ctor_ModeInvalid"));
            
            return null; // None mode
        }

        /// <summary>Forces initialization during serialization.</summary>
        /// <param name="context">The StreamingContext for the serialization operation.</param>
        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            // Force initialization
            T dummy = Value;
        }

        /// <summary>Creates and returns a string representation of this instance.</summary>
        /// <returns>The result of calling <see cref="System.Object.ToString"/> on the <see
        /// cref="Value"/>.</returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <see cref="Value"/> is null.
        /// </exception>
        public override string ToString()
        {
            return IsValueCreated ? Value.ToString() : Environment.GetResourceString("Lazy_ToString_ValueNotCreated");
        }

        /// <summary>Gets the value of the Lazy&lt;T&gt; for debugging display purposes.</summary>
        internal T ValueForDebugDisplay
        {
            get
            {
                if (!IsValueCreated)
                {
                    return default(T);
                }
                return ((Boxed)m_boxed).m_value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance may be used concurrently from multiple threads.
        /// </summary>
        internal LazyThreadSafetyMode Mode
        {
            get
            {
                if (m_threadSafeObj == null) return LazyThreadSafetyMode.None;
                if (m_threadSafeObj == (object)LazyHelpers.PUBLICATION_ONLY_SENTINEL) return LazyThreadSafetyMode.PublicationOnly;
                return LazyThreadSafetyMode.ExecutionAndPublication;
            }
        }

        /// <summary>
        /// Gets whether the value creation is faulted or not
        /// </summary>
        internal bool IsValueFaulted
        {
            get { return m_boxed is LazyInternalExceptionHolder; }
        }

        /// <summary>Gets a value indicating whether the <see cref="T:System.Lazy{T}"/> has been initialized.
        /// </summary>
        /// <value>true if the <see cref="T:System.Lazy{T}"/> instance has been initialized;
        /// otherwise, false.</value>
        /// <remarks>
        /// The initialization of a <see cref="T:System.Lazy{T}"/> instance may result in either
        /// a value being produced or an exception being thrown.  If an exception goes unhandled during initialization, 
        /// <see cref="IsValueCreated"/> will return false.
        /// </remarks>
        public bool IsValueCreated
        {
            get
            {
                return m_boxed != null && m_boxed is Boxed;
            }
        }

        /// <summary>Gets the lazily initialized value of the current <see
        /// cref="T:System.Threading.Lazy{T}"/>.</summary>
        /// <value>The lazily initialized value of the current <see
        /// cref="T:System.Threading.Lazy{T}"/>.</value>
        /// <exception cref="T:System.MissingMemberException">
        /// The <see cref="T:System.Threading.Lazy{T}"/> was initialized to use the default constructor 
        /// of the type being lazily initialized, and that type does not have a public, parameterless constructor.
        /// </exception>
        /// <exception cref="T:System.MemberAccessException">
        /// The <see cref="T:System.Threading.Lazy{T}"/> was initialized to use the default constructor 
        /// of the type being lazily initialized, and permissions to access the constructor were missing.
        /// </exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// The <see cref="T:System.Threading.Lazy{T}"/> was constructed with the <see cref="T:System.Threading.LazyThreadSafetyMode.ExecutionAndPublication"/> or
        /// <see cref="T:System.Threading.LazyThreadSafetyMode.None"/>  and the initialization function attempted to access <see cref="Value"/> on this instance.
        /// </exception>
        /// <remarks>
        /// If <see cref="IsValueCreated"/> is false, accessing <see cref="Value"/> will force initialization.
        /// Please <see cref="System.Threading.LazyThreadSafetyMode"> for more information on how <see cref="T:System.Threading.Lazy{T}"/> will behave if an exception is thrown
        /// from initialization delegate.
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value
        {
            get
            {
                Boxed boxed = null;
                if (m_boxed != null )
                {
                    // Do a quick check up front for the fast path.
                    boxed = m_boxed as Boxed;
                    if (boxed != null)
                    {
                        return boxed.m_value;
                    }

                    LazyInternalExceptionHolder exc = m_boxed as LazyInternalExceptionHolder;
                    Contract.Assert(exc != null);
                    exc.m_edi.Throw();
                }

                // Fall through to the slow path.
#if !FEATURE_CORECLR
                // We call NOCTD to abort attempts by the debugger to funceval this property (e.g. on mouseover)
                //   (the debugger proxy is the correct way to look at state/value of this object)
                Debugger.NotifyOfCrossThreadDependency(); 
#endif
                return LazyInitValue();
               
            }
        }

        /// <summary>
        /// local helper method to initialize the value 
        /// </summary>
        /// <returns>The inititialized T value</returns>
        private T LazyInitValue()
        {
            Boxed boxed = null;
            LazyThreadSafetyMode mode = Mode;
            if (mode == LazyThreadSafetyMode.None)
            {
                boxed = CreateValue();
                m_boxed = boxed;
            }
            else if (mode == LazyThreadSafetyMode.PublicationOnly)
            {
                boxed = CreateValue();
                if (boxed == null ||
                    Interlocked.CompareExchange(ref m_boxed, boxed, null) != null)
                {
                    // If CreateValue returns null, it means another thread successfully invoked the value factory
                    // and stored the result, so we should just take what was stored.  If CreateValue returns non-null
                    // but another thread set the value we should just take what was stored.
                    boxed = (Boxed)m_boxed;
                }
                else
                {
                    // We successfully created and stored the value.  At this point, the value factory delegate is
                    // no longer needed, and we don't want to hold onto its resources.
                    m_valueFactory = ALREADY_INVOKED_SENTINEL;
                }
            }
            else
            {
                object threadSafeObj = Volatile.Read(ref m_threadSafeObj);
                bool lockTaken = false;
                try
                {
                    if (threadSafeObj != (object)ALREADY_INVOKED_SENTINEL)
                        Monitor.Enter(threadSafeObj, ref lockTaken);
                    else
                        Contract.Assert(m_boxed != null);

                    if (m_boxed == null)
                    {
                        boxed = CreateValue();
                        m_boxed = boxed;
                        Volatile.Write(ref m_threadSafeObj, ALREADY_INVOKED_SENTINEL);
                    }
                    else // got the lock but the value is not null anymore, check if it is created by another thread or faulted and throw if so
                    {
                        boxed = m_boxed as Boxed;
                        if (boxed == null) // it is not Boxed, so it is a LazyInternalExceptionHolder
                        {
                            LazyInternalExceptionHolder exHolder = m_boxed as LazyInternalExceptionHolder;
                            Contract.Assert(exHolder != null);
                            exHolder.m_edi.Throw();
                        }
                    }
                }
                finally
                {
                    if (lockTaken)
                        Monitor.Exit(threadSafeObj);
                }
            }
            Contract.Assert(boxed != null);
            return boxed.m_value;
        }

        /// <summary>Creates an instance of T using m_valueFactory in case its not null or use reflection to create a new T()</summary>
        /// <returns>An instance of Boxed.</returns>
        private Boxed CreateValue()
        {
            Boxed boxed = null;
            LazyThreadSafetyMode mode = Mode;
            if (m_valueFactory != null)
            {
                try
                {
                    // check for recursion
                    if (mode != LazyThreadSafetyMode.PublicationOnly && m_valueFactory == ALREADY_INVOKED_SENTINEL)
                        throw new InvalidOperationException(Environment.GetResourceString("Lazy_Value_RecursiveCallsToValue"));

                    Func<T> factory = m_valueFactory;
                    if (mode != LazyThreadSafetyMode.PublicationOnly) // only detect recursion on None and ExecutionAndPublication modes
                    {
                        m_valueFactory = ALREADY_INVOKED_SENTINEL;
                    }
                    else if (factory == ALREADY_INVOKED_SENTINEL)
                    {
                        // Another thread raced to successfully invoke the factory.
                        return null;
                    }
                    boxed = new Boxed(factory());
                }
                catch (Exception ex)
                {
                    if (mode != LazyThreadSafetyMode.PublicationOnly) // don't cache the exception for PublicationOnly mode
                        m_boxed = new LazyInternalExceptionHolder(ex);
                    throw;
                }
            }
            else
            {
                try
                {
                    boxed = new Boxed((T)Activator.CreateInstance(typeof(T)));

                }
                catch (System.MissingMethodException)
                {
                    Exception ex = new System.MissingMemberException(Environment.GetResourceString("Lazy_CreateValue_NoParameterlessCtorForT"));
                    if (mode != LazyThreadSafetyMode.PublicationOnly) // don't cache the exception for PublicationOnly mode
                        m_boxed = new LazyInternalExceptionHolder(ex);
                    throw ex;
                }
            }

            return boxed;
        }

    }

    /// <summary>A debugger view of the Lazy&lt;T&gt; to surface additional debugging properties and 
    /// to ensure that the Lazy&lt;T&gt; does not become initialized if it was not already.</summary>
    internal sealed class System_LazyDebugView<T>
    {
        //The Lazy object being viewed.
        private readonly Lazy<T> m_lazy;

        /// <summary>Constructs a new debugger view object for the provided Lazy object.</summary>
        /// <param name="lazy">A Lazy object to browse in the debugger.</param>
        public System_LazyDebugView(Lazy<T> lazy)
        {
            m_lazy = lazy;
        }

        /// <summary>Returns whether the Lazy object is initialized or not.</summary>
        public bool IsValueCreated
        {
            get { return m_lazy.IsValueCreated; }
        }

        /// <summary>Returns the value of the Lazy object.</summary>
        public T Value
        {
            get
            { return m_lazy.ValueForDebugDisplay; }
        }

        /// <summary>Returns the execution mode of the Lazy object</summary>
        public LazyThreadSafetyMode Mode
        {
            get { return m_lazy.Mode; }
        }

        /// <summary>Returns the execution mode of the Lazy object</summary>
        public bool IsValueFaulted
        {
            get { return m_lazy.IsValueFaulted; }
        }

    }
}
