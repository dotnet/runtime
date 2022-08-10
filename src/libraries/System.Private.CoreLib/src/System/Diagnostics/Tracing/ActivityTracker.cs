// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Threading;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// Tracks activities.  This is meant to be a singleton (accessed by the ActivityTracer.Instance static property)
    ///
    /// Logically this is simply holds the m_current variable that holds the async local that holds the current ActivityInfo
    /// An ActivityInfo is represents a activity (which knows its creator and thus knows its path).
    ///
    /// Most of the magic is in the async local (it gets copied to new tasks)
    ///
    /// On every start event call OnStart
    ///
    ///     Guid activityID;
    ///     Guid relatedActivityID;
    ///     if (OnStart(activityName, out activityID, out relatedActivityID, ForceStop, options))
    ///         // Log Start event with activityID and relatedActivityID
    ///
    /// On every stop event call OnStop
    ///
    ///     Guid activityID;
    ///     if (OnStop(activityName, ref activityID  ForceStop))
    ///         // Stop event with activityID
    ///
    /// On any normal event log the event with activityTracker.CurrentActivityId
    /// </summary>
    internal sealed class ActivityTracker
    {
        /// <summary>
        /// Called on work item begins.  The activity name = providerName + activityName without 'Start' suffix.
        /// It updates CurrentActivityId to track.
        ///
        /// It returns true if the Start should be logged, otherwise (if it is illegal recursion) it return false.
        ///
        /// The start event should use as its activity ID the CurrentActivityId AFTER calling this routine and its
        /// RelatedActivityID the CurrentActivityId BEFORE calling this routine (the creator).
        ///
        /// If activity tracing is not on, then activityId and relatedActivityId are not set
        /// </summary>
        public void OnStart(string providerName, string activityName, int task, ref Guid activityId, ref Guid relatedActivityId, EventActivityOptions options, bool useTplSource = true)
        {
            if (m_current == null)        // We are not enabled
            {
                // We  used to rely on the TPL provider turning us on, but that has the disadvantage that you don't get Start-Stop tracking
                // until you use Tasks for the first time (which you may never do).   Thus we change it to pull rather tan push for whether
                // we are enabled.
                if (m_checkedForEnable)
                    return;
                m_checkedForEnable = true;
                if (useTplSource && TplEventSource.Log.IsEnabled(EventLevel.Informational, TplEventSource.Keywords.TasksFlowActivityIds))
                    Enable();
                if (m_current == null)
                    return;
            }

            Debug.Assert((options & EventActivityOptions.Disable) == 0);

            ActivityInfo? currentActivity = m_current.Value;
            string fullActivityName = NormalizeActivityName(providerName, activityName, task);

            TplEventSource? log = useTplSource ? TplEventSource.Log : null;
            bool tplDebug = log != null && log.Debug;
            if (tplDebug)
            {
                log!.DebugFacilityMessage("OnStartEnter", fullActivityName);
                log!.DebugFacilityMessage("OnStartEnterActivityState", ActivityInfo.LiveActivities(currentActivity));
            }

            if (currentActivity != null)
            {
                // Stop activity tracking if we reached the maximum allowed depth
                if (currentActivity.m_level >= MAX_ACTIVITY_DEPTH)
                {
                    activityId = Guid.Empty;
                    relatedActivityId = Guid.Empty;
                    if (tplDebug)
                        log!.DebugFacilityMessage("OnStartRET", "Fail");
                    return;
                }
                // Check for recursion, and force-stop any activities if the activity already started.
                if ((options & EventActivityOptions.Recursive) == 0)
                {
                    ActivityInfo? existingActivity = FindActiveActivity(fullActivityName, currentActivity);
                    if (existingActivity != null)
                    {
                        OnStop(providerName, activityName, task, ref activityId);
                        currentActivity = m_current.Value;
                    }
                }
            }

            // Get a unique ID for this activity.
            long id;
            if (currentActivity == null)
                id = Interlocked.Increment(ref m_nextId);
            else
                id = Interlocked.Increment(ref currentActivity.m_lastChildID);

            // The previous ID is my 'causer' and becomes my related activity ID
            relatedActivityId = EventSource.CurrentThreadActivityId;

            // Add to the list of started but not stopped activities.
            ActivityInfo newActivity = new ActivityInfo(fullActivityName, id, currentActivity, relatedActivityId, options);
            m_current.Value = newActivity;

            // Remember the current ID so we can log it
            activityId = newActivity.ActivityId;

            if (tplDebug)
            {
                log!.DebugFacilityMessage("OnStartRetActivityState", ActivityInfo.LiveActivities(newActivity));
                log!.DebugFacilityMessage1("OnStartRet", activityId.ToString(), relatedActivityId.ToString());
            }
        }

        /// <summary>
        /// Called when a work item stops.  The activity name = providerName + activityName without 'Stop' suffix.
        /// It updates m_current variable to track this fact.   The Stop event associated with stop should log the ActivityID associated with the event.
        ///
        /// If activity tracing is not on, then activityId and relatedActivityId are not set
        /// </summary>
        public void OnStop(string providerName, string activityName, int task, ref Guid activityId, bool useTplSource = true)
        {
            if (m_current == null)        // We are not enabled
                return;

            string fullActivityName = NormalizeActivityName(providerName, activityName, task);

            TplEventSource? log = useTplSource ? TplEventSource.Log : null;
            bool tplDebug = log != null && log.Debug;
            if (tplDebug)
            {
                log!.DebugFacilityMessage("OnStopEnter", fullActivityName);
                log!.DebugFacilityMessage("OnStopEnterActivityState", ActivityInfo.LiveActivities(m_current.Value));
            }

            while (true) // This is a retry loop.
            {
                ActivityInfo? currentActivity = m_current.Value;
                ActivityInfo? newCurrentActivity = null;               // if we have seen any live activities (orphans), at he first one we have seen.

                // Search to find the activity to stop in one pass.   This insures that we don't let one mistake
                // (stopping something that was not started) cause all active starts to be stopped
                // By first finding the target start to stop we are more robust.
                ActivityInfo? activityToStop = FindActiveActivity(fullActivityName, currentActivity);

                // ignore stops where we can't find a start because we may have popped them previously.
                if (activityToStop == null)
                {
                    activityId = Guid.Empty;
                    // TODO add some logging about this. Basically could not find matching start.
                    if (tplDebug)
                        log!.DebugFacilityMessage("OnStopRET", "Fail");
                    return;
                }

                activityId = activityToStop.ActivityId;

                // See if there are any orphans that need to be stopped.
                ActivityInfo? orphan = currentActivity;
                while (orphan != activityToStop && orphan != null)
                {
                    if (orphan.m_stopped != 0)      // Skip dead activities.
                    {
                        orphan = orphan.m_creator;
                        continue;
                    }
                    if (orphan.CanBeOrphan())
                    {
                        // We can't pop anything after we see a valid orphan, remember this for later when we update m_current.
                        newCurrentActivity ??= orphan;
                    }
                    else
                    {
                        orphan.m_stopped = 1;
                        Debug.Assert(orphan.m_stopped != 0);
                    }
                    orphan = orphan.m_creator;
                }

                // try to Stop the activity atomically.  Other threads may be trying to do this as well.
                if (Interlocked.CompareExchange(ref activityToStop.m_stopped, 1, 0) == 0)
                {
                    // I succeeded stopping this activity. Now we update our m_current pointer

                    // If I haven't yet determined the new current activity, it is my creator.
                    newCurrentActivity ??= activityToStop.m_creator;

                    m_current.Value = newCurrentActivity;

                    if (tplDebug)
                    {
                        log!.DebugFacilityMessage("OnStopRetActivityState", ActivityInfo.LiveActivities(newCurrentActivity));
                        log!.DebugFacilityMessage("OnStopRet", activityId.ToString());
                    }
                    return;
                }
                // We failed to stop it.  We must have hit a race to stop it.  Just start over and try again.
            }
        }

        /// <summary>
        /// Turns on activity tracking.    It is sticky, once on it stays on (race issues otherwise)
        /// </summary>
        public void Enable()
        {
            if (m_current == null)
            {
                // Catch the not Implemented
                try
                {
                    m_current = new AsyncLocal<ActivityInfo?>(ActivityChanging);
                }
                catch (NotImplementedException)
                {
                    // send message to debugger without delay
                    System.Diagnostics.Debugger.Log(0, null, "Activity Enabled() called but AsyncLocals Not Supported (pre V4.6).  Ignoring Enable");
                }
            }
        }

        /// <summary>
        /// An activity tracker is a singleton, this is how you get the one and only instance.
        /// </summary>
        public static ActivityTracker Instance => s_activityTrackerInstance;

        #region private

        /// <summary>
        /// Searched for a active (nonstopped) activity with the given name.  Returns null if not found.
        /// </summary>
        private static ActivityInfo? FindActiveActivity(string name, ActivityInfo? startLocation)
        {
            ActivityInfo? activity = startLocation;
            while (activity != null)
            {
                if (name == activity.m_name && activity.m_stopped == 0)
                    return activity;
                activity = activity.m_creator;
            }
            return null;
        }

        /// <summary>
        /// Strip out "Start" or "End" suffix from activity name and add providerName prefix.
        /// If 'task'  it does not end in Start or Stop and Task is non-zero use that as the name of the activity
        /// </summary>
        private static string NormalizeActivityName(string providerName, string activityName, int task)
        {
            // We use provider name to distinguish between activities from different providers.

            if (activityName.EndsWith(EventSource.s_ActivityStartSuffix, StringComparison.Ordinal))
            {
                return string.Concat(providerName, activityName.AsSpan(0, activityName.Length - EventSource.s_ActivityStartSuffix.Length));
            }
            else if (activityName.EndsWith(EventSource.s_ActivityStopSuffix, StringComparison.Ordinal))
            {
                return string.Concat(providerName, activityName.AsSpan(0, activityName.Length - EventSource.s_ActivityStopSuffix.Length));
            }
            else if (task != 0)
            {
                return providerName + "task" + task.ToString();
            }
            else
            {
                return providerName + activityName;
            }
        }

        // *******************************************************************************
        /// <summary>
        /// An ActivityInfo represents a particular activity.   It is almost read-only.   The only
        /// fields that change after creation are
        ///    m_lastChildID - used to generate unique IDs for the children activities and for the most part can be ignored.
        ///    m_stopped - indicates that this activity is dead
        /// This read-only-ness is important because an activity's  m_creator chain forms the
        /// 'Path of creation' for the activity (which is also its unique ID) but is also used as
        /// the 'list of live parents' which indicate of those ancestors, which are alive (if they
        /// are not marked dead they are alive).
        /// </summary>
        private sealed class ActivityInfo
        {
            public ActivityInfo(string name, long uniqueId, ActivityInfo? creator, Guid activityIDToRestore, EventActivityOptions options)
            {
                m_name = name;
                m_eventOptions = options;
                m_creator = creator;
                m_uniqueId = uniqueId;
                m_level = creator != null ? creator.m_level + 1 : 0;
                m_activityIdToRestore = activityIDToRestore;

                // Create a nice GUID that encodes the chain of activities that started this one.
                CreateActivityPathGuid(out m_guid, out m_activityPathGuidOffset);
            }

            public Guid ActivityId => m_guid;

            public static string Path(ActivityInfo? activityInfo)
            {
                if (activityInfo == null)
                    return "";
                return $"{Path(activityInfo.m_creator)}/{activityInfo.m_uniqueId}";
            }

            public override string ToString()
            {
                return m_name + "(" + Path(this) + (m_stopped != 0 ? ",DEAD)" : ")");
            }

            public static string LiveActivities(ActivityInfo? list)
            {
                if (list == null)
                    return "";
                return list.ToString() + ";" + LiveActivities(list.m_creator);
            }

            public bool CanBeOrphan()
            {
                if ((m_eventOptions & EventActivityOptions.Detachable) != 0)
                    return true;
                return false;
            }

            #region private

            #region CreateActivityPathGuid
            /// <summary>
            /// Logically every activity Path (see Path()) that describes the activities that caused this
            /// (rooted in an activity that predates activity tracking.
            ///
            /// We wish to encode this path in the Guid to the extent that we can.  Many of the paths have
            /// many small numbers in them and we take advantage of this in the encoding to output as long
            /// a path in the GUID as possible.
            ///
            /// Because of the possibility of GUID collision, we only use 96 of the 128 bits of the GUID
            /// for encoding the path.  The last 32 bits are a simple checksum (and random number) that
            /// identifies this as using the convention defined here.
            ///
            /// It returns both the GUID which has the path as well as the offset that points just beyond
            /// the end of the activity (so it can be appended to).  Note that if the end is in a nibble
            /// (it uses nibbles instead of bytes as the unit of encoding, then it will point at the unfinished
            /// byte (since the top nibble can't be zero you can determine if this is true by seeing if
            /// this byte is nonZero.   This offset is needed to efficiently create the ID for child activities.
            /// </summary>
            private unsafe void CreateActivityPathGuid(out Guid idRet, out int activityPathGuidOffset)
            {
                fixed (Guid* outPtr = &idRet)
                {
                    int activityPathGuidOffsetStart = 0;
                    if (m_creator != null)
                    {
                        activityPathGuidOffsetStart = m_creator.m_activityPathGuidOffset;
                        idRet = m_creator.m_guid;
                    }
                    else
                    {
                        int appDomainID = System.Threading.Thread.GetDomainID();
                        // We start with the appdomain number to make this unique among appdomains.
                        activityPathGuidOffsetStart = AddIdToGuid(outPtr, activityPathGuidOffsetStart, (uint)appDomainID);
                    }

                    activityPathGuidOffset = AddIdToGuid(outPtr, activityPathGuidOffsetStart, (uint)m_uniqueId);

                    // If the path does not fit, Make a GUID by incrementing rather than as a path, keeping as much of the path as possible
                    if (12 < activityPathGuidOffset)
                        CreateOverflowGuid(outPtr);
                }
            }

            /// <summary>
            /// If we can't fit the activity Path into the GUID we come here.   What we do is simply
            /// generate a 4 byte number (s_nextOverflowId).  Then look for an ancestor that has
            /// sufficient space for this ID.   By doing this, we preserve the fact that this activity
            /// is a child (of unknown depth) from that ancestor.
            /// </summary>
            private unsafe void CreateOverflowGuid(Guid* outPtr)
            {
                // Search backwards for an ancestor that has sufficient space to put the ID.
                for (ActivityInfo? ancestor = m_creator; ancestor != null; ancestor = ancestor.m_creator)
                {
                    if (ancestor.m_activityPathGuidOffset <= 10)  // we need at least 2 bytes.
                    {
                        uint id = unchecked((uint)Interlocked.Increment(ref ancestor.m_lastChildID));        // Get a unique ID
                        // Try to put the ID into the GUID
                        *outPtr = ancestor.m_guid;
                        int endId = AddIdToGuid(outPtr, ancestor.m_activityPathGuidOffset, id, true);

                        // Does it fit?
                        if (endId <= 12)
                            break;
                    }
                }
            }

            /// <summary>
            /// The encoding for a list of numbers used to make Activity  GUIDs.   Basically
            /// we operate on nibbles (which are nice because they show up as hex digits).  The
            /// list is ended with a end nibble (0) and depending on the nibble value (Below)
            /// the value is either encoded into nibble itself or it can spill over into the
            /// bytes that follow.
            /// </summary>
            private enum NumberListCodes : byte
            {
                End = 0x0,             // ends the list.   No valid value has this prefix.
                LastImmediateValue = 0xA,

                PrefixCode = 0xB,      // all the 'long' encodings go here.  If the next nibble is MultiByte1-4
                                       // than this is a 'overflow' id.   Unlike the hierarchical IDs these are
                                       // allocated densely but don't tell you anything about nesting. we use
                                       // these when we run out of space in the GUID to store the path.

                MultiByte1 = 0xC,   // 1 byte follows.  If this Nibble is in the high bits, it the high bits of the number are stored in the low nibble.
                // commented out because the code does not explicitly reference the names (but they are logically defined).
                // MultiByte2 = 0xD,   // 2 bytes follow (we don't bother with the nibble optimization)
                // MultiByte3 = 0xE,   // 3 bytes follow (we don't bother with the nibble optimization)
                // MultiByte4 = 0xF,   // 4 bytes follow (we don't bother with the nibble optimization)
            }

            /// Add the activity id 'id' to the output Guid 'outPtr' starting at the offset 'whereToAddId'
            /// Thus if this number is 6 that is where 'id' will be added.    This will return 13 (12
            /// is the maximum number of bytes that fit in a GUID) if the path did not fit.
            /// If 'overflow' is true, then the number is encoded as an 'overflow number (which has a
            /// special (longer prefix) that indicates that this ID is allocated differently
            private static unsafe int AddIdToGuid(Guid* outPtr, int whereToAddId, uint id, bool overflow = false)
            {
                byte* ptr = (byte*)outPtr;
                byte* endPtr = ptr + 12;
                ptr += whereToAddId;
                if (endPtr <= ptr)
                    return 13;                // 12 means we might exactly fit, 13 means we definitely did not fit

                if (0 < id && id <= (uint)NumberListCodes.LastImmediateValue && !overflow)
                    WriteNibble(ref ptr, endPtr, id);
                else
                {
                    uint len = 4;
                    if (id <= 0xFF)
                        len = 1;
                    else if (id <= 0xFFFF)
                        len = 2;
                    else if (id <= 0xFFFFFF)
                        len = 3;

                    if (overflow)
                    {
                        if (endPtr <= ptr + 2)        // I need at least 2 bytes
                            return 13;

                        // Write out the prefix code nibble and the length nibble
                        WriteNibble(ref ptr, endPtr, (uint)NumberListCodes.PrefixCode);
                    }
                    // The rest is the same for overflow and non-overflow case
                    WriteNibble(ref ptr, endPtr, (uint)NumberListCodes.MultiByte1 + (len - 1));

                    // Do we have an odd nibble?   If so flush it or use it for the 12 byte case.
                    if (ptr < endPtr && *ptr != 0)
                    {
                        // If the value < 4096 we can use the nibble we are otherwise just outputting as padding.
                        if (id < 4096)
                        {
                            // Indicate this is a 1 byte multicode with 4 high order bits in the lower nibble.
                            *ptr = (byte)(((uint)NumberListCodes.MultiByte1 << 4) + (id >> 8));
                            id &= 0xFF;     // Now we only want the low order bits.
                        }
                        ptr++;
                    }

                    // Write out the bytes.
                    while (0 < len)
                    {
                        if (endPtr <= ptr)
                        {
                            ptr++;        // Indicate that we have overflowed
                            break;
                        }
                        *ptr++ = (byte)id;
                        id >>= 8;
                        --len;
                    }
                }

                // Compute the checksum
                uint* sumPtr = (uint*)outPtr;
                // We set the last DWORD the sum of the first 3 DWORDS in the GUID.   This
                // This last number is a random number (it identifies us as us)  the process ID to make it unique per process.
                sumPtr[3] = (sumPtr[0] + sumPtr[1] + sumPtr[2] + 0x599D99AD) ^ (uint)Environment.ProcessId;

                return (int)(ptr - ((byte*)outPtr));
            }

            /// <summary>
            /// Write a single Nible 'value' (must be 0-15) to the byte buffer represented by *ptr.
            /// Will not go past 'endPtr'.  Also it assumes that we never write 0 so we can detect
            /// whether a nibble has already been written to ptr  because it will be nonzero.
            /// Thus if it is non-zero it adds to the current byte, otherwise it advances and writes
            /// the new byte (in the high bits) of the next byte.
            /// </summary>
            private static unsafe void WriteNibble(ref byte* ptr, byte* endPtr, uint value)
            {
                Debug.Assert(value < 16);
                Debug.Assert(ptr < endPtr);

                if (*ptr != 0)
                    *ptr++ |= (byte)value;
                else
                    *ptr = (byte)(value << 4);
            }

            #endregion // CreateGuidForActivityPath

            internal readonly string m_name;                        // The name used in the 'start' and 'stop' APIs to help match up
            private readonly long m_uniqueId;                               // a small number that makes this activity unique among its siblings
            internal readonly Guid m_guid;                          // Activity Guid, it is basically an encoding of the Path() (see CreateActivityPathGuid)
            internal readonly int m_activityPathGuidOffset;         // Keeps track of where in m_guid the causality path stops (used to generated child GUIDs)
            internal readonly int m_level;                          // current depth of the Path() of the activity (used to keep recursion under control)
            internal readonly EventActivityOptions m_eventOptions;  // Options passed to start.
            internal long m_lastChildID;                            // used to create a unique ID for my children activities
            internal int m_stopped;                                 // This work item has stopped
            internal readonly ActivityInfo? m_creator;               // My parent (creator).  Forms the Path() for the activity.
            internal readonly Guid m_activityIdToRestore;           // The Guid to restore after a stop.
            #endregion
        }

        // This callback is used to initialize the m_current AsyncLocal Variable.
        // Its job is to keep the ETW Activity ID (part of thread local storage) in sync
        // with m_current.ActivityID
        //
        // WARNING: When mixing manual usage of EventSource.SetCurrentThreadActivityID
        // and Start/Stop EventSource events I can't identify a clear design how this
        // synchronization is intended to work. For example code that changes
        // SetCurrentThreadActivityID after a FooStart() event will not flow the
        // explicit ID with the async work, but if FooStart() event is called after
        // SetCurrentThreadActivityID then the explicit ID change does flow.
        // For now I've adopted the approach:
        // Priority 1: Make the API predictable/sensible when only Start/Stop events
        // are in use.
        // Priority 2: If users aren't complaining and it doesn't interfere with
        // goal #1, try to preserve the arbitrary/buggy? existing behavior
        // for mixed usage of SetActivityID + events.
        //
        // For scenarios that only use start/stop events this is what we expect:
        // calling start -> push new ID on stack and update thread-local to match new ID
        // calling stop -> pop ID from stack and update thread-local to match new topmost
        //                 still active ID. If there is none, set ID to zero
        // thread swap -> update thread-local to match topmost active ID.
        //                 If there is none, set ID to zero.
        private void ActivityChanging(AsyncLocalValueChangedArgs<ActivityInfo?> args)
        {
            ActivityInfo? cur = args.CurrentValue;
            ActivityInfo? prev = args.PreviousValue;

            // Special case only relevant for mixed SetActivityID usage:
            //
            // Are we MAYBE popping off a value?   (we have a prev, and it creator is cur)
            // We can't be certain this is a pop because a thread swapping between two
            // ExecutionContexts can also appear the same way.
            // Then check if we should use the GUID at the time of the start event
            if (prev != null && prev.m_creator == cur)
            {
                // If the saved activity ID is not the same as the creator activity
                // that takes precedence (it means someone explicitly did a SetActivityID)
                // Set it to that and get out
                if (cur == null || prev.m_activityIdToRestore != cur.ActivityId)
                {
                    EventSource.SetCurrentThreadActivityId(prev.m_activityIdToRestore);
                    return;
                }
            }

            // Set the activity to current ActivityInfo.  However that activity
            // might be dead, in which case we should skip it, so we never set
            // the ID to dead things.
            while (cur != null)
            {
                // We found a live activity (typically the first time), set it to that.
                if (cur.m_stopped == 0)
                {
                    EventSource.SetCurrentThreadActivityId(cur.ActivityId);
                    return;
                }
                cur = cur.m_creator;
            }

            // we can get here if there is no information on our activity stack (everything is dead)
            // Set ActivityID to zero
            EventSource.SetCurrentThreadActivityId(Guid.Empty);
        }

        /// <summary>
        /// Async local variables have the property that the are automatically copied whenever a task is created and used
        /// while that task is running.   Thus m_current 'flows' to any task that is caused by the current thread that
        /// last set it.
        ///
        /// This variable points to a linked list that represents all Activities that have started but have not stopped.
        /// </summary>
        private AsyncLocal<ActivityInfo?>? m_current;
        private bool m_checkedForEnable;

        // Singleton
        private static readonly ActivityTracker s_activityTrackerInstance = new ActivityTracker();

        // Used to create unique IDs at the top level.  Not used for nested Ids (each activity has its own id generator)
        private static long m_nextId;
        private const ushort MAX_ACTIVITY_DEPTH = 100;            // Limit maximum depth of activities to be tracked at 100.
                                                                  // This will avoid leaking memory in case of activities that are never stopped.

        #endregion
    }
}
