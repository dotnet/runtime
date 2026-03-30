# SuperPMI Integration for New JIT-EE APIs

SuperPMI acts as a (de)serializer for JIT-VM queries to replay them without the actual VM (for jit-diffs and other scenarios). All parameters and return values are recorded/restored using special primitive types and helpers.

## Step 6 — SuperPMI shared files

Update these 4 files in order:

### 6a. `src/coreclr/tools/superpmi/superpmi-shared/agnostic.h`

Define `Agnostic_*` types for input arguments and output parameters (return value, output args).
Re-use generic types (`DLD`, `DD`, `DLDL`, etc.) when possible. Use `DWORD*`-like types for integers.
Inspect existing definitions in the file for patterns.

### 6b. `src/coreclr/tools/superpmi/superpmi-shared/lwmlist.h`

Add a new `LWM` entry after the last existing one. **Use upper-case first letter** for the API name:

```diff
+LWM(GetUnboxedEntry, DWORDLONG, DLD);
```

### 6c. `src/coreclr/tools/superpmi/superpmi-shared/methodcontext.h`

Add 3 methods to `class MethodContext` (end of class). The triplet pattern is:
- `rec*` — record input args + return value
- `dmp*` — dump to console for debugging
- `rep*` — replay (look up recorded value)

```diff
+   void recGetUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg, CORINFO_METHOD_HANDLE result);
+   void dmpGetUnboxedEntry(DWORDLONG key, DLD value);
+   CORINFO_METHOD_HANDLE repGetUnboxedEntry(CORINFO_METHOD_HANDLE ftn, bool* requiresInstMethodTableArg);
```

Add a new `Packet_*` to `enum mcPackets`:

```diff
+   Packet_GetUnboxedEntry = <last value + 1>,
```

### 6d. `src/coreclr/tools/superpmi/superpmi-shared/methodcontext.cpp`

Implement the rec/dmp/rep triplet at the end of the file. Follow existing methods as reference.

**rec* pattern** — initialize map, create key from inputs, create value from outputs, add to map:
```cpp
void MethodContext::recGetUnboxedEntry(CORINFO_METHOD_HANDLE ftn,
                                      bool*                 requiresInstMethodTableArg,
                                      CORINFO_METHOD_HANDLE result)
{
    if (GetUnboxedEntry == nullptr)
        GetUnboxedEntry = new LightWeightMap<DWORDLONG, DLD>();

    DWORDLONG key = CastHandle(ftn);
    DLD       value;
    value.A = CastHandle(result);
    value.B = (requiresInstMethodTableArg != nullptr)
        ? (DWORD)(*requiresInstMethodTableArg ? 1 : 0)
        : 0;

    GetUnboxedEntry->Add(key, value);
    DEBUG_REC(dmpGetUnboxedEntry(key, value));
}
```

**dmp* pattern** — printf key and value fields:
```cpp
void MethodContext::dmpGetUnboxedEntry(DWORDLONG key, DLD value)
{
   printf("GetUnboxedEntry ftn-%016" PRIX64 ", result-%016" PRIX64 ", requires-inst-%u",
          key, value.A, value.B);
}
```

**rep* pattern** — look up key, propagate to output args and return value:
```cpp
CORINFO_METHOD_HANDLE MethodContext::repGetUnboxedEntry(CORINFO_METHOD_HANDLE ftn,
                                                        bool* requiresInstMethodTableArg)
{
   DWORDLONG key = CastHandle(ftn);
   DLD value = LookupByKeyOrMiss(GetUnboxedEntry, key, ": key %016" PRIX64 "", key);
   DEBUG_REP(dmpGetUnboxedEntry(key, value));

   if (requiresInstMethodTableArg != nullptr)
       *requiresInstMethodTableArg = (value.B == 1);
   return (CORINFO_METHOD_HANDLE)(value.A);
}
```

## Step 7 — SuperPMI replay shim

Add to `src/coreclr/tools/superpmi/superpmi/icorjitinfo.cpp` — calls the `rep*` method:

```cpp
CORINFO_METHOD_HANDLE MyICJI::getUnboxedEntry(CORINFO_METHOD_HANDLE ftn,
                                               bool* requiresInstMethodTableArg)
{
   jitInstance->mc->cr->AddCall("getUnboxedEntry");
   return jitInstance->mc->repGetUnboxedEntry(ftn, requiresInstMethodTableArg);
}
```

## Step 8 — SuperPMI collector shim

Add to `src/coreclr/tools/superpmi/superpmi-shim-collector/icorjitinfo.cpp` — calls real API then `rec*`:

```cpp
CORINFO_METHOD_HANDLE interceptor_ICJI::getUnboxedEntry(CORINFO_METHOD_HANDLE ftn,
                                                         bool* requiresInstMethodTableArg)
{
   mc->cr->AddCall("getUnboxedEntry");
   bool localRequiresInstMethodTableArg = false;
   CORINFO_METHOD_HANDLE result = original_ICorJitInfo->getUnboxedEntry(ftn, &localRequiresInstMethodTableArg);
   mc->recGetUnboxedEntry(ftn, &localRequiresInstMethodTableArg, result);
   if (requiresInstMethodTableArg != nullptr)
       *requiresInstMethodTableArg = localRequiresInstMethodTableArg;
   return result;
}
```
