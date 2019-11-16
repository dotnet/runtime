# JitBench #

JitBench is a collection of scenario benchmarks that were originally designed to do performance testing of the tiered jitting feature. They can be easily run for ad-hoc investigation or as part of automated performance testing


## Running the test (ad-hoc) ##

Execute 'dotnet run' in this directory. The test should eventually produce output like this:

 
 
      === CONFIGURATION ===

    DotnetFrameworkVersion: 2.1.0-preview2-26131-06
    DotnetSdkVersion:       2.2.0-preview1-007558
    PrivateCoreCLRBinDir:
    Architecture:           X64
    OutputDir:              C:\Users\noahfalk\AppData\Local\Temp\JitBench_2018_02_12_05_16_34_0611
    Iterations:             3
    UseExistingSetup:       True
    Configurations:         Default


    Benchmark run in progress...
    Verbose log: C:\Users\noahfalk\AppData\Local\Temp\JitBench_2018_02_12_05_16_34_0611\JitBench_log.txt


      === RESULTS ===

           Benchmark                Metric           Default
    -----------------------  --------------------  -----------
    Dotnet_Build_HelloWorld         Duration (ms)  1322.5+-2.9
            Csc_Hello_World         Duration (ms)     782+-105
          Csc_Roslyn_Source         Duration (ms)     2858+-16
                 MusicStore          Startup (ms)   703.5+-6.9
                 MusicStore    First Request (ms)      636+-11
                 MusicStore  Median Response (ms)        89+-0


By default the test downloads versions of dotnet framework and SDK from Azure, downloads various workloads that will run in the benchmarks, and then executes each benchmark multiple times in the default configuration. The results are tabulated and displayed in the output.


Examples of more customized ways the benchmark can be run (see the command line for other options not show here):

**Run with a private CoreCLR build instead of a downloaded one**

    dotnet.exe run --  --coreclr-bin-dir F:\github\coreclr\artifacts\bin\coreclr\Windows_NT.x64.Release

**Run multiple configurations for comparison**

    dotnet.exe run -- --configs Default,Tiering,Minopts

    ...
      === RESULTS ===

           Benchmark                Metric          Default     Tiering    Minopts
    -----------------------  --------------------  ---------  -----------  --------
    Dotnet_Build_HelloWorld         Duration (ms)   1368+-66      1227+-2  1188+-37
            Csc_Hello_World         Duration (ms)    648+-41     542+-9.8  518+-7.8
          Csc_Roslyn_Source         Duration (ms)  2806+-185     3130+-50  2842+-68
                 MusicStore          Startup (ms)    716+-15   633.5+-4.9   628+-21
                 MusicStore    First Request (ms)    626+-53  482.5+-0.98   456+-17
                 MusicStore  Median Response (ms)      89+-0        89+-0     89+-0

**Run only a specific benchmark**

    dotnet.exe run -- --benchmark Dotnet_Build_HelloWorld

    ...
      === RESULTS ===

           Benchmark            Metric      Default
    -----------------------  -------------  --------
    Dotnet_Build_HelloWorld  Duration (ms)  1391+-25

**Run with ETW collection enabled**

    dotnet.exe run -- --perf:collect BranchMispredictions+CacheMisses+InstructionRetired

