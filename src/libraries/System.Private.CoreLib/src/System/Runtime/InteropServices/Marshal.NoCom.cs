// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        public static int GetHRForException(Exception? e)
        {
            return e?.HResult ?? 0;
        }

        public static bool AreComObjectsAvailableForCleanup() => false;

        [SupportedOSPlatform("windows")]
        public static IntPtr CreateAggregatedObject(IntPtr pOuter, object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
        [SupportedOSPlatform("windows")]
        public static object BindToMoniker(string monikerName)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static void CleanupUnusedObjectsInCurrentContext()
        {
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o) where T : notnull
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object? CreateWrapperOfType(object? o, Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static TWrapper CreateWrapperOfType<T, TWrapper>(T? o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static void ChangeWrapperHandleStrength(object otp, bool fIsWeak)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static int FinalReleaseComObject(object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetComInterfaceForObject(object o, Type T)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetComInterfaceForObject(object o, Type T, CustomQueryInterfaceMode mode)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetComInterfaceForObject<T, TInterface>([DisallowNull] T o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object? GetComObjectData(object obj, object key)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetIDispatchForObject(object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static IntPtr GetIUnknownForObject(object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static void GetNativeVariantForObject(object? obj, IntPtr pDstNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static void GetNativeVariantForObject<T>(T? obj, IntPtr pDstNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object GetTypedObjectForIUnknown(IntPtr pUnk, Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object GetObjectForIUnknown(IntPtr pUnk)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object? GetObjectForNativeVariant(IntPtr pSrcNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static T? GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object?[] GetObjectsForNativeVariants(IntPtr aSrcNativeVariant, int cVars)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static int GetStartComSlot(Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static int GetEndComSlot(Type t)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        internal static Type? GetTypeFromCLSID(Guid clsid, string? server, bool throwOnError)
        {
            if (throwOnError)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);

            return null;
        }

        [SupportedOSPlatform("windows")]
        public static string GetTypeInfoName(ITypeInfo typeInfo)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static object GetUniqueObjectForIUnknown(IntPtr unknown)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        public static bool IsComObject(object o)
        {
            if (o is null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            return false;
        }

        public static bool IsTypeVisibleFromCom(Type t)
        {
            if (t is null)
            {
                throw new ArgumentNullException(nameof(t));
            }
            return false;
        }

        [SupportedOSPlatform("windows")]
        public static int ReleaseComObject(object o)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }

        [SupportedOSPlatform("windows")]
        public static bool SetComObjectData(object obj, object key, object? data)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
        }
    }
}
