# DiagnosticSource User's Guide

This document describes `DiagnosticSource`, a simple module that allows code
to be instrumented for production-time logging of **rich data payloads** for
consumption **within the process** that was instrumented. At runtime consumers
can **dynamically discover** data sources and subscribe to the ones of interest.

In addition to background on how the class works, this document also covers
[naming conventions](#naming-conventions) and [best practices](#best-practices) when
instrumenting code.

-------------------------------------------
## Relationship to Other Logging Facilities

In addition to `DiagnosticSource`, there are two other logging systems provided by Microsoft:

1. `EventSource` [docs](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx)
   and [src](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs).
   `EventSource` has been available since V4.5 of the .NET Runtime and is what is used
   to instrument the runtime itself. It is designed to be fast and to be strongly
   typed (payloads are typed, named properties), and to interface with OS logging
   infrastructure like Event Tracing for Windows [(ETW)](https://msdn.microsoft.com/en-us/library/windows/desktop/aa363668(v=vs.85).aspx)
   or [LTTng](http://lttng.org/) on Linux.

2. `ILogger` [src](https://github.com/aspnet/logging). A number of popular third party
   formatted string logging systems for .NET have been built including NLog, SeriLog,
   and Log4Net. The `ILogger` NuGet package is designed to 'wrap' any of these and
   hide from the instrumentation code which exact logging system is being used. Using
   this wrapper makes the most sense if your goal is to 'plug into' a logging pipeline
   that assumes one of these logging systems.

`DiagnosticSource` has more architectural similarity to `EventSource`. The main difference
is that `EventSource` assumes that the data being logged will **leave the process** and thus
requires that **only serializable data** be logged. However, `DiagnosticSource` was designed
to allow in-process tools to get at very rich data. Because the consumer is assumed to be
within the same process, non-serializable types (e.g. `HttpResponseMessage` or `HttpContext`)
can be passed, which gives the consumer a lot of potential data to work with.

As explained in [DiagnosticSourceEventSource.cs](System/Diagnostics/DiagnosticSourceEventSource.cs),
there is a bridge that pipes information from `DiagnosticSources` to an `EventSource`. Thus `EventSource`
consumers can get at all `DiagnosticSource` events. While the data payloads from
`DiagnosticSource` can't in general be passed through to the `EventSource` (because they are
not serializable), there is a mechanism in the bridge that enables consumers to specify which fields
to pass along to the `EventSource`.

What this means is that in general it is not necessary to instrument a code site multiple
times. By instrumenting with Diagnostic source, both clients that need the rich data
(and thus use `DiagnosticListener`) as well as any consumers using `EventListeners`
(or OS facilities like ETW) can get at the data.

----------------------------------------
## Instrumenting with DiagnosticSource/DiagnosticListener

Perhaps surprisingly, the heart of the `DiagnosticSource` logging architecture is not the
`DiagnosticSource` class but rather the `DiagnosticListener` class which 'receives' the
events. This is because the `DiagnosticSource` type is just an abstract base class
that defines the methods needed to actually log events. It is the  `DiagnosticListener`
which holds the actual implementation.

Thus the first step in instrumenting code with `DiagnosticSource` is to create a
`DiagnosticListener`. For example:

```C#
    private static DiagnosticSource httpLogger = new DiagnosticListener("System.Net.Http");
```
Notice that httpLogger is typed as a `DiagnosticSource`. This is because this code
only cares about writing events and thus only cares about the  `DiagnosticSource` methods that
the `DiagnosticListener` implements. `DiagnosticListeners` are given names when they are created
and this name should be the name of logical grouping of related events (typically the component).
Later this name is used to find the Listener and subscribe to any of its events.

Once you have an instance of a `DiagnosticSource`, logging is very straightforward. The
interface consists of only two methods:

```C#
    bool IsEnabled(string name)
    void Write(string name, object value);
```

A typical call site will look like:

```C#
    if (httpLogger.IsEnabled("RequestStart"))
        httpLogger.Write("RequestStart", new { Url="http://clr", Request=aRequest });
```

Already some of the architectural elements are being exposed, namely:

1. Every event has a `string` name (e.g. `RequestStart`), and exactly one `object` as a payload.

2. If you need to send more than one item, you can do so by creating an `object` with all information
   in it as properties. C#'s [anonymous type](https://msdn.microsoft.com/en-us/library/bb397696.aspx)
   feature is typically used to create a type to pass 'on the fly', and makes this scheme very
   convenient.

3. At the instrumentation site, you must guard the call to `Write()` with an `IsEnabled()` check on
   the same event name. Without this check, even when the instrumentation is inactive, the rules
   of the C# language require all the work of creating the payload `object` and calling `Write()` to be
   done, even though nothing is actually listening for the data. By guarding the `Write()` call, we
   make it efficient when the source is not enabled.

### Creating DiagnosticSources (Actually DiagnosticListeners)

Perhaps confusingly you make a `DiagnosticSource` by creating a `DiagnosticListener`:

```C#
    static DiagnosticSource mySource = new DiagnosticListener("System.Net.Http");
```

Basically a `DiagnosticListener` is a named place where a source sends its information (events).
From an implementation point of view, `DiagnosticSource` is an abstract class that has the two
instrumentation methods, and `DiagnosticListener` is something that implements that abstract class.
Thus every `DiagnosticListener` is a `DiagnosticSource`, and by making a `DiagnosticListener` you
implicitly make a `DiagnosticSource` as well.

`DiagnosticListeners` have a name, which is used to represent the component associated with the event.
Thus the event names only need to be unique within a component.

----------------------------------------
## Best Practices

### Naming Conventions

#### DiagnosticListener Names

* CONSIDER - the likely scenarios for USING information when deciding how many
  `DiagnosticListener` to have and the events in each. Keep in mind that it is **very easy
  and efficient** to filter all the events in a particular listener so ideally the
  most important scenarios involve turning on whole listeners and not needing to filter
  for particular events. You may need to split
  a source into multiple smaller ones to achieve this, and this is OK. For example there
  are both incoming HTTP requests and outgoing HTTP requests and you may only need one
  or the other, so having a System.Net.Http.Incoming and System.Net.Http.Outgoing for
  each sub-case is good.

* CONSIDER -  the likely volume of events. High volume events may deserve their own
  `DiagnosticListener`. You don't really want to mix high volume and low volume events
  in the same listener unless they both support the same scenario. It is OK however to
  put several **low volume** events in a 'miscellaneous' listener, even if they support different
  scenarios if it simplifies things enough.

* DO - Consider the scenario when picking the name for the `DiagnosticListener`. Often, this
  name is the component in which the `DiagnosticListener` lives, but **usage scenarios trump
  component naming**. You want it to be the case that users can correctly guess which
  listeners to activate knowing just their scenario.

* DO - Make the name for the `DiagnosticListeners` **globally unique**. This is typically
  done by making the first part of the name the component (e.g. System.Net.Http)

* DO - Use dots '.' to create multi-part names. This works well if the name is a Name
  of a component (which uses dots).

* DO NOT - name the listener after the Listener (thus something like System.Net.HttpDiagnosticListener
  is bad).



#### Event Names

* DO - keep the names reasonably short (< 16 characters). Keep in mind that event names
  are already qualified by the Listener so the name only needs to be unique within a listener.
  Short names make `IsEnabled()` faster.

* DO - use activities (see [Activity Users Guide](ActivityUserGuide.md)) for events that are
  marking the beginning and end of an interval of time.   The key value of Activities is that they
  indicate that they represent a DURATION, and they also track what 'caused' them (and thus
  logging systems can stitch together a 'causality graph').

* DO - If for some reason you can't use Activities, and your events mark the start and stop of
  an interval of time, use the 'Start' and 'Stop' suffixes on the events.

### Payloads

* DO use the anonymous type syntax 'new { property1 = value1 ...}' as the default way to pass
  a payload *even if there is only one data element*. This makes adding more data later easy
  and compatible.

* CONSIDER creating an explicit type for the payload.   The main value for doing this is that the
  receiver can cast the received object to that type and immediately fetch fields (with anonymous types
  reflection must be used to fetch fields).   This is both easier to program and more efficient.
  Thus in scenarios where there is likely high-volume filtering to be done by the logging listener, having
  this type available to do the cast is valuable.   Note that this type needs to be made public (since
  the listener needs to see it), and should be under the namespace System.Diagnostics.DiagnosticSource.PayloadTypes.
  Note that if there is doubt about the value DO NOT create an explicit type, as you CAN convert from
  an anonymous type to a explicit type compatibly in the future, but once you expose the payload type
  you must keep it forever.   The payload type should simply have C#  'TYPE NAME {get; set; }' properties
  (you can't use fields). You may add new properties as needed in the future.

* CONSIDER in high volume cases (e.g. > 1K/sec) consider reusing the payload object instead of
  creating a new one each time the event is fired.   This only works well if you already have locking
  or exclusive objects where you can remember the payload for the 'next' event to send easily and
  correctly (you are only saving an object allocation, which is not large).

* CONSIDER - if you have an event that is so frequent that the performance of the logging is
  an important consideration,  **and** you have only one data item **and** it is unlikely that
  you will ever have more data to pass to the event, **and** the data item is a normal class
  (not a value type) **then** you save some cost by simply by passing the data `object` directly
  without using an anonymous type wrapper.

* DO - use standard names for particular payload items. (TODO: Put the list here as we define standard payload names).

### Filtering

* DO - always enclose the `Write()` call in a call to `IsEnabled()` for the same event name. Otherwise
  a lot of setup logic will be called even if there is nothing listening for the event.

* CONSIDER - enclosing `IsEnabled(string, object, object)` calls with pure `IsEnabled(string)` calls to avoid
  the extra cost of creating a context in case a consumer is not interested in such events at all.

* CONSIDER - passing public named types instances to `IsEnabled()` overloads with `object` parameters
  to keep `IsEnabled()` as efficient as possible.

* DO - when subscribing to `DiagnosticSource` with an advanced filter for event name and extended context,
  make sure the filter returns `true` for `null` context properties if the consumer is interested
  in at least some events with context.

### Other Conventions

* DO NOT - make the `DiagnosticListener` public. There is no need to as subscribers will
  use the `AllListener` property to hook up.

 ----------------------------------------
## Consuming Data with DiagnosticListener

Up until now, this guide has focused on how to instrument code to generate logging
information. In this section we focus on subscribing and decoding of that information.

### Discovery of DiagnosticListeners

The first step in receiving events is to discover which `DiagnosticListeners` you are
interested in. While it is possible to discover `DiagnosticListeners`  at compile time
by referencing static variables (e.g. like the `httpLogger` in the previous example), this
is typically not flexible enough.

Instead `DiagnosticListener` supports a way of discovering `DiagnosticListeners` that are
active in the system at runtime. The API to accomplish this is the `AllListeners`
`IObservable<DiagnosticListener>`.

The `IObservable` interface is the 'callback' version of the `IEnumerable` interface. You can learn
more about it at the [Reactive Extensions](https://msdn.microsoft.com/en-us/data/gg577609.aspx) site.
In a nutshell, you have an object called an `IObserver` which has three callbacks, `OnNext`, `OnComplete`
and `OnError`, and an `IObservable` has single method called `Subscribe` which gets passed one of these
Observers. Once connected, the Observer gets callbacks (mostly `OnNext` callbacks) when things
happen. By including the `System.Reactive.Core` NuGet package, you can get a bunch of useful
extensions that make using `IObservable` nice.

A typical use of the `AllListeners` static property looks like this:

```C#
    // We are using AllListeners to turn an Action<DiagnosticListener> into an IObserver<DiagnosticListener>
    static IDisposable listenerSubscription = DiagnosticListener.AllListeners.Subscribe(delegate (DiagnosticListener listener)
    {
        // We get a callback of every Diagnostics Listener that is active in the system (past present or future)
        if (listener.Name == "System.Net.Http")
        {
            // Here is where we put code to subscribe to the Listener.
        }
    });

    // Typically you leave the listenerSubscription subscription active forever.
    // However when you no longer want your callback to be called, you can
    // call listenerSubscription.Dispose() to cancel your subscription to the IObservable.
```

This code basically creates a callback delegate and using the `AllListeners.Subscribe` method requests
that the delegate be called for every active `DiagnosticListener` in the system. Typically you inspect
the name of the listener and based on that, decide whether to subscribe to the listener or not. The
code above is looking for our 'System.Net.Http' listener that we created previously.

Like all calls to `Subscribe()`, this one returns an `IDisposable` that represents the subscription itself.
Callbacks will continue to happen as long as nothing calls `Dispose()` on this subscription object.
The above code never calls it, so it will receive callbacks forever.

It is important to note that when you subscribe to `AllListeners`, you get a callback for ALL ACTIVE `DiagnosticListeners`.
Thus, upon subscribing, you get a flurry of callbacks for all existing `DiagnosticListeners`, but as new ones
are created, you get a callback for those as well. Thus you get a complete list of everything it is possible
to subscribe to.

Finally, note that the code above is taking advantage of convenience functionality in the `System.Reactive.Core`
library. The `DiagnosticListener.AllListeners.Subscribe` method actually requires that it be passed
an `IObserver<DiagnosticListener>`, which is a class that has three callbacks (`OnNext`, `OnError`, `OnComplete`),
but we passed it an `Action<DiagnosticListener>`. The magic that makes this work is an extension method
in `System.Reactive.Core` that takes the `Action` and from it makes an `IObserver` (called `AnonymousObserver`)
which calls the `Action` in its `OnNext` callback. This glue is what makes the code concise.

#### Subscribing to DiagnosticListeners

A `DiagnosticListener` implements the `IObservable<KeyValuePair<string, object>>` interface, so you can
call `Subscribe()` on it as well. Thus we can fill out the previous example a bit:

```C#
    static IDisposable networkSubscription = null;
    static IDisposable listenerSubscription = DiagnosticListener.AllListeners.Subscribe(delegate (DiagnosticListener listener)
    {
        if (listener.Name == "System.Net.Http")
        {
            lock(allListeners)
            {
                if (networkSubscription != null)
                    networkSubscription.Dispose();

                networkSubscription = listener.Subscribe((KeyValuePair<string, object> evnt) =>
                    Console.WriteLine("From Listener {0} Received Event {1} with payload {2}",
                    networkListener.Name, evnt.Key, evnt.Value.ToString()));
            }
        }
    });

    // At some point you may wish to dispose the networkSubscription.
```

In this example, after finding the 'System.Net.Http' `DiagnosticListener`, we create an action that
prints out the name of the listener, event, and `payload.ToString()`. Notice a few things:

1. `DiagnosticListener` implements `IObservable<KeyValuePair<string, object>>`. This means
   on each callback we get a `KeyValuePair`. The key of this pair is the name of the event
   and the value is the payload `object`. In the code above we simply log this information
   to the console.

2. We keep track of our subscriptions to the `DiagnosticListener`. In this case we have
   a networkSubscription variable that remembers this, and if we get another 'creation' we
   unsubscribe the previous listener and subscribe to the new one.

3. We use locks. The `DiagnosticSource`/`DiagnosticListener` code is thread safe, but the
   callback code also needs to be thread safe. It is possible that two `DiagnosticListeners`
   with the same name are created at the same time (although that is a bit unexpected), so
   to avoid races we do updates of our shared variables under the protection of a lock.

Once the above code is run, the next time a `Write()` is done on 'System.Net.Http' `DiagnosticListener`
the information will be logged to the console.

It is also important to note that subscriptions are independent of one another. Thus other code
can do exactly the same thing as the code above, and thus generate two 'pipes' of the logging
information.

#### Decoding Payloads

The `KeyvaluePair` that is passed to the callback has the event name and payload, but the payload is typed simply as
an `object`. Odds are that you want to get at more specific data. There are two ways of doing this:

1. If the payload is a well known type (e.g. a `string`, or an `HttpMessageRequest`) then you can simply
   cast the `object` to the expected type (using the `as` operator so as not to cause an exception if
   you are wrong) and then access the fields. This is very efficient.

2. Use reflection API. For example, if we assume we have the method:

```C#
    /// Define a shortcut method that fetches a field of a particular name.
    static class PropertyExtensions
    {
        static object GetProperty(this object _this, string propertyName)
        {
            return _this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName)?.GetValue(_this);
        }
    }
```

Then we could replace the `listener.Subscribe()` call above with the following code, to decode the payload more fully:

```C#
    networkSubscription = listener.Subscribe(delegate(KeyValuePair<string, object> evnt) {
        var eventName = evnt.Key;
        var payload = evnt.Value;
        if (eventName == "RequestStart")
        {
            var url = payload.GetProperty("Url") as string;
            var request = payload.GetProperty("Request");
            Console.WriteLine("Got RequestStart with URL {0} and Request {1}", url, request);
        }
    });
```

Note that using reflection is relatively expensive. However, using reflection is your only
option if the payloads were generated using anonymous types. You can reduce this overhead by
making fast, specialized property fetchers either using `PropertyInfo.CreateDelegate` or
`ReflectEmit`, but that is beyond the scope of this document.
(See the [PropertySpec](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs#L784)
class used in the `DiagnosticSourceEventSource` for an example of a fast, delegate-based property fetcher.)

#### Filtering

In the example above the code uses the `IObservable.Subscribe()` method to hook up the callback, which
causes all events to be given to the callback. However `DiagnosticListener` has overloads of
`Subscribe()` that allow the controller to control which events get through.

Thus we could replace the `listener.Subscribe()` call in the previous example with the following code:

```C#
    // Create the callback delegate
    Action<KeyValuePair<string, object>> callback = (KeyValuePair<string, object> evnt) =>
        Console.WriteLine("From Listener {0} Received Event {1} with payload {2}", networkListener.Name, evnt.Key, evnt.Value.ToString());

    // Turn it into an observer (using System.Reactive.Core's AnonymousObserver)
    Observer<KeyValuePair<string, object>> observer = new AnonymousObserver<KeyValuePair<string, object>>(callback);

    // Create a predicate (asks only for one kind of event)
    Predicate<string> predicate = (string eventName) => eventName == "RequestStart";

    // Subscribe with a filter predicate
    IDisposable subscription = listener.Subscribe(observer, predicate);

    // subscription.Dispose() to stop the callbacks.
```

This very efficiently subscribes to only the 'RequestStart' events. All other events will cause the `DiagnosticSource.IsEnabled()`
method to return `false`, and thus be efficiently filtered out.

##### Context-based Filtering
Some scenarios require advanced filtering based on extended context.
Producers may call `DiagnosticSource.IsEnabled()` overloads and supply additional event properties:

```C#
    if (httpLogger.IsEnabled("RequestStart", aRequest, anActivity))
        httpLogger.Write("RequestStart", new { Url="http://clr", Request=aRequest });
```

And consumers may use such properties to filter events more precisely:

```C#
    // Create a predicate (asks only for Requests for certains URIs)
    Func<string, object, object, bool> predicate = (string eventName, object context, object activity) =>
    {
        if (eventName == "RequestStart")
        {
            HttpRequestMessage request = context as HttpRequestMessage;
            if (request != null)
            {
                return IsUriEnabled(request.RequestUri);
            }
        }
        return false;
    }

    // Subscribe with a filter predicate
    IDisposable subscription = listener.Subscribe(observer, predicate);
```

Note that producers are not aware of the filter a consumer has provided. `DiagnosticListener`
will invoke the provided filter, omitting additional arguments if necessary, thus the filter
should expect to receive a `null` context.
Producers should enclose `IsEnabled()` calls with event name and context with pure `IsEnabled()`
calls for event name, so consumers must ensure that their filter allows events without context
to pass through.

----------------------------------------
## Consuming DiagnosticSource Data with EventListeners and ETW

The `System.Diagnostic.DiagnosticSource` NuGet package comes with a built in `EventSource`
called Microsoft-Diagnostics-DiagnosticSource. This `EventSource` has the ability to
subscribe to any `DiagnosticListener` as well as pluck off particular data items from
`DiagnosticSource` payloads.

Thus code that is using `System.Diagnostics.Tracing.EventListener` or ETW can get at
any information logged with `DiagnosticSource`.

See [DiagnosticSourceEventSource](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs)
for more information on how to use it.
