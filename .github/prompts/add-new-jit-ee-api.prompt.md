---
mode: 'agent'
tools: ['githubRepo', 'codebase', 'terminalLastCommand']
description: 'Add a new API to the JIT-VM (aka JIT-EE) interface in the codebase.'
---
Your goal is to add a new JIT-VM API by modifying several C++ and C# files. 
The JIT-VM interface defines the APIs through which the JIT compiler communicates with the runtime (VM).
If the new API signature is not provided, prompt the user for it with `src/coreclr/tools/Common/JitInterface/ThunkGenerator/ThunkInput.txt` file as a reference.

The steps to add the new API signature are given below and use the following API signature as an example:

```
CORINFO_METHOD_HANDLE getUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg);
```

# Steps to add the new API:

1. Update the `ThunkInput.txt` file with the new API definition. Example:

```diff
+CORINFO_METHOD_HANDLE getUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg);
```

Insert the new API definition without removing any existing entries, placing it near similar signatures.

2. Invoke `src/coreclr/tools/Common/JitInterface/ThunkGenerator/gen.sh` script (or `src/coreclr/tools/Common/JitInterface/ThunkGenerator/gen.sh` on Windows) to update auto-generated files.

3. Open `src/coreclr/inc/corinfo.h` and append the new API in the end of `class ICorStaticInfo` class declaration. Example:

```diff
+   virtual CORINFO_METHOD_HANDLE getUnboxedEntry(
+       CORINFO_METHOD_HANDLE ftn,
+       bool*                 requiresInstMethodTableArg
+       ) = 0;
```

4. Open `src/coreclr/tools/Common/JitInterface/CorInfoImpl.cs` and append the new API in the end of `class CorInfoImpl` class declaration. Use `src/coreclr/tools/Common/JitInterface/CorInfoImpl_generated.cs` to inspect how type parameters look like for C# for the newly added API since it is expected to be auto-generated there by the gen.sh(bat) script. Example:

```diff
+    private CORINFO_METHOD_STRUCT_* getUnboxedEntry(CORINFO_METHOD_STRUCT_* ftn, ref bool requiresInstMethodTableArg)
+    {
+        // Hint for the developer: Use CorInfoImpl.RyuJit.cs and CorInfoImpl.ReadyToRun.cs if the implementation
+        // is not shared for NativeAOT and R2R.
+        throw new NotImplementedException();
+    }
```

5. Open `src/coreclr/vm/jitinterface.cpp` and append a dummy implementation at the file's end. Example:

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

6. Now implement the most complex part - SuperPMI. SuperPMI acts as a (de)serializer for JIT-VM queries in order 
to then replay them without the actual VM to speed up jit-diffs and other scenarios. All parameters and return 
values recorded/restored using special primitve types and helpers. We need to update the following files:

* `src/coreclr/tools/superpmi/superpmi-shared/agnostic.h`:
* `src/coreclr/tools/superpmi/superpmi-shared/lwmlist.h`:
* `src/coreclr/tools/superpmi/superpmi-shared/methodcontext.h`:
* `src/coreclr/tools/superpmi/superpmi-shared/methodcontext.cpp`:

Go through each of them one by one.

* `src/coreclr/tools/superpmi/superpmi-shared/agnostic.h`:
Define two `Agnostic_*` types for input arguments and another one for output parameters (return value, output arguments).
 Do not create them if one of the generics ones can be re-used such as `DLD`, `DD`, `DLDL`, etc. Use `DWORD*` 
 like types for integers. Inspect the whole file to see how other APIs are defined.

* `src/coreclr/tools/superpmi/superpmi-shared/lwmlist.h`:
Add a new entry to the `LWM` list. Example:

```diff
+LWM(GetUnboxedEntry, DWORDLONG, DLD);
```

NOTE: Use upper-case for the first letter of the API name here.
Add the new record after the very last LWM one.

* `src/coreclr/tools/superpmi/superpmi-shared/methodcontext.h`:
Define 3 methods in this header file inside `class MethodContext` class (at the end of its definition).

The methods are prefixed with `rec*` (record), `dmp*` (dump to console) and `rep*` (replay). Example

