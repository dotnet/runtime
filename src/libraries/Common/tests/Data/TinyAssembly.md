# TinyAssembly fixture

`TinyAssembly.il` defines the unsigned `TinyAssembly, Version=0.0.0.0` assembly,
the `TinyModule.dll` module, and the empty public `TinyAssembly.TinyInterface`
interface. Both consuming test projects import `TinyAssembly.targets` to assemble
this source into their own intermediate directories instead of using a prebuilt
DLL.

Native ILAsm assigns a timestamp and MVID on each invocation. The
`TinyAssemblyMetadata_inbuild` host tool locates these fields through the PE
headers and CLI metadata, validates their framing, and sets only:

- The MVID `06BB2468-908C-48CF-ADE9-DB6DE4614004`, required by
  `ModuleTests.GetModuleVersionId_KnownAssembly_ReturnsExpected`.
- The PE/COFF `TimeDateStamp` `0x5931C36A`, retained from the fixture's PE header.

The normalized output must have SHA-256
`1F76A2E47DDEEBDC51E59DEE277C54A024D81F561F19CC8B9DFFC889175CDF17`
before it is moved into place. This fingerprint is validation only; neither the
IL compiler nor the metadata tool reads a reference DLL or uses the fingerprint
to construct the image. A toolchain that changes other bytes fails this check.
The same check also runs on incremental builds.

The ILAsm package is selected for the SDK host RID, independently of the test
target. `ILAsmToolPath` can select an already-built native host ILAsm, as with
other IL projects. The metadata tool builds for AnyCPU against the SDK
framework, not the test runtime, including when the tests target another
architecture or compilation mode. Generation explicitly builds this private
helper even in IDE builds or when project-reference building is disabled.
Design-time builds retain the generated content and resource paths but do not
build the helper, run ILAsm, or verify an output that may not exist yet.

From the repository root in `cmd.exe`, with the normal library test prerequisites:

```cmd
dotnet.cmd build src\libraries\System.Reflection.TypeExtensions\tests\System.Reflection.TypeExtensions.Tests.csproj -t:Test
dotnet.cmd build src\libraries\System.Runtime\tests\System.Reflection.Tests\CoreCLR\System.Reflection.CoreCLR.Tests.csproj -t:Test
```
