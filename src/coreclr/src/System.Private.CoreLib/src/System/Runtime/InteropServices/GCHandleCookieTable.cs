// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if MDA_SUPPORTED

namespace System.Runtime.InteropServices
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using ObjectHandle = IntPtr;
    using GCHandleCookie = IntPtr;

    // Internal class used to map a GCHandle to an IntPtr. Instead of handing out the underlying CLR
    // handle, we now hand out a cookie that can later be converted back to the CLR handle it 
    // is associated with.

    // NOTE:
    // this implementation uses a single lock between FindOrAddHandle and RemoveHandleIfPresent which
    // could create some scalability issues when this MDA is turned on.  if this is affecting perf
    // then additional tuning work will be required.

    internal class GCHandleCookieTable
    {
        private const int InitialHandleCount = 10;
        private const int MaxListSize = 0xFFFFFF;
        private const uint CookieMaskIndex = 0x00FFFFFF;
        private const uint CookieMaskSentinal = 0xFF000000;

        internal GCHandleCookieTable()
        {
            m_HandleList = new ObjectHandle[InitialHandleCount];
            m_CycleCounts = new byte[InitialHandleCount];
            m_HandleToCookieMap = new Dictionary<ObjectHandle, GCHandleCookie>(InitialHandleCount);
            m_syncObject = new object();

            for (int i = 0; i < InitialHandleCount; i++)
            {
                m_HandleList[i] = ObjectHandle.Zero;
                m_CycleCounts[i] = 0;
            }
        }

        // Retrieve a cookie for the passed in handle. If no cookie has yet been allocated for 
        // this handle, one will be created. This method is thread safe.
        internal GCHandleCookie FindOrAddHandle(ObjectHandle handle)
        {
            // Don't accept a null object handle
            if (handle == ObjectHandle.Zero)
                return GCHandleCookie.Zero;

            GCHandleCookie cookie = GCHandleCookie.Zero;

            lock (m_syncObject)
            {
                // First see if we already have a cookie for this handle.
                if (m_HandleToCookieMap.ContainsKey(handle))
                    return m_HandleToCookieMap[handle];

                if ((m_FreeIndex < m_HandleList.Length) && (Volatile.Read(ref m_HandleList[m_FreeIndex]) == ObjectHandle.Zero))
                {
                    Volatile.Write(ref m_HandleList[m_FreeIndex],  handle);
                    cookie = GetCookieFromData((uint)m_FreeIndex, m_CycleCounts[m_FreeIndex]);

                    // Set our next guess just one higher as this index is now in use.
                    // it's ok if this sets m_FreeIndex > m_HandleList.Length as this condition is
                    // checked at the beginning of the if statement.
                    ++m_FreeIndex;
                }
                else
                {
                    for (m_FreeIndex = 0; m_FreeIndex < MaxListSize; ++m_FreeIndex)
                    {
                        if (m_HandleList[m_FreeIndex] == ObjectHandle.Zero)
                        {
                            Volatile.Write(ref m_HandleList[m_FreeIndex], handle);
                            cookie = GetCookieFromData((uint)m_FreeIndex, m_CycleCounts[m_FreeIndex]);

                            // this will be our next guess for a free index.
                            // it's ok if this sets m_FreeIndex > m_HandleList.Length
                            // since we check for this condition in the if statement.
                            ++m_FreeIndex;
                            break;
                        }

                        if (m_FreeIndex + 1 == m_HandleList.Length)
                            GrowArrays();
                    }
                }

                if (cookie == GCHandleCookie.Zero)
                    throw new OutOfMemoryException(SR.OutOfMemory_GCHandleMDA);

                // This handle hasn't been added to the map yet so add it.
                m_HandleToCookieMap.Add(handle, cookie);
            }

            return cookie;
        }

        // Get a handle.
        internal ObjectHandle GetHandle(GCHandleCookie cookie)
        {
            ObjectHandle oh = ObjectHandle.Zero;

            if (!ValidateCookie(cookie))
                return ObjectHandle.Zero;

            oh = Volatile.Read(ref m_HandleList[GetIndexFromCookie(cookie)]);

            return oh;
        }

        // Remove the handle from the cookie table if it is present. 
        //
        internal void RemoveHandleIfPresent(ObjectHandle handle)
        {
            if (handle == ObjectHandle.Zero)
                return;

            lock (m_syncObject)
            {
                if (m_HandleToCookieMap.ContainsKey(handle))
                {
                    GCHandleCookie cookie = m_HandleToCookieMap[handle];

                    // Remove it from the array first
                    if (!ValidateCookie(cookie))
                        return;

                    int index = GetIndexFromCookie(cookie);

                    m_CycleCounts[index]++;
                    Volatile.Write(ref m_HandleList[index], ObjectHandle.Zero);

                    // Remove it from the hashtable last
                    m_HandleToCookieMap.Remove(handle);

                    // Update our guess
                    m_FreeIndex = index;
                }
            }
        }

        private bool ValidateCookie(GCHandleCookie cookie)
        {
            int index;
            byte xorData;

            GetDataFromCookie(cookie, out index, out xorData);

            // Validate the index
            if (index >= MaxListSize)
                return false;

            if (index >= m_HandleList.Length)
                return false;

            if (Volatile.Read(ref m_HandleList[index]) == ObjectHandle.Zero)
                return false;

            // Validate the xorData byte (this contains the cycle count and appdomain id).
            byte ADID = (byte)(AppDomain.CurrentDomain.Id % 0xFF);
            byte goodData = (byte)(Volatile.Read(ref m_CycleCounts[index]) ^ ADID);
            if (xorData != goodData)
                return false;

            return true;
        }

        // Double the size of our arrays - must be called with the lock taken.
        private void GrowArrays()
        {
            int CurrLength = m_HandleList.Length;

            ObjectHandle[] newHandleList = new ObjectHandle[CurrLength * 2];
            byte[] newCycleCounts = new byte[CurrLength * 2];

            Array.Copy(m_HandleList, newHandleList, CurrLength);
            Array.Copy(m_CycleCounts, newCycleCounts, CurrLength);

            m_HandleList = newHandleList;
            m_CycleCounts = newCycleCounts;
        }

        // Generate a cookie based on the index, cyclecount, and current domain id.
        private GCHandleCookie GetCookieFromData(uint index, byte cycleCount)
        {
            byte ADID = (byte)(AppDomain.CurrentDomain.Id % 0xFF);
            return (GCHandleCookie)(((cycleCount ^ ADID) << 24) + index + 1);
        }

        // Break down the cookie into its parts
        private void GetDataFromCookie(GCHandleCookie cookie, out int index, out byte xorData)
        {
            uint intCookie = (uint)cookie;
            index = (int)(intCookie & CookieMaskIndex) - 1;
            xorData = (byte)((intCookie & CookieMaskSentinal) >> 24);
        }

        // Just get the index from the cookie
        private int GetIndexFromCookie(GCHandleCookie cookie)
        {
            uint intCookie = (uint)cookie;
            return (int)(intCookie & CookieMaskIndex) - 1;
        }

        private Dictionary<ObjectHandle, GCHandleCookie> m_HandleToCookieMap;
        private volatile ObjectHandle[] m_HandleList;
        private volatile byte[] m_CycleCounts;
        private int m_FreeIndex;
        private readonly object m_syncObject;
    }
}

#endif

