// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin

/* DESIGN NOTES DESIGN NOTES DESIGN NOTES DESIGN NOTES */
// DESIGN NOTES
// Over the years EventSource has become more complex and so it is important to understand
// the basic structure of the code to ensure that it does not grow more complex.
//
// Basic Model
//
// PRINCIPLE: EventSource - ETW decoupling
//
// Conceptually an EventSource is something that takes event logging data from the source methods
// to the EventListener that can subscribe to them.  Note that CONCEPTUALLY EVENTSOURCES DON'T
// KNOW ABOUT ETW!.   The MODEL of the system is that there is a special EventListener which
// we will call the EtwEventListener, that forwards commands from ETW to EventSources and
// listens to EventSources and forwards on those events to ETW.   Thus the model should
// be that you DON'T NEED ETW.
//
// Now in actual practice, EventSource have rather intimate knowledge of ETW and send events
// to it directly, but this can be VIEWED AS AN OPTIMIZATION.
//
// Basic Event Data Flow:
//
// There are two ways for event Data to enter the system
//     1) WriteEvent* and friends.  This is called the 'contract' based approach because
//        you write a method per event which forms a contract that is know at compile time.
//        In this scheme each event is given an EVENTID (small integer), which is its identity
//     2) Write<T> methods.   This is called the 'dynamic' approach because new events
//        can be created on the fly.  Event identity is determined by the event NAME, and these
//        are not quite as efficient at runtime since you have at least a hash table lookup
//        on every event write.
//
// EventSource-EventListener transfer fully supports both ways of writing events (either contract
// based (WriteEvent*) or dynamic (Write<T>)).   Both ways fully support the same set of data
// types.   It is recommended, however, that you use the contract based approach when the event scheme
// is known at compile time (that is whenever possible).  It is more efficient, but more importantly
// it makes the contract very explicit, and centralizes all policy about logging.  These are good
// things.    The Write<T> API is really meant for more ad-hoc cases.
//
// Allowed Data:
//
// Note that EventSource-EventListeners have a conceptual serialization-deserialization that happens
// during the transfer.   In particular object identity is not preserved, some objects are morphed,
// and not all data types are supported.   In particular you can pass
//
// A Valid type to log to an EventSource include
//   * Primitive data types
//   * IEnumerable<T> of valid types T (this include arrays)  (* New for V4.6)
//   * Explicitly Opted in class or struct with public property Getters over Valid types.  (* New for V4.6)
//
// This set of types is roughly a generalization of JSON support (basically primitives, bags, and arrays).
//
// Explicitly allowed structs include (* New for V4.6)
//   * Marked with the EventData attribute
//   * implicitly defined (e.g the C# new {x = 3, y = 5} syntax)
//   * KeyValuePair<K,V>  (thus dictionaries can be passed since they are an IEnumerable of KeyValuePair)
//
// When classes are returned in an EventListener, what is returned is something that implements
// IDictionary<string, T>.  Thus when objects are passed to an EventSource they are transformed
// into a key-value bag (the IDictionary<string, T>) for consumption in the listener.   These
// are obviously NOT the original objects.
//
// ETW serialization formats:
//
// As mentioned, conceptually EventSources send data to EventListeners and there is a conceptual
// copy/morph of that data as described above.   In addition the .NET framework supports a conceptual
// ETWListener that will send the data to the ETW stream.   If you use this feature, the data needs
// to be serialized in a way that ETW supports.  ETW supports the following serialization formats
//
//     1) Manifest Based serialization.
//     2) SelfDescribing serialization (TraceLogging style in the TraceLogging directory)
//
// A key factor is that the Write<T> method, which supports on the fly definition of events, can't
// support the manifest based serialization because the manifest needs the schema of all events
// to be known before any events are emitted.  This implies the following:
//
// If you use Write<T> and the output goes to ETW it will use the SelfDescribing format.
// If you use the EventSource(string) constructor for an eventSource (in which you don't
// create a subclass), the default is also to use Self-Describing serialization.  In addition
// you can use the EventSoruce(EventSourceSettings) constructor to also explicitly specify
// Self-Describing serialization format.   These affect the WriteEvent* APIs going to ETW.
//
// Note that none of this ETW serialization logic affects EventListeners.   Only the ETW listener.
//
// *************************************************************************************
// *** INTERNALS: Event Propagation
//
//   Data enters the system either though
//
// 1) A user defined method in the user defined subclass of EventSource which calls
//     A) A typesafe type specific overload of WriteEvent(ID, ...)  e.g. WriteEvent(ID, string, string)
//           * which calls into the unsafe WriteEventCore(ID COUNT EventData*) WriteEventWithRelatedActivityIdCore()
//     B) The typesafe overload WriteEvent(ID, object[])  which calls the private helper WriteEventVarargs(ID, Guid* object[])
//     C) Directly into the unsafe WriteEventCore(ID, COUNT EventData*) or WriteEventWithRelatedActivityIdCore()
//
//     All event data eventually flows to one of
//        * WriteEventWithRelatedActivityIdCore(ID, Guid*, COUNT, EventData*)
//        * WriteEventVarargs(ID, Guid*, object[])
//
// 2) A call to one of the overloads of Write<T>.   All these overloads end up in
//        * WriteImpl<T>(EventName, Options, Data, Guid*, Guid*)
//
// On output there are the following routines
//    Writing to all listeners that are NOT ETW, we have the following routines
//       * WriteToAllListeners(ID, Guid*, Guid*, COUNT, EventData*)
//       * WriteToAllListeners(ID, Guid*, Guid*, object[])
//       * WriteToAllListeners(NAME, Guid*, Guid*, EventPayload)
//
//       EventPayload is the internal type that implements the IDictionary<string, object> interface
//       The EventListeners will pass back for serialized classes for nested object, but
//       WriteToAllListeners(NAME, Guid*, Guid*, EventPayload) unpacks this and uses the fields as if they
//       were parameters to a method.
//
//       The first two are used for the WriteEvent* case, and the later is used for the Write<T> case.
//
//    Writing to ETW, Manifest Based
//          EventProvider.WriteEvent(EventDescriptor, Guid*, COUNT, EventData*)
//          EventProvider.WriteEvent(EventDescriptor, Guid*, object[])
//    Writing to ETW, Self-Describing format
//          WriteMultiMerge(NAME, Options, Types, EventData*)
//          WriteMultiMerge(NAME, Options, Types, object[])
//          WriteImpl<T> has logic that knows how to serialize (like WriteMultiMerge) but also knows
//             where it will write it to
//
//    All ETW writes eventually call
//      EventWriteTransfer
//         EventProvider.WriteEventRaw   - sets last error
//         EventSource.WriteEventRaw     - Does EventSource exception handling logic
//            WriteMultiMerge
//            WriteImpl<T>
//         EventProvider.WriteEvent(EventDescriptor, Guid*, COUNT, EventData*)
//         EventProvider.WriteEvent(EventDescriptor, Guid*, object[])
//
// Serialization:  We have a bit of a hodge-podge of serializers right now.   Only the one for ETW knows
// how to deal with nested classes or arrays.   I will call this serializer the 'TypeInfo' serializer
// since it is the TraceLoggingTypeInfo structure that knows how to do this.   Effectively for a type you
// can call one of these
//      WriteMetadata - transforms the type T into serialization meta data blob for that type
//      WriteObjectData - transforms an object of T into serialization data blob for that instance
//      GetData - transforms an object of T into its deserialized form suitable for passing to EventListener.
// The first two are used to serialize something for ETW.   The second one is used to transform the object
// for use by the EventListener.    We also have a 'DecodeObject' method that will take a EventData* and
// deserialize to pass to an EventListener, but it only works on primitive types (types supported in version V4.5).
//
// It is an important observation that while EventSource does support users directly calling with EventData*
// blobs, we ONLY support that for the primitive types (V4.5 level support).   Thus while there is a EventData*
// path through the system it is only for some types.  The object[] path is the more general (but less efficient) path.
//
// TODO There is cleanup needed There should be no divergence until WriteEventRaw.
//
// TODO: We should have a single choke point (right now we always have this parallel EventData* and object[] path.   This
// was historical (at one point we tried to pass object directly from EventSoruce to EventListener.  That was always
// fragile and a compatibility headache, but we have finally been forced into the idea that there is always a transformation.
// This allows us to use the EventData* form to be the canonical data format in the low level APIs.  This also gives us the
// opportunity to expose this format to EventListeners in the future.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing
{
    [Conditional("NEEDED_FOR_SOURCE_GENERATOR_ONLY")]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class EventSourceAutoGenerateAttribute : Attribute
    {
    }

    /// <summary>
    /// This class is meant to be inherited by a user-defined event source in order to define a managed
    /// ETW provider.   Please See DESIGN NOTES above for the internal architecture.
    /// The minimal definition of an EventSource simply specifies a number of ETW event methods that
    /// call one of the EventSource.WriteEvent overloads, <see cref="WriteEventCore"/>,
    /// or <see cref="WriteEventWithRelatedActivityIdCore"/> to log them. This functionality
    /// is sufficient for many users.
    /// <para>
    /// To achieve more control over the ETW provider manifest exposed by the event source type, the
    /// [<see cref="EventAttribute"/>] attributes can be specified for the ETW event methods.
    /// </para><para>
    /// For very advanced EventSources, it is possible to intercept the commands being given to the
    /// eventSource and change what filtering is done (see EventListener.EnableEvents and
    /// <see cref="EventListener.DisableEvents"/>) or cause actions to be performed by the eventSource,
    /// e.g. dumping a data structure (see EventSource.SendCommand and
    /// <see cref="OnEventCommand"/>).
    /// </para><para>
    /// The eventSources can be turned on with Windows ETW controllers (e.g. logman), immediately.
    /// It is also possible to control and intercept the data dispatcher programmatically.  See
    /// <see cref="EventListener"/> for more.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This is a minimal definition for a custom event source:
    /// <code>
    /// [EventSource(Name="Samples.Demos.Minimal")]
    /// sealed class MinimalEventSource : EventSource
    /// {
    ///     public static readonly MinimalEventSource Log = new MinimalEventSource();
    ///     public void Load(long ImageBase, string Name) { WriteEvent(1, ImageBase, Name); }
    ///     public void Unload(long ImageBase) { WriteEvent(2, ImageBase); }
    ///     private MinimalEventSource() {}
    /// }
    /// </code>
    /// </remarks>
    // The EnsureDescriptorsInitialized() method might need to access EventSource and its derived type
    // members and the trimmer ensures that these members are preserved.
    [DynamicallyAccessedMembers(ManifestMemberTypes)]
    public partial class EventSource : IDisposable
    {
        // private instance state
        private string m_name = null!;                  // My friendly name (privided in ctor)
        internal int m_id;                              // A small integer that is unique to this instance.
        private Guid m_guid;                            // GUID representing the ETW eventSource to the OS.
        internal volatile Dictionary<int, EventMetadata>? m_eventData; // None per-event data
        private volatile byte[]? m_rawManifest;          // Bytes to send out representing the event schema

        private EventHandler<EventCommandEventArgs>? m_eventCommandExecuted;

        private readonly EventSourceSettings m_config;      // configuration information

        private bool m_eventSourceDisposed;              // has Dispose been called.

        // Enabling bits
        private bool m_eventSourceEnabled;              // am I enabled (any of my events are enabled for any dispatcher)
        internal EventLevel m_level;                    // highest level enabled by any output dispatcher
        internal EventKeywords m_matchAnyKeyword;       // the logical OR of all levels enabled by any output dispatcher (zero is a special case) meaning 'all keywords'

        // Dispatching state
        internal volatile EventDispatcher? m_Dispatchers;    // Linked list of code:EventDispatchers we write the data to (we also do ETW specially)
        private volatile OverrideEventProvider m_etwProvider = null!;   // This hooks up ETW commands to our 'OnEventCommand' callback
#if FEATURE_PERFTRACING
        private object? m_createEventLock;
        private IntPtr m_writeEventStringEventHandle = IntPtr.Zero;
        private volatile OverrideEventProvider m_eventPipeProvider = null!;
#endif
        private bool m_completelyInited;                // The EventSource constructor has returned without exception.
        private Exception? m_constructionException;      // If there was an exception construction, this is it
        private byte m_outOfBandMessageCount;           // The number of out of band messages sent (we throttle them
        private EventCommandEventArgs? m_deferredCommands; // If we get commands before we are fully we store them here and run the when we are fully inited.

        private string[]? m_traits;                      // Used to implement GetTraits

        [ThreadStatic]
        private static byte m_EventSourceExceptionRecurenceCount; // current recursion count inside ThrowEventSourceException

        internal volatile ulong[]? m_channelData;

        // We use a single instance of ActivityTracker for all EventSources instances to allow correlation between multiple event providers.
        // We have m_activityTracker field simply because instance field is more efficient than static field fetch.
        private ActivityTracker m_activityTracker = null!;
        internal const string ActivityStartSuffix = "Start";
        internal const string ActivityStopSuffix = "Stop";

        // This switch controls an opt-in, off-by-default mechanism for allowing multiple EventSources to have the same
        // name and by extension GUID. This is not considered a mainline scenario and is explicitly intended as a release
        // valve for users that make heavy use of AssemblyLoadContext and experience exceptions from EventSource.
        // This does not solve any issues that might arise from this configuration. For instance:
        //
        // * If multiple manifest-mode EventSources have the same name/GUID, it is ambiguous which manifest should be used by an ETW parser.
        //   This can result in events being incorrectly parse. The data will still be there, but EventTrace (or other libraries) won't
        //   know how to parse it.
        // * Potential issues in parsing self-describing EventSources that use the same name/GUID, event name, and payload type from the same AssemblyLoadContext
        //   but have different event IDs set.
        //
        // Most users should not turn this on.
        internal const string DuplicateSourceNamesSwitch = "System.Diagnostics.Tracing.EventSource.AllowDuplicateSourceNames";
        private static readonly bool AllowDuplicateSourceNames = AppContext.TryGetSwitch(DuplicateSourceNamesSwitch, out bool isEnabled) ? isEnabled : false;

        [FeatureSwitchDefinition("System.Diagnostics.Tracing.EventSource.IsSupported")]
        internal static bool IsSupported { get; } = InitializeIsSupported();

        private static bool InitializeIsSupported() =>
            AppContext.TryGetSwitch("System.Diagnostics.Tracing.EventSource.IsSupported", out bool isSupported) ? isSupported : true;

        [FeatureSwitchDefinition("System.Diagnostics.Metrics.Meter.IsSupported")]
        internal static bool IsMeterSupported { get; } = InitializeIsMeterSupported();

        private static bool InitializeIsMeterSupported() =>
            AppContext.TryGetSwitch("System.Diagnostics.Metrics.Meter.IsSupported", out bool isSupported) ? isSupported : true;

#if FEATURE_EVENTSOURCE_XPLAT
#pragma warning disable CA1823 // field is used to keep listener alive
        private static readonly EventListener? persistent_Xplat_Listener = IsSupported ? XplatEventLogger.InitializePersistentListener() : null;
#pragma warning restore CA1823
#endif //FEATURE_EVENTSOURCE_XPLAT

        /// <summary>
        /// The human-friendly name of the eventSource.  It defaults to the simple name of the class
        /// </summary>
        public string Name => m_name;
        /// <summary>
        /// Every eventSource is assigned a GUID to uniquely identify it to the system.
        /// </summary>
        public Guid Guid => m_guid;

        /// <summary>
        /// Returns true if the eventSource has been enabled at all. This is the preferred test
        /// to be performed before a relatively expensive EventSource operation.
        /// </summary>
        public bool IsEnabled()
        {
            return m_eventSourceEnabled;
        }

        /// <summary>
        /// Returns true if events with greater than or equal 'level' and have one of 'keywords' set are enabled.
        ///
        /// Note that the result of this function is only an approximation on whether a particular
        /// event is active or not. It is only meant to be used as way of avoiding expensive
        /// computation for logging when logging is not on, therefore it sometimes returns false
        /// positives (but is always accurate when returning false).  EventSources are free to
        /// have additional filtering.
        /// </summary>
        public bool IsEnabled(EventLevel level, EventKeywords keywords)
        {
            return IsEnabled(level, keywords, EventChannel.None);
        }

        /// <summary>
        /// Returns true if events with greater than or equal 'level' and have one of 'keywords' set are enabled, or
        /// if 'keywords' specifies a channel bit for a channel that is enabled.
        ///
        /// Note that the result of this function only an approximation on whether a particular
        /// event is active or not. It is only meant to be used as way of avoiding expensive
        /// computation for logging when logging is not on, therefore it sometimes returns false
        /// positives (but is always accurate when returning false).  EventSources are free to
        /// have additional filtering.
        /// </summary>
        public bool IsEnabled(EventLevel level, EventKeywords keywords, EventChannel channel)
        {
            if (!IsEnabled())
                return false;

            if (!IsEnabledCommon(m_eventSourceEnabled, m_level, m_matchAnyKeyword, level, keywords, channel))
                return false;

            return true;
        }

        /// <summary>
        /// Returns the settings for the event source instance
        /// </summary>
        public EventSourceSettings Settings => m_config;

        // Manifest support
        /// <summary>
        /// Returns the GUID that uniquely identifies the eventSource defined by 'eventSourceType'.
        /// This API allows you to compute this without actually creating an instance of the EventSource.
        /// It only needs to reflect over the type.
        /// </summary>
        public static Guid GetGuid(Type eventSourceType)
        {
            ArgumentNullException.ThrowIfNull(eventSourceType);

            EventSourceAttribute? attrib = (EventSourceAttribute?)GetCustomAttributeHelper(eventSourceType, typeof(EventSourceAttribute));
            string name = eventSourceType.Name;
            if (attrib != null)
            {
                if (attrib.Guid != null)
                {
                    if (Guid.TryParse(attrib.Guid, out Guid g))
                        return g;
                }

                if (attrib.Name != null)
                    name = attrib.Name;
            }

            if (name == null)
            {
                throw new ArgumentException(SR.Argument_InvalidTypeName, nameof(eventSourceType));
            }
            return GenerateGuidFromName(name.ToUpperInvariant());       // Make it case insensitive.
        }
        /// <summary>
        /// Returns the official ETW Provider name for the eventSource defined by 'eventSourceType'.
        /// This API allows you to compute this without actually creating an instance of the EventSource.
        /// It only needs to reflect over the type.
        /// </summary>
        public static string GetName(Type eventSourceType)
        {
            return GetName(eventSourceType, EventManifestOptions.None);
        }

        private const DynamicallyAccessedMemberTypes ManifestMemberTypes =
            DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.PublicNestedTypes;

        /// <summary>
        /// Returns a string of the XML manifest associated with the eventSourceType. The scheme for this XML is
        /// documented at in EventManifest Schema https://learn.microsoft.com/windows/desktop/WES/eventmanifestschema-schema.
        /// This is the preferred way of generating a manifest to be embedded in the ETW stream as it is fast and
        /// the fact that it only includes localized entries for the current UI culture is an acceptable tradeoff.
        /// </summary>
        /// <param name="eventSourceType">The type of the event source class for which the manifest is generated</param>
        /// <param name="assemblyPathToIncludeInManifest">The manifest XML fragment contains the string name of the DLL name in
        /// which it is embedded.  This parameter specifies what name will be used</param>
        /// <returns>The XML data string</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2114:ReflectionToDynamicallyAccessedMembers",
            Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                            "has dynamically accessed members requirements, but EnsureDescriptorsInitialized does not " +
                            "access this member and is safe to call.")]
        public static string? GenerateManifest(
            [DynamicallyAccessedMembers(ManifestMemberTypes)]
            Type eventSourceType,
            string? assemblyPathToIncludeInManifest)
        {
            return GenerateManifest(eventSourceType, assemblyPathToIncludeInManifest, EventManifestOptions.None);
        }
        /// <summary>
        /// Returns a string of the XML manifest associated with the eventSourceType. The scheme for this XML is
        /// documented at in EventManifest Schema https://learn.microsoft.com/windows/desktop/WES/eventmanifestschema-schema.
        /// Pass EventManifestOptions.AllCultures when generating a manifest to be registered on the machine. This
        /// ensures that the entries in the event log will be "optimally" localized.
        /// </summary>
        /// <param name="eventSourceType">The type of the event source class for which the manifest is generated</param>
        /// <param name="assemblyPathToIncludeInManifest">The manifest XML fragment contains the string name of the DLL name in
        /// which it is embedded.  This parameter specifies what name will be used</param>
        /// <param name="flags">The flags to customize manifest generation. If flags has bit OnlyIfNeededForRegistration specified
        /// this returns null when the eventSourceType does not require explicit registration</param>
        /// <returns>The XML data string or null</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2114:ReflectionToDynamicallyAccessedMembers",
            Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                            "has dynamically accessed members requirements, but EnsureDescriptorsInitialized does not " +
                            "access this member and is safe to call.")]
        public static string? GenerateManifest(
            [DynamicallyAccessedMembers(ManifestMemberTypes)]
            Type eventSourceType,
            string? assemblyPathToIncludeInManifest,
            EventManifestOptions flags)
        {
            if (!IsSupported)
            {
                return null;
            }

            ArgumentNullException.ThrowIfNull(eventSourceType);

            byte[]? manifestBytes = CreateManifestAndDescriptors(eventSourceType, assemblyPathToIncludeInManifest, null, flags);
            return (manifestBytes == null) ? null : Encoding.UTF8.GetString(manifestBytes, 0, manifestBytes.Length);
        }

        // EventListener support
        /// <summary>
        /// returns a list (IEnumerable) of all sources in the appdomain).  EventListeners typically need this.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<EventSource> GetSources()
        {
            if (!IsSupported)
            {
                return Array.Empty<EventSource>();
            }

            var ret = new List<EventSource>();
            lock (EventListener.EventListenersLock)
            {
                Debug.Assert(EventListener.s_EventSources != null);

                foreach (WeakReference<EventSource> eventSourceRef in EventListener.s_EventSources)
                {
                    if (eventSourceRef.TryGetTarget(out EventSource? eventSource) && !eventSource.IsDisposed)
                        ret.Add(eventSource);
                }
            }
            return ret;
        }

        /// <summary>
        /// Send a command to a particular EventSource identified by 'eventSource'.
        /// Calling this routine simply forwards the command to the EventSource.OnEventCommand
        /// callback.  What the EventSource does with the command and its arguments are from
        /// that point EventSource-specific.
        /// </summary>
        /// <param name="eventSource">The instance of EventSource to send the command to</param>
        /// <param name="command">A positive user-defined EventCommand, or EventCommand.SendManifest</param>
        /// <param name="commandArguments">A set of (name-argument, value-argument) pairs associated with the command</param>
        public static void SendCommand(EventSource eventSource, EventCommand command, IDictionary<string, string?>? commandArguments)
        {
            if (!IsSupported)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(eventSource);

            // User-defined EventCommands should not conflict with the reserved commands.
            if ((int)command <= (int)EventCommand.Update && (int)command != (int)EventCommand.SendManifest)
            {
                throw new ArgumentException(SR.EventSource_InvalidCommand, nameof(command));
            }

            eventSource.SendCommand(null, EventProviderType.ETW, 0, command, true, EventLevel.LogAlways, EventKeywords.None, commandArguments);
        }

        // Error APIs.  (We don't throw by default, but you can probe for status)
        /// <summary>
        /// Because
        ///
        ///     1) Logging is often optional and thus should not generate fatal errors (exceptions)
        ///     2) EventSources are often initialized in class constructors (which propagate exceptions poorly)
        ///
        /// The event source constructor does not throw exceptions.  Instead we remember any exception that
        /// was generated (it is also logged to Trace.WriteLine).
        /// </summary>
        public Exception? ConstructionException => m_constructionException;

        /// <summary>
        /// EventSources can have arbitrary string key-value pairs associated with them called Traits.
        /// These traits are not interpreted by the EventSource but may be interpreted by EventListeners
        /// (e.g. like the built in ETW listener).   These traits are specified at EventSource
        /// construction time and can be retrieved by using this GetTrait API.
        /// </summary>
        /// <param name="key">The key to look up in the set of key-value pairs passed to the EventSource constructor</param>
        /// <returns>The value string associated with key.  Will return null if there is no such key.</returns>
        public string? GetTrait(string key)
        {
            if (m_traits != null)
            {
                for (int i = 0; i < m_traits.Length - 1; i += 2)
                {
                    if (m_traits[i] == key)
                        return m_traits[i + 1];
                }
            }

            return null;
        }

        /// <summary>
        /// Displays the name and GUID for the eventSource for debugging purposes.
        /// </summary>
        public override string ToString()
        {
            if (!IsSupported)
                return base.ToString()!;

            return SR.Format(SR.EventSource_ToString, Name, Guid);
        }

        /// <summary>
        /// Fires when a Command (e.g. Enable) comes from a an EventListener.
        /// </summary>
        public event EventHandler<EventCommandEventArgs>? EventCommandExecuted
        {
            add
            {
                if (value == null)
                    return;

                m_eventCommandExecuted += value;

                if (m_completelyInited)
                {
                    // If we have an EventHandler<EventCommandEventArgs> attached to the EventSource before the first command arrives
                    // It should get a chance to handle the deferred commands.
                    EventCommandEventArgs? deferredCommands = m_deferredCommands;
                    while (deferredCommands != null)
                    {
                        value(this, deferredCommands);
                        deferredCommands = deferredCommands.nextCommand;
                    }
                }
            }
            remove
            {
                m_eventCommandExecuted -= value;
            }
        }

#region ActivityID

        /// <summary>
        /// When a thread starts work that is on behalf of 'something else' (typically another
        /// thread or network request) it should mark the thread as working on that other work.
        /// This API marks the current thread as working on activity 'activityID'. This API
        /// should be used when the caller knows the thread's current activity (the one being
        /// overwritten) has completed. Otherwise, callers should prefer the overload that
        /// return the oldActivityThatWillContinue (below).
        ///
        /// All events created with the EventSource on this thread are also tagged with the
        /// activity ID of the thread.
        ///
        /// It is common, and good practice after setting the thread to an activity to log an event
        /// with a 'start' opcode to indicate that precise time/thread where the new activity
        /// started.
        /// </summary>
        /// <param name="activityId">A Guid that represents the new activity with which to mark
        /// the current thread</param>
        public static void SetCurrentThreadActivityId(Guid activityId)
        {
            if (!IsSupported)
            {
                return;
            }

            TplEventSource.Log?.SetActivityId(activityId);

            // We ignore errors to keep with the convention that EventSources do not throw errors.
            // Note we can't access m_throwOnWrites because this is a static method.
#if FEATURE_PERFTRACING
            // Set the activity id via EventPipe.
            EventPipeEventProvider.EventActivityIdControl(
                Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID,
                ref activityId);
#endif // FEATURE_PERFTRACING
#if TARGET_WINDOWS
            // Set the activity id via ETW.
            Interop.Advapi32.EventActivityIdControl(
                Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID,
                ref activityId);
#endif // TARGET_WINDOWS
        }

        /// <summary>
        /// Retrieves the ETW activity ID associated with the current thread.
        /// </summary>
        public static Guid CurrentThreadActivityId
        {
            get
            {
                if (!IsSupported)
                {
                    return default;
                }

                // We ignore errors to keep with the convention that EventSources do not throw
                // errors. Note we can't access m_throwOnWrites because this is a static method.
                Guid retVal = default;
#if TARGET_WINDOWS
                Interop.Advapi32.EventActivityIdControl(
                    Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref retVal);
#elif FEATURE_PERFTRACING
                EventPipeEventProvider.EventActivityIdControl(
                    Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_GET_ID,
                    ref retVal);
#endif // TARGET_WINDOWS
                return retVal;
            }
        }

        /// <summary>
        /// When a thread starts work that is on behalf of 'something else' (typically another
        /// thread or network request) it should mark the thread as working on that other work.
        /// This API marks the current thread as working on activity 'activityID'. It returns
        /// whatever activity the thread was previously marked with. There is a convention that
        /// callers can assume that callees restore this activity mark before the callee returns.
        /// To encourage this, this API returns the old activity, so that it can be restored later.
        ///
        /// All events created with the EventSource on this thread are also tagged with the
        /// activity ID of the thread.
        ///
        /// It is common, and good practice after setting the thread to an activity to log an event
        /// with a 'start' opcode to indicate that precise time/thread where the new activity
        /// started.
        /// </summary>
        /// <param name="activityId">A Guid that represents the new activity with which to mark
        /// the current thread</param>
        /// <param name="oldActivityThatWillContinue">The Guid that represents the current activity
        /// which will continue at some point in the future, on the current thread</param>
        public static void SetCurrentThreadActivityId(Guid activityId, out Guid oldActivityThatWillContinue)
        {
            if (!IsSupported)
            {
                oldActivityThatWillContinue = default;
                return;
            }

            oldActivityThatWillContinue = activityId;
            // We ignore errors to keep with the convention that EventSources do not throw errors.
            // Note we can't access m_throwOnWrites because this is a static method.

#if FEATURE_PERFTRACING && TARGET_WINDOWS
            EventPipeEventProvider.EventActivityIdControl(
                Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_SET_ID,
                    ref oldActivityThatWillContinue);
#elif FEATURE_PERFTRACING
            EventPipeEventProvider.EventActivityIdControl(
                Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_GET_SET_ID,
                    ref oldActivityThatWillContinue);
#endif // FEATURE_PERFTRACING && TARGET_WINDOWS

#if TARGET_WINDOWS
            Interop.Advapi32.EventActivityIdControl(
                Interop.Advapi32.ActivityControl.EVENT_ACTIVITY_CTRL_GET_SET_ID,
                    ref oldActivityThatWillContinue);
#endif // TARGET_WINDOWS

            // We don't call the activityDying callback here because the caller has declared that
            // it is not dying.
            TplEventSource.Log?.SetActivityId(activityId);
        }
