// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

#if !ES_BUILD_AGAINST_DOTNET_V35
using Contract = System.Diagnostics.Contracts.Contract;
#else
using Contract = Microsoft.Diagnostics.Contracts.Internal.Contract;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Provides support for EventSource activities by marking the start and
    /// end of a particular operation.
    /// </summary>
    internal sealed class EventSourceActivity
        : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the EventSourceActivity class that
        /// is attached to the specified event source. The new activity will
        /// not be attached to any related (parent) activity.
        /// The activity is created in the Initialized state.
        /// </summary>
        /// <param name="eventSource">
        /// The event source to which the activity information is written.
        /// </param>
        public EventSourceActivity(EventSource eventSource)
        {
            if (eventSource == null)
                throw new ArgumentNullException(nameof(eventSource));

            this.eventSource = eventSource;
        }

        /// <summary>
        /// You can make an activity out of just an EventSource.  
        /// </summary>
        public static implicit operator EventSourceActivity(EventSource eventSource) { return new EventSourceActivity(eventSource); }

        /* Properties */
        /// <summary>
        /// Gets the event source to which this activity writes events.
        /// </summary>
        public EventSource EventSource
        {
            get { return this.eventSource; }
        }

        /// <summary>
        /// Gets this activity's unique identifier, or the default Guid if the
        /// event source was disabled when the activity was initialized.
        /// </summary>
        public Guid Id
        {
            get { return this.activityId; }
        }

#if false // don't expose RelatedActivityId unless there is a need.   
        /// <summary>
        /// Gets the unique identifier of this activity's related (parent)
        /// activity.
        /// </summary>
        public Guid RelatedId
        {
            get { return this.relatedActivityId; }
        }
