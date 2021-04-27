// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// A class that provides a simple, lightweight implementation of thread-local lazy-initialization, where a value is initialized once per accessing
// thread; this provides an alternative to using a ThreadStatic static variable and having
// to check the variable prior to every access to see if it's been initialized.

namespace System.Threading
{
    /// <summary>
    /// Provides thread-local storage of data.
    /// </summary>
    /// <typeparam name="T">Specifies the type of data stored per-thread.</typeparam>
    /// <remarks>
    /// <para>
    /// With the exception of <see cref="Dispose()"/>, all public and protected members of
    /// <see cref="ThreadLocal{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </para>
    /// </remarks>
    [DebuggerTypeProxy(typeof(SystemThreading_ThreadLocalDebugView<>))]
    [DebuggerDisplay("IsValueCreated={IsValueCreated}, Value={ValueForDebugDisplay}, Count={ValuesCountForDebugDisplay}")]
    public class ThreadLocal<T> : IDisposable
    {
        // a delegate that returns the created value, if null the created value will be default(T)
        private Func<T>? _valueFactory;

        // ts_slotArray is a table of thread-local values for all ThreadLocal<T> instances
        //
        // So, when a thread reads ts_slotArray, it gets back an array of *all* ThreadLocal<T> values for this thread and this T.
        // The slot relevant to this particular ThreadLocal<T> instance is determined by the _idComplement instance field stored in
        // the ThreadLocal<T> instance.
        [ThreadStatic]
        private static LinkedSlotVolatile[]? ts_slotArray;

        [ThreadStatic]
        private static FinalizationHelper? ts_finalizationHelper;

        // Slot ID of this ThreadLocal<> instance. We store a bitwise complement of the ID (that is ~ID), which allows us to distinguish
        // between the case when ID is 0 and an incompletely initialized object, either due to a thread abort in the constructor, or
        // possibly due to a memory model issue in user code.
        private int _idComplement;

        // This field is set to true when the constructor completes. That is helpful for recognizing whether a constructor
        // threw an exception - either due to invalid argument or due to a thread abort. Finally, the field is set to false
        // when the instance is disposed.
        private volatile bool _initialized;

        // IdManager assigns and reuses slot IDs. Additionally, the object is also used as a global lock.
        private static readonly IdManager s_idManager = new IdManager();

        // A linked list of all values associated with this ThreadLocal<T> instance.
        // We create a dummy head node. That allows us to remove any (non-dummy)  node without having to locate the m_linkedSlot field.
        private LinkedSlot? _linkedSlot = new LinkedSlot(null);

        // Whether the Values property is supported
        private bool _trackAllValues;

        /// <summary>
        /// Initializes the <see cref="System.Threading.ThreadLocal{T}"/> instance.
        /// </summary>
        public ThreadLocal()
        {
            Initialize(null, false);
        }

        /// <summary>
        /// Initializes the <see cref="System.Threading.ThreadLocal{T}"/> instance.
        /// </summary>
        /// <param name="trackAllValues">Whether to track all values set on the instance and expose them through the Values property.</param>
        public ThreadLocal(bool trackAllValues)
        {
            Initialize(null, trackAllValues);
        }


        /// <summary>
        /// Initializes the <see cref="System.Threading.ThreadLocal{T}"/> instance with the
        /// specified <paramref name="valueFactory"/> function.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="System.Func{T}"/> invoked to produce a lazily-initialized value when
        /// an attempt is made to retrieve <see cref="Value"/> without it having been previously initialized.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="valueFactory"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public ThreadLocal(Func<T> valueFactory)
        {
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            Initialize(valueFactory, false);
        }

        /// <summary>
        /// Initializes the <see cref="System.Threading.ThreadLocal{T}"/> instance with the
        /// specified <paramref name="valueFactory"/> function.
        /// </summary>
        /// <param name="valueFactory">
        /// The <see cref="System.Func{T}"/> invoked to produce a lazily-initialized value when
        /// an attempt is made to retrieve <see cref="Value"/> without it having been previously initialized.
        /// </param>
        /// <param name="trackAllValues">Whether to track all values set on the instance and expose them via the Values property.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="valueFactory"/> is a null reference (Nothing in Visual Basic).
        /// </exception>
        public ThreadLocal(Func<T> valueFactory, bool trackAllValues)
        {
            if (valueFactory == null)
                throw new ArgumentNullException(nameof(valueFactory));

            Initialize(valueFactory, trackAllValues);
        }

