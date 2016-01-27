// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// a set of lightweight static helpers for lazy initialization.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-


using System.Security.Permissions;
using System.Diagnostics.Contracts;
namespace System.Threading
{

    /// <summary>
    /// Specifies how a <see cref="T:System.Threading.Lazy{T}"/> instance should synchronize access among multiple threads.
    /// </summary>
    public enum LazyThreadSafetyMode
    {
        /// <summary>
        /// This mode makes no guarantees around the thread-safety of the <see cref="T:System.Threading.Lazy{T}"/> instance.  If used from multiple threads, the behavior of the <see cref="T:System.Threading.Lazy{T}"/> is undefined.
        /// This mode should be used when a <see cref="T:System.Threading.Lazy{T}"/> is guaranteed to never be initialized from more than one thread simultaneously and high performance is crucial. 
        /// If valueFactory throws an exception when the <see cref="T:System.Threading.Lazy{T}"/> is initialized, the exception will be cached and returned on subsequent accesses to Value. Also, if valueFactory recursively
        /// accesses Value on this <see cref="T:System.Threading.Lazy{T}"/> instance, a <see cref="T:System.InvalidOperationException"/> will be thrown.
        /// </summary>
        None,

        /// <summary>
        /// When multiple threads attempt to simultaneously initialize a <see cref="T:System.Threading.Lazy{T}"/> instance, this mode allows each thread to execute the
        /// valueFactory but only the first thread to complete initialization will be allowed to set the final value of the  <see cref="T:System.Threading.Lazy{T}"/>.
        /// Once initialized successfully, any future calls to Value will return the cached result.  If valueFactory throws an exception on any thread, that exception will be
        /// propagated out of Value. If any thread executes valueFactory without throwing an exception and, therefore, successfully sets the value, that value will be returned on
        /// subsequent accesses to Value from any thread.  If no thread succeeds in setting the value, IsValueCreated will remain false and subsequent accesses to Value will result in
        /// the valueFactory delegate re-executing.  Also, if valueFactory recursively accesses Value on this  <see cref="T:System.Threading.Lazy{T}"/> instance, an exception will NOT be thrown.
        /// </summary>
        PublicationOnly,

