// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** __ComObject is the root class for all COM wrappers.  This class
** defines only the basics. This class is used for wrapping COM objects
** accessed from COM+
**
** 
===========================================================*/

using System;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace System
{
    internal class __ComObject : MarshalByRefObject
    {
        private Hashtable m_ObjectToDataMap;

        /*============================================================
        ** default constructor
        ** can't instantiate this directly
        =============================================================*/
        protected __ComObject()
        {
        }

        //====================================================================
        // Overrides ToString() to make sure we call to IStringable if the 
        // COM object implements it in the case of weakly typed RCWs
        //====================================================================
        public override string ToString()
        {
            //
            // Only do the IStringable cast when running under AppX for better compat
            // Otherwise we could do a IStringable cast in classic apps which could introduce
            // a thread transition which would lead to deadlock
            //
            if (AppDomain.IsAppXModel())
            {
                // Check whether the type implements IStringable.
                IStringable stringableType = this as IStringable;
                if (stringableType != null)
                {
                    return stringableType.ToString();
                }
            }

            return base.ToString();
        }

        //====================================================================
        // This method retrieves the data associated with the specified
        // key if any such data exists for the current __ComObject.
        //====================================================================
        internal object GetData(object key)
        {
            object data = null;

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

        //====================================================================
        // This method sets the data for the specified key on the current 
        // __ComObject.
        //====================================================================
        internal bool SetData(object key, object data)
        {
            bool bAdded = false;

            // Synchronize access to the map.
            lock (this)
            {
                // If the map hasn't been allocated yet, allocate it.
                if (m_ObjectToDataMap == null)
                    m_ObjectToDataMap = new Hashtable();

                // If there isn't already data in the map then add it.
                if (m_ObjectToDataMap[key] == null)
                {
                    m_ObjectToDataMap[key] = data;
                    bAdded = true;
                }
            }

            return bAdded;
        }

        //====================================================================
        // This method is called from within the EE and releases all the 
        // cached data for the __ComObject.
        //====================================================================
        internal void ReleaseAllData()
        {
            // Synchronize access to the map.
            lock (this)
            {
                // If the map hasn't been allocated, then there is nothing to do.
                if (m_ObjectToDataMap != null)
                {
                    foreach (object o in m_ObjectToDataMap.Values)
                    {
                        // Note: the value could be an object[]
                        // We are fine for now as object[] doesn't implement IDisposable nor derive from __ComObject

                        // If the object implements IDisposable, then call Dispose on it.
                        IDisposable DisposableObj = o as IDisposable;
                        if (DisposableObj != null)
                            DisposableObj.Dispose();

                        // If the object is a derived from __ComObject, then call Marshal.ReleaseComObject on it.
                        __ComObject ComObj = o as __ComObject;
                        if (ComObj != null)
                            Marshal.ReleaseComObject(ComObj);
                    }

                    // Set the map to null to indicate it has been cleaned up.
                    m_ObjectToDataMap = null;
                }
            }
        }

        //====================================================================
        // This method is called from within the EE and is used to handle
        // calls on methods of event interfaces.
        //====================================================================
        internal object GetEventProvider(RuntimeType t)
        {
            // Check to see if we already have a cached event provider for this type.
            object EvProvider = GetData(t);

            // If we don't then we need to create one.
            if (EvProvider == null)
                EvProvider = CreateEventProvider(t);

            return EvProvider;
        }

        internal int ReleaseSelf()
        {
            return Marshal.InternalReleaseComObject(this);
        }

        internal void FinalReleaseSelf()
        {
            Marshal.InternalFinalReleaseComObject(this);
        }

        private object CreateEventProvider(RuntimeType t)
        {
            // Create the event provider for the specified type.
            object EvProvider = Activator.CreateInstance(t, Activator.ConstructorDefault | BindingFlags.NonPublic, null, new object[] { this }, null);

            // Attempt to cache the wrapper on the object.
            if (!SetData(t, EvProvider))
            {
                // Dispose the event provider if it implements IDisposable.
                IDisposable DisposableEvProv = EvProvider as IDisposable;
                if (DisposableEvProv != null)
                    DisposableEvProv.Dispose();

                // Another thead already cached the wrapper so use that one instead.
                EvProvider = GetData(t);
            }

            return EvProvider;
        }
    }
}
