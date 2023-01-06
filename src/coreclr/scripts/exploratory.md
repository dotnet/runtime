# Documentation of Exploratory tools Antigen and Fuzzlyn

[Antigen](https://github.com/kunalspathak/antigen) and [Fuzzlyn](https://github.com/jakobbotsch/Fuzzlyn) are exploratory fuzzing tools used to test the JIT compiler.

## Overview

The basics of both tools are the same: they generate random programs using Roslyn and execute them with `corerun.exe` in a baseline and a test mode.
Typically, baseline uses the JIT with minimum optimizations enabled while the test mode has optimizations enabled.
Antigen also sets various `DOTNET_*` variables in its test mode to turn on different stress modes or turn on/off different optimizations.

The fuzzers detect issues by checking for assertion failures and by comparing results between the baseline and test modes.
For more information, see the respectives repos.

## Pipeline

Both Antigen and Fuzzlyn run on a schedule using the CI pipeline. They can also be triggered on PRs with `/azp run Antigen` and `/azp run Fuzzlyn` respectively.
The pipeline produces a summary of issues found under the "Extensions" tab when looking at the pipeline results.

## Getting test examples from Antigen runs

For Antigen runs, the summary will show the assertion errors that were hit.
Individual test examples are available as artifacts that can be downloaded for each OS/arch.
The issues will be `.cs` files that will contain the program's output, environment variables that are needed to reproduce the issue.
Since there can be several issues, the pipeline will just upload at most 5 issues.

## Getting test examples from Fuzzlyn runs

The Fuzzlyn pipeline will automatically reduce silent bad codegen examples that are found and include them as source code in the summary that can be viewed under "Extensions".
The pipeline does not currently reduce assertion error examples automatically and instead only displays the seeds for the programs that failed.
To reduce these examples manually, clone Fuzzlyn and run it as follows (adapting for Linux platforms as necessary):
```powershell
Fuzzlyn.exe --host <path to corerun.exe under test (typically a checked build)> --reduce --seed <seed from the summary>
```
This will take a long time if the example being reduced is one that brings down the host process on every run (e.g. for assertion failures).
When completed, the reduced example will be output on stdout.

### Pipeline details

1. `eng/pipeline/coreclr/exploratory.yml` : This is the main pipeline which will perform Coreclr/libraries build and then further trigger `jit-exploratory-job.yml` pipeline.
    It uses the name of the pipeline being run to determine whether Antigen or Fuzzlyn is being used.
1. `eng/pipeline/coreclr/templates/jit-exploratory-job.yml` : This pipeline will download all the Coreclr/libraries artifacts and create `CORE_ROOT` folder. It further triggers `jit-run-exploratory-job.yml`  pipeline.
1. `eng/pipeline/coreclr/templates/jit-run-exploratory-job.yml` : This pipeline will perform the actual run in 3 steps:
   * `src/coreclr/scripts/fuzzer_setup.py`: This script will clone the Antigen/Fuzzlyn repo, build and prepare the payloads to be sent for testing.
   * `src/coreclr/scripts/<antigen or fuzzlyn>_run.py`: This script will execute the tool and upload back the issues.
   * `src/coreclr/scripts/<antigen or fuzzlyn>_summarize.py`: In addition to uploading the issues, this script will also summarize the issues that were found so the developer can quickly take a look at them and decide how to proceed.
   This script is responsible for printing the markdown summary that uses Azure devops features to show up under the "Extensions" tab.
   Furthermore, it returns an error code if any issues were found.
1. `src/coreclr/scripts/exploratory.proj`: This proj file is the one that creates the helix jobs. Currently, this file configures to run Antigen/Fuzzlyn on 4 partitions.
    Thus, if Antigen can generate and test 1000 test cases on 1 machine, with current setup, the pipeline will be able to test 4000 test cases.