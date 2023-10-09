## About

<!-- A description of the package and where one can find more documentation -->

This package provides types that allow applications to interact with the Windows performance counters.

Windows allows you to examine how programs you run affect your computer's performance, both in real time and by collecting log data for later analysis. You can do this via the Windows Performance Monitor tool, which uses performance counters, among other features.

Windows performance counters provide a high-level abstraction layer that provides a consistent interface for collecting various kinds of system data such as CPU, memory, and disk usage. They can be included in the operating system or can be part of individual applications. Windows Performance Monitor requests the current value of performance counters at specifiedtime intervals.

System administrators often use performance counters to monitor systems for performance or behavior problems. Software developers often use performance counters to examine the resource usage of their programs.

## Key Features

<!-- The key features of this package -->

* Can be used to read existing predefined or custom counters.
* Can be used for publishing (writing) data to custom counters.
* Can collect performance counters from the local machine or from a remote machine.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```cs
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class App
{
    public static void Main()
    {
        List<CounterSample> samples = [];

        // If the category does not exist, create the category and exit.
        // Performance counters should not be created and immediately used.
        // There is a latency time to enable the counters, they should be created
        // prior to executing the application that uses the counters.
        // Execute this sample a second time to use the category.
        if (SetupCategory())
        {
            return;
        }

        CollectSamples(samples);
        CalculateResults(samples);
    }

    private static bool SetupCategory()
    {
        if (PerformanceCounterCategory.Exists("AverageCounter64SampleCategory"))
        {
            Console.WriteLine("Category exists - AverageCounter64SampleCategory");
            return false;
        }

        CounterCreationDataCollection counterDataCollection = [];

        // Add the counter.
        CounterCreationData averageCount64 = new()
        {
            CounterType = PerformanceCounterType.AverageCount64,
            CounterName = "AverageCounter64Sample"
        };
        counterDataCollection.Add(averageCount64);

        // Add the base counter.
        CounterCreationData averageCount64Base = new()
        {
            CounterType = PerformanceCounterType.AverageBase,
            CounterName = "AverageCounter64SampleBase"
        };
        counterDataCollection.Add(averageCount64Base);

        // Create the category.
        PerformanceCounterCategory.Create("AverageCounter64SampleCategory",
            "Demonstrates usage of the AverageCounter64 performance counter type.",
            PerformanceCounterCategoryType.SingleInstance, counterDataCollection);

        return true;
    }

    private static void CollectSamples(List<CounterSample> samples)
    {
        // Create the counters

        PerformanceCounter avgCounter64Sample = new PerformanceCounter("AverageCounter64SampleCategory",
            "AverageCounter64Sample",
            false)
        {
            RawValue = 0
        };

        PerformanceCounter avgCounter64SampleBase = new PerformanceCounter("AverageCounter64SampleCategory",
            "AverageCounter64SampleBase",
            false)
        {
            RawValue = 0
        };

        Random r = new(DateTime.Now.Millisecond);

        for (int j = 0; j < 100; j++)
        {
            int value = r.Next(1, 10);
            Console.Write(j + " = " + value);

            avgCounter64Sample.IncrementBy(value);

            avgCounter64SampleBase.Increment();

            if ((j % 10) == 9)
            {
                OutputSample(avgCounter64Sample.NextSample());
                samples.Add(avgCounter64Sample.NextSample());
            }
            else
            {
                Console.WriteLine();
            }

            System.Threading.Thread.Sleep(50);
        }
    }

    private static void CalculateResults(List<CounterSample> samples)
    {
        for (int i = 0; i < (samples.Count - 1); i++)
        {
            // Output the sample.
            OutputSample(samples[i]);
            OutputSample(samples[i + 1]);

            // Use .NET to calculate the counter value.
            Console.WriteLine($".NET computed counter value = {CounterSampleCalculator.ComputeCounterValue(samples[i], samples[i + 1])}");

            // Calculate the counter value manually.
            Console.WriteLine($"My computed counter value = {MyComputeCounterValue(samples[i], samples[i + 1])}");
        }
    }

    //    Description - This counter type shows how many items are processed, on average,
    //        during an operation. Counters of this type display a ratio of the items
    //        processed (such as bytes sent) to the number of operations completed. The
    //        ratio is calculated by comparing the number of items processed during the
    //        last interval to the number of operations completed during the last interval.
    // Generic type - Average
    //      Formula - (N1 - N0) / (D1 - D0), where the numerator (N) represents the number
    //        of items processed during the last sample interval and the denominator (D)
    //        represents the number of operations completed during the last two sample
    //        intervals.
    //    Average (Nx - N0) / (Dx - D0)
    //    Example PhysicalDisk\ Avg. Disk Bytes/Transfer
    private static float MyComputeCounterValue(CounterSample s0, CounterSample s1)
    {
        float numerator = (float)s1.RawValue - s0.RawValue;
        float denomenator = (float)s1.BaseValue - s0.BaseValue;
        float counterValue = numerator / denomenator;
        return counterValue;
    }

    private static void OutputSample(CounterSample s)
    {
        Console.WriteLine("\r\n+++++++++++");
        Console.WriteLine("Sample values - \r\n");
        Console.WriteLine($"   BaseValue        = {s.BaseValue}");
        Console.WriteLine($"   CounterFrequency = {s.CounterFrequency}");
        Console.WriteLine($"   CounterTimeStamp = {s.CounterTimeStamp}");
        Console.WriteLine($"   CounterType      = {s.CounterType}");
        Console.WriteLine($"   RawValue         = {s.RawValue}");
        Console.WriteLine($"   SystemFrequency  = {s.SystemFrequency}");
        Console.WriteLine($"   TimeStamp        = {s.TimeStamp}");
        Console.WriteLine($"   TimeStamp100nSec = {s.TimeStamp100nSec}");
        Console.WriteLine("++++++++++++++++++++++");
    }
}
```