#endif

        /// <summary>
        /// Writes a Start event with the specified name and data.   If the start event is not active (because the provider 
        /// is not on or keyword-level indicates the event is off, then the returned activity is simply the 'this' pointer
        /// and it is effectively like start did not get called.
        /// 
        /// A new activityID GUID is generated and the returned
        /// EventSourceActivity remembers this activity and will mark every event (including the start stop and any writes)
        /// with this activityID.   In addition the Start activity will log a 'relatedActivityID' that was the activity
        /// ID before the start event.   This way event processors can form a linked list of all the activities that
        /// caused this one (directly or indirectly).  
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event.   It is strongly suggested that this name end in 'Start' (e.g. DownloadStart).  
        /// If you do this, then the Stop() method will automatically replace the 'Start' suffix with a 'Stop' suffix.  
        /// </param>
        /// <param name="options">Allow options (keywords, level) to be set for the write associated with this start 
        /// These will also be used for the stop event.</param>
        /// <param name="data">The data to include in the event.</param>
        public EventSourceActivity Start<T>(string? eventName, EventSourceOptions options, T data)
        {
            return this.Start(eventName, ref options, ref data);
        }
        /// <summary>
        /// Shortcut version see Start(string eventName, EventSourceOptions options, T data) Options is empty (no keywords 
        /// and level==Info) Data payload is empty.  
        /// </summary>
        public EventSourceActivity Start(string? eventName)
        {
            var options = new EventSourceOptions();
            var data = new EmptyStruct();
            return this.Start(eventName, ref options, ref data);
        }
        /// <summary>
        /// Shortcut version see Start(string eventName, EventSourceOptions options, T data).  Data payload is empty. 
        /// </summary>
        public EventSourceActivity Start(string? eventName, EventSourceOptions options)
        {
            var data = new EmptyStruct();
            return this.Start(eventName, ref options, ref data);
        }
        /// <summary>
        /// Shortcut version see Start(string eventName, EventSourceOptions options, T data) Options is empty (no keywords 
        /// and level==Info) 
        /// </summary>
        public EventSourceActivity Start<T>(string? eventName, T data)
        {
            var options = new EventSourceOptions();
            return this.Start(eventName, ref options, ref data);
        }

        /// <summary>
        /// Writes a Stop event with the specified data, and sets the activity
        /// to the Stopped state.  The name is determined by the eventName used in Start.
        /// If that Start event name is suffixed with 'Start' that is removed, and regardless
        /// 'Stop' is appended to the result to form the Stop event name.  
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="data">The data to include in the event.</param>
        public void Stop<T>(T data)
        {
            this.Stop(null, ref data);
        }
        /// <summary>
        /// Used if you wish to use the non-default stop name (which is the start name with Start replace with 'Stop')
        /// This can be useful to indicate unusual ways of stopping (but it is still STRONGLY recommended that
        /// you start with the same prefix used for the start event and you end with the 'Stop' suffix.   
        /// </summary>
        public void Stop<T>(string? eventName)
        {
            var data = new EmptyStruct();
            this.Stop(eventName, ref data);
        }
        /// <summary>
        /// Used if you wish to use the non-default stop name (which is the start name with Start replace with 'Stop')
        /// This can be useful to indicate unusual ways of stopping (but it is still STRONGLY recommended that
        /// you start with the same prefix used for the start event and you end with the 'Stop' suffix.   
        /// </summary>
        public void Stop<T>(string? eventName, T data)
        {
            this.Stop(eventName, ref data);
        }

        /// <summary>
        /// Writes an event associated with this activity to the eventSource associated with this activity.  
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. If null, the name is determined from
        /// data's type.
        /// </param>
        /// <param name="options">
        /// The options to use for the event.
        /// </param>
        /// <param name="data">The data to include in the event.</param>
        public void Write<T>(string? eventName, EventSourceOptions options, T data)
        {
            this.Write(this.eventSource, eventName, ref options, ref data);
        }
        /// <summary>
        /// Writes an event associated with this activity.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. If null, the name is determined from
        /// data's type.
        /// </param>
        /// <param name="data">The data to include in the event.</param>
        public void Write<T>(string? eventName, T data)
        {
            var options = new EventSourceOptions();
            this.Write(this.eventSource, eventName, ref options, ref data);
        }
        /// <summary>
        /// Writes a trivial event associated with this activity.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. Must not be null.
        /// </param>
        /// <param name="options">
        /// The options to use for the event.
        /// </param>
        public void Write(string? eventName, EventSourceOptions options)
        {
            var data = new EmptyStruct();
            this.Write(this.eventSource, eventName, ref options, ref data);
        }
        /// <summary>
        /// Writes a trivial event associated with this activity.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. Must not be null.
        /// </param>
        public void Write(string? eventName)
        {
            var options = new EventSourceOptions();
            var data = new EmptyStruct();
            this.Write(this.eventSource, eventName, ref options, ref data);
        }
        /// <summary>
        /// Writes an event to a arbitrary eventSource stamped with the activity ID of this activity.   
        /// </summary>
        public void Write<T>(EventSource source, string? eventName, EventSourceOptions options, T data)
        {
            this.Write(source, eventName, ref options, ref data);
        }

        /// <summary>
        /// Releases any unmanaged resources associated with this object.
        /// If the activity is in the Started state, calls Stop().
        /// </summary>
        public void Dispose()
        {
            if (this.state == State.Started)
            {
                var data = new EmptyStruct();
                this.Stop(null, ref data);
            }
        }

        #region private
        private EventSourceActivity Start<T>(string? eventName, ref EventSourceOptions options, ref T data)
        {
            if (this.state != State.Started)
                throw new InvalidOperationException();

            // If the source is not on at all, then we don't need to do anything and we can simply return ourselves.  
            if (!this.eventSource.IsEnabled())
                return this;

            var newActivity = new EventSourceActivity(eventSource);
            if (!this.eventSource.IsEnabled(options.Level, options.Keywords))
            {
                // newActivity.relatedActivityId = this.Id;
                Guid relatedActivityId = this.Id;
                newActivity.activityId = Guid.NewGuid();
                newActivity.startStopOptions = options;
                newActivity.eventName = eventName;
                newActivity.startStopOptions.Opcode = EventOpcode.Start;
                this.eventSource.Write(eventName, ref newActivity.startStopOptions, ref newActivity.activityId, ref relatedActivityId, ref data);
            }
            else
            {
                // If we are not active, we don't set the eventName, which basically also turns off the Stop event as well.  
                newActivity.activityId = this.Id;
            }

            return newActivity;
        }

        private void Write<T>(EventSource eventSource, string? eventName, ref EventSourceOptions options, ref T data)
        {
            if (this.state != State.Started)
                throw new InvalidOperationException();      // Write after stop. 
            if (eventName == null)
                throw new ArgumentNullException();

            eventSource.Write(eventName, ref options, ref this.activityId, ref s_empty, ref data);
        }

        private void Stop<T>(string? eventName, ref T data)
        {
            if (this.state != State.Started)
                throw new InvalidOperationException();

            // If start was not fired, then stop isn't as well.  
            if (!StartEventWasFired)
                return;

            Debug.Assert(this.eventName != null);

            this.state = State.Stopped;
            if (eventName == null)
            {
                eventName = this.eventName;
                if (eventName.EndsWith("Start"))
                    eventName = eventName.Substring(0, eventName.Length - 5);
                eventName = eventName + "Stop";
            }
            this.startStopOptions.Opcode = EventOpcode.Stop;
            this.eventSource.Write(eventName, ref this.startStopOptions, ref this.activityId, ref s_empty, ref data);
        }

        private enum State
        {
            Started,
            Stopped
        }

        /// <summary>
        /// If eventName is non-null then we logged a start event 
        /// </summary>
        private bool StartEventWasFired { get { return eventName != null; } }

        private readonly EventSource eventSource;
        private EventSourceOptions startStopOptions;
        internal Guid activityId;
        // internal Guid relatedActivityId;
        private State state;
        private string? eventName;

        internal static Guid s_empty;
        #endregion
    }
}