```diff
+   void recGetUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg, CORINFO_METHOD_HANDLE result);
+   void dmpGetUnboxedEntry(DWORDLONG key, DLD value);
+   CORINFO_METHOD_HANDLE repGetUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg);
```
Now append a new element to `enum mcPackets` enum in the same file. Example:

```diff
+   Packet_GetUnboxedEntry = <last value + 1>,
```

* `src/coreclr/tools/superpmi/superpmi-shared/methodcontext.cpp`:
Add the implementation of the 3 methods to `methodcontext.cpp` at the end of it. 
Consider other similar methods in the file for reference. Do not change implementations of other methods in the file. Example:

```diff
+void MethodContext::recGetUnboxedEntry(CORINFO_METHOD_HANDLE ftn,
+                                      bool*                 requiresInstMethodTableArg,
+                                      CORINFO_METHOD_HANDLE result)
+{
+    // Initialize the "input - output" map if it is not already initialized
+    if (GetUnboxedEntry == nullptr)
+    {
+        GetUnboxedEntry = new LightWeightMap<DWORDLONG, DLD>();
+    }
+
+    // Create a key out of the input arguments
+    DWORDLONG key = CastHandle(ftn);
+    DLD       value;
+    value.A = CastHandle(result);
+
+    // Create a value out of the return value and out parameters
+    if (requiresInstMethodTableArg != nullptr)
+    {
+        value.B = (DWORD)*requiresInstMethodTableArg ? 1 : 0;
+    }
+    else
+    {
+        value.B = 0;
+    }
+
+    // Save it to the map
+    GetUnboxedEntry->Add(key, value);
+    DEBUG_REC(dmpGetUnboxedEntry(key, value));
+}
+void MethodContext::dmpGetUnboxedEntry(DWORDLONG key, DLD value)
+{
+   // Dump key and value to the console for debug purposes.
+   printf("GetUnboxedEntry ftn-%016" PRIX64 ", result-%016" PRIX64 ", requires-inst-%u", key, value.A, value.B);
+}
+CORINFO_METHOD_HANDLE MethodContext::repGetUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg)
+{
+   // Create a key out of the input arguments
+   DWORDLONG key = CastHandle(ftn);
+
+   // Perform the lookup to obtain the value (output arguments and return value)
+   DLD value = LookupByKeyOrMiss(GetUnboxedEntry, key, ": key %016" PRIX64 "", key);
+   DEBUG_REP(dmpGetUnboxedEntry(key, value));
+
+   // propagate result to output arguments and return value (if exists)
+   if (requiresInstMethodTableArg != nullptr)
+   {
+       *requiresInstMethodTableArg = (value.B == 1);
+   }
+   return (CORINFO_METHOD_HANDLE)(value.A);
+}
```

7. Add a new function to `src\coreclr\tools\superpmi\superpmi\icorjitinfo.cpp` that calls the `rep*` method. Example:

```diff
+CORINFO_METHOD_HANDLE MyICJI::getUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg)
+{
+   jitInstance->mc->cr->AddCall("getUnboxedEntry");
+   CORINFO_METHOD_HANDLE result = jitInstance->mc->repGetUnboxedEntry(ftn, requiresInstMethodTableArg);
+   return result;
+}
```

8. Add a new function to `src/coreclr/tools/superpmi/superpmi-shim-collector/icorjitinfo.cpp` that calls the `rec*` method. Example:

```diff
+CORINFO_METHOD_HANDLE interceptor_ICJI::getUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg)
+{
+   mc->cr->AddCall("getUnboxedEntry");
+   bool                  localRequiresInstMethodTableArg = false;
+   CORINFO_METHOD_HANDLE result = original_ICorJitInfo->getUnboxedEntry(ftn, &localRequiresInstMethodTableArg);
+   mc->recGetUnboxedEntry(ftn, &localRequiresInstMethodTableArg, result);
+   if (requiresInstMethodTableArg != nullptr)
+   {
+       *requiresInstMethodTableArg = localRequiresInstMethodTableArg;
+   }
+   return result;
+}
```