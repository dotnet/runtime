# Evolving EventCounter

When EventCounter was first designed, it was tailored towards aggregating a set of events that can each be represented as a single number, and then summarizing that as a set of statistics made available to one client. It works well for that purpose, but now we need it to do more:

1. We'd like to use it from multiple clients. Right now when multiple clients try to use EventCounter the statistics get produced at whatever aggregation interval was specified by the most recent client to specify one. The ideal outcome is that each client is unaware of any other client. An acceptable outcome is that each client understands how to get the results it wants despite interference from other clients.
2. [Simple viewers] - These viewers only know how to display name-value pairs (textually or in a simple plot over time). Given a set of statistics per counter there must be a way to produce a single canonical value that gets displayed.
3. [Simple viewers] - It is useful to have both a simple name that is compact and has no spaces in it for manipulation on the command line as well as a more descriptive name that can be shown in the UI. Right now counters have only one name, and naming conventions for it aren't specified.
4. We want a rate counter, for example "Exceptions Thrown Per Second." The developer specifies the timescale but the counter-viewer specifies the aggregation interval so scaling needs to occur. For example the user could ask for hourly reports of the "Exceptions Thrown Per Second" counter and something needs to compute # of exceptions in that hour * 1/3600.
5. We want to render counters where there is no pre-existing control-flow that occurs at convenient discrete intervals. For example getting the % of CPU used is time-varying function, but there is no OnCpuUsageUpdated() API. A developer could always emulate one by polling a query function, but they wouldn't know what is an efficient rate to poll that balances counter accuracy vs. performance overhead.


## Design


### Multi-client support ###

**Emit data to all sessions at the rates requested by all clients** - This requires a little extra complexity in the runtime to maintain potentially multiple concurrent aggregations, and it is more verbose in the event stream if that is occurring. Clients need to filter out responses that don't match their requested rate, which is a little more complex than ideal, but still simpler than needing to synthesize statistics. In the case of multiple clients we can still encourage people to use a few canonical rates such as per-second, per-10 seconds, per-minute, per-hour which makes it likely that similar use cases will be able to share the exact same set of events. In the worst case that a few different aggregations are happening in parallel the overhead of our common counter aggregations shouldn't be that high, otherwise they weren't very suitable for lightweight monitoring in the first place. In terms of runtime code complexity I think the difference between supporting 1 aggregation and N aggregations is probably <50 lines per counter type and we only have a few counter types.

Doing the filtering requires that each client can identify which EventCounter data packets are the ones it asked for and which are unrelated. Using IntervalSec as I had originally intended does not work because IntervalSec contains the exact amount of time measured in each interval rather than the nominal interval the client requested. For example a client that asks for EventCounterIntervalSec=1 could see packets that have IntervalSec=1.002038, IntervalSec=0.997838, etc. To resolve this we will add another key/pair to the payload, Series="Interval=T", where T is the number of seconds that was passed to EventCounterIntervalSec. To ensure clients with basically the same needs don't arbitrarily create different series that are identical or near identical we enforce that IntervalSec is always a whole non-negative number of seconds. Any value that can't be parsed by uint.TryParse() will be interpreted the same as IntervalSec=0. Using leading zeros on the number, ie IntervalSec=0002 may or may not work so clients are discouraged from doing so (in practice, its whatever text uint.TryParse handles).

The changes to the parsing of EventCounterIntervalSec is technically a **breaking change**, but in practice I anticipate it will not be an issue.


### API design ###

There are bunch of things above that touch the API, so I am bundling them all here. A few requirements:
Goal 3 - to have multiple names - requires that we add an extra string somewhere
Goal 4 - to have rate counters - requires that the developer can specify which kind of counter it is
Goal 5 - we need a way for the counter infrastructure to poll at an appropriate rate


We believe adding some new top-level types will satisfy these requests:

    class EventCounter {
        EventCounter(string name, EventSource eventSource);
        string DisplayName;
        void WriteMetric(float metric);
        void AddMetaData(string key, string value);
    }

    class PollingCounter {
        PollingCounter(string name, EventSource eventSource Func<float> getMetricFunction);
        string DisplayName;
        void AddMetaData(string key, string value);
    }

    class IncrementingEventCounter {
        IncrementingEventCounter(string name, EventSource eventSource);
        string DisplayName;
        TimeSpan DisplayRateTimeScale;
        void Increment(float increment = 1);
        void AddMetaData(string key, string value);
    }

    class IncrementingPollingCounter {
        IncrementingPollingCounter(string name, EventSource eventSource, Func<float> getCountFunction);
        string DisplayName;
        TimeSpan DisplayRateTimeScale;
        void AddMetaData(string key, string value);
    }


