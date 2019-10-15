Using CrossGen to Create Native Images
======================================

Introduction
------------

When you create a .NET assembly using C# compiler, your assembly contains only MSIL code.
When the app runs, the JIT compiler translates the MSIL code into native code, before the CPU can execute them.
This execution model has some advantages. For example, your assembly code can be portable across all platforms and architectures that support .NET Core.
However, this portability comes with a performance cost. Your app starts up more slowly because the JIT compiler has to spend time to translate the code.

To help make your app start up faster, CoreCLR includes a tool called CrossGen, which can pre-compile the MSIL code into native code.

Getting CrossGen
----------------

If you build CoreCLR yourself, the CrossGen tool (`crossgen.exe` on Windows, or `crossgen` on other platforms) is created as part of the build and stored in the same output directory as other CoreCLR binaries.
If you install CoreCLR using a NuGet package, you can find CrossGen in the `tools` folder of the NuGet package.

Regardless of how you obtain CrossGen, it is very important that it must match other CoreCLR binaries.
- If you build CrossGen yourself, you should use it with runtime and `System.Private.CoreLib.dll` generated from the same build. Do not attempt to mix CrossGen from one build with binaries generated from another build.
- If you install CrossGen from NuGet, make sure you use CrossGen from exactly the same NuGet package as the rest of your CoreCLR binaries. Do not attempt to mix binaries from multiple NuGet packages.

If you do not follow the above rules, you are likely to encounter errors while running CrossGen.

Using CrossGen
--------------

In most cases, the build script automatically runs CrossGen to create the native image for `System.Private.CoreLib.dll`.
When this happens, you will find `System.Private.CoreLib.ni.dll` in your output directory.
`System.Private.CoreLib.dll` is the MSIL assemblies created by the C# compiler, while `System.Private.CoreLib.ni.dll` is the native images that contain CPU-specific code.
Once the build is done, you only need `System.Private.CoreLib.ni.dll` to use CoreCLR.
The original MSIL assemblies are no longer needed by the runtime.

If you installed CoreCLR from a NuGet package, `System.Private.CoreLib.ni.dll` is included in the package.

If for some reason you did not get `System.Private.CoreLib.dll` with the rest of your CoreCLR, you can easily create it yourself using CrossGen.

If your `System.Private.CoreLib.dll` and JIT compiler (`clrjit.dll` on Windows or `libclrjit.*` on other platforms) are all in the same directory as CrossGen itself, you can compile `System.Private.CoreLib.dll` with the following commands (first two commands for Windows, next two commands for other platforms):

    .\crossgen.exe System.Private.CoreLib.dll
    ./crossgen System.Private.CoreLib.dll
    
If your files are scattered in different directories, or if you want to create native images for other assemblies, the command line is slightly more complex:

    .\crossgen.exe /JITPath path\clrjit.dll /p "path1;path2;..." path\assemblyName.dll
    ./crossgen -JITPath path/libclrjit.so -p "path1:path2:..." path/assemblyName.dll
    
The /p is used to specify the locations of all the dependencies of the input assembly, including the input assembly itself.
You should use full paths for these locations. Relative paths do not always work.
If there are multiple paths, separate them with semicolons (`;`) on Windows, or colons (`:`) on non-Windows platforms.

Another way to reference assemblies is by using the /r switch, which takes file names instead of paths. This switch can be used multiple times, once for each reference file, since is not a semicolon/colon separated list of file names. Example:

    .\crossgen.exe /JITPath path\clrjit.dll /r "reference1.dll" /r "path\reference2.dll" /r ... path\assemblyName.dll
    ./crossgen -JITPath path/libclrjit.so -r "reference1.dll" -r "path/reference2.dll" /r ... path/assemblyName.dll

It is generally a good idea to enclose the path list or file names in quotes to protect any special characters from the shell.
Note that the /r and /p options cannot be used together.

Using native images
-------------------

Running CrossGen on an assembly creates a "native image" file, with the extension of `.ni.dll` or `.ni.exe`.
You should include the native images in your app, either by replacing the original MSIL assemblies with the native images or by putting the native images next to the MSIL assemblies.
When the native images are present, the CoreCLR runtime will automatically use it instead of the original MSIL assemblies.

Common errors
-------------

The following are some of the command errors while creating or using native images:
- "Error: Could not load file or assembly '...' or one of its dependencies. The system cannot find the file specified. (Exception from HRESULT: 0x80070002)": CrossGen wasn't able to find a particular dependency that it needs. Verify that you have the assembly specified in the error message, and make sure its location is included in `/Platform_Assemblies_Paths`.
- CoreCLR unable to initialize: While there are many possible causes of this error, one possibility is a mismatch between System.Private.CoreLib.ni.dll and coreclr.dll (or libcoreclr.so). Make sure they come from the same build or NuGet package.
- "Unable to load Jit Compiler": Please get a copy of `clrjit.dll` (or `libclrjit.so` or `libclrjit.dylib`, depending on your platform), and place it in the same directory as CrossGen (or use /JITPath option). You can either build `clrjit.dll` yourself or get it from `Microsoft.NETCore.Jit` NuGet package. To avoid possible issues, please use `clrjit.dll` from the same build as `crossgen.exe` if possible.
