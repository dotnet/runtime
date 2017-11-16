// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Contains interlocked exchange mechanisms for immutable collections.
    /// </summary>
    public static class ImmutableInterlocked
    {
        /// <summary>
        /// Mutates a value in-place with optimistic locking transaction semantics
        /// via a specified transformation function.
        /// The transformation is retried as many times as necessary to win the optimistic locking race.
        /// </summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <param name="location">
        /// The variable or field to be changed, which may be accessed by multiple threads.
        /// </param>
        /// <param name="transformer">
        /// A function that mutates the value. This function should be side-effect free, 
        /// as it may run multiple times when races occur with other threads.</param>
        /// <returns>
        /// <c>true</c> if the location's value is changed by applying the result of the 
        /// <paramref name="transformer"/> function;
        /// <c>false</c> if the location's value remained the same because the last 
        /// invocation of <paramref name="transformer"/> returned the existing value.
        /// </returns>
        public static bool Update<T>(ref T location, Func<T, T> transformer) where T : class
        {
            Requires.NotNull(transformer, nameof(transformer));

            bool successful;
            T oldValue = Volatile.Read(ref location);
            do
            {
                T newValue = transformer(oldValue);
                if (ReferenceEquals(oldValue, newValue))
                {
                    // No change was actually required.
                    return false;
                }

                T interlockedResult = Interlocked.CompareExchange(ref location, newValue, oldValue);
                successful = ReferenceEquals(oldValue, interlockedResult);
                oldValue = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
            while (!successful);

            return true;
        }

        /// <summary>
        /// Mutates a value in-place with optimistic locking transaction semantics
        /// via a specified transformation function.
        /// The transformation is retried as many times as necessary to win the optimistic locking race.
        /// </summary>
        /// <typeparam name="T">The type of data.</typeparam>
        /// <typeparam name="TArg">The type of argument passed to the <paramref name="transformer"/>.</typeparam>
        /// <param name="location">
        /// The variable or field to be changed, which may be accessed by multiple threads.
        /// </param>
        /// <param name="transformer">
        /// A function that mutates the value. This function should be side-effect free, 
        /// as it may run multiple times when races occur with other threads.</param>
        /// <param name="transformerArgument">The argument to pass to <paramref name="transformer"/>.</param>
        /// <returns>
        /// <c>true</c> if the location's value is changed by applying the result of the 
        /// <paramref name="transformer"/> function;
        /// <c>false</c> if the location's value remained the same because the last 
        /// invocation of <paramref name="transformer"/> returned the existing value.
        /// </returns>
        public static bool Update<T, TArg>(ref T location, Func<T, TArg, T> transformer, TArg transformerArgument) where T : class
        {
            Requires.NotNull(transformer, nameof(transformer));

            bool successful;
            T oldValue = Volatile.Read(ref location);
            do
            {
                T newValue = transformer(oldValue, transformerArgument);
                if (ReferenceEquals(oldValue, newValue))
                {
                    // No change was actually required.
                    return false;
                }

                T interlockedResult = Interlocked.CompareExchange(ref location, newValue, oldValue);
                successful = ReferenceEquals(oldValue, interlockedResult);
                oldValue = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
            while (!successful);

            return true;
        }

        #region ImmutableArray<T> members

        /// <summary>
        /// Assigns a field or variable containing an immutable array to the specified value and returns the previous value.
        /// </summary>
        /// <typeparam name="T">The type of element stored by the array.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <returns>The prior value at the specified <paramref name="location"/>.</returns>
        public static ImmutableArray<T> InterlockedExchange<T>(ref ImmutableArray<T> location, ImmutableArray<T> value)
        {
            return new ImmutableArray<T>(Interlocked.Exchange(ref location.array, value.array));
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable array to the specified value
        /// if it is currently equal to another specified value. Returns the previous value.
        /// </summary>
        /// <typeparam name="T">The type of element stored by the array.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <param name="comparand">The value to check equality for before assigning.</param>
        /// <returns>The prior value at the specified <paramref name="location"/>.</returns>
        public static ImmutableArray<T> InterlockedCompareExchange<T>(ref ImmutableArray<T> location, ImmutableArray<T> value, ImmutableArray<T> comparand)
        {
            return new ImmutableArray<T>(Interlocked.CompareExchange(ref location.array, value.array, comparand.array));
        }

        /// <summary>
        /// Assigns a field or variable containing an immutable array to the specified value
        /// if it is has not yet been initialized.
        /// </summary>
        /// <typeparam name="T">The type of element stored by the array.</typeparam>
        /// <param name="location">The field or local variable to change.</param>
        /// <param name="value">The new value to assign.</param>
        /// <returns>True if the field was assigned the specified value; <c>false</c> if it was previously initialized.</returns>
        public static bool InterlockedInitialize<T>(ref ImmutableArray<T> location, ImmutableArray<T> value)
        {
            return InterlockedCompareExchange(ref location, value, default(ImmutableArray<T>)).IsDefault;
        }

        #endregion

        #region ImmutableDictionary<TKey, TValue> members

        /// <summary>
        /// Obtains the value for the specified key from a dictionary, or adds a new value to the dictionary where the key did not previously exist.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <typeparam name="TArg">The type of argument supplied to the value factory.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key for the value to retrieve or add.</param>
        /// <param name="valueFactory">The function to execute to obtain the value to insert into the dictionary if the key is not found.</param>
        /// <param name="factoryArgument">The argument to pass to the value factory.</param>
        /// <returns>The value obtained from the dictionary or <paramref name="valueFactory"/> if it was not present.</returns>
        public static TValue GetOrAdd<TKey, TValue, TArg>(ref ImmutableDictionary<TKey, TValue> location, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            Requires.NotNull(valueFactory, nameof(valueFactory));

            var map = Volatile.Read(ref location);
            Requires.NotNull(map, nameof(location));

            TValue value;
            if (map.TryGetValue(key, out value))
            {
                return value;
            }

            value = valueFactory(key, factoryArgument);
            return GetOrAdd(ref location, key, value);
        }

        /// <summary>
        /// Obtains the value for the specified key from a dictionary, or adds a new value to the dictionary where the key did not previously exist.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key for the value to retrieve or add.</param>
        /// <param name="valueFactory">
        /// The function to execute to obtain the value to insert into the dictionary if the key is not found.
        /// This delegate will not be invoked more than once.
        /// </param>
        /// <returns>The value obtained from the dictionary or <paramref name="valueFactory"/> if it was not present.</returns>
        public static TValue GetOrAdd<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, Func<TKey, TValue> valueFactory)
        {
            Requires.NotNull(valueFactory, nameof(valueFactory));

            var map = Volatile.Read(ref location);
            Requires.NotNull(map, nameof(location));

            TValue value;
            if (map.TryGetValue(key, out value))
            {
                return value;
            }

            value = valueFactory(key);
            return GetOrAdd(ref location, key, value);
        }

        /// <summary>
        /// Obtains the value for the specified key from a dictionary, or adds a new value to the dictionary where the key did not previously exist.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key for the value to retrieve or add.</param>
        /// <param name="value">The value to add to the dictionary if one is not already present.</param>
        /// <returns>The value obtained from the dictionary or <paramref name="value"/> if it was not present.</returns>
        public static TValue GetOrAdd<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, TValue value)
        {
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));
                TValue oldValue;
                if (priorCollection.TryGetValue(key, out oldValue))
                {
                    return oldValue;
                }

                var updatedCollection = priorCollection.Add(key, value);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
            while (!successful);

            // We won the race-condition and have updated the collection.
            // Return the value that is in the collection (as of the Interlocked operation).
            return value;
        }

        /// <summary>
        /// Obtains the value from a dictionary after having added it or updated an existing entry.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key for the value to add or update.</param>
        /// <param name="addValueFactory">The function that receives the key and returns a new value to add to the dictionary when no value previously exists.</param>
        /// <param name="updateValueFactory">The function that receives the key and prior value and returns the new value with which to update the dictionary.</param>
        /// <returns>The added or updated value.</returns>
        public static TValue AddOrUpdate<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            Requires.NotNull(addValueFactory, nameof(addValueFactory));
            Requires.NotNull(updateValueFactory, nameof(updateValueFactory));

            TValue newValue;
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                TValue oldValue;
                if (priorCollection.TryGetValue(key, out oldValue))
                {
                    newValue = updateValueFactory(key, oldValue);
                }
                else
                {
                    newValue = addValueFactory(key);
                }

                var updatedCollection = priorCollection.SetItem(key, newValue);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
            while (!successful);

            // We won the race-condition and have updated the collection.
            // Return the value that is in the collection (as of the Interlocked operation).
            return newValue;
        }

        /// <summary>
        /// Obtains the value from a dictionary after having added it or updated an existing entry.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key for the value to add or update.</param>
        /// <param name="addValue">The value to use if no previous value exists.</param>
        /// <param name="updateValueFactory">The function that receives the key and prior value and returns the new value with which to update the dictionary.</param>
        /// <returns>The added or updated value.</returns>
        public static TValue AddOrUpdate<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            Requires.NotNull(updateValueFactory, nameof(updateValueFactory));

            TValue newValue;
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                TValue oldValue;
                if (priorCollection.TryGetValue(key, out oldValue))
                {
                    newValue = updateValueFactory(key, oldValue);
                }
                else
                {
                    newValue = addValue;
                }

                var updatedCollection = priorCollection.SetItem(key, newValue);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            }
            while (!successful);

            // We won the race-condition and have updated the collection.
            // Return the value that is in the collection (as of the Interlocked operation).
            return newValue;
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary if no colliding key already exists in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key to add, if is not already defined in the dictionary.</param>
        /// <param name="value">The value to add.</param>
        /// <returns><c>true</c> if the key was not previously set in the dictionary and the value was set; <c>false</c> otherwise.</returns>
        public static bool TryAdd<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, TValue value)
        {
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                if (priorCollection.ContainsKey(key))
                {
                    return false;
                }

                var updatedCollection = priorCollection.Add(key, value);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            } while (!successful);

            return true;
        }

        /// <summary>
        /// Sets the specified key to the given value if the key already is set to a specific value.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key to update.</param>
        /// <param name="newValue">The new value to set.</param>
        /// <param name="comparisonValue">The value that must already be set in the dictionary in order for the update to succeed.</param>
        /// <returns><c>true</c> if the key and comparison value were present in the dictionary and the update was made; <c>false</c> otherwise.</returns>
        public static bool TryUpdate<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, TValue newValue, TValue comparisonValue)
        {
            var valueComparer = EqualityComparer<TValue>.Default;
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                TValue priorValue;
                if (!priorCollection.TryGetValue(key, out priorValue) || !valueComparer.Equals(priorValue, comparisonValue))
                {
                    // The key isn't in the dictionary, or its current value doesn't match what the caller expected.
                    return false;
                }

                var updatedCollection = priorCollection.SetItem(key, newValue);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            } while (!successful);

            return true;
        }

        /// <summary>
        /// Removes an entry from the dictionary with the specified key if it is defined and returns its value.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key"/> is not in the dictionary.</param>
        /// <param name="key">The key to remove.</param>
        /// <param name="value">Receives the value from the pre-existing entry, if one exists.</param>
        /// <returns><c>true</c> if the key was found and removed; <c>false</c> otherwise.</returns>
        public static bool TryRemove<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, out TValue value)
        {
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                if (!priorCollection.TryGetValue(key, out value))
                {
                    return false;
                }

                var updatedCollection = priorCollection.Remove(key);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            } while (!successful);

            return true;
        }

        #endregion

        #region ImmutableStack<T> members

        /// <summary>
        /// Pops the top element off a stack and returns it to the caller, if the stack is not empty.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the stack.</typeparam>
        /// <param name="location">The variable or field to atomically update.</param>
        /// <param name="value">The value popped from the stack, if it was non-empty.</param>
        /// <returns><c>true</c> if an element was removed from the stack; <c>false</c> otherwise.</returns>
        public static bool TryPop<T>(ref ImmutableStack<T> location, out T value)
        {
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                if (priorCollection.IsEmpty)
                {
                    value = default(T);
                    return false;
                }

                var updatedCollection = priorCollection.Pop(out value);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            } while (!successful);

            return true;
        }

        /// <summary>
        /// Pushes a new element onto a stack.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the stack.</typeparam>
        /// <param name="location">The variable or field to atomically update.</param>
        /// <param name="value">The value to push.</param>
        public static void Push<T>(ref ImmutableStack<T> location, T value)
        {
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                var updatedCollection = priorCollection.Push(value);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            } while (!successful);
        }

        #endregion

        #region ImmutableQueue<T> members

        /// <summary>
        /// Atomically removes the element at the head of a queue and returns it to the caller, if the queue is not empty.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the queue.</typeparam>
        /// <param name="location">The variable or field to atomically update.</param>
        /// <param name="value">Receives the value from the head of the queue, if the queue is non-empty.</param>
        /// <returns><c>true</c> if the queue was not empty and the head element was removed; <c>false</c> otherwise.</returns>
        public static bool TryDequeue<T>(ref ImmutableQueue<T> location, out T value)
        {
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                if (priorCollection.IsEmpty)
                {
                    value = default(T);
                    return false;
                }

                var updatedCollection = priorCollection.Dequeue(out value);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            } while (!successful);

            return true;
        }

        /// <summary>
        /// Atomically enqueues an element to the tail of a queue.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the queue.</typeparam>
        /// <param name="location">The variable or field to atomically update.</param>
        /// <param name="value">The value to enqueue.</param>
        public static void Enqueue<T>(ref ImmutableQueue<T> location, T value)
        {
            var priorCollection = Volatile.Read(ref location);
            bool successful;
            do
            {
                Requires.NotNull(priorCollection, nameof(location));

                var updatedCollection = priorCollection.Enqueue(value);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                successful = Object.ReferenceEquals(priorCollection, interlockedResult);
                priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
            } while (!successful);
        }
        #endregion
    }
}
