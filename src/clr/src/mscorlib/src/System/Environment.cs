// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Provides some basic access to some environment 
** functionality.
**
**
============================================================*/

namespace System
{
    using System.IO;
    using System.Security;
    using System.Resources;
    using System.Globalization;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Configuration.Assemblies;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Diagnostics;
    using Microsoft.Win32;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    public enum EnvironmentVariableTarget
    {
        Process = 0,
        User = 1,
        Machine = 2,
    }

    internal static partial class Environment
    {
        // Assume the following constants include the terminating '\0' - use <, not <=
        private const int MaxEnvVariableValueLength = 32767;  // maximum length for environment variable name and value
        // System environment variables are stored in the registry, and have 
        // a size restriction that is separate from both normal environment 
        // variables and registry value name lengths, according to MSDN.
        // MSDN doesn't detail whether the name is limited to 1024, or whether
        // that includes the contents of the environment variable.
        private const int MaxSystemEnvVariableLength = 1024;
        private const int MaxUserEnvVariableLength = 255;
        private const int MaxMachineNameLength = 256;

        // Looks up the resource string value for key.
        // 
        // if you change this method's signature then you must change the code that calls it
        // in excep.cpp and probably you will have to visit mscorlib.h to add the new signature
        // as well as metasig.h to create the new signature type
        internal static String GetResourceStringLocal(String key)
        {
            return SR.GetResourceString(key);
        }

