## PR Builds
When submitting a PR to the `dotnet/runtime` repository various builds will run validation in many areas to ensure we keep productivity and quality high.

The `dotnet/runtime` validation system can become overwhelming as we need to cover a lot of build scenarios and test in all the platforms that we support. In order to try to make this more reliable and spend the least amount of time testing what the PR changes need we have various pipelines, required and optional that are covered in this document.

Most of the repository pipelines use a custom mechanism to evaluate paths based on the changes contained in the PR to try and build/test the least that we can without compromising quality. This is the initial step on every pipeline that depends on this infrastructure, called "Evaluate Paths". In this step you can see the result of the evaluation for each subset of the repository. For more details on which subsets we have based on paths see [here](https://github.com/dotnet/runtime/blob/513fe2863ad5ec6dc453d223d4b60f787a0ffa78/eng/pipelines/common/evaluate-default-paths.yml). Also to understand how this mechanism works you can read this [comment](https://github.com/dotnet/runtime/blob/513fe2863ad5ec6dc453d223d4b60f787a0ffa78/eng/pipelines/evaluate-changed-paths.sh#L3-L12).

### Runtime pipeline
This is the "main" pipeline for the runtime product. In this pipeline we include the most critical tests and platforms where we have enough test resources in order to deliver test results in a reasonable amount of time. The tests executed in this pipeline for runtime and libraries are considered innerloop, are the tests that are executed locally when one runs tests locally.

For mobile platforms and wasm we run some smoke tests that aim to protect the quality of these platforms. We had to move to a smoke test approach given the hardware and time limitations that we encountered and contributors were affected by this with unstability and long wait times for their PRs to finish validation.

### Runtime-dev-innerloop pipeline
This pipeline is also required, and its intent is to cover a developer innerloop scenarios that could be affected by any change, like running a specific build command or running tests inside Visual Studio, etc.

### Dotnet-linker-tests
This is also a required pipeline. The purpose of this pipeline is to test that the libraries code is linker friendly. Meaning that when we trim our libraries using the ILLink, we don't have any trimming bugs, like a required method on a specific scenario is trimmed away by accident.

### Runtime-staging
This pipeline runs on every change, however it behaves a little different than the other pipelines. This pipeline, will not fail if there are test failures, however it will fail if there is a timeout or a build failure. The reason why we fail on build failures is because we want to protect the developer innerloop (building the repository) for this platform.

The tests will not fail because the intent of this platform is to stage new platforms where the test infrastructure is new and we need to test if we have enough capacity to include that new platform on the "main" runtime pipeline without causing flakiness. Once we analyze data and a platform is stable when running on PRs in this pipeline for at least a weak it can be promoted either to the `runtime-extra-platforms` pipeline or to the `runtime` pipeline.

### Runtime-extra-platforms
This pipeline does not run by default as it is not required for a PR, but it runs twice a day, and it can also be invoked in specific PRs by commenting `/azp run runtime-extra-platforms`. However, this pipeline is still an important part of our testing.

This pipeline runs innerloop tests on platforms where we don't have enough hardware capacity to run tests (mobile, browser) or on platforms where we believe tests should organically pass based on the coverage we have in the "main" runtime pipeline. For example, in the "main" pipeline we run tests on Ubuntu 21.10 but since we also support Ubuntu 18.04 which is an LTS release, we run tests on Ubuntu 18.04 of this pipeline just to make sure we have healthy tests on those platforms which we are releasing a product for.

Another concrete scenario would be windows arm64 for libraries tests. Where we don't have enough hardware, but the JIT is the most important piece to test as that is what generates the native code to run on that platform, so we run JIT tests on arm64 in the "main" pipeline, but our libraries tests are only run on the `runtime-extra-platforms` pipeline.

### Outerloop pipelines
We have various pipelines that their names contain `Outerloop` on them. These pipelines will not run by default on every PR, they can also be invoked using the `/azp run` comment and will run on a daily basis to analyze test results.

These pipelines will run tests that take very long, that are not very stable (i.e some networking tests), or that modify machine state. Such tests are called `Outerloop` tests rather than `innerloop`.

## Rerunning Validation

Validation may fail for several reasons:

### Option 1: You have a defect in your PR

* Simply push the fix to your PR branch, and validation will start over.

### Option 2: There is a flaky test that is not related to your PR

* Your assumption should be that a failed test indicates a problem in your PR. (If we don't operate this way, chaos ensues.) If the test fails when run again, it is almost surely a failure caused by your PR. However, there are occasions where unrelated failures occur. Here's some ways to know:
  * Perhaps you see the same failure in CI results for unrelated active PR's.
  * It's a known issue listed in our [big tracking issue](https://github.com/dotnet/runtime/issues/702) or tagged `blocking-clean-ci` [(query here)](https://github.com/dotnet/runtime/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+label%3Ablocking-clean-ci+)
  * It's otherwise beyond any reasonable doubt that your code changes could not have caused this.
  * If the tests pass on rerun, that may suggest it's not related.
* In this situation, you want to re-run but not necessarily rebase on main.
  * To rerun just the failed leg(s):
    * Click on any leg. Navigate through the Azure DevOps UI, find the "..." button and choose "Retry failed legs"
    * Or, on the GitHub Checks tab choose "re-run failed checks". This will not rebase your change.
  * To rerun all validation:
    * Add a comment `/azp run runtime`
    * Or, click on "re-run all checks" in the GitHub Checks tab
    * Or, simply close and reopen the PR.
* If you have established that it is an unrelated failure, please ensure we have an active issue for it. See the [unrelated failure](#what-to-do-if-you-determine-the-failure-is-unrelated) section below.
* Whoever merges the PR should be satisfied that the failure is unrelated, is not introduced by the change, and that we are appropriately tracking it.

### Option 3: The state of the main branch HEAD is bad.

* This is the very rare case where there was a build break in main, and you got unlucky. Hopefully the break has been fixed, and you want CI to rebase your change and rerun validation.
* To rebase and rerun all validation:
  * Add a comment `/azp run runtime`
  * Or, click on "re-run all checks" in the GitHub Checks tab
  * Or, simply close and reopen the PR.
  * Or, amend your commit with `--amend --no-edit` and force push to your branch.

### Additional information:
  * You can list the available pipelines by adding a comment like `/azp list` or get the available commands by adding a comment like `azp help`.
  * In the rare case the license/cla check fails to register a response, it can be rerun by issuing a GET request to `https://cla.dotnetfoundation.org/check/dotnet/runtime?pullRequest={pr_number}`. A successful response may be a redirect to `https://github.com`.
  * Reach out to the infrastructure team for assistance on [Teams channel](https://teams.microsoft.com/l/channel/19%3ab27b36ecd10a46398da76b02f0411de7%40thread.skype/Infrastructure?groupId=014ca51d-be57-47fa-9628-a15efcc3c376&tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47) (for corpnet users) or on [Gitter](https://gitter.im/dotnet/community) in other cases.

## What to do if you determine the failure is unrelated

If you have determined the failure is definitely not caused by changes in your PR, please do this:

* Search for an [existing issue](https://github.com/dotnet/runtime/issues). Usually the test method name or (if a crash/hang) the test assembly name are good search parameters.
  * If there's an existing issue, add a comment with
    * a) the link to the build
    * b) the affected configuration (ie `net6.0-windows-Release-x64-Windows.81.Amd64.Open`)
    * c) all console output including the error message and stack trace from the Azure DevOps tab (This is necessary as retention policies are in place that recycle old builds.)
    * d) if there's a dump file (see Attachments tab in Azure DevOps) include that
    * If the issue is already closed, reopen it and update the labels to reflect the current failure state.
  * If there's no existing issue, create an issue with the same information listed above.
  * Update the original pull request with a comment linking to the new or existing issue.
* In a follow-up Pull Request, disable the failing test(s) with the corresponding issue link tracking the disable.
  * Update the tracking issue with the label `disabled-test`.
  * For libraries tests add a [`[ActiveIssue(link)]`](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.XUnitExtensions/src/Attributes/ActiveIssueAttribute.cs) attribute on the test method. You can narrow the disabling down to runtime variant, flavor, and platform. For an example see [File_AppendAllLinesAsync_Encoded](https://github.com/dotnet/runtime/blob/cf49643711ad8aa4685a8054286c1348cef6e1d8/src/libraries/System.IO.FileSystem/tests/File/AppendAsync.cs#L74)
  * For runtime tests found under `src/tests`, please edit [`issues.targets`](https://github.com/dotnet/runtime/blob/main/src/tests/issues.targets). There are several groups for different types of disable (mono vs. coreclr, different platforms, different scenarios). Add the folder containing the test and issue mimicking any of the samples in the file.

There are plenty of possible bugs, e.g. race conditions, where a failure might highlight a real problem and it won't manifest again on a retry. Therefore these steps should be followed for every iteration of the PR build, e.g. before retrying/rebuilding.
