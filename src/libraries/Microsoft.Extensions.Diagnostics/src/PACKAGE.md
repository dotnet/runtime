## About

<!-- A description of the package and where one can find more documentation -->

`Microsoft.Extensions.Diagnostics` provides the default implementation of `IMeterFactory` and the extension methods that register it with [dependency injection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/). It also lets you choose which metrics are collected, and by which listeners, from code or from configuration. Starting with version 11.0, it provides the same support for tracing, with a default implementation of `ActivitySourceFactory`.

Applications created with `Host.CreateApplicationBuilder` or `Host.CreateDefaultBuilder` get the metrics services automatically, with rules read from the `Metrics` section of the app configuration. Tracing is not registered by default; call `AddTracing` to use it.

## Key Features

<!-- The key features of this package -->

* Register a default `IMeterFactory` with `AddMetrics`, so that libraries and app code can create `Meter` instances through dependency injection.
* Reuse meters: the factory returns the same `Meter` for the same name, version and tags, and disposes the meters it created when the service provider is disposed.
* Enable or disable metrics per meter, instrument and listener, in code or through `IConfiguration`.
* Write measurements to the console during development with the built-in debug console listener.
* Starting with version 11.0, register a default `ActivitySourceFactory` with `AddTracing`, add activity listeners, and enable or disable tracing per activity source, operation and listener, in code or through `IConfiguration`.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

### Collecting metrics

The following example creates a counter through `IMeterFactory` and writes its measurements to the console with the debug console listener:

```csharp
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Metrics.AddDebugConsole();
builder.Metrics.EnableMetrics("MyCompany.Orders", listenerName: ConsoleMetrics.DebugListenerName);
builder.Services.AddSingleton<OrderMetrics>();

using IHost host = builder.Build();
await host.StartAsync();

host.Services.GetRequiredService<OrderMetrics>().OrderProcessed();

await host.StopAsync();

public sealed class OrderMetrics
{
    private readonly Counter<long> _ordersProcessed;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create("MyCompany.Orders");
        _ordersProcessed = meter.CreateCounter<long>("orders.processed", unit: "{order}", description: "Number of processed orders");
    }

    public void OrderProcessed() => _ordersProcessed.Add(1);
}
```

Listeners are subscribed when the host starts, so measurements recorded before `StartAsync` are not collected.

### Configuring metrics

Instead of calling `EnableMetrics`, the same rule can be set in `appsettings.json`. Rules under a listener's name, such as `DebugConsole`, apply only to that listener. Rules directly under an `EnabledMetrics` section at the top level apply to all listeners. The following configuration enables the meter for the debug console listener only:

```json
{
  "Metrics": {
    "DebugConsole": {
      "EnabledMetrics": {
        "MyCompany.Orders": true
      }
    }
  }
}
```

### Tracing

Starting with version 11.0, tracing works in a similar way. An activity is created only when a listener that is enabled for its source samples it, so the listener in this example sets `Sample`:

```csharp
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Tracing;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTracing(tracing =>
{
    tracing.AddListener("Console", listener =>
    {
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded;
        listener.ActivityStopped = activity =>
            Console.WriteLine($"{activity.OperationName} took {activity.Duration.TotalMilliseconds} ms");
    });
    tracing.EnableTracing("MyCompany.Orders", listenerName: "Console");
});

using IHost host = builder.Build();
await host.StartAsync();

ActivitySourceFactory activitySourceFactory = host.Services.GetRequiredService<ActivitySourceFactory>();
ActivitySource activitySource = activitySourceFactory.Create("MyCompany.Orders");

using (Activity? activity = activitySource.StartActivity("ProcessOrder"))
{
    activity?.SetTag("order.id", 42);
}

await host.StopAsync();
```

To read tracing rules from configuration, call `tracing.AddConfiguration(builder.Configuration.GetSection("Tracing"))`. The section uses the same layout as `Metrics`, with `EnabledTracing` in place of `EnabledMetrics`.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.DependencyInjection.MetricsServiceExtensions`
* `Microsoft.Extensions.Diagnostics.Metrics.MetricsBuilderConfigurationExtensions`
* `Microsoft.Extensions.Diagnostics.Metrics.MetricsBuilderConsoleExtensions`
* `Microsoft.Extensions.DependencyInjection.TracingServiceExtensions`
* `Microsoft.Extensions.Diagnostics.Tracing.TracingBuilderConfigurationExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation: Creating metrics](https://learn.microsoft.com/dotnet/core/diagnostics/metrics-instrumentation)
* [Conceptual documentation: Distributed tracing](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.diagnostics.metrics)

## Related Packages

<!-- The related packages associated with this package -->

* [Microsoft.Extensions.Diagnostics.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.Abstractions)
* [System.Diagnostics.DiagnosticSource](https://www.nuget.org/packages/System.Diagnostics.DiagnosticSource)
* [Microsoft.Extensions.Hosting](https://www.nuget.org/packages/Microsoft.Extensions.Hosting)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Diagnostics is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