        private void Initialize(Func<T>? valueFactory, bool trackAllValues)
        {
            _valueFactory = valueFactory;
            _trackAllValues = trackAllValues;

            // Assign the ID and mark the instance as initialized.
             _idComplement = ~s_idManager.GetId();

            // As the last step, mark the instance as fully initialized. (Otherwise, if _initialized=false, we know that an exception
            // occurred in the constructor.)
            _initialized = true;
        }

        /// <summary>
        /// Releases the resources used by this <see cref="System.Threading.ThreadLocal{T}" /> instance.
        /// </summary>
        ~ThreadLocal()
        {
            // finalizer to return the type combination index to the pool
            Dispose(false);
        }

        #region IDisposable Members

        /// <summary>
        /// Releases the resources used by this <see cref="System.Threading.ThreadLocal{T}" /> instance.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="System.Threading.ThreadLocal{T}"/>, this method is not thread-safe.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the resources used by this <see cref="System.Threading.ThreadLocal{T}" /> instance.
        /// </summary>
        /// <param name="disposing">
        /// A Boolean value that indicates whether this method is being called due to a call to <see cref="Dispose()"/>.
        /// </param>
        /// <remarks>
        /// Unlike most of the members of <see cref="System.Threading.ThreadLocal{T}"/>, this method is not thread-safe.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            int id;

            lock (s_idManager)
            {
                id = ~_idComplement;
                _idComplement = 0;

                if (id < 0 || !_initialized)
                {
                    Debug.Assert(id >= 0 || !_initialized, "expected id >= 0 if initialized");

                    // Handle double Dispose calls or disposal of an instance whose constructor threw an exception.
                    return;
                }
                _initialized = false;

                Debug.Assert(_linkedSlot != null, "Should be non-null if not yet disposed");
                for (LinkedSlot? linkedSlot = _linkedSlot._next; linkedSlot != null; linkedSlot = linkedSlot._next)
                {
                    LinkedSlotVolatile[]? slotArray = linkedSlot._slotArray;

                    if (slotArray == null)
                    {
                        // The thread that owns this slotArray has already finished.
                        continue;
                    }

                    // Remove the reference from the LinkedSlot to the slot table.
                    linkedSlot._slotArray = null;

                    // And clear the references from the slot table to the linked slot and the value so that
                    // both can get garbage collected.
                    slotArray[id].Value!._value = default;
                    slotArray[id].Value = null;
                }
            }
            _linkedSlot = null;
            s_idManager.ReturnId(id);
        }

        #endregion

        /// <summary>Creates and returns a string representation of this instance for the current thread.</summary>
        /// <returns>The result of calling <see cref="object.ToString"/> on the <see cref="Value"/>.</returns>
        /// <exception cref="System.NullReferenceException">
        /// The <see cref="Value"/> for the current thread is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The initialization function referenced <see cref="Value"/> in an improper manner.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="ThreadLocal{T}"/> instance has been disposed.
        /// </exception>
        /// <remarks>
        /// Calling this method forces initialization for the current thread, as is the
        /// case with accessing <see cref="Value"/> directly.
        /// </remarks>
        public override string? ToString()
        {
            return Value!.ToString(); // Throws NullReferenceException as if caller called ToString on the value itself
        }

