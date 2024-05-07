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

When testing changes locally, consider changing the `JobNotificationEmail` in `Program.cs` to your own alias to avoid spamming the DG.

Job notification emails will contain job ID GUIDs. Use the commands below to query the status and download artifacts:
```bash
:: Get the list of tasks and their status
onefuzz jobs get <id>

:: Download all containers (setup, crashes, inputs, ...)
onefuzz containers download_job <id>

:: Download logs for all tasks
onefuzz debug logs get --job_id <id>

:: Submit a test job that runs for 12 hours (run from one of the deployment subfolders)
oip submit --config .\OneFuzzConfig.json --drop-path . --do-not-file-bugs --duration 12 --platform windows
```

[OneFuzz onboarding instructions]: https://eng.ms/docs/cloud-ai-platform/azure-edge-platform-aep/aep-security/epsf-edge-and-platform-security-fundamentals/the-onefuzz-service/onefuzz/faq/onefuzz/onefuzz_access#:~:text=with%20these%20permissions.-,For%20the%20developer%20who%20will%20be%20fuzzing,-Access%20to%20the
[OneFuzzConfig.json format docs]: https://eng.ms/docs/cloud-ai-platform/azure-edge-platform-aep/aep-security/epsf-edge-and-platform-security-fundamentals/the-onefuzz-service/onefuzz/onefuzzconfig/onefuzzconfigv3