EventCounter does what it has always done, producing a set of 5 stats (Min/Max/Mean/Count/StandardDeviation) and emitting a sequence of events that hold that data. PollingCounter is the same as EventCounter except instead of the caller invoking WriteMetric, the counter infrastructure invokes the callback function periodically to retrieve the data. The counter infrastructure will invoke the callback at least as often as necessary to have 1 sample of data in each aggregated sampling interval. In the current implementation it will occur exactly once at the transition point between adjacent sampling intervals. If multiple time series have an interval boundary at the same moment in time the callback is allowed to be shared for each of them but it is not required to be.

On the wire EventCounter and PollingCounter both produce an event with name "EventCounters" and example body:

    Payload = {
        DisplayName: "Request Bytes",
        DisplayRateTimeScale: "1",
        Name: "request-bytes",
        Mean: 12.32,
        StandardDeviation: 2.45,
        Count: 7,
        Min: -3.4,
        Max: 22.98,
        IntervalSec: 1.00324,
        Series: "Interval=1",
        CounterType: "Mean",
        MetaData: "key1=value1,key2=value2,key3=value3"
    }



IncrementingEventCounter and IncrementingPollingCounter, unlike the previous two, generate only a single sum value as their output statistic. IncrementingEventCounter adds together all arguments passed to its Increment() function during the time interval. IncrementingPollingCounter uses the callback to sample the count at the beginning of the interval and again at the end, using the difference between the two as the result (the increment during that interval).

On the wire IncrementingEventCounter and IncrementingPollingCounter both produce an event with the name "EventCounters" and example body:

    Payload = {
        DisplayName: "Exceptions Thrown",
        DisplayRateTimeScale: "1",
        Name: "exceptions-thrown",
        Increment: 246,
        IntervalSec: 1.0043,
        Series: "Interval=1",
        CounterType: "Sum",
        MetaData: "key1=value1,key2=value2,key3=value3"
    }


**Note: Why not match the five-tuple used by EventCounter?** Originally the plan was that counter viewers could treat all the counter types the same when it rendered them. Serializing one number as five numbers, needing to explain that the field which says 'Mean' really means 'Sum', or that WriteMetric distinguished individual events whereas Increment() pre-merges them all seemed a bit awkward, but it was a price to pay to get standardization for viewers. However late in our last discussion Vance said he wanted the wire format to be the sum, but the display format should still be a rate. This means we've lost the benefits of standardization because now viewers have to handle the incrementing counters differently than the averaging counters. It also means that any old viewer, such as PerfView's graph UI, would incorrectly apply the averaging conventions instead of the sum/rate convention because it wasn't designed to distinguish. So I am proposing that if we aren't going to get the benefits of a standardized output convention, we should at least avoid letting it muddy the waters.

I think there is still an argument to be made that standardization could benefit libraries that are merely exporting the data for storage in a time series database because rendering isn't their concern. However these tools are going to care about data storage size. If a few lines of code to discriminate two cases is going to distinguish counters that produce 5 numbers and counters that produce 1, I think any good export tool is going to want that savings.

### Canonicalizing a single value output per counter ###

For EventCounter and PollingCounter we expect simple viewers to use the display name as-is and use the value for 'Mean'. For IncrementingEventCounter and IncrementingPollingCounter, we expect simple viewers to display the display name with " / Min" appended after it. The display value should be computed by reading the 'Value' statistic and multiplying it by the number of measurement intervals per minute. For example if the counter had display name "Exceptions Thrown", value 2, and IntervalSec=1.01 the viewer should display "Exceptions Thrown / Min" with value 118.8.


### Metadata

To add any optional metadata about the counters that we do not already provide a way of encoding, users can call the `AddMetaData(string key, string value)` API. This API exists on all variants of the Counter APIs, and allows users to add one or many key-value pairs of metadata, which is dumped to the Payload as a comma-separated string value. This API exists so that users can add any metadata about their Counter that is not known to us and is different from the ones we provide by default (i.e. `DisplayName`, `CounterType`, `DisplayRateTimeScale`).
