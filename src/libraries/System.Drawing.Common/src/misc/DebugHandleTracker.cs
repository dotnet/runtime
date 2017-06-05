// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Internal
{
    using System.ComponentModel;
    using System.Diagnostics;

    using Hashtable = System.Collections.Hashtable;

    /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker"]/*' />
    /// <devdoc>
    ///     The job of this class is to collect and track handle usage in
    ///     windows forms.  Ideally, a developer should never have to call dispose() on
    ///     any windows forms object.  The problem in making this happen is in objects that
    ///     are very small to the VM garbage collector, but take up huge amounts
    ///     of resources to the system.  A good example of this is a Win32 region
    ///     handle.  To the VM, a Region object is a small six ubyte object, so there
    ///     isn't much need to garbage collect it anytime soon.  To Win32, however,
    ///     a region handle consumes expensive USER and GDI resources.  Ideally we
    ///     would like to be able to mark an object as "expensive" so it uses a different
    ///     garbage collection algorithm.  In absence of that, we use the HandleCollector class, which
    ///     runs a daemon thread to garbage collect when handle usage goes up.
    /// </devdoc>
    /// <internalonly/>
    internal class DebugHandleTracker
    {
        private static Hashtable s_handleTypes = new Hashtable();
        private static DebugHandleTracker s_tracker;

        static DebugHandleTracker()
        {
            s_tracker = new DebugHandleTracker();

            if (CompModSwitches.HandleLeak.Level > TraceLevel.Off || CompModSwitches.TraceCollect.Enabled)
            {
                System.Internal.HandleCollector.HandleAdded += new System.Internal.HandleChangeEventHandler(s_tracker.OnHandleAdd);
                System.Internal.HandleCollector.HandleRemoved += new System.Internal.HandleChangeEventHandler(s_tracker.OnHandleRemove);
            }
        }

        private DebugHandleTracker()
        {
        }

        private static object s_internalSyncObject = new object();

        /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.IgnoreCurrentHandlesAsLeaks"]/*' />
        /// <devdoc>
        ///     All handles available at this time will be not be considered as leaks
        ///     when CheckLeaks is called to report leaks.
        /// </devdoc>
        /** @conditional(DEBUG) */
        public static void IgnoreCurrentHandlesAsLeaks()
        {
            lock (s_internalSyncObject)
            {
                if (CompModSwitches.HandleLeak.Level >= TraceLevel.Warning)
                {
                    HandleType[] types = new HandleType[s_handleTypes.Values.Count];
                    s_handleTypes.Values.CopyTo(types, 0);

                    for (int i = 0; i < types.Length; i++)
                    {
                        if (types[i] != null)
                        {
                            types[i].IgnoreCurrentHandlesAsLeaks();
                        }
                    }
                }
            }
        }

        /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.CheckLeaks"]/*' />
        /// <devdoc>
        ///     Called at shutdown to check for handles that are currently allocated.
        ///     Normally, there should be none.  This will print a list of all
        ///     handle leaks.
        /// </devdoc>
        /** @conditional(DEBUG) */
        public static void CheckLeaks()
        {
            lock (s_internalSyncObject)
            {
                if (CompModSwitches.HandleLeak.Level >= TraceLevel.Warning)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    HandleType[] types = new HandleType[s_handleTypes.Values.Count];
                    s_handleTypes.Values.CopyTo(types, 0);

                    Debug.WriteLine("------------Begin--CheckLeaks--------------------");
                    for (int i = 0; i < types.Length; i++)
                    {
                        if (types[i] != null)
                        {
                            types[i].CheckLeaks();
                        }
                    }
                    Debug.WriteLine("-------------End--CheckLeaks---------------------");
                }
            }
        }

        /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.Initialize"]/*' />
        /// <devdoc>
        ///     Ensures leak detection has been initialized.
        /// </devdoc>
        /** @conditional(DEBUG) */
        public static void Initialize()
        {
            // Calling this method forces the class to be loaded, thus running the
            // static constructor which does all the work.
        }

        /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.OnHandleAdd"]/*' />
        /// <devdoc>
        ///     Called by the Win32 handle collector when a new handle is created.
        /// </devdoc>
        /** @conditional(DEBUG) */
        private void OnHandleAdd(string handleName, IntPtr handle, int handleCount)
        {
            HandleType type = (HandleType)s_handleTypes[handleName];
            if (type == null)
            {
                type = new HandleType(handleName);
                s_handleTypes[handleName] = type;
            }
            type.Add(handle);
        }

        /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.OnHandleRemove"]/*' />
        /// <devdoc>
        ///     Called by the Win32 handle collector when a new handle is created.
        /// </devdoc>
        /** @conditional(DEBUG) */
        private void OnHandleRemove(string handleName, IntPtr handle, int HandleCount)
        {
            HandleType type = (HandleType)s_handleTypes[handleName];

            bool removed = false;
            if (type != null)
            {
                removed = type.Remove(handle);
            }

            if (!removed)
            {
                if (CompModSwitches.HandleLeak.Level >= TraceLevel.Error)
                {
                    // It seems to me we shouldn't call HandleCollector.Remove more than once
                    // for a given handle, but we do just that for HWND's (NativeWindow.DestroyWindow
                    // and Control.WmNCDestroy).
                    Debug.WriteLine("*************************************************");
                    Debug.WriteLine("While removing, couldn't find handle: " + Convert.ToString(unchecked((int)handle), 16));
                    Debug.WriteLine("Handle Type      : " + handleName);
                    Debug.WriteLine(Environment.StackTrace);
                    Debug.WriteLine("-------------------------------------------------");
                }
            }
        }

        /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType"]/*' />
        /// <devdoc>
        ///     Represents a specific type of handle.
        /// </devdoc>
        private class HandleType
        {
            public readonly string name;

            private int _handleCount;
            private HandleEntry[] _buckets;

            private const int BUCKETS = 10;

            /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleType"]/*' />
            /// <devdoc>
            ///     Creates a new handle type.
            /// </devdoc>
            public HandleType(string name)
            {
                this.name = name;
                _buckets = new HandleEntry[BUCKETS];
            }

            /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.Add"]/*' />
            /// <devdoc>
            ///     Adds a handle to this handle type for monitoring.
            /// </devdoc>
            public void Add(IntPtr handle)
            {
                lock (this)
                {
                    int hash = ComputeHash(handle);
                    if (CompModSwitches.HandleLeak.Level >= TraceLevel.Info)
                    {
                        Debug.WriteLine("-------------------------------------------------");
                        Debug.WriteLine("Handle Allocating: " + Convert.ToString(unchecked((int)handle), 16));
                        Debug.WriteLine("Handle Type      : " + name);
                        if (CompModSwitches.HandleLeak.Level >= TraceLevel.Verbose)
                            Debug.WriteLine(Environment.StackTrace);
                    }

                    HandleEntry entry = _buckets[hash];
                    while (entry != null)
                    {
                        Debug.Assert(entry.handle != handle, "Duplicate handle of type " + name);
                        entry = entry.next;
                    }

                    _buckets[hash] = new HandleEntry(_buckets[hash], handle);

                    _handleCount++;
                }
            }

            /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.CheckLeaks"]/*' />
            /// <devdoc>
            ///     Checks and reports leaks for handle monitoring.
            /// </devdoc>
            public void CheckLeaks()
            {
                lock (this)
                {
                    bool reportedFirstLeak = false;
                    if (_handleCount > 0)
                    {
                        for (int i = 0; i < BUCKETS; i++)
                        {
                            HandleEntry e = _buckets[i];
                            while (e != null)
                            {
                                if (!e.ignorableAsLeak)
                                {
                                    if (!reportedFirstLeak)
                                    {
                                        Debug.WriteLine("\r\nHandle leaks detected for handles of type " + name + ":");
                                        reportedFirstLeak = true;
                                    }
                                    Debug.WriteLine(e.ToString(this));
                                }
                                e = e.next;
                            }
                        }
                    }
                }
            }

            /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.IgnoreCurrentHandlesAsLeaks"]/*' />
            /// <devdoc>
            ///     Marks all the handles currently stored, as ignorable, so that they will not be reported as leaks later.
            /// </devdoc>
            public void IgnoreCurrentHandlesAsLeaks()
            {
                lock (this)
                {
                    if (_handleCount > 0)
                    {
                        for (int i = 0; i < BUCKETS; i++)
                        {
                            HandleEntry e = _buckets[i];
                            while (e != null)
                            {
                                e.ignorableAsLeak = true;
                                e = e.next;
                            }
                        }
                    }
                }
            }

            /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.ComputeHash"]/*' />
            /// <devdoc>
            ///     Computes the hash bucket for this handle.
            /// </devdoc>
            private int ComputeHash(IntPtr handle)
            {
                return (unchecked((int)handle) & 0xFFFF) % BUCKETS;
            }

            /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.Remove"]/*' />
            /// <devdoc>
            ///     Removes the given handle from our monitor list.
            /// </devdoc>
            public bool Remove(IntPtr handle)
            {
                lock (this)
                {
                    int hash = ComputeHash(handle);
                    if (CompModSwitches.HandleLeak.Level >= TraceLevel.Info)
                    {
                        Debug.WriteLine("-------------------------------------------------");
                        Debug.WriteLine("Handle Releaseing: " + Convert.ToString(unchecked((int)handle), 16));
                        Debug.WriteLine("Handle Type      : " + name);
                        if (CompModSwitches.HandleLeak.Level >= TraceLevel.Verbose)
                            Debug.WriteLine(Environment.StackTrace);
                    }
                    HandleEntry e = _buckets[hash];
                    HandleEntry last = null;
                    while (e != null && e.handle != handle)
                    {
                        last = e;
                        e = e.next;
                    }
                    if (e != null)
                    {
                        if (last == null)
                        {
                            _buckets[hash] = e.next;
                        }
                        else
                        {
                            last.next = e.next;
                        }
                        _handleCount--;
                        return true;
                    }
                    return false;
                }
            }

            /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry"]/*' />
            /// <devdoc>
            ///     Denotes a single entry in our handle list.
            /// </devdoc>
            private class HandleEntry
            {
                public readonly IntPtr handle;
                public HandleEntry next;
                public readonly string callStack;
                public bool ignorableAsLeak;

                /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.HandleEntry"]/*' />
                /// <devdoc>
                ///     Creates a new handle entry
                /// </devdoc>
                public HandleEntry(HandleEntry next, IntPtr handle)
                {
                    this.handle = handle;
                    this.next = next;

                    if (CompModSwitches.HandleLeak.Level > TraceLevel.Off)
                    {
                        callStack = Environment.StackTrace;
                    }
                    else
                    {
                        callStack = null;
                    }
                }

                /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.ToString"]/*' />
                /// <devdoc>
                ///     Converts this handle to a printable string.  the string consists
                ///     of the handle value along with the callstack for it's
                ///     allocation.
                /// </devdoc>
                public string ToString(HandleType type)
                {
                    StackParser sp = new StackParser(callStack);

                    // Discard all of the stack up to and including the "Handle.create" call
                    //
                    sp.DiscardTo("HandleCollector.Add");

                    // Skip the next call as it is always a debug wrapper
                    //
                    sp.DiscardNext();

                    // Now recreate the leak list with a lot of stack entries
                    //
                    sp.Truncate(40);

                    string description = "";
                    /*if (type.name.Equals("GDI") || type.name.Equals("HDC")) {
                        int objectType = UnsafeNativeMethods.GetObjectType(new HandleRef(null, handle));
                        switch (objectType) {
                            case NativeMethods.OBJ_DC: description = "normal DC"; break;
                            case NativeMethods.OBJ_MEMDC: description = "memory DC"; break;
                            case NativeMethods.OBJ_METADC: description = "metafile DC"; break;
                            case NativeMethods.OBJ_ENHMETADC: description = "enhanced metafile DC"; break;

                            case NativeMethods.OBJ_PEN: description = "Pen"; break;
                            case NativeMethods.OBJ_BRUSH: description = "Brush"; break;
                            case NativeMethods.OBJ_PAL: description = "Palette"; break;
                            case NativeMethods.OBJ_FONT: description = "Font"; break;
                            case NativeMethods.OBJ_BITMAP: description = "Bitmap"; break;
                            case NativeMethods.OBJ_REGION: description = "Region"; break;
                            case NativeMethods.OBJ_METAFILE: description = "Metafile"; break;
                            case NativeMethods.OBJ_EXTPEN: description = "Extpen"; break;
                            default: description = "?"; break;
                        }
                        description = " (" + description + ")";
                    }*/

                    return Convert.ToString(unchecked((int)handle), 16) + description + ": " + sp.ToString();
                }

                /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.StackParser"]/*' />
                /// <devdoc>
                ///     Simple stack parsing class to manipulate our callstack.
                /// </devdoc>
                private class StackParser
                {
                    internal string releventStack;
                    internal int startIndex;
                    internal int endIndex;
                    internal int length;

                    /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.StackParser.StackParser"]/*' />
                    /// <devdoc>
                    ///     Creates a new stackparser with the given callstack
                    /// </devdoc>
                    public StackParser(string callStack)
                    {
                        releventStack = callStack;
                        length = releventStack.Length;
                    }

                    /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.StackParser.ContainsString"]/*' />
                    /// <devdoc>
                    ///     Determines if the given string contains token.  This is a case
                    ///     sensitive match.
                    /// </devdoc>
                    private static bool ContainsString(string str, string token)
                    {
                        int stringLength = str.Length;
                        int tokenLength = token.Length;

                        for (int s = 0; s < stringLength; s++)
                        {
                            int t = 0;
                            while (t < tokenLength && str[s + t] == token[t])
                            {
                                t++;
                            }
                            if (t == tokenLength)
                            {
                                return true;
                            }
                        }
                        return false;
                    }

                    /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.StackParser.DiscardNext"]/*' />
                    /// <devdoc>
                    ///     Discards the next line of the stack trace.
                    /// </devdoc>
                    public void DiscardNext()
                    {
                        GetLine();
                    }

                    /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.StackParser.DiscardTo"]/*' />
                    /// <devdoc>
                    ///     Discards all lines up to and including the line that contains
                    ///     discardText.
                    /// </devdoc>
                    public void DiscardTo(string discardText)
                    {
                        while (startIndex < length)
                        {
                            string line = GetLine();
                            if (line == null || ContainsString(line, discardText))
                            {
                                break;
                            }
                        }
                    }

                    /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.StackParser.GetLine"]/*' />
                    /// <devdoc>
                    ///     Retrieves the next line of the stack.
                    /// </devdoc>
                    private string GetLine()
                    {
                        endIndex = releventStack.IndexOf('\r', startIndex);
                        if (endIndex < 0)
                        {
                            endIndex = length - 1;
                        }

                        string line = releventStack.Substring(startIndex, endIndex - startIndex);
                        char ch;

                        while (endIndex < length && ((ch = releventStack[endIndex]) == '\r' || ch == '\n'))
                        {
                            endIndex++;
                        }
                        if (startIndex == endIndex) return null;
                        startIndex = endIndex;
                        line = line.Replace('\t', ' ');
                        return line;
                    }

                    /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.StackParser.ToString"]/*' />
                    /// <devdoc>
                    ///     Rereives the string of the parsed stack trace
                    /// </devdoc>
                    public override string ToString()
                    {
                        return releventStack.Substring(startIndex);
                    }

                    /// <include file='doc\DebugHandleTracker.uex' path='docs/doc[@for="DebugHandleTracker.HandleType.HandleEntry.StackParser.Truncate"]/*' />
                    /// <devdoc>
                    ///     Truncates the stack trace, saving the given # of lines.
                    /// </devdoc>
                    public void Truncate(int lines)
                    {
                        string truncatedStack = "";

                        while (lines-- > 0 && startIndex < length)
                        {
                            if (truncatedStack == null)
                            {
                                truncatedStack = GetLine();
                            }
                            else
                            {
                                truncatedStack += ": " + GetLine();
                            }
                            truncatedStack += Environment.NewLine;
                        }

                        releventStack = truncatedStack;
                        startIndex = 0;
                        endIndex = 0;
                        length = releventStack.Length;
                    }
                }
            }
        }

        //#endif // DEBUG
    }
}
