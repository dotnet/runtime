Using CrossGen to Create Native Images
======================================

Introduction
------------

When you create a .NET assembly using C# compiler, your assembly contains only MSIL code.
When the app runs, the JIT compiler translates the MSIL code into native code, before the CPU can execute them.
This execution model has some advantages. For example, your assembly code can be portable across all platforms and architectures that support .NET Core.
However, this portability comes with a performance penalty. Your app starts up slower, because the JIT compiler has to spend time to translate the code.

To help make your app start up faster, CoreCLR includes a tool called CrossGen, which can pre-compile the MSIL code into native code.

Getting CrossGen
----------------

If you build CoreCLR yourself, the CrossGen tool (`crossgen.exe` on Windows, or `crossgen` on other platforms) is created as part of the build, and stored in the same output directory as other CoreCLR binaries.
If you install CoreCLR using a NuGet package, you can find CrossGen in the `tools` folder of the NuGet package.

Regardless of how you obtain CrossGen, it is very important that it must match other CoreCLR binaries.
- If you build CrossGen yourself, you should use it with coreclr and mscorlib generated from the same build. Do not attempt to mix CrossGen from one build with binaries generated from another build.
- If you install CrossGen from NuGet, make sure you use CrossGen from exactly the same NuGet package as the rest of your CoreCLR binaries. Do not attempt to mix binaries from multiple NuGet packages.

If you do not follow the above rules, you are likely to encounter errors while running CrossGen.

Using CrossGen
--------------

In most cases, the build script automatically runs CrossGen to create the native image for mscorlib.
When this happens, you will find both `mscorlib.dll` and `mscorlib.ni.dll` in your output directory.
`mscorlib.dll` is the MSIL assembly created by the C# compiler, while `mscorlib.ni.dll` is the native image that contains CPU-specific code.
Once the build is done, you only need `mscorlib.ni.dll` to use CoreCLR.
As a matter of fact, most CoreCLR NuGet packages contain only `mscorlib.ni.dll`, without `mscorlib.dll`

If for some reason you did not get `mscorlib.ni.dll` with the rest of your CoreCLR, you can easily create it yourself using CrossGen.
First, make sure you have `crossgen.exe` (on Windows) or `crossgen` (other platforms) in the same directory as `mscorlib.dll`.
Then, run one of the following two commands (first command for Windows, second command for other platforms):

    .\crossgen.exe mscorlib.dll
    ./crossgen mscorlib.dll
    
To create native images for other assemblies, the command line is slightly more complex:

    .\crossgen.exe /Platform_Assemblies_Paths "path1;path2;..." assemblyName.dll
    ./crossgen /Platform_Assemblies_Paths "path1:path2:..." assemblyName.dll
    
The /Platform_Assemblies_Paths is used to specify the location of all the dependencies of the input assembly.
You should use full paths for this locations. Relative paths do not always work.
If there are multiple paths, separate them with semicolons (`;`) on Windows, or colons (`:`) on non-Windows platforms.
It is generally a good idea to enclose the path list in quotes to protect any special characters from the shell.

Using native images
-------------------

Running CrossGen on an assembly creates a "native image" file, with the extension of `.ni.dll` or `.ni.exe`.
You should include the native images in your app, at the same location where you normally install the MSIL assemblies.
Once you have included native images, you do not need to include the original MSIL assemblies in your apps.

Common errors
-------------

The following are some of the command errors while creating or using native images:
- "Could not load file or assembly 'mscorlib.dll' or one of its dependencies. The native image could not be loaded, because it was generated for use by a different version of the runtime. (Exception from HRESULT: 0x80131059)": This error indicates that there is a mismatch between CrossGen and mscorlib.ni.dll. Make sure to use CrossGen and mscorlib.ni.dll from the same build or NuGet package.
- "Error: Could not load file or assembly '...' or one of its dependencies. The system cannot find the file specified. (Exception from HRESULT: 0x80070002)": CrossGen wasn't able to find a particular dependency that it needs. Verify that you have the assembly specified in the error message, and make sure its location is included in `/Platform_Assemblies_Paths`.
- CoreCLR unable to initialize: While there are many possible causes of this error, one possibility is a mismatch between mscorlib.ni.dll and coreclr.dll (or libcoreclr.so). Make sure they come from the same build or NuGet package.
- "Unable to load Jit Compiler": Please get a copy of `clrjit.dll` (or `libclrjit.so` or `libclrjit.dylib`, depending on your platform), and place it in the same directory as CrossGen. You can either build `clrjit.dll` yourself, or get it from `Microsoft.NETCore.Jit` NuGet package. To avoid possible issues, please use `clrjit.dll` from the same build as `crossgen.exe` if possible.
