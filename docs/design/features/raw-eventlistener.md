# Raw EventListener API #

The goal of this design document is to describe what a Raw EventListener API will look like and how it will work.

A raw API is one that dispatches events, but does not decode the data payload.  It simply provides a raw blob and a metadata blob that can be used to interpret the data when desired.

## API Surface ##

The existing EventListener API usage pattern is to:

1. Create a new class that derives from EventListener.
2. Implement the OnEventSourceCreated method to get callbacks when new EventSource objects are created (this allows EventListeners to subscribe to events).
3. Implement the OnEventWritten method to get callbacks whenever a subscribed event is dispatched.

The proposed extension of this pattern is the following:

1. Add a new EventListener constructor that takes an EventListenerSettings enum parameter:

```
[Flags]
public enum EventListenerSettings
{
   None,
   RawEventDispatch
}
```

This parameter is used to specify the desired dispatch behavior (in this case, do not deserialize event payloads).

2. Depending on the configuration, EventListener will call the traditional (deserialized payload) API or the raw API.

The new raw dispatch API will be:

```
public void OnEventWrittenRaw(RawEventWrittenEventArgs args);

public sealed class RawEventWrittenEventArgs
{

    // Event metadata copied from EventWrittenEventArgs (for consistency).
    public string EventName { get; }
    public int EventId { get; }
    public Guid ActivityId { get; }
    public Guid RelatedActivityId { get; }
    public EventSource EventSource { get; }
    public EventKeywords Keywords { get; }
    public EventOpcode Opcode { get; }
    public EventTask Task { get; }
    public EventTags Tags { get; }
    public string Message { get; }
    public byte Version { get; }
    public EventLevel Level { get; }
    public long OSThreadId { get; }
    public DateTime TimeStamp { get; }

    // Replacement properties for Payload and PayloadNames.
    public ReadOnlySpan<byte> Metadata { get; }
    public ReadOnlySpan<byte> Payload { get; }
}
```

The lifetime of the RawEventWrittenEventArgs object will be the lifetime of the callback.  Thus, we can recycle the object and reduce the overhead of this API.  This is similar to how TraceEvent works today, but is different than the existing EventListener.OnEventWritten callback.

NOTE: Until the API becomes public, testing can be performed by reflection using the following steps:

1. Set the configuration flags via private reflection.
2. Registering a callback that matches the required signature of the raw API using private reflection.

It is possible that due to this reflection requirement, some calls to the deserialized payload API will be made until EventListener discovers that this is no longer needed.  This will occur if setting the flag from the constructor.  This can be worked around by setting the flag from OnEventSourceCreated prior to calling EnableEvents.

## Architecture Changes ##

As of today, when an EventListener subscribes to an event, this is the determining factor on whether or not the event data is deserialized and the event dispatched.

The following changes will be needed to support raw dispatch:

1. Each EventListener has an EventListenerSettings field that represents the settings of the listener.  One of these settings will be the type of dispatch (deserialized or raw).
2. Each EventSource will have a new set of flags that represent the aggregate types of dispatch requested by the EventListeners that have subscribed to events from the EventSource.
3. The flags defined in #2 above will be used by EventSource.WriteToAllListeners to determine whether or not to deserialize the event payloads before dispatching the event.  4. EventSource.WriteToAllListeners will be updated to take both the serialized and deserialized payloads as parameters.  The dispatching code will iterate over the subscribed EventListeners as it does today, and for each EventListener, determine whether or not to call the dispatch event that takes the deserialized payload or the raw payload.
5. Each EventSource will save the metadata blobs produced by EventSource.DefineEventPipeEvents as a field within the EventMetadata struct.  The blob will be passed to the raw dispatch API when the event is dispatched so that it can be used by the consumer to decode the event.
6. The ILLink configuration for System.Private.CoreLib needs to be updated to opt-out the new type from trimming (since it will likely not become public in 2.2).