#endregion

#region protected
        /// <summary>
        /// This is the constructor that most users will use to create their eventSource.   It takes
        /// no parameters.  The ETW provider name and GUID of the EventSource are determined by the EventSource
        /// custom attribute (so you can determine these things declaratively).   If the GUID for the eventSource
        /// is not specified in the EventSourceAttribute (recommended), it is Generated by hashing the name.
        /// If the ETW provider name of the EventSource is not given, the name of the EventSource class is used as
        /// the ETW provider name.
        /// </summary>
        protected EventSource()
            : this(EventSourceSettings.EtwManifestEventFormat)
        {
        }

        /// <summary>
        /// By default calling the 'WriteEvent' methods do NOT throw on errors (they silently discard the event).
        /// This is because in most cases users assume logging is not 'precious' and do NOT wish to have logging failures
        /// crash the program. However for those applications where logging is 'precious' and if it fails the caller
        /// wishes to react, setting 'throwOnEventWriteErrors' will cause an exception to be thrown if WriteEvent
        /// fails. Note the fact that EventWrite succeeds does not necessarily mean that the event reached its destination
        /// only that operation of writing it did not fail. These EventSources will not generate self-describing ETW events.
        ///
        /// For compatibility only use the EventSourceSettings.ThrowOnEventWriteErrors flag instead.
        /// </summary>
        // [Obsolete("Use the EventSource(EventSourceSettings) overload")]
        protected EventSource(bool throwOnEventWriteErrors)
            : this(EventSourceSettings.EtwManifestEventFormat | (throwOnEventWriteErrors ? EventSourceSettings.ThrowOnEventWriteErrors : 0))
        { }

        /// <summary>
        /// Construct an EventSource with additional non-default settings (see EventSourceSettings for more)
        /// </summary>
        protected EventSource(EventSourceSettings settings) : this(settings, null) { }

        /// <summary>
        /// Construct an EventSource with additional non-default settings.
        ///
        /// Also specify a list of key-value pairs called traits (you must pass an even number of strings).
        /// The first string is the key and the second is the value.   These are not interpreted by EventSource
        /// itself but may be interpreted the listeners.  Can be fetched with GetTrait(string).
        /// </summary>
        /// <param name="settings">See EventSourceSettings for more.</param>
        /// <param name="traits">A collection of key-value strings (must be an even number).</param>
        protected EventSource(EventSourceSettings settings, params string[]? traits)
        {
            if (IsSupported)
            {
#if FEATURE_PERFTRACING
                m_eventHandleTable = new TraceLoggingEventHandleTable();
#endif
                m_config = ValidateSettings(settings);

                Type myType = this.GetType();
                Guid eventSourceGuid = GetGuid(myType);
                string eventSourceName = GetName(myType);

                Initialize(eventSourceGuid, eventSourceName, traits);
            }
        }

#if FEATURE_PERFTRACING
        // Generate the serialized blobs that describe events for all strongly typed events (that is events that define strongly
        // typed event methods. Dynamically defined events (that use Write) hare defined on the fly and are handled elsewhere.
        private unsafe void DefineEventPipeEvents()
        {
            // If the EventSource is set to emit all events as TraceLogging events, skip this initialization.
            // Events will be defined when they are emitted for the first time.
            if (SelfDescribingEvents)
            {
                return;
            }

            Debug.Assert(m_eventData != null);
            Debug.Assert(m_eventPipeProvider != null);
            foreach (int metaKey in m_eventData.Keys)
            {
                ref EventMetadata eventMetadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, metaKey);
                uint eventID = (uint)eventMetadata.Descriptor.EventId;
                if (eventID == 0)
                    continue;

                byte[]? metadata = EventPipeMetadataGenerator.Instance.GenerateEventMetadata(eventMetadata);
                uint metadataLength = (metadata != null) ? (uint)metadata.Length : 0;

                string eventName = eventMetadata.Name;
                long keywords = eventMetadata.Descriptor.Keywords;
                uint eventVersion = eventMetadata.Descriptor.Version;
                uint level = eventMetadata.Descriptor.Level;

                fixed (byte *pMetadata = metadata)
                {
                    IntPtr eventHandle = m_eventPipeProvider._eventProvider.DefineEventHandle(
                        eventID,
                        eventName,
                        keywords,
                        eventVersion,
                        level,
                        pMetadata,
                        metadataLength);

                    Debug.Assert(eventHandle != IntPtr.Zero);
                    eventMetadata.EventHandle = eventHandle;
                }
            }
        }
#endif

        /// <summary>
        /// This method is called when the eventSource is updated by the controller.
        /// </summary>
        protected virtual void OnEventCommand(EventCommandEventArgs command) { }

