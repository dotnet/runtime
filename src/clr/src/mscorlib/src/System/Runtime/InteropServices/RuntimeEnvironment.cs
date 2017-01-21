// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Runtime information
**          
**
=============================================================================*/

using System;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using Microsoft.Win32;
using System.Runtime.Versioning;
using StackCrawlMark = System.Threading.StackCrawlMark;

namespace System.Runtime.InteropServices
{
    [System.Runtime.InteropServices.ComVisible(true)]
    static public class RuntimeEnvironment {

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String GetModuleFileName();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String GetDeveloperPath();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String GetHostBindingFile();

        public static bool FromGlobalAccessCache(Assembly a)
        {
            return a.GlobalAssemblyCache;
        }

        [MethodImpl (MethodImplOptions.NoInlining)]
        public static String GetSystemVersion()
        {
            return Assembly.GetExecutingAssembly().ImageRuntimeVersion;
        }

        public static String GetRuntimeDirectory()
        {
            String dir = GetRuntimeDirectoryImpl();
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, dir).Demand();
            return dir;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String GetRuntimeDirectoryImpl();
        
#if FEATURE_COMINTEROP
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern IntPtr GetRuntimeInterfaceImpl(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid clsid,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        //
        // This function does the equivalent of calling GetInterface(clsid, riid) on the
        // ICLRRuntimeInfo representing this runtime. See MetaHost.idl for a list of
        // CLSIDs and IIDs supported by this method.
        //
        // Returns unmanaged pointer to requested interface on success. Throws
        // COMException with failed HR if there is a QI failure.
        //
        [ComVisible(false)]
        public static IntPtr GetRuntimeInterfaceAsIntPtr(Guid clsid, Guid riid)
        {
            return GetRuntimeInterfaceImpl(clsid, riid);
        }

        //
        // This function does the equivalent of calling GetInterface(clsid, riid) on the
        // ICLRRuntimeInfo representing this runtime. See MetaHost.idl for a list of
        // CLSIDs and IIDs supported by this method.
        //
        // Returns an RCW to requested interface on success. Throws
        // COMException with failed HR if there is a QI failure.
        //
        [ComVisible(false)]
        public static object GetRuntimeInterfaceAsObject(Guid clsid, Guid riid)
        {
            IntPtr p = IntPtr.Zero;
            try {
                p = GetRuntimeInterfaceImpl(clsid, riid);
                return Marshal.GetObjectForIUnknown(p);
            } finally {
                if(p != IntPtr.Zero) {
                    Marshal.Release(p);
                }
            }
        }
#endif // FEATURE_COMINTEROP
    }
}
