This folder contains the program used to generate the Unicode character categories file CharUnicodeInfoData.cs.

Before running this tool, ensure the following are all in sync:

 - The package at https://github.com/dotnet/runtime-assets/tree/master/src/System.Private.Runtime.UnicodeData contains
   the up-to-date Unicode data you want to process.

 - The <SystemPrivateRuntimeUnicodeDataVersion> element in $(REPOROOT)/eng/Versions.props contains the correct version
   of the package mentioned above.

 - The <UnicodeUcdVersion> element in ./GenUnicodeProp.csproj contains the UCD version of the files you wish to process.

Once this has been configured, from this directory, invoke:

> `dotnet run`

If you want to include casing data (simple case mappings + case folding) in the generated file, execute:

> `dotnet run -- -IncludeCasingData`

Then move the generated CharUnicodeInfoData.cs file to $(LIBRARIESROOT)/System.Private.CoreLib/src/System/Globalization,
overwriting the file in that directory, and commit it. DO NOT commit the file to this directory.
