// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1852 // __ComObject should not be sealed

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    /// <summary>
    /// __ComObject is the root class for all COM wrappers. This class defines only
    /// the basics. This class is used for wrapping COM objects accessed from managed.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class __ComObject : MarshalByRefObject
    {
        private Hashtable? m_ObjectToDataMap; // Do not rename (runtime relies on this name).

        /// <summary>
        /// Default constructor - can't instantiate this directly.
        /// </summary>
        protected __ComObject()
        {
        }

        /// <summary>
        /// Retrieves the data associated with the specified if such data exists.
        /// </summary>
        internal object? GetData(object key)
        {
            object? data = null;

            // Synchronize access to the map.
            lock (this)
            {
                // If the map hasn't been allocated, then there can be no data for the specified key.
                if (m_ObjectToDataMap != null)
                {
                    // Look up the data in the map.
                    data = m_ObjectToDataMap[key];
                }
            }

            return data;
        }

        /// <summary>
        /// Sets the data for the specified key on the current __ComObject.
        /// </summary>
        internal bool SetData(object key, object? data)
        {
            bool bAdded = false;

            // Synchronize access to the map.
            lock (this)
            {
                // If the map hasn't been allocated yet, allocate it.
                m_ObjectToDataMap ??= new Hashtable();

                // If there isn't already data in the map then add it.
                if (m_ObjectToDataMap[key] == null)
                {
                    m_ObjectToDataMap[key] = data;
                    bAdded = true;
                }
            }

            return bAdded;
        }

        /// <summary>
        /// Called from within the EE and releases all the cached data for the __ComObject.
        /// </summary>
        internal void ReleaseAllData()
        {
            // Synchronize access to the map.
            lock (this)
            {
                // If the map hasn't been allocated, then there is nothing to do.
                if (m_ObjectToDataMap != null)
                {
                    foreach (object? o in m_ObjectToDataMap.Values)
                    {
                        // Note: the value could be an object[]
                        // We are fine for now as object[] doesn't implement IDisposable nor derive from __ComObject

                        // If the object implements IDisposable, then call Dispose on it.
                        if (o is IDisposable DisposableObj)
                            DisposableObj.Dispose();

                        // If the object is a derived from __ComObject, then call Marshal.ReleaseComObject on it.
                        if (o is __ComObject ComObj)
                            Marshal.ReleaseComObject(ComObj);
                    }

                    // Set the map to null to indicate it has been cleaned up.
                    m_ObjectToDataMap = null;
                }
            }
        }

        /// <summary>
        /// Called from within the EE and is used to handle calls on methods of event interfaces.
        /// </summary>
        internal object GetEventProvider(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] RuntimeType t)
        {
            // Check to see if we already have a cached event provider for this type.
            object? provider = GetData(t);
            if (provider != null)
            {
                return provider;
            }

            // If we don't then we need to create one.
            return CreateEventProvider(t);
        }

        private object CreateEventProvider(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] RuntimeType t)
        {
            // Create the event provider for the specified type.
            object EvProvider = Activator.CreateInstance(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new object[] { this }, null)!;

            // Attempt to cache the wrapper on the object.
            if (!SetData(t, EvProvider))
            {
                // Dispose the event provider if it implements IDisposable.
                if (EvProvider is IDisposable DisposableEvProv)
                {
                    DisposableEvProv.Dispose();
                }

                // Another thead already cached the wrapper so use that one instead.
                EvProvider = GetData(t)!;
            }

            return EvProvider;
        }
    }
}
