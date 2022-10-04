# CI Health and Investigation

`dotnet/runtime` runs testing across many different architectures and operating systems. The breadth of testing that happens lends itself to a complex system which is susceptible to different points of failure.

Note that this document focuses on coreclr testing in `dotnet/runtime`.

https://github.com/dotnet/runtime/issues/702 was opened as a way to simply view in one place all issues that are affecting `dotnet/runtime`'s CI.

## TOC

1. [Terminology](#Terminology)
2. [CI Overview](#CI-Overview)
3. [Analytics](#Analytics)
4. [Resources](#Resources)

#### Terminology

In order to follow some of the terminology used, there is an expected familiarity of Azure DevOps required. For an in depth guide with Azure DevOps pipeline definitions, please see: https://docs.microsoft.com/en-us/azure/devops/pipelines/yaml-schema?view=azure-devops&tabs=schema.

The most common terminology and most important are the different containers work happens in.

`pipeline`: This is the largest unit of work. A pipeline contains many or no stages, jobs, and steps

`Stage`: A stage is a collection of jobs. By default, stages do not run in parallel.

`Job`: Jobs are the smallest unit of work which happen on a unique machine. Jobs by default run in parallel, but may be set to depend on another job. **Every job executes its work on a unique machine**.

`Steps`: Steps are the smallest unit of work, they generally correspond to one command that will happen in a job. Normally a job contains steps, which execute serially.

## CI Overview

Coreclr has many different pipelines. These exist to test stress configurations of the runtime and JIT. The two non-stress related pipelines are [runtime-coreclr](https://dev.azure.com/dnceng/public/_build?definitionId=649), and [runtime-coreclr outerloop](https://dev.azure.com/dnceng/public/_build?definitionId=655).

#### **Inner Loop**

Our innerloop CI runs on each PR where `src/coreclr/*` is modified. The build definition that is run is [runtime-coreclr](https://dev.azure.com/dnceng/public/_build?definitionId=649). Currently this is defined to run the following builds and tests. Note that each row in the column runs on one build machine, and if there are tests runs, they scale to many test machines in Helix. For each test run we currently run with TieredCompilation and TieredCompilation off. Therefore if we have 2,000 innerloop tests we will run 4,000 tests total for that architecture/os. If we also run R2R testing for the platform, it is another 2,000 innerloop tests that run by running crossgen on the test, then invoking the R2R compiled executable. In the table the Test Count is an aggregate of all tests run for the platform.

*Note*

The **Build Tests** column is important to call out as one of the most important long running jobs. If there is
a "Shared" comment it signifies that our tests are built on OSX in ~15 minutes instead of the platform they run on and share the managed components with every other shared test platform. If "Shared" is missing, the platform takes ~25 minutes and happens in parallel with other platforms.

*Special Jobs*

`Formatting Linux x64` and `Formatting Windows x64` are special jobs which run clang-tidy of `src/coreclr/jit/*`. If there is a failure, there is a patch file that is created that can be applied to fix the source formatting.

| OS      | Architecture | Build Type | Product Build | Build Tests | Run coreclr Tests | Test Count | R2R   |
| --      | ------------ | ---------- | ------------- | ----------- | ----------------- | ---------- | ----- |
| Windows | x64          | Debug      | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Windows | x86          | Debug      | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Windows | x64          | Checked    | - [x]         | - [x]       | - [x]             | 6k         | - [x] |
| Windows | x86          | Checked    | - [x]         | - [x]       | - [x]             | 6k         | - [x] |
| Windows | arm          | Checked    | - [x]         | - [x]       | - [x]             | 4k         | - [ ] |
| Windows | arm64        | Checked    | - [x]         | - [x]       | - [ ]             | 0          | - [ ] |
| Windows | x64          | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Windows | arm          | Release    | - [x]         | - [x]       | - [ ]             | 0          | - [ ] |
| Windows | arm64        | Release    | - [x]         | - [x]       | - [ ]             | 0          | - [ ] |
| Linux   | arm          | Checked    | - [x]         | - [x]       | - [x]             | 4k         | - [ ] |
| Linux   | arm64        | Checked    | - [x]         | - [x]       | - [x]             | 4k         | - [ ] |
| Linux   | x64_musl     | Checked    | - [x]         |   Shared    | - [x]             | 4k         | - [ ] |
| Linux   | x64          | Checked    | - [x]         |   Shared    | - [x]             | 6k         | - [x] |
| OSX     | x64          | Checked    | - [x]         |   Shared    | - [x]             | 6k         | - [x] |
| Linux   | x64_musl     | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Linux   | x64_rhel     | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| OSX     | x64          | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |

#### **Outerloop Loop**

| OS      | Architecture | Build Type | Product Build | Build Tests | Run coreclr Tests | Test Count | R2R   |
| --      | ------------ | ---------- | ------------- | ----------- | ----------------- | ---------- | ----- |
| Windows | arm          | Debug      | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Windows | arm64        | Debug      | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Linux   | musl_x64     | Debug      | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Linux   | musl_arm64   | Debug      | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| OSX     | x64          | Debug      | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Linux   | x64          | Debug      | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Windows | x64          | Checked    | - [x]         | - [x]       | - [x]             | 30k        | - [x] |
| Windows | x86          | Checked    | - [x]         | - [x]       | - [x]             | 30k        | - [x] |
| Windows | arm          | Checked    | - [x]         | - [x]       | - [x]             | 0          | - [ ] |
| Windows | arm64        | Checked    | - [x]         | - [x]       | - [ ]             | 0          | - [ ] |
| Linux   | arm          | Checked    | - [x]         | - [x]       | - [x]             | 20k        | - [ ] |
| Linux   | arm64        | Checked    | - [x]         | - [x]       | - [x]             | 30k        | - [x] |
| Linux   | musl_x64     | Checked    | - [x]         |   Shared    | - [x]             | 30k        | - [x] |
| Linux   | musl_arm64   | Checked    | - [x]         | - [x]       | - [x]             | 30k        | - [x] |
| Linux   | x64_rhel     | Checked    | - [x]         | - [x]       | - [x]             | 0          | - [ ] |
| Linux   | x64          | Checked    | - [x]         |   Shared    | - [x]             | 30k        | - [x] |
| OSX     | x64          | Checked    | - [x]         |   Shared    | - [x]             | 30k        | - [x] |
| Linux   | arm          | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Linux   | arm64        | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Linux   | x64          | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| OSX     | x64          | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |
| Windows | x86          | Release    | - [x]         | - [ ]       | - [ ]             | 0          | - [ ] |

## Analytics

Azure Dev ops gives per pipeline analysis of pass rates. The unfortunate problem about how metrics are measured is the entire pipeline has to complete with a success. If there is a single failure, then the entire pipeline will be marked as a failure and analytics will track a failure.

Coreclr's pipeline is complex in that it runs on a distributed system between 50 and 100 machines. This distributed nature makes the end to end success of the pipeline very vulnerable to machine issues.

Azure Dev Ops provides analytics which can help bucket failures into categories. This bucketing requires logging in our build and test steps to be correctly reported.

In order to view the analytics of the pipeline navigate to [runtime-coreclr](https://dev.azure.com/dnceng/public/_build?definitionId=649) and click the [analytics tab](https://dev.azure.com/dnceng/public/_build?definitionId=649&view=ms.vss-pipelineanalytics-web.new-build-definition-pipeline-analytics-view-cardmetrics). There are three different tabs all useful for different reasons.

**Pipeline Pass Rate**

This is tracking the pipeline pass rate generally over two weeks. This view is not very useful for [runtime-coreclr](https://dev.azure.com/dnceng/public/_build?definitionId=649) as the PR pipeline is expected to break during PR validation. Therefore, it is generally recommended to view [runtime-coreclr outerloop](https://dev.azure.com/dnceng/public/_build?definitionId=655) to get a better idea of what the overall success rate is for [runtime-coreclr](https://dev.azure.com/dnceng/public/_build?definitionId=228). Note that this is not exactly a fair comparison as we run signicantly more tests in [runtime-outerloop](https://dev.azure.com/dnceng/public/_build?definitionId=655) across more platforms. It is however, a good proxy to see the overall CI health.

Opening the [runtime-outerloop Pipeline Pass Rate](https://dev.azure.com/dnceng/public/_pipeline/analytics/stageawareoutcome?definitionId=655&contextType=build) there is a presentation of a line graph of the end to end success rate for the pipeline over time.

The **Failure Trend** graph attempts to show what is failing in bar graph and give a small insight into what is generally failing.

The **Failed Runs** graph is the most interesting for finding specific issues. As of writing the `Top 10 failing tasks` are:

*Note* that any one of these buckets can include random one off infrastructure failures or systematic Azure Dev Ops failures. For example the build bucket can include issues like:

>  fips.c(143): OpenSSL internal error

**Failure Buckets**

1. Default\Send tests to Helix
    - This set can be one of two problems. Either we have tests that failed or Helix has failed with some infrastructure issue.
2. Default\Build product
    - Build related failures
3. Default\Build managed test components
    - Build failures while building the managed components of our tests
4. Default\Initialize containers
    - This is an Azure DevOps infrastructure issue. It manifests while setting up the docker container and fails to start the environment correctly.
5. Send Helix End Telemetry
    - This is a Helix infrastructure issue
6. Default\Build native test components
    - This is a failure in the native test build. Generally this is an Azure Dev Ops issue because we do little work inside this step.
7. Default\Download product build
    - Generally this is an Azure Dev Ops issue because we do little work inside this step.
8. Default\Unsize GIT Repository
    - Generally this is an Azure Dev Ops issue because we do little work inside this step.
9. Default\Component Detection
    - This is an Azure DevOps issue

Below each of these buckets are tabs which show individual runs which can be drilled through to find specific instances of each failure.

**Test Pass rate**

This drill through is extremely useful for finding individual flakey tests. Coreclr works to keep tests 100% reliable. Tests which appear on this list should be disabled **and** fixed. As even a small amount of unreliability in the tests will equate to a significant percentage of pipeline failure.

Clicking on an individual test will show its pass/failures for every run. **Looking back through this history is useful for finding a change that may have caused a test to become flakey.**

**Pipeline Duration**

This tracks the overall end to end run time of a pipeline. This graph is useful for looking at machine utilization on a daily cadence. Coreclr has a generous timeout, which generally means that when our pipeline time goes up significantly, we have hit machine load, either with the build or tests.

## Resources

**Kusto**

[Kusto](https://dataexplorer.azure.com/clusters/engsrvprod/databases/engineeringdata) is a hot data storage we have access to, to help query information from several different locations. There are many uses for Helix, but it involves heavy use of query language. For example below is a query which graphs machine utilization by day.

Specifically the query is useful for finding out whether a specific Helix Queue (a group of machines) is overloaded or not. This is useful for diagnosing arm hardware issues, because we have a fixed amount that is easily overloaded.

```
WorkItems
| where QueueName == "ubuntu.1804.armarch.open"
| extend DaysAgo = datetime_diff('Day', now(), Queued)
| extend QueueTimeInSeconds = datetime_diff('Second', Started, Queued)
| extend RunTimeInSeconds = datetime_diff('Second', Finished, Started)
| summarize percentile(QueueTimeInSeconds, 90), percentile(RunTimeInSeconds, 90), count() by DaysAgo
| sort by DaysAgo asc
| render columnchart
```

