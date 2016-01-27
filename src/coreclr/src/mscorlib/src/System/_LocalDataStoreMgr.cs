// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Class that manages stores of local data. This class is used in 
**          cooperation with the LocalDataStore class.
**
**
=============================================================================*/
namespace System {
    
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    // This class is an encapsulation of a slot so that it is managed in a secure fashion.
    // It is constructed by the LocalDataStoreManager, holds the slot and the manager
    // and cleans up when it is finalized.
    // This class will not be marked serializable
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class LocalDataStoreSlot
    {
        private LocalDataStoreMgr m_mgr;
        private int m_slot;
        private long m_cookie;

        // Construct the object to encapsulate the slot.
        internal LocalDataStoreSlot(LocalDataStoreMgr mgr, int slot, long cookie)
        {
            m_mgr = mgr;
            m_slot = slot;
            m_cookie = cookie;
        }

        // Accessors for the two fields of this class.
        internal LocalDataStoreMgr Manager
        {
            get
            {
                return m_mgr;
            }
        }
        internal int Slot
        {
            get
            {
                return m_slot;
            }
        }
        internal long Cookie
        {
            get
            {
                return m_cookie;
        }
        }

        // Release the slot reserved by this object when this object goes away.
        ~LocalDataStoreSlot()
        {
            LocalDataStoreMgr mgr = m_mgr;
            if (mgr == null)
                return;

            int slot = m_slot;

                // Mark the slot as free.
                m_slot = -1;

            mgr.FreeDataSlot(slot, m_cookie);
        }
    }

    // This class will not be marked serializable
    sealed internal class LocalDataStoreMgr
    {
        private const int InitialSlotTableSize            = 64;
        private const int SlotTableDoubleThreshold        = 512;
        private const int LargeSlotTableSizeIncrease    = 128;
    
        /*=========================================================================
        ** Create a data store to be managed by this manager and add it to the
        ** list. The initial size of the new store matches the number of slots
        ** allocated in this manager.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        public LocalDataStoreHolder CreateLocalDataStore()
        {
            // Create a new local data store.
            LocalDataStore store = new LocalDataStore(this, m_SlotInfoTable.Length);
            LocalDataStoreHolder holder = new LocalDataStoreHolder(store);

            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                // Add the store to the array list and return it.
                m_ManagedLocalDataStores.Add(store);
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
            return holder;
        }

        /*=========================================================================
         * Remove the specified store from the list of managed stores..
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void DeleteLocalDataStore(LocalDataStore store)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                // Remove the store to the array list and return it.
                m_ManagedLocalDataStores.Remove(store);
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        /*=========================================================================
        ** Allocates a data slot by finding an available index and wrapping it
        ** an object to prevent clients from manipulating it directly, allowing us
        ** to make assumptions its integrity.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        public LocalDataStoreSlot AllocateDataSlot()
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                LocalDataStoreSlot slot;

                int slotTableSize = m_SlotInfoTable.Length;

                // In case FreeDataSlot has moved the pointer back, the next slot may not be available.
                // Find the first actually available slot.
                int availableSlot = m_FirstAvailableSlot;
                while (availableSlot < slotTableSize)
                {
                    if (!m_SlotInfoTable[availableSlot])
                        break;
                    availableSlot++;
                }

                // Check if there are any slots left.
                if (availableSlot >= slotTableSize)
                {
                    // The table is full so we need to increase its size.
                    int newSlotTableSize;
                    if (slotTableSize < SlotTableDoubleThreshold)
                    {
                        // The table is still relatively small so double it.
                        newSlotTableSize = slotTableSize * 2;
                    }
                    else
                    {
                        // The table is relatively large so simply increase its size by a given amount.
                        newSlotTableSize = slotTableSize + LargeSlotTableSizeIncrease;
                    }

                    // Allocate the new slot info table.
                    bool[] newSlotInfoTable = new bool[newSlotTableSize];

                    // Copy the old array into the new one.
                    Array.Copy(m_SlotInfoTable, newSlotInfoTable, slotTableSize);
                    m_SlotInfoTable = newSlotInfoTable;
                }

                // availableSlot is the index of the empty slot.
                m_SlotInfoTable[availableSlot] = true;

                // We do not need to worry about overflowing m_CookieGenerator. It would take centuries
                // of intensive slot allocations on current machines to get the 2^64 counter to overflow.
                // We will perform the increment with overflow check just to play it on the safe side.
                slot = new LocalDataStoreSlot(this, availableSlot, checked(m_CookieGenerator++));

                // Save the new "first available slot".hint
                m_FirstAvailableSlot = availableSlot + 1;

                // Return the selected slot
                return slot;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }
        
        /*=========================================================================
        ** Allocate a slot and associate a name with it.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        public LocalDataStoreSlot AllocateNamedDataSlot(String name)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                // Allocate a normal data slot.
                LocalDataStoreSlot slot = AllocateDataSlot();

                // Insert the association between the name and the data slot number
                // in the hash table.
                m_KeyToSlotMap.Add(name, slot);
                return slot;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        /*=========================================================================
        ** Retrieve the slot associated with a name, allocating it if no such
        ** association has been defined.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        public LocalDataStoreSlot GetNamedDataSlot(String name)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                // Lookup in the hashtable to try find a slot for the name.
                LocalDataStoreSlot slot = m_KeyToSlotMap.GetValueOrDefault(name);

                // If the name is not yet in the hashtable then add it.
                if (null == slot)
                    return AllocateNamedDataSlot(name);

                // The name was in the hashtable so return the associated slot.
                return slot;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        /*=========================================================================
        ** Eliminate the association of a name with a slot.  The actual slot will
        ** be reclaimed when the finalizer for the slot object runs.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void FreeNamedDataSlot(String name)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                // Remove the name slot association from the hashtable.
                m_KeyToSlotMap.Remove(name);
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        /*=========================================================================
        ** Free's a previously allocated data slot on ALL the managed data stores.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal void FreeDataSlot(int slot, long cookie)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                Monitor.Enter(this, ref tookLock);
                // Go thru all the managed stores and set the data on the specified slot to 0.
                for (int i = 0; i < m_ManagedLocalDataStores.Count; i++)
                {
                    ((LocalDataStore)m_ManagedLocalDataStores[i]).FreeData(slot, cookie);
                }

                // Mark the slot as being no longer occupied. 
                m_SlotInfoTable[slot] = false;
                if (slot < m_FirstAvailableSlot)
                    m_FirstAvailableSlot = slot;
            }
            finally
            {
                if (tookLock)
                    Monitor.Exit(this);
            }
        }

        /*=========================================================================
        ** Check that this is a valid slot for this store
        =========================================================================*/
        public void ValidateSlot(LocalDataStoreSlot slot)
        {
            // Make sure the slot was allocated for this store.
            if (slot == null || slot.Manager != this)
                throw new ArgumentException(Environment.GetResourceString("Argument_ALSInvalidSlot"));
            Contract.EndContractBlock();
        }

        /*=========================================================================
        ** Return the number of allocated slots in this manager.
        =========================================================================*/
        internal int GetSlotTableLength()
        {
            return m_SlotInfoTable.Length;
        }

        private bool[] m_SlotInfoTable = new bool[InitialSlotTableSize];
        private int m_FirstAvailableSlot;
        private List<LocalDataStore> m_ManagedLocalDataStores = new List<LocalDataStore>();
        private Dictionary<String, LocalDataStoreSlot> m_KeyToSlotMap = new Dictionary<String, LocalDataStoreSlot>();
        private long m_CookieGenerator;
    }
}
