### Introduction

This folder contains tools which allow updating the Unicode data within the __System.Text.Encodings.Web__ package. These data files come from the Unicode Consortium's web site (see https://www.unicode.org/Public/UCD/latest/) and are used to generate the `UnicodeRanges` class and the internal "defined characters" bitmap against which charaters to be escaped are checked.

### Current implementation

The current version of the Unicode data checked in is __13.0.0__. The archived files can be found at https://unicode.org/Public/13.0.0/.

### Updating the implementation

Updating the implementation consists of three steps: checking in a new version of the Unicode data files (into the [runtime-assets](https://github.com/dotnet/runtime-assets) repo), generating the shared files used by the runtime and the unit tests, and pointing the unit test files to the correct version of the data files.

As a prerequisite for updating the tools, you will need the _dotnet_ tool (version 3.1 or above) available from your local command prompt.

1. Update the [runtime-assets](https://github.com/dotnet/runtime-assets) repo with the new Unicode data files. Instructions for generating new packages are listed at the repo root. Preserve the directory structure already present at https://github.com/dotnet/runtime-assets/tree/master/src/System.Private.Runtime.UnicodeData when making the change.

2. Get the latest __UnicodeData.txt__ and __Blocks.txt__ files from the Unicode Consortium web site. Drop them into a temporary location; they're not going to be committed to the main _runtime_ repo.

3. Open a command prompt and navigate to the __src/libraries/System.Text.Encodings.Web/tools/GenDefinedCharList__ directory, then run the following command, replacing the first parameter with the path to the _UnicodeData.txt_ file you downloaded in the previous step. This command will update the "defined characters" bitmap within the runtime folder. The test project also consumes the file from the _src_ folder, so running this command will update both the runtime and the test project.

```txt
dotnet run --framework netcoreapp3.1 -- "path_to_UnicodeData.txt" ../../src/System/Text/Unicode/UnicodeHelpers.generated.cs
```

4. Open a command prompt and navigate to the __src/libraries/System.Text.Encodings.Web/tools/GenUnicodeRanges__ directory, then run the following command, replacing the first parameter with the path to the _Blocks.txt_ file you downloaded earlier. This command will update the `UnicodeRanges` type in the runtime folder and update the unit tests to exercise the new APIs.

```txt
dotnet run --framework netcoreapp3.1 -- "path_to_Blocks.txt" ../../src/System/Text/Unicode/UnicodeRanges.generated.cs ../../tests/UnicodeRangesTests.generated.cs
```

5. Update the __ref__ APIs to reflect any new `UnicodeRanges` static properties which were added in the previous step, otherwise the unit test project will not be able to reference them. See https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/updating-ref-source.md for instructions on how to update the reference assemblies.

6. Update the __src/libraries/System.Text.Encodings.Web/tests/System.Text.Encodings.Web.Tests.csproj__ file to reference the new __UnicodeData.txt__ file that was added to the [runtime-assets](https://github.com/dotnet/runtime-assets) repo in step (1). Open the .csproj file in a text editor and replace the `<UnicodeUcdVersion>` property value near the top of the file to reference the new UCD version being consumed.

7. Finally, update the _Current implementation_ section at the beginning of this markdown file to reflect the version of the Unicode data files which were given to the tools. Remember also to update the URL within that section so that these data files can be easily accessed in the future.

8. Commit to Git the __*.cs__, __*.csproj__, and __*.md__ files that were modified as part of the above process.
