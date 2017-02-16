# HttpClient Diagnostic  Instrumentation Users Guide

This document describes `HttpClientHandler` instrumentation with [DiagnosticSource](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md).

# Overview
Applications typically log outgoing HTTP requests; typically it's done with DelegatingHandler implementation that logs every request. However in existing system it may require extensive code changes, since DelegatingHandler needs to be added to HttpClient pipeline every time when new client is created.
DiagnosticListener instrumentation allows to enable outgoing request tracing with a few lines of code; it also provides context necessary to correlate logs.

## Subscription
Instrumentation is off by default; to enable it, consumer first needs to subscribe to DiagnosticListener called "HttpHandlerDiagnosticListener". 

```C#
var subscription = DiagnosticListener.AllListeners.Subscribe(delegate (DiagnosticListener listener)
{
    if (listener.Name == "HttpHandlerDiagnosticListener")
    {
        listener.Subscribe(delegate (KeyValuePair<string, object> value)
        {
            Console.WriteLine($"Event: {value.Key} ActivityName: {Activity.Current.OperationName} Id: {Activity.Current.Id} ");
        });
    }
} );
```

## Events
If there is a consumer, subscribed for "HttpHandlerDiagnosticListener" events, `HttpClientHandler` instruments outgoing request depending on subscription and request properties as well as request context.

Request context is represented as `System.Diagnostics.Activity` class. Activity describes context for logical piece of work. Activity may be started as a child of another Activity and the whole operation is represented with a tree of Activities. You can find more details in [Activity User Guide](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/ActivityUserGuide.md).

Activity carries useful properties for logging such as Id, start time, Tags, Baggage and parent information. 

Instrumentation ensures `Activity.Current` represents current outgoing request in Write event callbacks (if request is instrumented). Consumers **should not** assume `Activity.Current` is accurate in IsEnabled callbacks.

Instrumentation starts a new Activity for outgoing Http request as a child of some other Activity (e.g. incoming request Activity). If there is no parent Activity, outgoing request **will not** be instrumented. This is important for sampling scenarios to minimize instrumentation costs.

Instrumentation also checks if user has enabled "System.Net.Http.Activity" event through the optional predicate provided in `DiagnosticListener.Subscribe`  method. 

### Preconditions
Summarizing preconditions for instrumentation:

1. There must be **at least one consumer** for "HttpHandlerDiagnosticListener"(*)
2. There must be **parent Activity** (Activity.Current must not be null)
3. Consumer must enable **System.Net.Http.Activity** event 

(*) Avoid having more than one consumer for DiagnosticListener. Note that DiagnosticListener best practice is to guard every `Write` method with `IsEnabled` check. In case there is more than one consumer, **each consumer** will receive Write event if **at least one** consumer returned true for `IsEnabled`.

#### IsEnabled: System.Net.Http.Activity
If instrumentation preconditions are met, there is another check to ensure consumer wants this particular request to be instrumented.
It's done with `DiagnosticListener.IsEnabled("System.Net.Http.Activity", request)` call.
Consumer may optionally provide predicate to DiagnosticListener to prevent some requests from being instrumented: e.g. if logging system has HTTP interface, it could be necessary to filter out requests to logging system itself.

```C#
    var predicate = (name, r, _) => 
    {
        var request = r as HttpRequestMessage;
        if (request != null)
        {
           return !request.RequestUri.Equals(ElasticSearchUri);
        }
        return true;
    }
    listener.Subscribe(observer, predicate);
```

#### IsEnabled: System.Net.Http.Activity.Propagate
In order to correlate logs in distributed systems, instrumentation adds context into the request headers: 
 * Request-Id header with `Activity.Id` value
 * Correlation-Context header with `Activity.Baggage` key-value pair list in `k1=v1, k2=v2` format
 
See [Http Protocol proposal](https://github.com/lmolkova/correlation/blob/master/http_protocol_proposal_v1.md) for more details

[comment]: TODO: Update link once it's moved
 
Instrumentation will call `DiagnosticListener.IsEnabled("System.Net.Http.Activity.Propagate")` method, so consumer may enable or disable headers injection.

```C#
    listener.Subscribe(observer, name != "System.Net.Http.Activity.Propagate");
```

Above example demonstrates predicate that allows to instrument the request, but does not allow context propagation through headers. Note that in this case telemetry from downstream services cannot be correlated with telemetry from current and upstream services. 

#### IsEnabled: System.Net.Http.Exception
If request processing throws exception, instrumentation first checks if consumer wants to receive this event.

### System.Net.Http.Activity.Start
After initial instrumentation preconditions are met and `DiagnosticListener.IsEnabled("System.Net.Http.Activity", request)` check is passed, instrumentation starts a new Activity to represent outgoing request and writes  **"System.Net.Http.Activity.Start"** event.  Event payload has Request property with `HttpRequestMessage` object representing request.

### System.Net.Http.Activity.Stop
When request is completed (faulted with exception, cancelled or successfully completed), instrumentation stops activity and writes  **"System.Net.Http.Activity.Stop"** event.

Event payload has following properties:
* **Response**  with `HttpResponseMessage` object representing response, which could be null if request was failed or cancelled.  
* **RequestTaskStatus** with `TaskStatus` enum value that describes status of the request task.

This event is sent under the same conditions as "System.Net.Http.Activity.Start" event.

### System.Net.Http.Exception
If request throws an exception, instrumentation sends **"System.Net.Http.Exception"** event with payload containing `Exception` property.
Exception event is sent if initial instrumentation preconditions are met and does not require current outgoing request to be instrumented.

If current outgoing request is instrumented, `Activity.Current` represents it's context.
Otherwise, `Activity.Current` represent some 'parent' activity (presumably incoming request).

# Events Flow and Order

1. `DiagnosticListener.IsEnabled()` - determines if there is a consumer
2. `DiagnosticListener.IsEnabled("System.Net.Http.Activity")` - determines if consumer is interested in any Activity events
3. `DiagnosticListener.IsEnabled("System.Net.Http.Activity", request)` - determines if this particular request should be instrumented
4. `DiagnosticListener.Write("System.Net.Http.Activity.Start", new {Request})` - notifies that activity (outgoing request) was started
5. `DiagnosticListener.IsEnabled("System.Net.Http.Exception")` - determines if exception event (if thrown) should be written
6. `DiagnosticListener.Write("System.Net.Http.Activity.Exception", new {Exception})` - notifies about exception during request processing (if thrown)
7. `DiagnosticListener.Write("System.Net.Http.Activity.Stop", new {Response, RequestTaskStatus})` - notifies that activity (outgoing request) is stopping

# Non-Activity events (deprecated)
If there is a subscriber to "HttpHandlerDiagnosticListener", but Activity events are disabled, instrumentation attempts to send legacy "System.Net.Http.Request" and "System.Net.Http.Response" events if they are enabled.
Consumers should consider migrating to Activity events instead of Request/Response events.