        /// <summary>
        /// This mode uses locks to ensure that only a single thread can initialize a <see cref="T:System.Threading.Lazy{T}"/> instance in a thread-safe manner.  In general,
        /// taken if this mode is used in conjunction with a <see cref="T:System.Threading.Lazy{T}"/> valueFactory delegate that uses locks internally, a deadlock can occur if not
        /// handled carefully.  If valueFactory throws an exception when the<see cref="T:System.Threading.Lazy{T}"/> is initialized, the exception will be cached and returned on
        /// subsequent accesses to Value. Also, if valueFactory recursively accesses Value on this <see cref="T:System.Threading.Lazy{T}"/> instance, a  <see cref="T:System.InvalidOperationException"/> will be thrown.
        /// </summary>
        ExecutionAndPublication
    }
    /// <summary>
    /// Provides lazy initialization routines.
    /// </summary>
    /// <remarks>
    /// These routines avoid needing to allocate a dedicated, lazy-initialization instance, instead using
    /// references to ensure targets have been initialized as they are accessed.
    /// </remarks>
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public static class LazyInitializer
    {
        /// <summary>
        /// Initializes a target reference type with the type's default constructor if the target has not
        /// already been initialized.
        /// </summary>
        /// <typeparam name="T">The refence type of the reference to be initialized.</typeparam>
        /// <param name="target">A reference of type <typeparamref name="T"/> to initialize if it has not
        /// already been initialized.</param>
        /// <returns>The initialized reference of type <typeparamref name="T"/>.</returns>
        /// <exception cref="T:System.MissingMemberException">Type <typeparamref name="T"/> does not have a default
        /// constructor.</exception>
        /// <exception cref="T:System.MemberAccessException">
        /// Permissions to access the constructor of type <typeparamref name="T"/> were missing.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method may only be used on reference types. To ensure initialization of value
        /// types, see other overloads of EnsureInitialized.
        /// </para>
        /// <para>
        /// This method may be used concurrently by multiple threads to initialize <paramref name="target"/>.  
        /// In the event that multiple threads access this method concurrently, multiple instances of <typeparamref name="T"/>
        /// may be created, but only one will be stored into <paramref name="target"/>. In such an occurrence, this method will not dispose of the
        /// objects that were not stored.  If such objects must be disposed, it is up to the caller to determine 
        /// if an object was not used and to then dispose of the object appropriately.
        /// </para>
        /// </remarks>
        public static T EnsureInitialized<T>(ref T target) where T : class
        {
            // Fast path.
            if (Volatile.Read<T>(ref target) != null)
            {
                return target;
            }

            return EnsureInitializedCore<T>(ref target, LazyHelpers<T>.s_activatorFactorySelector);
        }

        /// <summary>
        /// Initializes a target reference type using the specified function if it has not already been
        /// initialized.
        /// </summary>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <param name="target">The reference of type <typeparamref name="T"/> to initialize if it has not
        /// already been initialized.</param>
        /// <param name="valueFactory">The <see cref="T:System.Func{T}"/> invoked to initialize the
        /// reference.</param>
        /// <returns>The initialized reference of type <typeparamref name="T"/>.</returns>
        /// <exception cref="T:System.MissingMemberException">Type <typeparamref name="T"/> does not have a
        /// default constructor.</exception>
        /// <exception cref="T:System.InvalidOperationException"><paramref name="valueFactory"/> returned
        /// null.</exception>
        /// <remarks>
        /// <para>
        /// This method may only be used on reference types, and <paramref name="valueFactory"/> may
        /// not return a null reference (Nothing in Visual Basic). To ensure initialization of value types or
        /// to allow null reference types, see other overloads of EnsureInitialized.
        /// </para>
        /// <para>
        /// This method may be used concurrently by multiple threads to initialize <paramref name="target"/>.  
        /// In the event that multiple threads access this method concurrently, multiple instances of <typeparamref name="T"/>
        /// may be created, but only one will be stored into <paramref name="target"/>. In such an occurrence, this method will not dispose of the
        /// objects that were not stored.  If such objects must be disposed, it is up to the caller to determine 
        /// if an object was not used and to then dispose of the object appropriately.
        /// </para>
        /// </remarks>
        public static T EnsureInitialized<T>(ref T target, Func<T> valueFactory) where T : class
        {
            // Fast path.
            if (Volatile.Read<T>(ref target) != null)
            {
                return target;
            }

            return EnsureInitializedCore<T>(ref target, valueFactory);
        }

        /// <summary>
        /// Initialize the target using the given delegate (slow path).
        /// </summary>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <param name="target">The variable that need to be initialized</param>
        /// <param name="valueFactory">The delegate that will be executed to initialize the target</param>
        /// <returns>The initialized variable</returns>
        private static T EnsureInitializedCore<T>(ref T target, Func<T> valueFactory) where T : class
        {
            T value = valueFactory();
            if (value == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Lazy_StaticInit_InvalidOperation"));
            }

            Interlocked.CompareExchange(ref target, value, null);
            Contract.Assert(target != null);
            return target;
        }


        /// <summary>
        /// Initializes a target reference or value type with its default constructor if it has not already
        /// been initialized.
        /// </summary>
        /// <typeparam name="T">The type of the reference to be initialized.</typeparam>
        /// <param name="target">A reference or value of type <typeparamref name="T"/> to initialize if it
        /// has not already been initialized.</param>
        /// <param name="initialized">A reference to a boolean that determines whether the target has already
        /// been initialized.</param>
        /// <param name="syncLock">A reference to an object used as the mutually exclusive lock for initializing
        /// <paramref name="target"/>. If <paramref name="syncLock"/> is null, a new object will be instantiated.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock)
        {
            // Fast path.
            if (Volatile.Read(ref initialized))
            {
                return target;
            }

            return EnsureInitializedCore<T>(ref target, ref initialized, ref syncLock, LazyHelpers<T>.s_activatorFactorySelector);
        }

        /// <summary>
        /// Initializes a target reference or value type with a specified function if it has not already been
        /// initialized.
        /// </summary>
        /// <typeparam name="T">The type of the reference to be initialized.</typeparam>
        /// <param name="target">A reference or value of type <typeparamref name="T"/> to initialize if it
        /// has not already been initialized.</param>
        /// <param name="initialized">A reference to a boolean that determines whether the target has already
        /// been initialized.</param>
        /// <param name="syncLock">A reference to an object used as the mutually exclusive lock for initializing
        /// <paramref name="target"/>. If <paramref name="syncLock"/> is null, a new object will be instantiated.</param>
        /// <param name="valueFactory">The <see cref="T:System.Func{T}"/> invoked to initialize the
        /// reference or value.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        public static T EnsureInitialized<T>(ref T target, ref bool initialized, ref object syncLock, Func<T> valueFactory)
        {
            // Fast path.
            if (Volatile.Read(ref initialized))
            {
                return target;
            }


            return EnsureInitializedCore<T>(ref target, ref initialized, ref syncLock, valueFactory);
        }

        /// <summary>
        /// Ensure the target is initialized and return the value (slow path). This overload permits nulls
        /// and also works for value type targets. Uses the supplied function to create the value.
        /// </summary>
        /// <typeparam name="T">The type of target.</typeparam>
        /// <param name="target">A reference to the target to be initialized.</param>
        /// <param name="initialized">A reference to a location tracking whether the target has been initialized.</param>
        /// <param name="syncLock">A reference to a location containing a mutual exclusive lock. If <paramref name="syncLock"/> is null, 
        /// a new object will be instantiated.</param>
        /// <param name="valueFactory">
        /// The <see cref="T:System.Func{T}"/> to invoke in order to produce the lazily-initialized value.
        /// </param>
        /// <returns>The initialized object.</returns>
        private static T EnsureInitializedCore<T>(ref T target, ref bool initialized, ref object syncLock, Func<T> valueFactory)
        {
            // Lazily initialize the lock if necessary.
            object slock = syncLock;
            if (slock == null)
            {
                object newLock = new object();
                slock = Interlocked.CompareExchange(ref syncLock, newLock, null);
                if (slock == null)
                {
                    slock = newLock;
                }
            }

            // Now double check that initialization is still required.
            lock (slock)
            {
                if (!Volatile.Read(ref initialized))
                {
                    target = valueFactory();
                    Volatile.Write(ref initialized, true);
                }
            }

            return target;
        }

    }

    // Caches the activation selector function to avoid delegate allocations.
    static class LazyHelpers<T>
    {
        internal static Func<T> s_activatorFactorySelector = new Func<T>(ActivatorFactorySelector);

        private static T ActivatorFactorySelector()
        {
            try
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            catch (MissingMethodException)
            {
                throw new MissingMemberException(Environment.GetResourceString("Lazy_CreateValue_NoParameterlessCtorForT"));
            }
        }
    }
}
