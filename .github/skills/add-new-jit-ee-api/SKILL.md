---
name: add-new-jit-ee-api
description: >
  Add a new API to the JIT-VM (aka JIT-EE) interface in dotnet/runtime.
  USE FOR: extending ICorStaticInfo with new methods, adding JIT-to-runtime
  interface APIs across all layers (corinfo.h, jitinterface.cpp, CorInfoImpl.cs,
  SuperPMI), implementing cross-layer JIT interface changes with ThunkGenerator.
  DO NOT USE FOR: JIT code generation logic, runtime GC or type system changes,
  debugging existing JIT-EE implementations, or modifying compiler internals
  unrelated to the interface contract.
---

# JIT-EE Interface extension

## When to Use This Skill

- Adding a new method to `ICorStaticInfo` / `ICorDynamicInfo` in `corinfo.h`
- Given a C-like signature to integrate across the full JIT-VM stack
- Implementing SuperPMI record/replay for a new JIT-EE API
- Updating `ThunkInput.txt` and running the thunk generator
- Files mentioned: `corinfo.h`, `jitinterface.cpp`, `methodcontext.cpp`, `ThunkInput.txt`

## Stopping Conditions

- ✅ **Stop after step 5** if the user only needs the interface + VM stub (no SuperPMI). Ask before proceeding to step 6.
- ✅ **Stop after step 8** if implementations use `NotImplementedException()` / `UNREACHABLE()`. The scaffolding is complete.
- ❌ **Never skip a layer** — partial implementations break SuperPMI replay. All files in the checklist must be updated.

#### 1 — Goal

Implement **one** new JIT-VM (also known as JIT-EE) API and all supporting glue.
The JIT-VM interface defines the APIs through which the JIT compiler communicates with the runtime (VM).

#### 2 — Required user inputs

Ask the user for a C-like signature of the new API if it's not provided.
Suggest `<repo_root>/src/coreclr/tools/Common/JitInterface/ThunkGenerator/ThunkInput.txt` file as a reference. Example:

```
CORINFO_METHOD_HANDLE getUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg);
```

#### 3 — Implementation steps (must be completed in order)

1. Update the `ThunkInput.txt` file with the new API definition. Example:

```diff
+CORINFO_METHOD_HANDLE getUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg);
```

Insert the new API definition without removing any existing entries, placing it near similar signatures.

2. Invoke `<repo_root>/src/coreclr/tools/Common/JitInterface/ThunkGenerator/gen.sh` script
(or `<repo_root>/src/coreclr/tools/Common/JitInterface/ThunkGenerator/gen.bat` on Windows) to update auto-generated files.
Use the correct directory for the script to run.

3. Open `<repo_root>/src/coreclr/inc/corinfo.h` and add the new API inside `class ICorStaticInfo` class as the last member. Example:

```diff
+   virtual CORINFO_METHOD_HANDLE getUnboxedEntry(
+       CORINFO_METHOD_HANDLE ftn,
+       bool*                 requiresInstMethodTableArg
+       ) = 0;
```

4. Open `<repo_root>/src/coreclr/tools/Common/JitInterface/CorInfoImpl.cs` and add the new API in the end of `class CorInfoImpl` class declaration. Use `<repo_root>/src/coreclr/tools/Common/JitInterface/CorInfoImpl_generated.cs` to inspect how type parameters look like for C# for the newly added API since it is expected to be auto-generated there by the gen.sh(bat) script. Example:

```diff
+    private CORINFO_METHOD_STRUCT_* getUnboxedEntry(CORINFO_METHOD_STRUCT_* ftn, ref bool requiresInstMethodTableArg)
+    {
+        // Use CorInfoImpl.RyuJit.cs and CorInfoImpl.ReadyToRun.cs if the implementation
+        // is not shared for NativeAOT and R2R.
+        throw new NotImplementedException();
+    }
```

Implement the API if asked, leave the NotImplementedException() otherwise.

5. Open `<repo_root>/src/coreclr/vm/jitinterface.cpp` and add a dummy implementation at the file's end. Example:

```diff
+CORINFO_METHOD_HANDLE CEEInfo::getUnboxedEntry(
+   CORINFO_METHOD_HANDLE ftn,
+   bool* requiresInstMethodTableArg)
+{
+    CONTRACTL {
+        THROWS;
+        GC_TRIGGERS;
+        MODE_PREEMPTIVE;
+    } CONTRACTL_END;
+
+    CORINFO_METHOD_HANDLE result = NULL;
+
+   JIT_TO_EE_TRANSITION();
+
+   UNREACHABLE(); // To be implemented
+
+   EE_TO_JIT_TRANSITION();
+
+   return result;
+}
```

Implement the API if asked, leave the UNREACHABLE() otherwise.

6. **SuperPMI integration** — the most complex part. SuperPMI records and replays JIT-VM queries for jit-diffs.
You need to update 4 shared files (`agnostic.h`, `lwmlist.h`, `methodcontext.h`, `methodcontext.cpp`) plus 2 shim files.
Each new API needs a `rec*`/`dmp*`/`rep*` method triplet and a `Packet_*` enum entry.
See `references/superpmi-integration.md` for the full file-by-file walkthrough and code templates.

7. Add a replay function to `<repo_root>/src/coreclr/tools/superpmi/superpmi/icorjitinfo.cpp` that calls the `rep*` method. See `references/superpmi-integration.md` Step 7.

8. Add a collector function to `<repo_root>/src/coreclr/tools/superpmi/superpmi-shim-collector/icorjitinfo.cpp` that calls the real API then `rec*`. See `references/superpmi-integration.md` Step 8.

#### 4 — Definition of Done (self-check list)

* [ ] New API present in **all** layers.
* [ ] Each source file changed exactly once; no unrelated edits. The following files must be changed:
   * `<repo_root>/src/coreclr/tools/Common/JitInterface/ThunkGenerator/ThunkInput.txt`
   * `<repo_root>/src/coreclr/inc/corinfo.h`
   * `<repo_root>/src/coreclr/tools/Common/JitInterface/CorInfoImpl.cs`
   * `<repo_root>/src/coreclr/vm/jitinterface.cpp`
   * `<repo_root>/src/coreclr/tools/superpmi/superpmi-shared/agnostic.h` [optional - only if new types are needed]
   * `<repo_root>/src/coreclr/tools/superpmi/superpmi-shared/lwmlist.h`
   * `<repo_root>/src/coreclr/tools/superpmi/superpmi-shared/methodcontext.h`
   * `<repo_root>/src/coreclr/tools/superpmi/superpmi-shared/methodcontext.cpp`
   * `<repo_root>/src/coreclr/tools/superpmi/superpmi/icorjitinfo.cpp`
   * `<repo_root>/src/coreclr/tools/superpmi/superpmi-shim-collector/icorjitinfo.cpp`
* [ ] All TODO/UNREACHABLE markers remain for future functional implementation.
