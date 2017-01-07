# Activity User Guide

This document describes Activity, a class that allows storing and accessing diagnostics context and consuming it with logging system.

This document provides Activity architecture [overview](#overview) and [usage](#activity-usage).

# Overview
When application starts procesing an operation e.g. HTTP request or task from queue, it creates an `Activity` to track it through the system as the request is processed. Examples of context stored in `Activity` could be HTTP request path, method, user-agent, or correlation id: all the details important to be logged along with every trace. 
When application calls external dependency to complete an operation, it may need to pass some of the context (e.g. correlation id) along with dependency call to be able to correlate logs from multiple services.

`Activity` provides [Tags](#tags) to represent context which is needed for logging only and [Baggage](#baggage) to represent context which needs to be propagated to external dependencies. It has other properties described in [Activity Reference](#activity-reference).

Every `Activity` has an `Id`, defining particular request in application, which is generated when Actvitity is started.

`Activity` (except root one) has a [Parent](#parent) (in-process or external). E.g. application calls external dependency while processing incoming request, so there is an Activity for dependency call and it has Parent Activity representing incoming call.
External dependency Activity Id is passed along with the request, so the dependency may use it as ParentId for it's activities and thus allow unique mapping between child and parent calls. Parent is assigned when activity is started.

Activities may be created and started/stopped by platform code or frameworks; applications need to be notified about activities events and need to have access to current activity being processed.
Therefore code which creates activity also writes corresponding event to `DiagnosticSource`:
- DO - create new `DiagnosticListener` for specific Activity type to allow filtering by activity. E.g Incoming and Outgoing Http Requests should be different DiagnosticListeners. Follow [DiagnosticSource User Guilde](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md) to peek a name.
- DO - Guard Activity creation and start with call to `DiagnosticSource.IsEnabled` to avoid creating activities if no one listens to them and enable event name-based filtering.
- DO - Use `DiagnosticSource.Start(Actvity)` and `DiagnosticSource.Stop(Actvity)` (extension) methods instead of Activity methods to ensure Activity events are always written to `DiagnosticSource`.
- DO - pass necessary context to `DiagnosticListener`, so application may enrich Activity. In case of HTTP incoming request, application needs `HttpContext` to add custom tags (method, path, user-agent, etc)

Current Activity is exposed as statis variable and flows with async calls so in every Start/Stop event callback it is accurate and may be used to log these events.

Applications may access `Activity.Current` anywhere in the code to log events along with the context stored in Activity.
# Activity Usage
At the moment application writes log record, it can access `Activity.Current` to get all required details from it.

## Creating Activities
```C#
    Activity activity = new Activity("Http_In");
```
Activity can be created with operation name. This is an coarse name that is useful for grouping and filtering log records.

After Activity is created you can add additional details: [Start time](#starttimeutc), [Tags](#tags) and [Baggage](#baggage)
```C#
   activity.WithStartTime(highPrecisionStartTime)
           .WithTag("Path", request.Path)
           .WithBaggage("CorrelationId", request.Headers["x-ms-correlation-id"]);
```

When activity is built, it's time to start it and continue with request processing.

## Starting and Stoping Activity

Activity.Start() and Stop() methods maitain [Activity.Current](current) which flows with async calls and available during request processing.
When activity is started, it assigned with an [Id](id) and [Parent](parent).

```C#
   public void OnIncomingRequest(DiagnosticListener httpListener, HttpContext request)
   {
       if (httpListener.IsEnabled("Http_In"))
       {
           Activity activity = new Activity("Http_In");           
           //add tags, baggage, etc..           
           activity.WithParentId(context.Request.headers["x-ms-request-id"])
           foreach (var header in context.Request.Headers)
             if (header.Key.StartsWith("x-baggage-")
                 activity.WithBaggage(header.Key, header.Value);
                 
           httpListener.Start(activity, httpContext);               
           try {
               //process request ...
           } finally {
               //stop activity
               httpListener.Stop(activity, highPrecisionStopTime);
           }       
       }
   }
```
Note that instead of Activity.Start() and Stop() methods, in above example we call `DiagnosticSource.Start()` and `Stop()` (extension) methods that fire events to be consumed with DiagnosticListener.
Note that Activities creation is guarded with `DiagnosticSource.IsEnabled` and will only happen if someone listens to this `DiagnosticSource` thus eliminating any unnecessary performance impact.

## Creating child Activities
When application calls external web-service, new activity is created to represent external operation. This activity may have Parent (if this request is part of incoming request processing), assigned to it during Start().

```C#
    public void OnOutgoingRequest(DiagnosticListener httpListener, HttpRequestMessage request)
    {
        if (httpListener.IsEnabled(request.RequestUri.ToString()))
        {
            var activity = new Activity("Http_Out");
            httpListener.Start(activity, request);

            request.Headers.Add("x-ms-request-id", activity.Id);
            foreach (var baggage in activity.Baggage)
                request.Headers.Add(baggage.Key, baggage.Value);
            try {
               //process request ...
            } finally {
                //stop activity
                httpListener.Stop(activity, value.Value, DateTimeStopwatch.GetTime(timestamp));
            }       
        }   
    }
```

New Activity will inherit Baggage from parent. Above example demonstrates how baggage could be propagated to downstream web-service in HTTP request headers.

Similarly to incoming request processing, activity creation should be guarded with `DiagnosticSource.IsEnabled()` call, however allowing to filter headers injection based on request Uri.
Note that different DiagnosticSources should be used for incoming and outgoing HTTP activities allowing to implement separate filtering for events.

## Listening to Activity Events
Application may listen to activity events and log them. It can access `Activity.Current` to get information about current activity.
This follows normal [DiagnosticListener conventions](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md)

Application may also add tags and baggage to current activity when processing Activity start callback.
Note in the [Incoming Request Sample](#starting-and-stoping-activity), we pass `HttpContext` to DiagnosticSource, so application may access request properties to enrich current activity.

# Reference
## Activity
### Tags
`IEnumerable<KeyValuePair<string, string>> Tags { get; }` - Represents information to be logged along with activity. Good examples of tags are instance/machine name, incoming request HTTP method, path, user/user-agent, etc. Tags are **not passed** to the children of Activity.
Typical tag usage includes adding a few custom tags and enumeration through them to fill log event payload. Getting tag by it's key is not supported.

### Baggage
`IEnumerable<KeyValuePair<string, string>> Baggage { get; }` - Represents information to be logged woth activity AND passed to it's children. Examples of baggage include correlation id, sampling and feature flags.
Baggage is serialized and **passed it along with external dependency request**.
Typical Baggage usage includes adding a few baggage properties and enumeration through them to fill log event payload. 

### OperationName
`string OperationName { get; }` - Coarset name for an activity
 
### StartTimeUtc
`DateTime StartTimeUtc { get; private set; }`  - DateTime in UTC (Greenwitch Mean Time) when activity was started. DateTime.UtcNow if not specified

### Duration 
`TimeSpan Duration { get; private set; }` - Represent Activity duration if activity was stopped, TimeSpan.Zero otherwise

### Id
`string Id { get; private set; }` - Represents particular activity identifier. Filtering to a particular Id insures that you get only log records related to specific request within the operation. It is assigned when activity is started.
Id is passed to external dependencies and considered as [ParentId](#parentid) for new external activity.

### ParentId
`string ParentId { get; private set; }` - Activity may have either in-process [Parent](#parent) or Id of external Parent if it was deserialized from request. ParentId together with Id represent parent-child relationship in logs and allows to uniquely map outgoing and incoming requests.
ParentId is implemented as a tag (see [Tags](#tags)).

### Current
`static Activity Current { get; }` - Returns current Activity which flows across async calls

### Parent
`public Activity Parent { get; private set; }` - If activity was created from another activity in the same process, you can get that Activity with the Parent accessor. However this can be null if the Activity is root activity or parent is from outside the process.

### Start()
`static Activity Start(Activity activity)` - Starts Activity: sets Activity.Current and Parent for the activity.

### Stop()
`static void Stop(Activity activity, DateTime stopTimeUtc = default(DateTime))` - Stops Activity: sets Activity.Current and Diration for the activity. Uses DateTime.UtcNow by default as stopTimeUtc if not provided.

### WithBaggage()
`Activity WithBaggage(string key, string value)` - adds baggage item, see [Baggage](#baggage)

### GetBaggageItem()
`string GetBaggageItem(string key)` - returns value of [Baggage](#baggage) key-value pair with given key, null if key does not exist

### WithTag()
`Activity WithTag(string key, string value)` - adds tag, see [Tags](#tags)

### WithParentId()
`Activity WithParentId(string key, string value)` - sets parent Id, see [ParentId](#parentid)

### WithStartTime()
`Activity WithStartTime(DateTime startTimeUtc)` - sets parent Id, see [StartTimeUtc](#starttimeutc)

##DiagnosticSource
### Start
`static Activity Start(this DiagnosticSource self, Activity activity, object args)` - Starts activity and writes DiagnosticSource event
### Stop
`static Activity Start(this DiagnosticSource self, string activityName, object args)` - Stops activity and writes DiagnosticSource event
