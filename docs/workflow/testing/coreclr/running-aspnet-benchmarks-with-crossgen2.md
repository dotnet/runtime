# Working with Benchmarks Driver 2

This document describes how to run the ASP&#46;NET Benchmarks with _crossgen2_
using the latest driver and servers.

## Requirements

* A clone of the [ASP.NET Benchmarks repo](https://github.com/aspnet/benchmarks).
* A clone of the [runtime repo](https://github.com/dotnet/runtime).
* A code editor of your choice.

## Setup

Before using the remote servers for the benchmarks, you will need to follow the
steps described in the next sections.

### Build CoreCLR and generate the Core_Root

In the runtime repo, you will need the CoreCLR binaries and the Core_Root to do
the crossgen'ing of the ASP&#46;NET application. The simplest steps you can do for
this are below.

For Windows:

```powershell
.\build.cmd -subset clr+libs -c release
cd src\coreclr
.\build-test.cmd Release generatelayoutonly
```

For Linux:

```bash
./build.sh -subset clr+libs -c release
cd src/coreclr
./build-test.sh -release -generatelayoutonly
```

### Generate a Configuration File for ASP&#46;NET Benchmarking Runs

The ASP&#46;NET Benchmarks are configured by means of profiles, which are specified
in `yml` files. Here is a simple example of a configuration file, which we will
be using throughout this document.

```yml
imports:
  - https://raw.githubusercontent.com/aspnet/Benchmarks/master/src/WrkClient/wrk.yml

jobs:
  aspnetbenchmarks:
    source:
      repository: https://github.com/aspnet/benchmarks.git
      branchOrCommit: master
      project: src/Benchmarks/Benchmarks.csproj
    readyStateText: Application started.
    variables:
      protocol: http
      server: Kestrel
      transport: Sockets
      scenario: plaintext
    channel: edge
    framework: netcoreapp5.0
    arguments: "--nonInteractive true --scenarios {{scenario}} --server-urls {{protocol}}://[*]:{{serverPort}} --server {{server}} --kestrelTransport {{transport}} --protocol {{protocol}}"

scenarios:
  json:
    application:
      job: aspnetbenchmarks
      variables:
        scenario: json
    load:
      job: wrk
      variables:
        presetHeaders: json
        path: /json
        duration: 60
        warmup: 5
        serverPort: 5000

profiles:
  aspnet-physical-win:
    variables:
      serverUri: http://10.0.0.110
      cores: 12
    jobs:
      application:
        endpoints: 
          - http://asp-perf-win:5001
      load:
        endpoints: 
          - http://asp-perf-load:5001

  aspnet-physical-lin:
    variables:
      serverUri: http://10.0.0.102
      cores: 12
    jobs:
      application:
        endpoints: 
          - http://asp-perf-lin:5001
      load:
        endpoints: 
          - http://asp-perf-load:5001
```

Now, what does this configuration mean and how is it applied? Let's go over
the most important fields to understand its main functionality.

* **Imports**: These are external tools hosted in the Benchmarks repo. 
In this case, we only need `wrk`, which is a tool that loads and tests 
performance in Web applications.

* **Jobs**: Here go the job descriptions. A job in this context is the set of
server configuration, launch arguments, .NET version, etc.
    * _Source_: This shows the repo where the Benchmarking application is hosted.
    * _Variables_: These define how the communication with the server and the
    information exchange will take place.
    * _Channel_: Resolves the runtime versions. In this example, `edge` means it
    will use the latest nightly build.
    * _Framework_: Which .NET version will be used to build the application.
    * _Arguments_: Command-line arguments to call onto the server.

* **Scenarios**: The scenarios describe how each job will be run (from the ones
described in the previous section).
    * _Application_: Here we choose which job will be selected as the application
    to run the benchmarks on, as well as other variables.
    * _Load_: This is the tool that will generate and send the requests to
    benchmark the web application. In this example, we are using `wrk` with a
    warmup of 5 seconds and running the test for 60 seconds. In this example,
    we are using the `json` headers for the load generation. There are various
    headers that can be used, and these are defined in `wrk.yml`, which is
    referenced at the top of this configuration file.

* **Profiles**: The profiles describe the machines where the benchmarks will
be run. This information was provided by the ASP&#46;NET team, who is in charge
of these servers. In our example, there are two profiles, one for Windows,
and one for Linux.

## Run the Benchmarks

Once you have your configuration file and CoreCLR built, it's time to run the
initial benchmarks.

### Initial Application

From the `BenchmarksDriver2` folder, run the following command.

On Windows:

```powershell
dotnet run -- --config crossgen2-benchmarks.yml --scenario json --profile aspnet-physical-win
--application.options.fetch true
```

On Linux:

```bash
dotnet run -- --config crossgen2-benchmarks.yml --scenario json --profile aspnet-physical-lin
--application.options.fetch true
```

Splitting and analyzing the previous command:

* `--config crossgen2-benchmarks.yml`: This selects the configuration file to use.
* `--scenario json`: This runs the scenario labelled as _json_ in the configuration file.
* `--profile aspnet-physical-win`: This chooses the Windows profile from the configuration file.
* `--application.options.fetch true`: This downloads the built application used for
the benchmarks. We need these files to apply _crossgen2_ and then compare the benchmarks.
Note that `application` is just the label given in the configuration file.

At the end of the run, the tool will print a summary of statistics regarding
how the performance went.

### Crossgen2

Grab the downloaded zip file with the application and extract it somewhere else.
This is to avoid mixing up stuff or losing it when running `git clean` or the like.

In this example, we will be using a new folder called `results` outside of the
repos. Here, extract the zip into a folder we will refer to as `application`.
Next, create another folder within `results` called `composite`. This is where
the crossgen2'd assemblies will be stored.

Now, go to your _Core\_Root_ inside the _runtime_ repo. From there, apply _crossgen2_
using the following command.

On Windows:

```powershell
CoreRun.exe \runtime\artifacts\bin\coreclr\Windows_NT.x64.Release\crossgen2\crossgen2.dll
--Os --composite -o \path\to\results\composite\TotalComposite.dll \path\to\results\application\*.dll
```

On Linux:

```bash
./corerun /runtime/artifacts/bin/coreclr/Linux.x64.Release/crossgen2/crossgen2.dll
--Os --composite -o /path/to/results/composite/TotalComposite.dll /path/to/results/application/*.dll
```

This will generate new assemblies within the `composite` folder that you will
want to copy into the downloaded `application` one. Replace all those that
already exist there.

### Optimized Application

To run the optimized version of the application, go back to the `BenchmarksDriver2`
folder, and run the driver with this other command line.

On Windows:

```powershell
dotnet run -- --config crossgen2-benchmarks.yml --scenario json --profile aspnet-physical-win
--application.options.outputFile \path\to\results\application\*
```

On Linux:

```bash
dotnet run -- --config crossgen2-benchmarks.yml --scenario json --profile aspnet-physical-lin
--application.options.outputFile /path/to/results/application/*.dll
```

This is the same command as in the initial run, with one difference:

* `--application.options.outputFile`: This instructs the tool to upload your
crossgen2'd application and build and test with that one.

Same as before, once the test finishes running, it will display a summary of the
performance statistics, which you can compare to the original one and do some
analysis later.
