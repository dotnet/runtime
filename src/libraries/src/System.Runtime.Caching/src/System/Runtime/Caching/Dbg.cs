// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Runtime.Versioning;

namespace System.Runtime.Caching
{
    internal static class Dbg
    {
#if DEBUG
        static readonly DateTime MinValuePlusOneDay = DateTime.MinValue.AddDays(1);
        static readonly DateTime    MaxValueMinusOneDay = DateTime.MaxValue.AddDays(-1);
#endif

        internal const string TAG_INTERNAL = "Internal";
        internal const string TAG_EXTERNAL = "External";
        internal const string TAG_ALL = "*";

        internal const string DATE_FORMAT = @"yyyy/MM/dd HH:mm:ss.ffff";
        internal const string TIME_FORMAT = @"HH:mm:ss:ffff";

#if DEBUG
        private static class NativeMethods {
            [DllImport("kernel32.dll")]
            internal extern static void DebugBreak();

            [DllImport("kernel32.dll")]
            internal extern static int GetCurrentProcessId();

            [DllImport("kernel32.dll")]
            internal extern static int GetCurrentThreadId();

            [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
            internal extern static IntPtr GetCurrentProcess();

            [DllImport("kernel32.dll")]
            internal extern static bool IsDebuggerPresent();

            [DllImport("kernel32.dll", SetLastError=true)]
            internal extern static bool TerminateProcess(HandleRef processHandle, int exitCode);

            internal const int PM_NOREMOVE = 0x0000;
            internal const int PM_REMOVE = 0x0001;

            [StructLayout(LayoutKind.Sequential)]
            internal struct MSG {
                internal IntPtr   hwnd;
                internal int      message;
                internal IntPtr   wParam;
                internal IntPtr   lParam;
                internal int      time;
                internal int      pt_x;
                internal int      pt_y;
            }

            //[DllImport("user32.dll", CharSet=CharSet.Auto)]
            //internal extern static bool PeekMessage([In, Out] ref MSG msg, HandleRef hwnd, int msgMin, int msgMax, int remove);

            internal const int 
                MB_OK = 0x00000000,
                MB_OKCANCEL = 0x00000001,
                MB_ABORTRETRYIGNORE = 0x00000002,
                MB_YESNOCANCEL = 0x00000003,
                MB_YESNO = 0x00000004,
                MB_RETRYCANCEL = 0x00000005,
                MB_ICONHAND = 0x00000010,
                MB_ICONQUESTION = 0x00000020,
                MB_ICONEXCLAMATION = 0x00000030,
                MB_ICONASTERISK = 0x00000040,
                MB_USERICON = 0x00000080,
                MB_ICONWARNING = 0x00000030,
                MB_ICONERROR = 0x00000010,
                MB_ICONINFORMATION = 0x00000040,
                MB_DEFBUTTON1 = 0x00000000,
                MB_DEFBUTTON2 = 0x00000100,
                MB_DEFBUTTON3 = 0x00000200,
                MB_DEFBUTTON4 = 0x00000300,
                MB_APPLMODAL = 0x00000000,
                MB_SYSTEMMODAL = 0x00001000,
                MB_TASKMODAL = 0x00002000,
                MB_HELP = 0x00004000,
                MB_NOFOCUS = 0x00008000,
                MB_SETFOREGROUND = 0x00010000,
                MB_DEFAULT_DESKTOP_ONLY = 0x00020000,
                MB_TOPMOST = 0x00040000,
                MB_RIGHT = 0x00080000,
                MB_RTLREADING = 0x00100000,
                MB_SERVICE_NOTIFICATION = 0x00200000,
                MB_SERVICE_NOTIFICATION_NT3X = 0x00040000,
                MB_TYPEMASK = 0x0000000F,
                MB_ICONMASK = 0x000000F0,
                MB_DEFMASK = 0x00000F00,
                MB_MODEMASK = 0x00003000,
                MB_MISCMASK = 0x0000C000;

            internal const int
                IDOK = 1,
                IDCANCEL = 2,
                IDABORT = 3,
                IDRETRY = 4,
                IDIGNORE = 5,
                IDYES = 6,
                IDNO = 7,
                IDCLOSE = 8,
                IDHELP = 9;

            //[DllImport("user32.dll", CharSet=CharSet.Auto, BestFitMapping=false)]
            //internal extern static int MessageBox(HandleRef hWnd, string text, string caption, int type);

            internal static readonly IntPtr HKEY_LOCAL_MACHINE = unchecked((IntPtr)(int)0x80000002);

            internal const int READ_CONTROL           = 0x00020000;
            internal const int STANDARD_RIGHTS_READ   = READ_CONTROL;

            internal const int SYNCHRONIZE            = 0x00100000;

            internal const int KEY_QUERY_VALUE        = 0x0001;
            internal const int KEY_ENUMERATE_SUB_KEYS = 0x0008;
            internal const int KEY_NOTIFY             = 0x0010;

            internal const int KEY_READ               = ((STANDARD_RIGHTS_READ |
                                                               KEY_QUERY_VALUE |
                                                               KEY_ENUMERATE_SUB_KEYS |
                                                               KEY_NOTIFY)
                                                              &
                                                              (~SYNCHRONIZE));

            internal const int REG_NOTIFY_CHANGE_NAME       = 1;
            internal const int REG_NOTIFY_CHANGE_LAST_SET   = 4;

            [DllImport("advapi32.dll", CharSet=CharSet.Auto, BestFitMapping=false, SetLastError=true)]
            internal extern static int RegOpenKeyEx(IntPtr hKey, string lpSubKey, int ulOptions, int samDesired, out SafeRegistryHandle hkResult);

            [DllImport("advapi32.dll", ExactSpelling=true, SetLastError=true)]
            internal extern static int RegNotifyChangeKeyValue(SafeRegistryHandle hKey, bool watchSubTree, uint notifyFilter, SafeWaitHandle regEvent, bool async);
        }

