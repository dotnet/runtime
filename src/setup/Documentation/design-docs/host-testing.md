# Testing of host components

The hosting layer of .NET Core consists of several native libraries and executables (entirely written in C++). The full end to end features sometimes require code in the `coreclr` repo, but testing of that is outside of the scope of this document. For description of the various hosting components, please see [host-components](host-components.md).

Testing these comes with certain challenges which this document tries to address.

## Existing tests
Almost all tests for the hosting components are currently basically End-to-End tests which execute the entire product (with very limited amount of "mocking").

### End-to-End tests
This is almost all tests in the `HostActivation` test project currently.
The tests prepare folders on the disk which emulate the product installation by having
* The muxer `dotnet` or `apphost`
* The `hostfxr` in the right place
* Shared frameworks (or for self contained apps in the app directory)

For the most part all these are real components. The shared frameworks contain a real `Microsoft.NETCore.App` copy, only higher level frameworks are "mocked" by creating only the necessary files.
Almost all tests then execute a test application using this test product installation and observe the behavior of the app. This is done by
* Having the app output information to standard output
* Turning on host tracing and looking for certain text in the tracing output
* Exit code of the app

Pros:
* True End-to-End tests which test real product binaries in the real-world conditions (mostly)
* Tests are written in C# - so natural integration with test infra, VS UI and so on

Cons:
* All tests are out-of-proc which makes debugging product complicated
* Tests perform large setup work and copy/write lot of files on disk - tests are slow for this reason.
* Tests run the entire product, not just the host. This includes starting the runtime, JITing and so on. Vast majority of tests don't actually use that and effectively ignore the part of running an actual app code - makes the tests slow.

Going forward we would keep these tests and add new ones to provide true End-to-End coverage.

### Native API tests
Small portion of the tests actually call specific exports on `hostfxr` or `hostpolicy` directly to test these. All these tests are in the `HostActivation` project under the `NativeHostApis` test class.
These tests use a special test application (managed) which invokes the selected exports through PInvokes and performs the testing. The `HostActivation` test only prepares this app and executes it, looking for pieces of its output to verify the outcome.
Ideally we would migrate these over time to the proposed Component tests infra.

Pros:
* Testing real product binaries
* Tests are written in C#, but in a separate project - no direct test infra integration or VS UI support. This is worked around by effectively having "wrappers" for these tests.
* Direct calls to exports in a controlled environment without running the whole product (mostly)

Cons:
* Still runs out-of-proc making debugging harder

## Test proposal

### Unit tests
Add the ability to write true unit tests. So tests are written in C++ which can directly call methods/classes from the product. These tests would be compiled as native executables. For simplicity of the build system, they would live in the product source tree (`src` folder) and would be compiled during the product build. They would combine the test source files with product source files into one executable. The intent is to keep them in a separate subdirectory with clear separation from the product, just the build would be used by both.
For integration purposes there would be managed wrappers in the `HostActivation` test project which would just call the native executable and check the exit code. As this is the simplest way to integrate these tests into all our test infrastructure.

Unit tests should be used to test self-contained bits of functionality. So for example helper classes and such.

First unit test is being introduced in this PR: [dotnet/core-setup#4953](https://github.com/dotnet/core-setup/pull/4953).

Pros:
* Can test C++ code directly
* Can create very isolated environment for the tested code
* Debugging is relatively easy since the test run in a standalone native process

Cons:
* Test must be written in C++
* Not testing shipping binaries

### Component tests
Testing larger functionality blocks is probably easier by testing entire components.

These tests would call entry points on the host components directly (so `hostfxr` and `hostpolicy` mostly) and observe the resulting behavior. The tests would **not** try to emulate real product installation. Instead they would rely on mocks.
The core of this proposed test approach is
* The tested component (`hostfxr` for example) is loaded directly into the test process - so it would be running on some toolset version of .NET Core and not the tested version of .NET Core - but that should not matter as all of that should be abstracted out.
* Mock file system access and other OS services which would otherwise require complex setup
* Mock dependent libraries (so for example `coreclr`) as necessary

Product changes would be required to allow for mocking OS services. This would be achieved by having test-only exports/env variables which would turn on the mocking. For example file system mocking would be done by abstracting file system access through and interface and having an export which would let the test provide custom implementation of that interface.
There are two way to do this:
* Test-only build of the product component, which would have additional exports to enable mocking.
  * Pros:
    * Hides all the test related features from the real product
    * Allows for usage of conditional compilation to completely exclude some of this functionality from the product.
  * Cons:
    * Not testing real product binaries
* Add test-only entry-points to the product. Either as new exports on the libraries or by having a "plugin" mechanism (env. variable which specifies a path to a library to load and call some export on).
  * Pros:
    * Allows testing real product binaries
    * Only one compilation
  * Cons:
    * Product binaries have publicly accessible test entry points. These would not be documented as being part of the product, but they would still be accessible.
    * Product binaries always contain all test-related code (no conditional compilation)

Pros:
* Tests are written in C#. This has several large advantages:
  * Easier to write
  * Full integration with test infra
  * VS UI support for running selected tests
  * Mocking file system and other OS services requires relatively complex data structures which are much easier to work with in C# than in C++
  * Mocks can still be written in C# (COM-style interfaces for example) for the most part
* Debugging should be easier as everything runs in-proc (although it does require mixed mode debugging for full experience)

Cons:
* Potentially not testing shipping binaries
* Requires larger testing infrastructure (all the mocks)
