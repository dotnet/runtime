# Evolving EventCounter

When EventCounter was first designed, it was tailored towards aggregating a set of events that can each be represented as a single number, and then summarizing that as a set of statistics made available to one client. It works well for that purpose, but now we need it to do more:

1. We'd like to use it from multiple clients. Right now when multiple clients try to use EventCounter the statistics get produced at whatever aggregation interval was specified by the most recent client to specify one. The ideal outcome is that each client is unaware of any other client. An acceptable outcome is that each client understands how to get the results it wants despite interference from other clients.
2. [Simple viewers] - These viewers only know how to display name-value pairs (textually or in a simple plot over time). Given a set of statistics per counter there must be a way to produce a single canonical value that gets displayed.
3. [Simple viewers] - It is useful to have both a simple name that is compact and has no spaces in it for manipulation on the command line as well as a more descriptive name that can be shown in the UI. Right now counters have only one name, and naming conventions for it aren't specified.
4. We want a rate counter, for example "Exceptions Thrown Per Second." The developer specifies the timescale but the counter-viewer specifies the aggregation interval so scaling needs to occur. For example the user could ask for hourly reports of the "Exceptions Thrown Per Second" counter and something needs to compute # of exceptions in that hour * 1/3600.
5. We want to render counters where there is no pre-existing control-flow that occurs at convenient discrete intervals. For example getting the % of CPU used is time-varying function, but there is no OnCpuUsageUpdated() API. A developer could always emulate one by polling a query function, but they wouldn't know what is an efficient rate to poll that balances counter accuracy vs. performance overhead.


## Design


### Multi-client support ###

**Emit data to all sessions at the rates requested by all clients** - This requires a little extra complexity in the runtime to maintain potentially multiple concurrent aggregations, and it is more verbose in the event stream if that is occuring. Clients need to filter out responses that don't match their requested rate, which is a little more complex than ideal, but still simpler than needing to synthesize statistics. In the case of multiple clients we can still encourage people to use a few canonical rates such as per-second, per-10 seconds, per-minute, per-hour which makes it likely that similar use cases will be able to share the exact same set of events. In the worst case that a few different aggregations are happening in parallel the overhead of our common counter aggregations shouldn't be that high, otherwise they weren't very suitable for lightweight monitoring in the first place. In terms of runtime code complexity I think the difference between supporting 1 aggregation and N aggregations is probably <50 lines per counter type and we only have a few counter types.


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
        
    }

    class PollingCounter {
        PollingCounter(string name, EventSource eventSource Func<float> getMetricFunction);
        string DisplayName;
    }

    class AggregatingEventCounter {
        AggregatingEventCounter(string name, EventSource eventSource);
        string DisplayName;
        Increment(long increment = 1);
    }

    class AggregatingPollingCounter {
        AggregatingPollingCounter(string name, EventSource eventSource, Func<long> getCountFunction);
        string DisplayName;
    }
    

EventCounter does what it has always done. PollingCounter calls getMetricFunction once per aggregation interval. All the other counters produce 1 value, call it X, per interval and publish it using the same statistics payload that EventCounter uses. It will have values Min=X, Max=X, Mean=X, Count=1, StdDev=0. How each counter produces X:
1. PollingCounter - X is the return value of the call to getMetricFunction()
2. AggregatingEventCounter - X is the sum of all the values passed to Increment()
3. AggregatingPollingCounter - X is the most recent result from getCountFunction() - the previous result. (It is the amount the counter increased during the time interval)

### Canonicalizing a single value output per counter ###

For EventCounter and PollingCounter we expect simple viewers to use the display name as-is and use the value for 'Mean'. For AggregatingEventCounter and AggregatingPollingCounter, we expect simple viewers to display the display name with " / Min" appended after it. The value should be computed by reading the Mean statistic and multiplying it by the number of measurement intervals per minute. For example if the counter had display name "Exceptions Thrown", value 2, and Interval=1sec the viewer should display "Exceptions Thrown / Min" with value 120.


### EventStream format

We should add "DisplayName" and "Metadata" to the payload fields. We still need to define exactly what the encoding of Metadata is, but at minimum assume that it contains the counter type as a well-known constant.