        private enum TagValue {
            Disabled = 0,
            Enabled = 1,
            Break = 2,

            Min = Disabled,
            Max = Break,
        }

        private const string            TAG_ASSERT = "Assert";
        private const string            TAG_ASSERT_BREAK = "AssertBreak";

        private const string            TAG_DEBUG_VERBOSE = "DebugVerbose";
        private const string            TAG_DEBUG_MONITOR = "DebugMonitor";
        private const string            TAG_DEBUG_PREFIX = "DebugPrefix";
        private const string            TAG_DEBUG_THREAD_PREFIX = "DebugThreadPrefix";

        private const string            PRODUCT = "Microsoft .NET Framework";
        private const string            COMPONENT = "System.Web";

        private static string           s_regKeyName = @"Software\Microsoft\ASP.NET\Debug";
        private static string           s_listenKeyName = @"Software\Microsoft";

        private static bool             s_assert;
        private static bool             s_assertBreak;

        private static bool             s_includePrefix;
        private static bool             s_includeThreadPrefix;
        private static bool             s_monitor;

        private static object           s_lock;
        private static volatile bool    s_inited;
        private static ReadOnlyCollection<Tag>  s_tagDefaults;
        private static List<Tag>        s_tags;

        private static AutoResetEvent       s_notifyEvent;
        private static RegisteredWaitHandle s_waitHandle;
        private static SafeRegistryHandle   s_regHandle;
        private static bool                 s_stopMonitoring;

        private static Hashtable        s_tableAlwaysValidate;
        private static Type[]           s_DumpArgs;
        private static Type[]           s_ValidateArgs;

        private class Tag {
            string      _name;
            TagValue    _value;
            int         _prefixLength;

            internal Tag(string name, TagValue value) {
                _name = name;
                _value = value;

                if (_name[_name.Length - 1] == '*') {
                    _prefixLength = _name.Length - 1;
                }
                else {
                    _prefixLength = -1;
                }
            }

            internal string Name {
                get {return _name;}
            }

            internal TagValue Value {
                get {return _value;}
            }