        /// <summary>
        /// Gets or sets the value of this instance for the current thread.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// The initialization function referenced <see cref="Value"/> in an improper manner.
        /// </exception>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="ThreadLocal{T}"/> instance has been disposed.
        /// </exception>
        /// <remarks>
        /// If this instance was not previously initialized for the current thread,
        /// accessing <see cref="Value"/> will attempt to initialize it. If an initialization function was
        /// supplied during the construction, that initialization will happen by invoking the function
        /// to retrieve the initial value for <see cref="Value"/>.  Otherwise, the default value of
        /// <typeparamref name="T"/> will be used.
        /// </remarks>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [MaybeNull]
        public T Value
        {
            get
            {
                LinkedSlotVolatile[]? slotArray = ts_slotArray;
                LinkedSlot? slot;
                int id = ~_idComplement;

                //
                // Attempt to get the value using the fast path
                //
                if (slotArray != null   // Has the slot array been initialized?
                    && id >= 0   // Is the ID non-negative (i.e., instance is not disposed)?
                    && id < slotArray.Length   // Is the table large enough?
                    && (slot = slotArray[id].Value) != null   // Has a LinkedSlot object has been allocated for this ID?
                    && _initialized // Has the instance *still* not been disposed (important for a race condition with Dispose)?
                )
                {
                    // We verified that the instance has not been disposed *after* we got a reference to the slot.
                    // This guarantees that we have a reference to the right slot.
                    //
                    // Volatile read of the LinkedSlotVolatile.Value property ensures that the m_initialized read
                    // will not be reordered before the read of slotArray[id].
                    return slot._value;
                }

                return GetValueSlow();
            }
            set
            {
                LinkedSlotVolatile[]? slotArray = ts_slotArray;
                LinkedSlot? slot;
                int id = ~_idComplement;

                // Attempt to set the value using the fast path
                if (slotArray != null   // Has the slot array been initialized?
                    && id >= 0   // Is the ID non-negative (i.e., instance is not disposed)?
                    && id < slotArray.Length   // Is the table large enough?
                    && (slot = slotArray[id].Value) != null   // Has a LinkedSlot object has been allocated for this ID?
                    && _initialized // Has the instance *still* not been disposed (important for a race condition with Dispose)?
                    )
                {
                    // We verified that the instance has not been disposed *after* we got a reference to the slot.
                    // This guarantees that we have a reference to the right slot.
                    //
                    // Volatile read of the LinkedSlotVolatile.Value property ensures that the m_initialized read
                    // will not be reordered before the read of slotArray[id].
                    slot._value = value;
                }
                else
                {
                    SetValueSlow(value, slotArray);
                }
            }
        }

        private T? GetValueSlow()
        {
            // If the object has been disposed, the id will be -1.
            int id = ~_idComplement;
            if (id < 0)
            {
                throw new ObjectDisposedException(SR.ThreadLocal_Disposed);
            }

            Debugger.NotifyOfCrossThreadDependency();

            // Determine the initial value
            T value;
            if (_valueFactory == null)
            {
                value = default!;
            }
            else
            {
                value = _valueFactory();

                if (IsValueCreated)
                {
                    throw new InvalidOperationException(SR.ThreadLocal_Value_RecursiveCallsToValue);
                }
            }

            // Since the value has been previously uninitialized, we also need to set it (according to the ThreadLocal semantics).
            Value = value;
            return value;
        }

        private void SetValueSlow(T value, LinkedSlotVolatile[]? slotArray)
        {
            int id = ~_idComplement;

            // If the object has been disposed, id will be -1.
            if (id < 0)
            {
                throw new ObjectDisposedException(SR.ThreadLocal_Disposed);
            }

            // If a slot array has not been created on this thread yet, create it.
            if (slotArray == null)
            {
                slotArray = new LinkedSlotVolatile[GetNewTableSize(id + 1)];
                ts_finalizationHelper = new FinalizationHelper(slotArray, _trackAllValues);
                ts_slotArray = slotArray;
            }

            // If the slot array is not big enough to hold this ID, increase the table size.
            if (id >= slotArray.Length)
            {
                GrowTable(ref slotArray!, id + 1);
                Debug.Assert(ts_finalizationHelper != null, "Should have been initialized when this thread's slot array was created.");
                ts_finalizationHelper.SlotArray = slotArray;
                ts_slotArray = slotArray;
            }

            // If we are using the slot in this table for the first time, create a new LinkedSlot and add it into
            // the linked list for this ThreadLocal instance.
            if (slotArray[id].Value == null)
            {
                CreateLinkedSlot(slotArray, id, value);
            }
            else
            {
                // Volatile read of the LinkedSlotVolatile.Value property ensures that the m_initialized read
                // that follows will not be reordered before the read of slotArray[id].
                LinkedSlot? slot = slotArray[id].Value;

                // It is important to verify that the ThreadLocal instance has not been disposed. The check must come
                // after capturing slotArray[id], but before assigning the value into the slot. This ensures that
                // if this ThreadLocal instance was disposed on another thread and another ThreadLocal instance was
                // created, we definitely won't assign the value into the wrong instance.

                if (!_initialized)
                {
                    throw new ObjectDisposedException(SR.ThreadLocal_Disposed);
                }

                slot!._value = value;
            }
        }

