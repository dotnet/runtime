This folder contains the program used to generate the Unicode character categories file CharUnicodeInfoData.cs.
To run this tool, fetch the files listed below from https://www.unicode.org/Public/UCD/latest/ucd/.

 - `CaseFolding.txt`
 - `PropList.txt`
 - `UnicodeData.txt`
 - `auxiliary/GraphemeBreakProperty.txt`
 - `extracted/DerivedBidiClass.txt`
 - `extracted/DerivedName.txt`

And the files listed below from https://www.unicode.org/Public/emoji/:

 - `emoji-data.txt`

Drop all seven of these files into the same directory as this application, then execute:

> `dotnet run`

If you want to include casing data (simple case mappings + case folding) in the generated file, execute:

> `dotnet run -- -IncludeCasingData`