            internal int PrefixLength {
                get {return _prefixLength;}
            }
        }

        static Dbg() {
            s_lock = new object();
        }

        private static void EnsureInit() {
            bool continueInit = false;

            if (!s_inited) {
                lock (s_lock) {
                    if (!s_inited) {
                        s_tableAlwaysValidate = new Hashtable();
                        s_DumpArgs = new Type[1] {typeof(string)}; 
                        s_ValidateArgs = new Type[0];              

                        List<Tag> tagDefaults = new List<Tag>();
                        tagDefaults.Add(new Tag(TAG_ALL, TagValue.Disabled));
                        tagDefaults.Add(new Tag(TAG_INTERNAL, TagValue.Enabled));
                        tagDefaults.Add(new Tag(TAG_EXTERNAL, TagValue.Enabled));
                        tagDefaults.Add(new Tag(TAG_ASSERT, TagValue.Break));
                        tagDefaults.Add(new Tag(TAG_ASSERT_BREAK, TagValue.Disabled));
                        tagDefaults.Add(new Tag(TAG_DEBUG_VERBOSE, TagValue.Enabled));
                        tagDefaults.Add(new Tag(TAG_DEBUG_MONITOR, TagValue.Enabled));
                        tagDefaults.Add(new Tag(TAG_DEBUG_PREFIX, TagValue.Enabled));
                        tagDefaults.Add(new Tag(TAG_DEBUG_THREAD_PREFIX, TagValue.Enabled));

                        s_tagDefaults = tagDefaults.AsReadOnly();
                        s_tags = new List<Tag>(s_tagDefaults);
                        GetBuiltinTagValues();

                        continueInit = true;
                        s_inited = true;
                    }
                }
            }

            // Work to do outside the init lock.
            if (continueInit) {
                ReadTagsFromRegistry();
                Trace(TAG_DEBUG_VERBOSE, "Debugging package initialized");

                // Need to read tags before starting to monitor in order to get TAG_DEBUG_MONITOR
                StartRegistryMonitor();
            }
        }

        private static bool StringEqualsIgnoreCase(string s1, string s2) {
            return StringComparer.OrdinalIgnoreCase.Equals(s1, s2);
        }

        private static void WriteTagsToRegistry() {
            try {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(s_regKeyName)) {
                    List<Tag> tags = s_tags;
                    foreach (Tag tag in tags) {
                        key.SetValue(tag.Name, tag.Value, RegistryValueKind.DWord);
                    }
                }
            }
            catch {
            }
        }

        private static void GetBuiltinTagValues() {
            // Use GetTagValue instead of IsTagEnabled because it does not call EnsureInit
            // and potentially recurse.
            s_assert              = (GetTagValue(TAG_ASSERT) != TagValue.Disabled);
            s_assertBreak         = (GetTagValue(TAG_ASSERT_BREAK) != TagValue.Disabled);
            s_includePrefix       = (GetTagValue(TAG_DEBUG_PREFIX) != TagValue.Disabled);
            s_includeThreadPrefix = (GetTagValue(TAG_DEBUG_THREAD_PREFIX) != TagValue.Disabled);
            s_monitor             = (GetTagValue(TAG_DEBUG_MONITOR) != TagValue.Disabled);
        }

