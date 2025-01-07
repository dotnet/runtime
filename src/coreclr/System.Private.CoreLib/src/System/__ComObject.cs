// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1852 // __ComObject should not be sealed

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.CustomMarshalers;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

using IEnumVARIANT = System.Runtime.InteropServices.ComTypes.IEnumVARIANT;
using DISPPARAMS = System.Runtime.InteropServices.ComTypes.DISPPARAMS;

namespace System
{
    /// <summary>
    /// __ComObject is the root class for all COM wrappers. This class defines only
    /// the basics. This class is used for wrapping COM objects accessed from managed.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class __ComObject : MarshalByRefObject, IDynamicInterfaceCastable
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
            object EvProvider = Activator.CreateInstance(t, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, [this], null)!;

            // Attempt to cache the wrapper on the object.
            if (!SetData(t, EvProvider))
            {
                // Dispose the event provider if it implements IDisposable.
                if (EvProvider is IDisposable DisposableEvProv)
                {
                    DisposableEvProv.Dispose();
                }

                // Another thread already cached the wrapper so use that one instead.
                EvProvider = GetData(t)!;
            }

            return EvProvider;
        }

        bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (interfaceType.Equals(typeof(IEnumerable).TypeHandle))
            {
                return IsIEnumerable(this);
            }
            else if (interfaceType.Equals(typeof(IEnumerator).TypeHandle))
            {
                return IsIEnumerator(this);
            }
            else if (interfaceType.Equals(typeof(ICustomAdapter).TypeHandle))
            {
                // This is for backwards compatibility purposes.
                return IsIEnumerator(this) || IsIEnumerable(this);
            }
            else
            {
                return false;
            }

            static bool IsIEnumerable(__ComObject co)
            {
                try
                {
                    // We just need to know the query was successful.
                    _ = IEnumerableOverDispatchImpl.QueryForNewEnum(co);
                    return true;
                }
                catch
                {
                    // Exception was thrown. There are few options, but all imply "not supported".
                    //  - Not IDispatch.
                    //  - IDispatch doesn't support DISPID_NEWENUM operation.
                    //  - The returned instance isn't IEnumVARIANT.
                    return false;
                }
            }

            static bool IsIEnumerator(__ComObject co)
            {
                return co is IEnumVARIANT;
            }
        }

        RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
        {
            if (interfaceType.Equals(typeof(IEnumerable).TypeHandle))
            {
                return typeof(IEnumerableOverDispatchImpl).TypeHandle;
            }
            else if (interfaceType.Equals(typeof(ICustomAdapter).TypeHandle))
            {
                return typeof(ICustomAdapterOverDispatchImpl).TypeHandle;
            }
            else if (interfaceType.Equals(typeof(IEnumerator).TypeHandle))
            {
                object? enumVariantImpl = GetData(typeof(IEnumeratorOverEnumVARIANTImpl));
                if (enumVariantImpl is null)
                {
                    IntPtr enumVariantPtr = IntPtr.Zero;
                    try
                    {   IEnumVARIANT enumVariant = (IEnumVARIANT)this;
                        enumVariantPtr = Marshal.GetIUnknownForObject(enumVariant);
                        enumVariantImpl = (IEnumerator)EnumeratorToEnumVariantMarshaler.GetInstance(null).MarshalNativeToManaged(enumVariantPtr);
                    }
                    finally
                    {
                        if (enumVariantPtr != IntPtr.Zero)
                        {
                            Marshal.Release(enumVariantPtr);
                        }
                    }
                    SetData(typeof(IEnumeratorOverEnumVARIANTImpl), enumVariantImpl);
                }
                return typeof(IEnumeratorOverEnumVARIANTImpl).TypeHandle;
            }

            return default;
        }

        [DynamicInterfaceCastableImplementation]
        private interface IEnumerableOverDispatchImpl : IEnumerable
        {
            public static IEnumVARIANT QueryForNewEnum(__ComObject obj)
            {
                // Reserved DISPID slot for getting an enumerator from an IDispatch-implementing COM interface.
                const int DISPID_NEWENUM = -4;
                const int LCID_DEFAULT = 1;

                IDispatch dispatch = (IDispatch)obj;
                ComVariant result = default;
                object? resultAsObject = null;
                try
                {
                    unsafe
                    {
                        void* resultLocal = &result;
                        DISPPARAMS dispParams = default;
                        Guid guid = Guid.Empty;
                        dispatch.Invoke(
                            DISPID_NEWENUM,
                            ref guid,
                            LCID_DEFAULT,
                            InvokeFlags.DISPATCH_METHOD | InvokeFlags.DISPATCH_PROPERTYGET,
                            ref dispParams,
                            new IntPtr(resultLocal),
                            IntPtr.Zero,
                            IntPtr.Zero);
                    }
                    resultAsObject = result.ToObject();
                }
                finally
                {
                    result.Dispose();
                }

                if (resultAsObject is not IEnumVARIANT enumVariant)
                {
                    throw new InvalidOperationException(SR.InvalidOp_InvalidNewEnumVariant);
                }

                return enumVariant;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                IEnumVARIANT enumVariant = QueryForNewEnum((__ComObject)this);
                IntPtr enumVariantPtr = IntPtr.Zero;
                try
                {
                    enumVariantPtr = Marshal.GetIUnknownForObject(enumVariant);
                    return (IEnumerator)EnumeratorToEnumVariantMarshaler.GetInstance(null).MarshalNativeToManaged(enumVariantPtr);
                }
                finally
                {
                    if (enumVariantPtr != IntPtr.Zero)
                    {
                        Marshal.Release(enumVariantPtr);
                    }
                }
            }
        }

        [DynamicInterfaceCastableImplementation]
        private interface ICustomAdapterOverDispatchImpl : ICustomAdapter
        {
            object ICustomAdapter.GetUnderlyingObject() => this;
        }

        [DynamicInterfaceCastableImplementation]
        private interface IEnumeratorOverEnumVARIANTImpl : IEnumerator
        {
            bool IEnumerator.MoveNext()
            {
                __ComObject co = (__ComObject)this;
                return ((IEnumerator)co.GetData(typeof(IEnumeratorOverEnumVARIANTImpl))!).MoveNext();
            }

            void IEnumerator.Reset()
            {
                __ComObject co = (__ComObject)this;
                ((IEnumerator)co.GetData(typeof(IEnumeratorOverEnumVARIANTImpl))!).Reset();
            }

            object IEnumerator.Current
            {
                get
                {
                    __ComObject co = (__ComObject)this;
                    return ((IEnumerator)co.GetData(typeof(IEnumeratorOverEnumVARIANTImpl))!).Current;
                }
            }
        }
    }
}
