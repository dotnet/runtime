// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// --------------------------------------------------------------------------------------
//
// A class that provides a simple, lightweight implementation of lazy initialization, 
// obviating the need for a developer to implement a custom, thread-safe lazy initialization 
// solution.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace System
{
    internal enum LazyState
    {
        NoneViaConstructor = 0,
        NoneViaFactory     = 1,
        NoneException      = 2,

        PublicationOnlyViaConstructor = 3,
        PublicationOnlyViaFactory     = 4,
        PublicationOnlyWait           = 5,
        PublicationOnlyException      = 6,

        ExecutionAndPublicationViaConstructor = 7,
        ExecutionAndPublicationViaFactory     = 8,
        ExecutionAndPublicationException      = 9,
    }

    /// <summary>
    /// LazyHelper serves multiples purposes
    /// - minimizing code size of Lazy&lt;T&gt; by implementing as much of the code that is not generic
    ///   this reduces generic code bloat, making faster class initialization
    /// - contains singleton objects that are used to handle threading primitives for PublicationOnly mode
    /// - allows for instantiation for ExecutionAndPublication so as to create an object for locking on
    /// - holds exception information.
    /// </summary>
    internal class LazyHelper
    {
        internal readonly static LazyHelper NoneViaConstructor            = new LazyHelper(LazyState.NoneViaConstructor);
        internal readonly static LazyHelper NoneViaFactory                = new LazyHelper(LazyState.NoneViaFactory);
        internal readonly static LazyHelper PublicationOnlyViaConstructor = new LazyHelper(LazyState.PublicationOnlyViaConstructor);
        internal readonly static LazyHelper PublicationOnlyViaFactory     = new LazyHelper(LazyState.PublicationOnlyViaFactory);
        internal readonly static LazyHelper PublicationOnlyWaitForOtherThreadToPublish       = new LazyHelper(LazyState.PublicationOnlyWait);

        internal LazyState State { get; }

        private readonly ExceptionDispatchInfo? _exceptionDispatch;

        /// <summary>
        /// Constructor that defines the state
        /// </summary>
        internal LazyHelper(LazyState state)
        {
            State = state;
        }

        /// <summary>
        /// Constructor used for exceptions
        /// </summary>
        internal LazyHelper(LazyThreadSafetyMode mode, Exception exception)
        {
            switch(mode)
            {
                case LazyThreadSafetyMode.ExecutionAndPublication:
                    State = LazyState.ExecutionAndPublicationException;
                    break;

                case LazyThreadSafetyMode.None:
                    State = LazyState.NoneException;
                    break;

                case LazyThreadSafetyMode.PublicationOnly:
                    State = LazyState.PublicationOnlyException;
                    break;

                default:
                    Debug.Fail("internal constructor, this should never occur");
                    break;
            }

            _exceptionDispatch = ExceptionDispatchInfo.Capture(exception);
        }

        [DoesNotReturn]
        internal void ThrowException()
        {
            Debug.Assert(_exceptionDispatch != null, "execution path is invalid");

            _exceptionDispatch.Throw();
        }

        private LazyThreadSafetyMode GetMode()
        {
            switch (State)
            {
                case LazyState.NoneViaConstructor:
                case LazyState.NoneViaFactory:
                case LazyState.NoneException:
                    return LazyThreadSafetyMode.None;

                case LazyState.PublicationOnlyViaConstructor:
                case LazyState.PublicationOnlyViaFactory:
                case LazyState.PublicationOnlyWait:
                case LazyState.PublicationOnlyException:
                    return LazyThreadSafetyMode.PublicationOnly;

                case LazyState.ExecutionAndPublicationViaConstructor:
                case LazyState.ExecutionAndPublicationViaFactory:
                case LazyState.ExecutionAndPublicationException:
                    return LazyThreadSafetyMode.ExecutionAndPublication;

                default:
                    Debug.Fail("Invalid logic; State should always have a valid value");
                    return default;
            }
        }

        internal static LazyThreadSafetyMode? GetMode(LazyHelper? state)
        {
            if (state == null)
                return null; // we don't know the mode anymore
            return state.GetMode();
        }

        internal static bool GetIsValueFaulted(LazyHelper? state) => state?._exceptionDispatch != null;

        internal static LazyHelper Create(LazyThreadSafetyMode mode, bool useDefaultConstructor)
        {
            switch (mode)
            {
                case LazyThreadSafetyMode.None:
                    return useDefaultConstructor ? NoneViaConstructor : NoneViaFactory;

                case LazyThreadSafetyMode.PublicationOnly:
                    return useDefaultConstructor ? PublicationOnlyViaConstructor : PublicationOnlyViaFactory;

                case LazyThreadSafetyMode.ExecutionAndPublication:
                    // we need to create an object for ExecutionAndPublication because we use Monitor-based locking
                    var state = useDefaultConstructor ? LazyState.ExecutionAndPublicationViaConstructor : LazyState.ExecutionAndPublicationViaFactory;
                    return new LazyHelper(state);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), SR.Lazy_ctor_ModeInvalid);
            }
        }

        internal static T CreateViaDefaultConstructor<T>()
        {
            try
            {
                return Activator.CreateInstance<T>();
            }
            catch (MissingMethodException)
            {
                throw new MissingMemberException(SR.Lazy_CreateValue_NoParameterlessCtorForT);
            }
        }

        internal static LazyThreadSafetyMode GetModeFromIsThreadSafe(bool isThreadSafe)
        {
            return isThreadSafe ? LazyThreadSafetyMode.ExecutionAndPublication : LazyThreadSafetyMode.None;
        }
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
    [DebuggerTypeProxy(typeof(LazyDebugView<>))]
    [DebuggerDisplay("ThreadSafetyMode={Mode}, IsValueCreated={IsValueCreated}, IsValueFaulted={IsValueFaulted}, Value={ValueForDebugDisplay}")]
    public class Lazy<T>
    {
        private static T CreateViaDefaultConstructor() => LazyHelper.CreateViaDefaultConstructor<T>();

        // _state, a volatile reference, is set to null after _value has been set
        private volatile LazyHelper? _state;

        // we ensure that _factory when finished is set to null to allow garbage collector to clean up
        // any referenced items
        private Func<T>? _factory;

        // _value eventually stores the lazily created value. It is valid when _state = null.
        private T _value = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/> class that 
        /// uses <typeparamref name="T"/>'s default constructor for lazy initialization.
        /// </summary>
        /// <remarks>
        /// An instance created with this constructor may be used concurrently from multiple threads.
        /// </remarks>
        public Lazy()
            : this(null, LazyThreadSafetyMode.ExecutionAndPublication, useDefaultConstructor: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/> class that
        /// uses a pre-initialized specified value.
        /// </summary>
        /// <remarks>
        /// An instance created with this constructor should be usable by multiple threads
        /// concurrently.
        /// </remarks>
        public Lazy(T value)
        {
            _value = value;
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
            : this(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication, useDefaultConstructor: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/>
        /// class that uses <typeparamref name="T"/>'s default constructor and a specified thread-safety mode.
        /// </summary>
        /// <param name="isThreadSafe">true if this instance should be usable by multiple threads concurrently; false if the instance will only be used by one thread at a time.
        /// </param>
        public Lazy(bool isThreadSafe) :
            this(null, LazyHelper.GetModeFromIsThreadSafe(isThreadSafe), useDefaultConstructor: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Threading.Lazy{T}"/>
        /// class that uses <typeparamref name="T"/>'s default constructor and a specified thread-safety mode.
        /// </summary>
        /// <param name="mode">The lazy thread-safety mode</param>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="mode"/> mode contains an invalid valuee</exception>
        public Lazy(LazyThreadSafetyMode mode) :
            this(null, mode, useDefaultConstructor:true)
        {
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
        public Lazy(Func<T> valueFactory, bool isThreadSafe) :
            this(valueFactory, LazyHelper.GetModeFromIsThreadSafe(isThreadSafe), useDefaultConstructor: false)
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
            : this(valueFactory, mode, useDefaultConstructor:false)
        {
        }

        private Lazy(Func<T>? valueFactory, LazyThreadSafetyMode mode, bool useDefaultConstructor)
        {
            if (valueFactory == null && !useDefaultConstructor)
                throw new ArgumentNullException(nameof(valueFactory));

            _factory = valueFactory;
            _state = LazyHelper.Create(mode, useDefaultConstructor);
        }

        private void ViaConstructor()
        {
            _value = CreateViaDefaultConstructor();
            _state = null; // volatile write, must occur after setting _value
        }

        private void ViaFactory(LazyThreadSafetyMode mode)
        {
            try
            {
                Func<T>? factory = _factory;
                if (factory == null)
                    throw new InvalidOperationException(SR.Lazy_Value_RecursiveCallsToValue);
                _factory = null;

                _value = factory();
                _state = null; // volatile write, must occur after setting _value
            }
            catch (Exception exception)
            {
                _state = new LazyHelper(mode, exception);
                throw;
            }
        }

        private void ExecutionAndPublication(LazyHelper executionAndPublication, bool useDefaultConstructor)
        {
            lock (executionAndPublication)
            {
                // it's possible for multiple calls to have piled up behind the lock, so we need to check
                // to see if the ExecutionAndPublication object is still the current implementation.
                if (ReferenceEquals(_state, executionAndPublication))
                {
                    if (useDefaultConstructor)
                    {
                        ViaConstructor();
                    }
                    else
                    {
                        ViaFactory(LazyThreadSafetyMode.ExecutionAndPublication);
                    }
                }
            }
        }

        private void PublicationOnly(LazyHelper publicationOnly, T possibleValue)
        {
            LazyHelper? previous = Interlocked.CompareExchange(ref _state, LazyHelper.PublicationOnlyWaitForOtherThreadToPublish, publicationOnly);
            if (previous == publicationOnly)
            {
                _factory = null;
                _value = possibleValue;
                _state = null; // volatile write, must occur after setting _value
            }
        }

        private void PublicationOnlyViaConstructor(LazyHelper initializer)
        {
            PublicationOnly(initializer, CreateViaDefaultConstructor());
        }

        private void PublicationOnlyViaFactory(LazyHelper initializer)
        {
            Func<T>? factory = _factory;
            if (factory == null)
            {
                PublicationOnlyWaitForOtherThreadToPublish();
            }
            else
            {
                PublicationOnly(initializer, factory());
            }
        }

        private void PublicationOnlyWaitForOtherThreadToPublish()
        {
            var spinWait = new SpinWait();
            while (!ReferenceEquals(_state, null))
            {
                // We get here when PublicationOnly temporarily sets _state to LazyHelper.PublicationOnlyWaitForOtherThreadToPublish.
                // This temporary state should be quickly followed by _state being set to null.
                spinWait.SpinOnce();
            }
        }

        private T CreateValue()
        {
            // we have to create a copy of state here, and use the copy exclusively from here on in
            // so as to ensure thread safety.
            LazyHelper? state = _state;
            if (state != null) 
            {
                switch (state.State)
                {
                    case LazyState.NoneViaConstructor:
                        ViaConstructor();
                        break;

                    case LazyState.NoneViaFactory:
                        ViaFactory(LazyThreadSafetyMode.None);
                        break;

                    case LazyState.PublicationOnlyViaConstructor:
                        PublicationOnlyViaConstructor(state);
                        break;

                    case LazyState.PublicationOnlyViaFactory:
                        PublicationOnlyViaFactory(state);
                        break;

                    case LazyState.PublicationOnlyWait:
                        PublicationOnlyWaitForOtherThreadToPublish();
                        break;

                    case LazyState.ExecutionAndPublicationViaConstructor:
                        ExecutionAndPublication(state, useDefaultConstructor:true);
                        break;

                    case LazyState.ExecutionAndPublicationViaFactory:
                        ExecutionAndPublication(state, useDefaultConstructor:false);
                        break;

                    default:
                        state.ThrowException();
                        break;
                }
            }
            return Value;
        }

        /// <summary>Creates and returns a string representation of this instance.</summary>
        /// <returns>The result of calling <see cref="System.Object.ToString"/> on the <see
        /// cref="Value"/>.</returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <see cref="Value"/> is null.
        /// </exception>
        public override string? ToString()
        {
            return IsValueCreated ?
                Value!.ToString() : // Throws NullReferenceException as if caller called ToString on the value itself
                SR.Lazy_ToString_ValueNotCreated;
        }

        /// <summary>Gets the value of the Lazy&lt;T&gt; for debugging display purposes.</summary>
        [MaybeNull]
        internal T ValueForDebugDisplay
        {
            get
            {
                if (!IsValueCreated)
                {
                    return default!;
                }
                return _value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance may be used concurrently from multiple threads.
        /// </summary>
        internal LazyThreadSafetyMode? Mode => LazyHelper.GetMode(_state);

        /// <summary>
        /// Gets whether the value creation is faulted or not
        /// </summary>
        internal bool IsValueFaulted => LazyHelper.GetIsValueFaulted(_state);

        /// <summary>Gets a value indicating whether the <see cref="T:System.Lazy{T}"/> has been initialized.
        /// </summary>
        /// <value>true if the <see cref="T:System.Lazy{T}"/> instance has been initialized;
        /// otherwise, false.</value>
        /// <remarks>
        /// The initialization of a <see cref="T:System.Lazy{T}"/> instance may result in either
        /// a value being produced or an exception being thrown.  If an exception goes unhandled during initialization, 
        /// <see cref="IsValueCreated"/> will return false.
        /// </remarks>
        public bool IsValueCreated => _state == null;

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
        /// Please <see cref="System.Threading.LazyThreadSafetyMode"/> for more information on how <see cref="T:System.Threading.Lazy{T}"/> will behave if an exception is thrown
        /// from initialization delegate.
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value => _state == null ? _value : CreateValue();
    }

    /// <summary>A debugger view of the Lazy&lt;T&gt; to surface additional debugging properties and 
    /// to ensure that the Lazy&lt;T&gt; does not become initialized if it was not already.</summary>
    internal sealed class LazyDebugView<T>
    {
        //The Lazy object being viewed.
        private readonly Lazy<T> _lazy;

        /// <summary>Constructs a new debugger view object for the provided Lazy object.</summary>
        /// <param name="lazy">A Lazy object to browse in the debugger.</param>
        public LazyDebugView(Lazy<T> lazy)
        {
            _lazy = lazy;
        }

        /// <summary>Returns whether the Lazy object is initialized or not.</summary>
        public bool IsValueCreated
        {
            get { return _lazy.IsValueCreated; }
        }

        /// <summary>Returns the value of the Lazy object.</summary>
        public T Value
        {
            get { return _lazy.ValueForDebugDisplay; }
        }

        /// <summary>Returns the execution mode of the Lazy object</summary>
        public LazyThreadSafetyMode? Mode
        {
            get { return _lazy.Mode; }
        }

        /// <summary>Returns the execution mode of the Lazy object</summary>
        public bool IsValueFaulted
        {
            get { return _lazy.IsValueFaulted; }
        }
    }
}
