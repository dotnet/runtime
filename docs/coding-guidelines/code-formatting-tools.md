# Code Formatting Tools in dotnet/runtime

In this repository, we use various different formatting conventions depending on who owns the code. Additionally, we use different formatting tools depending on if the code is managed C# or VB code or if the code is native C or C++ code.

To help enable an easy workflow and reduce the number of formatting changes requested, this document provides steps to download and enable the various different tools in different development environments to enable a seamless workflow for ensuring that keeping code formatted is a pleasant experience.

## Downloading formatting tools

### C#/VB

C# and VB code in the repository use the built-in Roslyn support for EditorConfig to enable auto-formatting in many IDEs. As a result, no additional tools are required to enable keeping your code formatted.

### C/C++

To download the formatting tools for C++, run the `download-tools.ps1/sh` script in the `eng/formatting` folder. Specifically, run the `download-tools.ps1` script if you're on Windows, or the `download-tools.sh` script otherwise.

This script will download the `clang-format` and `clang-tidy` tools to the `artifacts/tools` folder in your clone of the repository. Only specific parts of the repository use `clang-tidy`, so this document primarily focuses on setting up `clang-format` for C and C++ scenarios.

## Setting up automatic formatting

To make the formatting workflow more seamless for contributors, instructions are included below to help enable "format-on-save" or other forms of automatic formatting in various different IDEs or as a Git pre-commit hook for IDEs that do not support "format-on-save" scenarios.

This section is open to contributions for instructions on how to enable "format-on-save"-like semantics in IDEs and editors not mentioned.

### Visual Studio Code

Enabling a "format-on-save" experience in VSCode is quite easy. Add the following setting to your `.vscode/settings.json` file to enable "format-on-save":

```json
    "editor.formatOnSave": true,
```

The sections below include any additional instructions to configure the experience for different languages.

#### C#/VB

TODO: Omnisharp doesn't install a VSCode "formatter"

#### C/C++

VSCode ships with a different version of clang-format than we use in this repo, so you will need to add a few more settings to get VSCode to run the correct clang-format for "format-on-save".

You can add the following setting on Windows to your `.vscode/settings.json` file to configure clang-format for the repository:

```json
    "C_Cpp.clang_format_path": "./artifacts/tools/clang-format.exe"
```

On non-Windows, you can add the following setting instead:

```json
    "C_Cpp.clang_format_path": "./artifacts/tools/clang-format"
```

### Visual Studio

#### C#/VB

#### C/C++

### Git Hooks
