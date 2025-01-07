## Simple wasm benchmark sample app

Runs various performance measurements. It is intended as a quick tool to measure mono/wasm performance
in dotnet/runtime repo, using in-tree runtime. Can be used to check performance impact of runtime changes
and the summary is provided in [.md](https://guides.github.com/features/mastering-markdown/) markdown format,
suitable for commit messages and PR descriptions.

Browser and console versions are available.

### Running the benchmark

To run the benchmark on linux/mac:

    > make build
    > make run

can be used in the `browser-bench/` and also in the `browser-bench/Console/`.

To run the benchmark on windows:

    > dotnet build /t:RunSample

### Blazor startup measurements

To run the benchmark with blazor startup measurements, set `BlazorStartup` property to `true`, like:

    > dotnet build /t:RunSample /p:BlazorStartup=true

### Additional build arguments

The benchmark project is built in a separate process, so to pass additional msbuild arguments, use `BuildAdditionalArgs` property, like:

    > dotnet build /t:RunSample  /p:BuildAdditionalArgs="/p:WasmEnableSIMD=false"

Example console output:

    > make run
    console.debug: MONO_WASM: Initializing mono runtime
    console.debug: MONO_WASM: ICU data archive(s) loaded, disabling invariant mode
    console.info: Initializing.....
    Benchmark started
    Exceptions, NoExceptionHandling count: 8344090, per call: 6.807213249138013E-05ms, total: 0.568s
    Exceptions, NoExceptionHandling count: 8500347, per call: 6.940893118833855E-05ms, total: 0.59s
    ...
    .md
    | measurement | time |
    |-:|-:|
    |        Exceptions, NoExceptionHandling |     0.0680us |
    |                   Exceptions, TryCatch |     0.0723us |
    |              Exceptions, TryCatchThrow |     0.0035ms |
    |             Exceptions, TryCatchFilter |     0.0848us |
    |       Exceptions, TryCatchFilterInline |     0.0659us |
    |        Exceptions, TryCatchFilterThrow |     0.0046ms |
    | Exceptions, TryCatchFilterThrowApplies |     0.0036ms |
    |         Json, non-ASCII text serialize |    15.3855ms |
    |       Json, non-ASCII text deserialize |    24.7299ms |
    |                  Json, small serialize |     0.2980ms |
    |                Json, small deserialize |     0.5080ms |
    |                  Json, large serialize |    95.3333ms |
    |                Json, large deserialize |   141.4737ms |

The `.md` output will look like this:

| measurement | time |
|-:|-:|
|        Exceptions, NoExceptionHandling |     0.0680us |
|                   Exceptions, TryCatch |     0.0723us |
|              Exceptions, TryCatchThrow |     0.0035ms |
|             Exceptions, TryCatchFilter |     0.0848us |
|       Exceptions, TryCatchFilterInline |     0.0659us |
|        Exceptions, TryCatchFilterThrow |     0.0046ms |
| Exceptions, TryCatchFilterThrowApplies |     0.0036ms |
|         Json, non-ASCII text serialize |    15.3855ms |
|       Json, non-ASCII text deserialize |    24.7299ms |
|                  Json, small serialize |     0.2980ms |
|                Json, small deserialize |     0.5080ms |
|                  Json, large serialize |    95.3333ms |
|                Json, large deserialize |   141.4737ms |

Multiple results can be also easily combined together in text editor to have a table with measurements next to each other for comparison.

### Filter jobs/measurements

The `-t` option can be used to run subset of jobs/measurements like this, 1st line linux/mac, 2nd line windows:

    > make run ARGS="-t Json:non,Exceptions:Inline"
    > dotnet build /v:n /t:RunSample /p:Args="-t Json:non%2cExceptions:Inline"

Note the escaped `,` character (`%2c`) in the dotnet property above.

### Console options

    > make run ARGS=-h
    ...
    Simple mono wasm benchmark
    
    Copyright 2021 Microsoft Corporation
    
    Options:
      -h, --help, -?             Show this message and exit
      -t, --tasks=VALUE          Filter comma separated tasks and its measurements
                                   matching, TASK[:REGEX][,TASK[:REGEX],...].
                                   Example: -t Json:non,Exceptions:Inline
