// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "System.Internal")]

namespace System.Internal
{
    internal sealed class HandleCollector
    {
        private static HandleType[] s_handleTypes;
        private static int s_handleTypeCount;

        internal static event HandleChangeEventHandler HandleAdded;

        internal static event HandleChangeEventHandler HandleRemoved;

        private static object s_internalSyncObject = new object();

        /// <summary>
        /// Adds the given handle to the handle collector. This keeps the handle on a "hot list" of objects that may
        /// need to be garbage collected.
        /// </summary>
        internal static IntPtr Add(IntPtr handle, int type)
        {
            s_handleTypes[type - 1].Add(handle);
            return handle;
        }

        /// <summary>
        /// Registers a new type of handle with the handle collector.
        /// </summary>
        internal static int RegisterType(string typeName, int expense, int initialThreshold)
        {
            lock (s_internalSyncObject)
            {
                if (s_handleTypeCount == 0 || s_handleTypeCount == s_handleTypes.Length)
                {
                    HandleType[] newTypes = new HandleType[s_handleTypeCount + 10];
                    if (s_handleTypes != null)
                    {
                        Array.Copy(s_handleTypes, 0, newTypes, 0, s_handleTypeCount);
                    }

                    s_handleTypes = newTypes;
                }

                s_handleTypes[s_handleTypeCount++] = new HandleType(typeName, expense, initialThreshold);
                return s_handleTypeCount;
            }
        }

        /// <summary>
        /// Removes the given handle from the handle collector. Removing a handle removes it from our "hot list" of
        /// objects that should be frequently garbage collected.
        /// </summary>
        internal static IntPtr Remove(IntPtr handle, int type)
        {
            return s_handleTypes[type - 1].Remove(handle);
        }

        /// <summary>
        /// Represents a specific type of handle.
        /// </summary>
        private class HandleType
        {
            internal readonly string name;

            private int _initialThreshHold;
            private int _threshHold;
            private int _handleCount;
            private readonly int _deltaPercent;

#if DEBUG_HANDLECOLLECTOR
            private List<IntPtr> handles = new List<IntPtr>();
#endif

            /// <summary>
            /// Creates a new handle type.
            /// </summary>
            internal HandleType(string name, int expense, int initialThreshHold)
            {
                this.name = name;
                _initialThreshHold = initialThreshHold;
                _threshHold = initialThreshHold;
                _deltaPercent = 100 - expense;
            }

            /// <summary>
            /// Adds a handle to this handle type for monitoring.
            /// </summary>            
            internal void Add(IntPtr handle)
            {
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                bool performCollect = false;
                int currentCount = 0;

                lock (this)
                {
                    _handleCount++;
#if DEBUG_HANDLECOLLECTOR
                    Debug.Assert(!handles.Contains(handle));
                    handles.Add(handle);
#endif
                    performCollect = NeedCollection();
                    currentCount = _handleCount;
                }
                lock (s_internalSyncObject)
                {
                    HandleAdded?.Invoke(name, handle, currentCount);
                }

                if (!performCollect)
                {
                    return;
                }


                if (performCollect)
                {
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> Forcing garbage collect");
                    Debug.WriteLine("HC>     name        :" + name);
                    Debug.WriteLine("HC>     threshHold  :" + (threshHold).ToString());
                    Debug.WriteLine("HC>     handleCount :" + (handleCount).ToString());
                    Debug.WriteLine("HC>     deltaPercent:" + (deltaPercent).ToString());
#endif                  
                    GC.Collect();

                    // We just performed a GC.  If the main thread is in a tight
                    // loop there is a this will cause us to increase handles forever and prevent handle collector
                    // from doing its job.  Yield the thread here.  This won't totally cause
                    // a finalization pass but it will effectively elevate the priority
                    // of the finalizer thread just for an instant.  But how long should
                    // we sleep?  We base it on how expensive the handles are because the
                    // more expensive the handle, the more critical that it be reclaimed.
                    int sleep = (100 - _deltaPercent) / 4;
                    System.Threading.Thread.Sleep(sleep);
                }
            }

            /// <summary>
            /// Determines if this handle type needs a garbage collection pass.
            /// </summary>
            internal bool NeedCollection()
            {
                if (_handleCount > _threshHold)
                {
                    _threshHold = _handleCount + ((_handleCount * _deltaPercent) / 100);
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> NeedCollection: increase threshHold to " + threshHold);
#endif                  
                    return true;
                }

                // If handle count < threshHold, we don't
                // need to collect, but if it 10% below the next lowest threshhold we
                // will bump down a rung.  We need to choose a percentage here or else
                // we will oscillate.
                int oldThreshHold = (100 * _threshHold) / (100 + _deltaPercent);
                if (oldThreshHold >= _initialThreshHold && _handleCount < (int)(oldThreshHold * .9F))
                {
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> NeedCollection: throttle threshhold " + threshHold + " down to " + oldThreshHold);
#endif                  
                    _threshHold = oldThreshHold;
                }

                return false;
            }

            /// <summary>
            /// Removes the given handle from our monitor list.
            /// </summary>
            internal IntPtr Remove(IntPtr handle)
            {
                if (handle == IntPtr.Zero)
                {
                    return handle;
                }
                int currentCount = 0;
                lock (this)
                {
                    _handleCount--;
#if DEBUG_HANDLECOLLECTOR
                    Debug.Assert(handles.Contains(handle));
                    handles.Remove(handle);
#endif
                    if (_handleCount < 0)
                    {
                        Debug.Fail("Handle collector underflow for type '" + name + "'");
                        _handleCount = 0;
                    }
                    currentCount = _handleCount;
                }
                lock (s_internalSyncObject)
                {
                    HandleRemoved?.Invoke(name, handle, currentCount);
                }
                return handle;
            }
        }
    }

    internal delegate void HandleChangeEventHandler(string handleType, IntPtr handleValue, int currentHandleCount);
}
