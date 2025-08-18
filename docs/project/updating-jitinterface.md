# Updating JitInterface

JitInterface is the binary interface that is used to communicate with the JIT. The bulk of the interface consists of the ICorStaticInfo and ICorDynamicInfo interfaces and enums/structs used by those interfaces.

The JitInterface serves two purposes:
* Standardizes the interface between the runtime and the JIT (potentially allowing mixing and matching JITs and runtimes)
* Allows the JIT to be used elsewhere (outside of the runtime)

There are several components that consume the JIT outside of the runtime. Since those components don't consume the JIT using the header, changes to the JIT have to be ported manually.

The JitInterface is versioned by a GUID. Any change to JitInterface is required to update the JitInterface GUID located in jiteeversionguid.h (look for `JITEEVersionIdentifier`). Not doing so has consequences that are sometimes hard to debug.

## Adding a new JIT-VM API manually

It's a good idea to choose an existing API that is similar to the one you want to add and use it as a template. The following steps are required to add a new JIT-VM API:

1) Start from adding a new entry in the `ThunkInput.txt` file. This file is used to generate the JIT-VM interface and is located in `src/coreclr/tools/Common/JitInterface/ThunkGenerator/`. For complex types, you may need to also configure type mapping in the beginning of the file.
2) Invoke the `gen.sh` script (or `gen.bat` on Windows) to update the auto-generated files `*_generated.*` and update the JIT-EE guid.
3) Open `src/coreclr/inc/corinfo.h` and add the new API in `ICorStaticInfo`
4) Open `src/coreclr/tools/Common/JitInterface/CorInfoImpl.cs` and add the new API in `CorInfoImpl` class. If the implementation is not shared for NativeAOT and R2R, use `CorInfoImpl.RyuJit.cs` and `CorInfoImpl.ReadyToRun.cs` to implement the API.
5) Open `src/coreclr/vm/jitinterface.cpp` and add the CoreCLR-specific implementation
6) Open `lwmlist.h` and add a definition of "input-args" - "output-args" map. Either use the generic `DLD`-like structs or create new ones in `agnostic.h`
7) Open `src/coreclr/tools/superpmi/superpmi-shared/methodcontext.h` and add the necessary recording, dumping, and replaying methods for the new API and then implement them in `methodcontext.cpp`
8) Update `enum mcPackets` in `src/coreclr/tools/superpmi/superpmi-shared/methodcontext.h` to include an entry for the new API and bump the max value of the enum
9) Use the `rec*` and `rep*` methods in `src/coreclr/tools/superpmi/superpmi/icorjitinfo.cpp` and `src/coreclr/tools/superpmi/superpmi-shim-collector/icorjitinfo.cpp` accordingly

## Adding a new JIT-VM API through an agent

[add-new-jit-ee-api.prompt.md](../../.github/prompts/add-new-jit-ee-api.prompt.md) contains a prompt that can be used to add a new JIT-VM API through an agent. Example usage in VSCode:
* Open the Copilot Chat Window
* Type "/add-new-jit-ee-api.prompt" and either hit enter and follow the instructions or provide the API signature directly. Gpt-4.1 and Claude Sonnet 4 or 3.7 are recommended for this task.