        // Private object for locking instead of locking on a public type for SQL reliability work.
        private static Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject;
            }
        }

        /*==================================TickCount===================================
        **Action: Gets the number of ticks since the system was started.
        **Returns: The number of ticks since the system was started.
        **Arguments: None
        **Exceptions: None
        ==============================================================================*/
        public static extern int TickCount
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        // Terminates this process with the given exit code.
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        internal static extern void _Exit(int exitCode);

        public static void Exit(int exitCode)
        {
            _Exit(exitCode);
        }


        public static extern int ExitCode
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;

            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            set;
        }

        // Note: The CLR's Watson bucketization code looks at the caller of the FCALL method
        // to assign blame for crashes.  Don't mess with this, such as by making it call 
        // another managed helper method, unless you consult with some CLR Watson experts.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void FailFast(String message);

        // This overload of FailFast will allow you to specify the exception object
        // whose bucket details *could* be used when undergoing the failfast process.
        // To be specific:
        //
        // 1) When invoked from within a managed EH clause (fault/finally/catch),
        //    if the exception object is preallocated, the runtime will try to find its buckets
        //    and use them. If the exception object is not preallocated, it will use the bucket
        //    details contained in the object (if any).
        //
        // 2) When invoked from outside the managed EH clauses (fault/finally/catch),
        //    if the exception object is preallocated, the runtime will use the callsite's
        //    IP for bucketing. If the exception object is not preallocated, it will use the bucket
        //    details contained in the object (if any).
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern void FailFast(String message, Exception exception);

        /*===============================CurrentDirectory===============================
        **Action:  Provides a getter and setter for the current directory.  The original
        **         current directory is the one from which the process was started.  
        **Returns: The current directory (from the getter).  Void from the setter.
        **Arguments: The current directory to which to switch to the setter.
        **Exceptions: 
        ==============================================================================*/
        internal static String CurrentDirectory
        {
            get
            {
                return Directory.GetCurrentDirectory();
            }

            set
            {
                Directory.SetCurrentDirectory(value);
            }
        }

        // Returns the system directory (ie, C:\WinNT\System32).
        internal static String SystemDirectory
        {
            get
            {
                StringBuilder sb = new StringBuilder(Path.MaxPath);
                int r = Win32Native.GetSystemDirectory(sb, Path.MaxPath);
                Debug.Assert(r < Path.MaxPath, "r < Path.MaxPath");
                if (r == 0) __Error.WinIOError();
                String path = sb.ToString();

                return path;
            }
        }

        public static String ExpandEnvironmentVariables(String name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Contract.EndContractBlock();

            if (name.Length == 0)
            {
                return name;
            }

            int currentSize = 100;
            StringBuilder blob = new StringBuilder(currentSize); // A somewhat reasonable default size

#if PLATFORM_UNIX // Win32Native.ExpandEnvironmentStrings isn't available
            int lastPos = 0, pos;
            while (lastPos < name.Length && (pos = name.IndexOf('%', lastPos + 1)) >= 0)
            {
                if (name[lastPos] == '%')
                {
                    string key = name.Substring(lastPos + 1, pos - lastPos - 1);
                    string value = Environment.GetEnvironmentVariable(key);
                    if (value != null)
                    {
                        blob.Append(value);
                        lastPos = pos + 1;
                        continue;
                    }
                }
                blob.Append(name.Substring(lastPos, pos - lastPos));
                lastPos = pos;
            }
            blob.Append(name.Substring(lastPos));
#else

            int size;

            blob.Length = 0;
            size = Win32Native.ExpandEnvironmentStrings(name, blob, currentSize);
            if (size == 0)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            while (size > currentSize)
            {
                currentSize = size;
                blob.Capacity = currentSize;
                blob.Length = 0;

                size = Win32Native.ExpandEnvironmentStrings(name, blob, currentSize);
                if (size == 0)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
#endif // PLATFORM_UNIX

            return blob.ToString();
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        private static extern Int32 GetProcessorCount();

        public static int ProcessorCount
        {
            get
            {
                return GetProcessorCount();
            }
        }

        /*==============================GetCommandLineArgs==============================
        **Action: Gets the command line and splits it appropriately to deal with whitespace,
        **        quotes, and escape characters.
        **Returns: A string array containing your command line arguments.
        **Arguments: None
        **Exceptions: None.
        ==============================================================================*/
        public static String[] GetCommandLineArgs()
        {
            /*
             * There are multiple entry points to a hosted app.
             * The host could use ::ExecuteAssembly() or ::CreateDelegate option
             * ::ExecuteAssembly() -> In this particular case, the runtime invokes the main 
               method based on the arguments set by the host, and we return those arguments
             *
             * ::CreateDelegate() -> In this particular case, the host is asked to create a 
             * delegate based on the appDomain, assembly and methodDesc passed to it.
             * which the caller uses to invoke the method. In this particular case we do not have
             * any information on what arguments would be passed to the delegate.
             * So our best bet is to simply use the commandLine that was used to invoke the process.
             * in case it is present.
             */
            if (s_CommandLineArgs != null)
                return (string[])s_CommandLineArgs.Clone();

            return GetCommandLineArgsNative();
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern String[] GetCommandLineArgsNative();

        private static string[] s_CommandLineArgs = null;
        private static void SetCommandLineArgs(string[] cmdLineArgs)
        {
            s_CommandLineArgs = cmdLineArgs;
        }

        private unsafe static char[] GetEnvironmentCharArray()
        {
            char[] block = null;

            // Make sure pStrings is not leaked with async exceptions
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                char* pStrings = null;

                try
                {
                    pStrings = Win32Native.GetEnvironmentStrings();
                    if (pStrings == null)
                    {
                        throw new OutOfMemoryException();
                    }

                    // Format for GetEnvironmentStrings is:
                    // [=HiddenVar=value\0]* [Variable=value\0]* \0
                    // See the description of Environment Blocks in MSDN's
                    // CreateProcess page (null-terminated array of null-terminated strings).

                    // Search for terminating \0\0 (two unicode \0's).
                    char* p = pStrings;
                    while (!(*p == '\0' && *(p + 1) == '\0'))
                        p++;

                    int len = (int)(p - pStrings + 1);
                    block = new char[len];

                    fixed (char* pBlock = block)
                        string.wstrcpy(pBlock, pStrings, len);
                }
                finally
                {
                    if (pStrings != null)
                        Win32Native.FreeEnvironmentStrings(pStrings);
                }
            }

            return block;
        }

        /*===================================NewLine====================================
        **Action: A property which returns the appropriate newline string for the given
        **        platform.
        **Returns: \r\n on Win32.
        **Arguments: None.
        **Exceptions: None.
        ==============================================================================*/
        public static String NewLine
        {
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
#if PLATFORM_WINDOWS
                return "\r\n";
#else
                return "\n";
#endif // PLATFORM_WINDOWS
            }
        }


        /*===================================Version====================================
        **Action: Returns the COM+ version struct, describing the build number.
        **Returns:
        **Arguments:
        **Exceptions:
        ==============================================================================*/
        public static Version Version
        {
            get
            {
                // Previously this represented the File version of mscorlib.dll.  Many other libraries in the framework and outside took dependencies on the first three parts of this version 
                // remaining constant throughout 4.x.  From 4.0 to 4.5.2 this was fine since the file version only incremented the last part.Starting with 4.6 we switched to a file versioning
                // scheme that matched the product version.  In order to preserve compatibility with existing libraries, this needs to be hard-coded.

                return new Version(4, 0, 30319, 42000);
            }
        }

#if !FEATURE_PAL
        private static Lazy<bool> s_IsWindows8OrAbove = new Lazy<bool>(() => 
        {
            ulong conditionMask = Win32Native.VerSetConditionMask(0, Win32Native.VER_MAJORVERSION, Win32Native.VER_GREATER_EQUAL);
            conditionMask = Win32Native.VerSetConditionMask(conditionMask, Win32Native.VER_MINORVERSION, Win32Native.VER_GREATER_EQUAL);
            conditionMask = Win32Native.VerSetConditionMask(conditionMask, Win32Native.VER_SERVICEPACKMAJOR, Win32Native.VER_GREATER_EQUAL);
            conditionMask = Win32Native.VerSetConditionMask(conditionMask, Win32Native.VER_SERVICEPACKMINOR, Win32Native.VER_GREATER_EQUAL);

            // Windows 8 version is 6.2
            var version = new Win32Native.OSVERSIONINFOEX { MajorVersion = 6, MinorVersion = 2, ServicePackMajor = 0, ServicePackMinor = 0 };
                
            return Win32Native.VerifyVersionInfoW(version, 
                       Win32Native.VER_MAJORVERSION | Win32Native.VER_MINORVERSION | Win32Native.VER_SERVICEPACKMAJOR | Win32Native.VER_SERVICEPACKMINOR,
                       conditionMask);
        });
        internal static bool IsWindows8OrAbove => s_IsWindows8OrAbove.Value;
#endif
        
#if FEATURE_COMINTEROP
        // Does the current version of Windows have Windows Runtime suppport?
        private static Lazy<bool> s_IsWinRTSupported = new Lazy<bool>(() =>
        {
            return WinRTSupported();
        });

        internal static bool IsWinRTSupported => s_IsWinRTSupported.Value;

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WinRTSupported();
#endif // FEATURE_COMINTEROP


        /*==================================StackTrace==================================
        **Action:
        **Returns:
        **Arguments:
        **Exceptions:
        ==============================================================================*/
        public static String StackTrace
        {
            [MethodImpl(MethodImplOptions.NoInlining)] // Prevent inlining from affecting where the stacktrace starts
            get
            {
                Contract.Ensures(Contract.Result<String>() != null);
                return Internal.Runtime.Augments.EnvironmentAugments.StackTrace;
            }
        }

        internal static String GetStackTrace(Exception e, bool needFileInfo)
        {
            // Note: Setting needFileInfo to true will start up COM and set our
            // apartment state.  Try to not call this when passing "true" 
            // before the EE's ExecuteMainMethod has had a chance to set up the
            // apartment state.  -- 
            StackTrace st;
            if (e == null)
                st = new StackTrace(needFileInfo);
            else
                st = new StackTrace(e, needFileInfo);

            // Do no include a trailing newline for backwards compatibility
            return st.ToString(System.Diagnostics.StackTrace.TraceFormat.Normal);
        }

        public static extern bool HasShutdownStarted
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        internal static bool UserInteractive
        {
            get
            {
                return true;
            }
        }
        public static int CurrentManagedThreadId
        {
            get
            {
                return Thread.CurrentThread.ManagedThreadId;
            }
        }

        internal static extern int CurrentProcessorNumber
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        // The upper bits of t_executionIdCache are the executionId. The lower bits of
        // the t_executionIdCache are counting down to get it periodically refreshed.
        // TODO: Consider flushing the executionIdCache on Wait operations or similar 
        // actions that are likely to result in changing the executing core
        [ThreadStatic]
        private static int t_executionIdCache;

        private const int ExecutionIdCacheShift = 16;
        private const int ExecutionIdCacheCountDownMask = (1 << ExecutionIdCacheShift) - 1;
        private const int ExecutionIdRefreshRate = 5000;

        private static int RefreshExecutionId()
        {
            int executionId = CurrentProcessorNumber;

            // On Unix, CurrentProcessorNumber is implemented in terms of sched_getcpu, which
            // doesn't exist on all platforms.  On those it doesn't exist on, GetCurrentProcessorNumber
            // returns -1.  As a fallback in that case and to spread the threads across the buckets
            // by default, we use the current managed thread ID as a proxy.
            if (executionId < 0) executionId = Environment.CurrentManagedThreadId;

            Debug.Assert(ExecutionIdRefreshRate <= ExecutionIdCacheCountDownMask);

            // Mask with Int32.MaxValue to ensure the execution Id is not negative
            t_executionIdCache = ((executionId << ExecutionIdCacheShift) & Int32.MaxValue) | ExecutionIdRefreshRate;

            return executionId;
        }

        // Cached processor number used as a hint for which per-core stack to access. It is periodically
        // refreshed to trail the actual thread core affinity.
        internal static int CurrentExecutionId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int executionIdCache = t_executionIdCache--;
                if ((executionIdCache & ExecutionIdCacheCountDownMask) == 0)
                    return RefreshExecutionId();
                return (executionIdCache >> ExecutionIdCacheShift);
            }
        }

        public static string GetEnvironmentVariable(string variable)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            return GetEnvironmentVariableCore(variable);
        }

        internal static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            ValidateTarget(target);

            return GetEnvironmentVariableCore(variable, target);
        }

        public static IDictionary GetEnvironmentVariables()
        {
            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            return GetEnvironmentVariablesCore();
        }

        internal static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            ValidateTarget(target);

            return GetEnvironmentVariablesCore(target);
        }

        public static void SetEnvironmentVariable(string variable, string value)
        {
            ValidateVariableAndValue(variable, ref value);

            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            SetEnvironmentVariableCore(variable, value);
        }

        internal static void SetEnvironmentVariable(string variable, string value, EnvironmentVariableTarget target)
        {
            ValidateVariableAndValue(variable, ref value);
            ValidateTarget(target);

            SetEnvironmentVariableCore(variable, value, target);
        }

        private static void ValidateVariableAndValue(string variable, ref string value)
        {
            const int MaxEnvVariableValueLength = 32767;

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }
            if (variable.Length == 0)
            {
                throw new ArgumentException(SR.Argument_StringZeroLength, nameof(variable));
            }
            if (variable[0] == '\0')
            {
                throw new ArgumentException(SR.Argument_StringFirstCharIsZero, nameof(variable));
            }
            if (variable.Length >= MaxEnvVariableValueLength)
            {
                throw new ArgumentException(SR.Argument_LongEnvVarValue, nameof(variable));
            }
            if (variable.IndexOf('=') != -1)
            {
                throw new ArgumentException(SR.Argument_IllegalEnvVarName, nameof(variable));
            }

            if (string.IsNullOrEmpty(value) || value[0] == '\0')
            {
                // Explicitly null out value if it's empty
                value = null;
            }
            else if (value.Length >= MaxEnvVariableValueLength)
            {
                throw new ArgumentException(SR.Argument_LongEnvVarValue, nameof(value));
            }
        }

        private static void ValidateTarget(EnvironmentVariableTarget target)
        {
            if (target != EnvironmentVariableTarget.Process &&
                target != EnvironmentVariableTarget.Machine &&
                target != EnvironmentVariableTarget.User)
            {
                throw new ArgumentOutOfRangeException(nameof(target), target, SR.Format(SR.Arg_EnumIllegalVal, target));
            }
        }

        private static Dictionary<string, string> GetRawEnvironmentVariables()
        {
            // Format for GetEnvironmentStrings is:
            // (=HiddenVar=value\0 | Variable=value\0)* \0
            // See the description of Environment Blocks in MSDN's
            // CreateProcess page (null-terminated array of null-terminated strings).
            // Note the =HiddenVar's aren't always at the beginning.

            // Copy strings out, parsing into pairs and inserting into the table.
            // The first few environment variable entries start with an '='.
            // The current working directory of every drive (except for those drives
            // you haven't cd'ed into in your DOS window) are stored in the 
            // environment block (as =C:=pwd) and the program's exit code is 
            // as well (=ExitCode=00000000).

            var results = new Dictionary<string, string>();
            char[] block = GetEnvironmentCharArray();
            for (int i = 0; i < block.Length; i++)
            {
                int startKey = i;

                // Skip to key. On some old OS, the environment block can be corrupted.
                // Some will not have '=', so we need to check for '\0'. 
                while (block[i] != '=' && block[i] != '\0') i++;
                if (block[i] == '\0') continue;

                // Skip over environment variables starting with '='
                if (i - startKey == 0)
                {
                    while (block[i] != 0) i++;
                    continue;
                }

                string key = new string(block, startKey, i - startKey);
                i++;  // skip over '='

                int startValue = i;
                while (block[i] != 0) i++; // Read to end of this entry 
                string value = new string(block, startValue, i - startValue); // skip over 0 handled by for loop's i++

                results[key] = value;
            }
            return results;
        }

        private static string GetEnvironmentVariableCore(string variable)
        {
            StringBuilder sb = StringBuilderCache.Acquire(128); // A somewhat reasonable default size
            int requiredSize = Win32Native.GetEnvironmentVariable(variable, sb, sb.Capacity);

            if (requiredSize == 0 && Marshal.GetLastWin32Error() == Win32Native.ERROR_ENVVAR_NOT_FOUND)
            {
                StringBuilderCache.Release(sb);
                return null;
            }

            while (requiredSize > sb.Capacity)
            {
                sb.Capacity = requiredSize;
                sb.Length = 0;
                requiredSize = Win32Native.GetEnvironmentVariable(variable, sb, sb.Capacity);
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private static string GetEnvironmentVariableCore(string variable, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
                return GetEnvironmentVariableCore(variable);

#if !FEATURE_WIN32_REGISTRY
            return null;
#else
            RegistryKey baseKey;
            string keyName;

            if (target == EnvironmentVariableTarget.Machine)
            {
                baseKey = Registry.LocalMachine;
                keyName = @"System\CurrentControlSet\Control\Session Manager\Environment";
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                Debug.Assert(target == EnvironmentVariableTarget.User);
                baseKey = Registry.CurrentUser;
                keyName = "Environment";
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)target));
            }

            using (RegistryKey environmentKey = baseKey.OpenSubKey(keyName, writable: false))
            {
                return environmentKey?.GetValue(variable) as string;
            }