        /// <summary>
        /// Creates a LinkedSlot and inserts it into the linked list for this ThreadLocal instance.
        /// </summary>
        private void CreateLinkedSlot(LinkedSlotVolatile[] slotArray, int id, T value)
        {
            // Create a LinkedSlot
            var linkedSlot = new LinkedSlot(slotArray);

            // Insert the LinkedSlot into the linked list maintained by this ThreadLocal<> instance and into the slot array
            lock (s_idManager)
            {
                // Check that the instance has not been disposed. It is important to check this under a lock, since
                // Dispose also executes under a lock.
                if (!_initialized)
                {
                    throw new ObjectDisposedException(SR.ThreadLocal_Disposed);
                }

                Debug.Assert(_linkedSlot != null, "Should only be null if disposed");
                LinkedSlot? firstRealNode = _linkedSlot._next;

                // Insert linkedSlot between nodes m_linkedSlot and firstRealNode.
                // (_linkedSlot is the dummy head node that should always be in the front.)
                linkedSlot._next = firstRealNode;
                linkedSlot._previous = _linkedSlot;
                linkedSlot._value = value;

                if (firstRealNode != null)
                {
                    firstRealNode._previous = linkedSlot;
                }
                _linkedSlot._next = linkedSlot;

                // Assigning the slot under a lock prevents a race condition with Dispose (dispose also acquires the lock).
                // Otherwise, it would be possible that the ThreadLocal instance is disposed, another one gets created
                // with the same ID, and the write would go to the wrong instance.
                slotArray[id].Value = linkedSlot;
            }
        }

        /// <summary>
        /// Gets a list for all of the values currently stored by all of the threads that have accessed this instance.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="ThreadLocal{T}"/> instance has been disposed.
        /// </exception>
        public IList<T> Values
        {
            get
            {
                if (!_trackAllValues)
                {
                    throw new InvalidOperationException(SR.ThreadLocal_ValuesNotAvailable);
                }

                List<T>? list = GetValuesAsList(); // returns null if disposed
                if (list == null) throw new ObjectDisposedException(SR.ThreadLocal_Disposed);
                return list;
            }
        }

        /// <summary>Gets all of the threads' values in a list.</summary>
        private List<T>? GetValuesAsList()
        {
            LinkedSlot? linkedSlot = _linkedSlot;
            int id = ~_idComplement;
            if (id == -1 || linkedSlot == null)
            {
                return null;
            }

            // Walk over the linked list of slots and gather the values associated with this ThreadLocal instance.
            var valueList = new List<T>();
            for (linkedSlot = linkedSlot._next; linkedSlot != null; linkedSlot = linkedSlot._next)
            {
                // We can safely read linkedSlot.Value. Even if this ThreadLocal has been disposed in the meantime, the LinkedSlot
                // objects will never be assigned to another ThreadLocal instance.
                valueList.Add(linkedSlot._value!);
            }

            return valueList;
        }

        internal IEnumerable<T> ValuesAsEnumerable
        {
            get
            {
                if (!_trackAllValues)
                {
                    throw new InvalidOperationException(SR.ThreadLocal_ValuesNotAvailable);
                }

                LinkedSlot? linkedSlot = _linkedSlot;
                int id = ~_idComplement;
                if (id == -1 || linkedSlot == null)
                {
                    throw new ObjectDisposedException(SR.ThreadLocal_Disposed);
                }

                // Walk over the linked list of slots and gather the values associated with this ThreadLocal instance.
                for (linkedSlot = linkedSlot._next; linkedSlot != null; linkedSlot = linkedSlot._next)
                {
                    // We can safely read linkedSlot.Value. Even if this ThreadLocal has been disposed in the meantime, the LinkedSlot
                    // objects will never be assigned to another ThreadLocal instance.
                    yield return linkedSlot._value!;
                }
            }
        }