#pragma warning disable 1591
        // optimized for common signatures (no args)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId)
        {
            WriteEventCore(eventId, 0, null);
        }

        // optimized for common signatures (ints)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, int arg1)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[1];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[0].Reserved = 0;
                WriteEventCore(eventId, 1, descrs);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, int arg1, int arg2)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[2];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[0].Reserved = 0;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                descrs[1].Reserved = 0;
                WriteEventCore(eventId, 2, descrs);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 4;
                descrs[0].Reserved = 0;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 4;
                descrs[1].Reserved = 0;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 4;
                descrs[2].Reserved = 0;
                WriteEventCore(eventId, 3, descrs);
            }
        }

        // optimized for common signatures (longs)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, long arg1)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[1];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[0].Reserved = 0;
                WriteEventCore(eventId, 1, descrs);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, long arg1, long arg2)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[2];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[0].Reserved = 0;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 8;
                descrs[1].Reserved = 0;
                WriteEventCore(eventId, 2, descrs);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, long arg1, long arg2, long arg3)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[0].Reserved = 0;
                descrs[1].DataPointer = (IntPtr)(&arg2);
                descrs[1].Size = 8;
                descrs[1].Reserved = 0;
                descrs[2].DataPointer = (IntPtr)(&arg3);
                descrs[2].Size = 8;
                descrs[2].Reserved = 0;
                WriteEventCore(eventId, 3, descrs);
            }
        }

        // optimized for common signatures (strings)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, string? arg1)
        {
            if (IsEnabled())
            {
                arg1 ??= "";
                fixed (char* string1Bytes = arg1)
                {
                    EventData* descrs = stackalloc EventData[1];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[0].Reserved = 0;
                    WriteEventCore(eventId, 1, descrs);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, string? arg1, string? arg2)
        {
            if (IsEnabled())
            {
                arg1 ??= "";
                arg2 ??= "";
                fixed (char* string1Bytes = arg1)
                fixed (char* string2Bytes = arg2)
                {
                    EventData* descrs = stackalloc EventData[2];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    descrs[1].Reserved = 0;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3)
        {
            if (IsEnabled())
            {
                arg1 ??= "";
                arg2 ??= "";
                arg3 ??= "";
                fixed (char* string1Bytes = arg1)
                fixed (char* string2Bytes = arg2)
                fixed (char* string3Bytes = arg3)
                {
                    EventData* descrs = stackalloc EventData[3];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    descrs[1].Reserved = 0;
                    descrs[2].DataPointer = (IntPtr)string3Bytes;
                    descrs[2].Size = ((arg3.Length + 1) * 2);
                    descrs[2].Reserved = 0;
                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }

        // optimized for common signatures (string and ints)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, string? arg1, int arg2)
        {
            if (IsEnabled())
            {
                arg1 ??= "";
                fixed (char* string1Bytes = arg1)
                {
                    EventData* descrs = stackalloc EventData[2];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[1].Reserved = 0;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, string? arg1, int arg2, int arg3)
        {
            if (IsEnabled())
            {
                arg1 ??= "";
                fixed (char* string1Bytes = arg1)
                {
                    EventData* descrs = stackalloc EventData[3];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[1].Reserved = 0;
                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = 4;
                    descrs[2].Reserved = 0;
                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }

        // optimized for common signatures (string and longs)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, string? arg1, long arg2)
        {
            if (IsEnabled())
            {
                arg1 ??= "";
                fixed (char* string1Bytes = arg1)
                {
                    EventData* descrs = stackalloc EventData[2];
                    descrs[0].DataPointer = (IntPtr)string1Bytes;
                    descrs[0].Size = ((arg1.Length + 1) * 2);
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 8;
                    descrs[1].Reserved = 0;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        // optimized for common signatures (long and string)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, long arg1, string? arg2)
        {
            if (IsEnabled())
            {
                arg2 ??= "";
                fixed (char* string2Bytes = arg2)
                {
                    EventData* descrs = stackalloc EventData[2];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 8;
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    descrs[1].Reserved = 0;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        // optimized for common signatures (int and string)
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, int arg1, string? arg2)
        {
            if (IsEnabled())
            {
                arg2 ??= "";
                fixed (char* string2Bytes = arg2)
                {
                    EventData* descrs = stackalloc EventData[2];
                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 4;
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)string2Bytes;
                    descrs[1].Size = ((arg2.Length + 1) * 2);
                    descrs[1].Reserved = 0;
                    WriteEventCore(eventId, 2, descrs);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, byte[]? arg1)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[2];
                if (arg1 == null || arg1.Length == 0)
                {
                    int blobSize = 0;
                    descrs[0].DataPointer = (IntPtr)(&blobSize);
                    descrs[0].Size = 4;
                    descrs[0].Reserved = 0;
                    descrs[1].DataPointer = (IntPtr)(&blobSize); // valid address instead of empty content
                    descrs[1].Size = 0;
                    descrs[1].Reserved = 0;
                    WriteEventCore(eventId, 2, descrs);
                }
                else
                {
                    int blobSize = arg1.Length;
                    fixed (byte* blob = &arg1[0])
                    {
                        descrs[0].DataPointer = (IntPtr)(&blobSize);
                        descrs[0].Size = 4;
                        descrs[0].Reserved = 0;
                        descrs[1].DataPointer = (IntPtr)blob;
                        descrs[1].Size = blobSize;
                        descrs[1].Reserved = 0;
                        WriteEventCore(eventId, 2, descrs);
                    }
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected unsafe void WriteEvent(int eventId, long arg1, byte[]? arg2)
        {
            if (IsEnabled())
            {
                EventData* descrs = stackalloc EventData[3];
                descrs[0].DataPointer = (IntPtr)(&arg1);
                descrs[0].Size = 8;
                descrs[0].Reserved = 0;
                if (arg2 == null || arg2.Length == 0)
                {
                    int blobSize = 0;
                    descrs[1].DataPointer = (IntPtr)(&blobSize);
                    descrs[1].Size = 4;
                    descrs[1].Reserved = 0;
                    descrs[2].DataPointer = (IntPtr)(&blobSize); // valid address instead of empty contents
                    descrs[2].Size = 0;
                    descrs[2].Reserved = 0;
                    WriteEventCore(eventId, 3, descrs);
                }
                else
                {
                    int blobSize = arg2.Length;
                    fixed (byte* blob = &arg2[0])
                    {
                        descrs[1].DataPointer = (IntPtr)(&blobSize);
                        descrs[1].Size = 4;
                        descrs[1].Reserved = 0;
                        descrs[2].DataPointer = (IntPtr)blob;
                        descrs[2].Size = blobSize;
                        descrs[2].Reserved = 0;
                        WriteEventCore(eventId, 3, descrs);
                    }
                }
            }
        }

        // Returns the object as a IntPtr - safe when only used for logging
        internal static unsafe nint ObjectIDForEvents(object? o) => *(nint*)&o;

#pragma warning restore 1591

        /// <summary>
        /// A wrapper type for separating primitive types (int, long, string, etc) from other types
        /// in the EventSource API. This type shouldn't be used directly, but just as implicit conversions
        /// when using the WriteEvent API.
        /// </summary>
        public readonly struct EventSourcePrimitive
        {
            internal readonly object? Value;

            private EventSourcePrimitive(object? value)
            {
                Value = value;
            }
            public static implicit operator EventSourcePrimitive(bool value) => new(value);
            public static implicit operator EventSourcePrimitive(byte value) => new(value);
            public static implicit operator EventSourcePrimitive(short value) => new(value);
            public static implicit operator EventSourcePrimitive(int value) => new(value);
            public static implicit operator EventSourcePrimitive(long value) => new(value);

            [CLSCompliant(false)]
            public static implicit operator EventSourcePrimitive(sbyte value) => new(value);
            [CLSCompliant(false)]
            public static implicit operator EventSourcePrimitive(ushort value) => new(value);
            [CLSCompliant(false)]
            public static implicit operator EventSourcePrimitive(uint value) => new(value);
            [CLSCompliant(false)]
            public static implicit operator EventSourcePrimitive(ulong value) => new(value);
            [CLSCompliant(false)]
            // Added to prevent going through the nuint -> ulong conversion
            public static implicit operator EventSourcePrimitive(nuint value) => new(value);

            public static implicit operator EventSourcePrimitive(float value) => new(value);
            public static implicit operator EventSourcePrimitive(double value) => new(value);
            public static implicit operator EventSourcePrimitive(decimal value) => new(value);

            public static implicit operator EventSourcePrimitive(string? value) => new(value);
            public static implicit operator EventSourcePrimitive(byte[]? value) => new(value);

            public static implicit operator EventSourcePrimitive(Guid value) => new(value);
            public static implicit operator EventSourcePrimitive(DateTime value) => new(value);
            public static implicit operator EventSourcePrimitive(nint value) => new(value);
            public static implicit operator EventSourcePrimitive(char value) => new(value);

            public static implicit operator EventSourcePrimitive(Enum value) => new(value);
        }

        /// <summary>
        /// Used to construct the data structure to be passed to the native ETW APIs - EventWrite and EventWriteTransfer.
        /// </summary>
        protected internal struct EventData
        {
            /// <summary>
            /// Address where the one argument lives (if this points to managed memory you must ensure the
            /// managed object is pinned.
            /// </summary>
            public unsafe IntPtr DataPointer
            {
                get => (IntPtr)(void*)m_Ptr;
                set => m_Ptr = unchecked((ulong)(void*)value);
            }

            /// <summary>
            /// Size of the argument referenced by DataPointer
            /// </summary>
            public int Size
            {
                get => m_Size;
                set => m_Size = value;
            }

            /// <summary>
            /// Reserved by ETW.  This property is present to ensure that we can zero it
            /// since System.Private.CoreLib uses are not zero'd.
            /// </summary>
            internal int Reserved
            {
                get => m_Reserved;
                set => m_Reserved = value;
            }

#region private
            /// <summary>
            /// Initializes the members of this EventData object to point at a previously-pinned
            /// tracelogging-compatible metadata blob.
            /// </summary>
            /// <param name="pointer">Pinned tracelogging-compatible metadata blob.</param>
            /// <param name="size">The size of the metadata blob.</param>
            /// <param name="reserved">Value for reserved: 2 for per-provider metadata, 1 for per-event metadata</param>
            internal unsafe void SetMetadata(byte* pointer, int size, int reserved)
            {
                this.m_Ptr = (ulong)pointer;
                this.m_Size = size;
                this.m_Reserved = reserved; // Mark this descriptor as containing tracelogging-compatible metadata.
            }

            // Important, we pass this structure directly to the Win32 EventWrite API, so this structure must
            // be laid out exactly the way EventWrite wants it.
            internal ulong m_Ptr;
            internal int m_Size;
#pragma warning disable 0649
            internal int m_Reserved;       // Used to pad the size to match the Win32 API
#pragma warning restore 0649
#endregion
        }

        /// <summary>
        /// This routine allows you to create efficient WriteEvent helpers, however the code that you use to
        /// do this, while straightforward, is unsafe.
        /// </summary>
        /// <remarks>
        /// <code>
        ///    protected unsafe void WriteEvent(int eventId, string arg1, long arg2)
        ///    {
        ///        if (IsEnabled())
        ///        {
        ///            arg2 ??= "";
        ///            fixed (char* string2Bytes = arg2)
        ///            {
        ///                EventSource.EventData* descrs = stackalloc EventSource.EventData[2];
        ///                descrs[0].DataPointer = (IntPtr)(&amp;arg1);
        ///                descrs[0].Size = 8;
        ///                descrs[0].Reserved = 0;
        ///                descrs[1].DataPointer = (IntPtr)string2Bytes;
        ///                descrs[1].Size = ((arg2.Length + 1) * 2);
        ///                descrs[1].Reserved = 0;
        ///                WriteEventCore(eventId, 2, descrs);
        ///            }
        ///        }
        ///    }
        /// </code>
        /// </remarks>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                    Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                    "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
        [RequiresUnreferencedCode(EventSourceRequiresUnreferenceMessage)]
        [CLSCompliant(false)]
        protected unsafe void WriteEventCore(int eventId, int eventDataCount, EventData* data)
        {
            WriteEventWithRelatedActivityIdCore(eventId, null, eventDataCount, data);
        }

        /// <summary>
        /// This routine allows you to create efficient WriteEventWithRelatedActivityId helpers, however the code
        /// that you use to do this, while straightforward, is unsafe. The only difference from
        /// <see cref="WriteEventCore"/> is that you pass the relatedActivityId from caller through to this API
        /// </summary>
        /// <remarks>
        /// <code>
        ///    protected unsafe void WriteEventWithRelatedActivityId(int eventId, Guid relatedActivityId, string arg1, long arg2)
        ///    {
        ///        if (IsEnabled())
        ///        {
        ///            arg2 ??= "";
        ///            fixed (char* string2Bytes = arg2)
        ///            {
        ///                EventSource.EventData* descrs = stackalloc EventSource.EventData[2];
        ///                descrs[0].DataPointer = (IntPtr)(&amp;arg1);
        ///                descrs[0].Size = 8;
        ///                descrs[1].DataPointer = (IntPtr)string2Bytes;
        ///                descrs[1].Size = ((arg2.Length + 1) * 2);
        ///                WriteEventWithRelatedActivityIdCore(eventId, relatedActivityId, 2, descrs);
        ///            }
        ///        }
        ///    }
        /// </code>
        /// </remarks>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                    Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                    "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
        [RequiresUnreferencedCode(EventSourceRequiresUnreferenceMessage)]
        [CLSCompliant(false)]
        protected unsafe void WriteEventWithRelatedActivityIdCore(int eventId, Guid* relatedActivityId, int eventDataCount, EventData* data)
        {
            if (IsEnabled())
            {
                Debug.Assert(m_eventData != null);  // You must have initialized this if you enabled the source.
                try
                {
                    ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventId);

                    EventOpcode opcode = (EventOpcode)metadata.Descriptor.Opcode;
                    Guid* pActivityId = null;
                    Guid activityId = Guid.Empty;
                    Guid relActivityId = Guid.Empty;

                    if (opcode != EventOpcode.Info && relatedActivityId == null &&
                       ((metadata.ActivityOptions & EventActivityOptions.Disable) == 0))
                    {
                        if (opcode == EventOpcode.Start)
                        {
                            m_activityTracker.OnStart(m_name, metadata.Name, metadata.Descriptor.Task, ref activityId, ref relActivityId, metadata.ActivityOptions);
                        }
                        else if (opcode == EventOpcode.Stop)
                        {
                            m_activityTracker.OnStop(m_name, metadata.Name, metadata.Descriptor.Task, ref activityId);
                        }

                        if (activityId != Guid.Empty)
                            pActivityId = &activityId;
                        if (relActivityId != Guid.Empty)
                            relatedActivityId = &relActivityId;
                    }

                    if (!SelfDescribingEvents)
                    {
                        if (metadata.EnabledForETW && !m_etwProvider.WriteEvent(ref metadata.Descriptor, metadata.EventHandle, pActivityId, relatedActivityId, eventDataCount, (IntPtr)data))
                            ThrowEventSourceException(metadata.Name);
#if FEATURE_PERFTRACING
                        if (metadata.EnabledForEventPipe && !m_eventPipeProvider.WriteEvent(ref metadata.Descriptor, metadata.EventHandle, pActivityId, relatedActivityId, eventDataCount, (IntPtr)data))
                            ThrowEventSourceException(metadata.Name);
#endif // FEATURE_PERFTRACING
                    }
                    else if (metadata.EnabledForETW
#if FEATURE_PERFTRACING
                            || metadata.EnabledForEventPipe
#endif // FEATURE_PERFTRACING
                            )
                    {
                        EventSourceOptions opt = new EventSourceOptions
                        {
                            Keywords = (EventKeywords)metadata.Descriptor.Keywords,
                            Level = (EventLevel)metadata.Descriptor.Level,
                            Opcode = (EventOpcode)metadata.Descriptor.Opcode
                        };

                        WriteMultiMerge(metadata.Name, ref opt, metadata.TraceLoggingEventTypes, pActivityId, relatedActivityId, data);
                    }

                    if (m_Dispatchers != null && metadata.EnabledForAnyListener)
                    {
#if MONO && !TARGET_WASI
                        // On Mono, managed events from NativeRuntimeEventSource are written using WriteEventCore which can be
                        // written doubly because EventPipe tries to pump it back up to EventListener via NativeRuntimeEventSource.ProcessEvents.
                        // So we need to prevent this from getting written directly to the Listeners.
                        if (this.GetType() != typeof(NativeRuntimeEventSource))
#endif // MONO && !TARGET_WASI
                        {
                            var eventCallbackArgs = new EventWrittenEventArgs(this, eventId, pActivityId, relatedActivityId);
                            WriteToAllListeners(eventCallbackArgs, eventDataCount, data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is EventSourceException)
                        throw;
                    else
                    {
                        ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventId);
                        ThrowEventSourceException(metadata.Name, ex);
                    }
                }
            }
        }

        /// <summary>
        /// This is a varargs helper for writing an event. It does create an array and box all the arguments so it is
        /// relatively inefficient and should only be used for relatively rare events (e.g. less than 100 / sec). If your
        /// rates are faster than that you should use <see cref="WriteEventCore"/> to create fast helpers for your particular
        /// method signature. Even if you use this for rare events, this call should be guarded by an <see cref="IsEnabled()"/>
        /// check so that the varargs call is not made when the EventSource is not active.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        protected void WriteEvent(int eventId, params EventSourcePrimitive[] args)
        {
            var argValues = new object?[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                argValues[i] = args[i].Value;
            }
            unsafe
            {
                WriteEventVarargs(eventId, null, argValues);
            }
        }

        // fallback varags helpers.
        /// <summary>
        /// This is the varargs helper for writing an event. It does create an array and box all the arguments so it is
        /// relatively inefficient and should only be used for relatively rare events (e.g. less than 100 / sec). If your
        /// rates are faster than that you should use <see cref="WriteEventCore"/> to create fast helpers for your particular
        /// method signature. Even if you use this for rare events, this call should be guarded by an <see cref="IsEnabled()"/>
        /// check so that the varargs call is not made when the EventSource is not active.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                    Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                    "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
        [RequiresUnreferencedCode(EventSourceRequiresUnreferenceMessage)]
        protected unsafe void WriteEvent(int eventId, params object?[] args)
        {
            WriteEventVarargs(eventId, null, args);
        }

        /// <summary>
        /// This is the varargs helper for writing an event which also specifies a related activity. It is completely analogous
        /// to corresponding WriteEvent (they share implementation). It does create an array and box all the arguments so it is
        /// relatively inefficient and should only be used for relatively rare events (e.g. less than 100 / sec).  If your
        /// rates are faster than that you should use <see cref="WriteEventWithRelatedActivityIdCore"/> to create fast helpers for your
        /// particular method signature. Even if you use this for rare events, this call should be guarded by an <see cref="IsEnabled()"/>
        /// check so that the varargs call is not made when the EventSource is not active.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                    Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                    "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
        [RequiresUnreferencedCode(EventSourceRequiresUnreferenceMessage)]
        protected unsafe void WriteEventWithRelatedActivityId(int eventId, Guid relatedActivityId, params object?[] args)
        {
            WriteEventVarargs(eventId, &relatedActivityId, args);
        }

#endregion

#region IDisposable Members
        /// <summary>
        /// Disposes of an EventSource.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Disposes of an EventSource.
        /// </summary>
        /// <remarks>
        /// Called from Dispose() with disposing=true, and from the finalizer (~EventSource) with disposing=false.
        /// Guidelines:
        /// 1. We may be called more than once: do nothing after the first call.
        /// 2. Avoid throwing exceptions if disposing is false, i.e. if we're being finalized.
        /// </remarks>
        /// <param name="disposing">True if called from Dispose(), false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            // NOTE: If !IsSupported, we use ILLink.Substitutions to nop out the finalizer.
            //       Do not add any code before this line (or you'd need to delete the substitution).
            if (!IsSupported)
            {
                return;
            }

            // Do not invoke Dispose under the lock as this can lead to a deadlock.
            // See https://github.com/dotnet/runtime/issues/48342 for details.
            Debug.Assert(!Monitor.IsEntered(EventListener.EventListenersLock));

            if (disposing)
            {
                // Send the manifest one more time to ensure circular buffers have a chance to get to this information
                // even in scenarios with a high volume of ETW events.
                if (m_eventSourceEnabled)
                {
                    try
                    {
                        SendManifest(m_rawManifest);
                    }
                    catch { } // If it fails, simply give up.
                    m_eventSourceEnabled = false;
                }
                if (m_etwProvider != null)
                {
                    m_etwProvider.Dispose();
                    m_etwProvider = null!;
                }
#if FEATURE_PERFTRACING
                if (m_eventPipeProvider != null)
                {
                    m_eventPipeProvider.Dispose();
                    m_eventPipeProvider = null!;
                }
#endif
            }
            m_eventSourceEnabled = false;
            m_eventSourceDisposed = true;
        }
        /// <summary>
        /// Finalizer for EventSource
        /// </summary>
        ~EventSource()
        {
            // NOTE: we nop out this method body if !IsSupported using ILLink.Substitutions.
            this.Dispose(false);
        }
#endregion

#region private

        private unsafe void WriteEventRaw(
            string? eventName,
            ref EventDescriptor eventDescriptor,
            IntPtr eventHandle,
            Guid* activityID,
            Guid* relatedActivityID,
            int dataCount,
            IntPtr data)
        {
            bool allAreNull = m_etwProvider == null;
            if (m_etwProvider != null
                && !m_etwProvider.WriteEventRaw(ref eventDescriptor, eventHandle, activityID, relatedActivityID, dataCount, data))
            {
                ThrowEventSourceException(eventName);
            }
#if FEATURE_PERFTRACING
            allAreNull &= (m_eventPipeProvider == null);
            if (m_eventPipeProvider != null
                && !m_eventPipeProvider.WriteEventRaw(ref eventDescriptor, eventHandle, activityID, relatedActivityID, dataCount, data))
            {
                ThrowEventSourceException(eventName);
            }
#endif // FEATURE_PERFTRACING
            if (allAreNull)
            {
                ThrowEventSourceException(eventName);
            }
        }

        // FrameworkEventSource is on the startup path for the framework, so we have this internal overload that it can use
        // to prevent the working set hit from looking at the custom attributes on the type to get the Guid.
        internal EventSource(Guid eventSourceGuid, string eventSourceName)
            : this(eventSourceGuid, eventSourceName, EventSourceSettings.EtwManifestEventFormat)
        { }

        // Used by the internal FrameworkEventSource constructor and the TraceLogging-style event source constructor
        internal EventSource(Guid eventSourceGuid, string eventSourceName, EventSourceSettings settings, string[]? traits = null)
        {
            if (IsSupported)
            {
#if FEATURE_PERFTRACING
                m_eventHandleTable = new TraceLoggingEventHandleTable();
#endif

                m_config = ValidateSettings(settings);
                Initialize(eventSourceGuid, eventSourceName, traits);
            }
        }

        /// <summary>
        /// This method is responsible for the common initialization path from our constructors. It must
        /// not leak any exceptions (otherwise, since most EventSource classes define a static member,
        /// "Log", such an exception would become a cached exception for the initialization of the static
        /// member, and any future access to the "Log" would throw the cached exception).
        /// </summary>
        private unsafe void Initialize(Guid eventSourceGuid, string eventSourceName, string[]? traits)
        {
            try
            {
                m_traits = traits;
                if (m_traits != null && m_traits.Length % 2 != 0)
                {
                    throw new ArgumentException(SR.EventSource_TraitEven, nameof(traits));
                }

                if (eventSourceGuid == Guid.Empty)
                {
                    throw new ArgumentException(SR.EventSource_NeedGuid);
                }

                if (eventSourceName == null)
                {
                    throw new ArgumentException(SR.EventSource_NeedName);
                }

                m_name = eventSourceName;
                m_guid = eventSourceGuid;

                // Enable Implicit Activity tracker
                m_activityTracker = ActivityTracker.Instance;

#if !DEBUG
                if (ProviderMetadata.Length == 0)
#endif
                {
                    // Create and register our provider traits.  We do this early because it is needed to log errors
                    // In the self-describing event case.
                    InitializeProviderMetadata();
                }

                // Register the provider with ETW
                Func<EventSource?> eventSourceFactory = () => this;
                OverrideEventProvider? etwProvider = EventSourceInitHelper.TryGetPreregisteredEtwProvider(eventSourceGuid);
                if (etwProvider == null)
                {
                    etwProvider = new OverrideEventProvider(eventSourceFactory, EventProviderType.ETW);
                    etwProvider.Register(eventSourceGuid, eventSourceName);
    #if TARGET_WINDOWS
                    // API available on OS >= Win 8 and patched Win 7.
                    // Disable only for FrameworkEventSource to avoid recursion inside exception handling.
                    if (this.Name != "System.Diagnostics.Eventing.FrameworkEventSource" || Environment.IsWindows8OrAbove)
                    {
                        var providerMetadata = ProviderMetadata;
                        fixed (byte* pMetadata = providerMetadata)
                        {
                            etwProvider.SetInformation(
                                Interop.Advapi32.EVENT_INFO_CLASS.SetTraits,
                                pMetadata,
                                (uint)providerMetadata.Length);
                        }
                    }
    #endif // TARGET_WINDOWS
                }

#if FEATURE_PERFTRACING
                // Register the provider with EventPipe
                OverrideEventProvider? eventPipeProvider = EventSourceInitHelper.TryGetPreregisteredEventPipeProvider(eventSourceName);
                if (eventPipeProvider == null)
                {
                    eventPipeProvider = new OverrideEventProvider(eventSourceFactory, EventProviderType.EventPipe);
                    eventPipeProvider.Register(eventSourceGuid, eventSourceName);

                }
#endif
                // Add the eventSource to the global (weak) list.
                // This also sets m_id, which is the index in the list.
                EventListener.AddEventSource(this);

                // OK if we get this far without an exception, then we can at least write out error messages.
                // Set m_provider, which allows this.
                m_etwProvider = etwProvider;

#if FEATURE_PERFTRACING
                m_eventPipeProvider = eventPipeProvider;
#endif
                Debug.Assert(!m_eventSourceEnabled);     // We can't be enabled until we are completely initted.
            }
            catch (Exception e)
            {
                m_constructionException ??= e;
                ReportOutOfBandMessage("ERROR: Exception during construction of EventSource " + Name + ": " + e.Message);
            }

            // Once m_completelyInited is set, you can have concurrency, so all work is under the lock.
            lock (EventListener.EventListenersLock)
            {
                // If there are any deferred commands, we can do them now.
                // This is the most likely place for exceptions to happen.
                // Note that we are NOT resetting m_deferredCommands to NULL here,
                // We are giving for EventHandler<EventCommandEventArgs> that will be attached later
                EventCommandEventArgs? deferredCommands = m_deferredCommands;
                while (deferredCommands != null)
                {
                    DoCommand(deferredCommands);      // This can never throw, it catches them and reports the errors.
                    deferredCommands = deferredCommands.nextCommand;
                }

                if (m_constructionException == null)
                {
                    m_completelyInited = true;
                }
            }
        }

        private static string GetName(Type eventSourceType, EventManifestOptions flags)
        {
            ArgumentNullException.ThrowIfNull(eventSourceType);

            EventSourceAttribute? attrib = (EventSourceAttribute?)GetCustomAttributeHelper(eventSourceType, typeof(EventSourceAttribute), flags);
            if (attrib != null && attrib.Name != null)
                return attrib.Name;

            return eventSourceType.Name;
        }

        private static Guid GenerateGuidFromName(string name)
        {
            ReadOnlySpan<byte> namespaceBytes =
            [
                0x48, 0x2C, 0x2D, 0xB2, 0xC3, 0x90, 0x47, 0xC8,
                0x87, 0xF8, 0x1A, 0x15, 0xBF, 0xC1, 0x30, 0xFB,
            ];

            byte[] bytes = Encoding.BigEndianUnicode.GetBytes(name);
            Sha1ForNonSecretPurposes hash = default;
            hash.Start();
            hash.Append(namespaceBytes);
            hash.Append(bytes);
            Array.Resize(ref bytes, 16);
            hash.Finish(bytes);

            bytes[7] = unchecked((byte)((bytes[7] & 0x0F) | 0x50));    // Set high 4 bits of octet 7 to 5, as per RFC 4122
            return new Guid(bytes);
        }

        private static unsafe void DecodeObjects(object?[] decodedObjects, Type[] parameterTypes, EventData* data)
        {
            for (int i = 0; i < decodedObjects.Length; i++, data++)
            {
                IntPtr dataPointer = data->DataPointer;
                Type dataType = parameterTypes[i];
                object? decoded;

                if (dataType == typeof(string))
                {
                    goto String;
                }
                else if (dataType == typeof(int))
                {
                    Debug.Assert(data->Size == 4);
                    decoded = *(int*)dataPointer;
                }
                else
                {
                    TypeCode typeCode = Type.GetTypeCode(dataType);
                    int size = data->Size;

                    if (size == 4)
                    {
                        if ((uint)(typeCode - TypeCode.SByte) <= TypeCode.Int32 - TypeCode.SByte)
                        {
                            Debug.Assert(dataType.IsEnum);
                            // Enums less than 4 bytes in size should be treated as int.
                            decoded = *(int*)dataPointer;
                        }
                        else if (typeCode == TypeCode.UInt32)
                        {
                            decoded = *(uint*)dataPointer;
                        }
                        else if (typeCode == TypeCode.Single)
                        {
                            decoded = *(float*)dataPointer;
                        }
                        else if (typeCode == TypeCode.Boolean)
                        {
                            // The manifest defines a bool as a 32bit type (WIN32 BOOL), not 1 bit as CLR Does.
                            decoded = *(int*)dataPointer == 1;
                        }
                        else if (dataType == typeof(byte[]))
                        {
                            // byte[] are written to EventData* as an int followed by a blob
                            Debug.Assert(*(int*)dataPointer == (data + 1)->Size);
                            data++;
                            goto BytePtr;
                        }
                        else if (IntPtr.Size == 4 && dataType == typeof(IntPtr))
                        {
                            decoded = *(IntPtr*)dataPointer;
                        }
                        else
                        {
                            goto Unknown;
                        }
                    }
                    else if (size <= 2)
                    {
                        Debug.Assert(!dataType.IsEnum);
                        if (typeCode == TypeCode.Byte)
                        {
                            Debug.Assert(size == 1);
                            decoded = *(byte*)dataPointer;
                        }
                        else if (typeCode == TypeCode.SByte)
                        {
                            Debug.Assert(size == 1);
                            decoded = *(sbyte*)dataPointer;
                        }
                        else if (typeCode == TypeCode.Int16)
                        {
                            Debug.Assert(size == 2);
                            decoded = *(short*)dataPointer;
                        }
                        else if (typeCode == TypeCode.UInt16)
                        {
                            Debug.Assert(size == 2);
                            decoded = *(ushort*)dataPointer;
                        }
                        else if (typeCode == TypeCode.Char)
                        {
                            Debug.Assert(size == 2);
                            decoded = *(char*)dataPointer;
                        }
                        else
                        {
                            goto Unknown;
                        }
                    }
                    else if (size == 8)
                    {
                        if (typeCode == TypeCode.Int64)
                        {
                            decoded = *(long*)dataPointer;
                        }
                        else if (typeCode == TypeCode.UInt64)
                        {
                            decoded = *(ulong*)dataPointer;
                        }
                        else if (typeCode == TypeCode.Double)
                        {
                            decoded = *(double*)dataPointer;
                        }
                        else if (typeCode == TypeCode.DateTime)
                        {
                            decoded = DateTime.FromFileTimeUtc(*(long*)dataPointer);
                        }
                        else if (IntPtr.Size == 8 && dataType == typeof(IntPtr))
                        {
                            decoded = *(IntPtr*)dataPointer;
                        }
                        else
                        {
                            goto Unknown;
                        }
                    }
                    else if (typeCode == TypeCode.Decimal)
                    {
                        Debug.Assert(size == 16);
                        decoded = *(decimal*)dataPointer;
                    }
                    else if (dataType == typeof(Guid))
                    {
                        Debug.Assert(size == 16);
                        decoded = *(Guid*)dataPointer;
                    }
                    else
                    {
                        goto Unknown;
                    }
                }

                goto Store;

            Unknown:
                if (dataType != typeof(byte*))
                {
                    // Everything else is marshaled as a string.
                    goto String;
                }

            BytePtr:
                if (data->Size == 0)
                {
                    decoded = Array.Empty<byte>();
                }
                else
                {
                    var blob = new byte[data->Size];
                    Marshal.Copy(data->DataPointer, blob, 0, blob.Length);
                    decoded = blob;
                }
                goto Store;

            String:
                // ETW strings are NULL-terminated, so marshal everything up to the first null in the string.
                AssertValidString(data);
                decoded = dataPointer == IntPtr.Zero ? null : new string((char*)dataPointer, 0, (data->Size >> 1) - 1);

            Store:
                decodedObjects[i] = decoded;
            }
        }

        [Conditional("DEBUG")]
        private static unsafe void AssertValidString(EventData* data)
        {
            Debug.Assert(data->Size >= 0 && data->Size % 2 == 0, "String size should be even");
            char* charPointer = (char*)data->DataPointer;
            int charLength = data->Size / 2 - 1;
            for (int i = 0; i < charLength; i++)
            {
                Debug.Assert(*(charPointer + i) != 0, "String may not contain null chars");
            }
            Debug.Assert(*(charPointer + charLength) == 0, "String must be null terminated");
        }

        // Finds the Dispatcher (which holds the filtering state), for a given dispatcher for the current
        // eventSource).
        private EventDispatcher? GetDispatcher(EventListener? listener)
        {
            EventDispatcher? dispatcher = m_Dispatchers;
            while (dispatcher != null)
            {
                if (dispatcher.m_Listener == listener)
                    return dispatcher;
                dispatcher = dispatcher.m_Next;
            }
            return dispatcher;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                    Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                    "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
        [RequiresUnreferencedCode(EventSourceRequiresUnreferenceMessage)]
        private unsafe void WriteEventVarargs(int eventId, Guid* childActivityID, object?[] args)
        {
            if (IsEnabled())
            {
                Debug.Assert(m_eventData != null);  // You must have initialized this if you enabled the source.
                try
                {
                    ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventId);

                    if (childActivityID != null)
                    {
                        // If you use WriteEventWithRelatedActivityID you MUST declare the first argument to be a GUID
                        // with the name 'relatedActivityID, and NOT pass this argument to the WriteEvent method.
                        // During manifest creation we modify the ParameterInfo[] that we store to strip out any
                        // first parameter that is of type Guid and named "relatedActivityId." Thus, if you call
                        // WriteEventWithRelatedActivityID from a method that doesn't name its first parameter correctly
                        // we can end up in a state where the ParameterInfo[] doesn't have its first parameter stripped,
                        // and this leads to a mismatch between the number of arguments and the number of ParameterInfos,
                        // which would cause a cryptic IndexOutOfRangeException later if we don't catch it here.
                        if (!metadata.HasRelatedActivityID)
                        {
                            throw new ArgumentException(SR.EventSource_NoRelatedActivityId);
                        }
                    }

                    LogEventArgsMismatches(eventId, args);

                    Guid* pActivityId = null;
                    Guid activityId = Guid.Empty;
                    Guid relatedActivityId = Guid.Empty;
                    EventOpcode opcode = (EventOpcode)metadata.Descriptor.Opcode;
                    EventActivityOptions activityOptions = metadata.ActivityOptions;

                    if (childActivityID == null &&
                       ((activityOptions & EventActivityOptions.Disable) == 0))
                    {
                        if (opcode == EventOpcode.Start)
                        {
                            m_activityTracker.OnStart(m_name, metadata.Name, metadata.Descriptor.Task, ref activityId, ref relatedActivityId, metadata.ActivityOptions);
                        }
                        else if (opcode == EventOpcode.Stop)
                        {
                            m_activityTracker.OnStop(m_name, metadata.Name, metadata.Descriptor.Task, ref activityId);
                        }

                        if (activityId != Guid.Empty)
                            pActivityId = &activityId;
                        if (relatedActivityId != Guid.Empty)
                            childActivityID = &relatedActivityId;
                    }

                    if (metadata.EnabledForETW
#if FEATURE_PERFTRACING
                            || metadata.EnabledForEventPipe
#endif // FEATURE_PERFTRACING
                        )
                    {
                        if (!SelfDescribingEvents)
                        {
                            if (!m_etwProvider.WriteEvent(ref metadata.Descriptor, metadata.EventHandle, pActivityId, childActivityID, args))
                                ThrowEventSourceException(metadata.Name);
#if FEATURE_PERFTRACING
                            if (!m_eventPipeProvider.WriteEvent(ref metadata.Descriptor, metadata.EventHandle, pActivityId, childActivityID, args))
                                ThrowEventSourceException(metadata.Name);
#endif // FEATURE_PERFTRACING
                        }
                        else
                        {
                            // TODO: activity ID support
                            EventSourceOptions opt = new EventSourceOptions
                            {
                                Keywords = (EventKeywords)metadata.Descriptor.Keywords,
                                Level = (EventLevel)metadata.Descriptor.Level,
                                Opcode = (EventOpcode)metadata.Descriptor.Opcode
                            };

                            WriteMultiMerge(metadata.Name, ref opt, metadata.TraceLoggingEventTypes, pActivityId, childActivityID, args);
                        }
                    }

                    if (m_Dispatchers != null && metadata.EnabledForAnyListener)
                    {
                        // Maintain old behavior - object identity is preserved
                        if (!LocalAppContextSwitches.PreserveEventListenerObjectIdentity)
                        {
                            args = SerializeEventArgs(eventId, args);
                        }

                        var eventCallbackArgs = new EventWrittenEventArgs(this, eventId, pActivityId, childActivityID)
                        {
                            Payload = new ReadOnlyCollection<object?>(args)
                        };

                        DispatchToAllListeners(eventCallbackArgs);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is EventSourceException)
                        throw;
                    else
                    {
                        ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventId);
                        ThrowEventSourceException(metadata.Name, ex);
                    }
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
            Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                            "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
        [RequiresUnreferencedCode(EventSourceRequiresUnreferenceMessage)]
        private object?[] SerializeEventArgs(int eventId, object?[] args)
        {
            Debug.Assert(m_eventData != null);
            ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventId);
            TraceLoggingEventTypes eventTypes = metadata.TraceLoggingEventTypes;
            int paramCount = Math.Min(eventTypes.typeInfos.Length, args.Length); // parameter count mismatch get logged in LogEventArgsMismatches
            var eventData = new object?[eventTypes.typeInfos.Length];
            for (int i = 0; i < paramCount; i++)
            {
                eventData[i] = eventTypes.typeInfos[i].GetData(args[i]);
            }
            return eventData;
        }

        /// <summary>
        /// We expect that the arguments to the Event method and the arguments to WriteEvent match. This function
        /// checks that they in fact match and logs a warning to the debugger if they don't.
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="args"></param>
        private void LogEventArgsMismatches(int eventId, object?[] args)
        {
            Debug.Assert(m_eventData != null);
            ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventId);
            ParameterInfo[] infos = metadata.Parameters;

            if (args.Length != infos.Length)
            {
                ReportOutOfBandMessage(SR.Format(SR.EventSource_EventParametersMismatch, eventId, args.Length, infos.Length));
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                Type pType = infos[i].ParameterType;
                object? arg = args[i];

                // Checking to see if the Parameter types (from the Event method) match the supplied argument types.
                // Fail if one of two things hold : either the argument type is not equal or assignable to the parameter type, or the
                // argument is null and the parameter type is a non-Nullable<T> value type.
                if ((arg != null && !pType.IsAssignableFrom(arg.GetType()))
                    || (arg == null && (pType.IsValueType && !(pType.IsGenericType && pType.GetGenericTypeDefinition() == typeof(Nullable<>))))
                    )
                {
                    ReportOutOfBandMessage(SR.Format(SR.EventSource_VarArgsParameterMismatch, eventId, infos[i].Name));
                    return;
                }
            }
        }

        private unsafe void WriteToAllListeners(EventWrittenEventArgs eventCallbackArgs, int eventDataCount, EventData* data)
        {
            Debug.Assert(m_eventData != null);
            ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventCallbackArgs.EventId);

            if (eventDataCount != metadata.EventListenerParameterCount)
            {
                ReportOutOfBandMessage(SR.Format(SR.EventSource_EventParametersMismatch, eventCallbackArgs.EventId, eventDataCount, metadata.Parameters.Length));
            }

            object?[] args;
            if (eventDataCount == 0)
            {
                eventCallbackArgs.Payload = ReadOnlyCollection<object?>.Empty;
            }
            else
            {
                args = new object?[Math.Min(eventDataCount, metadata.Parameters.Length)];

                if (metadata.AllParametersAreString)
                {
                    for (int i = 0; i < args.Length; i++, data++)
                    {
                        AssertValidString(data);
                        IntPtr dataPointer = data->DataPointer;
                        args[i] = dataPointer == IntPtr.Zero ? null : new string((char*)dataPointer, 0, (data->Size >> 1) - 1);
                    }
                }
                else if (metadata.AllParametersAreInt32)
                {
                    for (int i = 0; i < args.Length; i++, data++)
                    {
                        Debug.Assert(data->Size == 4);
                        args[i] = *(int*)data->DataPointer;
                    }
                }
                else
                {
                    DecodeObjects(args, metadata.ParameterTypes, data);
                }

                eventCallbackArgs.Payload = new ReadOnlyCollection<object?>(args);
            }

            DispatchToAllListeners(eventCallbackArgs);
        }

        internal void DispatchToAllListeners(EventWrittenEventArgs eventCallbackArgs)
        {
            int eventId = eventCallbackArgs.EventId;
            Exception? lastThrownException = null;
            for (EventDispatcher? dispatcher = m_Dispatchers; dispatcher != null; dispatcher = dispatcher.m_Next)
            {
                Debug.Assert(dispatcher.m_EventEnabled != null);
                if (eventId == -1 || dispatcher.m_EventEnabled[eventId])
                {
                    {
                        try
                        {
                            dispatcher.m_Listener.OnEventWritten(eventCallbackArgs);
                        }
                        catch (Exception e)
                        {
                            ReportOutOfBandMessage("ERROR: Exception during EventSource.OnEventWritten: "
                                 + e.Message);
                            lastThrownException = e;
                        }
                    }
                }
            }

            if (lastThrownException != null && ThrowOnEventWriteErrors)
            {
                throw new EventSourceException(lastThrownException);
            }
        }

        // WriteEventString is used for logging an error message (or similar) to
        // ETW and EventPipe providers. It is not a general purpose API, it will
        // log the message with Level=LogAlways and Keywords=All to make sure whoever
        // is listening gets the message.
        private unsafe void WriteEventString(string msgString)
        {
            bool allAreNull = m_etwProvider == null;
#if FEATURE_PERFTRACING
            allAreNull &= (m_eventPipeProvider == null);
#endif // FEATURE_PERFTRACING
            if (allAreNull)
            {
                return;
            }

            EventLevel level = EventLevel.LogAlways;
            long keywords = -1;
            const string EventName = "EventSourceMessage";
            if (SelfDescribingEvents)
            {
                EventSourceOptions opt = new EventSourceOptions
                {
                    Keywords = (EventKeywords)unchecked(keywords),
                    Level = level
                };

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                    Justification = "The call to TraceLoggingEventTypes with the below parameter values are trim safe")]
                static TraceLoggingEventTypes GetTrimSafeTraceLoggingEventTypes() =>
                    new TraceLoggingEventTypes(EventName, EventTags.None, new Type[] { typeof(string) });

                var tlet = GetTrimSafeTraceLoggingEventTypes();
                WriteMultiMergeInner(EventName, ref opt, tlet, null, null, msgString);
            }
            else
            {
                // We want the name of the provider to show up so if we don't have a manifest we create
                // on that at least has the provider name (I don't define any events).
                if (m_rawManifest == null && m_outOfBandMessageCount == 1)
                {
                    ManifestBuilder manifestBuilder = new ManifestBuilder(Name, Guid, Name, null, EventManifestOptions.None);
                    manifestBuilder.StartEvent(EventName, new EventAttribute(0) { Level = level, Task = (EventTask)0xFFFE });
                    manifestBuilder.AddEventParameter(typeof(string), "message");
                    manifestBuilder.EndEvent();
                    SendManifest(manifestBuilder.CreateManifest());
                }

                // We use this low level routine to bypass the enabled checking, since the eventSource itself is only partially inited.
                fixed (char* msgStringPtr = msgString)
                {
                    EventDescriptor descr = new EventDescriptor(0, 0, 0, (byte)level, 0, 0, keywords);
                    EventProvider.EventData data = default;
                    data.Ptr = (ulong)msgStringPtr;
                    data.Size = (uint)(2 * (msgString.Length + 1));
                    data.Reserved = 0;
                    m_etwProvider?.WriteEvent(ref descr, IntPtr.Zero, null, null, 1, (IntPtr)((void*)&data));
#if FEATURE_PERFTRACING
                    if (m_eventPipeProvider != null)
                    {
                        if (m_writeEventStringEventHandle == IntPtr.Zero)
                        {
                            if (m_createEventLock is null)
                            {
                                Interlocked.CompareExchange(ref m_createEventLock, new object(), null);
                            }

                            lock (m_createEventLock)
                            {
                                if (m_writeEventStringEventHandle == IntPtr.Zero)
                                {
                                    string eventName = "EventSourceMessage";
                                    EventParameterInfo paramInfo = default(EventParameterInfo);
                                    paramInfo.SetInfo("message", typeof(string));
                                    byte[]? metadata = EventPipeMetadataGenerator.Instance.GenerateMetadata(0, eventName, keywords, (uint)level, 0, EventOpcode.Info, [paramInfo]);
                                    uint metadataLength = (metadata != null) ? (uint)metadata.Length : 0;

                                    fixed (byte* pMetadata = metadata)
                                    {
                                        m_writeEventStringEventHandle = m_eventPipeProvider._eventProvider.DefineEventHandle(0, eventName, keywords, 0, (uint)level,
                                                                            pMetadata, metadataLength);
                                    }
                                }
                            }
                        }

                        m_eventPipeProvider.WriteEvent(ref descr, m_writeEventStringEventHandle, null, null, 1, (IntPtr)((void*)&data));
                    }
#endif // FEATURE_PERFTRACING
                }
            }
        }

        private static ReadOnlyCollection<string>? s_errorPayloadNames;

        /// <summary>
        /// Since this is a means of reporting errors (see ReportoutOfBandMessage) any failure encountered
        /// while writing the message to any one of the listeners will be silently ignored.
        /// </summary>
        private void WriteStringToAllListeners(string eventName, string msg)
        {
#pragma warning disable CA1861
            var eventCallbackArgs = new EventWrittenEventArgs(this, 0)
            {
                EventName = eventName,
                Message = msg,
                Payload = new ReadOnlyCollection<object?>(new object[] { msg }),
                PayloadNames = s_errorPayloadNames ??= new ReadOnlyCollection<string>(new string[] { "message" })
            };
#pragma warning restore CA1861

            for (EventDispatcher? dispatcher = m_Dispatchers; dispatcher != null; dispatcher = dispatcher.m_Next)
            {
                bool dispatcherEnabled = false;
                if (dispatcher.m_EventEnabled == null)
                {
                    // if the listeners that weren't correctly initialized, we will send to it
                    // since this is an error message and we want to see it go out.
                    dispatcherEnabled = true;
                }
                else
                {
                    // if there's *any* enabled event on the dispatcher we'll write out the string
                    // otherwise we'll treat the listener as disabled and skip it
                    foreach (KeyValuePair<int, bool> entry in dispatcher.m_EventEnabled)
                    {
                        if (entry.Value)
                        {
                            dispatcherEnabled = true;
                            break;
                        }
                    }
                }
                try
                {
                    if (dispatcherEnabled)
                        dispatcher.m_Listener.OnEventWritten(eventCallbackArgs);
                }
                catch
                {
                    // ignore any exceptions thrown by listeners' OnEventWritten
                }
            }
        }

        /// <summary>
        /// Returns true if 'eventNum' is enabled if you only consider the level and matchAnyKeyword filters.
        /// It is possible that eventSources turn off the event based on additional filtering criteria.
        /// </summary>
        private bool IsEnabledByDefault(int eventNum, bool enable, EventLevel currentLevel, EventKeywords currentMatchAnyKeyword)
        {
            if (!enable)
                return false;

            Debug.Assert(m_eventData != null);
            ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventNum);
            EventLevel eventLevel = (EventLevel)metadata.Descriptor.Level;
            EventKeywords eventKeywords = unchecked((EventKeywords)((ulong)metadata.Descriptor.Keywords & (~(SessionMask.All.ToEventKeywords()))));
            EventChannel channel = unchecked((EventChannel)metadata.Descriptor.Channel);

            return IsEnabledCommon(enable, currentLevel, currentMatchAnyKeyword, eventLevel, eventKeywords, channel);
        }

        private bool IsEnabledCommon(bool enabled, EventLevel currentLevel, EventKeywords currentMatchAnyKeyword,
                                                          EventLevel eventLevel, EventKeywords eventKeywords, EventChannel eventChannel)
        {
            if (!enabled)
                return false;

            // does is pass the level test?
            if ((currentLevel != 0) && (currentLevel < eventLevel))
                return false;

            // if yes, does it pass the keywords test?
            if (currentMatchAnyKeyword != 0 && eventKeywords != 0)
            {
                // is there a channel with keywords that match currentMatchAnyKeyword?
                if (eventChannel != EventChannel.None && this.m_channelData != null && this.m_channelData.Length > (int)eventChannel)
                {
                    EventKeywords channel_keywords = unchecked((EventKeywords)(m_channelData[(int)eventChannel] | (ulong)eventKeywords));
                    if (channel_keywords != 0 && (channel_keywords & currentMatchAnyKeyword) == 0)
                        return false;
                }
                else
                {
                    if ((unchecked((ulong)eventKeywords & (ulong)currentMatchAnyKeyword)) == 0)
                        return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowEventSourceException(string? eventName, Exception? innerEx = null)
        {
            // If we fail during out of band logging we may end up trying
            // to throw another EventSourceException, thus hitting a StackOverflowException.
            // Avoid StackOverflow by making sure we do not recursively call this method.
            if (m_EventSourceExceptionRecurenceCount > 0)
                return;
            try
            {
                m_EventSourceExceptionRecurenceCount++;

                string errorPrefix = "EventSourceException";
                if (eventName != null)
                {
                    errorPrefix += " while processing event \"" + eventName + "\"";
                }

                // TODO Create variations of EventSourceException that indicate more information using the error code.
                switch (EventProvider.GetLastWriteEventError())
                {
                    case EventProvider.WriteEventErrorCode.EventTooBig:
                        ReportOutOfBandMessage(errorPrefix + ": " + SR.EventSource_EventTooBig);
                        if (ThrowOnEventWriteErrors) throw new EventSourceException(SR.EventSource_EventTooBig, innerEx);
                        break;
                    case EventProvider.WriteEventErrorCode.NoFreeBuffers:
                        ReportOutOfBandMessage(errorPrefix + ": " + SR.EventSource_NoFreeBuffers);
                        if (ThrowOnEventWriteErrors) throw new EventSourceException(SR.EventSource_NoFreeBuffers, innerEx);
                        break;
                    case EventProvider.WriteEventErrorCode.NullInput:
                        ReportOutOfBandMessage(errorPrefix + ": " + SR.EventSource_NullInput);
                        if (ThrowOnEventWriteErrors) throw new EventSourceException(SR.EventSource_NullInput, innerEx);
                        break;
                    case EventProvider.WriteEventErrorCode.TooManyArgs:
                        ReportOutOfBandMessage(errorPrefix + ": " + SR.EventSource_TooManyArgs);
                        if (ThrowOnEventWriteErrors) throw new EventSourceException(SR.EventSource_TooManyArgs, innerEx);
                        break;
                    default:
                        if (innerEx != null)
                        {
                            innerEx = innerEx.GetBaseException();
                            ReportOutOfBandMessage(errorPrefix + ": " + innerEx.GetType() + ":" + innerEx.Message);
                        }
                        else
                            ReportOutOfBandMessage(errorPrefix);
                        if (ThrowOnEventWriteErrors) throw new EventSourceException(innerEx);
                        break;
                }
            }
            finally
            {
                m_EventSourceExceptionRecurenceCount--;
            }
        }

        internal static EventOpcode GetOpcodeWithDefault(EventOpcode opcode, string? eventName)
        {
            if (opcode == EventOpcode.Info && eventName != null)
            {
                if (eventName.EndsWith(ActivityStartSuffix, StringComparison.Ordinal))
                {
                    return EventOpcode.Start;
                }
                else if (eventName.EndsWith(ActivityStopSuffix, StringComparison.Ordinal))
                {
                    return EventOpcode.Stop;
                }
            }

            return opcode;
        }

        /// <summary>
        /// This class lets us hook the 'OnEventCommand' from the eventSource.
        /// </summary>
        internal sealed class OverrideEventProvider : EventProvider
        {
            public OverrideEventProvider(Func<EventSource?> eventSourceFactory, EventProviderType providerType)
                : base(providerType)
            {
                _eventSourceFactory = eventSourceFactory;
                _eventProviderType = providerType;
            }
            internal override void OnControllerCommand(ControllerCommand command, IDictionary<string, string?>? arguments,
                                                              int perEventSourceSessionId)
            {
                // We use null to represent the ETW EventListener.
                EventListener? listener = null;
                _eventSourceFactory()?.SendCommand(listener, _eventProviderType, perEventSourceSessionId,
                                          (EventCommand)command, IsEnabled(), Level, MatchAnyKeyword, arguments);
            }
            private readonly Func<EventSource?> _eventSourceFactory;
            private readonly EventProviderType _eventProviderType;
        }

        /// <summary>
        /// Used to hold all the static information about an event.  This includes everything in the event
        /// descriptor as well as some stuff we added specifically for EventSource. see the
        /// code:m_eventData for where we use this.
        /// </summary>

        internal partial struct EventMetadata
        {
            public EventDescriptor Descriptor;
            public IntPtr EventHandle;              // EventPipeEvent handle.
            public EventTags Tags;
            public bool EnabledForAnyListener;      // true if any dispatcher has this event turned on
            public bool EnabledForETW;              // is this event on for ETW?
#if FEATURE_PERFTRACING
            public bool EnabledForEventPipe;        // is this event on for EventPipe?
#endif

            public bool HasRelatedActivityID;       // Set if the event method's first parameter is a Guid named 'relatedActivityId'
            public string Name;                     // the name of the event
            public string? Message;                  // If the event has a message associated with it, this is it.
            public ParameterInfo[] Parameters;      // TODO can we remove?
            public int EventListenerParameterCount;
            public bool AllParametersAreString;
            public bool AllParametersAreInt32;

            public EventActivityOptions ActivityOptions;

            private TraceLoggingEventTypes _traceLoggingEventTypes;
            public TraceLoggingEventTypes TraceLoggingEventTypes
            {
                [RequiresUnreferencedCode(EventSourceRequiresUnreferenceMessage)]
                get
                {
                    if (_traceLoggingEventTypes is null)
                    {
                        var tlet = new TraceLoggingEventTypes(Name, Tags, Parameters);
                        Interlocked.CompareExchange(ref _traceLoggingEventTypes, tlet, null);
                    }
                    return _traceLoggingEventTypes;
                }
            }

            private ReadOnlyCollection<string>? _parameterNames;
            public ReadOnlyCollection<string> ParameterNames
            {
                get
                {
                    if (_parameterNames is null)
                    {
                        ParameterInfo[] parameters = Parameters;
                        var names = new string[parameters.Length];
                        for (int i = 0; i < names.Length; i++)
                        {
                            names[i] = parameters[i].Name!;
                        }
                        _parameterNames = new ReadOnlyCollection<string>(names);
                    }
                    return _parameterNames;
                }
            }

            private Type[]? _parameterTypes;
            public Type[] ParameterTypes
            {
                get
                {
                    return _parameterTypes ??= GetParameterTypes(Parameters);

                    static Type[] GetParameterTypes(ParameterInfo[] parameters)
                    {
                        var types = new Type[parameters.Length];
                        for (int i = 0; i < types.Length; i++)
                        {
                            types[i] = parameters[i].ParameterType;
                        }
                        return types;
                    }
                }
            }
        }

        // This is the internal entry point that code:EventListeners call when wanting to send a command to a
        // eventSource. The logic is as follows
        //
        // * if Command == Update
        //     * perEventSourceSessionId specifies the per-provider ETW session ID that the command applies
        //         to (if listener != null)
        //         perEventSourceSessionId = 0 - reserved for EventListeners
        //         perEventSourceSessionId = 1..SessionMask.MAX - reserved for activity tracing aware ETW sessions
        //                  perEventSourceSessionId-1 represents the bit in the reserved field (bits 44..47) in
        //                  Keywords that identifies the session
        //         perEventSourceSessionId = SessionMask.MAX+1 - reserved for legacy ETW sessions; these are
        //                  discriminated by etwSessionId
        //     * etwSessionId specifies a machine-wide ETW session ID; this allows correlation of
        //         activity tracing across different providers (which might have different sessionIds
        //         for the same ETW session)
        //     * enable, level, matchAnyKeywords are used to set a default for all events for the
        //         eventSource.  In particular, if 'enabled' is false, 'level' and
        //         'matchAnyKeywords' are not used.
        //     * OnEventCommand is invoked, which may cause calls to
        //         code:EventSource.EnableEventForDispatcher which may cause changes in the filtering
        //         depending on the logic in that routine.
        // * else (command != Update)
        //     * Simply call OnEventCommand. The expectation is that filtering is NOT changed.
        //     * The 'enabled' 'level', matchAnyKeyword' arguments are ignored (must be true, 0, 0).
        //
        // dispatcher == null has special meaning. It is the 'ETW' dispatcher.
        internal void SendCommand(EventListener? listener, EventProviderType eventProviderType, int perEventSourceSessionId,
                                  EventCommand command, bool enable,
                                  EventLevel level, EventKeywords matchAnyKeyword,
                                  IDictionary<string, string?>? commandArguments)
        {
            if (!IsSupported)
            {
                return;
            }

            var commandArgs = new EventCommandEventArgs(command, commandArguments, this, listener, eventProviderType, perEventSourceSessionId, enable, level, matchAnyKeyword);
            lock (EventListener.EventListenersLock)
            {
                if (m_completelyInited)
                {
                    // After the first command arrive after construction, we are ready to get rid of the deferred commands
                    this.m_deferredCommands = null;
                    // We are fully initialized, do the command
                    DoCommand(commandArgs);
                }
                else
                {
                    // We can't do the command, simply remember it and we do it when we are fully constructed.
                    if (m_deferredCommands == null)
                    {
                        m_deferredCommands = commandArgs;       // create the first entry
                    }
                    else
                    {
                        // We have one or more entries, find the last one and add it to that.
                        EventCommandEventArgs lastCommand = m_deferredCommands;
                        while (lastCommand.nextCommand != null)
                            lastCommand = lastCommand.nextCommand;
                        lastCommand.nextCommand = commandArgs;
                    }
                }
            }
        }

        /// <summary>
        /// We want the eventSource to be fully initialized when we do commands because that way we can send
        /// error messages and other logging directly to the event stream.   Unfortunately we can get callbacks
        /// when we are not fully initialized.  In that case we store them in 'commandArgs' and do them later.
        /// This helper actually does all actual command logic.
        /// </summary>
        internal void DoCommand(EventCommandEventArgs commandArgs)
        {
            if (!IsSupported)
            {
                return;
            }

            // PRECONDITION: We should be holding the EventListener.EventListenersLock
            Debug.Assert(Monitor.IsEntered(EventListener.EventListenersLock));

            // We defer commands until we can send error messages.
            if (m_etwProvider == null)     // If we failed to construct
                return;

#if FEATURE_PERFTRACING
            if (m_eventPipeProvider == null)
                return;
#endif

            m_outOfBandMessageCount = 0;
            try
            {
                EnsureDescriptorsInitialized();
                Debug.Assert(m_eventData != null);

                // Find the per-EventSource dispatcher corresponding to registered dispatcher
                commandArgs.dispatcher = GetDispatcher(commandArgs.listener);
                if (commandArgs.dispatcher == null && commandArgs.listener != null)     // dispatcher == null means ETW dispatcher
                {
                    throw new ArgumentException(SR.EventSource_ListenerNotFound);
                }

                commandArgs.Arguments ??= new Dictionary<string, string?>();

                if (commandArgs.Command == EventCommand.Update)
                {
                    // Set it up using the 'standard' filtering bitfields (use the "global" enable, not session specific one)
                    foreach (int eventID in m_eventData.Keys)
                        EnableEventForDispatcher(commandArgs.dispatcher, commandArgs.eventProviderType, eventID, IsEnabledByDefault(eventID, commandArgs.enable, commandArgs.level, commandArgs.matchAnyKeyword));

                    if (commandArgs.enable)
                    {
                        if (!m_eventSourceEnabled)
                        {
                            // EventSource turned on for the first time, simply copy the bits.
                            m_level = commandArgs.level;
                            m_matchAnyKeyword = commandArgs.matchAnyKeyword;
                        }
                        else
                        {
                            // Already enabled, make it the most verbose of the existing and new filter
                            if (commandArgs.level > m_level)
                                m_level = commandArgs.level;
                            if (commandArgs.matchAnyKeyword == 0)
                                m_matchAnyKeyword = 0;
                            else if (m_matchAnyKeyword != 0)
                                m_matchAnyKeyword = unchecked(m_matchAnyKeyword | commandArgs.matchAnyKeyword);
                        }
                    }

                    // interpret perEventSourceSessionId's sign, and adjust perEventSourceSessionId to
                    // represent 0-based positive values
                    bool bSessionEnable = (commandArgs.perEventSourceSessionId >= 0);
                    if (commandArgs.perEventSourceSessionId == 0 && !commandArgs.enable)
                        bSessionEnable = false;

                    if (commandArgs.listener == null)
                    {
                        if (!bSessionEnable)
                            commandArgs.perEventSourceSessionId = -commandArgs.perEventSourceSessionId;
                        // for "global" enable/disable (passed in with listener == null and
                        //  perEventSourceSessionId == 0) perEventSourceSessionId becomes -1
                        --commandArgs.perEventSourceSessionId;
                    }

                    commandArgs.Command = bSessionEnable ? EventCommand.Enable : EventCommand.Disable;

                    // perEventSourceSessionId = -1 when ETW sent a notification, but the set of active sessions
                    // hasn't changed.
                    // sesisonId = SessionMask.MAX when one of the legacy ETW sessions changed
                    // 0 <= perEventSourceSessionId < SessionMask.MAX for activity-tracing aware sessions
                    Debug.Assert(commandArgs.perEventSourceSessionId >= -1 && commandArgs.perEventSourceSessionId <= SessionMask.MAX);

                    // Send the manifest if we are enabling an ETW session
                    if (bSessionEnable && commandArgs.dispatcher == null)
                    {
                        // eventSourceDispatcher == null means this is the ETW manifest

                        // Note that we unconditionally send the manifest whenever we are enabled, even if
                        // we were already enabled.   This is because there may be multiple sessions active
                        // and we can't know that all the sessions have seen the manifest.
                        if (!SelfDescribingEvents)
                            SendManifest(m_rawManifest);
                    }

                    // Turn on the enable bit before making the OnEventCommand callback  This allows you to do useful
                    // things like log messages, or test if keywords are enabled in the callback.
                    if (commandArgs.enable)
                    {
                        Debug.Assert(m_eventData != null);
                        m_eventSourceEnabled = true;
                    }

                    if (!commandArgs.enable)
                    {
                        // If we are disabling, maybe we can turn on 'quick checks' to filter
                        // quickly.  These are all just optimizations (since later checks will still filter)

                        // There is a good chance EnabledForAnyListener are not as accurate as
                        // they could be, go ahead and get a better estimate.
                        foreach (int eventID in m_eventData.Keys)
                        {
                            bool isEnabledForAnyListener = false;
                            for (EventDispatcher? dispatcher = m_Dispatchers; dispatcher != null; dispatcher = dispatcher.m_Next)
                            {
                                Debug.Assert(dispatcher.m_EventEnabled != null);

                                if (dispatcher.m_EventEnabled[eventID])
                                {
                                    isEnabledForAnyListener = true;
                                    break;
                                }
                            }
                            ref  EventMetadata eventMeta = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventID);
                            eventMeta.EnabledForAnyListener = isEnabledForAnyListener;
                        }

                        // If no events are enabled, disable the global enabled bit.
                        if (!AnyEventEnabled())
                        {
                            m_level = 0;
                            m_matchAnyKeyword = 0;
                            m_eventSourceEnabled = false;
                        }
                    }

                    this.OnEventCommand(commandArgs);
                    this.m_eventCommandExecuted?.Invoke(this, commandArgs);
                }
                else
                {
                    if (commandArgs.Command == EventCommand.SendManifest)
                    {
                        // TODO: should we generate the manifest here if we hadn't already?
                        if (m_rawManifest != null)
                            SendManifest(m_rawManifest);
                    }

                    // These are not used for non-update commands and thus should always be 'default' values
                    // Debug.Assert(enable == true);
                    // Debug.Assert(level == EventLevel.LogAlways);
                    // Debug.Assert(matchAnyKeyword == EventKeywords.None);

                    this.OnEventCommand(commandArgs);
                    m_eventCommandExecuted?.Invoke(this, commandArgs);
                }
            }
            catch (Exception e)
            {
                // When the ETW session is created after the EventSource has registered with the ETW system
                // we can send any error messages here.
                ReportOutOfBandMessage("ERROR: Exception in Command Processing for EventSource " + Name + ": " + e.Message);
                // We never throw when doing a command.
            }
        }

        /// <summary>
        /// If 'value is 'true' then set the eventSource so that 'dispatcher' will receive event with the eventId
        /// of 'eventId.  If value is 'false' disable the event for that dispatcher.   If 'eventId' is out of
        /// range return false, otherwise true.
        /// </summary>
        internal bool EnableEventForDispatcher(EventDispatcher? dispatcher, EventProviderType eventProviderType, int eventId, bool value)
        {
            if (!IsSupported)
                return false;

            Debug.Assert(m_eventData != null);
            ref EventMetadata eventMeta = ref CollectionsMarshal.GetValueRefOrNullRef(m_eventData, eventId);
            if (Unsafe.IsNullRef(ref eventMeta)) return false;

            if (dispatcher == null)
            {
                if (m_etwProvider != null && eventProviderType == EventProviderType.ETW)
                    eventMeta.EnabledForETW = value;

#if FEATURE_PERFTRACING
                if (m_eventPipeProvider != null && eventProviderType == EventProviderType.EventPipe)
                    eventMeta.EnabledForEventPipe = value;
#endif
            }
            else
            {
                Debug.Assert(dispatcher.m_EventEnabled != null);

                if (!dispatcher.m_EventEnabled.ContainsKey(eventId))
                    return false;

                dispatcher.m_EventEnabled[eventId] = value;
                if (value)
                    eventMeta.EnabledForAnyListener = true;
            }
            return true;
        }

        /// <summary>
        /// Returns true if any event at all is on.
        /// </summary>
        private bool AnyEventEnabled()
        {
            Debug.Assert(m_eventData != null);
            foreach (EventMetadata eventMeta in m_eventData.Values)
                if (eventMeta.EnabledForETW || eventMeta.EnabledForAnyListener
#if FEATURE_PERFTRACING
                        || eventMeta.EnabledForEventPipe
#endif // FEATURE_PERFTRACING
                    )
                    return true;
            return false;
        }

        private bool IsDisposed => m_eventSourceDisposed;

        private void EnsureDescriptorsInitialized()
        {
            Debug.Assert(Monitor.IsEntered(EventListener.EventListenersLock));
            if (m_eventData == null)
            {
                // get the metadata via reflection.
                Debug.Assert(m_rawManifest == null);
                m_rawManifest = CreateManifestAndDescriptors(this.GetType(), Name, this);
                Debug.Assert(m_eventData != null);

                // TODO Enforce singleton pattern
                if (!AllowDuplicateSourceNames)
                {
                    Debug.Assert(EventListener.s_EventSources != null, "should be called within lock on EventListener.EventListenersLock which ensures s_EventSources to be initialized");
                    foreach (WeakReference<EventSource> eventSourceRef in EventListener.s_EventSources)
                    {
                        if (eventSourceRef.TryGetTarget(out EventSource? eventSource) && eventSource.Guid == m_guid && !eventSource.IsDisposed)
                        {
                            if (eventSource != this)
                            {
                                throw new ArgumentException(SR.Format(SR.EventSource_EventSourceGuidInUse, m_guid));
                            }
                        }
                    }
                }

                // Make certain all dispatchers also have their arrays initialized
                EventDispatcher? dispatcher = m_Dispatchers;
                while (dispatcher != null)
                {
                    Dictionary<int, bool> eventEnabled = new Dictionary<int, bool>(m_eventData.Count);
                    foreach (int eventId in m_eventData.Keys)
                    {
                        eventEnabled[eventId] = false;
                    }
                    dispatcher.m_EventEnabled ??= eventEnabled;
                    dispatcher = dispatcher.m_Next;
                }
#if FEATURE_PERFTRACING
                // Initialize the EventPipe event handles.
                DefineEventPipeEvents();
#endif
            }
        }

        // Send out the ETW manifest XML out to ETW
        // Today, we only send the manifest to ETW, custom listeners don't get it.
        private unsafe void SendManifest(byte[]? rawManifest)
        {
            if (rawManifest == null)
            {
                return;
            }

            Debug.Assert(!SelfDescribingEvents);

            fixed (byte* dataPtr = rawManifest)
            {
                // we don't want the manifest to show up in the event log channels so we specify as keywords
                // everything but the first 8 bits (reserved for the 8 channels)
                var manifestDescr = new EventDescriptor(0xFFFE, 1, 0, 0, 0xFE, 0xFFFE, 0x00ffFFFFffffFFFF);
                ManifestEnvelope envelope = default;

                envelope.Format = ManifestEnvelope.ManifestFormats.SimpleXmlFormat;
                envelope.MajorVersion = 1;
                envelope.MinorVersion = 0;
                envelope.Magic = 0x5B;              // An unusual number that can be checked for consistency.
                int dataLeft = rawManifest.Length;
                envelope.ChunkNumber = 0;

                EventProvider.EventData* dataDescrs = stackalloc EventProvider.EventData[2];

                dataDescrs[0].Ptr = (ulong)&envelope;
                dataDescrs[0].Size = (uint)sizeof(ManifestEnvelope);
                dataDescrs[0].Reserved = 0;

                dataDescrs[1].Ptr = (ulong)dataPtr;
                dataDescrs[1].Reserved = 0;

                int chunkSize = ManifestEnvelope.MaxChunkSize;
                TRY_AGAIN_WITH_SMALLER_CHUNK_SIZE:
                envelope.TotalChunks = (ushort)((dataLeft + (chunkSize - 1)) / chunkSize);
                while (dataLeft > 0)
                {
                    dataDescrs[1].Size = (uint)Math.Min(dataLeft, chunkSize);
                    if (m_etwProvider != null)
                    {
                        if (!m_etwProvider.WriteEvent(ref manifestDescr, IntPtr.Zero, null, null, 2, (IntPtr)dataDescrs))
                        {
                            // Turns out that if users set the BufferSize to something less than 64K then WriteEvent
                            // can fail.   If we get this failure on the first chunk try again with something smaller
                            // The smallest BufferSize is 1K so if we get to 256 (to account for envelope overhead), we can give up making it smaller.
                            if (EventProvider.GetLastWriteEventError() == EventProvider.WriteEventErrorCode.EventTooBig)
                            {
                                if (envelope.ChunkNumber == 0 && chunkSize > 256)
                                {
                                    chunkSize /= 2;
                                    goto TRY_AGAIN_WITH_SMALLER_CHUNK_SIZE;
                                }
                            }

                            if (ThrowOnEventWriteErrors)
                                ThrowEventSourceException("SendManifest");
                            break;
                        }
                    }
                    dataLeft -= chunkSize;
                    dataDescrs[1].Ptr += (uint)chunkSize;
                    envelope.ChunkNumber++;

                    // For large manifests we want to not overflow any receiver's buffer. Most manifests will fit within
                    // 5 chunks, so only the largest manifests will hit the pause.
                    if ((envelope.ChunkNumber % 5) == 0)
                    {
                        Thread.Sleep(15);
                    }
                }
            }
        }

        // Helper to deal with the fact that the type we are reflecting over might be loaded in the ReflectionOnly context.
        // When that is the case, we have to build the custom assemblies on a member by hand.
        internal static bool IsCustomAttributeDefinedHelper(
            MemberInfo member,
            Type attributeType,
            EventManifestOptions flags = EventManifestOptions.None)
        {
            // AllowEventSourceOverride is an option that allows either Microsoft.Diagnostics.Tracing or
            // System.Diagnostics.Tracing EventSource to be considered valid.  This should not mattter anywhere but in Microsoft.Diagnostics.Tracing (nuget package).
            if (!member.Module.Assembly.ReflectionOnly && (flags & EventManifestOptions.AllowEventSourceOverride) == 0)
            {
                // Let the runtime do the work for us, since we can execute code in this context.
                return member.IsDefined(attributeType, inherit: false);
            }

            foreach (CustomAttributeData data in CustomAttributeData.GetCustomAttributes(member))
            {
                if (AttributeTypeNamesMatch(attributeType, data.Constructor.ReflectedType!))
                {
                    return true;
                }
            }

            return false;
        }

        // Helper to deal with the fact that the type we are reflecting over might be loaded in the ReflectionOnly context.
        // When that is the case, we have the build the custom assemblies on a member by hand.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2114:ReflectionToDynamicallyAccessedMembers",
            Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                            "has dynamically accessed members requirements, but EnsureDescriptorsInitialized does not "+
                            "access this member and is safe to call.")]
        internal static Attribute? GetCustomAttributeHelper(
            MemberInfo member,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
            Type attributeType,
            EventManifestOptions flags = EventManifestOptions.None)
        {
            Debug.Assert(attributeType == typeof(EventAttribute) || attributeType == typeof(EventSourceAttribute));
            // AllowEventSourceOverride is an option that allows either Microsoft.Diagnostics.Tracing or
            // System.Diagnostics.Tracing EventSource to be considered valid.  This should not mattter anywhere but in Microsoft.Diagnostics.Tracing (nuget package).
            if (!member.Module.Assembly.ReflectionOnly && (flags & EventManifestOptions.AllowEventSourceOverride) == 0)
            {
                // Let the runtime do the work for us, since we can execute code in this context.
                return member.GetCustomAttribute(attributeType, inherit: false);
            }

            foreach (CustomAttributeData data in CustomAttributeData.GetCustomAttributes(member))
            {
                if (AttributeTypeNamesMatch(attributeType, data.Constructor.ReflectedType!))
                {
                    Attribute? attr = null;

                    Debug.Assert(data.ConstructorArguments.Count <= 1);

                    if (data.ConstructorArguments.Count == 1)
                    {
                        attr = (Attribute?)Activator.CreateInstance(attributeType, [data.ConstructorArguments[0].Value]);
                    }
                    else if (data.ConstructorArguments.Count == 0)
                    {
                        attr = (Attribute?)Activator.CreateInstance(attributeType);
                    }

                    if (attr != null)
                    {
                        foreach (CustomAttributeNamedArgument namedArgument in data.NamedArguments)
                        {
                            PropertyInfo p = attributeType.GetProperty(namedArgument.MemberInfo.Name, BindingFlags.Public | BindingFlags.Instance)!;
                            object value = namedArgument.TypedValue.Value!;

                            if (p.PropertyType.IsEnum)
                            {
                                string val = value.ToString()!;
                                value = Enum.Parse(p.PropertyType, val);
                            }

                            p.SetValue(attr, value, null);
                        }

                        return attr;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Evaluates if two related "EventSource"-domain types should be considered the same
        /// </summary>
        /// <param name="attributeType">The attribute type in the load context - it's associated with the running
        /// EventSource type. This type may be different fromt he base type of the user-defined EventSource.</param>
        /// <param name="reflectedAttributeType">The attribute type in the reflection context - it's associated with
        /// the user-defined EventSource, and is in the same assembly as the eventSourceType passed to
        /// </param>
        /// <returns>True - if the types should be considered equivalent, False - otherwise</returns>
        private static bool AttributeTypeNamesMatch(Type attributeType, Type reflectedAttributeType)
        {
            return
                // are these the same type?
                attributeType == reflectedAttributeType ||
                // are the full typenames equal?
                string.Equals(attributeType.FullName, reflectedAttributeType.FullName, StringComparison.Ordinal) ||
                    // are the typenames equal and the namespaces under "Diagnostics.Tracing" (typically
                    // either Microsoft.Diagnostics.Tracing or System.Diagnostics.Tracing)?
                    string.Equals(attributeType.Name, reflectedAttributeType.Name, StringComparison.Ordinal) &&
                    attributeType.Namespace!.EndsWith("Diagnostics.Tracing", StringComparison.Ordinal) &&
                    reflectedAttributeType.Namespace!.EndsWith("Diagnostics.Tracing", StringComparison.Ordinal);
        }

        private static Type? GetEventSourceBaseType(Type eventSourceType, bool allowEventSourceOverride, bool reflectionOnly)
        {
            Type? ret = eventSourceType;

            // return false for "object" and interfaces
            if (ret.BaseType == null)
                return null;

            // now go up the inheritance chain until hitting a concrete type ("object" at worse)
            do
            {
                ret = ret.BaseType;
            }
            while (ret != null && ret.IsAbstract);

            if (ret != null)
            {
                if (!allowEventSourceOverride)
                {
                    if (reflectionOnly && ret.FullName != typeof(EventSource).FullName ||
                        !reflectionOnly && ret != typeof(EventSource))
                        return null;
                }
                else
                {
                    if (ret.Name != "EventSource")
                        return null;
                }
            }
            return ret;
        }

        // Use reflection to look at the attributes of a class, and generate a manifest for it (as UTF8) and
        // return the UTF8 bytes.  It also sets up the code:EventData structures needed to dispatch events
        // at run time.  'source' is the event source to place the descriptors.  If it is null,
        // then the descriptors are not created, and just the manifest is generated.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2114:ReflectionToDynamicallyAccessedMembers",
            Justification = "EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                            "has dynamically accessed members requirements, but its use of this method satisfies " +
                            "these requirements because it passes in the result of GetType with the same annotations.")]
        private static byte[]? CreateManifestAndDescriptors(
            [DynamicallyAccessedMembers(ManifestMemberTypes)]
            Type eventSourceType,
            string? eventSourceDllName,
            EventSource? source,
            EventManifestOptions flags = EventManifestOptions.None)
        {
            ManifestBuilder? manifest = null;
            bool bNeedsManifest = source != null ? !source.SelfDescribingEvents : true;
            Exception? exception = null; // exception that might get raised during validation b/c we couldn't/didn't recover from a previous error
            byte[]? res = null;

            if (eventSourceType.IsAbstract && (flags & EventManifestOptions.Strict) == 0)
                return null;

            try
            {
                MethodInfo[] methods = eventSourceType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                EventAttribute defaultEventAttribute;
                int eventId = 1;        // The number given to an event that does not have a explicitly given ID.
                Dictionary<int, EventMetadata>? eventData = null;
                Dictionary<string, string>? eventsByName = null;
                if (source != null || (flags & EventManifestOptions.Strict) != 0)
                {
                    eventData = new Dictionary<int, EventMetadata>();
                    ref EventMetadata newEventMetadata = ref CollectionsMarshal.GetValueRefOrAddDefault(eventData, 0, out _);
                    newEventMetadata.Name = ""; // Event 0 is the 'write messages string' event, and has an empty name.
                }

                // See if we have localization information.
                ResourceManager? resources = null;
                EventSourceAttribute? eventSourceAttrib = (EventSourceAttribute?)GetCustomAttributeHelper(eventSourceType, typeof(EventSourceAttribute), flags);
                if (eventSourceAttrib != null && eventSourceAttrib.LocalizationResources != null)
                    resources = new ResourceManager(eventSourceAttrib.LocalizationResources, eventSourceType.Assembly);

                if (source?.GetType() == typeof(NativeRuntimeEventSource))
                {
                    // Don't emit nor generate the manifest for NativeRuntimeEventSource i.e., Microsoft-Windows-DotNETRuntime.
                    manifest = new ManifestBuilder(resources, flags);
                    bNeedsManifest = false;
                }
                else
                {
                    // Try to get name and GUID directly from the source. Otherwise get it from the Type's attribute.
                    string providerName = source?.Name ?? GetName(eventSourceType, flags);
                    Guid providerGuid = source?.Guid ?? GetGuid(eventSourceType);

                    manifest = new ManifestBuilder(providerName, providerGuid, eventSourceDllName, resources, flags);
                }

                // Add an entry unconditionally for event ID 0 which will be for a string message.
                manifest.StartEvent("EventSourceMessage", new EventAttribute(0) { Level = EventLevel.LogAlways, Task = (EventTask)0xFFFE });
                manifest.AddEventParameter(typeof(string), "message");
                manifest.EndEvent();

                // eventSourceType must be sealed and must derive from this EventSource
                if ((flags & EventManifestOptions.Strict) != 0)
                {
                    bool typeMatch = GetEventSourceBaseType(eventSourceType, (flags & EventManifestOptions.AllowEventSourceOverride) != 0, eventSourceType.Assembly.ReflectionOnly) != null;

                    if (!typeMatch)
                    {
                        manifest.ManifestError(SR.EventSource_TypeMustDeriveFromEventSource);
                    }
                    if (!eventSourceType.IsAbstract && !eventSourceType.IsSealed)
                    {
                        manifest.ManifestError(SR.EventSource_TypeMustBeSealedOrAbstract);
                    }
                }

                // Collect task, opcode, keyword and channel information
                foreach (string providerEnumKind in (ReadOnlySpan<string>)["Keywords", "Tasks", "Opcodes"])
                {
                    Type? nestedType = eventSourceType.GetNestedType(providerEnumKind);
                    if (nestedType != null)
                    {
                        if (eventSourceType.IsAbstract)
                        {
                            manifest.ManifestError(SR.Format(SR.EventSource_AbstractMustNotDeclareKTOC, nestedType.Name));
                        }
                        else
                        {
                            foreach (FieldInfo staticField in nestedType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                            {
                                AddProviderEnumKind(manifest, staticField, providerEnumKind);
                            }
                        }
                    }
                }
                // ensure we have keywords for the session-filtering reserved bits
                {
                    manifest.AddKeyword("Session3", (long)0x1000 << 32);
                    manifest.AddKeyword("Session2", (long)0x2000 << 32);
                    manifest.AddKeyword("Session1", (long)0x4000 << 32);
                    manifest.AddKeyword("Session0", (long)0x8000 << 32);
                }

                if (eventSourceType != typeof(EventSource))
                {
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];

                        // Compat: until v4.5.1 we ignored any non-void returning methods as well as virtual methods for
                        // the only reason of limiting the number of methods considered to be events. This broke a common
                        // design of having event sources implement specific interfaces. To fix this in a compatible way
                        // we will now allow both non-void returning and virtual methods to be Event methods, as long
                        // as they are marked with the [Event] attribute
                        if (/* method.IsVirtual || */ method.IsStatic)
                        {
                            continue;
                        }

                        // Get the EventDescriptor (from the Custom attributes)
                        EventAttribute? eventAttribute = (EventAttribute?)GetCustomAttributeHelper(method, typeof(EventAttribute), flags);

                        if (eventSourceType.IsAbstract)
                        {
                            if (eventAttribute != null)
                            {
                                manifest.ManifestError(SR.Format(SR.EventSource_AbstractMustNotDeclareEventMethods, method.Name, eventAttribute.EventId));
                            }
                            continue;
                        }
                        else if (eventAttribute == null)
                        {
                            // Methods that don't return void can't be events, if they're NOT marked with [Event].
                            // (see Compat comment above)
                            if (method.ReturnType != typeof(void))
                            {
                                continue;
                            }

                            // Continue to ignore virtual methods if they do NOT have the [Event] attribute
                            // (see Compat comment above)
                            if (method.IsVirtual)
                            {
                                continue;
                            }

                            // If we explicitly mark the method as not being an event, then honor that.
                            if (IsCustomAttributeDefinedHelper(method, typeof(NonEventAttribute), flags))
                                continue;

                            defaultEventAttribute = new EventAttribute(eventId);
                            eventAttribute = defaultEventAttribute;
                        }
                        else if (eventAttribute.EventId <= 0)
                        {
                            manifest.ManifestError(SR.EventSource_NeedPositiveId, true);
                            continue;   // don't validate anything else for this event
                        }
                        if (method.Name.LastIndexOf('.') >= 0)
                        {
                            manifest.ManifestError(SR.Format(SR.EventSource_EventMustNotBeExplicitImplementation, method.Name, eventAttribute.EventId));
                        }

                        eventId++;
                        string eventName = method.Name;

                        if (eventAttribute.Opcode == EventOpcode.Info)      // We are still using the default opcode.
                        {
                            // By default pick a task ID derived from the EventID, starting with the highest task number and working back
                            bool noTask = (eventAttribute.Task == EventTask.None);
                            if (noTask)
                                eventAttribute.Task = (EventTask)(0xFFFE - eventAttribute.EventId);

                            // Unless we explicitly set the opcode to Info (to override the auto-generate of Start or Stop opcodes,
                            // pick a default opcode based on the event name (either Info or start or stop if the name ends with that suffix).
                            if (!eventAttribute.IsOpcodeSet)
                                eventAttribute.Opcode = GetOpcodeWithDefault(EventOpcode.Info, eventName);

                            // Make the stop opcode have the same task as the start opcode.
                            if (noTask)
                            {
                                if (eventAttribute.Opcode == EventOpcode.Start)
                                {
                                    if (eventName.EndsWith(ActivityStartSuffix, StringComparison.Ordinal))
                                    {
                                        string taskName = eventName[..^ActivityStartSuffix.Length]; // Remove the Start suffix to get the task name

                                        // Add a task that is just the task name for the start event.   This suppress the auto-task generation
                                        // That would otherwise happen (and create 'TaskName'Start as task name rather than just 'TaskName'
                                        manifest.AddTask(taskName, (int)eventAttribute.Task);
                                    }
                                }
                                else if (eventAttribute.Opcode == EventOpcode.Stop)
                                {
                                    // Find the start associated with this stop event.  We require start to be immediately before the stop
                                    int startEventId = eventAttribute.EventId - 1;
                                    Debug.Assert(0 <= startEventId);
                                    if (eventData != null)
                                    {
                                        ref EventMetadata startEventMetadata = ref CollectionsMarshal.GetValueRefOrNullRef(eventData, startEventId);
                                        if (!Unsafe.IsNullRef(ref startEventMetadata))
                                        {
                                            // If you remove the Stop and add a Start does that name match the Start Event's Name?
                                            // Ideally we would throw an error
                                            if (startEventMetadata.Descriptor.Opcode == (byte)EventOpcode.Start &&
                                                startEventMetadata.Name.EndsWith(ActivityStartSuffix, StringComparison.Ordinal) &&
                                                eventName.EndsWith(ActivityStopSuffix, StringComparison.Ordinal) &&
                                                startEventMetadata.Name.AsSpan()[..^ActivityStartSuffix.Length].SequenceEqual(
                                                    eventName.AsSpan()[..^ActivityStopSuffix.Length]))
                                            {
                                                // Make the stop event match the start event
                                                eventAttribute.Task = (EventTask)startEventMetadata.Descriptor.Task;
                                                noTask = false;
                                            }
                                        }
                                    }
                                    if (noTask && (flags & EventManifestOptions.Strict) != 0)        // Throw an error if we can compatibly.
                                    {
                                        throw new ArgumentException(SR.EventSource_StopsFollowStarts);
                                    }
                                }
                            }
                        }

                        ParameterInfo[] args = method.GetParameters();

                        bool hasRelatedActivityID = RemoveFirstArgIfRelatedActivityId(ref args);
                        if (!(source != null && source.SelfDescribingEvents))
                        {
                            manifest.StartEvent(eventName, eventAttribute);
                            for (int fieldIdx = 0; fieldIdx < args.Length; fieldIdx++)
                            {
                                manifest.AddEventParameter(args[fieldIdx].ParameterType, args[fieldIdx].Name!);
                            }
                            manifest.EndEvent();
                        }

                        if (source != null || (flags & EventManifestOptions.Strict) != 0)
                        {
                            Debug.Assert(eventData != null);
                            // Do checking for user errors (optional, but not a big deal so we do it).
                            DebugCheckEvent(ref eventsByName, eventData, method, eventAttribute, manifest, flags);

                            // add the channel keyword for Event Viewer channel based filters. This is added for creating the EventDescriptors only
                            // and is not required for the manifest
                            if (eventAttribute.Channel != EventChannel.None)
                            {
                                unchecked
                                {
                                    eventAttribute.Keywords |= (EventKeywords)manifest.GetChannelKeyword(eventAttribute.Channel, (ulong)eventAttribute.Keywords);
                                }
                            }

                            if (manifest.HasResources)
                            {
                                string eventKey = "event_" + eventName;
                                if (manifest.GetLocalizedMessage(eventKey, CultureInfo.CurrentUICulture, etwFormat: false) is string msg)
                                {
                                    // overwrite inline message with the localized message
                                    eventAttribute.Message = msg;
                                }
                            }

                            AddEventDescriptor(ref eventData, eventName, eventAttribute, args, hasRelatedActivityID);
                        }
                    }
                }

                // Tell the TraceLogging stuff where to start allocating its own IDs.
                NameInfo.ReserveEventIDsBelow(eventId);

                if (source != null)
                {
                    Debug.Assert(eventData != null);
                    source.m_eventData = eventData;     // officially initialize it. We do this at most once (it is racy otherwise).
                    source.m_channelData = manifest.GetChannelData();
                }

                // if this is an abstract event source we've already performed all the validation we can
                if (!eventSourceType.IsAbstract && (source == null || !source.SelfDescribingEvents))
                {
                    bNeedsManifest = (flags & EventManifestOptions.OnlyIfNeededForRegistration) == 0 || manifest.GetChannelData().Length > 0;

                    // if the manifest is not needed and we're not requested to validate the event source return early
                    if (!bNeedsManifest && (flags & EventManifestOptions.Strict) == 0)
                        return null;

                    res = manifest.CreateManifest();
                    res = (res.Length > 0) ? res : null;
                }
            }
            catch (Exception e)
            {
                // if this is a runtime manifest generation let the exception propagate
                if ((flags & EventManifestOptions.Strict) == 0)
                    throw;
                // else store it to include it in the Argument exception we raise below
                exception = e;
            }

            if ((flags & EventManifestOptions.Strict) != 0 && (manifest?.Errors.Count > 0 || exception != null))
            {
                string msg = string.Empty;

                if (manifest?.Errors.Count > 0)
                {
                    bool firstError = true;
                    foreach (string error in manifest.Errors)
                    {
                        if (!firstError)
                            msg += Environment.NewLine;
                        firstError = false;
                        msg += error;
                    }
                }
                else
                    msg = "Unexpected error: " + exception!.Message;

                throw new ArgumentException(msg, exception);
            }

            return bNeedsManifest ? res : null;
        }

        private static bool RemoveFirstArgIfRelatedActivityId(ref ParameterInfo[] args)
        {
            // If the first parameter is (case insensitive) 'relatedActivityId' then skip it.
            if (args.Length > 0 && args[0].ParameterType == typeof(Guid) &&
                string.Equals(args[0].Name, "relatedActivityId", StringComparison.OrdinalIgnoreCase))
            {
                var newargs = new ParameterInfo[args.Length - 1];
                Array.Copy(args, 1, newargs, 0, args.Length - 1);
                args = newargs;

                return true;
            }

            return false;
        }

        // adds a enumeration (keyword, opcode, task or channel) represented by 'staticField'
        // to the manifest.
        private static void AddProviderEnumKind(ManifestBuilder manifest, FieldInfo staticField, string providerEnumKind)
        {
            bool reflectionOnly = staticField.Module.Assembly.ReflectionOnly;
            Type staticFieldType = staticField.FieldType;
            if (!reflectionOnly && (staticFieldType == typeof(EventOpcode)) || AttributeTypeNamesMatch(staticFieldType, typeof(EventOpcode)))
            {
                if (providerEnumKind != "Opcodes") goto Error;
                int value = (int)staticField.GetRawConstantValue()!;
                manifest.AddOpcode(staticField.Name, value);
            }
            else if (!reflectionOnly && (staticFieldType == typeof(EventTask)) || AttributeTypeNamesMatch(staticFieldType, typeof(EventTask)))
            {
                if (providerEnumKind != "Tasks") goto Error;
                int value = (int)staticField.GetRawConstantValue()!;
                manifest.AddTask(staticField.Name, value);
            }
            else if (!reflectionOnly && (staticFieldType == typeof(EventKeywords)) || AttributeTypeNamesMatch(staticFieldType, typeof(EventKeywords)))
            {
                if (providerEnumKind != "Keywords") goto Error;
                ulong value = unchecked((ulong)(long)staticField.GetRawConstantValue()!);
                manifest.AddKeyword(staticField.Name, value);
            }
            return;
            Error:
            manifest.ManifestError(SR.Format(SR.EventSource_EnumKindMismatch, staticField.FieldType.Name, providerEnumKind));
        }

        // Helper used by code:CreateManifestAndDescriptors to add a code:EventData descriptor for a method
        // with the code:EventAttribute 'eventAttribute'.  resourceManger may be null in which case we populate it
        // it is populated if we need to look up message resources
        private static void AddEventDescriptor(
            [NotNull] ref Dictionary<int, EventMetadata> eventData,
            string eventName,
            EventAttribute eventAttribute,
            ParameterInfo[] eventParameters,
            bool hasRelatedActivityID)
        {
            ref EventMetadata metadata = ref CollectionsMarshal.GetValueRefOrAddDefault(eventData, eventAttribute.EventId, out _);
            metadata.Descriptor = new EventDescriptor(
                    eventAttribute.EventId,
                    eventAttribute.Version,
                    (byte)eventAttribute.Channel,
                    (byte)eventAttribute.Level,
                    (byte)eventAttribute.Opcode,
                    (int)eventAttribute.Task,
                    unchecked((long)((ulong)eventAttribute.Keywords | SessionMask.All.ToEventKeywords())));

            metadata.Tags = eventAttribute.Tags;
            metadata.Name = eventName;
            metadata.Parameters = eventParameters;
            metadata.Message = eventAttribute.Message;
            metadata.ActivityOptions = eventAttribute.ActivityOptions;
            metadata.HasRelatedActivityID = hasRelatedActivityID;
            metadata.EventHandle = IntPtr.Zero;

            // We represent a byte[] with 2 EventData entries: an integer denoting the length and a blob of bytes in the data pointer.
            // This causes a spurious warning because eventDataCount is off by one for the byte[] case.
            // When writing to EventListeners, we want to check that the number of parameters is correct against the byte[] case.
            int eventListenerParameterCount = eventParameters.Length;
            bool allParametersAreInt32 = true;
            bool allParametersAreString = true;

            foreach (ParameterInfo parameter in eventParameters)
            {
                Type dataType = parameter.ParameterType;
                if (dataType == typeof(string))
                {
                    allParametersAreInt32 = false;
                }
                else if (dataType == typeof(int) ||
                    (dataType.IsEnum && Type.GetTypeCode(dataType.GetEnumUnderlyingType()) <= TypeCode.UInt32))
                {
                    // Int32 or an enum with a 1/2/4 byte backing type
                    allParametersAreString = false;
                }
                else
                {
                    if (dataType == typeof(byte[]))
                    {
                        eventListenerParameterCount++;
                    }

                    allParametersAreInt32 = false;
                    allParametersAreString = false;
                }
            }

            metadata.AllParametersAreInt32 = allParametersAreInt32;
            metadata.AllParametersAreString = allParametersAreString;
            metadata.EventListenerParameterCount = eventListenerParameterCount;
        }

        // Helper used by code:EventListener.AddEventSource and code:EventListener.EventListener
        // when a listener gets attached to a eventSource
        internal void AddListener(EventListener listener)
        {
            lock (EventListener.EventListenersLock)
            {
                Dictionary<int, bool>? enabledDict = null;
                if (m_eventData != null)
                {
                    enabledDict = new Dictionary<int, bool>(m_eventData.Count);
                    foreach (int eventId in m_eventData.Keys)
                    {
                        enabledDict[eventId] = false;
                    }
                }
                m_Dispatchers = new EventDispatcher(m_Dispatchers, enabledDict, listener);
                listener.OnEventSourceCreated(this);
            }
        }

        // Helper used by code:CreateManifestAndDescriptors to find user mistakes like reusing an event
        // index for two distinct events etc.  Throws exceptions when it finds something wrong.
        private static void DebugCheckEvent(ref Dictionary<string, string>? eventsByName,
            Dictionary<int, EventMetadata> eventData, MethodInfo method, EventAttribute eventAttribute,
            ManifestBuilder manifest, EventManifestOptions options)
        {
            int evtId = eventAttribute.EventId;
            string evtName = method.Name;
            int eventArg = GetHelperCallFirstArg(method);
            if (eventArg >= 0 && evtId != eventArg)
            {
                manifest.ManifestError(SR.Format(SR.EventSource_MismatchIdToWriteEvent, evtName, evtId, eventArg), true);
            }

            if (eventData.TryGetValue(evtId, out EventMetadata metadata) && metadata.Descriptor.EventId != 0)
            {
                manifest.ManifestError(SR.Format(SR.EventSource_EventIdReused, evtName, evtId), true);
            }

            // We give a task to things if they don't have one.
            // TODO this is moderately expensive (N*N).   We probably should not even bother....
            Debug.Assert(eventAttribute.Task != EventTask.None || eventAttribute.Opcode != EventOpcode.Info);
            foreach (int idx in eventData.Keys)
            {
                // skip unused Event IDs.
                if (eventData[idx].Name == null)
                    continue;

                if (eventData[idx].Descriptor.Task == (int)eventAttribute.Task && eventData[idx].Descriptor.Opcode == (int)eventAttribute.Opcode)
                {
                    manifest.ManifestError(SR.Format(SR.EventSource_TaskOpcodePairReused,
                                            evtName, evtId, eventData[idx].Name, idx));
                    // If we are not strict stop on first error.   We have had problems with really large providers taking forever.  because of many errors.
                    if ((options & EventManifestOptions.Strict) == 0)
                        break;
                }
            }

            // for non-default event opcodes the user must define a task!
            if (eventAttribute.Opcode != EventOpcode.Info)
            {
                bool failure = false;
                if (eventAttribute.Task == EventTask.None)
                    failure = true;
                else
                {
                    // If you have the auto-assigned Task, then you did not explicitly set one.
                    // This is OK for Start events because we have special logic to assign the task to a prefix derived from the event name
                    // But all other cases we want to catch the omission.
                    var autoAssignedTask = (EventTask)(0xFFFE - evtId);
                    if (eventAttribute.Opcode != EventOpcode.Start && eventAttribute.Opcode != EventOpcode.Stop && eventAttribute.Task == autoAssignedTask)
                        failure = true;
                }
                if (failure)
                {
                    manifest.ManifestError(SR.Format(SR.EventSource_EventMustHaveTaskIfNonDefaultOpcode, evtName, evtId));
                }
            }

            // If we ever want to enforce the rule: MethodName = TaskName + OpcodeName here's how:
            //  (the reason we don't is backwards compat and the need for handling this as a non-fatal error
            //  by eventRegister.exe)
            // taskName & opcodeName could be passed in by the caller which has opTab & taskTab handy
            // if (!(((int)eventAttribute.Opcode == 0 && evtName == taskName) || (evtName == taskName+opcodeName)))
            // {
            //     throw new WarningException(SR.EventSource_EventNameDoesNotEqualTaskPlusOpcode);
            // }

            eventsByName ??= new Dictionary<string, string>();

            if (eventsByName.ContainsKey(evtName))
            {
                manifest.ManifestError(SR.Format(SR.EventSource_EventNameReused, evtName), true);
            }

            eventsByName[evtName] = evtName;
        }

        /// <summary>
        /// This method looks at the IL and tries to pattern match against the standard
        /// 'boilerplate' event body
        /// <code>
        ///     { if (Enabled()) WriteEvent(#, ...) }
        /// </code>
        /// If the pattern matches, it returns the literal number passed as the first parameter to
        /// the WriteEvent.  This is used to find common user errors (mismatching this
        /// number with the EventAttribute ID).  It is only used for validation.
        /// </summary>
        /// <param name="method">The method to probe.</param>
        /// <returns>The literal value or -1 if the value could not be determined. </returns>
#if !NATIVEAOT
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                    Justification = "The method calls MethodBase.GetMethodBody. Trimming application can change IL of various methods" +
                                    "which can lead to change of behavior. This method only uses this to validate usage of event source APIs." +
                                    "In the worst case it will not be able to determine the value it's looking for and will not perform" +
                                    "any validation.")]
#endif
        private static int GetHelperCallFirstArg(MethodInfo method)
        {
#if !NATIVEAOT
            // Currently searches for the following pattern
            //
            // ...     // CAN ONLY BE THE INSTRUCTIONS BELOW
            // LDARG0
            // LDC.I4 XXX
            // ...     // CAN ONLY BE THE INSTRUCTIONS BELOW CAN'T BE A BRANCH OR A CALL
            // CALL
            // NOP     // 0 or more times
            // RET
            //
            // If we find this pattern we return the XXX.  Otherwise we return -1.
            byte[] instrs = method.GetMethodBody()!.GetILAsByteArray()!;
            int retVal = -1;
            for (int idx = 0; idx < instrs.Length;)
            {
                switch (instrs[idx])
                {
                    case 0: // NOP
                    case 1: // BREAK
                    case 2: // LDARG_0
                    case 3: // LDARG_1
                    case 4: // LDARG_2
                    case 5: // LDARG_3
                    case 6: // LDLOC_0
                    case 7: // LDLOC_1
                    case 8: // LDLOC_2
                    case 9: // LDLOC_3
                    case 10: // STLOC_0
                    case 11: // STLOC_1
                    case 12: // STLOC_2
                    case 13: // STLOC_3
                        break;
                    case 14: // LDARG_S
                    case 16: // STARG_S
                        idx++;
                        break;
                    case 20: // LDNULL
                        break;
                    case 21: // LDC_I4_M1
                    case 22: // LDC_I4_0
                    case 23: // LDC_I4_1
                    case 24: // LDC_I4_2
                    case 25: // LDC_I4_3
                    case 26: // LDC_I4_4
                    case 27: // LDC_I4_5
                    case 28: // LDC_I4_6
                    case 29: // LDC_I4_7
                    case 30: // LDC_I4_8
                        if (idx > 0 && instrs[idx - 1] == 2)  // preceded by LDARG0
                            retVal = instrs[idx] - 22;
                        break;
                    case 31: // LDC_I4_S
                        if (idx > 0 && instrs[idx - 1] == 2)  // preceded by LDARG0
                            retVal = instrs[idx + 1];
                        idx++;
                        break;
                    case 32: // LDC_I4
                        idx += 4;
                        break;
                    case 37: // DUP
                        break;
                    case 40: // CALL
                        idx += 4;

                        if (retVal >= 0)
                        {
                            // Is this call just before return?
                            for (int search = idx + 1; search < instrs.Length; search++)
                            {
                                if (instrs[search] == 42)  // RET
                                    return retVal;
                                if (instrs[search] != 0)   // NOP
                                    break;
                            }
                        }
                        retVal = -1;
                        break;
                    case 44: // BRFALSE_S
                    case 45: // BRTRUE_S
                        retVal = -1;
                        idx++;
                        break;
                    case 57: // BRFALSE
                    case 58: // BRTRUE
                        retVal = -1;
                        idx += 4;
                        break;
                    case 103: // CONV_I1
                    case 104: // CONV_I2
                    case 105: // CONV_I4
                    case 106: // CONV_I8
                    case 109: // CONV_U4
                    case 110: // CONV_U8
                        break;
                    case 140: // BOX
                    case 141: // NEWARR
                        idx += 4;
                        break;
                    case 162: // STELEM_REF
                        break;
                    case 254: // PREFIX
                        idx++;
                        // Covers the CEQ instructions used in debug code for some reason.
                        if (idx >= instrs.Length || instrs[idx] >= 6)
                            goto default;
                        break;
                    default:
                        /* Debug.Fail("Warning: User validation code sub-optimial: Unsupported opcode " + instrs[idx] +
                            " at " + idx + " in method " + method.Name); */
                        return -1;
                }
                idx++;
            }
#endif
            return -1;
        }

        /// <summary>
        /// Sends an error message to the debugger (outputDebugString), as well as the EventListeners
        /// It will do this even if the EventSource is not enabled.
        /// </summary>
        internal void ReportOutOfBandMessage(string msg)
        {
            try
            {
                if (m_outOfBandMessageCount < 16 - 1)     // Note this is only if size byte
                {
                    m_outOfBandMessageCount++;
                }
                else
                {
                    if (m_outOfBandMessageCount == 16)
                        return;
                    m_outOfBandMessageCount = 16;    // Mark that we hit the limit.  Notify them that this is the case.
                    msg = "Reached message limit.   End of EventSource error messages.";
                }

                // send message to debugger
                Debugger.Log(0, null, $"EventSource Error: {msg}{Environment.NewLine}");

                // Send it to all listeners.
                WriteEventString(msg);
                WriteStringToAllListeners("EventSourceMessage", msg);
            }
            catch { }      // If we fail during last chance logging, well, we have to give up....
        }

        private static EventSourceSettings ValidateSettings(EventSourceSettings settings)
        {
            const EventSourceSettings evtFormatMask = EventSourceSettings.EtwManifestEventFormat |
                                EventSourceSettings.EtwSelfDescribingEventFormat;
            if ((settings & evtFormatMask) == evtFormatMask)
            {
                throw new ArgumentException(SR.EventSource_InvalidEventFormat, nameof(settings));
            }

            // If you did not explicitly ask for manifest, you get self-describing.
            if ((settings & evtFormatMask) == 0)
                settings |= EventSourceSettings.EtwSelfDescribingEventFormat;
            return settings;
        }

        private bool ThrowOnEventWriteErrors => (m_config & EventSourceSettings.ThrowOnEventWriteErrors) != 0;

        private bool SelfDescribingEvents
        {
            get
            {
                Debug.Assert(((m_config & EventSourceSettings.EtwManifestEventFormat) != 0) !=
                                ((m_config & EventSourceSettings.EtwSelfDescribingEventFormat) != 0));
                return (m_config & EventSourceSettings.EtwSelfDescribingEventFormat) != 0;
            }
        }

#if NATIVEAOT
        // If EventSource feature is enabled, default EventSources need to be initialized for NativeAOT
        // In CoreCLR, this is done via a call from the runtime as part of coreclr_initialize
#pragma warning disable CA2255
        [ModuleInitializer]
#pragma warning restore CA2255
#endif
        internal static void InitializeDefaultEventSources()
        {
            if (!EventSource.IsSupported)
            {
                return;
            }

// NOTE: this define is being used inconsistently. Most places mean just EventPipe support, but then a few places use
// it to mean other aspects of tracing such as these EventSources.
#if FEATURE_PERFTRACING
            _ = NativeRuntimeEventSource.Log;
#if !TARGET_BROWSER
            _ = RuntimeEventSource.Log;
#endif
#endif
            // System.Diagnostics.MetricsEventSource allows listening to Meters and indirectly
            // also creates the System.Runtime Meter.

            // Functionally we could preregister NativeRuntimeEventSource and RuntimeEventSource as well, but it may not provide
            // much benefit. The main benefit for MetricsEventSource is that the app may never use it and it defers
            // pulling the System.Diagnostics.DiagnosticSource assembly into the process until it is needed.
            if (IsMeterSupported)
            {
                const string name = "System.Diagnostics.Metrics";
                Guid id = new Guid("20752bc4-c151-50f5-f27b-df92d8af5a61");
                EventSourceInitHelper.PreregisterEventProviders(id, name, EventSourceInitHelper.GetMetricsEventSource);
            }
        }
#endregion
    }

    // This type is logically just more static EventSource functionality but it needs to be a separate class
    // to ensure that the IL linker can remove unused methods in it. Methods defined within the EventSource type
    // are never removed because EventSource has the potential to reflect over its own members.
    internal static class EventSourceInitHelper
    {
        private static List<Func<EventSource?>> s_preregisteredEventSourceFactories = new List<Func<EventSource?>>();
        private static readonly Dictionary<Guid, EventSource.OverrideEventProvider> s_preregisteredEtwProviders = new Dictionary<Guid, EventSource.OverrideEventProvider>();
#if FEATURE_PERFTRACING
        private static readonly Dictionary<string, EventSource.OverrideEventProvider> s_preregisteredEventPipeProviders = new Dictionary<string, EventSource.OverrideEventProvider>();
#endif

        internal static EventSource? GetMetricsEventSource()
        {
            return GetInstance(null) as EventSource;

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetInstance")]
            [return: UnsafeAccessorType("System.Diagnostics.Metrics.MetricsEventSource, System.Diagnostics.DiagnosticSource")]
            static extern object GetInstance(
                [UnsafeAccessorType("System.Diagnostics.Metrics.MetricsEventSource, System.Diagnostics.DiagnosticSource")] object? _);
        }

        // Pre-registration creates and registers an EventProvider prior to the EventSource being constructed.
        // If a tracing session is started using the provider then the EventSource will be constructed on demand.
        internal static unsafe void PreregisterEventProviders(Guid id, string name, Func<EventSource?> eventSourceFactory)
        {
            // NOTE: Pre-registration has some minor limitations and variations to normal EventSource behavior:
            // 1. Instead of delivering OnEventCommand callbacks during the EventSource constructor it may deliver them after
            //    the constructor instead. This occurs because the EventProvider callback might create the EventSource instance
            //    in the factory method, then deliver the callback.
            // 2. EventSource traits aren't supported. Normally EventSource.Initialize() would init provider metadata including
            //    traits but the SetInformation() call we use below generates metadata from name only. If we want traits support
            //    in the future it could be added.

            // NOTE: You might think this preregister logic could be simplified by using an Action to create the EventSource instead of
            // Func<EventSource> and then allow the EventSource to initialize as normal. This doesn't work however because calling
            // EtwEventProvider.Register() inside of an ETW callback deadlocks. Instead we have to bind the EventSource to the
            // EtwEventProvider that was already registered and use the callback we got on that provider to invoke EventSource.SendCommand().
            try
            {
                s_preregisteredEventSourceFactories.Add(eventSourceFactory);

                EventSource.OverrideEventProvider etwProvider = new EventSource.OverrideEventProvider(eventSourceFactory, EventProviderType.ETW);
                etwProvider.Register(id, name);
#if TARGET_WINDOWS
                byte[] providerMetadata = Statics.MetadataForString(name, 0, 0, 0);
                fixed (byte* pMetadata = providerMetadata)
                {
                    etwProvider.SetInformation(
                        Interop.Advapi32.EVENT_INFO_CLASS.SetTraits,
                        pMetadata,
                        (uint)providerMetadata.Length);
                }
#endif // TARGET_WINDOWS
                lock (s_preregisteredEtwProviders)
                {
                    s_preregisteredEtwProviders[id] = etwProvider;
                }

#if FEATURE_PERFTRACING
                EventSource.OverrideEventProvider eventPipeProvider = new EventSource.OverrideEventProvider(eventSourceFactory, EventProviderType.EventPipe);
                eventPipeProvider.Register(id, name);
                lock (s_preregisteredEventPipeProviders)
                {
                    s_preregisteredEventPipeProviders[name] = eventPipeProvider;
                }
#endif
            }
            catch (Exception)
            {
                // If there is a failure registering then the normal EventSource.Initialize() path can try to register
                // again if/when the EventSource is constructed.
            }
        }

        internal static void EnsurePreregisteredEventSourcesExist()
        {
            if (!EventSource.IsSupported)
            {
                return;
            }

            // In a multi-threaded race its possible that one thread will be creating the EventSources while a 2nd thread
            // exits this function and observes the s_EventSources list without the new EventSources in it.
            // There is no known issue here having a small window of time where the pre-registered EventSources are not in
            // the list as long as we still guarantee they get initialized in the near future and reported to the
            // same EventListener.OnEventSourceCreated() callback.
            Func<EventSource?>[] factories;
            lock (s_preregisteredEventSourceFactories)
            {
                factories = s_preregisteredEventSourceFactories.ToArray();
                s_preregisteredEventSourceFactories.Clear();
            }
            foreach (Func<EventSource?> factory in factories)
            {
                factory();
            }
        }

        internal static EventSource.OverrideEventProvider? TryGetPreregisteredEtwProvider(Guid id)
        {
            lock (s_preregisteredEtwProviders)
            {
                s_preregisteredEtwProviders.Remove(id, out EventSource.OverrideEventProvider? provider);
                return provider;
            }
        }

#if FEATURE_PERFTRACING
        internal static EventSource.OverrideEventProvider? TryGetPreregisteredEventPipeProvider(string name)
        {
            lock (s_preregisteredEventPipeProviders)
            {
                s_preregisteredEventPipeProviders.Remove(name, out EventSource.OverrideEventProvider? provider);
                return provider;
            }
        }
#endif
    }

    /// <summary>
    /// Enables specifying event source configuration options to be used in the EventSource constructor.
    /// </summary>
    [Flags]
    public enum EventSourceSettings
    {
        /// <summary>
        /// This specifies none of the special configuration options should be enabled.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Normally an EventSource NEVER throws; setting this option will tell it to throw when it encounters errors.
        /// </summary>
        ThrowOnEventWriteErrors = 1,
        /// <summary>
        /// Setting this option is a directive to the ETW listener should use manifest-based format when
        /// firing events. This is the default option when defining a type derived from EventSource
        /// (using the protected EventSource constructors).
        /// Only one of EtwManifestEventFormat or EtwSelfDescribingEventFormat should be specified
        /// </summary>
        EtwManifestEventFormat = 4,
        /// <summary>
        /// Setting this option is a directive to the ETW listener should use self-describing event format
        /// when firing events. This is the default option when creating a new instance of the EventSource
        /// type (using the public EventSource constructors).
        /// Only one of EtwManifestEventFormat or EtwSelfDescribingEventFormat should be specified
        /// </summary>
        EtwSelfDescribingEventFormat = 8,
    }

    /// <summary>
    /// An EventListener represents a target for the events generated by EventSources (that is subclasses
    /// of <see cref="EventSource"/>), in the current appdomain. When a new EventListener is created
    /// it is logically attached to all eventSources in that appdomain. When the EventListener is Disposed, then
    /// it is disconnected from the event eventSources. Note that there is a internal list of STRONG references
    /// to EventListeners, which means that relying on the lack of references to EventListeners to clean up
    /// EventListeners will NOT work. You must call EventListener.Dispose explicitly when a dispatcher is no
    /// longer needed.
    /// <para>
    /// Once created, EventListeners can enable or disable on a per-eventSource basis using verbosity levels
    /// (<see cref="EventLevel"/>) and bitfields (<see cref="EventKeywords"/>) to further restrict the set of
    /// events to be sent to the dispatcher. The dispatcher can also send arbitrary commands to a particular
    /// eventSource using the 'SendCommand' method. The meaning of the commands are eventSource specific.
    /// </para><para>
    /// The Null Guid (that is (new Guid()) has special meaning as a wildcard for 'all current eventSources in
    /// the appdomain'. Thus it is relatively easy to turn on all events in the appdomain if desired.
    /// </para><para>
    /// It is possible for there to be many EventListener's defined in a single appdomain. Each dispatcher is
    /// logically independent of the other listeners. Thus when one dispatcher enables or disables events, it
    /// affects only that dispatcher (other listeners get the events they asked for). It is possible that
    /// commands sent with 'SendCommand' would do a semantic operation that would affect the other listeners
    /// (like doing a GC, or flushing data ...), but this is the exception rather than the rule.
    /// </para><para>
    /// Thus the model is that each EventSource keeps a list of EventListeners that it is sending events
    /// to. Associated with each EventSource-dispatcher pair is a set of filtering criteria that determine for
    /// that eventSource what events that dispatcher will receive.
    /// </para><para>
    /// Listeners receive the events on their 'OnEventWritten' method. Thus subclasses of EventListener must
    /// override this method to do something useful with the data.
    /// </para><para>
    /// In addition, when new eventSources are created, the 'OnEventSourceCreate' method is called. The
    /// invariant associated with this callback is that every eventSource gets exactly one
    /// 'OnEventSourceCreate' call for ever eventSource that can potentially send it log messages. In
    /// particular when a EventListener is created, typically a series of OnEventSourceCreate' calls are
    /// made to notify the new dispatcher of all the eventSources that existed before the EventListener was
    /// created.
    /// </para>
    /// </summary>
    public abstract class EventListener : IDisposable
    {
        private event EventHandler<EventSourceCreatedEventArgs>? _EventSourceCreated;

        /// <summary>
        /// This event is raised whenever a new eventSource is 'attached' to the dispatcher.
        /// This can happen for all existing EventSources when the EventListener is created
        /// as well as for any EventSources that come into existence after the EventListener
        /// has been created.
        ///
        /// These 'catch up' events are called during the construction of the EventListener.
        /// Subclasses need to be prepared for that.
        ///
        /// In a multi-threaded environment, it is possible that 'EventSourceEventWrittenCallback'
        /// events for a particular eventSource to occur BEFORE the EventSourceCreatedCallback is issued.
        /// </summary>
        public event EventHandler<EventSourceCreatedEventArgs>? EventSourceCreated
        {
            add
            {
                CallBackForExistingEventSources(false, value);

                this._EventSourceCreated = (EventHandler<EventSourceCreatedEventArgs>?)Delegate.Combine(_EventSourceCreated, value);
            }
            remove
            {
                this._EventSourceCreated = (EventHandler<EventSourceCreatedEventArgs>?)Delegate.Remove(_EventSourceCreated, value);
            }
        }

        /// <summary>
        /// This event is raised whenever an event has been written by a EventSource for which
        /// the EventListener has enabled events.
        /// </summary>
        public event EventHandler<EventWrittenEventArgs>? EventWritten;

        /// <summary>
        /// Create a new EventListener in which all events start off turned off (use EnableEvents to turn
        /// them on).
        /// </summary>
        protected EventListener()
        {
            // This will cause the OnEventSourceCreated callback to fire.
            CallBackForExistingEventSources(true, (obj, args) =>
                args.EventSource!.AddListener((EventListener)obj!));
        }

        /// <summary>
        /// Dispose should be called when the EventListener no longer desires 'OnEvent*' callbacks. Because
        /// there is an internal list of strong references to all EventListeners, calling 'Dispose' directly
        /// is the only way to actually make the listen die. Thus it is important that users of EventListener
        /// call Dispose when they are done with their logging.
        /// </summary>
        public virtual void Dispose()
        {
            lock (EventListenersLock)
            {
                if (s_Listeners != null)
                {
                    if (this == s_Listeners)
                    {
                        EventListener cur = s_Listeners;
                        s_Listeners = this.m_Next;
                        RemoveReferencesToListenerInEventSources(cur);
                    }
                    else
                    {
                        // Find 'this' from the s_Listeners linked list.
                        EventListener prev = s_Listeners;
                        while (true)
                        {
                            EventListener? cur = prev.m_Next;
                            if (cur == null)
                                break;
                            if (cur == this)
                            {
                                // Found our Listener, remove references to it in the eventSources
                                prev.m_Next = cur.m_Next;       // Remove entry.
                                RemoveReferencesToListenerInEventSources(cur);
                                break;
                            }
                            prev = cur;
                        }
                    }
                }
                Validate();
            }

#if FEATURE_PERFTRACING
            // Remove the listener from the EventPipe dispatcher. EventCommand.Update with enable==false removes it.
            EventPipeEventDispatcher.Instance.SendCommand(this, EventCommand.Update, false, EventLevel.LogAlways, (EventKeywords)0);
#endif // FEATURE_PERFTRACING
        }
        // We don't expose a Dispose(bool), because the contract is that you don't have any non-syncronous
        // 'cleanup' associated with this object

        /// <summary>
        /// Enable all events from the eventSource identified by 'eventSource' to the current
        /// dispatcher that have a verbosity level of 'level' or lower.
        ///
        /// This call can have the effect of REDUCING the number of events sent to the
        /// dispatcher if 'level' indicates a less verbose level than was previously enabled.
        ///
        /// This call never has an effect on other EventListeners.
        ///
        /// </summary>
        public void EnableEvents(EventSource eventSource, EventLevel level)
        {
            EnableEvents(eventSource, level, EventKeywords.None);
        }
        /// <summary>
        /// Enable all events from the eventSource identified by 'eventSource' to the current
        /// dispatcher that have a verbosity level of 'level' or lower and have a event keyword
        /// matching any of the bits in 'matchAnyKeyword'.
        ///
        /// This call can have the effect of REDUCING the number of events sent to the
        /// dispatcher if 'level' indicates a less verbose level than was previously enabled or
        /// if 'matchAnyKeyword' has fewer keywords set than where previously set.
        ///
        /// This call never has an effect on other EventListeners.
        /// </summary>
        public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword)
        {
            EnableEvents(eventSource, level, matchAnyKeyword, null);
        }
        /// <summary>
        /// Enable all events from the eventSource identified by 'eventSource' to the current
        /// dispatcher that have a verbosity level of 'level' or lower and have a event keyword
        /// matching any of the bits in 'matchAnyKeyword' as well as any (eventSource specific)
        /// effect passing additional 'key-value' arguments 'arguments' might have.
        ///
        /// This call can have the effect of REDUCING the number of events sent to the
        /// dispatcher if 'level' indicates a less verbose level than was previously enabled or
        /// if 'matchAnyKeyword' has fewer keywords set than where previously set.
        ///
        /// This call never has an effect on other EventListeners.
        /// </summary>
        public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string?>? arguments)
        {
            ArgumentNullException.ThrowIfNull(eventSource);

            eventSource.SendCommand(this, EventProviderType.None, 0, EventCommand.Update, true, level, matchAnyKeyword, arguments);

#if FEATURE_PERFTRACING
            if (eventSource.GetType() == typeof(NativeRuntimeEventSource))
            {
                EventPipeEventDispatcher.Instance.SendCommand(this, EventCommand.Update, true, level, matchAnyKeyword);
            }
#endif // FEATURE_PERFTRACING
        }
        /// <summary>
        /// Disables all events coming from eventSource identified by 'eventSource'.
        ///
        /// This call never has an effect on other EventListeners.
        /// </summary>
        public void DisableEvents(EventSource eventSource)
        {
            ArgumentNullException.ThrowIfNull(eventSource);

            eventSource.SendCommand(this, EventProviderType.None, 0, EventCommand.Update, false, EventLevel.LogAlways, EventKeywords.None, null);

#if FEATURE_PERFTRACING
            if (eventSource.GetType() == typeof(NativeRuntimeEventSource))
            {
                EventPipeEventDispatcher.Instance.SendCommand(this, EventCommand.Update, false, EventLevel.LogAlways, EventKeywords.None);
            }
#endif // FEATURE_PERFTRACING
        }

        /// <summary>
        /// EventSourceIndex is small non-negative integer (suitable for indexing in an array)
        /// identifying EventSource. It is unique per-appdomain. Some EventListeners might find
        /// it useful to store additional information about each eventSource connected to it,
        /// and EventSourceIndex allows this extra information to be efficiently stored in a
        /// (growable) array (eg List(T)).
        /// </summary>
        protected internal static int EventSourceIndex(EventSource eventSource) { return eventSource.m_id; }

        /// <summary>
        /// This method is called whenever a new eventSource is 'attached' to the dispatcher.
        /// This can happen for all existing EventSources when the EventListener is created
        /// as well as for any EventSources that come into existence after the EventListener
        /// has been created.
        ///
        /// These 'catch up' events are called during the construction of the EventListener.
        /// Subclasses need to be prepared for that.
        ///
        /// In a multi-threaded environment, it is possible that 'OnEventWritten' callbacks
        /// for a particular eventSource to occur BEFORE the OnEventSourceCreated is issued.
        /// </summary>
        /// <param name="eventSource"></param>
        protected internal virtual void OnEventSourceCreated(EventSource eventSource)
        {
            EventHandler<EventSourceCreatedEventArgs>? callBack = this._EventSourceCreated;
            if (callBack != null)
            {
                EventSourceCreatedEventArgs args = new EventSourceCreatedEventArgs();
                args.EventSource = eventSource;
                callBack(this, args);
            }
        }

        /// <summary>
        /// This method is called whenever an event has been written by a EventSource for which
        /// the EventListener has enabled events.
        /// </summary>
        /// <param name="eventData"></param>
        protected internal virtual void OnEventWritten(EventWrittenEventArgs eventData)
        {
            this.EventWritten?.Invoke(this, eventData);
        }

#region private
        /// <summary>
        /// This routine adds newEventSource to the global list of eventSources, it also assigns the
        /// ID to the eventSource (which is simply the ordinal in the global list).
        ///
        /// EventSources currently do not pro-actively remove themselves from this list. Instead
        /// when eventSources's are GCed, the weak handle in this list naturally gets nulled, and
        /// we will reuse the slot. Today this list never shrinks (but we do reuse entries
        /// that are in the list). This seems OK since the expectation is that EventSources
        /// tend to live for the lifetime of the appdomain anyway (they tend to be used in
        /// global variables).
        /// </summary>
        /// <param name="newEventSource"></param>
        internal static void AddEventSource(EventSource newEventSource)
        {
            lock (EventListenersLock)
            {
                Debug.Assert(s_EventSources != null);

                // Periodically search the list for existing entries to reuse, this avoids
                // unbounded memory use if we keep recycling eventSources (an unlikely thing).
                int newIndex = -1;
                if (s_EventSources.Count % 64 == 63)   // on every block of 64, fill up the block before continuing
                {
                    int i = s_EventSources.Count;      // Work from the top down.
                    while (0 < i)
                    {
                        --i;
                        WeakReference<EventSource> weakRef = s_EventSources[i];
                        if (!weakRef.TryGetTarget(out _))
                        {
                            newIndex = i;
                            weakRef.SetTarget(newEventSource);
                            break;
                        }
                    }
                }
                if (newIndex < 0)
                {
                    newIndex = s_EventSources.Count;
                    s_EventSources.Add(new WeakReference<EventSource>(newEventSource));
                }
                newEventSource.m_id = newIndex;

#if DEBUG
                // Disable validation of EventSource/EventListener connections in case a call to EventSource.AddListener
                // causes a recursive call into this method.
                bool previousValue = s_ConnectingEventSourcesAndListener;
                s_ConnectingEventSourcesAndListener = true;
                try
                {
#endif
                    // Add every existing dispatcher to the new EventSource
                    for (EventListener? listener = s_Listeners; listener != null; listener = listener.m_Next)
                        newEventSource.AddListener(listener);
#if DEBUG
                }
                finally
                {
                    s_ConnectingEventSourcesAndListener = previousValue;
                }
#endif

                Validate();
            }
        }

        // Whenever we have async callbacks from native code, there is an ugly issue where
        // during .NET shutdown native code could be calling the callback, but the CLR
        // has already prohibited callbacks to managed code in the appdomain, causing the CLR
        // to throw a COMPLUS_BOOT_EXCEPTION.   The guideline we give is that you must unregister
        // such callbacks on process shutdown or appdomain so that unmanaged code will never
        // do this.  This is what this callback is for.
        // See bug 724140 for more
        internal static void DisposeOnShutdown()
        {
            Debug.Assert(EventSource.IsSupported);
            List<EventSource> sourcesToDispose = new List<EventSource>();
            lock (EventListenersLock)
            {
                Debug.Assert(s_EventSources != null);
                foreach (WeakReference<EventSource> esRef in s_EventSources)
                {
                    if (esRef.TryGetTarget(out EventSource? es))
                    {
                        sourcesToDispose.Add(es);
                    }
                }
            }

            // Do not invoke Dispose under the lock as this can lead to a deadlock.
            // See https://github.com/dotnet/runtime/issues/48342 for details.
            Debug.Assert(!Monitor.IsEntered(EventListenersLock));
            foreach (EventSource es in sourcesToDispose)
            {
                es.Dispose();
            }
        }

        // If an EventListener calls Dispose without calling DisableEvents first we want to issue the Disable command now
        private static void CallDisableEventsIfNecessary(EventDispatcher eventDispatcher, EventSource eventSource)
        {
#if DEBUG
            // Disable validation of EventSource/EventListener connections in case a call to EventSource.AddListener
            // causes a recursive call into this method.
            bool previousValue = s_ConnectingEventSourcesAndListener;
            s_ConnectingEventSourcesAndListener = true;
            try
            {
#endif
                if (eventDispatcher.m_EventEnabled == null)
                {
                    return;
                }

                foreach (bool value in eventDispatcher.m_EventEnabled.Values)
                {
                    if (value)
                    {
                        eventDispatcher.m_Listener.DisableEvents(eventSource);
                    }
                }
#if DEBUG
            }
            finally
            {
                s_ConnectingEventSourcesAndListener = previousValue;
            }
#endif
        }

        /// <summary>
        /// Helper used in code:Dispose that removes any references to 'listenerToRemove' in any of the
        /// eventSources in the appdomain.
        ///
        /// The EventListenersLock must be held before calling this routine.
        /// </summary>
        private static void RemoveReferencesToListenerInEventSources(EventListener listenerToRemove)
        {
            Debug.Assert(Monitor.IsEntered(EventListenersLock));
            // Foreach existing EventSource in the appdomain
            Debug.Assert(s_EventSources != null);

            // First pass to call DisableEvents
            WeakReference<EventSource>[] eventSourcesSnapshot = s_EventSources.ToArray();
            foreach (WeakReference<EventSource> eventSourceRef in eventSourcesSnapshot)
            {
                if (eventSourceRef.TryGetTarget(out EventSource? eventSource))
                {
                    EventDispatcher? cur = eventSource.m_Dispatchers;
                    while (cur != null)
                    {
                        if (cur.m_Listener == listenerToRemove)
                        {
                            CallDisableEventsIfNecessary(cur!, eventSource);
                        }

                        cur = cur.m_Next;
                    }
                }
            }

            // DisableEvents can call back to user code and we have to start over since s_EventSources and
            // eventSource.m_Dispatchers could have mutated
            foreach (WeakReference<EventSource> eventSourceRef in s_EventSources)
            {
                if (eventSourceRef.TryGetTarget(out EventSource? eventSource)
                    && eventSource.m_Dispatchers != null)
                {
                    // Is the first output dispatcher the dispatcher we are removing?
                    if (eventSource.m_Dispatchers.m_Listener == listenerToRemove)
                    {
                        eventSource.m_Dispatchers = eventSource.m_Dispatchers.m_Next;
                    }
                    else
                    {
                        // Remove 'listenerToRemove' from the eventSource.m_Dispatchers linked list.
                        EventDispatcher? prev = eventSource.m_Dispatchers;
                        while (true)
                        {
                            EventDispatcher? cur = prev.m_Next;
                            if (cur == null)
                            {
                                Debug.Fail("EventSource did not have a registered EventListener!");
                                break;
                            }
                            if (cur.m_Listener == listenerToRemove)
                            {
                                prev.m_Next = cur.m_Next;       // Remove entry.
                                break;
                            }
                            prev = cur;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Checks internal consistency of EventSources/Listeners.
        /// </summary>
        [Conditional("DEBUG")]
        internal static void Validate()
        {
#if DEBUG
            // Don't run validation code if we're in the middle of modifying the connections between EventSources and EventListeners.
            if (s_ConnectingEventSourcesAndListener)
            {
                return;
            }
#endif

            lock (EventListenersLock)
            {
                Debug.Assert(s_EventSources != null);
                // Get all listeners
                Dictionary<EventListener, bool> allListeners = new Dictionary<EventListener, bool>();
                EventListener? cur = s_Listeners;
                while (cur != null)
                {
                    allListeners.Add(cur, true);
                    cur = cur.m_Next;
                }

                // For all eventSources
                int id = -1;
                foreach (WeakReference<EventSource> eventSourceRef in s_EventSources)
                {
                    id++;
                    if (!eventSourceRef.TryGetTarget(out EventSource? eventSource))
                        continue;
                    Debug.Assert(eventSource.m_id == id, "Unexpected event source ID.");

                    // None listeners on eventSources exist in the dispatcher list.
                    EventDispatcher? dispatcher = eventSource.m_Dispatchers;
                    while (dispatcher != null)
                    {
                        Debug.Assert(allListeners.ContainsKey(dispatcher.m_Listener), "EventSource has a listener not on the global list.");
                        dispatcher = dispatcher.m_Next;
                    }

                    // Every dispatcher is on Dispatcher List of every eventSource.
                    foreach (EventListener listener in allListeners.Keys)
                    {
                        dispatcher = eventSource.m_Dispatchers;
                        while (true)
                        {
                            Debug.Assert(dispatcher != null, "Listener is not on all eventSources.");
                            if (dispatcher.m_Listener == listener)
                                break;
                            dispatcher = dispatcher.m_Next;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a global lock that is intended to protect the code:s_Listeners linked list and the
        /// code:s_EventSources list.  (We happen to use the s_EventSources list as the lock object)
        /// </summary>
        internal static object EventListenersLock
        {
            get
            {
                if (s_EventSources == null)
                {
                    Interlocked.CompareExchange(ref s_EventSources, new List<WeakReference<EventSource>>(2), null);
                }
                return s_EventSources;
            }
        }

        private void CallBackForExistingEventSources(bool addToListenersList, EventHandler<EventSourceCreatedEventArgs>? callback)
        {
            // Pre-registered EventSources may not have been constructed yet but we need to do so now to ensure they are
            // reported to the EventListener.
            EventSourceInitHelper.EnsurePreregisteredEventSourcesExist();

            lock (EventListenersLock)
            {
                Debug.Assert(s_EventSources != null);

                // Disallow creating EventListener reentrancy.
                if (s_CreatingListener)
                {
                    throw new InvalidOperationException(SR.EventSource_ListenerCreatedInsideCallback);
                }

                try
                {
                    s_CreatingListener = true;

                    if (addToListenersList)
                    {
                        // Add to list of listeners in the system, do this BEFORE firing the 'OnEventSourceCreated' so that
                        // Those added sources see this listener.
                        this.m_Next = s_Listeners;
                        s_Listeners = this;
                    }

                    if (callback != null)
                    {
                        // Find all existing eventSources call OnEventSourceCreated to 'catchup'
                        // Note that we DO have reentrancy here because 'AddListener' calls out to user code (via OnEventSourceCreated callback)
                        // We tolerate this by iterating over a copy of the list here. New event sources will take care of adding listeners themselves
                        // EventSources are not guaranteed to be added at the end of the s_EventSource list -- We re-use slots when a new source
                        // is created.
                        WeakReference<EventSource>[] eventSourcesSnapshot = s_EventSources.ToArray();

#if DEBUG
                        bool previousValue = s_ConnectingEventSourcesAndListener;
                        s_ConnectingEventSourcesAndListener = true;
                        try
                        {
#endif
                            for (int i = 0; i < eventSourcesSnapshot.Length; i++)
                            {
                                WeakReference<EventSource> eventSourceRef = eventSourcesSnapshot[i];
                                if (eventSourceRef.TryGetTarget(out EventSource? eventSource))
                                {
                                    EventSourceCreatedEventArgs args = new EventSourceCreatedEventArgs();
                                    args.EventSource = eventSource;
                                    callback(this, args);
                                }
                            }
#if DEBUG
                        }
                        finally
                        {
                            s_ConnectingEventSourcesAndListener = previousValue;
                        }
#endif
                    }

                    Validate();
                }
                finally
                {
                    s_CreatingListener = false;
                }
            }
        }

        // Instance fields
        internal volatile EventListener? m_Next;                         // These form a linked list in s_Listeners

        // static fields

        /// <summary>
        /// The list of all listeners in the appdomain.  Listeners must be explicitly disposed to remove themselves
        /// from this list.   Note that EventSources point to their listener but NOT the reverse.
        /// </summary>
        internal static EventListener? s_Listeners;
        /// <summary>
        /// The list of all active eventSources in the appdomain.  Note that eventSources do NOT
        /// remove themselves from this list this is a weak list and the GC that removes them may
        /// not have happened yet.  Thus it can contain event sources that are dead (thus you have
        /// to filter those out.
        /// </summary>
        internal static List<WeakReference<EventSource>>? s_EventSources;

        /// <summary>
        /// Used to disallow reentrancy.
        /// </summary>
        private static bool s_CreatingListener;

#if DEBUG
        /// <summary>
        /// Used to disable validation of EventSource and EventListener connectivity.
        /// This is needed when an EventListener is in the middle of being published to all EventSources
        /// and another EventSource is created as part of the process.
        /// </summary>
        [ThreadStatic]
        private static bool s_ConnectingEventSourcesAndListener;
#endif

#endregion
    }

    /// <summary>
    /// Passed to the code:EventSource.OnEventCommand callback
    /// </summary>
    public class EventCommandEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the command for the callback.
        /// </summary>
        public EventCommand Command { get; internal set; }

        /// <summary>
        /// Gets the arguments for the callback.
        /// </summary>
        public IDictionary<string, string?>? Arguments { get; internal set; }

        /// <summary>
        /// Enables the event that has the specified identifier.
        /// </summary>
        /// <param name="eventId">Event ID of event to be enabled</param>
        /// <returns>true if eventId is in range</returns>
        public bool EnableEvent(int eventId)
        {
            if (Command != EventCommand.Enable && Command != EventCommand.Disable)
                throw new InvalidOperationException();
            return eventSource.EnableEventForDispatcher(dispatcher, eventProviderType, eventId, true);
        }

        /// <summary>
        /// Disables the event that have the specified identifier.
        /// </summary>
        /// <param name="eventId">Event ID of event to be disabled</param>
        /// <returns>true if eventId is in range</returns>
        public bool DisableEvent(int eventId)
        {
            if (Command != EventCommand.Enable && Command != EventCommand.Disable)
                throw new InvalidOperationException();
            return eventSource.EnableEventForDispatcher(dispatcher, eventProviderType, eventId, false);
        }

#region private

        internal EventCommandEventArgs(EventCommand command, IDictionary<string, string?>? arguments, EventSource eventSource,
            EventListener? listener, EventProviderType eventProviderType, int perEventSourceSessionId, bool enable, EventLevel level, EventKeywords matchAnyKeyword)
        {
            this.Command = command;
            this.Arguments = arguments;
            this.eventSource = eventSource;
            this.listener = listener;
            this.eventProviderType = eventProviderType;
            this.perEventSourceSessionId = perEventSourceSessionId;
            this.enable = enable;
            this.level = level;
            this.matchAnyKeyword = matchAnyKeyword;
        }

        internal EventSource eventSource;
        internal EventDispatcher? dispatcher;
        internal EventProviderType eventProviderType;

        // These are the arguments of sendCommand and are only used for deferring commands until after we are fully initialized.
        internal EventListener? listener;
        internal int perEventSourceSessionId;
        internal bool enable;
        internal EventLevel level;
        internal EventKeywords matchAnyKeyword;
        internal EventCommandEventArgs? nextCommand;     // We form a linked list of these deferred commands.

#endregion
    }

    /// <summary>
    /// EventSourceCreatedEventArgs is passed to <see cref="EventListener.EventSourceCreated"/>
    /// </summary>
    public class EventSourceCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// The EventSource that is attaching to the listener.
        /// </summary>
        public EventSource? EventSource
        {
            get;
            internal set;
        }
    }

    /// <summary>
    /// EventWrittenEventArgs is passed to the user-provided override for
    /// <see cref="EventListener.OnEventWritten"/> when an event is fired.
    /// </summary>
    public class EventWrittenEventArgs : EventArgs
    {
        private ref EventSource.EventMetadata Metadata => ref CollectionsMarshal.GetValueRefOrNullRef(EventSource.m_eventData!, EventId);

        /// <summary>
        /// The name of the event.
        /// </summary>
        public string? EventName
        {
            get => _moreInfo?.EventName ?? (EventId <= 0 ? null : Metadata.Name);
            internal set => MoreInfo.EventName = value;
        }

        /// <summary>
        /// Gets the event ID for the event that was written.
        /// </summary>
        public int EventId { get; }

        private Guid _activityId;

        /// <summary>
        /// Gets the activity ID for the thread on which the event was written.
        /// </summary>
        public Guid ActivityId
        {
            get
            {
                if (_activityId == Guid.Empty)
                {
                    _activityId = EventSource.CurrentThreadActivityId;
                }

                return _activityId;
            }
        }

        /// <summary>
        /// Gets the related activity ID if one was specified when the event was written.
        /// </summary>
        public Guid RelatedActivityId => _moreInfo?.RelatedActivityId ?? default;

        /// <summary>
        /// Gets the payload for the event.
        /// </summary>
        public ReadOnlyCollection<object?>? Payload { get; internal set; }

        /// <summary>
        /// Gets the payload argument names.
        /// </summary>
        public ReadOnlyCollection<string>? PayloadNames
        {
            get => _moreInfo?.PayloadNames ?? (EventId <= 0 ? null : Metadata.ParameterNames);
            internal set => MoreInfo.PayloadNames = value;
        }

        /// <summary>
        /// Gets the event source object.
        /// </summary>
        public EventSource EventSource { get; }

        /// <summary>
        /// Gets the keywords for the event.
        /// </summary>
        public EventKeywords Keywords
        {
            get => EventId <= 0 ? (_moreInfo?.Keywords ?? default) : (EventKeywords)Metadata.Descriptor.Keywords;
            internal set => MoreInfo.Keywords = value;
        }

        /// <summary>
        /// Gets the operation code for the event.
        /// </summary>
        public EventOpcode Opcode
        {
            get => EventId <= 0 ? (_moreInfo?.Opcode ?? default) : (EventOpcode)Metadata.Descriptor.Opcode;
            internal set => MoreInfo.Opcode = value;
        }

        /// <summary>
        /// Gets the task for the event.
        /// </summary>
        public EventTask Task => EventId <= 0 ? EventTask.None : (EventTask)Metadata.Descriptor.Task;

        /// <summary>
        /// Any provider/user defined options associated with the event.
        /// </summary>
        public EventTags Tags
        {
            get => EventId <= 0 ? (_moreInfo?.Tags ?? default) : Metadata.Tags;
            internal set => MoreInfo.Tags = value;
        }

        /// <summary>
        /// Gets the message for the event.  If the message has {N} parameters they are NOT substituted.
        /// </summary>
        public string? Message
        {
            get => _moreInfo?.Message ?? (EventId <= 0 ? null : Metadata.Message);
            internal set => MoreInfo.Message = value;
        }

        /// <summary>
        /// Gets the channel for the event.
        /// </summary>
        public EventChannel Channel => EventId <= 0 ? EventChannel.None : (EventChannel)Metadata.Descriptor.Channel;

        /// <summary>
        /// Gets the version of the event.
        /// </summary>
        public byte Version => EventId <= 0 ? (byte)0 : Metadata.Descriptor.Version;

        /// <summary>
        /// Gets the level for the event.
        /// </summary>
        public EventLevel Level
        {
            get => EventId <= 0 ? (_moreInfo?.Level ?? default) : (EventLevel)Metadata.Descriptor.Level;
            internal set => MoreInfo.Level = value;
        }

        /// <summary>
        /// Gets the identifier for the OS thread that wrote the event.
        /// </summary>
        public long OSThreadId
        {
            get
            {
                ref long? osThreadId = ref MoreInfo.OsThreadId;
                if (!osThreadId.HasValue)
                {
                    osThreadId = (long)Thread.CurrentOSThreadId;
                }

                return osThreadId.Value;
            }
            internal set => MoreInfo.OsThreadId = value;
        }

        /// <summary>
        /// Gets a UTC DateTime that specifies when the event was written.
        /// </summary>
        public DateTime TimeStamp { get; internal set; }

        internal EventWrittenEventArgs(EventSource eventSource, int eventId)
        {
            EventSource = eventSource;
            EventId = eventId;
            TimeStamp = DateTime.UtcNow;
        }

        internal unsafe EventWrittenEventArgs(EventSource eventSource, int eventId, Guid* pActivityID, Guid* pChildActivityID)
            : this(eventSource, eventId)
        {
            if (pActivityID != null)
            {
                _activityId = *pActivityID;
            }

            if (pChildActivityID != null)
            {
                MoreInfo.RelatedActivityId = *pChildActivityID;
            }
        }

        private MoreEventInfo? _moreInfo;
        private MoreEventInfo MoreInfo => _moreInfo ??= new MoreEventInfo();

        private sealed class MoreEventInfo
        {
            public string? Message;
            public string? EventName;
            public ReadOnlyCollection<string>? PayloadNames;
            public Guid RelatedActivityId;
            public long? OsThreadId;
            public EventTags Tags;
            public EventOpcode Opcode;
            public EventLevel Level;
            public EventKeywords Keywords;
        }
    }

    /// <summary>
    /// Allows customizing defaults and specifying localization support for the event source class to which it is applied.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EventSourceAttribute : Attribute
    {
        /// <summary>
        /// Overrides the ETW name of the event source (which defaults to the class name)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Overrides the default (calculated) Guid of an EventSource type. Explicitly defining a GUID is discouraged,
        /// except when upgrading existing ETW providers to using event sources.
        /// </summary>
        public string? Guid { get; set; }

        /// <summary>
        /// <para>
        /// EventSources support localization of events. The names used for events, opcodes, tasks, keywords and maps
        /// can be localized to several languages if desired. This works by creating a ResX style string table
        /// (by simply adding a 'Resource File' to your project). This resource file is given a name e.g.
        /// 'DefaultNameSpace.ResourceFileName' which can be passed to the ResourceManager constructor to read the
        /// resources. This name is the value of the LocalizationResources property.
        /// </para><para>
        /// If LocalizationResources property is non-null, then EventSource will look up the localized strings for events by
        /// using the following resource naming scheme
        /// </para>
        ///     <para>* event_EVENTNAME</para>
        ///     <para>* task_TASKNAME</para>
        ///     <para>* keyword_KEYWORDNAME</para>
        ///     <para>* map_MAPNAME</para>
        /// <para>
        /// where the capitalized name is the name of the event, task, keyword, or map value that should be localized.
        /// Note that the localized string for an event corresponds to the Message string, and can have {0} values
        /// which represent the payload values.
        /// </para>
        /// </summary>
        public string? LocalizationResources { get; set; }
    }

    /// <summary>
    /// Any instance methods in a class that subclasses <see cref="EventSource"/> and that return void are
    /// assumed by default to be methods that generate an ETW event. Enough information can be deduced from the
    /// name of the method and its signature to generate basic schema information for the event. The
    /// <see cref="EventAttribute"/> class allows you to specify additional event schema information for an event if
    /// desired.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EventAttribute : Attribute
    {
        /// <summary>Construct an EventAttribute with specified eventId</summary>
        /// <param name="eventId">ID of the ETW event (an integer between 1 and 65535)</param>
        public EventAttribute(int eventId)
        {
            EventId = eventId;
            Level = EventLevel.Informational;
        }

        /// <summary>Event's ID</summary>
        public int EventId { get; }
        /// <summary>Event's severity level: indicates the severity or verbosity of the event</summary>
        public EventLevel Level { get; set; }
        /// <summary>Event's keywords: allows classification of events by "categories"</summary>
        public EventKeywords Keywords { get; set; }
        /// <summary>Event's operation code: allows defining operations, generally used with Tasks</summary>
        public EventOpcode Opcode
        {
            get => m_opcode;
            set
            {
                m_opcode = value;
                m_opcodeSet = true;
            }
        }

        internal bool IsOpcodeSet => m_opcodeSet;

        /// <summary>Event's task: allows logical grouping of events</summary>
        public EventTask Task { get; set; }

        /// <summary>Event's channel: defines an event log as an additional destination for the event</summary>
        public EventChannel Channel { get; set; }

        /// <summary>Event's version</summary>
        public byte Version { get; set; }

        /// <summary>
        /// This can be specified to enable formatting and localization of the event's payload. You can
        /// use standard .NET substitution operators (eg {1}) in the string and they will be replaced
        /// with the 'ToString()' of the corresponding part of the  event payload.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// User defined options associated with the event.  These do not have meaning to the EventSource but
        /// are passed through to listeners which given them semantics.
        /// </summary>
        public EventTags Tags { get; set; }

        /// <summary>
        /// Allows fine control over the Activity IDs generated by start and stop events
        /// </summary>
        public EventActivityOptions ActivityOptions { get; set; }

#region private
        private EventOpcode m_opcode;
        private bool m_opcodeSet;
#endregion
    }

    /// <summary>
    /// By default all instance methods in a class that subclasses code:EventSource that and return
    /// void are assumed to be methods that generate an event. This default can be overridden by specifying
    /// the code:NonEventAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NonEventAttribute : Attribute
    {
        /// <summary>
        /// Constructs a default NonEventAttribute
        /// </summary>
        public NonEventAttribute() { }
    }

    /// <summary>
    /// EventChannelAttribute allows customizing channels supported by an EventSource. This attribute must be
    /// applied to an member of type EventChannel defined in a Channels class nested in the EventSource class:
    /// <code>
    ///     public static class Channels
    ///     {
    ///         [Channel(Enabled = true, EventChannelType = EventChannelType.Admin)]
    ///         public const EventChannel Admin = (EventChannel)16;
    ///
    ///         [Channel(Enabled = false, EventChannelType = EventChannelType.Operational)]
    ///         public const EventChannel Operational = (EventChannel)17;
    ///     }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class EventChannelAttribute : Attribute
    {
        /// <summary>
        /// Specified whether the channel is enabled by default
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Legal values are in EventChannelType
        /// </summary>
        public EventChannelType EventChannelType { get; set; }

        // TODO: there is a convention that the name is the Provider/Type   Should we provide an override?
        // public string Name { get; set; }
    }

    /// <summary>
    /// Allowed channel types
    /// </summary>
    internal enum EventChannelType
    {
        /// <summary>The admin channel</summary>
        Admin = 1,
        /// <summary>The operational channel</summary>
        Operational,
        /// <summary>The Analytic channel</summary>
        Analytic,
        /// <summary>The debug channel</summary>
        Debug,
    }

    /// <summary>
    /// Describes the pre-defined command (EventCommandEventArgs.Command property) that is passed to the OnEventCommand callback.
    /// </summary>
    public enum EventCommand
    {
        /// <summary>
        /// Update EventSource state
        /// </summary>
        Update = 0,
        /// <summary>
        /// Request EventSource to generate and send its manifest
        /// </summary>
        SendManifest = -1,
        /// <summary>
        /// Enable event
        /// </summary>
        Enable = -2,
        /// <summary>
        /// Disable event
        /// </summary>
        Disable = -3
    }

#region private classes

    // holds a bitfield representing a session mask
    /// <summary>
    /// A SessionMask represents a set of (at most MAX) sessions as a bit mask. The perEventSourceSessionId
    /// is the index in the SessionMask of the bit that will be set. These can translate to
    /// EventSource's reserved keywords bits using the provided ToEventKeywords() and
    /// FromEventKeywords() methods.
    /// </summary>
    internal struct SessionMask
    {
        public SessionMask(SessionMask m)
        { m_mask = m.m_mask; }

        public SessionMask(uint mask = 0)
        { m_mask = mask & MASK; }

        public bool IsEqualOrSupersetOf(SessionMask m)
        {
            return (this.m_mask | m.m_mask) == this.m_mask;
        }

        public static SessionMask All => new SessionMask(MASK);

        public static SessionMask FromId(int perEventSourceSessionId)
        {
            Debug.Assert(perEventSourceSessionId < MAX);
            return new SessionMask((uint)1 << perEventSourceSessionId);
        }

        public ulong ToEventKeywords()
        {
            return (ulong)m_mask << SHIFT_SESSION_TO_KEYWORD;
        }

        public static SessionMask FromEventKeywords(ulong m)
        {
            return new SessionMask((uint)(m >> SHIFT_SESSION_TO_KEYWORD));
        }

        public bool this[int perEventSourceSessionId]
        {
            get
            {
                Debug.Assert(perEventSourceSessionId < MAX);
                return (m_mask & (1 << perEventSourceSessionId)) != 0;
            }
            set
            {
                Debug.Assert(perEventSourceSessionId < MAX);
                if (value) m_mask |= ((uint)1 << perEventSourceSessionId);
                else m_mask &= ~((uint)1 << perEventSourceSessionId);
            }
        }

        public static SessionMask operator |(SessionMask m1, SessionMask m2) =>
            new SessionMask(m1.m_mask | m2.m_mask);

        public static SessionMask operator &(SessionMask m1, SessionMask m2) =>
            new SessionMask(m1.m_mask & m2.m_mask);

        public static SessionMask operator ^(SessionMask m1, SessionMask m2) =>
            new SessionMask(m1.m_mask ^ m2.m_mask);

        public static SessionMask operator ~(SessionMask m) =>
            new SessionMask(MASK & ~(m.m_mask));

        public static explicit operator ulong(SessionMask m) => m.m_mask;

        public static explicit operator uint(SessionMask m) => m.m_mask;

        private uint m_mask;

        internal const int SHIFT_SESSION_TO_KEYWORD = 44;         // bits 44-47 inclusive are reserved
        internal const uint MASK = 0x0fU;                         // the mask of 4 reserved bits
        internal const uint MAX = 4;                              // maximum number of simultaneous ETW sessions supported
    }

    /// <summary>
    /// code:EventDispatchers are a simple 'helper' structure that holds the filtering state
    /// (m_EventEnabled) for a particular EventSource X EventListener tuple
    ///
    /// Thus a single EventListener may have many EventDispatchers (one for every EventSource
    /// that EventListener has activate) and a Single EventSource may also have many
    /// event Dispatchers (one for every EventListener that has activated it).
    ///
    /// Logically a particular EventDispatcher belongs to exactly one EventSource and exactly
    /// one EventListener (although EventDispatcher does not 'remember' the EventSource it is
    /// associated with.
    /// </summary>
    internal sealed class EventDispatcher
    {
        internal EventDispatcher(EventDispatcher? next, Dictionary<int, bool>? eventEnabled, EventListener listener)
        {
            m_Next = next;
            m_EventEnabled = eventEnabled;
            m_Listener = listener;
        }

        // Instance fields
        internal readonly EventListener m_Listener;   // The dispatcher this entry is for
        internal Dictionary<int, bool>? m_EventEnabled;              // For every event in a the eventSource, is it enabled?

        // Only guaranteed to exist after a EnsureInit()
        internal EventDispatcher? m_Next;              // These form a linked list in code:EventSource.m_Dispatchers
        // Of all listeners for that eventSource.
    }

    /// <summary>
    /// Flags that can be used with EventSource.GenerateManifest to control how the ETW manifest for the EventSource is
    /// generated.
    /// </summary>
    [Flags]
    public enum EventManifestOptions
    {
        /// <summary>
        /// Only the resources associated with current UI culture are included in the  manifest
        /// </summary>
        None = 0x0,
        /// <summary>
        /// Throw exceptions for any inconsistency encountered
        /// </summary>
        Strict = 0x1,
        /// <summary>
        /// Generate a "resources" node under "localization" for every satellite assembly provided
        /// </summary>
        AllCultures = 0x2,
        /// <summary>
        /// Generate the manifest only if the event source needs to be registered on the machine,
        /// otherwise return null (but still perform validation if Strict is specified)
        /// </summary>
        OnlyIfNeededForRegistration = 0x4,
        /// <summary>
        /// When generating the manifest do *not* enforce the rule that the current EventSource class
        /// must be the base class for the user-defined type passed in. This allows validation of .net
        /// event sources using the new validation code
        /// </summary>
        AllowEventSourceOverride = 0x8,
    }

    /// <summary>
    /// ManifestBuilder is designed to isolate the details of the message of the event from the
    /// rest of EventSource.  This one happens to create XML.
    /// </summary>
    internal sealed class ManifestBuilder
    {
        /// <summary>
        /// Build a manifest for 'providerName' with the given GUID, which will be packaged into 'dllName'.
        /// 'resources, is a resource manager.  If specified all messages are localized using that manager.
        /// </summary>
        public ManifestBuilder(string providerName, Guid providerGuid, string? dllName, ResourceManager? resources,
                               EventManifestOptions flags) : this(resources, flags)
        {
            this.providerName = providerName;

            sb = new StringBuilder();
            events = new StringBuilder();
            templates = new StringBuilder();
            sb.AppendLine("<instrumentationManifest xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
            sb.AppendLine(" <instrumentation xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:win=\"http://manifests.microsoft.com/win/2004/08/windows/events\">");
            sb.AppendLine("  <events xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
            sb.Append($"<provider name=\"{providerName}\" guid=\"{{{providerGuid}}}\"");
            if (dllName != null)
                sb.Append($" resourceFileName=\"{dllName}\" messageFileName=\"{dllName}\"");

            sb.Append(" symbol=\"");
            int pos = sb.Length;
            sb.Append(providerName); // Period and dash are illegal; replace them.
            sb.Replace('.', '_', pos, sb.Length - pos).Replace("-", "", pos, sb.Length - pos);
            sb.AppendLine("\">");
        }

        /// <summary>
        /// <term>Will NOT build a manifest!</term> If the intention is to build a manifest don't use this constructor.
        ///'resources, is a resource manager.  If specified all messages are localized using that manager.
        /// </summary>
        internal ManifestBuilder(ResourceManager? resources, EventManifestOptions flags)
        {
            providerName = "";

            this.flags = flags;

            this.resources = resources;
            sb = null;
            events = null;
            templates = null;
            opcodeTab = new Dictionary<int, string>();
            stringTab = new Dictionary<string, string>();
            errors = new List<string>();
            perEventByteArrayArgIndices = new Dictionary<string, List<int>>();
        }

        public void AddOpcode(string name, int value)
        {
            if ((flags & EventManifestOptions.Strict) != 0)
            {
                if (value <= 10 || value >= 239)
                {
                    ManifestError(SR.Format(SR.EventSource_IllegalOpcodeValue, name, value));
                }

                if (opcodeTab.TryGetValue(value, out string? prevName) && !name.Equals(prevName, StringComparison.Ordinal))
                {
                    ManifestError(SR.Format(SR.EventSource_OpcodeCollision, name, prevName, value));
                }
            }

            opcodeTab[value] = name;
        }

        public void AddTask(string name, int value)
        {
            if ((flags & EventManifestOptions.Strict) != 0)
            {
                if (value <= 0 || value >= 65535)
                {
                    ManifestError(SR.Format(SR.EventSource_IllegalTaskValue, name, value));
                }

                if (taskTab != null && taskTab.TryGetValue(value, out string? prevName) && !name.Equals(prevName, StringComparison.Ordinal))
                {
                    ManifestError(SR.Format(SR.EventSource_TaskCollision, name, prevName, value));
                }
            }

            taskTab ??= new Dictionary<int, string>();
            taskTab[value] = name;
        }

        public void AddKeyword(string name, ulong value)
        {
            if ((value & (value - 1)) != 0) // Must be zero or a power of 2
            {
                ManifestError(SR.Format(SR.EventSource_KeywordNeedPowerOfTwo, $"0x{value:x}", name), true);
            }
            if ((flags & EventManifestOptions.Strict) != 0)
            {
                if (value >= 0x0000100000000000UL && !name.StartsWith("Session", StringComparison.Ordinal))
                {
                    ManifestError(SR.Format(SR.EventSource_IllegalKeywordsValue, name, $"0x{value:x}"));
                }

                if (keywordTab != null && keywordTab.TryGetValue(value, out string? prevName) && !name.Equals(prevName, StringComparison.Ordinal))
                {
                    ManifestError(SR.Format(SR.EventSource_KeywordCollision, name, prevName, $"0x{value:x}"));
                }
            }

            keywordTab ??= new Dictionary<ulong, string>();
            keywordTab[value] = name;
        }

        /// <summary>
        /// Add a channel.  channelAttribute can be null
        /// </summary>
        public void AddChannel(string? name, int value, EventChannelAttribute? channelAttribute)
        {
            EventChannel chValue = (EventChannel)value;
            if (value < (int)EventChannel.Admin || value > 255)
                ManifestError(SR.Format(SR.EventSource_EventChannelOutOfRange, name, value));
            else if (chValue >= EventChannel.Admin && chValue <= EventChannel.Debug &&
                     channelAttribute != null && EventChannelToChannelType(chValue) != channelAttribute.EventChannelType)
            {
                // we want to ensure developers do not define EventChannels that conflict with the builtin ones,
                // but we want to allow them to override the default ones...
                ManifestError(SR.Format(SR.EventSource_ChannelTypeDoesNotMatchEventChannelValue,
                                                                            name, ((EventChannel)value).ToString()));
            }

            // TODO: validate there are no conflicting manifest exposed names (generally following the format "provider/type")

            ulong kwd = GetChannelKeyword(chValue);

            channelTab ??= new Dictionary<int, ChannelInfo>(4);
            channelTab[value] = new ChannelInfo { Name = name, Keywords = kwd, Attribs = channelAttribute };
        }

        private static EventChannelType EventChannelToChannelType(EventChannel channel)
        {
            Debug.Assert(channel >= EventChannel.Admin && channel <= EventChannel.Debug);
            return (EventChannelType)((int)channel - (int)EventChannel.Admin + (int)EventChannelType.Admin);
        }

        private static EventChannelAttribute GetDefaultChannelAttribute(EventChannel channel)
        {
            EventChannelAttribute attrib = new EventChannelAttribute();
            attrib.EventChannelType = EventChannelToChannelType(channel);
            if (attrib.EventChannelType <= EventChannelType.Operational)
                attrib.Enabled = true;
            return attrib;
        }

        public ulong[] GetChannelData()
        {
            if (this.channelTab == null)
            {
                return Array.Empty<ulong>();
            }

            // We create an array indexed by the channel id for fast look up.
            // E.g. channelMask[Admin] will give you the bit mask for Admin channel.
            int maxkey = -1;
            foreach (int item in this.channelTab.Keys)
            {
                if (item > maxkey)
                {
                    maxkey = item;
                }
            }

            ulong[] channelMask = new ulong[maxkey + 1];
            foreach (KeyValuePair<int, ChannelInfo> item in this.channelTab)
            {
                channelMask[item.Key] = item.Value.Keywords;
            }

            return channelMask;
        }

        public void StartEvent(string eventName, EventAttribute eventAttribute)
        {
            Debug.Assert(numParams == 0);
            Debug.Assert(this.eventName == null);
            this.eventName = eventName;
            numParams = 0;
            byteArrArgIndices = null;

            events?.Append("  <event value=\"").Append(eventAttribute.EventId).
                Append("\" version=\"").Append(eventAttribute.Version).
                Append("\" level=\"");
            AppendLevelName(events, eventAttribute.Level);
            events?.Append("\" symbol=\"").Append(eventName).Append('"');

            // at this point we add to the manifest's stringTab a message that is as-of-yet
            // "untranslated to manifest convention", b/c we don't have the number or position
            // of any byte[] args (which require string format index updates)
            WriteMessageAttrib(events, "event", eventName, eventAttribute.Message);

            if (eventAttribute.Keywords != 0)
            {
                events?.Append(" keywords=\"");
                AppendKeywords(events, (ulong)eventAttribute.Keywords, eventName);
                events?.Append('"');
            }

            if (eventAttribute.Opcode != 0)
            {
                string? str = GetOpcodeName(eventAttribute.Opcode, eventName);
                events?.Append(" opcode=\"").Append(str).Append('"');
            }

            if (eventAttribute.Task != 0)
            {
                string? str = GetTaskName(eventAttribute.Task, eventName);
                events?.Append(" task=\"").Append(str).Append('"');
            }

            if (eventAttribute.Channel != 0)
            {
                string? str = GetChannelName(eventAttribute.Channel, eventName, eventAttribute.Message);
                events?.Append(" channel=\"").Append(str).Append('"');
            }
        }

        public void AddEventParameter(Type type, string name)
        {
            if (numParams == 0)
                templates?.Append("  <template tid=\"").Append(eventName).AppendLine("Args\">");
            if (type == typeof(byte[]))
            {
                // mark this index as "extraneous" (it has no parallel in the managed signature)
                // we use these values in TranslateToManifestConvention()
                byteArrArgIndices ??= new List<int>(4);
                byteArrArgIndices.Add(numParams);

                // add an extra field to the template representing the length of the binary blob
                numParams++;
                templates?.Append("   <data name=\"").Append(name).AppendLine("Size\" inType=\"win:UInt32\"/>");
            }
            numParams++;
            templates?.Append("   <data name=\"").Append(name).Append("\" inType=\"").Append(GetTypeName(type)).Append('"');
            // TODO: for 'byte*' types it assumes the user provided length is named using the same naming convention
            //       as for 'byte[]' args (blob_arg_name + "Size")
            if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof(byte))
            {
                // add "length" attribute to the "blob" field in the template (referencing the field added above)
                templates?.Append(" length=\"").Append(name).Append("Size\"");
            }
            // ETW does not support 64-bit value maps, so we don't specify these as ETW maps
            if (type.IsEnum && Enum.GetUnderlyingType(type) != typeof(ulong) && Enum.GetUnderlyingType(type) != typeof(long))
            {
                templates?.Append(" map=\"").Append(type.Name).Append('"');
                mapsTab ??= new Dictionary<string, Type>();
                mapsTab.TryAdd(type.Name, type);        // Remember that we need to dump the type enumeration
            }

            templates?.AppendLine("/>");
        }
        public void EndEvent()
        {
            Debug.Assert(eventName != null);

            if (numParams > 0)
            {
                templates?.AppendLine("  </template>");
                events?.Append(" template=\"").Append(eventName).Append("Args\"");
            }
            events?.AppendLine("/>");

            if (byteArrArgIndices != null)
                perEventByteArrayArgIndices[eventName] = byteArrArgIndices;

            // at this point we have all the information we need to translate the C# Message
            // to the manifest string we'll put in the stringTab
            string prefixedEventName = "event_" + eventName;
            if (stringTab.TryGetValue(prefixedEventName, out string? msg))
            {
                msg = TranslateToManifestConvention(msg, eventName);
                stringTab[prefixedEventName] = msg;
            }

            eventName = null;
            numParams = 0;
            byteArrArgIndices = null;
        }

        // Channel keywords are generated one per channel to allow channel based filtering in event viewer. These keywords are autogenerated
        // by mc.exe for compiling a manifest and are based on the order of the channels (fields) in the Channels inner class (when advanced
        // channel support is enabled), or based on the order the predefined channels appear in the EventAttribute properties (for simple
        // support). The manifest generated *MUST* have the channels specified in the same order (that's how our computed keywords are mapped
        // to channels by the OS infrastructure).
        // If channelKeyworkds is present, and has keywords bits in the ValidPredefinedChannelKeywords then it is
        // assumed that the keyword for that channel should be that bit.
        // otherwise we allocate a channel bit for the channel.
        // explicit channel bits are only used by WCF to mimic an existing manifest,
        // so we don't dont do error checking.
        public ulong GetChannelKeyword(EventChannel channel, ulong channelKeyword = 0)
        {
            // strip off any non-channel keywords, since we are only interested in channels here.
            channelKeyword &= ValidPredefinedChannelKeywords;
            channelTab ??= new Dictionary<int, ChannelInfo>(4);

            if (channelTab.Count == MaxCountChannels)
                ManifestError(SR.EventSource_MaxChannelExceeded);

            if (!channelTab.TryGetValue((int)channel, out ChannelInfo? info))
            {
                // If we were not given an explicit channel, allocate one.
                if (channelKeyword == 0)
                {
                    channelKeyword = nextChannelKeywordBit;
                    nextChannelKeywordBit >>= 1;
                }
            }
            else
            {
                channelKeyword = info.Keywords;
            }

            return channelKeyword;
        }

        public byte[] CreateManifest()
        {
            string str = CreateManifestString();
            return (str != "") ? Encoding.UTF8.GetBytes(str) : Array.Empty<byte>();
        }

        public IList<string> Errors => errors;

        public bool HasResources => resources != null;

        /// <summary>
        /// When validating an event source it adds the error to the error collection.
        /// When not validating it throws an exception if runtimeCritical is "true".
        /// Otherwise the error is ignored.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="runtimeCritical"></param>
        public void ManifestError(string msg, bool runtimeCritical = false)
        {
            if ((flags & EventManifestOptions.Strict) != 0)
                errors.Add(msg);
            else if (runtimeCritical)
                throw new ArgumentException(msg);
        }

        private string CreateManifestString()
        {
            Span<char> ulongHexScratch = stackalloc char[16]; // long enough for ulong.MaxValue formatted as hex

            // Write out the channels
            if (channelTab != null)
            {
                sb?.AppendLine(" <channels>");
                var sortedChannels = new List<KeyValuePair<int, ChannelInfo>>();
                foreach (KeyValuePair<int, ChannelInfo> p in channelTab) { sortedChannels.Add(p); }
                sortedChannels.Sort((p1, p2) => -Comparer<ulong>.Default.Compare(p1.Value.Keywords, p2.Value.Keywords));

                foreach (KeyValuePair<int, ChannelInfo> kvpair in sortedChannels)
                {
                    int channel = kvpair.Key;
                    ChannelInfo channelInfo = kvpair.Value;

                    string? channelType = null;
                    bool enabled = false;
                    string? fullName = null;

                    if (channelInfo.Attribs != null)
                    {
                        EventChannelAttribute attribs = channelInfo.Attribs;
                        if (Enum.IsDefined(attribs.EventChannelType))
                            channelType = attribs.EventChannelType.ToString();
                        enabled = attribs.Enabled;
                    }

                    fullName ??= providerName + "/" + channelInfo.Name;

                    sb?.Append("  <channel chid=\"").Append(channelInfo.Name).Append("\" name=\"").Append(fullName).Append('"');
                    Debug.Assert(channelInfo.Name != null);
                    WriteMessageAttrib(sb, "channel", channelInfo.Name, null);
                    sb?.Append(" value=\"").Append(channel).Append('"');
                    if (channelType != null)
                        sb?.Append(" type=\"").Append(channelType).Append('"');
                    sb?.Append(" enabled=\"").Append(enabled ? "true" : "false").AppendLine("\"/>");
                }
                sb?.AppendLine(" </channels>");
            }

            // Write out the tasks
            if (taskTab != null)
            {
                sb?.AppendLine(" <tasks>");
                var sortedTasks = new List<int>(taskTab.Keys);
                sortedTasks.Sort();

                foreach (int task in sortedTasks)
                {
                    sb?.Append("  <task");
                    WriteNameAndMessageAttribs(sb, "task", taskTab[task]);
                    sb?.Append(" value=\"").Append(task).AppendLine("\"/>");
                }
                sb?.AppendLine(" </tasks>");
            }

            // Write out the maps

            // Scoping the call to enum GetFields to a local function to limit the trimming suppressions
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Trimmer does not trim enums")]
            static FieldInfo[] GetEnumFields(Type localEnumType)
            {
                Debug.Assert(localEnumType.IsEnum);
                return localEnumType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
            }

            if (mapsTab != null)
            {
                sb?.AppendLine(" <maps>");
                foreach (Type enumType in mapsTab.Values)
                {
                    bool isbitmap = EventSource.IsCustomAttributeDefinedHelper(enumType, typeof(FlagsAttribute), flags);
                    string mapKind = isbitmap ? "bitMap" : "valueMap";
                    sb?.Append("  <").Append(mapKind).Append(" name=\"").Append(enumType.Name).AppendLine("\">");

                    // write out each enum value
                    FieldInfo[] staticFields = GetEnumFields(enumType);
                    bool anyValuesWritten = false;
                    foreach (FieldInfo staticField in staticFields)
                    {
                        object? constantValObj = staticField.GetRawConstantValue();

                        if (constantValObj != null)
                        {
                            ulong hexValue;
                            if (constantValObj is ulong)
                                hexValue = (ulong)constantValObj;    // This is the only integer type that can't be represented by a long.
                            else
                                hexValue = (ulong)Convert.ToInt64(constantValObj); // Handles all integer types except ulong.

                            // ETW requires all bitmap values to be powers of 2.  Skip the ones that are not.
                            // TODO: Warn people about the dropping of values.
                            if (isbitmap && !BitOperations.IsPow2(hexValue))
                                continue;

                            hexValue.TryFormat(ulongHexScratch, out int charsWritten, "x");
                            ReadOnlySpan<char> hexValueFormatted = ulongHexScratch.Slice(0, charsWritten);

                            sb?.Append("   <map value=\"0x").Append(hexValueFormatted).Append('"');
                            WriteMessageAttrib(sb, "map", enumType.Name + "." + staticField.Name, staticField.Name);
                            sb?.AppendLine("/>");
                            anyValuesWritten = true;
                        }
                    }

                    // the OS requires that bitmaps and valuemaps have at least one value or it reject the whole manifest.
                    // To avoid that put a 'None' entry if there are no other values.
                    if (!anyValuesWritten)
                    {
                        sb?.Append("   <map value=\"0x0\"");
                        WriteMessageAttrib(sb, "map", enumType.Name + ".None", "None");
                        sb?.AppendLine("/>");
                    }
                    sb?.Append("  </").Append(mapKind).AppendLine(">");
                }
                sb?.AppendLine(" </maps>");
            }

            // Write out the opcodes
            sb?.AppendLine(" <opcodes>");
            var sortedOpcodes = new List<int>(opcodeTab.Keys);
            sortedOpcodes.Sort();

            foreach (int opcode in sortedOpcodes)
            {
                sb?.Append("  <opcode");
                WriteNameAndMessageAttribs(sb, "opcode", opcodeTab[opcode]);
                sb?.Append(" value=\"").Append(opcode).AppendLine("\"/>");
            }
            sb?.AppendLine(" </opcodes>");

            // Write out the keywords
            if (keywordTab != null)
            {
                sb?.AppendLine(" <keywords>");
                var sortedKeywords = new List<ulong>(keywordTab.Keys);
                sortedKeywords.Sort();

                foreach (ulong keyword in sortedKeywords)
                {
                    sb?.Append("  <keyword");
                    WriteNameAndMessageAttribs(sb, "keyword", keywordTab[keyword]);
                    keyword.TryFormat(ulongHexScratch, out int charsWritten, "x");
                    ReadOnlySpan<char> keywordFormatted = ulongHexScratch.Slice(0, charsWritten);
                    sb?.Append(" mask=\"0x").Append(keywordFormatted).AppendLine("\"/>");
                }
                sb?.AppendLine(" </keywords>");
            }

            sb?.AppendLine(" <events>");
            sb?.Append(events);
            sb?.AppendLine(" </events>");

            sb?.AppendLine(" <templates>");
            if (templates?.Length > 0)
            {
                sb?.Append(templates);
            }
            else
            {
                // Work around a corner-case ETW issue where a manifest with no templates causes
                // ETW events to not get sent to their associated channel.
                sb?.AppendLine("    <template tid=\"_empty\"></template>");
            }
            sb?.AppendLine(" </templates>");

            sb?.AppendLine("</provider>");
            sb?.AppendLine("</events>");
            sb?.AppendLine("</instrumentation>");

            // Output the localization information.
            sb?.AppendLine("<localization>");

            var sortedStrings = new string[stringTab.Keys.Count];
            stringTab.Keys.CopyTo(sortedStrings, 0);
            Array.Sort(sortedStrings, StringComparer.Ordinal);

            CultureInfo ci = CultureInfo.CurrentUICulture;
            sb?.Append(" <resources culture=\"").Append(ci.Name).AppendLine("\">");
            sb?.AppendLine("  <stringTable>");
            foreach (string stringKey in sortedStrings)
            {
                string? val = GetLocalizedMessage(stringKey, ci, etwFormat: true);
                sb?.Append("   <string id=\"").Append(stringKey).Append("\" value=\"").Append(val).AppendLine("\"/>");
            }
            sb?.AppendLine("  </stringTable>");
            sb?.AppendLine(" </resources>");

            sb?.AppendLine("</localization>");
            sb?.AppendLine("</instrumentationManifest>");
            return sb?.ToString() ?? "";
        }

#region private
        private void WriteNameAndMessageAttribs(StringBuilder? stringBuilder, string elementName, string name)
        {
            stringBuilder?.Append(" name=\"").Append(name).Append('"');
            WriteMessageAttrib(sb, elementName, name, name);
        }
        private void WriteMessageAttrib(StringBuilder? stringBuilder, string elementName, string name, string? value)
        {
            string? key = null;

            // See if the user wants things localized.
            if (resources != null)
            {
                // resource fallback: strings in the neutral culture will take precedence over inline strings
                key = elementName + "_" + name;
                if (resources.GetString(key, CultureInfo.InvariantCulture) is string localizedString)
                    value = localizedString;
            }

            if (value == null)
                return;

            key ??= elementName + "_" + name;
            stringBuilder?.Append(" message=\"$(string.").Append(key).Append(")\"");

            if (stringTab.TryGetValue(key, out string? prevValue) && !prevValue.Equals(value))
            {
                ManifestError(SR.Format(SR.EventSource_DuplicateStringKey, key), true);
                return;
            }

            stringTab[key] = value;
        }
        internal string? GetLocalizedMessage(string key, CultureInfo ci, bool etwFormat)
        {
            string? value = null;
            if (resources != null)
            {
                string? localizedString = resources.GetString(key, ci);
                if (localizedString != null)
                {
                    value = localizedString;
                    if (etwFormat && key.StartsWith("event_", StringComparison.Ordinal))
                    {
                        string evtName = key.Substring("event_".Length);
                        value = TranslateToManifestConvention(value, evtName);
                    }
                }
            }
            if (etwFormat && value == null)
                stringTab.TryGetValue(key, out value);

            return value;
        }

        private static void AppendLevelName(StringBuilder? sb, EventLevel level)
        {
            if ((int)level < 16)
            {
                sb?.Append("win:");
            }

            sb?.Append(level switch // avoid boxing that comes from level.ToString()
            {
                EventLevel.LogAlways => nameof(EventLevel.LogAlways),
                EventLevel.Critical => nameof(EventLevel.Critical),
                EventLevel.Error => nameof(EventLevel.Error),
                EventLevel.Warning => nameof(EventLevel.Warning),
                EventLevel.Informational => nameof(EventLevel.Informational),
                EventLevel.Verbose => nameof(EventLevel.Verbose),
                _ => ((int)level).ToString()
            });
        }

        private string? GetChannelName(EventChannel channel, string eventName, string? eventMessage)
        {
            if (channelTab == null || !channelTab.TryGetValue((int)channel, out ChannelInfo? info))
            {
                if (channel < EventChannel.Admin) // || channel > EventChannel.Debug)
                    ManifestError(SR.Format(SR.EventSource_UndefinedChannel, channel, eventName));

                // allow channels to be auto-defined.  The well known ones get their well known names, and the
                // rest get names Channel<N>.  This allows users to modify the Manifest if they want more advanced features.
                channelTab ??= new Dictionary<int, ChannelInfo>(4);

                string channelName = channel.ToString();        // For well know channels this is a nice name, otherwise a number
                if (EventChannel.Debug < channel)
                    channelName = "Channel" + channelName;      // Add a 'Channel' prefix for numbers.

                AddChannel(channelName, (int)channel, GetDefaultChannelAttribute(channel));
                if (!channelTab.TryGetValue((int)channel, out info))
                    ManifestError(SR.Format(SR.EventSource_UndefinedChannel, channel, eventName));
            }
            // events that specify admin channels *must* have non-null "Message" attributes
            if (resources != null)
                eventMessage ??= resources.GetString("event_" + eventName, CultureInfo.InvariantCulture);

            Debug.Assert(info!.Attribs != null);
            if (info.Attribs.EventChannelType == EventChannelType.Admin && eventMessage == null)
                ManifestError(SR.Format(SR.EventSource_EventWithAdminChannelMustHaveMessage, eventName, info.Name));
            return info.Name;
        }
        private string GetTaskName(EventTask task, string eventName)
        {
            if (task == EventTask.None)
                return "";

            taskTab ??= new Dictionary<int, string>();
            if (!taskTab.TryGetValue((int)task, out string? ret))
                ret = taskTab[(int)task] = eventName;
            return ret;
        }

        private string? GetOpcodeName(EventOpcode opcode, string eventName)
        {
            switch (opcode)
            {
                case EventOpcode.Info:
                    return "win:Info";
                case EventOpcode.Start:
                    return "win:Start";
                case EventOpcode.Stop:
                    return "win:Stop";
                case EventOpcode.DataCollectionStart:
                    return "win:DC_Start";
                case EventOpcode.DataCollectionStop:
                    return "win:DC_Stop";
                case EventOpcode.Extension:
                    return "win:Extension";
                case EventOpcode.Reply:
                    return "win:Reply";
                case EventOpcode.Resume:
                    return "win:Resume";
                case EventOpcode.Suspend:
                    return "win:Suspend";
                case EventOpcode.Send:
                    return "win:Send";
                case EventOpcode.Receive:
                    return "win:Receive";
            }

            if (opcodeTab == null || !opcodeTab.TryGetValue((int)opcode, out string? ret))
            {
                ManifestError(SR.Format(SR.EventSource_UndefinedOpcode, opcode, eventName), true);
                ret = null;
            }

            return ret;
        }

        private void AppendKeywords(StringBuilder? sb, ulong keywords, string eventName)
        {
            // ignore keywords associate with channels
            // See ValidPredefinedChannelKeywords def for more.
            keywords &= ~ValidPredefinedChannelKeywords;

            bool appended = false;
            for (ulong bit = 1; bit != 0; bit <<= 1)
            {
                if ((keywords & bit) != 0)
                {
                    string? keyword = null;
                    if ((keywordTab == null || !keywordTab.TryGetValue(bit, out keyword)) &&
                        (bit >= (ulong)0x1000000000000))
                    {
                        // do not report Windows reserved keywords in the manifest (this allows the code
                        // to be resilient to potential renaming of these keywords)
                        keyword = string.Empty;
                    }
                    if (keyword == null)
                    {
                        ManifestError(SR.Format(SR.EventSource_UndefinedKeyword, "0x" + bit.ToString("x", CultureInfo.CurrentCulture), eventName), true);
                        keyword = string.Empty;
                    }

                    if (keyword.Length != 0)
                    {
                        if (appended)
                        {
                            sb?.Append(' ');
                        }

                        sb?.Append(keyword);
                        appended = true;
                    }
                }
            }
        }

        private string GetTypeName(Type type)
        {
            if (type.IsEnum)
            {
                string typeName = GetTypeName(type.GetEnumUnderlyingType());
                return typeName switch // ETW requires enums to be unsigned.
                {
                    "win:Int8" => "win:UInt8",
                    "win:Int16" => "win:UInt16",
                    "win:Int32" => "win:UInt32",
                    "win:Int64" => "win:UInt64",
                    _ => typeName,
                };
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return "win:Boolean";
                case TypeCode.Byte:
                    return "win:UInt8";
                case TypeCode.Char:
                case TypeCode.UInt16:
                    return "win:UInt16";
                case TypeCode.UInt32:
                    return "win:UInt32";
                case TypeCode.UInt64:
                    return "win:UInt64";
                case TypeCode.SByte:
                    return "win:Int8";
                case TypeCode.Int16:
                    return "win:Int16";
                case TypeCode.Int32:
                    return "win:Int32";
                case TypeCode.Int64:
                    return "win:Int64";
                case TypeCode.String:
                    return "win:UnicodeString";
                case TypeCode.Single:
                    return "win:Float";
                case TypeCode.Double:
                    return "win:Double";
                case TypeCode.DateTime:
                    return "win:FILETIME";
                default:
                    if (type == typeof(Guid))
                        return "win:GUID";
                    else if (type == typeof(IntPtr))
                        return "win:Pointer";
                    else if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof(byte))
                        return "win:Binary";

                    ManifestError(SR.Format(SR.EventSource_UnsupportedEventTypeInManifest, type.Name), true);
                    return string.Empty;
            }
        }

        private static void UpdateStringBuilder([NotNull] ref StringBuilder? stringBuilder, string eventMessage, int startIndex, int count)
        {
            stringBuilder ??= new StringBuilder();
            stringBuilder.Append(eventMessage, startIndex, count);
        }

        private static readonly string[] s_escapes = ["&amp;", "&lt;", "&gt;", "&apos;", "&quot;", "%r", "%n", "%t"];
        // Manifest messages use %N conventions for their message substitutions.   Translate from
        // .NET conventions.   We can't use RegEx for this (we are in mscorlib), so we do it 'by hand'
        private string TranslateToManifestConvention(string eventMessage, string evtName)
        {
            StringBuilder? stringBuilder = null;        // We lazily create this
            int writtenSoFar = 0;
            for (int i = 0; ;)
            {
                if (i >= eventMessage.Length)
                {
                    if (stringBuilder == null)
                        return eventMessage;
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    return stringBuilder.ToString();
                }

                int chIdx;
                if (eventMessage[i] == '%')
                {
                    // handle format message escaping character '%' by escaping it
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    stringBuilder.Append("%%");
                    i++;
                    writtenSoFar = i;
                }
                else if (i < eventMessage.Length - 1 &&
                    (eventMessage[i] == '{' && eventMessage[i + 1] == '{' || eventMessage[i] == '}' && eventMessage[i + 1] == '}'))
                {
                    // handle C# escaped '{" and '}'
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    stringBuilder.Append(eventMessage[i]);
                    i++; i++;
                    writtenSoFar = i;
                }
                else if (eventMessage[i] == '{')
                {
                    int leftBracket = i;
                    i++;
                    int argNum = 0;
                    while (i < eventMessage.Length && char.IsDigit(eventMessage[i]))
                    {
                        argNum = argNum * 10 + eventMessage[i] - '0';
                        i++;
                    }
                    if (i < eventMessage.Length && eventMessage[i] == '}')
                    {
                        i++;
                        UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, leftBracket - writtenSoFar);
                        int manIndex = TranslateIndexToManifestConvention(argNum, evtName);
                        stringBuilder.Append('%').Append(manIndex);
                        // An '!' after the insert specifier {n} will be interpreted as a literal.
                        // We'll escape it so that mc.exe does not attempt to consider it the
                        // beginning of a format string.
                        if (i < eventMessage.Length && eventMessage[i] == '!')
                        {
                            i++;
                            stringBuilder.Append("%!");
                        }
                        writtenSoFar = i;
                    }
                    else
                    {
                        ManifestError(SR.Format(SR.EventSource_UnsupportedMessageProperty, evtName, eventMessage));
                    }
                }
                else if ((chIdx = "&<>'\"\r\n\t".IndexOf(eventMessage[i])) >= 0)
                {
                    UpdateStringBuilder(ref stringBuilder, eventMessage, writtenSoFar, i - writtenSoFar);
                    i++;
                    stringBuilder.Append(s_escapes[chIdx]);
                    writtenSoFar = i;
                }
                else
                    i++;
            }
        }

        private int TranslateIndexToManifestConvention(int idx, string evtName)
        {
            if (perEventByteArrayArgIndices.TryGetValue(evtName, out List<int>? byteArrArgIndices))
            {
                foreach (int byArrIdx in byteArrArgIndices)
                {
                    if (idx >= byArrIdx)
                        ++idx;
                    else
                        break;
                }
            }
            return idx + 1;
        }

        private sealed class ChannelInfo
        {
            public string? Name;
            public ulong Keywords;
            public EventChannelAttribute? Attribs;
        }

        private readonly Dictionary<int, string> opcodeTab;
        private Dictionary<int, string>? taskTab;
        private Dictionary<int, ChannelInfo>? channelTab;
        private Dictionary<ulong, string>? keywordTab;
        private Dictionary<string, Type>? mapsTab;
        private readonly Dictionary<string, string> stringTab;       // Maps unlocalized strings to localized ones

        // WCF used EventSource to mimic a existing ETW manifest.   To support this
        // in just their case, we allowed them to specify the keywords associated
        // with their channels explicitly.   ValidPredefinedChannelKeywords is
        // this set of channel keywords that we allow to be explicitly set.  You
        // can ignore these bits otherwise.
        internal const ulong ValidPredefinedChannelKeywords = 0xF000000000000000;
        private ulong nextChannelKeywordBit = 0x8000000000000000;   // available Keyword bit to be used for next channel definition, grows down
        private const int MaxCountChannels = 8; // a manifest can defined at most 8 ETW channels

        private readonly StringBuilder? sb;               // Holds the provider information.
        private readonly StringBuilder? events;           // Holds the events.
        private readonly StringBuilder? templates;
        private readonly string providerName;
        private readonly ResourceManager? resources;      // Look up localized strings here.
        private readonly EventManifestOptions flags;
        private readonly List<string> errors;           // list of currently encountered errors
        private readonly Dictionary<string, List<int>> perEventByteArrayArgIndices;  // "event_name" -> List_of_Indices_of_Byte[]_Arg

        // State we track between StartEvent and EndEvent.
        private string? eventName;               // Name of the event currently being processed.
        private int numParams;                  // keeps track of the number of args the event has.
        private List<int>? byteArrArgIndices;   // keeps track of the index of each byte[] argument
#endregion
    }

    /// <summary>
    /// Used to send the m_rawManifest into the event dispatcher as a series of events.
    /// </summary>
    internal struct ManifestEnvelope
    {
        public const int MaxChunkSize = 0xF700;
        public enum ManifestFormats : byte
        {
            SimpleXmlFormat = 1,          // simply dump the XML manifest as UTF8
        }

        public ManifestFormats Format;
        public byte MajorVersion;
        public byte MinorVersion;
        public byte Magic;
        public ushort TotalChunks;
        public ushort ChunkNumber;
    }

#endregion
}