#endif
        }

        private static IDictionary GetEnvironmentVariablesCore()
        {
            return GetRawEnvironmentVariables();
        }

        private static IDictionary GetEnvironmentVariablesCore(EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
                return GetEnvironmentVariablesCore();

#if !FEATURE_WIN32_REGISTRY
            // Without registry support we have nothing to return
            return new Dictionary<string, string>(0);
#else
            RegistryKey baseKey;
            string keyName;
            if (target == EnvironmentVariableTarget.Machine)
            {
                baseKey = Registry.LocalMachine;
                keyName = @"System\CurrentControlSet\Control\Session Manager\Environment";
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                Debug.Assert(target == EnvironmentVariableTarget.User);
                baseKey = Registry.CurrentUser;
                keyName = @"Environment";
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)target));
            }

            using (RegistryKey environmentKey = baseKey.OpenSubKey(keyName, writable: false))
            {
                var table = new Dictionary<string, string>();
                if (environmentKey != null)
                {
                    foreach (string name in environmentKey.GetValueNames())
                    {
                        table.Add(name, environmentKey.GetValue(name, "").ToString());
                    }
                }
                return table;
            }
#endif // FEATURE_WIN32_REGISTRY
        }

        private static void SetEnvironmentVariableCore(string variable, string value)
        {
            // explicitly null out value if is the empty string.
            if (string.IsNullOrEmpty(value) || value[0] == '\0')
                value = null;

            if (!Win32Native.SetEnvironmentVariable(variable, value))
            {
                int errorCode = Marshal.GetLastWin32Error();

                switch (errorCode)
                {
                    case Win32Native.ERROR_ENVVAR_NOT_FOUND:
                        // Allow user to try to clear a environment variable
                        return;
                    case Win32Native.ERROR_FILENAME_EXCED_RANGE:
                        // The error message from Win32 is "The filename or extension is too long",
                        // which is not accurate.
                        throw new ArgumentException(SR.Format(SR.Argument_LongEnvVarValue));
                    default:
                        throw new ArgumentException(Win32Native.GetMessage(errorCode));
                }
            }
        }

        private static void SetEnvironmentVariableCore(string variable, string value, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
            {
                SetEnvironmentVariableCore(variable, value);
                return;
            }

#if !FEATURE_WIN32_REGISTRY
            // other targets ignored
            return;
#else
            // explicitly null out value if is the empty string.
            if (string.IsNullOrEmpty(value) || value[0] == '\0')
                value = null;

            RegistryKey baseKey;
            string keyName;

            if (target == EnvironmentVariableTarget.Machine)
            {
                baseKey = Registry.LocalMachine;
                keyName = @"System\CurrentControlSet\Control\Session Manager\Environment";
            }
            else if (target == EnvironmentVariableTarget.User)
            {
                Debug.Assert(target == EnvironmentVariableTarget.User);

                // User-wide environment variables stored in the registry are limited to 255 chars for the environment variable name.
                const int MaxUserEnvVariableLength = 255;
                if (variable.Length >= MaxUserEnvVariableLength)
                {
                    throw new ArgumentException(SR.Argument_LongEnvVarValue, nameof(variable));
                }

                baseKey = Registry.CurrentUser;
                keyName = "Environment";
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)target));
            }

            using (RegistryKey environmentKey = baseKey.OpenSubKey(keyName, writable: true))
            {
                if (environmentKey != null)
                {
                    if (value == null)
                    {
                        environmentKey.DeleteValue(variable, throwOnMissingValue: false);
                    }
                    else
                    {
                        environmentKey.SetValue(variable, value);
                    }
                }
            }

            // send a WM_SETTINGCHANGE message to all windows
            IntPtr r = Win32Native.SendMessageTimeout(new IntPtr(Win32Native.HWND_BROADCAST),
                Win32Native.WM_SETTINGCHANGE, IntPtr.Zero, "Environment", 0, 1000, IntPtr.Zero);

            if (r == IntPtr.Zero) Debug.Assert(false, "SetEnvironmentVariable failed: " + Marshal.GetLastWin32Error());
#endif // FEATURE_WIN32_REGISTRY
        }
    }
}
