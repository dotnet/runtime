
# Editing and Debugging

If you are editing on the Windows Operating system, Using Visual Studio 2015 is a good option for editing
the code in this repository.    You can of course also use the editor of your choice.   One further option
is to use [Visual Studio Code](https://code.visualstudio.com/) which is a light weight, cross-platform tool that like
Visual Studio, is optimized for development workflow (code editing and debugging) but works on more platforms
(in particular OSX and Linux)

[Visual Studio Code](https://code.visualstudio.com/) has built-in support for syntax highlighting and previewing
markdown (`*.md`) files that GIT repositories like this one use for documentation.   If you want to modify
the docs, Visual Studio Code is a good choice.  See [Markdown and Visual Studio Code](https://code.visualstudio.com/Docs/languages/markdown)
for more on Visual Studio Code support and [Mastering Markdown](https://guides.github.com/features/mastering-markdown/) for
more on Markdown in general.

# Visual Studio Solutions

The repository has a number of Visual Studio Solutions files (`*.sln`) that are useful for editing parts of
what are in the repository.   In particular

   * `src\System.Private.CoreLib\System.Private.CorLib.sln` - This solution is for all managed (C#) code that is defined
   in the runtime itself.   This is all class library support of one form or another.
   * `bin\obj\Windows_NT.<Arch>.<BuildType>\CoreCLR.sln` - this solution contains most native (C++) projects
   associated with the repository, including
     * `coreclr` - This is the main runtime DLL (the GC, class loader, interop are all here)
     * `corjit` - This is the Just In Time (JIT) compiler that compiles .NET Intermediate language to native code.
     * `corerun` - This is the simple host program that can load the CLR and run a .NET Core application
     * `crossgen` - This is the host program that runs the JIT compiler and produces .NET Native images (`*.ni.dll`)
     for C# code.

Thus opening one of these two solution files (double clicking on them in Explorer) is typically all you need
to do most editing.

Notice that the CoreCLR solution is under the 'bin' directory.  This is because it is created as part of the build.
Thus you can only launch this solution after you have built at least once.

* See [Debugging](../building/debugging-instructions.md)

### Interacting with GIT in Visual Studio

Most interactions with GIT can be done from within Visual Studio.  See the following for more details.
* [Setting up with a fork with Visual Studio 2015](https://github.com/Microsoft/perfview/blob/master/documentation/OpenSourceGitWorkflow.md)

# See Also

Before you make modifications, you probably want to learn more about the general architecture of .NET Core.
See the following docs for more.

 * [Documentation on the .NET Core Runtime](../README.md)
