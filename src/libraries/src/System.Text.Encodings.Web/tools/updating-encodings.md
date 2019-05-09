### Introduction

This folder contains tools which allow updating the Unicode data within the __System.Text.Encodings.Web__ package. These data files come from the Unicode Consortium's web site (see https://www.unicode.org/Public/UCD/latest/) and are used to generate the `UnicodeRanges` class and the internal "defined characters" bitmap against which charaters to be escaped are checked.

### Current implementation

The current version of the Unicode data checked in is __12.1.0__. The archived files can be found at https://unicode.org/Public/12.1.0/.

### Updating the implementation

Updating the implementation consists of three steps: checking in a new version of the Unicode data files, generating the shared files used by the runtime and the unit tests, and pointing the unit test files to the correct version of the data files.

As a prerequisite for updating the tools, you will need the _dotnet_ tool (version 3.0 or above) available from your local command prompt.

1. Get the latest __UnicodeData.txt__ and __Blocks.txt__ files from the Unicode Consortium web site. Drop __UnicodeData.txt__ into the __src/Common/tests/Data__ folder, rename it so that the file name contains the Unicode version it represents (e.g., _UnicodeData.11.0.txt_) and submit it to the Git repo. Place the __Blocks.txt__ in a temporary location; it will not be submitted to the Git repo.

2. Open a command prompt and navigate to the __src/System.Text.Encodings.Web/tools/GenDefinedCharList__ directory, then run the following command, replacing the first parameter with the path to the _UnicodeData.txt_ file you downloaded in the previous step. This command will update the "defined characters" bitmap within the runtime folder. The test project also consumes the file from the _src_ folder, so running this command will update both the runtime and the test project.

```txt
dotnet run -- "path_to_UnicodeData.txt" ../../src/System/Text/Unicode/UnicodeHelpers.generated.cs
```

3. Open a command prompt and navigate to the __src/System.Text.Encodings.Web/tools/GenUnicodeRanges__ directory, then run the following command, replacing the first parameter with the path to the _Blocks.txt_ file you downloaded in the first step. This command will update the `UnicodeRanges` type in the runtime folder and update the unit tests to exercise the new APIs.

```txt
dotnet run -- "path_to_Blocks.txt" ../../src/System/Text/Unicode/UnicodeRanges.generated.cs ../../tests/UnicodeRangesTests.generated.cs
```

4. Update the __ref__ APIs to reflect any new `UnicodeRanges` static properties which were added in the previous step, otherwise the unit test project will not be able to reference them. See https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/updating-ref-source.md for instructions on how to update the reference assemblies.

5. Update the __src/System.Text.Encodings.Web/tests/System.Text.Encodings.Web.Tests.csproj__ file to reference the new file that you dropped into the __src/Common/tests/data__ folder in the first step above. Open the .csproj file in a text editor, and replace both the `<EmbeddedResource>` and the `<LogicalName>` elements at the end of the document to reference the new file name.

6. In the file __src/System.Text.Encodings.Web/tests/UnicodeHelpersTests.cs__, update the _UnicodeDataFileName_ const string at the top of the file to reflect the new file name for _UnicodeData.x.y.txt_.

7. Finally, update the _Current implementation_ section at the beginning of this markdown file to reflect the version of the Unicode data files which were given to the tools. Remember also to update the URL within that section so that these data files can be easily accessed in the future.

8. Commit to Git the __UnicodeData.x.y.txt__ file added to the _Common_ folder earlier, along with all files modified as part of following the above steps.