ETL traces will show up in the output directory here: <run\_id\>-JitBench-<benchmark\_name\>-<config\_name\>-traces\\<run\_id\>-JitBench-<benchmark\_name\>-<config\_name\>(#).etl

**Run without repeating all the setup steps (for a faster inner dev loop)**

    dotnet.exe run -- --use-existing-setup

**Run with fewer iterations (faster inner dev loop but error bounds increase)**

    dotnet.exe run -- --iterations 3

**Run with a specific output directory**

    dotnet.exe run -- --perf:outputdir C:\temp\JitBench\_results

## Adding a new Benchmark ##

In the Benchmarks folder create a new .cs file that implements a class deriving from Benchmark. Provide a name for the benchmark in the constructor and implement the abstract Setup() method. In Setup do whatever you need to do to acquire files specific to your benchmark and then set the properties

- ExePath
- WorkingDirPath
- EnvironmentVariables (optional) 

to determine what process will be invoked later when the benchmark runs. BuildHelloWorldBenchmark.cs is a simple example if you need a template to copy. MusicStore is a bit more sophisticated and shows gathering custom metrics + customizing the Benchview output.

## Automation
This how we currently setup to run the test in CI and then retrieve its results. 

**Setup:**

1. Create a directory with all the runtime and framework binaries in it (currently called the sandbox directory)
2. Build the JitBench executable with msbuild (this occurs as part of test build)
3. Set any COMPLUS variables that will modify the run
4. Invoke the test with commandline _sandbox\_dir_\\corerun.exe --perf:outputdir _output\_dir_ --perf:runid _run\_id_ --target-architecture x64 --perf:collect _metrics_


**Results:**

For each benchmark in the benchmark suite the test will write out a set of result files in the _output\_dir_:

- <run\_id\>-JitBench-<benchmark\_name\>-<config\_name\>.csv
- <run\_id\>-JitBench-<benchmark\_name\>-<config\_name\>.md
- <run\_id\>-JitBench-<benchmark\_name\>-<config\_name\>.xml

If ETW was enabled there will also be a set of ETW traces for each process execution in the test:

- <run\_id\>-JitBench-<benchmark\_name\>-<config\_name\>-traces\\<run\_id\>-JitBench-<benchmark\_name\>-<config\_name\>(#).etl

For example:

    02/15/2018  09:07 PM    <DIR>          Perf-On-JitBench-Csc_Hello_World-Default-traces
    02/15/2018  09:07 PM             2,766 Perf-On-JitBench-Csc_Hello_World-Default.csv
    02/15/2018  09:07 PM             3,801 Perf-On-JitBench-Csc_Hello_World-Default.md
    02/15/2018  09:07 PM            11,610 Perf-On-JitBench-Csc_Hello_World-Default.xml
    02/15/2018  09:08 PM    <DIR>          Perf-On-JitBench-Csc_Roslyn_Source-Default-traces
    02/15/2018  09:08 PM             2,856 Perf-On-JitBench-Csc_Roslyn_Source-Default.csv
    02/15/2018  09:08 PM             3,851 Perf-On-JitBench-Csc_Roslyn_Source-Default.md
    02/15/2018  09:08 PM            11,716 Perf-On-JitBench-Csc_Roslyn_Source-Default.xml
    02/15/2018  08:48 PM    <DIR>          Perf-On-JitBench-Dotnet_Build_HelloWorld_Default-traces
    02/15/2018  08:48 PM             2,901 Perf-On-JitBench-Dotnet_Build_HelloWorld_Default.csv
    02/15/2018  08:48 PM             4,001 Perf-On-JitBench-Dotnet_Build_HelloWorld_Default.md
    02/15/2018  08:48 PM            11,777 Perf-On-JitBench-Dotnet_Build_HelloWorld_Default.xml
    02/15/2018  09:08 PM    <DIR>          Perf-On-JitBench-MusicStore-Default-traces
    02/15/2018  09:09 PM             3,511 Perf-On-JitBench-MusicStore-Default.csv
    02/15/2018  09:09 PM             5,543 Perf-On-JitBench-MusicStore-Default.md
    02/15/2018  09:09 PM            15,965 Perf-On-JitBench-MusicStore-Default.xml

The result files use standard XUnitPerformanceHarness formatting. Typical metrics content from the csv when ETW is enabled looks like this:

    JitBench															Metric					Unit	Iterations	Average		STDEV.S		Min			Max
    MusicStore															Duration				ms		2			2146		4.242640687	2143		2149
    MusicStore/dotnet.exe												Duration				ms		2			2136.8458	3.163030054	2134.6092	2139.0824
    MusicStore/dotnet.exe												BranchMispredictions	count	2			57272320	147711.7782	57167872	57376768
    MusicStore/dotnet.exe												CacheMisses				count	2			47482880	78200.35314	47427584	47538176
    MusicStore/dotnet.exe												InstructionRetired		count	2			9266000000	32526911.93	9243000000	9289000000
    MusicStore/dotnet.exe!Anonymously Hosted DynamicMethods Assembly	BranchMispredictions	count	2			0			0			0			0
    MusicStore/dotnet.exe!Anonymously Hosted DynamicMethods Assembly	CacheMisses				count	2			0			0			0			0
    MusicStore/dotnet.exe!Anonymously Hosted DynamicMethods Assembly	InstructionRetired		count	2			0			0			0			0
    MusicStore/dotnet.exe!clrjit.dll									BranchMispredictions	count	2			23300096	147711.7782	23195648	23404544
    MusicStore/dotnet.exe!clrjit.dll									CacheMisses				count	2			9240576		330179.2688	9007104		9474048
    MusicStore/dotnet.exe!clrjit.dll									InstructionRetired		count	2			1562500000	48790367.9	1528000000	1597000000
    MusicStore/dotnet.exe!coreclr.dll									BranchMispredictions	count	2			15316992	298319.8657	15106048	15527936
    MusicStore/dotnet.exe!coreclr.dll									CacheMisses				count	2			16463872	49237.25939	16429056	16498688
    MusicStore/dotnet.exe!coreclr.dll									InstructionRetired		count	2			2432500000	58689862.84	2391000000	2474000000
    MusicStore/dotnet.exe!dotnet.exe									BranchMispredictions	count	2			0			0			0			0
    MusicStore/dotnet.exe!dotnet.exe									CacheMisses				count	2			0			0			0			0
    MusicStore/dotnet.exe!dotnet.exe									InstructionRetired		count	2			0			0			0			0
    MusicStore/dotnet.exe!MusicStore.dll								BranchMispredictions	count	2			0			0			0			0
    MusicStore/dotnet.exe!MusicStore.dll								CacheMisses				count	2			0			0			0			0
    MusicStore/dotnet.exe!MusicStore.dll								InstructionRetired		count	2			0			0			0			0
    MusicStore/dotnet.exe!ntoskrnl.exe									BranchMispredictions	count	2			12146688	112956.0657	12066816	12226560
    MusicStore/dotnet.exe!ntoskrnl.exe									CacheMisses				count	2			12468224	57926.18751	12427264	12509184
    MusicStore/dotnet.exe!ntoskrnl.exe									InstructionRetired		count	2			4192000000	15556349.19	4181000000	4203000000
    MusicStore/dotnet.exe!System.Private.CoreLib.dll					BranchMispredictions	count	2			489472		31859.40313	466944		512000
    MusicStore/dotnet.exe!System.Private.CoreLib.dll					CacheMisses				count	2			1196032		92681.90002	1130496		1261568
    MusicStore/dotnet.exe!System.Private.CoreLib.dll					InstructionRetired		count	2			85000000	7071067.812	80000000	90000000
    MusicStore/dotnet.exe!Unknown										BranchMispredictions	count	2			1380352		28963.09376	1359872		1400832
    MusicStore/dotnet.exe!Unknown										CacheMisses				count	2			2377728		228808.4407	2215936		2539520
    MusicStore/dotnet.exe!Unknown										InstructionRetired		count	2			129500000	707106.7812	129000000	130000000
    MusicStore/First Request											Duration				ms		2			872			24.04163056	855			889
    MusicStore/Median Response											Duration				ms		2			88.8		0			88.8		88.8
    MusicStore/Startup													Duration				ms		2			887			35.35533906	862			912

## Why is this project in a folder marked 'unofficial'?

CoreCLR CI machines don't currently support building netcoreapp2.0 projects authored with the new msbuild SDK authoring style so the repo build uses the JitBench.csproj one directory higher, not the one in this directory. If you try to build the project in this directory in CI you get this error:

    C:\Program Files\dotnet\sdk\1.1.0\Sdks\Microsoft.NET.Sdk\build\Microsoft.NET.TargetFrameworkInference.targets(112,5): error : The current .NET SDK does not support targeting .NET Core 2.0.  Either target .NET Core 1.1 or lower, or use a version of the .NET SDK that supports .NET Core 2.0. [D:\j\workspace\x64_checked_w---eac6a79c\tests\src\performance\Scenario\JitBench\JitBench.csproj]

I assume the CI machines have fairly old SDK tools installed but I didn't have enough time to keep investigating these build issues. From I can tell if you have .NET Core 2.0+ SDK installed on your machine this build works fine from the command line and from VS.
