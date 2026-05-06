> [!NOTE]
> These instructions require access to internal Microsoft resources.
> You **do not** need to read this to add new fuzzing targets / run them locally.
> This is meant to help anyone diagnosing issues if the OneFuzz pipeline starts failing.

Prerequisites:
- Follow [OneFuzz onboarding instructions] to get access to OneFuzz and download the tools (OIP, OneFuzz)
- Join the `dotnet-fuzz-updates` distribution group to receive notifications about started/failing jobs

Useful links:
- [OneFuzz Documentation](https://aka.ms/onefuzz)
- [OneFuzzConfig.json format docs]
- [deploy-to-onefuzz.yml](../../../eng/pipelines/libraries/fuzzing/deploy-to-onefuzz.yml)
- [Internal pipeline](https://dev.azure.com/dnceng/internal/_build?definitionId=1381)

Work items for issues identified by OneFuzz will be filed in https://dev.azure.com/dnceng/internal/_workitems
under the ".NET Libraries" area. Filter on the `OneFuzz` tag.

---

OneFuzz has support for native libFuzzer targets, where you supply the target harness that includes libFuzzer,
and dotnet-based targets, where you only supply the target managed assembly (library) with a well-known entry method.

Because we want to test the latest .NET runtime and libraries, we can't use the built-in dotnet target support in OneFuzz.
We need the flexibility to use the latest .NET version, as well as the ability to use custom builds of dotnet/runtime (such as using a checked CLR).

To get around this, we effectively supply the native target by bundling in [`libfuzzer-dotnet.exe`](https://github.com/Metalnem/libfuzzer-dotnet) ourselves,
which is a native libFuzzer harness that can talk with targets that were instrumented using [`SharpFuzz`](https://github.com/Metalnem/sharpfuzz).
Note that we also run `sharpfuzz` on target assemblies ourselves (e.g. `System.Text.Json.dll` in this example) before submitting jobs to OneFuzz.

When fuzzing, we (or the OneFuzz automation) launch `libfuzzer-dotnet`, and pass it the path to the managed target:
```bash
libfuzzer-dotnet.exe --target_path=DotnetFuzzing.exe --target_arg=JsonSerializerFuzzer -dict=dictionary -workers=1
```
This is what the generated `local-run.bat` script does, and what the configuration in `OneFuzzConfig.json` specifies as well.

In this case, `libfuzzer-dotnet` will use `target_path` and `target_arg` to start this project (`DotnetFuzzing`) with a single `JsonSerializerFuzzer` argument.
All other arguments (e.g. `dict`, `workers`) are only visible to `libfuzzer-dotnet`.

---

After following instructions in the README on how to run the fuzzer locally, you can deploy the same directory to OneFuzz as a test job.

When testing locally, consider changing the `JobNotificationEmail` in `Program.cs` to your own alias to avoid spamming the preset notification DG.

```bash
:: Submit a test job that runs for 12 hours (run from one of the deployment subfolders)
oip submit --config .\OneFuzzConfig.json --drop-path . --do-not-file-bugs --duration 12 --platform windows
```

Job notification emails will contain job ID GUIDs. Use the commands below to query the status and download artifacts:

```bash
:: Get the list of tasks and their status
onefuzz jobs get <id>

:: Download all containers (setup, crashes, inputs, ...)
onefuzz containers download_job <id>

:: Download logs for all tasks
onefuzz debug logs get --job_id <id>
```

[OneFuzz onboarding instructions]: https://eng.ms/docs/cloud-ai-platform/azure-edge-platform-aep/aep-security/epsf-edge-and-platform-security-fundamentals/the-onefuzz-service/onefuzz/faq/onefuzz/onefuzz_access#:~:text=with%20these%20permissions.-,For%20the%20developer%20who%20will%20be%20fuzzing,-Access%20to%20the
[OneFuzzConfig.json format docs]: https://eng.ms/docs/cloud-ai-platform/azure-edge-platform-aep/aep-security/epsf-edge-and-platform-security-fundamentals/the-onefuzz-service/onefuzz/onefuzzconfig/onefuzzconfigv3
