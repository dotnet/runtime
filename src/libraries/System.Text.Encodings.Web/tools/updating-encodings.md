### Introduction

*Note: For full instructions on how to update the Unicode version consumed by the whole repo (as opposed to just System.Text.Encodings.Web) please follow the steps on [this guide](../../System.Private.CoreLib/Tools/GenUnicodeProp/Updating-Unicode-Versions.md) instead.*

This folder contains tools which allow updating the Unicode data within the __System.Text.Encodings.Web__ package. These data files come from the Unicode Consortium's web site (see https://www.unicode.org/Public/UCD/latest/) and are used to generate the `UnicodeRanges` class and the internal "defined characters" bitmap against which charaters to be escaped are checked.

### Updating the implementation

Updating the implementation consists of two steps: checking in a new version of the Unicode data files (into the [runtime-assets](https://github.com/dotnet/runtime-assets) repo), and generating the shared files used by the runtime and the unit tests.

As a prerequisite for updating the tools, you will need the _dotnet_ tool (version 3.1 or above) available from your local command prompt.

1. Update the [runtime-assets](https://github.com/dotnet/runtime-assets) repo with the new Unicode data files. Instructions for generating new packages are listed at the repo root. Preserve the directory structure already present at https://github.com/dotnet/runtime-assets/tree/master/src/System.Private.Runtime.UnicodeData when making the change.

2. Get the latest __UnicodeData.txt__ and __Blocks.txt__ files from the Unicode Consortium web site. Drop them into a temporary location; they're not going to be committed to the main _runtime_ repo.

3. Open a command prompt and navigate to the __src/libraries/System.Text.Encodings.Web/tools/GenDefinedCharList__ directory, then run the following command, replacing the first parameter with the path to the _UnicodeData.txt_ file you downloaded in the previous step. This command will update the "defined characters" bitmap within the runtime folder. The test project also consumes the file from the _src_ folder, so running this command will update both the runtime and the test project.

```txt
dotnet run -- "path_to_UnicodeData.txt" ../../src/System/Text/Unicode/UnicodeHelpers.generated.cs
```

4. Open a command prompt and navigate to the __src/libraries/System.Text.Encodings.Web/tools/GenUnicodeRanges__ directory, then run the following command, replacing the first parameter with the path to the _Blocks.txt_ file you downloaded earlier. This command will update the `UnicodeRanges` type in the runtime folder and update the unit tests to exercise the new APIs.

```txt
dotnet run -- "path_to_Blocks.txt" ../../src/System/Text/Unicode/UnicodeRanges.generated.cs ../../tests/UnicodeRangesTests.generated.cs
```

5. Update the __ref__ APIs to reflect any new `UnicodeRanges` static properties which were added in the previous step, otherwise the unit test project will not be able to reference them. See https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/updating-ref-source.md for instructions on how to update the reference assemblies.
