# GenerateRegexCasingTable Tool

## Overview

This tool is used for generating RegexCaseEquivalences.Data.cs which contains the three tables that will be used for performing matching operations when using RegexOptions.IgnoreCase. This tool will need to be used every time that we are ingesting a new version of Unicode in the repo. The current table contains the Unicode data from version 15.0.0.

## Updating the version of Unicode used

For instructions on how to update Unicode version on the whole repo, you find the instructions [here](/src/libraries/System.Private.CoreLib/Tools/GenUnicodeProp/Updating-Unicode-Versions.md).

These are the steps to follow in order to update the Regex case equivalence table:

1. Download UnicodeData.txt from the version of Unicode that you are updating to from unicode.org. For example, for version 15.0.0, you can find that file [here](https://www.unicode.org/Public/15.0.0/ucd/UnicodeData.txt).
2. Once you have that file locally, run the following command from the command line: `dotnet run -- <pathToUnicodeData.txt>`.
3. A file named `RegexCaseEquivalences.Data.cs` will be created in this directory. Use it to replace the one at `src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/`.
4. Update this Readme Unicode version mentioned in the overview section to point to the version that was used to produce the table.

## Updating UnicodeCategoryRanges.cs file

`UnicodeCategoryRanges.cs` is programmatically generated file which provides serialized Binary Decision Diagram (BDD) Unicode category definitions.
Expect some tests can fail if updating the Unicode Categories data in the runtime without updating `UnicodeCategoryRanges.cs`. Here is some example of such failure:
```
      System.Text.RegularExpressions.Tests.RegexMatchTests.StandardCharSets_SameMeaningAcrossAllEngines(singleCharPattern: "\\w") [FAIL]
        Expected: True
        Actual:   False
        Stack Trace:
          C:\oss\runtime\src\libraries\System.Text.RegularExpressions\tests\FunctionalTests\Regex.Match.Tests.cs(2500,0): at System.Text.RegularExpressions.Tests.RegexMatchTests.VerifyIsMatch(Regex r, String input, Boolean expected, TimeSpan timeout, String pattern, RegexOptions options)
          C:\oss\runtime\src\libraries\System.Text.RegularExpressions\tests\FunctionalTests\Regex.Match.Tests.cs(2456,0): at System.Text.RegularExpressions.Tests.RegexMatchTests.StandardCharSets_SameMeaningAcrossAllEngines(String singleCharPattern)
```

To update `UnicodeCategoryRanges.cs`:
- Build the libraries and the test with `Debug` configuration.
- The code generate the file exist in [UnicodeCategoryRangesGenerator.Generate](https://github.com/dotnet/runtime/blob/ad9efe886e16b179f2ce8e93221386d420ffe10d/src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/Symbolic/UnicodeCategoryRangesGenerator.cs#L21) method.
- To call `UnicodeCategoryRangesGenerator.Generate`, set the [Enabled](https://github.com/dotnet/runtime/blob/70e1072edc6c1a399f77a4de7de84045193f1409/src/libraries/System.Text.RegularExpressions/tests/FunctionalTests/RegexExperiment.cs#L26) property to true. Then run the test case [System.Text.RegularExpressions.Tests.RegexExperiment.RegenerateUnicodeTables()](https://github.com/dotnet/runtime/blob/70e1072edc6c1a399f77a4de7de84045193f1409/src/libraries/System.Text.RegularExpressions/tests/FunctionalTests/RegexExperiment.cs#L38) which will generate the `UnicodeCategoryRanges.cs` file in the %temp% folder.
- Copy the file `UnicodeCategoryRanges.cs` from the %temp% folder to the path https://github.com/dotnet/runtime/blob/ad9efe886e16b179f2ce8e93221386d420ffe10d/src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/Symbolic



