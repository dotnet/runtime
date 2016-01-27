// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Class that stores local data. This class is used in cooperation
**          with the _LocalDataStoreMgr class.
**
**
=============================================================================*/

namespace System {
    
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Diagnostics.Contracts;

    // Helper class to aid removal of LocalDataStore from the LocalDataStoreMgr
    // LocalDataStoreMgr does not holds references to LocalDataStoreHolder. It holds
    // references to LocalDataStore only. LocalDataStoreHolder finalizer will run once
    // the only outstanding reference to the store is in LocalDataStoreMgr.
    sealed internal class LocalDataStoreHolder
    {
        private LocalDataStore m_Store;

        public LocalDataStoreHolder(LocalDataStore store)
        {
            m_Store = store;
        }

        ~LocalDataStoreHolder()
        {
            LocalDataStore store = m_Store;
            if (store == null)
                return;

            store.Dispose();
        }

        public LocalDataStore Store
        {
            get
            {
                return m_Store;
            }
        }
    }

    sealed internal class LocalDataStoreElement
    {
        private Object m_value;
        private long m_cookie;  // This is immutable cookie of the slot used to verify that 
                                // the value is indeed indeed owned by the slot. Necessary 
                                // to avoid resurection holes.

        public LocalDataStoreElement(long cookie)
        {
            m_cookie = cookie;
        }

        public Object Value
        {
            get
            {
                return m_value;
            }
            set
            {
                m_value = value;
            }
        }

        public long Cookie
        {
            get
            {
                return m_cookie;
            }
        }
    }

    // This class will not be marked serializable
    sealed internal class LocalDataStore
    {
        private LocalDataStoreElement[] m_DataTable;
        private LocalDataStoreMgr m_Manager;

        /*=========================================================================
        ** Initialize the data store.
        =========================================================================*/
        public LocalDataStore(LocalDataStoreMgr mgr, int InitialCapacity)
        {
            // Store the manager of the local data store.       
            m_Manager = mgr;

            // Allocate the array that will contain the data.
            m_DataTable = new LocalDataStoreElement[InitialCapacity];
        }

        /*=========================================================================
        ** Delete this store from its manager
        =========================================================================*/
        internal void Dispose()
        {
            m_Manager.DeleteLocalDataStore(this);
        }

        /*=========================================================================
        ** Retrieves the value from the specified slot.
        =========================================================================*/
        public Object GetData(LocalDataStoreSlot slot)
        {
            // Validate the slot.
            m_Manager.ValidateSlot(slot);

            // Cache the slot index to avoid synchronization issues.
            int slotIdx = slot.Slot;

            if (slotIdx >= 0)
            {
                // Delay expansion of m_DataTable if we can
                if (slotIdx >= m_DataTable.Length)
                    return null;         
                
                // Retrieve the data from the given slot.
                LocalDataStoreElement element = m_DataTable[slotIdx];

          //Initially we prepopulate the elements to be null.     
          if (element == null)
              return null;

                // Check that the element is owned by this slot by comparing cookies.
                // This is necesary to avoid resurection race conditions.
                if (element.Cookie == slot.Cookie)
                    return element.Value;

                // Fall thru and throw exception
            }
                
            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));
        }
    
        /*=========================================================================
        ** Sets the data in the specified slot.
        =========================================================================*/
        public void SetData(LocalDataStoreSlot slot, Object data)
        {
            // Validate the slot.
            m_Manager.ValidateSlot(slot);

            // Cache the slot index to avoid synchronization issues.
            int slotIdx = slot.Slot;

            if (slotIdx >= 0)
            {
                LocalDataStoreElement element = (slotIdx < m_DataTable.Length) ? m_DataTable[slotIdx] : null;
                if (element == null)
                {
                    element = PopulateElement(slot);
                }

                // Check that the element is owned by this slot by comparing cookies.
                // This is necesary to avoid resurection race conditions.
                if (element.Cookie == slot.Cookie)
                {
                    // Set the data on the given slot.
                    element.Value = data;
                    return;
                }

                // Fall thru and throw exception
            }

            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));
        }

        /*=========================================================================
        ** This method does clears the unused slot.
         * Assumes lock on m_Manager is taken
        =========================================================================*/
        internal void FreeData(int slot, long cookie)
        {
            // We try to delay allocate the dataTable (in cases like the manager clearing a
            // just-freed slot in all stores
            if (slot >= m_DataTable.Length)
                return;

            LocalDataStoreElement element = m_DataTable[slot];
            if (element != null && element.Cookie == cookie)
                m_DataTable[slot] = null;
        }

        /*=========================================================================
        ** Method used to expand the capacity of the local data store.
        =========================================================================*/
        [System.Security.SecuritySafeCritical]  // auto-generated
        private LocalDataStoreElement PopulateElement(LocalDataStoreSlot slot)
        {
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
                Monitor.Enter(m_Manager, ref tookLock);

                // Make sure that the slot was not freed in the meantime
                int slotIdx = slot.Slot;
                if (slotIdx < 0)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_SlotHasBeenFreed"));

                if (slotIdx >= m_DataTable.Length)
                {
                    int capacity = m_Manager.GetSlotTableLength();

                    // Validate that the specified capacity is larger than the current one.
                    Contract.Assert(capacity >= m_DataTable.Length, "LocalDataStore corrupted: capacity >= m_DataTable.Length");

                    // Allocate the new data table.
                    LocalDataStoreElement[] NewDataTable = new LocalDataStoreElement[capacity];

                    // Copy all the objects into the new table.
                    Array.Copy(m_DataTable, NewDataTable, m_DataTable.Length);

                    // Save the new table.
                    m_DataTable = NewDataTable;
                }

                // Validate that there is enough space in the local data store now
                Contract.Assert(slotIdx < m_DataTable.Length, "LocalDataStore corrupted: slotIdx < m_DataTable.Length");

                if (m_DataTable[slotIdx] == null)
                    m_DataTable[slotIdx] = new LocalDataStoreElement(slot.Cookie);

                return m_DataTable[slotIdx];
            }
            finally {
                if (tookLock)
                    Monitor.Exit(m_Manager);
            }
        }
    }
}
