---
name: fuzzlyn-triage
description: Triage Fuzzlyn CI runs.
---

# Fuzzlyn triage

#### 1 — Goal

Initial investigation of Fuzzlyn found issues with assertions in CI runs.

#### 2 — Required user inputs

Ask the user for a link to a Fuzzlyn CI run if not provided.

#### 3 — Investigation steps (must be completed in order)

1. Download all the Issues artifacts for the CI run.
These artifacts are zipped files named "Issues_{platform}_Checked.zip".
For example, "Issues_windows_x64_Checked.zip".
This may take a long time, so download these in parallel.
Extract all the downloaded zip files into a single directory, and delete the .zip files once extracted.

2. Look at the reduced examples that are available.
Focus on ones that have JIT assertion failures in them, and group by the specific assertion failure.
For each assertion failure pick a single example and create a directory named based on the number part of the seed.
Create an "example.cs" file in the directory with the content of the example.
For example, if the seed is 123456789, create a directory named "123456789" and an "example.cs" file in it with the content of the example.

3. Look for the .mc files corresponding to the examples you have created.
Move the relevant .mc files under the appropriate example folders.

4. Download the Helix payload for the host you are using to run these triage steps, from the Partition0 work item.
In most cases this will be the windows-x64 job; if you are running the triage steps on a different host, use that host's payload instead.
To download the payload, obtain the Job ID from the "Send job to Helix" step in the CI run.
Download the payload for Partition0 with `runfo get-helix-payload -j <helix job ID> -w Partition0 -o <output directory>`.
This should result in the host-appropriate corerun, superpmi, and mcs tools that you should use for the next steps.
Choose the payload based on the machine running triage, not on the platform where the examples reproduced.

Now, for each example, do the following. Do NOT parallelize. Finish ALL steps for each example before you start on the next example.

1. Use superpmi to replay the .mc files for that example until you find a context that reproduces the assertion failure.
To do that, for each example, run `superpmi.exe <path to clrjit.dll> <path to .mc>`.
The clrjit.dll used should be the one that corresponds to the target that reproduced the error.
For example, if the example reproduced on linux-x64, then use clrjit_unix_x64_x64.dll.
If the assertion failure reproduces it should give you the context index before the message, e.g. "#12345".
For the next steps use the numeric portion of that index, e.g. "12345" without the "#" symbol.

2. Once you have the context index, use superpmi to replay that specific context with `superpmi.exe <path to clrjit.dll> <path to .mc> -c <context index>`.
Validate that this reproduces the assertion failure instantly.

3. Use mcs.exe next to superpmi.exe to extract that single context to a repro.mc file with `mcs.exe -copy <context index> <path to .mc> <output repro.mc>`.
Delete all the other .mc files for that example, and keep only the repro.mc file.

4. Create a jitdump file.
To do that, run `superpmi.exe <path to clrjit.dll> <path to repro.mc> -jitoption JitDump=*` and pipe the output to a jitdump.txt file in the example's directory.

5. Analyze the jitdump file and try to come up with the root cause of that assertion failure.
The JIT source code creating the jitdump is available at src/coreclr/jit/*.
Do NOT make any changes to the source code; only analyze the jitdump file and try to come up with a root cause.
Put your analysis into an analysis.md file in the example's directory.

6. Create a details.zip file that includes the repro.mc and also the jitdump.txt file.

7. Create an issue.md file that can be used to open a GitHub issue later.
In this issue.md file include the following:
  - A header with the assertion failure message.
  For example, if the assertion failure is "Assertion failed: x > 0", the header should be "# Assertion failed: x > 0".
  - The reduced example from the .cs file.
  ALWAYS keep the comment header.
  - A section containing your analysis of the jitdump.
  Wrap the analysis in a `<details> <summary> Analysis of jitdump </summary> ... </details>` block.
  Make sure to add a note that the analysis is AI generated.
  - A blurb "Attached details.zip file that includes the repro.mc and jitdump.txt files for this example."
  - A "cc @dotnet/jit-contrib" at the end to make sure the JIT team sees the issue.

8. Once you have done the above steps, proceed with the next example if there are still examples left.