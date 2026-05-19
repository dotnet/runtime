---
name: performance-benchmark-crank
description: Generate and run performance benchmarks in client-server scenarios to validate code changes. Use this when asked to benchmark, profile, or validate the performance impact of a code change in dotnet/runtime in the networking areas such as HTTP(s) or TLS.
---

# Performance Benchmarking with crank

[crank](https://github.com/dotnet/crank) is a performance benchmarking tool that can be used to run client-server benchmarks, such as HTTP or TLS scenarios.

## Step 1: Select a Benchmark Scenario from existing scenarios

The [aspnet/Benchmarks](https://github.com/aspnet/Benchmarks/tree/main/scenarios) repository contains a variety of client-server benchmark scenarios that can be used to validate the performance impact of code changes in dotnet/runtime. Select a scenario that closely matches the area of code you are changing. For example, if you are changing code in the HTTP stack, you might select the [httpclient.benchmarks.yml](https://github.com/aspnet/Benchmarks/blob/main/scenarios/httpclient.benchmarks.yml) scenario.

## Step 2: Prepare the benchmarked binaries

Benchmarking local changes is done by comparing the performance of a "baseline" binary (e.g. the latest official build) with a "test" binary (e.g. a locally built version with your changes).

For best results, it is recommended to minimize the difference between the baseline and test code by either:

- Temporarily adding an environment variable check in the code to be benchmarked that allows you to switch between the baseline and test code paths, and then running the benchmark with the appropriate environment variable set for each run.
- Building both the baseline and test binaries locally and deploying the right binary for each test run.

In all cases make sure to build the binaries in release mode.

## Step 3: Run the benchmark

To run the benchmark, you will need to have the crank tool installed. Follow the instructions in the [crank repository](https://github.com/dotnet/crank).

Once you have crank installed, you can run the benchmark using the following command:

```bash
crank --config <path-or-link-to-benchmark-scenario.yml> --scenario <scenario> --profile <profile> <additional-options>
```

### Profile selection and deploying test binaries

The profile selection influences which machines are used for the benchmark runs (including OS and CPU architecture), see https://github.com/aspnet/Benchmarks/blob/main/scenarios/README.md#profiles for the list of available profiles. For example, you might use `--profile aspnet-perf-lin` to run the benchmark on Linux machines.

Additional options can be used to overlay locally built files into the published output for a job via `--[JOB].options.outputFiles path[;destination]`. This is useful when you want a benchmark run to use modified binaries by copying them into the published app output, optionally with an explicit destination filename. For example, for the `httpclient.benchmarks.yml` scenario, you might copy locally built assemblies into the client output like this:

```bash
crank --config httpclient.benchmarks.yml --profile aspnet-perf-lin \
  --client.options.outputFiles path/to/modified/System.Net.Http.dll;System.Net.Http.dll \
  --client.options.outputFiles path/to/modified/System.Net.Security.dll;System.Net.Security.dll
```

See [crank command line reference](https://github.com/dotnet/crank/blob/main/src/Microsoft.Crank.Controller/README.md) for more details on available options.

### Scenario selection and configuration

Each benchmark yml definition can contain multiple scenarios. E.g. for HttpClient benchmarks there are scenarios for GET or POST requests, for SslStream benchmarks there are scenarios for throughput and TLS handshake performance.

Most benchmarks also accept configuration options via variables. E.g. for the mentioned `httpclient.benchmarks.yml` scenario, you can specify the use of HTTPS via the `useHttps` variable:

```bash
crank --config httpclient.benchmarks.yml --profile aspnet-perf-lin \
  ... \
  --variable useHttps=true
```

Consult the benchmark yml definition for the specific scenario you are using to see which variables are available.

Prefer collecting the results into a file using either the `--json <path>` or `--csv <path>` argument so that the results can be easily shared and analyzed (for example, `--json results.json` or `--csv results.csv`). The metrics exported may be different for each benchmark scenario, so you may need to run the benchmark once to see which metrics are available before deciding which ones to focus on for your analysis.
