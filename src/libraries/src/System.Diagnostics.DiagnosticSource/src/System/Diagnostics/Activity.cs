// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// Activity represents operation with context to be used for logging.
    /// Activity has operation name, Id, start time and duration, tags and baggage.
    ///  
    /// Current activity can be accessed with static AsyncLocal variable Activity.Current.
    /// 
    /// Activities should be created with constructor, configured as necessarily 
    /// and then started with Activity.Start method which maintaines parent-child
    /// relationships for the activities and sets Activity.Current.
    /// 
    /// When activity is finished, it should be stopped with static Activity.Stop method.
    /// </summary>
    public partial class Activity
    {
        /// <summary>
        /// An operation name is a COARSEST name that is useful grouping/filtering. 
        /// The name is typically a compile time constant.   Names of Rest APIs are 
        /// reasonable, but arguments (e.g. specific accounts etc), should not be in
        /// the name but rather in the tags.  
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// This is an ID that is specific to a particular request.   Filtering
        /// to a particular ID insures that you get only one request that matches.  
        /// It is typically assigned the system itself. 
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The time that operation started.  Typcially when Start() is called 
        /// (but you can pass a value to Start() if necessary.  This use UTC (Greenwitch Mean Time)
        /// </summary>
        public DateTime StartTimeUtc { get; private set; }

        /// <summary>
        /// If the Activity that created this activity is  from the same process you can get 
        /// that Activit with Parent.   However this can be null if the Activity has no
        /// parent (a root activity) or if the Parent is from outside the process.  (see ParentId for more)
        /// </summary>
        public Activity Parent { get; private set; }

        /// <summary>
        /// If the parent for this activity comes from outside the process, the activity
        /// does not have a Parent Activity but MAY have a ParentId (which was serialized from
        /// from the parent) .   This accessor fetches the parent ID if it exists at all.  
        /// Note this can be null if this is a root Activity (it has no parent)
        /// </summary>
        public string ParentId { get; private set; }

        /// <summary>
        /// Tags are string-string key-value pairs that represent information that will
        /// be logged along with the Activity to the logging system.   This information
        /// however is NOT passed on to the children of this activity.  (see Baggage)
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Tags
        {
            get
            {
                for (var tags = _tags; tags != null; tags = tags.Next)
                    yield return tags.keyValue;
            }
        }

        /// <summary>
        /// Baggage is string-string key-value pairs that represent information that will
        /// be passed along to children of this activity.   Baggage is serialized 
        /// when requests leave the process (along with the ID).   Typically Baggage is
        /// used to do fine-grained control over logging of the activty and any children.  
        /// In general, if you are not using the data at runtime, you should be using Tags 
        /// instead. 
        /// </summary> 
        public IEnumerable<KeyValuePair<string, string>> Baggage
        {
            get
            {
                for (var activity = this; activity != null; activity = activity.Parent)
                    for (var baggage = activity._baggage; baggage != null; baggage = baggage.Next)
                        yield return baggage.keyValue;
            }
        }

        /// <summary>
        /// Returns the value of the key-value pair added to the activity with 'WithBaggage'.
        /// Returns null if that key does not exist.  
        /// </summary>
        public string GetBaggageItem(string key)
        {
            foreach (var keyValue in Baggage)
                if (key == keyValue.Key)
                    return keyValue.Value;
            return null;
        }

        /* Constructors  Builder methods */

        /// <summary>
        /// Note that Activity has a 'builder' pattern, where you call the constructor, a number of 'With*' APIs and then
        /// call 'Activity.Start' to build the activity.   You MUST call Start before using it 
        /// </summary>
        /// <param name="operationName">Operations name <see cref="OperationName"/></param>
        public Activity(string operationName)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException($"{nameof(operationName)} must not be null or empty");
            OperationName = operationName;
        }

        /// <summary>
        /// Update the Activity to have a tag with an additional 'key' and value 'value'.
        /// This shows up in the 'Tags' eumeration.   It is meant for information that
        /// is useful to log but not needed for runtime control (for the latter, use Baggage)
        /// </summary>
        /// <returns>'this' for convinient chaining</returns>
        public Activity AddTag(string key, string value)
        {
            _tags = new KeyValueListNode() { keyValue = new KeyValuePair<string, string>(key, value), Next = _tags };
            return this;
        }

        /// <summary>
        /// Update the Activity to have baggage with an additional 'key' and value 'value'.
        /// This shows up in the 'Baggage' eumeration as well as the 'GetBaggageItem' API.
        /// Baggage is mean for information that is needed for runtime control.   For information 
        /// that is simply useful to show up in the log with the activity use Tags.   
        /// Returns 'this' for convinient chaining.
        /// </summary>
        /// <returns>'this' for convinient chaining</returns>
        public Activity AddBaggage(string key, string value)
        {
            _baggage = new KeyValueListNode() { keyValue = new KeyValuePair<string, string>(key, value), Next = _baggage };
            return this;
        }

        /// <summary>
        /// Updates the Activity To indicate that the activity with ID 'parentID' 
        /// caused this activity.   This is only intended to be used at 'boundary' 
        /// scenarios where an activity from another process loggically started 
        /// this activity. The Parent ID shows up the Tags (as well as the ParentID 
        /// property), and can be used to reconstruct the causal tree.  
        /// Returns 'this' for convinient chaining.
        /// </summary>
        public Activity SetParentId(string parentId)
        {
            if (Parent != null)
                throw new InvalidOperationException(
                    $"Trying to set {nameof(ParentId)} on activity which has {nameof(Parent)}");

            if (ParentId != null)
                throw new InvalidOperationException(
                    $"{nameof(ParentId)} is already set");

            if (string.IsNullOrEmpty(parentId))
                throw new ArgumentException($"{nameof(parentId)} must not be null or empty");

            ParentId = parentId;
            return this;
        }

        /// <summary>
        /// Update the Activity to set start time
        /// </summary>
        /// <param name="startTimeUtc">Activity start time in UTC (Greenwitch Mean Time)</param>
        /// <returns>'this' for convinient chaining</returns>
        public Activity SetStartTime(DateTime startTimeUtc)
        {
            if (startTimeUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException($"{nameof(startTimeUtc)} is not UTC");
            StartTimeUtc = startTimeUtc;
            return this;
        }
        /// <summary>
        /// Update the Activity to set <see cref="Duration"/>
        /// as a difference between <see cref="StartTimeUtc"/>
        /// and given stop timestamp
        /// </summary>
        /// <param name="endTimeUtc">Activity stop time in UTC (Greenwitch Mean Time)</param>
        /// <returns>'this' for convinient chaining</returns>
        public Activity SetEndTime(DateTime endTimeUtc)
        {
            if (endTimeUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException($"{nameof(endTimeUtc)} is not UTC");

            Duration = endTimeUtc - StartTimeUtc;
            return this;
        }

        /// <summary>
        /// If the Activity has ended (Stop was called) then this is the delta
        /// between start and end.   If the activity is not ended then this is 
        /// TimeSpan.Zero.  
        /// </summary>
        public TimeSpan Duration { get; private set; }

        /// <summary>
        /// Starts activity: sets <see cref="Parent"/> to hold <see cref="Current"/> and sets Current to this Activity.
        /// If <see cref="StartTimeUtc"/> was not set previously, sets it to DateTime.UtcNow.
        /// Use DiagnosticSource.Start to start activity and write start event.
        /// </summary>
        /// <returns>Started activity for convinient chaining</returns>
        /// <seealso cref="DiagnosticSource.StartActivity(Activity, object)"/>
        /// <seealso cref="SetStartTime(DateTime)"/>
        public Activity Start()
        {
            if (Id != null)
                throw new InvalidOperationException("Trying to start an Activity that was already started");

            if (ParentId == null)
            {
                var parent = Current;
                if (parent != null)
                {
                    ParentId = parent.Id;
                    Parent = parent;
                }
            }

            if (StartTimeUtc == default(DateTime))
                StartTimeUtc = DateTime.UtcNow;

            Id = GenerateId();

            Current = this;
            return this;
        }

        /// <summary>
        /// Stops activity: sets Current to Parent.
        /// If end time was not set previously, sets <see cref="Duration"/> as a difference between DateTime.UtcNow and <see cref="StartTimeUtc"/>
        /// Use DiagnosticSource.Stop to stop activity and write stop event.
        /// </summary>
        /// <seealso cref="DiagnosticSource.StopActivity(Activity, object)"/>
        /// <seealso cref="SetEndTime(DateTime)"/>
        public void Stop()
        {
            if (Id == null)
                throw new InvalidOperationException("Trying to stop an Activity that was not started");

            if (!isFinished)
            {
                isFinished = true;

                if (Duration == TimeSpan.Zero)
                    SetEndTime(DateTime.UtcNow);

                Current = Parent;
            }
        }

        #region private 

        private string GenerateId()
        {
            string ret;
            if (Parent != null)
            {
                // Normal start within the process
                Debug.Assert(!string.IsNullOrEmpty(Parent.Id));
                ret = appendSuffix(Parent.Id, $"{Interlocked.Increment(ref Parent._currentChildId)}");
            }
            else if (ParentId != null)
            {
                // Start from outside the process (e.g. incoming HTTP)
                Debug.Assert(ParentId.Length != 0);
                ret = appendSuffix(ParentId, $"{Interlocked.Increment(ref s_currentRootId):x}");
            }
            else
            {
                ret = generateRootId();
            }

            // Useful place to place a conditional breakpoint.  
            return ret;
        }

        private string appendSuffix(string parentId, string suffix)
        {
            if (parentId.Length + suffix.Length <= 127)
                return $"{parentId}.{suffix}";

            //Id overflow:
            //find position in RequestId to trim
            int trimPosition = parentId.Length - 1;
            while (trimPosition > 0)
            {
                if ((parentId[trimPosition] == '.' || parentId[trimPosition] == '#')
                    && trimPosition <= 119) //overflow suffix length is 8 + 1 for #.
                    break;
                trimPosition--;
            }

            //ParentId is not valid Request-Id, let's generate proper one.
            if (trimPosition == 0)
                return generateRootId();

            //generate overflow suffix
            byte[] bytes = new byte[4];
            s_random.Value.NextBytes(bytes);

            return $"{parentId.Substring(0, trimPosition)}#{BitConverter.ToUInt32(bytes, 0):x8}";
        }

        private string generateRootId()
        {
            byte[] bytes = new byte[8];
            s_random.Value.NextBytes(bytes);
            return $"/{BitConverter.ToUInt64(bytes, 0):x}";
        }

        // Used to generate an ID 
        int _currentChildId;            // A unique number for all children of this activity.  
        static int s_currentRootId;      // A unique number inside the appdomain.
        private static readonly Lazy<Random> s_random = new Lazy<Random>();
        /// <summary>
        /// Having our own key-value linked list allows us to be more efficient  
        /// </summary>
        private class KeyValueListNode
        {
            public KeyValuePair<string, string> keyValue;
            public KeyValueListNode Next;
        }

        private KeyValueListNode _tags;
        private KeyValueListNode _baggage;
        private bool isFinished;
#endregion // private
    }
}
