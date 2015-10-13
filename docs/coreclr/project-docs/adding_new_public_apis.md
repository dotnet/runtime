Adding new public APIs to mscorlib
==================================

Many of the CoreFX libraries type-forward their public APIs to the implementations in mscorlib.
- The CoreFX build uses published contracts for mscorlib, and the CoreFX test build uses published contracts for some of the CoreFX libraries.
- Some of the CoreFX libraries are not built in the CoreFX repository. For example, System.Runtime.Loader.dll is purely a facade and type-forwards everything to mscorlib. These libraries are built and published through a separate process.
- Hence, when adding a new public API to mscorlib, changes must be staged to ensure that new prerequisites are published before they are used.

**Staging the changes**

Make the changes to CoreCLR, including mscorlib
- Update `coreclr/src/mscorlib/model.xml` with the new APIs. APIs that are not listed in this file will be stripped out prior to publishing.
- Note that at the moment, merging changes with new public APIs will cause an internal build failure. Please work with your PR reviewer to have these build breaks be fixed soon after merging, otherwise it will block the publishing process.
- Merge the changes
- Wait for a new mscorlib contract to be published. Check the latest published version [here](http://myget.org/gallery/dotnet-core).

Make the changes to CoreFX consuming the new APIs in mscorlib
- If the changes are to libraries that are built out of the CoreFX repository:
  - You will likely see a build failure until a new mscorlib contract is published
- If the changes are to libraries that are **not** built out of the CoreFX repository:
  - For example, pure facades such as System.Runtime.Loader.dll
  - There will likely not be a build failure
  - But you will still need to wait for the new mscorlib contract to be published before merging the change, otherwise, facade generation will fail
- Merge the changes
- Wait for new contracts to be published for libraries with new APIs. Check the latest published versions [here](http://myget.org/gallery/dotnet-core).

Add tests
- You should now be able to consume the new APIs and add tests to the CoreFX test suite
  - Until new contracts are published, you will likely see a build failure indicating that the new APIs don't exist.
- Note that on Windows, CoreFX tests currently use a potentially old published build of CoreCLR
  - You may need to disable the new tests on Windows until CoreFX tests are updated to use a newer build of CoreCLR.
