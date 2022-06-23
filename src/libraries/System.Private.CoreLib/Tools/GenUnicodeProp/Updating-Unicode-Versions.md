# Instructions for updating Unicode version in dotnet/runtime

## Table of Contents

- [Instructions for updating Unicode version in dotnet/runtime](#instructions-for-updating-unicode-version-in-dotnetruntime)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Add the new Unicode files into the runtime-assets repo](#add-the-new-unicode-files-into-the-runtime-assets-repo)
  - [Ingest the created package into dotnet/runtime repo](#ingest-the-created-package-into-dotnetruntime-repo)
  - [Update dotnet/runtime libraries to consume the new Unicode changes](#update-dotnetruntime-libraries-to-consume-the-new-unicode-changes)

## Overview

This repository has several places that need to be updated when we are ingesting a new version of Unicode, mainly because different libraries we have in the runtime depend on specific data that could change with each update (e.g., new characters being added, casing information changing, etc.). Here are the steps that need to be followed when ingesting a new version of Unicode in dotnet/runtime:

## Add the new Unicode files into the runtime-assets repo

1. First step is that we need to add the Unicode data to somewhere that our dotnet/runtime repo can later ingest. This "somewhere" is a package that we build in the runtime-assets repo. The Unicode data can be downloaded from the [Unicode website](https://www.unicode.org/), and more specifically from the files pushed to the following location: `https://www.unicode.org/Public/14.0.0/` (<-- change 14.0.0 for the version that you want to ingest.) Go into the `ucd` folder and download the following files:
    - CaseFolding.txt
    - PropList.txt
    - UnicodeData.txt
    - auxiliary/GraphemeBreakProperty.txt
    - auxiliary/GraphemeBreakTest.txt
    - emoji/emoji-data.txt
    - extracted/DerivedBidiClass.txt
    - extracted/DerivedName.txt

2. Once you have downloaded all those files, create a fork of the repo <https://github.com/dotnet/runtime-assets> and send a PR which creates a folder at `src/System.Private.Runtime.UnicodeData/<YourUnicodeVersion>` and places all of the downloaded files from step 1 there. You can look at a sample PR that did this for Unicode 14.0.0 here: <https://github.com/dotnet/runtime-assets/pull/179>

## Ingest the created package into dotnet/runtime repo

This should be done automatically by dependency-flow, so in theory there shouldn't be any user-action in order for this to happen, but we still call it out on these instructions since there could be a problem in the ingestion and that would cause a problem with the process. The way the process works, is that after the PR from the runtime-assets repo gets merged, a new build will be triggered in the runtime-assets pipeline which will produce the new Unicode package, and once that build is done (and assuming it succeeds) it will also trigger the subscription that dotnet/runtime has against the runtime-assets repo, which will generate a dependency PR (like [this one](https://github.com/dotnet/runtime/pull/65843)) which will ingest the new package version in dotnet/runtime.

## Update dotnet/runtime libraries to consume the new Unicode changes

1. Follow the [instructions to run GenUnicodeProp](./Readme.md) which will generate a new `CharUnicodeInfoData.cs` file and will tell you where you need to copy the generated file. Make sure after compiling the GenUnicodeProp tool, that by inspecting the contents of the produced assembly, it contains all of the updated resources embedded into it, since those embedded resources are what is used to produce `CharUnicodeInfoData.cs`. You can inspect the embedded resources on the assembly using a tool like ILSpy.
2. Follow the [instructions on how to update System.Text.Encondings.Web](../../../System.Text.Encodings.Web/tools/updating-encodings.md) projects. Those instructions will help you generate the files `UnicodeHelpers.generated.cs` and `UnicodeRangesTests.generated.cs`, which are consumed by both the test and the implementation projects for System.Text.Encodings.Web.
3. Search across the repo for all of the .csproj files which have the property `<UnicodeUcdVersion>` and update it to use the new version. If a project defines this property, then it is very likely it is consuming the runtime-assets package in some form, so it needs to be updated to consume the new version. At the time of the writing of this doc, the project files which need to be updated are:
   - GenUnicodeProp.csproj
   - TestUtilities.Unicode.csproj
   - System.Globalization.Tests.csproj
   - System.Globalization.Nls.Tests.csproj
   - System.Text.Encodings.Web.Tests.csproj
4. If the new Unicode data contains casing changes/updates, then we will also need to update `src/coreclr/pal/src/locale/unicodedata.cpp` file. This file is used by most of the reflection stack whenever you specify the `BindingFlags.IgnoreCase`. In order to regenerate the contents of the `unicdedata.cpp` file, you need to run the Program located at `src/coreclr/pal/src/locale/unicodedata.cs` and give a full path to the new UnicodeData.txt as a parameter.
5. Update the Regex casing equivalence table using the UnicodeData.txt file from the new Unicode version. You can find the instructions on how to do this [here](../../../System.Text.RegularExpressions/tools/Readme.md).
6. Finally, last step is to update the license for the Unicode data into our [Third party notices](../../../../../THIRD-PARTY-NOTICES.TXT) by copying the contents located in `https://www.unicode.org/license.html` to the section that has the Unicode license in our notices.
7. That's it, now commit all of the changed files, and send a PR into dotnet/runtime with the updates. If there were any special things you had to do that are not noted on this document, PLEASE UPDATE THESE INSTRUCTIONS to facilitate future updates.
