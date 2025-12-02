# Native external libraries

This folder contains the source code of vendored third party native libraries that are used as dependencies for dotnet/runtime.

Vendored libraries are copies of third-party dependencies that are included in the project repository. These copies are built alongside the rest of the repository. The document describing our approach for vendored libraries can be found here: https://github.com/dotnet/arcade/blob/main/Documentation/UnifiedBuild/VMR-Strategy-For-External-Source.md

### Folder structure

Each native library is roughly structured the following way:

```
library-folder/
    library-source-code-files
library.cmake
library-version.txt
cgmanifest.json
```

- `library-folder/` is where the native source code lives.
- `library.cmake` is the main cmake file we add to build this library from anywhere in this repo.
- `library-version.txt` contains all the detailed release information for this library, as well as information about any local patches applied to the library.
- `cgmanifest.json` is the official file that describes the source code provenance for each one of the external libraries we consume.

### How to add or update a vendored library

1. Consult with the .NET Security experts to make sure we meet all of Microsoft's Open Source guidance, especially regarding security updates and timeline expectations. This step can only be performed by the .NET team.

2. Download a copy of the source code from an official public release and extract it inside the library folder under `src/native/external/<library-name>`.

   - If the source code comes from a public github repo, you would download it from an url like: `https://github.com/org/repo/releases`.
   - Make a note of the commit used to snap that release.
   - From the root folder of the repo, make a note of the license file.

3. Open the `cgmanifest.json` file. Add or modify the entry for this library to indicate the commit hash from which the copy of this release was obtained. The entry looks like this:

```json
"Registrations": [
  {
    "Component": {
      "Type": "git",
      "Git": {
        "RepositoryUrl": "https://github.com/org/repo",
        "CommitHash": "<commit from which the source code was obtained>"
      }
    }
  }
]
```

4. Open the [THIRD-PARTY-NOTICES.txt](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT) file in the root folder of the runtime repo. Add or update the copy of the license of this library, making sure the license copy has a header like this:

```
License notice for <library name>
-----------------------

<link to GitHub commit from which the license was obtained>

```

4. Add or edit the `<library>-version.txt` file under `src/native/external`. This file should contain:
  - The release version of the downloaded release
  - The hash commit of the downloaded release
  - The URL to the tag of the downloaded release in the format `https://github.com/org/repo/releases/tag/<version_number>`
  - An optional section describing any additional information like:
    - Manual modifications we need to make after copying the source code. For example, deleting unnecessary files or trimming unnecessary code.
    - Important notes about the release, like security fixes.

5. Make any cmake changes to properly consume the source code, if needed. The information about these changes should be included in `<library>-version.txt` file. The same changes should be proposed for the library upstream so that they are not necessary during the next library update.

6. Submit a PR tagging the area owners as well as the @dotnet/runtime-infrastructure team.

7. Find ways to get notified about new releases for the external dependency. For example, if the source code comes from a GitHub repo, you can subscribe to new releases:
   - Go to the main org/repo page
   - Click on Watch
   - Click on Custom
   - Select the "Releases" and "Security alerts" checkboxes
   - Click on "Apply"

8. Validate that CG detects the dependency correctly. This step can only be performed by the .NET team.