        private static void ReadTagsFromRegistry() {
            lock (s_lock) {
                try {
                    List<Tag> tags = new List<Tag>(s_tagDefaults);
                    string[] names = null;

                    bool writeTags = false;
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(s_regKeyName, false)) {
                        if (key != null) {
                            names = key.GetValueNames();
                            foreach (string name in names) {
                                TagValue value = TagValue.Disabled;
                                try {
                                    TagValue keyvalue = (TagValue) key.GetValue(name);
                                    if (TagValue.Min <= keyvalue && keyvalue <= TagValue.Max) {
                                        value = keyvalue;
                                    }
                                    else {
                                        writeTags = true;
                                    }
                                }
                                catch {
                                    writeTags = true;
                                }

                                // Add tag to list, making sure it is unique.
                                Tag tag = new Tag(name, (TagValue) value);
                                bool found = false;
                                for (int i = 0; i < s_tagDefaults.Count; i++) {
                                    if (StringEqualsIgnoreCase(name, tags[i].Name)) {
                                        found = true;
                                        tags[i] = tag;
                                        break;
                                    }
                                }

                                if (!found) {
                                    tags.Add(tag);
                                }
                            }
                        }
                    }

                    s_tags = tags;
                    GetBuiltinTagValues();

                    // Write tags out if there was an invalid value or 
                    // not all default tags are present.
                    if (writeTags || (names != null && names.Length < tags.Count)) {
                        WriteTagsToRegistry();
                    }
                }
                catch {
                    s_tags = new List<Tag>(s_tagDefaults);
                }
            }
        }

        private static void StartRegistryMonitor() {
            if (!s_monitor) {
                Trace(TAG_DEBUG_VERBOSE, "WARNING: Registry monitoring disabled, changes during process execution will not be recognized."); 
                return;
            }

            Trace(TAG_DEBUG_VERBOSE, "Monitoring registry key " + s_listenKeyName + " for changes.");

            // Event used to notify of changes.
            s_notifyEvent = new AutoResetEvent(false);

            // Register a wait on the event.
            s_waitHandle = ThreadPool.RegisterWaitForSingleObject(s_notifyEvent, OnRegChangeKeyValue, null, -1, false);

            // Monitor the registry.
            MonitorRegistryForOneChange();
        }

        private static void StopRegistryMonitor() {
            // Cleanup allocated handles
            s_stopMonitoring = true;

            if (s_regHandle != null) {
                s_regHandle.Close();
                s_regHandle = null;
            }

            if (s_waitHandle != null) {
                s_waitHandle.Unregister(s_notifyEvent);
                s_waitHandle = null;
            }

            if (s_notifyEvent != null) {
                s_notifyEvent.Close();
                s_notifyEvent = null;
            }

            Trace(TAG_DEBUG_VERBOSE, "Registry monitoring stopped."); 
        }

        public static void OnRegChangeKeyValue(object state, bool timedOut) {
            if (!s_stopMonitoring) {
                if (timedOut) {
                    StopRegistryMonitor();
                }
                else {
                    // Monitor again
                    MonitorRegistryForOneChange();

                    // Once we're monitoring, read the changes to the registry.
                    // We have to do this after we start monitoring in order
                    // to catch all changes to the registry.
                    ReadTagsFromRegistry();
                }
            }
        }

        private static void MonitorRegistryForOneChange() {
            // Close the open reg handle
            if (s_regHandle != null) {
                s_regHandle.Close();
                s_regHandle = null;
            }

            // Open the reg key
            int result = NativeMethods.RegOpenKeyEx(NativeMethods.HKEY_LOCAL_MACHINE, s_listenKeyName, 0, NativeMethods.KEY_READ, out s_regHandle);
            if (result != 0) {
                StopRegistryMonitor();
                return;
            }

            // Listen for changes.
            result = NativeMethods.RegNotifyChangeKeyValue(
                    s_regHandle, 
                    true, 
                    NativeMethods.REG_NOTIFY_CHANGE_NAME | NativeMethods.REG_NOTIFY_CHANGE_LAST_SET,
                    s_notifyEvent.SafeWaitHandle,
                    true);

            if (result != 0) {
                StopRegistryMonitor();
            }
        }

        private static Tag FindMatchingTag(string name, bool exact) {
            List<Tag> tags = s_tags;

            // Look for exact match first
            foreach (Tag tag in tags) {
                if (StringEqualsIgnoreCase(name, tag.Name)) {
                    return tag;
                }
            }

            if (exact) {
                return null;
            }

            Tag longestTag = null;
            int longestPrefix = -1;
            foreach (Tag tag in tags) {
                if (    tag.PrefixLength > longestPrefix && 
                        0 == string.Compare(name, 0, tag.Name, 0, tag.PrefixLength, StringComparison.OrdinalIgnoreCase)) {

                    longestTag = tag;
                    longestPrefix = tag.PrefixLength;
                }
            }

            return longestTag;
        }

