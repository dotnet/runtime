# System.Private.CoreLib for Mono Runtime
This folder contains Mono runtime specific build of System.Private.CoreLib.dll. The actual implementation is located in two folders. The code which has hard dependency on Mono VM implementation can be found under [src](src/) and common code in [shared](/src/libraries/System.Private.CoreLib/src/README.md).

The goal is to have the majority of code located in the shared location of System.Private.CoreLib as that code is used by both Mono and CoreCLR runtimes. The source code can be shared at method level by declaring a type as `partial` and having common part stored in the shared location and Mono specific part here.

## File Naming Convention

For identification, if the implementation is split into multiple files the `.Mono.cs` filename suffix is used. The majority of the files include this suffix as we strive to implement as much functionality in the shared partition.
