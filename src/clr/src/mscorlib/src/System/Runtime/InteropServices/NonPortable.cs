// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Dummy implementations of non-portable interop methods that just throw PlatformNotSupportedException

namespace System.Runtime.InteropServices
{
    public  static partial class Marshal
    {
        [System.Security.SecurityCritical]
        public static int GetHRForException(Exception e)
        {
            return (e != null) ? e.HResult : 0;
        }

        [System.Security.SecurityCriticalAttribute]
        public static int AddRef(System.IntPtr pUnk)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static bool AreComObjectsAvailableForCleanup()
        { 
            return false;
        }

        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr CreateAggregatedObject(System.IntPtr pOuter, object o)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr CreateAggregatedObject<T>(System.IntPtr pOuter, T o)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static object CreateWrapperOfType(object o, System.Type t)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static TWrapper CreateWrapperOfType<T, TWrapper>(T o)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static int FinalReleaseComObject(object o)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetComInterfaceForObject(object o, System.Type T)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetComInterfaceForObject(object o, System.Type T, System.Runtime.InteropServices.CustomQueryInterfaceMode mode)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetComInterfaceForObject<T, TInterface>(T o)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static System.IntPtr GetIUnknownForObject(object o)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static void GetNativeVariantForObject(object obj, System.IntPtr pDstNativeVariant)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static void GetNativeVariantForObject<T>(T obj, System.IntPtr pDstNativeVariant)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static object GetObjectForIUnknown(System.IntPtr pUnk)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static object GetObjectForNativeVariant(System.IntPtr pSrcNativeVariant)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static T GetObjectForNativeVariant<T>(System.IntPtr pSrcNativeVariant)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static object[] GetObjectsForNativeVariants(System.IntPtr aSrcNativeVariant, int cVars)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static T[] GetObjectsForNativeVariants<T>(System.IntPtr aSrcNativeVariant, int cVars)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static int GetStartComSlot(System.Type t)
        {
            throw new PlatformNotSupportedException();
        }

        public static System.Type GetTypeFromCLSID(System.Guid clsid) 
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static string GetTypeInfoName(System.Runtime.InteropServices.ComTypes.ITypeInfo typeInfo)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static object GetUniqueObjectForIUnknown(System.IntPtr unknown)
        {
            throw new PlatformNotSupportedException();
        }

        public static bool IsComObject(object o)
        { 
            return false;
        }

        [System.Security.SecurityCriticalAttribute]
        public static int QueryInterface(System.IntPtr pUnk, ref System.Guid iid, out System.IntPtr ppv)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static int Release(System.IntPtr pUnk)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static int ReleaseComObject(object o)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static void ZeroFreeBSTR(System.IntPtr s)
        {
            throw new PlatformNotSupportedException();
        }
    }

    public class DispatchWrapper
    {
        public DispatchWrapper(object obj)
        {
            throw new PlatformNotSupportedException();
        }

        public object WrappedObject
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }
    }

    public static class ComEventsHelper
    {
        [System.Security.SecurityCriticalAttribute]
        public static void Combine(object rcw, System.Guid iid, int dispid, System.Delegate d)
        {
            throw new PlatformNotSupportedException();
        }

        [System.Security.SecurityCriticalAttribute]
        public static System.Delegate Remove(object rcw, System.Guid iid, int dispid, System.Delegate d)
        {
            throw new PlatformNotSupportedException();
        }
    }
}