        private static TagValue GetTagValue(string name) {
            Tag tag = FindMatchingTag(name, false);
            if (tag != null) {
                return tag.Value;
            }
            else {
                return TagValue.Disabled;
            }
        }

        private static bool TraceBreak(string tagName, string message, Exception e, bool includePrefix) {
            EnsureInit();

            TagValue tagValue = GetTagValue(tagName);
            if (tagValue == TagValue.Disabled) {
                return false;
            }

            bool isAssert = object.ReferenceEquals(tagName, TAG_ASSERT);
            if (isAssert) {
                tagName = "";
            }

            string exceptionMessage = null;
            if (e != null) {
                string httpCode = null;
                string errorCode = null;

                if (e is ExternalException) {
                    // note that HttpExceptions are ExternalExceptions
                    errorCode = "_hr=0x" + ((ExternalException)e).ErrorCode.ToString("x", CultureInfo.InvariantCulture);
                }

                // Use e.ToString() in order to get inner exception
                if (errorCode != null) {
                    exceptionMessage = "Exception " + e.ToString() + "\n" + httpCode + errorCode;
                }
                else {
                    exceptionMessage = "Exception " + e.ToString();
                }
            }

            if (string.IsNullOrEmpty(message) & exceptionMessage != null) {
                message = exceptionMessage;
                exceptionMessage = null;
            }

            string traceFormat;
            int idThread = 0;
            int idProcess = 0;

            if (!includePrefix || !s_includePrefix) {
                traceFormat = "{4}\n{5}";
            }
            else {
                if (s_includeThreadPrefix) {
                    idThread = NativeMethods.GetCurrentThreadId();
                    idProcess = NativeMethods.GetCurrentProcessId();
                    traceFormat = "[0x{0:x}.{1:x} {2} {3}] {4}\n{5}";
                }
                else {
                    traceFormat = "[{2} {3}] {4}\n{5}";
                }
            }

            string suffix = "";
            if (exceptionMessage != null) {
                suffix += exceptionMessage + "\n";
            }

            bool doBreak = (tagValue == TagValue.Break);
            if (doBreak && !isAssert) {
                suffix += "Breaking into debugger...\n";
            }

            string traceMessage = string.Format(CultureInfo.InvariantCulture, traceFormat, idProcess, idThread, COMPONENT, tagName, message, suffix);

            Debug.WriteLine(traceMessage);

            return doBreak;
        }

        //private class MBResult {
        //    internal int Result;
        //}

