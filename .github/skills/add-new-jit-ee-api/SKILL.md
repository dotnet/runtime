---
name: add-new-jit-ee-api
description: Add a new API to the JIT-VM (aka JIT-EE) interface in the codebase.
---

# JIT-EE Interface extension

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

NOTE: When inserting new entries/methods into any of the files below, their position and ordering does not matter. Any suggested placement (e.g. "at the end" or "near similar signatures") is just a convenient default, not a requirement.

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

6. Now implement the most complex part - SuperPMI. SuperPMI acts as a (de)serializer for JIT-VM queries in order
to then replay them without the actual VM to speed up jit-diffs and other scenarios. All parameters and return
values recorded/restored using special primitve types and helpers. It is written in C#; we need to update the following
files in `<repo_root>/src/coreclr/tools/superpmi/managed/SuperPmi.Shared`:

* `Agnostic.cs`
* `PacketId.cs`
* `MethodContext.Maps.cs`
* `MethodContext.Record.cs`
* `MethodContext.Dump.cs`
* `MethodContext.Replay.cs`

Go through each of them one by one.

* `Agnostic.cs`:
Define two `Agnostic_*` types for input arguments and another one for output parameters (return value, output arguments).
 Do not create them if one of the generics ones can be re-used such as `DLD`, `DD`, `DLDL`, etc. Use `uint`/`ulong`
 for integers and handles. Inspect the whole file to see how other APIs are defined.

* `PacketId.cs`:
Add a new packet ID at the end of `enum PacketId`. Example:

```diff
+    GetUnboxedEntry = <last value + 1>,
```

* `MethodContext.Maps.cs`:
Add the new map at the end of `s_maps`, with the next index, and a property and a dump for it at the end of the other ones. Example:

```diff
+        new MapInfo(<last index + 1>, "GetUnboxedEntry", PacketId.GetUnboxedEntry, static () => new LightWeightMap<ulong, DLD>()),
```

```diff
+    public LightWeightMap<ulong, DLD> GetUnboxedEntry { get => (LightWeightMap<ulong, DLD>)_maps[<index>]; set => _maps[<index>] = value; }
```

```diff
+        MapDumper.Dump(nameof(GetUnboxedEntry), GetUnboxedEntry, (k, v) => dmpGetUnboxedEntry(k, v));
```

NOTE: Use upper-case for the first letter of the API name here.

* `MethodContext.Record.cs`, `MethodContext.Dump.cs` and `MethodContext.Replay.cs`:
Add 3 methods, prefixed with `rec*` (record), `dmp*` (dump to console) and `rep*` (replay), one to each file at its end.
Consider other similar methods in the files for reference. Do not change implementations of other methods. Example:

```diff
+    public void recGetUnboxedEntry(CORINFO_METHOD_STRUCT_* ftn, bool* requiresInstMethodTableArg, CORINFO_METHOD_STRUCT_* result)
+    {
+        // Initialize the "input - output" map if it is not already initialized
+        GetUnboxedEntry ??= new();
+
+        // Create a key out of the input arguments
+        ulong key = CastHandle(ftn);
+
+        // Create a value out of the return value and out parameters
+        DLD value = default;
+        value.A = CastHandle(result);
+        value.B = requiresInstMethodTableArg != null && *requiresInstMethodTableArg ? 1u : 0u;
+
+        // Save it to the map
+        GetUnboxedEntry.Add(key, value);
+    }
```

```diff
+    public void dmpGetUnboxedEntry(ulong key, DLD value)
+        => CFormat.Printf(Out, "GetUnboxedEntry ftn-%016llX, result-%016llX, requires-inst-%u", key, value.A, value.B);
```

```diff
+    public CORINFO_METHOD_STRUCT_* repGetUnboxedEntry(CORINFO_METHOD_STRUCT_* ftn, bool* requiresInstMethodTableArg)
+    {
+        // Create a key out of the input arguments
+        ulong key = CastHandle(ftn);
+
+        // Perform the lookup to obtain the value (output arguments and return value)
+        DLD value = LookupByKeyOrMiss(GetUnboxedEntry, key, ": key %016llX", key);
+
+        // propagate result to output arguments and return value (if exists)
+        if (requiresInstMethodTableArg != null)
+            *requiresInstMethodTableArg = value.B == 1;
+        return (CORINFO_METHOD_STRUCT_*)value.A;
+    }
```

7. Add a new method to `<repo_root>/src/coreclr/tools/superpmi/managed/superpmi/CorInfoImpl.SuperPmi.cs` that calls the `rep*` method.
It has the signature of the API in `CorInfoImpl_generated.cs`. Example:

```diff
+    private CORINFO_METHOD_STRUCT_* getUnboxedEntry(CORINFO_METHOD_STRUCT_* ftn, ref bool requiresInstMethodTableArg)
+    {
+        AddCall("getUnboxedEntry");
+        return mc.repGetUnboxedEntry(ftn, P(ref requiresInstMethodTableArg));
+    }
```

8. Add a new method to `<repo_root>/src/coreclr/tools/superpmi/managed/superpmi-shim-collector/CorInfoImpl.Collector.cs` that calls the
VM (`_original`) and the `rec*` method. Example:

```diff
+    private CORINFO_METHOD_STRUCT_* getUnboxedEntry(CORINFO_METHOD_STRUCT_* ftn, ref bool requiresInstMethodTableArg)
+    {
+        AddCall("getUnboxedEntry");
+        bool localRequiresInstMethodTableArg = false;
+        CORINFO_METHOD_STRUCT_* result = _original.getUnboxedEntry(ftn, ref localRequiresInstMethodTableArg);
+        mc.recGetUnboxedEntry(ftn, &localRequiresInstMethodTableArg, result);
+        if (!Unsafe.IsNullRef(ref requiresInstMethodTableArg))
+            requiresInstMethodTableArg = localRequiresInstMethodTableArg;
+        return result;
+    }
```

#### 4 — Definition of Done (self-check list)

* [ ] New API present in **all** layers.
* [ ] Each source file changed exactly once; no unrelated edits. The following files must be changed:
   * `<repo_root>/src/coreclr/tools/Common/JitInterface/ThunkGenerator/ThunkInput.txt`
   * `<repo_root>/src/coreclr/inc/corinfo.h`
   * `<repo_root>/src/coreclr/tools/Common/JitInterface/CorInfoImpl.cs`
   * `<repo_root>/src/coreclr/vm/jitinterface.cpp`
   * `<repo_root>/src/coreclr/tools/superpmi/managed/SuperPmi.Shared/Agnostic.cs` [optional - only if new types are needed]
   * `<repo_root>/src/coreclr/tools/superpmi/managed/SuperPmi.Shared/PacketId.cs`
   * `<repo_root>/src/coreclr/tools/superpmi/managed/SuperPmi.Shared/MethodContext.Maps.cs`
   * `<repo_root>/src/coreclr/tools/superpmi/managed/SuperPmi.Shared/MethodContext.Record.cs`
   * `<repo_root>/src/coreclr/tools/superpmi/managed/SuperPmi.Shared/MethodContext.Dump.cs`
   * `<repo_root>/src/coreclr/tools/superpmi/managed/SuperPmi.Shared/MethodContext.Replay.cs`
   * `<repo_root>/src/coreclr/tools/superpmi/managed/superpmi/CorInfoImpl.SuperPmi.cs`
   * `<repo_root>/src/coreclr/tools/superpmi/managed/superpmi-shim-collector/CorInfoImpl.Collector.cs`
* [ ] All TODO/UNREACHABLE markers remain for future functional implementation.
