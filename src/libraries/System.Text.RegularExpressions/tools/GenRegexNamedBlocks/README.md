# GenRegexNamedBlocks Tool

## Overview

This tool generates the named Unicode blocks for `RegexCharClass.cs` based on the Unicode Character Database (UCD) `Blocks.txt` file. Named blocks allow regex patterns to match characters in specific Unicode blocks using syntax like `\p{IsBasicLatin}` or `\p{IsGreek}`.

The current implementation is based on **Unicode 17.0**.

## Usage

To update the named blocks when a new Unicode version is released:

1. Download the `Blocks.txt` file from the Unicode Consortium:
   ```
   https://www.unicode.org/Public/UCD/latest/ucd/Blocks.txt
   ```


2. Run the tool from this directory:
   ```bash
   dotnet run -- <path-to-Blocks.txt> ../../src/System/Text/RegularExpressions/RegexCharClass.Tables.cs
   ```

3. The tool will generate the `RegexCharClass.Tables.cs` file with all named blocks

4. Update tests in `RegexCharacterSetTests.cs` to include tests for new blocks if needed

5. Build and test to ensure all tests pass

## Notes

- The tool automatically excludes:
  - Blocks outside the Basic Multilingual Plane (BMP) (code points >= U+10000)
  - Surrogate blocks (U+D800-U+DFFF)
  - Private Use Area blocks


- Block names are converted to "Is" + alphanumeric characters + hyphens (e.g., "Greek and Coptic" becomes "IsGreekandCoptic")

- The tool sorts blocks alphabetically by name for consistent output

- For backward compatibility, some aliases like "IsGreek" (alias for "IsGreekandCoptic") should be manually maintained

## See Also

- [Unicode Character Database](https://www.unicode.org/ucd/)
- [Unicode Block Names](https://www.unicode.org/Public/UCD/latest/ucd/Blocks.txt)
- Related tool: `src/libraries/System.Text.Encodings.Web/tools/GenUnicodeRanges/`