        /// <summary>Gets the number of threads that have data in this instance.</summary>
        private int ValuesCountForDebugDisplay
        {
            get
            {
                int count = 0;
                for (LinkedSlot? linkedSlot = _linkedSlot?._next; linkedSlot != null; linkedSlot = linkedSlot._next)
                {
                    count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets whether <see cref="Value"/> is initialized on the current thread.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">
        /// The <see cref="ThreadLocal{T}"/> instance has been disposed.
        /// </exception>
        public bool IsValueCreated
        {
            get
            {
                int id = ~_idComplement;
                if (id < 0)
                {
                    throw new ObjectDisposedException(SR.ThreadLocal_Disposed);
                }

                LinkedSlotVolatile[]? slotArray = ts_slotArray;
                return slotArray != null && id < slotArray.Length && slotArray[id].Value != null;
            }
        }


        /// <summary>Gets the value of the ThreadLocal&lt;T&gt; for debugging display purposes. It takes care of getting
        /// the value for the current thread in the ThreadLocal mode.</summary>
        internal T? ValueForDebugDisplay
        {
            get
            {
                LinkedSlotVolatile[]? slotArray = ts_slotArray;
                int id = ~_idComplement;

                LinkedSlot? slot;
                if (slotArray == null || id >= slotArray.Length || (slot = slotArray[id].Value) == null || !_initialized)
                    return default;
                return slot._value;
            }
        }

        /// <summary>Gets the values of all threads that accessed the ThreadLocal&lt;T&gt;.</summary>
        internal List<T>? ValuesForDebugDisplay => // same as Values property, but doesn't throw if disposed
            GetValuesAsList();

        /// <summary>
        /// Resizes a table to a certain length (or larger).
        /// </summary>
        private static void GrowTable(ref LinkedSlotVolatile[] table, int minLength)
        {
            Debug.Assert(table.Length < minLength);

            // Determine the size of the new table and allocate it.
            int newLen = GetNewTableSize(minLength);
            LinkedSlotVolatile[] newTable = new LinkedSlotVolatile[newLen];

            //
            // The lock is necessary to avoid a race with ThreadLocal.Dispose. GrowTable has to point all
            // LinkedSlot instances referenced in the old table to reference the new table. Without locking,
            // Dispose could use a stale SlotArray reference and clear out a slot in the old array only, while
            // the value continues to be referenced from the new (larger) array.
            //
            lock (s_idManager)
            {
                for (int i = 0; i < table.Length; i++)
                {
                    LinkedSlot? linkedSlot = table[i].Value;
                    if (linkedSlot != null && linkedSlot._slotArray != null)
                    {
                        linkedSlot._slotArray = newTable;
                        newTable[i] = table[i];
                    }
                }
            }

            table = newTable;
        }

        /// <summary>
        /// Chooses the next larger table size
        /// </summary>
        private static int GetNewTableSize(int minSize)
        {
            if ((uint)minSize > Array.MaxLength)
            {
                // Intentionally return a value that will result in an OutOfMemoryException
                return int.MaxValue;
            }
            Debug.Assert(minSize > 0);

            //
            // Round up the size to the next power of 2
            //
            // The algorithm takes three steps:
            // input -> subtract one -> propagate 1-bits to the right -> add one
            //
            // Let's take a look at the 3 steps in both interesting cases: where the input
            // is (Example 1) and isn't (Example 2) a power of 2.
            //
            // Example 1: 100000 -> 011111 -> 011111 -> 100000
            // Example 2: 011010 -> 011001 -> 011111 -> 100000
            //
            int newSize = minSize;

            // Step 1: Decrement
            newSize--;

            // Step 2: Propagate 1-bits to the right.
            newSize |= newSize >> 1;
            newSize |= newSize >> 2;
            newSize |= newSize >> 4;
            newSize |= newSize >> 8;
            newSize |= newSize >> 16;

            // Step 3: Increment
            newSize++;

            // Don't set newSize to more than Array.MaxArrayLength
            if ((uint)newSize > Array.MaxLength)
            {
                newSize = Array.MaxLength;
            }

            return newSize;
        }

        /// <summary>
        /// A wrapper struct used as LinkedSlotVolatile[] - an array of LinkedSlot instances, but with volatile semantics
        /// on array accesses.
        /// </summary>
        private struct LinkedSlotVolatile
        {
            internal volatile LinkedSlot? Value;
        }

        /// <summary>
        /// A node in the doubly-linked list stored in the ThreadLocal instance.
        ///
        /// The value is stored in one of two places:
        ///
        ///     1. If SlotArray is not null, the value is in SlotArray.Table[id]
        ///     2. If SlotArray is null, the value is in FinalValue.
        /// </summary>
        private sealed class LinkedSlot
        {
            internal LinkedSlot(LinkedSlotVolatile[]? slotArray)
            {
                _slotArray = slotArray;
            }

            // The next LinkedSlot for this ThreadLocal<> instance
            internal volatile LinkedSlot? _next;

            // The previous LinkedSlot for this ThreadLocal<> instance
            internal volatile LinkedSlot? _previous;

            // The SlotArray that stores this LinkedSlot at SlotArray.Table[id].
            internal volatile LinkedSlotVolatile[]? _slotArray;

            // The value for this slot.
            internal T? _value;
        }

        /// <summary>
        /// A manager class that assigns IDs to ThreadLocal instances
        /// </summary>
        private sealed class IdManager
        {
            // The next ID to try
            private int _nextIdToTry;

            // Stores whether each ID is free or not. Additionally, the object is also used as a lock for the IdManager.
            private readonly List<bool> _freeIds = new List<bool>();

            internal int GetId()
            {
                lock (_freeIds)
                {
                    int availableId = _nextIdToTry;
                    while (availableId < _freeIds.Count)
                    {
                        if (_freeIds[availableId]) { break; }
                        availableId++;
                    }

                    if (availableId == _freeIds.Count)
                    {
                        _freeIds.Add(false);
                    }
                    else
                    {
                        _freeIds[availableId] = false;
                    }

                    _nextIdToTry = availableId + 1;

                    return availableId;
                }
            }

            // Return an ID to the pool
            internal void ReturnId(int id)
            {
                lock (_freeIds)
                {
                    _freeIds[id] = true;
                    if (id < _nextIdToTry) _nextIdToTry = id;
                }
            }
        }

        /// <summary>
        /// A class that facilitates ThreadLocal cleanup after a thread exits.
        ///
        /// After a thread with an associated thread-local table has exited, the FinalizationHelper
        /// is responsible for removing back-references to the table. Since an instance of FinalizationHelper
        /// is only referenced from a single thread-local slot, the FinalizationHelper will be GC'd once
        /// the thread has exited.
        ///
        /// The FinalizationHelper then locates all LinkedSlot instances with back-references to the table
        /// (all those LinkedSlot instances can be found by following references from the table slots) and
        /// releases the table so that it can get GC'd.
        /// </summary>
        private sealed class FinalizationHelper
        {
            internal LinkedSlotVolatile[] SlotArray;
            private readonly bool _trackAllValues;

            internal FinalizationHelper(LinkedSlotVolatile[] slotArray, bool trackAllValues)
            {
                SlotArray = slotArray;
                _trackAllValues = trackAllValues;
            }

            ~FinalizationHelper()
            {
                LinkedSlotVolatile[] slotArray = SlotArray;
                Debug.Assert(slotArray != null);

                for (int i = 0; i < slotArray.Length; i++)
                {
                    LinkedSlot? linkedSlot = slotArray[i].Value;
                    if (linkedSlot == null)
                    {
                        // This slot in the table is empty
                        continue;
                    }

                    if (_trackAllValues)
                    {
                        // Set the SlotArray field to null to release the slot array.
                        linkedSlot._slotArray = null;
                    }
                    else
                    {
                        // Remove the LinkedSlot from the linked list. Once the FinalizationHelper is done, all back-references to
                        // the table will be have been removed, and so the table can get GC'd.
                        lock (s_idManager)
                        {
                            if (linkedSlot._next != null)
                            {
                                linkedSlot._next._previous = linkedSlot._previous;
                            }

                            // Since the list uses a dummy head node, the Previous reference should never be null.
                            Debug.Assert(linkedSlot._previous != null);
                            linkedSlot._previous._next = linkedSlot._next;
                        }
                    }
                }
            }
        }
    }

    /// <summary>A debugger view of the ThreadLocal&lt;T&gt; to surface additional debugging properties and
    /// to ensure that the ThreadLocal&lt;T&gt; does not become initialized if it was not already.</summary>
    internal sealed class SystemThreading_ThreadLocalDebugView<T>
    {
        // The ThreadLocal object being viewed.
        private readonly ThreadLocal<T> _tlocal;

        /// <summary>Constructs a new debugger view object for the provided ThreadLocal object.</summary>
        /// <param name="tlocal">A ThreadLocal object to browse in the debugger.</param>
        public SystemThreading_ThreadLocalDebugView(ThreadLocal<T> tlocal)
        {
            _tlocal = tlocal;
        }

        /// <summary>Returns whether the ThreadLocal object is initialized or not.</summary>
        public bool IsValueCreated => _tlocal.IsValueCreated;

        /// <summary>Returns the value of the ThreadLocal object.</summary>
        public T? Value => _tlocal.ValueForDebugDisplay;

        /// <summary>Return all values for all threads that have accessed this instance.</summary>
        public List<T>? Values => _tlocal.ValuesForDebugDisplay;
    }
}
