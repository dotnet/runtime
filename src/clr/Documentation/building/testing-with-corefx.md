Testing with CoreFX
===================

It may be valuable to use CoreFX tests to validate your changes to CoreCLR or mscorlib.

**NOTE:** The `BUILDTOOLS_OVERRIDE_RUNTIME` property no longer works.

**Replace runtime between build.[cmd|sh] and build-tests.[cmd|sh]**

Use the following instructions to test a change to the dotnet/coreclr repo using dotnet/corefx tests.  Refer to the [CoreFx Developer Guide](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md) for information about CoreFx build scripts.

1. Build the CoreClr runtime you wish to test under `<coreclr_root>`
2. Build the CoreFx repo (`build.[cmd|sh]`) under `<corefx_root>`, but don't build tests yet
3. Copy the contents of the CoreCLR binary root you wish to test into the CoreFx runtime folder (`<flavor>` below) created in step #2.  For example:

  `copy <coreclr_root>\bin\Product\Windows_NT.<arch>.<build_type>\* <corefx_root>\bin\runtime\<flavor>`  
  -or-  
  `cp <coreclr_root>/bin/Product/<os>.<arch>.<build_type>/* <corefx_root>/bin/runtime/<flavor>`  
  
4. Run the CoreFx `build-tests.[cmd|sh]` script as described in the Developer Guide.

**CI Script**

[run-corefx-tests.py](https://github.com/dotnet/coreclr/blob/master/tests/scripts/run-corefx-tests.py) will clone dotnet/corefx and run steps 2-4 above automatically.  It is primarily intended to be run by the dotnet/coreclr CI system, but it might provide a useful reference or shortcut for individuals running the tests locally.

