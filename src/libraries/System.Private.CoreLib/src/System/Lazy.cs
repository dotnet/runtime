// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        // States used when exceptions thrown by the value factory are not cached, so
        // that a subsequent access to Value invokes the value factory again.
        NoneViaFactoryNoExceptionCaching                    = 10,
        ExecutionAndPublicationViaFactoryNoExceptionCaching = 11,

        // State used when PublicationOnly is explicitly asked to cache exceptions thrown by the value factory.
        PublicationOnlyViaFactoryWithExceptionCaching = 12,
    }

    /// <summary>
    /// LazyHelper serves multiples purposes
    /// - minimizing code size of Lazy&lt;T&gt; by implementing as much of the code that is not generic
    ///   this reduces generic code bloat, making faster class initialization
    /// - contains singleton objects that are used to handle threading primitives for PublicationOnly mode
    /// - allows for instantiation for ExecutionAndPublication so as to create an object for locking on
    /// - holds exception information.
    /// </summary>
    internal sealed class LazyHelper
    {
        internal static readonly LazyHelper NoneViaConstructor            = new LazyHelper(LazyState.NoneViaConstructor);
        internal static readonly LazyHelper NoneViaFactory                = new LazyHelper(LazyState.NoneViaFactory);
        internal static readonly LazyHelper NoneViaFactoryNoExceptionCaching = new LazyHelper(LazyState.NoneViaFactoryNoExceptionCaching);
        internal static readonly LazyHelper PublicationOnlyViaFactoryWithExceptionCaching = new LazyHelper(LazyState.PublicationOnlyViaFactoryWithExceptionCaching);
        internal static readonly LazyHelper PublicationOnlyViaConstructor = new LazyHelper(LazyState.PublicationOnlyViaConstructor);
        internal static readonly LazyHelper PublicationOnlyViaFactory     = new LazyHelper(LazyState.PublicationOnlyViaFactory);
        internal static readonly LazyHelper PublicationOnlyWaitForOtherThreadToPublish       = new LazyHelper(LazyState.PublicationOnlyWait);

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
            switch (mode)
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
                case LazyState.NoneViaFactoryNoExceptionCaching:
                    return LazyThreadSafetyMode.None;

                case LazyState.PublicationOnlyViaConstructor:
                case LazyState.PublicationOnlyViaFactory:
                case LazyState.PublicationOnlyWait:
                case LazyState.PublicationOnlyException:
                case LazyState.PublicationOnlyViaFactoryWithExceptionCaching:
                    return LazyThreadSafetyMode.PublicationOnly;

                case LazyState.ExecutionAndPublicationViaConstructor:
                case LazyState.ExecutionAndPublicationViaFactory:
                case LazyState.ExecutionAndPublicationException:
                case LazyState.ExecutionAndPublicationViaFactoryNoExceptionCaching:
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
                    LazyState state = useDefaultConstructor ?
                        LazyState.ExecutionAndPublicationViaConstructor :
                        LazyState.ExecutionAndPublicationViaFactory;
                    return new LazyHelper(state);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), SR.Lazy_ctor_ModeInvalid);
            }
        }

        internal static LazyHelper CreateViaFactory(LazyThreadSafetyMode mode, bool cacheExceptions)
        {
            switch (mode)
            {
                case LazyThreadSafetyMode.None:
                    return cacheExceptions ? NoneViaFactory : NoneViaFactoryNoExceptionCaching;

                case LazyThreadSafetyMode.PublicationOnly:
                    return cacheExceptions ? PublicationOnlyViaFactoryWithExceptionCaching : PublicationOnlyViaFactory;

                case LazyThreadSafetyMode.ExecutionAndPublication:
                    // we need to create an object for ExecutionAndPublication because we use Monitor-based locking
                    return new LazyHelper(cacheExceptions ?
                        LazyState.ExecutionAndPublicationViaFactory :
                        LazyState.ExecutionAndPublicationViaFactoryNoExceptionCaching);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), SR.Lazy_ctor_ModeInvalid);
            }
        }

        /// <summary>Gets whether exceptions are cached by default for the specified mode when a value factory is used.</summary>
        internal static bool CachesExceptionsByDefault(LazyThreadSafetyMode mode) => mode != LazyThreadSafetyMode.PublicationOnly;

        internal static T CreateViaDefaultConstructor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()
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
    [DebuggerDisplay("ThreadSafetyMode = {Mode}, IsValueCreated = {IsValueCreated}, IsValueFaulted = {IsValueFaulted}, Value = {ValueForDebugDisplay}")]
    public class Lazy<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>
    {
        private static T CreateViaDefaultConstructor() => LazyHelper.CreateViaDefaultConstructor<T>();

        // _state, a volatile reference, is set to null after _value has been set
        private volatile LazyHelper? _state;

        // we ensure that _factory when finished is set to null to allow garbage collector to clean up
        // any referenced items
        private Func<T>? _factory;

        // _value eventually stores the lazily created value. It is valid when _state = null.
        private T? _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T}"/> class that
        /// uses <typeparamref name="T"/>'s default constructor for lazy initialization.
        /// </summary>
        /// <remarks>
        /// An instance created with this constructor may be used concurrently from multiple threads.
        /// </remarks>
        public Lazy()
            : this(null, LazyThreadSafetyMode.ExecutionAndPublication, useDefaultConstructor: true, cacheExceptions: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T}"/> class that
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
        /// Initializes a new instance of the <see cref="Lazy{T}"/> class that uses a
        /// specified initialization function.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="Func{T}"/> invoked to produce the lazily-initialized value when it is
        /// needed.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is a null
        /// reference (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <remarks>
        /// An instance created with this constructor may be used concurrently from multiple threads.
        /// </remarks>
        public Lazy(Func<T> valueFactory)
            : this(valueFactory, LazyThreadSafetyMode.ExecutionAndPublication, useDefaultConstructor: false, cacheExceptions: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T}"/>
        /// class that uses <typeparamref name="T"/>'s default constructor and a specified thread-safety mode.
        /// </summary>
        /// <param name="isThreadSafe">true if this instance should be usable by multiple threads concurrently; false if the instance will only be used by one thread at a time.
        /// </param>
        public Lazy(bool isThreadSafe) :
            this(null, LazyHelper.GetModeFromIsThreadSafe(isThreadSafe), useDefaultConstructor: true, cacheExceptions: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T}"/>
        /// class that uses <typeparamref name="T"/>'s default constructor and a specified thread-safety mode.
        /// </summary>
        /// <param name="mode">The lazy thread-safety mode</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> mode contains an invalid value</exception>
        public Lazy(LazyThreadSafetyMode mode) :
            this(null, mode, useDefaultConstructor: true, cacheExceptions: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T}"/> class
        /// that uses a specified initialization function and a specified thread-safety mode.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="Func{T}"/> invoked to produce the lazily-initialized value when it is needed.
        /// </param>
        /// <param name="isThreadSafe">true if this instance should be usable by multiple threads concurrently; false if the instance will only be used by one thread at a time.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is
        /// a null reference (<see langword="Nothing" /> in Visual Basic).</exception>
        public Lazy(Func<T> valueFactory, bool isThreadSafe) :
            this(valueFactory, LazyHelper.GetModeFromIsThreadSafe(isThreadSafe), useDefaultConstructor: false, cacheExceptions: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T}"/> class
        /// that uses a specified initialization function and a specified thread-safety mode.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="Func{T}"/> invoked to produce the lazily-initialized value when it is needed.
        /// </param>
        /// <param name="mode">The lazy thread-safety mode.</param>
        /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is
        /// a null reference (<see langword="Nothing" /> in Visual Basic).</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> mode contains an invalid value.</exception>
        public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode)
            : this(valueFactory, mode, useDefaultConstructor: false, cacheExceptions: LazyHelper.CachesExceptionsByDefault(mode))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Lazy{T}"/> class
        /// that uses a specified initialization function and thread-safety mode, and that specifies whether
        /// exceptions thrown by the initialization function are cached.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="Func{T}"/> invoked to produce the lazily-initialized value when it is needed.
        /// </param>
        /// <param name="mode">The lazy thread-safety mode.</param>
        /// <param name="cacheExceptions">
        /// <see langword="true"/> to cache an exception thrown by <paramref name="valueFactory"/> and rethrow it on every subsequent access to <see cref="Value"/>;
        /// <see langword="false"/> to not cache it, so that a subsequent access to <see cref="Value"/> invokes <paramref name="valueFactory"/> again.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="valueFactory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> contains an invalid value.</exception>
        /// <remarks>
        /// <para>
        /// <paramref name="cacheExceptions"/> controls exception caching for every <paramref name="mode"/>. In comparison,
        /// <see cref="Lazy{T}(Func{T}, LazyThreadSafetyMode)"/> caches exceptions for <see cref="LazyThreadSafetyMode.None"/> and
        /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>, but not for <see cref="LazyThreadSafetyMode.PublicationOnly"/>.
        /// </para>
        /// <para>
        /// With <see cref="LazyThreadSafetyMode.PublicationOnly"/> and <paramref name="cacheExceptions"/> set to <see langword="true"/>,
        /// the first thread to complete <paramref name="valueFactory"/> publishes its result, whether a value or an exception, and
        /// all accesses to <see cref="Value"/> then observe that result.
        /// </para>
        /// </remarks>
        public Lazy(Func<T> valueFactory, LazyThreadSafetyMode mode, bool cacheExceptions)
            : this(valueFactory, mode, useDefaultConstructor: false, cacheExceptions)
        {
        }

        private Lazy(Func<T>? valueFactory, LazyThreadSafetyMode mode, bool useDefaultConstructor, bool cacheExceptions)
        {
            if (!useDefaultConstructor)
            {
                ArgumentNullException.ThrowIfNull(valueFactory);
            }

            _factory = valueFactory;
            _state = useDefaultConstructor ?
                LazyHelper.Create(mode, useDefaultConstructor: true) :
                LazyHelper.CreateViaFactory(mode, cacheExceptions);
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
                Func<T> factory = _factory ?? throw new InvalidOperationException(SR.Lazy_Value_RecursiveCallsToValue);
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

        private void ViaFactoryNoExceptionCaching()
        {
            // A null _factory means that the factory is currently running and accessed Value recursively.
            // That is reported without restoring anything, as the outer invocation still owns the factory.
            Func<T> factory = _factory ?? throw new InvalidOperationException(SR.Lazy_Value_RecursiveCallsToValue);
            _factory = null;

            try
            {
                _value = factory();
            }
            catch
            {
                // Don't cache the exception: restore the factory so that the next access to Value invokes it again.
                _factory = factory;
                throw;
            }

            _state = null; // volatile write, must occur after setting _value
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

        private void ExecutionAndPublicationNoExceptionCaching(LazyHelper executionAndPublication)
        {
            lock (executionAndPublication)
            {
                // it's possible for multiple calls to have piled up behind the lock, so we need to check
                // to see if the ExecutionAndPublication object is still the current implementation.
                // If a previous invocation of the factory threw, the state is unchanged and the factory is invoked again.
                if (ReferenceEquals(_state, executionAndPublication))
                {
                    ViaFactoryNoExceptionCaching();
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

        private void PublicationOnlyViaFactoryWithExceptionCaching(LazyHelper initializer)
        {
            Func<T>? factory = _factory;
            if (factory == null)
            {
                // Another thread published a value and is completing the publication.
                PublicationOnlyWaitForOtherThreadToPublish();
                return;
            }

            T possibleValue;
            try
            {
                possibleValue = factory();
            }
            catch (Exception exception)
            {
                // The first thread to complete publishes its result, whether a value or an exception. If another thread
                // already published, this exception is discarded, and the caller observes the published result instead.
                // _factory isn't cleared here, as a thread that observes a null factory waits for a value to be published.
                Interlocked.CompareExchange(ref _state, new LazyHelper(LazyThreadSafetyMode.PublicationOnly, exception), initializer);
                return;
            }

            PublicationOnly(initializer, possibleValue);
        }

        private void PublicationOnlyWaitForOtherThreadToPublish()
        {
            SpinWait spinWait = default;
            while (_state is not null)
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
                        ExecutionAndPublication(state, useDefaultConstructor: true);
                        break;

                    case LazyState.ExecutionAndPublicationViaFactory:
                        ExecutionAndPublication(state, useDefaultConstructor: false);
                        break;

                    case LazyState.NoneViaFactoryNoExceptionCaching:
                        ViaFactoryNoExceptionCaching();
                        break;

                    case LazyState.ExecutionAndPublicationViaFactoryNoExceptionCaching:
                        ExecutionAndPublicationNoExceptionCaching(state);
                        break;

                    case LazyState.PublicationOnlyViaFactoryWithExceptionCaching:
                        PublicationOnlyViaFactoryWithExceptionCaching(state);
                        break;

                    default:
                        state.ThrowException();
                        break;
                }
            }
            return Value;
        }

        /// <summary>Creates and returns a string representation of this instance.</summary>
        /// <returns>The result of calling <see cref="object.ToString"/> on the <see
        /// cref="Value"/>.</returns>
        /// <exception cref="NullReferenceException">
        /// The <see cref="Value"/> is null.
        /// </exception>
        public override string? ToString()
        {
            return IsValueCreated ?
                Value!.ToString() : // Throws NullReferenceException as if caller called ToString on the value itself
                SR.Lazy_ToString_ValueNotCreated;
        }

        /// <summary>Gets the value of the Lazy&lt;T&gt; for debugging display purposes.</summary>
        internal T? ValueForDebugDisplay
        {
            get
            {
                if (!IsValueCreated)
                {
                    return default;
                }
                return _value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance may be used concurrently from multiple threads.
        /// </summary>
        internal LazyThreadSafetyMode? Mode => LazyHelper.GetMode(_state);

        /// <summary>Gets a value indicating whether initialization of this instance threw an exception that is cached.</summary>
        /// <value>
        /// <see langword="true"/> if initialization threw an exception that is rethrown on every access to <see cref="Value"/>;
        /// otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// When exceptions aren't cached, for example with <see cref="LazyThreadSafetyMode.PublicationOnly"/> by default or when the instance
        /// was created with <c>cacheExceptions: false</c>, a failed initialization isn't faulted, because the next access to <see cref="Value"/>
        /// retries it.
        /// </remarks>
        public bool IsValueFaulted => LazyHelper.GetIsValueFaulted(_state);

        /// <summary>Gets a value indicating whether the <see cref="Lazy{T}"/> has been initialized.
        /// </summary>
        /// <value>true if the <see cref="Lazy{T}"/> instance has been initialized;
        /// otherwise, false.</value>
        /// <remarks>
        /// The initialization of a <see cref="Lazy{T}"/> instance may result in either
        /// a value being produced or an exception being thrown.  If an exception goes unhandled during initialization,
        /// <see cref="IsValueCreated"/> will return false.
        /// </remarks>
        public bool IsValueCreated => _state == null;

        /// <summary>Gets the lazily initialized value of the current <see
        /// cref="Lazy{T}"/>.</summary>
        /// <value>The lazily initialized value of the current <see
        /// cref="Lazy{T}"/>.</value>
        /// <exception cref="MissingMemberException">
        /// The <see cref="Lazy{T}"/> was initialized to use the default constructor
        /// of the type being lazily initialized, and that type does not have a public, parameterless constructor.
        /// </exception>
        /// <exception cref="MemberAccessException">
        /// The <see cref="Lazy{T}"/> was initialized to use the default constructor
        /// of the type being lazily initialized, and permissions to access the constructor were missing.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="Lazy{T}"/> was constructed with the <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> or
        /// <see cref="LazyThreadSafetyMode.None"/>  and the initialization function attempted to access <see cref="Value"/> on this instance.
        /// </exception>
        /// <remarks>
        /// If <see cref="IsValueCreated"/> is false, accessing <see cref="Value"/> will force initialization.
        /// Please <see cref="LazyThreadSafetyMode"/> for more information on how <see cref="Lazy{T}"/> will behave if an exception is thrown
        /// from initialization delegate.
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Value => _state == null ? _value! : CreateValue();
    }

    /// <summary>A debugger view of the Lazy&lt;T&gt; to surface additional debugging properties and
    /// to ensure that the Lazy&lt;T&gt; does not become initialized if it was not already.</summary>
    internal sealed class LazyDebugView<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>
    {
        // The Lazy object being viewed.
        private readonly Lazy<T> _lazy;

        /// <summary>Constructs a new debugger view object for the provided Lazy object.</summary>
        /// <param name="lazy">A Lazy object to browse in the debugger.</param>
        public LazyDebugView(Lazy<T> lazy)
        {
            _lazy = lazy;
        }

        /// <summary>Returns whether the Lazy object is initialized or not.</summary>
        public bool IsValueCreated => _lazy.IsValueCreated;

        /// <summary>Returns the value of the Lazy object.</summary>
        public T? Value => _lazy.ValueForDebugDisplay;

        /// <summary>Returns the execution mode of the Lazy object.</summary>
        public LazyThreadSafetyMode? Mode => _lazy.Mode;

        /// <summary>Returns whether the value creation of the Lazy object is faulted or not.</summary>
        public bool IsValueFaulted => _lazy.IsValueFaulted;
    }
}