        [ResourceExposure(ResourceScope.None)]
        static bool DoAssert(string message) {
            if (!s_assert) {
                return false;
            }

            // Skip 2 frames - one for this function, one for
            // the public Assert function that called this function.
            StackFrame frame = new StackFrame(2, true);
            StackTrace trace = new StackTrace(2, true);

            string fileName = frame.GetFileName();
            int lineNumber = frame.GetFileLineNumber();

            string traceFormat;
            if (!string.IsNullOrEmpty(fileName)) {
                traceFormat = "ASSERTION FAILED: {0}\nFile: {1}:{2}\nStack trace:\n{3}";
            }
            else {
                traceFormat = "ASSERTION FAILED: {0}\nStack trace:\n{3}";
            }

            string traceMessage = string.Format(CultureInfo.InvariantCulture, traceFormat, message, fileName, lineNumber, trace.ToString());

            if (!TraceBreak(TAG_ASSERT, traceMessage, null, true)) {
                // If the value of "Assert" is not TagValue.Break, then don't even ask user.
                return false;
            }

            if (s_assertBreak) {
                // If "AssertBreak" is enabled, then always break.
                return true;
            }

            string dialogFormat;
            if (!string.IsNullOrEmpty(fileName)) {
                dialogFormat = 
@"Failed expression: {0}
File: {1}:{2}
Component: {3}
PID={4} TID={5}
Stack trace:
{6}

A=Exit process R=Debug I=Continue";
            }
            else {
                dialogFormat = 
@"Failed expression: {0}
(no file information available)
Component: {3}
PID={4} TID={5}
Stack trace:
{6}

A=Exit process R=Debug I=Continue";
            }

            string dialogMessage = string.Format(
                CultureInfo.InvariantCulture,
                dialogFormat,
                message,
                fileName, lineNumber,
                COMPONENT,
                NativeMethods.GetCurrentProcessId(), NativeMethods.GetCurrentThreadId(),
                trace.ToString());

            //MBResult mbResult = new MBResult();

            //Thread thread = new Thread(
            //    delegate() {
            //        for (int i = 0; i < 100; i++) {
            //            NativeMethods.MSG msg = new NativeMethods.MSG();
            //            NativeMethods.PeekMessage(ref msg, new HandleRef(mbResult, IntPtr.Zero), 0, 0, NativeMethods.PM_REMOVE);
            //        }

            //        mbResult.Result = NativeMethods.MessageBox(new HandleRef(mbResult, IntPtr.Zero), dialogMessage, PRODUCT + " Assertion",                
            //            NativeMethods.MB_SERVICE_NOTIFICATION | 
            //            NativeMethods.MB_TOPMOST |
            //            NativeMethods.MB_ABORTRETRYIGNORE | 
            //            NativeMethods.MB_ICONEXCLAMATION);
            //    }
            //);

            //thread.Start();
            //thread.Join();

            //if (mbResult.Result == NativeMethods.IDABORT) {
            //    IntPtr currentProcess = NativeMethods.GetCurrentProcess();
            //    NativeMethods.TerminateProcess(new HandleRef(mbResult, currentProcess), 1);
            //}

            //return mbResult.Result == NativeMethods.IDRETRY;
            Debug.Fail(dialogMessage);
            return true;
        }
#endif

        //
        // Sends the message to the debugger if the tag is enabled.
        // Also breaks into the debugger the value of the tag is 2 (TagValue.Break).
        //
        [Conditional("DEBUG")]
        internal static void Trace(string tagName, string message)
        {
#if DEBUG
            if (TraceBreak(tagName, message, null, true)) {
                Break();
            }
#endif
        }

        //
        // Sends the message to the debugger if the tag is enabled.
        // Also breaks into the debugger the value of the tag is 2 (TagValue.Break).
        //
        [Conditional("DEBUG")]
        internal static void Trace(string tagName, string message, bool includePrefix)
        {
#if DEBUG
            if (TraceBreak(tagName, message, null, includePrefix)) {
                Break();
            }
#endif
        }

        //
        // Sends the message to the debugger if the tag is enabled.
        // Also breaks into the debugger the value of the tag is 2 (TagValue.Break).
        //
        [Conditional("DEBUG")]
        internal static void Trace(string tagName, string message, Exception e)
        {
#if DEBUG
            if (TraceBreak(tagName, message, e, true)) {
                Break();
            }
#endif
        }

        //
        // Sends the message to the debugger if the tag is enabled.
        // Also breaks into the debugger the value of the tag is 2 (TagValue.Break).
        //
        [Conditional("DEBUG")]
        internal static void Trace(string tagName, Exception e)
        {
#if DEBUG
            if (TraceBreak(tagName, null, e, true)) {
                Break();
            }
#endif
        }

        //
        // Sends the message to the debugger if the tag is enabled.
        // Also breaks into the debugger the value of the tag is 2 (TagValue.Break).
        //
        [Conditional("DEBUG")]
        internal static void Trace(string tagName, string message, Exception e, bool includePrefix)
        {
#if DEBUG
            if (TraceBreak(tagName, message, e, includePrefix)) {
                Break();
            }
#endif
        }

#if DEBUG
#endif

        [Conditional("DEBUG")]
        public static void TraceException(String tagName, Exception e)
        {
#if DEBUG
            if (TraceBreak(tagName, null, e, true)) {
                Break();
            }
#endif
        }


