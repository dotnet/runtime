// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// a set of lightweight static helpers for lazy initialization.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Provides lazy initialization routines.
    /// </summary>
    /// <remarks>
    /// These routines avoid needing to allocate a dedicated, lazy-initialization instance, instead using
    /// references to ensure targets have been initialized as they are accessed.
    /// </remarks>
    public static class LazyInitializer
    {
        /// <summary>
        /// Initializes a target reference type with the type's default constructor if the target has not
        /// already been initialized.
        /// </summary>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
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
        public static T EnsureInitialized<T>(ref T target) where T : class =>
            Volatile.Read(ref target) ?? EnsureInitializedCore(ref target);

        /// <summary>
        /// Initializes a target reference type with the type's default constructor (slow path)
        /// </summary>
        /// <typeparam name="T">The reference type of the reference to be initialized.</typeparam>
        /// <param name="target">The variable that need to be initialized</param>
        /// <returns>The initialized variable</returns>
        private static T EnsureInitializedCore<T>(ref T target) where T : class
        {
            try
            {
                Interlocked.CompareExchange(ref target, Activator.CreateInstance<T>(), null);
            }
            catch (MissingMethodException)
            {
                throw new MissingMemberException(SR.Lazy_CreateValue_NoParameterlessCtorForT);
            }

            Debug.Assert(target != null);
            return target;
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
        public static T EnsureInitialized<T>(ref T target, Func<T> valueFactory) where T : class =>
            Volatile.Read(ref target) ?? EnsureInitializedCore(ref target, valueFactory);

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
                throw new InvalidOperationException(SR.Lazy_StaticInit_InvalidOperation);
            }

            Interlocked.CompareExchange(ref target, value, null);
            Debug.Assert(target != null);
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

            return EnsureInitializedCore(ref target, ref initialized, ref syncLock);
        }

        /// <summary>
        /// Ensure the target is initialized and return the value (slow path). This overload permits nulls
        /// and also works for value type targets. Uses the type's default constructor to create the value.
        /// </summary>
        /// <typeparam name="T">The type of target.</typeparam>
        /// <param name="target">A reference to the target to be initialized.</param>
        /// <param name="initialized">A reference to a location tracking whether the target has been initialized.</param>
        /// <param name="syncLock">A reference to a location containing a mutual exclusive lock. If <paramref name="syncLock"/> is null, 
        /// a new object will be instantiated.</param>
        /// </param>
        /// <returns>The initialized object.</returns>
        private static T EnsureInitializedCore<T>(ref T target, ref bool initialized, ref object syncLock)
        {
            // Lazily initialize the lock if necessary and then double check if initialization is still required.
            lock (EnsureLockInitialized(ref syncLock))
            {
                if (!Volatile.Read(ref initialized))
                {
                    try
                    {
                        target = Activator.CreateInstance<T>();
                    }
                    catch (MissingMethodException)
                    {
                        throw new MissingMemberException(SR.Lazy_CreateValue_NoParameterlessCtorForT);
                    }

                    Volatile.Write(ref initialized, true);
                }
            }

            return target;
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

            return EnsureInitializedCore(ref target, ref initialized, ref syncLock, valueFactory);
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
            // Lazily initialize the lock if necessary and then double check if initialization is still required.
            lock (EnsureLockInitialized(ref syncLock))
            {
                if (!Volatile.Read(ref initialized))
                {
                    target = valueFactory();
                    Volatile.Write(ref initialized, true);
                }
            }

            return target;
        }

        /// <summary>
        /// Initializes a target reference type with a specified function if it has not already been initialized.
        /// </summary>
        /// <typeparam name="T">The type of the reference to be initialized. Has to be reference type.</typeparam>
        /// <param name="target">A reference of type <typeparamref name="T"/> to initialize if it has not already been initialized.</param>
        /// <param name="syncLock">A reference to an object used as the mutually exclusive lock for initializing
        /// <paramref name="target"/>. If <paramref name="syncLock"/> is null, a new object will be instantiated.</param>
        /// <param name="valueFactory">The <see cref="T:System.Func{T}"/> invoked to initialize the reference.</param>
        /// <returns>The initialized value of type <typeparamref name="T"/>.</returns>
        public static T EnsureInitialized<T>(ref T target, ref object syncLock, Func<T> valueFactory) where T : class =>
            Volatile.Read(ref target) ?? EnsureInitializedCore(ref target, ref syncLock, valueFactory);

        /// <summary>
        /// Ensure the target is initialized and return the value (slow path). This overload works only for reference type targets.
        /// Uses the supplied function to create the value.
        /// </summary>
        /// <typeparam name="T">The type of target. Has to be reference type.</typeparam>
        /// <param name="target">A reference to the target to be initialized.</param>
        /// <param name="syncLock">A reference to a location containing a mutual exclusive lock. If <paramref name="syncLock"/> is null, 
        /// a new object will be instantiated.</param>
        /// <param name="valueFactory">
        /// The <see cref="T:System.Func{T}"/> to invoke in order to produce the lazily-initialized value.
        /// </param>
        /// <returns>The initialized object.</returns>
        private static T EnsureInitializedCore<T>(ref T target, ref object syncLock, Func<T> valueFactory) where T : class
        {
            // Lazily initialize the lock if necessary and then double check if initialization is still required.
            lock (EnsureLockInitialized(ref syncLock))
            {
                if (Volatile.Read(ref target) == null)
                {
                    Volatile.Write(ref target, valueFactory());
                    if (target == null)
                    {
                        throw new InvalidOperationException(SR.Lazy_StaticInit_InvalidOperation);
                    }
                }
            }

            return target;
        }

        /// <summary>
        /// Ensure the lock object is initialized.
        /// </summary>
        /// <param name="syncLock">A reference to a location containing a mutual exclusive lock. If <paramref name="syncLock"/> is null,
        /// a new object will be instantiated.</param>
        /// <returns>Initialized lock object.</returns>
        private static object EnsureLockInitialized(ref object syncLock) =>
            syncLock ??
            Interlocked.CompareExchange(ref syncLock, new object(), null) ??
            syncLock;
    }
}
