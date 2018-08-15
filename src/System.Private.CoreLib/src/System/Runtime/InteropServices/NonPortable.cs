// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Dummy implementations of non-portable interop methods that just throw PlatformNotSupportedException

using System.Reflection;
using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        public static int GetHRForException(Exception e)
        {
            return (e != null) ? e.HResult : 0;
        }

        public static int AddRef(IntPtr pUnk)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static bool AreComObjectsAvailableForCleanup() => false;

        public static IntPtr CreateAggregatedObject(IntPtr pOuter, object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static Object BindToMoniker(String monikerName)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static void CleanupUnusedObjectsInCurrentContext()
        {
            return;
        }

        public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object CreateWrapperOfType(object o, Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static TWrapper CreateWrapperOfType<T, TWrapper>(T o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static void ChangeWrapperHandleStrength(Object otp, bool fIsWeak)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static int FinalReleaseComObject(object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static IntPtr GetComInterfaceForObject(object o, Type T)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static IntPtr GetComInterfaceForObject(object o, Type T, CustomQueryInterfaceMode mode)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static IntPtr GetComInterfaceForObject<T, TInterface>(T o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object GetComObjectData(object obj, object key)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static IntPtr GetHINSTANCE(Module m)
        {
            if (m == null)
            {
                throw new ArgumentNullException(nameof(m));
            }

            return (IntPtr) (-1);
        }           

        public static IntPtr GetIUnknownForObject(object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static void GetNativeVariantForObject(object obj, IntPtr pDstNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static void GetNativeVariantForObject<T>(T obj, IntPtr pDstNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static Object GetTypedObjectForIUnknown(IntPtr pUnk, Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object GetObjectForIUnknown(IntPtr pUnk)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object GetObjectForNativeVariant(IntPtr pSrcNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static T GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object[] GetObjectsForNativeVariants(IntPtr aSrcNativeVariant, int cVars)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static int GetStartComSlot(Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static int GetEndComSlot(Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static Type GetTypeFromCLSID(Guid clsid) 
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static string GetTypeInfoName(ITypeInfo typeInfo)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object GetUniqueObjectForIUnknown(IntPtr unknown)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static bool IsComObject(object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            return false;
        }

        public static bool IsTypeVisibleFromCom(Type t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }
            return false;
        }

        public static int QueryInterface(IntPtr pUnk, ref Guid iid, out IntPtr ppv)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static int Release(IntPtr pUnk)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static int ReleaseComObject(object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static bool SetComObjectData(object obj, object key, object data)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }
    }

    public class DispatchWrapper
    {
        public DispatchWrapper(object obj)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public object WrappedObject => throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
    }

    public static class ComEventsHelper
    {
        public static void Combine(object rcw, Guid iid, int dispid, Delegate d)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static Delegate Remove(object rcw, Guid iid, int dispid, Delegate d)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }
    }
}
