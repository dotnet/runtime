# Activity User Guide

This document describes Activity, a class that allows storing and accessing diagnostics context and consuming it with logging system.

This document provides Activity architecture [overview](#overview) and [usage](#activity-usage).

# Overview
When application starts processing an operation e.g. HTTP request or task from queue, it creates an `Activity` to track it through the system as the request is processed. Examples of context stored in `Activity` could be HTTP request path, method, user-agent, or correlation id: all the details important to be logged along with every trace. 
When application calls external dependency to complete an operation, it may need to pass some of the context (e.g. correlation id) along with dependency call to be able to correlate logs from multiple services.

`Activity` provides [Tags](#tags) to represent context which is needed for logging only and [Baggage](#baggage) to represent context which needs to be propagated to external dependencies. It has other properties described in [Activity Reference](#activity-reference).

Every `Activity` has an `Id`, defining particular request in application, which is generated when Activity is started.

`Activity` (except root one) has a [Parent](#parent) (in-process or external). E.g. application calls external dependency while processing incoming request, so there is an Activity for dependency call and it has Parent Activity representing incoming call.
External dependency Activity Id is passed along with the request, so the dependency may use it as ParentId for its activities and thus allow unique mapping between child and parent calls. Parent is assigned when activity is started.

Activities may be created and started/stopped by platform code or frameworks; applications need to be notified about activities events and need to have access to current activity being processed.
Therefore code which creates activity also writes corresponding event to `DiagnosticSource`:
- DO - create new `DiagnosticListener` for specific Activity type to allow filtering by activity. E.g Incoming and Outgoing Http Requests should be different DiagnosticListeners. Follow [DiagnosticSource User Guide](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md) to pick a name.
- DO - Guard Activity creation and start with call to `DiagnosticSource.IsEnabled` to avoid creating activities when no-one is listening to them and to enable event name-based filtering or sampling.
- DO - Use `DiagnosticSource.StartActivity(Activity)` and `DiagnosticSource.StopActivity(Activity)` methods instead of Activity methods to ensure Activity events are always written to `DiagnosticSource`.
- DO - pass any necessary context to `DiagnosticListener`, so your application may enrich Activity. For example, in the case of an incoming HTTP request, the application needs an instance of `HttpContext` in order to add custom tags (method, path, user-agent, etc.)
- CONSIDER - keeping [Baggage](#baggage) as small as possible.
- DO NOT - add sensitive information to baggage, since it may be propagated out of the process boundaries.
- DO - write activity [Id](#id) with every telemetry event. [ParentId](#parentid), [Tags](#tags) and [Baggage](#baggage) should be written at least once per operation and could be found by Id. Note that Tags and Baggage could be changed through the lifetime of activity, and it makes sense to write them when the activity stops. [Duration](#duration) should be logged only when the activity stops.
- CONSIDER - writing activity [RootId](#root-id) with every telemetry event *if* filtering by Id prefix is not supported by you logging backend or too expensive.

The current activity is exposed as static variable, `Activity.Current`, and flows with call context, including async calls, so that it is available in every Start/Stop event callback.

Applications may access `Activity.Current` anywhere in the code to log events along with the context stored in Activity.
# Activity Usage
At the moment an application writes log record, it can access `Activity.Current` to get all required details from it.

## Creating Activities
```C#
    Activity activity = new Activity("Http_In");
```
An activity must be created with an operation name. This is a coarse name that is useful for grouping and filtering log records.

After an Activity is created you can add additional details: [Start time](#starttimeutc), [Tags](#tags) and [Baggage](#baggage)
```C#
   activity.SetStartTime(GetHighPrecisionTimestamp())
           .AddTag("Path", request.Path)
           .AddBaggage("CorrelationId", request.Headers["x-ms-correlation-id"]);
```
Once an activity has been built, it's time to start it and continue with request processing.

## Starting and Stopping Activity

The `Start()` and `Stop()` methods maintain [Activity.Current](current) which flows with async calls and is available during request processing.
When that activity is started, it is given an [Id](id) and a [Parent](parent).

```C#
   public void OnIncomingRequest(DiagnosticListener httpListener, HttpContext request)
   {
       if (httpListener.IsEnabled("Http_In"))
       {
           Activity activity = new Activity("Http_In");
           activity.SetStartTime(GetHighPrecisionTimestamp())
           //add tags, baggage, etc.
           activity.SetParentId(context.Request.headers["x-ms-request-id"])
           foreach (var header in context.Request.Headers)
             if (header.Key.StartsWith("x-baggage-")
                 activity.AddBaggage(header.Key, header.Value);
                 
           httpListener.StartActivity(activity, httpContext);
           try {
               //process request ...
           } finally {
               activity.SetEndTime(GetHighPrecisionTimestamp());
               //stop activity
               httpListener.StopActivity(activity, highPrecisionStopTime);
           }       
       }
   }
```
**Note** 
- instead of Activity.Start() and Stop() methods, in above example we call `DiagnosticSource.StartActivity()` and `StopActivity()` methods that write events to DiagnosticSource.
- Activity creation is guarded with a call to `DiagnosticSource.IsEnabled` thus eliminating any unnecessary performance impact if no-one is listening to this `DiagnosticSource`.
- DateTime.UtcNow in practice is [accurate to 16 milliseconds](https://blogs.msdn.microsoft.com/ericlippert/2010/04/08/precision-and-accuracy-of-datetime/). If you want better accuracy, call [SetStartTime](#setstarttime) and [SetEndTime](#setendtime) with a timestamp derived from a combination of `DateTime` and `System.Diagnostics.Stopwatch`.


## Creating child Activities
When an application makes an outbound call, for example to an external web-service, a new activity should be created. If this child activity is part of an existing activity then its Parent will be assigned automatically during Start().

```C#
    public void OnOutgoingRequest(DiagnosticListener httpListener, HttpRequestMessage request)
    {
        if (httpListener.IsEnabled(request.RequestUri.ToString()))
        {
            var activity = new Activity("Http_Out");
            httpListener.StartActivity(activity, request);

            request.Headers.Add("x-ms-request-id", activity.Id);
            foreach (var baggage in activity.Baggage)
                request.Headers.Add(baggage.Key, baggage.Value);
            try {
               //process request ...
            } finally {
                activity.SetEndTime(GetHighPrecisionTimestamp());
                //stop activity
                httpListener.StopActivity(activity, value.Value, DateTimeStopwatch.GetTime(timestamp));
            }       
        }   
    }
```

The child Activity will automatically inherit Baggage from its parent. The above example also demonstrates how baggage could be propagated to a downstream web service in HTTP request headers.

Just as in the previous example, activity creation should be guarded with a `DiagnosticSource.IsEnabled()` call. In this case, however, it prevents headers injection based on request Uri.
Note that different DiagnosticSources should be used for incoming and outgoing HTTP activities allowing you to implement separate filtering for events.

## Listening to Activity Events
An application may listen to activity events and log them. It can access `Activity.Current` to get information about the current activity.
This follows normal [DiagnosticListener conventions](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md).

An application may also add tags and baggage to the current activity when processing an Activity start callback.
Note that in the [Incoming Request Sample](#starting-and-stopping-activity), we pass `HttpContext` to DiagnosticSource, so that the application has access to the request properties in order to enrich the current activity.

### Subscribe to DiagnosticSource
```
    DiagnosticListener.AllListeners.Subscribe(delegate (DiagnosticListener listener)
    {
        if (listener.Name == "MyActivitySource")
        {
            listener.Subscribe(delegate (KeyValuePair<string, object> value)
            {
                if (value.Key.EndsWith("Start", StringComparison.Ordinal))
                    LogActivityStart();
                else if (value.Key.EndsWith("Stop", StringComparison.Ordinal))
                    LogActivityStop();
            });
        }
    }
```

### Log Events

```
	public void LogActivityStart()
	{
		var document = new Dictionary<string,object>
		{
			["Message"] = $"Activity {activity.OperationName} was started",
			["LogLevel"] = LogLevel.Info,
			["Id"] = activity.Id,
			["ParentId"] = activity.ParentId,
			["StartTime"] = activity.StartTimeUtc,
		}
		//log tags and baggage if needed
		...// send document to log storage	   
	}

	public void LogActivityStop()
	{
		var document = new Dictionary<string,object>
		{
			["Message"] = $"Activity {activity.OperationName} is being stopped",
			["LogLevel"] = LogLevel.Info,
			["Id"] = activity.Id,
			["ParentId"] = activity.ParentId,
			["Duration"] = activity.Duration
		};
        foreach (var kv in activity.Tags)
            document[kv.Key] = kv.Value;
        foreach (var kv in activity.Baggage)
            document[kv.Key] = kv.Value;
		...// send document to log storage
	}

	public void Log(LogLevel level, string message)
	{
		var document = new Dictionary<string,object>
		{
			["Message"] = message,
			["LogLevel"] = logLevel,
		};

	    if (Activity.Current != null)
	    {
			document["Id"] = activity.Id;
			//add tags, baggage and ParentId if needed
		}
		...// send document to log storage
	}
```

It's crucial that Activity Id is logged along with every event. ParentId, Tags and Baggage must be logged at least once for every activity and may be logged with every telemetry event to simplify querying and aggregation. Duration is only available after SetEndTime is called and should be logged when Activity Stop event is received.

## Activity Id
The main goal of Activity is to ensure telemetry events could be correlated in order to trace user requests and Activity.Id is the key part of this functionality.

Applications start Activity to represent logical piece of work to be done; one Activity may be started as a child of another Activity. 
The whole operation may be represented as a tree of Activities. All operations done by the distributed system may be represented as a forest of Activities trees.
Id uniquely identifies Activity in the forest. It has an hierarchical structure to efficiently describe the operation as Activity tree.

Activity.Id serves as hierarchical Request-Id in terms of [HTTP standard proposal for correlation](https://github.com/lmolkova/correlation/blob/master/http_protocol_proposal_v1.md) 

### Id Format

`/root-id.id1.id2.id3...`

It starts with '/' followed by [root-id](#root-id) followed by '.' and small identifiers of local Activities, separated by '.'. 

[Root-id](#root-id) identifies the whole operation and 'Id' identifies particular Actvity involved in operation processing.

'/' indicates Id has hierarchcal structure, which is useful information for logging system. 

* Id is 128 bytes or shorter
* Id consist of [Base64](https://en.wikipedia.org/wiki/Base64), '-' (dash), '.' (dot) and '#' (pound) characters.

### Root Id
When you start the first Activity for the operation, you may provide root-id through `Activity.SetParentId(string)` API. When Activity is actually started, it will generate own Id by appending relatively small suffix (preceeded with '.') to root-id. E.g. `/<root-id>.2341`

Root-Id:
* MUST be sufficiently large to identify single operation: use 64-bit random number or Guid
* MUST be 64 bytes or shorter.
* MUST contain [Base64 characters](https://en.wikipedia.org/wiki/Base64) and '-' (dash)

If you don't provide it, Activity will generate root-id: e.g. `<root-id>` without suffix.

If provided ParentId does not start with '/', Activity will prepend it's own Id with '/' and will keep ParentId intact.
To get root id, use `Activity.RootId` property after providing ParentId or after starting Activity. 

### Child Avtivities and Parent Id
Any child Activity started in the same process as it's parent, will take Parent.Id and generate own Id by appending integer suffix to Parent.Id: e.g. `<Parent.Id>.1`. Suffix is an integer number of child activity started from the same parent.

Activities which parent is external to the process, should be assigned with Parent-Id (before start) with `Activity.SetParentId(string)` API. Activity would use another suffix for Id, as described in [Root Id](#root-id) section.

If ParentId does not start with '/', Activity will add prepend it's own Id with '/' and will keep ParentId intact.

So the Activity generates Id in following format `parent-id.local-id`, where `parent-id` may have similar hierarchical structure.

### Id overflow
Appending local-id to Parent.Id may cause Id to exceed length limit.
In case of overflow, last bytes of Parent.Id are trimmed to make a room for 32-bit random lower-hex encoded integer preceeded with '#': `<Beginning-Of-Parent-Id>#local-id`

# Reference

## Activity 
### Tags
`IEnumerable<KeyValuePair<string, string>> Tags { get; }` - Represents information to be logged along with the activity. Good examples of tags are instance/machine name, incoming request HTTP method, path, user/user-agent, etc. Tags are **not passed** to child of activities.
Typical tag usage includes adding a few custom tags and enumeration through them to fill log event payload. Retrieving a tag by its key is not supported.

### Baggage
`IEnumerable<KeyValuePair<string, string>> Baggage { get; }` - Represents information to be logged with the activity **and** passed to its children. Examples of baggage include correlation id, sampling and feature flags.
Baggage is serialized and **passed along with external dependency requests**.
Typical Baggage usage includes adding a few baggage properties and enumeration through them to fill log event payload. 

### OperationName
`string OperationName { get; }` - Coarsest name for an activity. This name must be set in the constructor.
 
### StartTimeUtc
`DateTime StartTimeUtc { get; private set; }`  - DateTime in UTC (Greenwich Mean Time) when activity was started. If it's not already initialized, it will be set to DateTime.UtcNow in `Start`.

### Duration 
`TimeSpan Duration { get; private set; }` - Represents Activity duration if activity was stopped, TimeSpan.Zero otherwise.

### Id
`string Id { get; private set; }` - Represents particular activity identifier. Filtering to a particular Id insures that you get only log records related to specific request within the operation. It is generated when the activity is started.
Id is passed to external dependencies and considered as [ParentId](#parentid) for new external activity.

### ParentId
`string ParentId { get; private set; }` - Activity may have either an in-process [Parent](#parent) or an external Parent if it was deserialized from request. ParentId together with Id represent the parent-child relationship in logs and allows you to correlate outgoing and incoming requests.

### RootId
`string RootId  { get; private set; }` - Returns [root id](#root-id): Id (or ParentId) substring from '/' to first '.' occurence.

### Current
`static Activity Current { get; }` - Returns current Activity which flows across async calls.

### Parent
`Activity Parent { get; private set; }` - If activity was created from another activity in the same process, you can get that Activity with the Parent accessor. However this can be null if the Activity is root activity or parent is from outside the process.

### Start()
`Activity Start()` - Starts Activity: sets Activity.Current and Parent for the activity, generates a unique Id and sets the StartTimeUtc if not already set.

### Stop()
`void Stop()` - Stops Activity: sets Activity.Current and Duration for the activity. Uses timestamp provided in [SetEndTime](#setendtime) or DateTime.UtcNow.

### AddBaggage()
`Activity AddBaggage(string key, string value)` - adds a baggage item. See [Baggage](#baggage).

### GetBaggageItem()
`string GetBaggageItem(string key)` - returns the value of [Baggage](#baggage) key-value pair with given key or null if the key does not exist.

### AddTag()
`Activity AddTag(string key, string value)` - adds a tag. See [Tags](#tags).

### SetParentId()
`Activity SetParentId(string parentId)` - sets the parent Id. See [ParentId](#parentid).

### SetStartTime()
`Activity SetStartTime(DateTime startTimeUtc)` - sets the start time. See [StartTimeUtc](#starttimeutc).

### SetEndTime()
`Activity SetEndTime(DateTime endTimeUtc)` - sets [Duration](#duration) as a difference between endTimeUtc and [StartTimeUtc](#starttimeutc).

##DiagnosticSource
### StartActivity
`Activity StartActivity(Activity activity, object args)` - Starts the given activity and writes DiagnosticSource event message OperationName.Start with args payload.
### StopActivity
`void StopActivity(Activity activity, object args)` - Stops the given activity and writes DiagnosticSource event OperationName.Stop with args payload.
