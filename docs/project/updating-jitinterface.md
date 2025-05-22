# Updating JitInterface

JitInterface is the binary interface that is used to communicate with the JIT. The bulk of the interface consists of the ICorStaticInfo and ICorDynamicInfo interfaces and enums/structs used by those interfaces.

The JitInterface serves two purposes:
* Standardizes the interface between the runtime and the JIT (potentially allowing mixing and matching JITs and runtimes)
* Allows the JIT to be used elsewhere (outside of the runtime)

There are several components that consume the JIT outside of the runtime. Since those components don't consume the JIT using the header, changes to the JIT have to be ported manually.

The JitInterface is versioned by a GUID. Any change to JitInterface is required to update the JitInterface GUID located in jiteeversionguid.h (look for `JITEEVersionIdentifier`). Not doing so has consequences that are sometimes hard to debug.

[add-new-jit-ee-api.prompt.md](../../.github/prompts/add-new-jit-ee-api.prompt.md) contains a prompt that can be used to add a new JIT-VM API through an agent. Example usage in VSCode:
* Open the Copilot Chat Window
* Type "/add-new-jit-ee-api.prompt" and either hit enter and follow the instructions or provide the API signature directly.
