Performance Tests
======================

This document contains instructions for building, running, and adding Performance tests. 

Requirements
--------------------

To run performance tests, .NET portable v5.0 is required. This library is included in [the Visual Studio Community 2015 download](https://www.visualstudio.com/products/visual-studio-community-vs). To get the correct packages during installation, follow these steps after opening the installer:
1. Select "Custom Installation" if no installation is present, or "Modify" otherwise
2. Check the "Universal Windows App Development Tools" box under the "Windows and Web Development" menu
3. Install

Running the tests
-----------
Performance test files (if present) are stored within a library's ```tests/Performance``` directory and contain test methods that are all marked with a perf-specific *Benchmark* attribute. The performance tests will only be run if the ```performance``` property is set to ```true```.

To build and run the tests using msbuild for only one project, run ```msbuild /t:BuildAndTest /p:Performance=true``` from the tests directory. If the v5.0 assemblies aren't installed on your system, an error will be raised and no tests will be run.

Performance tests for all libraries can also be run using the build script in ```corefx```: ```./build.cmd /p:Performance=true```
