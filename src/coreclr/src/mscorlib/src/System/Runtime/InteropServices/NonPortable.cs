// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Dummy implementations of non-portable interop methods that just throw PlatformNotSupportedException

namespace System.Runtime.InteropServices
{
    public  static partial class Marshal
    {
        public static int GetHRForException(Exception e)
        {
            return (e != null) ? e.HResult : 0;
        }

        public static int AddRef(System.IntPtr pUnk)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static bool AreComObjectsAvailableForCleanup()
        { 
            return false;
        }

        public static System.IntPtr CreateAggregatedObject(System.IntPtr pOuter, object o)
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

        public static System.IntPtr CreateAggregatedObject<T>(System.IntPtr pOuter, T o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object CreateWrapperOfType(object o, System.Type t)
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

        public static System.IntPtr GetComInterfaceForObject(object o, System.Type T)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static System.IntPtr GetComInterfaceForObject(object o, System.Type T, System.Runtime.InteropServices.CustomQueryInterfaceMode mode)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static System.IntPtr GetComInterfaceForObject<T, TInterface>(T o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static System.IntPtr GetHINSTANCE(System.Reflection.Module m)
        {
            if (m == null)
            {
                throw new ArgumentNullException(nameof(m));
            }
            return (System.IntPtr) (-1);
        }           

        public static System.IntPtr GetIUnknownForObject(object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static void GetNativeVariantForObject(object obj, System.IntPtr pDstNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static void GetNativeVariantForObject<T>(T obj, System.IntPtr pDstNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static Object GetTypedObjectForIUnknown(System.IntPtr pUnk, System.Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object GetObjectForIUnknown(System.IntPtr pUnk)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object GetObjectForNativeVariant(System.IntPtr pSrcNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static T GetObjectForNativeVariant<T>(System.IntPtr pSrcNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object[] GetObjectsForNativeVariants(System.IntPtr aSrcNativeVariant, int cVars)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static T[] GetObjectsForNativeVariants<T>(System.IntPtr aSrcNativeVariant, int cVars)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static int GetStartComSlot(System.Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static System.Type GetTypeFromCLSID(System.Guid clsid) 
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static string GetTypeInfoName(System.Runtime.InteropServices.ComTypes.ITypeInfo typeInfo)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static object GetUniqueObjectForIUnknown(System.IntPtr unknown)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static bool IsComObject(object o)
        { 
            return false;
        }

        public static int QueryInterface(System.IntPtr pUnk, ref System.Guid iid, out System.IntPtr ppv)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static int Release(System.IntPtr pUnk)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static int ReleaseComObject(object o)
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

        public object WrappedObject
        {
            get
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
            }
        }
    }

    public static class ComEventsHelper
    {
        public static void Combine(object rcw, System.Guid iid, int dispid, System.Delegate d)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static System.Delegate Remove(object rcw, System.Guid iid, int dispid, System.Delegate d)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }
    }
}