Notes:

* This assembly is only supported on Windows operating systems.
* Only the administrator of the computer or users in the Performance Logs User Group can log counter data. Users in the Administrator group can log counter data only if the tool they use to log counter data is started from a Command Prompt window that is opened with `Run as administrator...`. Any users in interactive logon sessions can view counter data. However, users in non-interactive logon sessions must be in the Performance Monitoring Users group to view counter data.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

Under the [`System.Diagnostics`](https://learn.microsoft.com/dotnet/api/System.Diagnostics) namespace, the main types are:

* [`System.Diagnostics.CounterCreationData`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.CounterCreationData)
* [`System.Diagnostics.CounterCreationDataCollection`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.CounterCreationDataCollection)
* [`System.Diagnostics.PerformanceCounter`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.PerformanceCounter)

Under the [`System.Diagnostics.PerformanceData`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.PerformanceData) namespace, the main types are:

* [`System.Diagnostics.PerformanceData.CounterData`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.PerformanceData.CounterData)
* [`System.Diagnostics.PerformanceData.CounterSet`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.PerformanceData.CounterSet)
* [`System.Diagnostics.PerformanceData.CounterType`](https://learn.microsoft.com/dotnet/api/System.Diagnostics.PerformanceData.CounterType)

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Microsoft Learn - System.Diagnostics.PerformanceCounter API reference](https://learn.microsoft.com/dotnet/api/system.diagnostics.performancecounter?view=dotnet-plat-ext-7.0)
* [Windows App Development - Performance Counters](https://learn.microsoft.com/windows/win32/perfctrs/performance-counters-portal)
* [Windows Performance and Reliability - Windows Performance Monitor](https://learn.microsoft.com/previous-versions/windows/it-pro/windows-server-2008-R2-and-2008/cc749249(v=ws.11))
* [Windows Server - perfmon](https://learn.microsoft.com/windows-server/administration/windows-commands/perfmon)
* [GitHub - Source code](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Diagnostics.PerformanceCounter)

## Related Packages

<!-- The related packages associated with this package -->

* [System.Diagnostics.EventLog](https://www.nuget.org/packages/System.Diagnostics.EventLog)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Diagnostics.PerformanceCounter is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
