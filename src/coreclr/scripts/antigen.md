# Documentation of Exploratory tool Antigen

Antigen is a an exploratory fuzzing tool to test JIT compiler. Currently the source code is present at https://github.com/kunalspathak/antigen and will eventually become a separate repository under "dotnet" organization or part of [dotnet/jitutils](https://github.com/dotnet/jitutils) repository.

## Overview

Antigen generates random test cases, compile them using Roslyn APIs and then execute it against `CoreRun.exe` in baseline and test mode. Baseline mode is without tiered JIT with minimum optimizations enabled while test mode is run by setting various combination of `COMPlus_*` environment variables. The output of both modes are compared to ensure they are same. If the output is different or an assert is hit in test mode, the tool saves the produced C# file in a `UniqueIssue` folder. Similar issues are placed in `UniqueIssueN` folder.

Antigen also comes with `Trimmer` that can be used to reduce the generated file and still reproduce the issue. Some more work needs to be done in this tool to make it efficient.

## Pipeline

Antigen tool is ran every night using CI pipeline. It can also be triggered on PRs. The pipeline would run Antigen tool for 3 hours on x86/x64 platforms and 2 hours for arm/arm64 platforms to generate test cases and verify if it contains issues. Once the run duration is complete, for each OS/arch, the pipeline will upload the issues that Antigen has found has an artifact that can be downloaded. The issues will be `.cs` files that will contain the program's output, environment variables that are needed to reproduce the issue. Since there can be several issues, the pipeline will just upload at most 5 issues from each `UniqueIssueN` folder.

### Pipeline details

1. `eng/pipeline/coreclr/jit-exploratory.yml` : This is the main pipeline which will perform Coreclr/libraries build and then further trigger `jit-exploratory-job.yml` pipeline.
1. `eng/pipeline/coreclr/templates/jit-exploratory-job.yml` : This pipeline will download all the Coreclr/libraries artifacts and create `CORE_ROOT` folder. It further triggers `jit-run-exploratory-job.yml`  pipeline.
1. `eng/pipeline/coreclr/templates/jit-run-exploratory-job.yml` : This pipeline will perform the actual run in 3 steps:
   * `src/coreclr/scripts/antigen_setup.py`: This script will clone the Antigen repo, build and prepare the payloads to be sent for testing.
   * `src/coreclr/scripts/antigen_run.py`: This script will execute Antigen tool and upload back the issues.
   * `src/coreclr/scripts/antigen_unique_issues.py`: In addition to uploading the issues, this script will also print the output of unique issues it has found so the developer can quickly take a look at them and decide which platform's artifacts to download. If there is any issue found by Antigen, this script will make the pipeline as "FAIL".
1. `src/coreclr/scripts/exploratory.proj`: This proj file is the one that creates the helix jobs. Currently, this file configures to run Antigen on 4 partitions. Thus, if Antigen can generate and test 1000 test cases on 1 machine, with current setup, the pipeline will be able to test 4000 test cases.