        //
        // If the assertion is false and the 'Assert' tag is enabled:
        //      * Send a message to the debugger.
        //      * If the 'AssertBreak' tag is enabled, immediately break into the debugger
        //      * Else display a dialog box asking the user to Abort, Retry (break), or Ignore
        //
        [Conditional("DEBUG")]
        internal static void Assert(bool assertion, string message)
        {
#if DEBUG
            EnsureInit();
            if (assertion == false) {
                if (DoAssert(message)) {
                    Break();
                }
            }
#endif
        }

        //
        // If the assertion is false and the 'Assert' tag is enabled:
        //      * Send a message to the debugger.
        //      * If the 'AssertBreak' tag is enabled, immediately break into the debugger
        //      * Else display a dialog box asking the user to Abort, Retry (break), or Ignore
        //
        [Conditional("DEBUG")]
        [ResourceExposure(ResourceScope.None)]
        internal static void Assert(bool assertion)
        {
#if DEBUG
            EnsureInit();
            if (assertion == false) {
                if (DoAssert(null)) {
                    Break();
                }
            }
#endif
        }

        //
        // Like Assert, but the assertion is always considered to be false.
        //
        [Conditional("DEBUG")]
        [ResourceExposure(ResourceScope.None)]
        internal static void Fail(string message)
        {
#if DEBUG
            Assert(false, message);
#endif
        }

        //
        // Returns true if the tag is enabled, false otherwise.
        // Note that the tag needn't be an exact match.
        //
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Grandfathered suppression from original caching code checkin")]
        [ResourceExposure(ResourceScope.None)]
        internal static bool IsTagEnabled(string tagName)
        {
#if DEBUG
            EnsureInit();
            return GetTagValue(tagName) != TagValue.Disabled;
#else
            return false;
#endif
        }

        //
        // Returns true if the tag present. 
        // This function chekcs for an exact match.
        //
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Grandfathered suppression from original caching code checkin")]
        [ResourceExposure(ResourceScope.None)]
        internal static bool IsTagPresent(string tagName)
        {
#if DEBUG
            EnsureInit();
            return FindMatchingTag(tagName, true) != null;
#else
            return false;
#endif
        }

        //
        // Breaks into the debugger, or launches one if not yet attached.
        //
        [Conditional("DEBUG")]
        [ResourceExposure(ResourceScope.None)]
        internal static void Break()
        {
#if DEBUG
            if (NativeMethods.IsDebuggerPresent()) {
                NativeMethods.DebugBreak();
            }
            else if (!Debugger.IsAttached) {
                Debugger.Launch();
            }
            else {
                Debugger.Break();            
            }
#endif
        }

        //
        // Tells the debug system to always validate calls for a
        // particular tag. This is useful for temporarily enabling
        // validation in stress tests or other situations where you
        // may not have control over the debug tags that are enabled
        // on a particular machine.
        // 
        [Conditional("DEBUG")]
        internal static void AlwaysValidate(string tagName)
        {
#if DEBUG
            EnsureInit();
            s_tableAlwaysValidate[tagName] = tagName;
#endif
        }

        //
        // Throws an exception if the assertion is not valid.
        // Use this function from a DebugValidate method where
        // you would otherwise use Assert.
        //
        [Conditional("DEBUG")]
        internal static void CheckValid(bool assertion, string message)
        {
#if DEBUG
            if (!assertion) {
                throw new Exception(message);
            }
#endif
        }

        //
        // Calls DebugValidate on an object if such a method exists.
        //
        // This method should be used from implementations of DebugValidate
        // where it is unknown whether an object has a DebugValidate method.
        // For example, the DoubleLink class uses it to validate the
        // item of type Object which it points to.
        //
        // This method should NOT be used when code wants to conditionally
        // validate an object and have a failed validation caught in an assert.
        // Use Debug.Validate(tagName, obj) for that purpose.
        //
        [Conditional("DEBUG")]
        [ResourceExposure(ResourceScope.None)]
        internal static void Validate(Object obj)
        {
#if DEBUG
            Type        type;
            MethodInfo  mi;

            if (obj != null) {
                type = obj.GetType();

                mi = type.GetMethod(
                        "DebugValidate", 
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        s_ValidateArgs,
                        null);

                if (mi != null) {
                    object[] tempIndex = null;
                    mi.Invoke(obj, tempIndex);
                }
            }
#endif
        }

