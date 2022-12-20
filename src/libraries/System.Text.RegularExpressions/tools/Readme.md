# GenerateRegexCasingTable Tool

## Overview

This tool is used for generating RegexCaseFolding.Data.cs which contains the three tables that will be used for performing matching operations when using RegexOptions.IgnoreCase. This tool will need to be used every time that we are ingesting a new version of Unicode in the repo. The current table contains the Unicode data from version 14.0.0.

## Updating the version of Unicode used

For instructions on how to update Unicode version on the whole repo, you find the instructions [here](../../System.Private.CoreLib/Tools/GenUnicodeProp/Updating-Unicode-Versions.md).

These are the steps to follow in order to update the Regex case equivalence table:

1. Download UnicodeData.txt from the version of Unicode that you are updating to from unicode.org. For example, for version 14.0.0, you can find that file [here](https://www.unicode.org/Public/14.0.0/ucd/UnicodeData.txt).
2. Once you have that file locally, run the following command from the command line: `dotnet run -- <pathToUnicodeData.txt>`.
3. A file named `RegexCaseFolding.Data.cs` will be created in this directory. Use it to replace the one at `src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/`.
4. Update this Readme Unicode version mentioned in the overview section to point to the version that was used to produce the table.
