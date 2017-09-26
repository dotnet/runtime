// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Internal
{
    /// <summary>
    /// The job of this class is to collect and track handle usage in windows forms. Ideally, a developer should never
    /// have to call dispose() on any windows forms object. The problem in making this happen is in objects that are
    /// very small to the VM garbage collector, but take up huge amounts of resources to the system. A good example of
    /// this is a Win32 region handle. To the VM, a Region object is a small six ubyte object, so there isn't much need
    /// to garbage collect it anytime soon. To Win32, however, a region handle consumes expensive USER and GDI
    /// resources. Ideally we would like to be able to mark an object as "expensive" so it uses a different garbage
    /// collection algorithm. In absence of that, we use the HandleCollector class, which runs a daemon thread to
    /// garbage collect when handle usage goes up.
    /// </summary>
    internal class DebugHandleTracker
    {
        private static Hashtable s_handleTypes = new Hashtable();
        private static DebugHandleTracker s_tracker;

        static DebugHandleTracker()
        {
            s_tracker = new DebugHandleTracker();

            if (CompModSwitches.HandleLeak.Level > TraceLevel.Off || CompModSwitches.TraceCollect.Enabled)
            {
                HandleCollector.HandleAdded += new HandleChangeEventHandler(s_tracker.OnHandleAdd);
                HandleCollector.HandleRemoved += new HandleChangeEventHandler(s_tracker.OnHandleRemove);
            }
        }

        private DebugHandleTracker()
        {
        }

        private static object s_internalSyncObject = new object();

        /// <summary>
        /// All handles available at this time will be not be considered as leaks when CheckLeaks is called to report leaks.
        /// </summary>
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
                        types[i]?.IgnoreCurrentHandlesAsLeaks();
                    }
                }
            }
        }

        /// <summary>
        /// Called at shutdown to check for handles that are currently allocated. Normally, there should be none. 
        /// This will print a list of all handle leaks.
        /// </summary>
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
                        types[i]?.CheckLeaks();
                    }
                    Debug.WriteLine("-------------End--CheckLeaks---------------------");
                }
            }
        }

        /// <summary>
        /// Ensures leak detection has been initialized.
        /// </summary>
        public static void Initialize()
        {
            // Calling this method forces the class to be loaded, thus running the
            // static constructor which does all the work.
        }

        /// <summary>
        /// Called by the Win32 handle collector when a new handle is created.
        /// </summary>
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

        /// <summary>
        /// Called by the Win32 handle collector when a new handle is created.
        /// </summary>
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

        /// <summary>
        /// Represents a specific type of handle.
        /// </summary>
        private class HandleType
        {
            public readonly string name;

            private int _handleCount;
            private HandleEntry[] _buckets;

            private const int NumberOfBuckets = 10;

            /// <summary>
            /// Creates a new handle type.
            /// </summary>
            public HandleType(string name)
            {
                this.name = name;
                _buckets = new HandleEntry[NumberOfBuckets];
            }

            /// <summary>
            /// Adds a handle to this handle type for monitoring.
            /// </summary>
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

            /// <summary>
            /// Checks and reports leaks for handle monitoring.
            /// </summary>
            public void CheckLeaks()
            {
                lock (this)
                {
                    bool reportedFirstLeak = false;
                    if (_handleCount > 0)
                    {
                        for (int i = 0; i < NumberOfBuckets; i++)
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

            /// <summary>
            /// Marks all the handles currently stored, as ignorable, so that they will not be reported as leaks later.
            /// </summary>
            public void IgnoreCurrentHandlesAsLeaks()
            {
                lock (this)
                {
                    if (_handleCount > 0)
                    {
                        for (int i = 0; i < NumberOfBuckets; i++)
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

            /// <summary>
            /// Computes the hash bucket for this handle.
            /// </summary>
            private int ComputeHash(IntPtr handle)
            {
                return (unchecked((int)handle) & 0xFFFF) % NumberOfBuckets;
            }

            /// <summary>
            /// Removes the given handle from our monitor list.
            /// </summary>
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

            /// <summary>
            /// Denotes a single entry in our handle list.
            /// </summary>
            private class HandleEntry
            {
                public readonly IntPtr handle;
                public HandleEntry next;
                public readonly string callStack;
                public bool ignorableAsLeak;

                /// <summary>
                /// Creates a new handle entry
                /// </summary>
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

                /// <summary>
                /// Converts this handle to a printable string.  the string consists of the handle value along with
                /// the callstack for it's allocation.
                /// </summary>
                public string ToString(HandleType type)
                {
                    StackParser sp = new StackParser(callStack);

                    // Discard all of the stack up to and including the "Handle.create" call
                    sp.DiscardTo("HandleCollector.Add");

                    // Skip the next call as it is always a debug wrapper
                    sp.DiscardNext();

                    // Now recreate the leak list with a lot of stack entries
                    sp.Truncate(40);

                    string description = "";

                    return Convert.ToString(unchecked((int)handle), 16) + description + ": " + sp.ToString();
                }

                /// <summary>
                /// Simple stack parsing class to manipulate our callstack.
                /// </summary>
                private class StackParser
                {
                    internal string releventStack;
                    internal int startIndex;
                    internal int endIndex;
                    internal int length;

                    /// <summary>
                    /// Creates a new stackparser with the given callstack
                    /// </summary>
                    public StackParser(string callStack)
                    {
                        releventStack = callStack;
                        length = releventStack.Length;
                    }

                    /// <summary>
                    /// Determines if the given string contains token.  This is a case sensitive match.
                    /// </summary>
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

                    /// <summary>
                    /// Discards the next line of the stack trace.
                    /// </summary>
                    public void DiscardNext()
                    {
                        GetLine();
                    }

                    /// <summary>
                    /// Discards all lines up to and including the line that contains discardText.
                    /// </summary>
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

                    /// <summary>
                    /// Retrieves the next line of the stack.
                    /// </summary>
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

                    /// <summary>
                    /// Retrieves the string of the parsed stack trace
                    /// </summary>
                    public override string ToString()
                    {
                        return releventStack.Substring(startIndex);
                    }

                    /// <summary>
                    /// Truncates the stack trace, saving the given # of lines.
                    /// </summary>
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
    }
}