        //
        // Validates an object is the "Validate" tag is enabled, or when
        // the "Validate" tag is not disabled and the given 'tag' is enabled.
        // An Assertion is made if the validation fails.
        //
        [Conditional("DEBUG")]
        [ResourceExposure(ResourceScope.None)]
        internal static void Validate(string tagName, Object obj)
        {
#if DEBUG
            EnsureInit();

            if (    obj != null 
                    && (    IsTagEnabled("Validate")
                            ||  (   !IsTagPresent("Validate") 
                                    && (   s_tableAlwaysValidate[tagName] != null 
                                           ||  IsTagEnabled(tagName))))) {
                try {
                    Validate(obj);
                }
                catch (Exception e) {
                    Assert(false, "Validate failed: " + e.InnerException.Message);
                }
#pragma warning disable 1058
                catch {
                    Assert(false, "Validate failed.  Non-CLS compliant exception caught.");
                }
#pragma warning restore 1058
            }
#endif
        }

#if DEBUG

        //
        // Calls DebugDescription on an object to get its description.
        //
        // This method should only be used in implementations of DebugDescription
        // where it is not known whether a nested objects has an implementation
        // of DebugDescription. For example, the double linked list class uses
        // GetDescription to get the description of the item it points to.
        //
        // This method should NOT be used when you want to conditionally
        // dump an object. Use Debug.Dump instead.
        //
        // @param obj      The object to call DebugDescription on. May be null.
        // @param indent   A prefix for each line in the description. This is used
        //                 to allow the nested display of objects within other objects.
        //                 The indent is usually a multiple of four spaces.
        //
        // @return         The description.
        //
        internal static string GetDescription(Object obj, string indent) {
            string      description;
            Type        type;
            MethodInfo  mi;
            Object[]   parameters;

            if (obj == null) {
                description = "\n";
            }
            else {
                type = obj.GetType();
                mi = type.GetMethod(
                        "DebugDescription", 
                        BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        s_DumpArgs,
                        null);
                        
                if (mi == null || mi.ReturnType != typeof(string)) {
                    description = indent + obj.ToString();
                }
                else {
                    parameters = new Object[1] {(Object) indent};
                    description = (string) mi.Invoke(obj, parameters);
                }
            }

            return description;
        }
#endif

        // 
        // Dumps an object to the debugger if the "Dump" tag is enabled,
        // or if the "Dump" tag is not present and the 'tag' is enabled.
        // 
        // @param tagName  The tag to Dump with.
        // @param obj  The object to dump.
        // 
        [Conditional("DEBUG")]
        internal static void Dump(string tagName, Object obj)
        {
#if DEBUG
            EnsureInit();

            string  description;
            string  traceTag = null;
            bool    dumpEnabled, dumpPresent;

            if (obj != null) {
                dumpEnabled = IsTagEnabled("Dump");
                dumpPresent = IsTagPresent("Dump");
                if (dumpEnabled || !dumpPresent) {
                    if (IsTagEnabled(tagName)) {
                        traceTag = tagName;
                    }
                    else if (dumpEnabled) {
                        traceTag = "Dump";
                    }

                    if (traceTag != null) {
                        description = GetDescription(obj, string.Empty);
                        Trace(traceTag, "Dump\n" + description);
                    }
                }
            }
#endif
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Grandfathered suppression from original caching code checkin")]
        static internal string FormatLocalDate(DateTime localTime)
        {
#if DEBUG
            return localTime.ToString(DATE_FORMAT, CultureInfo.InvariantCulture);
#else
            return string.Empty;
#endif
        }
    }
}

