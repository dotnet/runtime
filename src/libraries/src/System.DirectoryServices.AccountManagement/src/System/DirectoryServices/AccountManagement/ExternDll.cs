// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal static class ExternDll
    {
#if FEATURE_PAL && !SILVERLIGHT

#if !PLATFORM_UNIX
        internal const String DLLPREFIX = "";
        internal const String DLLSUFFIX = ".dll";
#else // !PLATFORM_UNIX
#if __APPLE__
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".dylib";
#elif _AIX
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".a";
#elif __hppa__ || IA64
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".sl";
#else
        internal const String DLLPREFIX = "lib";
        internal const String DLLSUFFIX = ".so";
#endif
#endif // !PLATFORM_UNIX

        public const string Kernel32 = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        public const string User32 = DLLPREFIX + "rotor_pal" + DLLSUFFIX;
        public const string Mscoree  = DLLPREFIX + "sscoree" + DLLSUFFIX;

#elif FEATURE_PAL && SILVERLIGHT

        public const string Kernel32 = "coreclr";
        public const string User32 = "coreclr";

#else
        public const string Activeds = "activeds.dll";
        public const string Advapi32 = "advapi32.dll";
        public const string Comctl32 = "comctl32.dll";
        public const string Comdlg32 = "comdlg32.dll";
        public const string Gdi32 = "gdi32.dll";
        public const string Gdiplus = "gdiplus.dll";
        public const string Hhctrl = "hhctrl.ocx";
        public const string Imm32 = "imm32.dll";
        public const string Kernel32 = "kernel32.dll";
        public const string Loadperf = "Loadperf.dll";
        public const string Mscoree = "mscoree.dll";
        public const string Clr = "clr.dll";
        public const string Msi = "msi.dll";
        public const string Mqrt = "mqrt.dll";
        public const string Ntdll = "ntdll.dll";
        public const string Ole32 = "ole32.dll";
        public const string Oleacc = "oleacc.dll";
        public const string Oleaut32 = "oleaut32.dll";
        public const string Olepro32 = "olepro32.dll";
        public const string PerfCounter = "perfcounter.dll";
        public const string Powrprof = "Powrprof.dll";
        public const string Psapi = "psapi.dll";
        public const string Shell32 = "shell32.dll";
        public const string User32 = "user32.dll";
        public const string Uxtheme = "uxtheme.dll";
        public const string WinMM = "winmm.dll";
        public const string Winspool = "winspool.drv";
        public const string Wtsapi32 = "wtsapi32.dll";
        public const string Version = "version.dll";
        public const string Vsassert = "vsassert.dll";
        public const string Fxassert = "Fxassert.dll";
        public const string Shlwapi = "shlwapi.dll";
        public const string Crypt32 = "crypt32.dll";

        // system.data specific
        internal const string Odbc32 = "odbc32.dll";
        internal const string SNI = "System.Data.dll";

        // system.data.oracleclient specific
        internal const string OciDll = "oci.dll";
        internal const string OraMtsDll = "oramts.dll";
#endif //!FEATURE_PAL
    }
}
