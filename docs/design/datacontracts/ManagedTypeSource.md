# Contract ManagedTypeSource

Resolves the runtime layout of managed CLR types and the addresses of their
static and thread-static fields by fully-qualified name. Consumers use this
contract when an algorithm needs to read a managed type that is defined in the
assemblies (e.g. `System.Threading.Lock`, `System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>+Container`) rather than via a native data descriptor.

All lookups are performed against `System.Private.CoreLib` (the runtime's
system assembly). Assembly forwarders are not followed. Nested types are
addressed by separating the outer and inner type names with `+` (matching
ECMA-335 / `Type.FullName` conventions), e.g.
`System.Runtime.InteropServices.ComWrappers+NativeObjectWrapper`.

## APIs of contract

``` csharp
// Return true and populate `info` with the instance-field layout of the type, or
// false if the type cannot be resolved.
bool TryGetTypeInfo(string fullyQualifiedName, out Target.TypeInfo info);

// Throws InvalidOperationException if the type cannot be resolved.
Target.TypeInfo GetTypeInfo(string fullyQualifiedName);

// Return true and populate `typeHandle` with the runtime ITypeHandle for the type,
// or false if the type cannot be resolved.
bool TryGetTypeHandle(string fullyQualifiedName, out ITypeHandle typeHandle);
ITypeHandle GetTypeHandle(string fullyQualifiedName);

// Return true and populate `address` with the address of the named static field,
// or false if the type / field cannot be resolved or its statics storage has not
// been allocated. Returns false for thread-static fields — use the thread-static
// API for those.
bool TryGetStaticFieldAddress(string fullyQualifiedName, string fieldName, out TargetPointer address);
TargetPointer GetStaticFieldAddress(string fullyQualifiedName, string fieldName);

// Return true and populate `address` with the per-thread address of the named
// thread-static field on the supplied `thread`, or false if the type / field
// cannot be resolved or per-thread storage has not been allocated for the
// enclosing class on this thread. Returns false for non-thread-static fields.
bool TryGetThreadStaticFieldAddress(string fullyQualifiedName, string fieldName, TargetPointer thread, out TargetPointer address);
TargetPointer GetThreadStaticFieldAddress(string fullyQualifiedName, string fieldName, TargetPointer thread);
```

## Version 1

### Data descriptors used

Data descriptors used: none

### Global variables used

Global variables used: none

### Managed types used

The contract does not itself enumerate a fixed set of managed types — the
caller supplies the FQN. Consumers should document the specific managed types
they read in their own `### Managed types used` section.

### Contracts used

| Contract Name |
| --- |
| `Loader` |
| `EcmaMetadata` |
| `RuntimeTypeSystem` |

``` csharp
// Type resolution: parse the fully-qualified name, walk System.Private.CoreLib's
// metadata to locate the TypeDef, then map TypeDef -> MethodTable via the loader.
bool TryResolveType(string managedFqName, out ITypeHandle th, out MetadataReader mdReader, out TypeDefinition typeDef)
{
    ILoader loader = target.Contracts.Loader;
    TargetPointer systemAssembly = loader.GetSystemAssembly();
    ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(systemAssembly);

    mdReader = target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
    if (mdReader == null)
        return false;

    // Split outer+nested segments on '+'. The outer segment is matched against
    // (Namespace + "." + Name); nested segments are matched against the Name of
    // GetNestedTypes() entries.
    if (!TryFindTypeDefinition(mdReader, managedFqName, out TypeDefinitionHandle typeDefHandle))
        return false;

    // Resolve the TypeDef token via the module's TypeDef -> MethodTable map.
    int token = MetadataTokens.GetToken(typeDefHandle);
    TargetPointer typeDefToMT = loader.GetLookupTables(moduleHandle).TypeDefToMethodTable;
    TargetPointer mt = loader.GetModuleLookupMapElement(typeDefToMT, (uint)token, out _);
    if (mt == TargetPointer.Null)
        return false;

    th = target.Contracts.RuntimeTypeSystem.GetTypeHandle(mt);
    typeDef = mdReader.GetTypeDefinition(typeDefHandle);
    return true;
}

bool TryGetTypeHandle(string fqn, out ITypeHandle th)
{
    return TryResolveType(fqn, out th, out _, out _);
}

bool TryGetTypeInfo(string fqn, out Target.TypeInfo info)
{
    if (!TryResolveType(fqn, out ITypeHandle th, out MetadataReader mdReader, out TypeDefinition typeDef))
        return false;

    IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
    Dictionary<string, Target.FieldInfo> fields = new();

    foreach (FieldDefinitionHandle fh in typeDef.GetFields())
    {
        FieldDefinition fd = mdReader.GetFieldDefinition(fh);
        if ((fd.Attributes & FieldAttributes.Static) != 0)
            continue;

        string fieldName = mdReader.GetString(fd.Name);
        TargetPointer fdAddr = rts.GetFieldDescByName(th, fieldName);
        if (fdAddr == TargetPointer.Null)
            continue;

        uint offset = rts.GetFieldDescOffset(fdAddr, fd);
        CorElementType et = rts.GetFieldDescType(fdAddr);
        // Raw field offset relative to the instance data start. Reference-type
        // consumers must add the object header size; value-type consumers (e.g.
        // struct entries embedded in arrays) read directly from the slot.
        fields[fieldName] = new Target.FieldInfo { Offset = (int)offset, TypeName = MapElementType(et) };
    }
    info = new Target.TypeInfo { Fields = fields };
    return true;
}

bool TryGetStaticFieldAddress(string fqn, string fieldName, out TargetPointer address)
{
    address = TargetPointer.Null;
    if (!TryGetFieldDesc(fqn, fieldName, out TargetPointer fdAddr))
        return false;

    IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

    // Thread-statics return a per-thread offset, not an absolute address.
    if (rts.IsFieldDescThreadStatic(fdAddr))
        return false;

    // Gate on the statics base being allocated for the enclosing class so callers
    // cannot dereference a small offset-from-zero when the class has not been
    // initialized.
    TargetPointer enclosingMT = rts.GetMTOfEnclosingClass(fdAddr);
    ITypeHandle ctx = rts.GetTypeHandle(enclosingMT);
    CorElementType et = rts.GetFieldDescType(fdAddr);
    bool isGC = et is CorElementType.Class or CorElementType.ValueType;
    TargetPointer @base = isGC
        ? rts.GetGCStaticsBasePointer(ctx)
        : rts.GetNonGCStaticsBasePointer(ctx);
    if (@base == TargetPointer.Null)
        return false;

    address = rts.GetFieldDescStaticAddress(fdAddr);
    return true;
}

bool TryGetThreadStaticFieldAddress(string fqn, string fieldName, TargetPointer thread, out TargetPointer address)
{
    address = TargetPointer.Null;
    if (!TryGetFieldDesc(fqn, fieldName, out TargetPointer fdAddr))
        return false;

    IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
    if (!rts.IsFieldDescThreadStatic(fdAddr))
        return false;

    // Gate on the per-thread base being allocated for the enclosing class on this
    // thread so callers cannot dereference a small offset-from-zero when this
    // thread has not initialized thread-static storage for the type.
    TargetPointer enclosingMT = rts.GetMTOfEnclosingClass(fdAddr);
    ITypeHandle ctx = rts.GetTypeHandle(enclosingMT);
    CorElementType et = rts.GetFieldDescType(fdAddr);
    bool isGC = et is CorElementType.Class or CorElementType.ValueType;
    TargetPointer @base = isGC
        ? rts.GetGCThreadStaticsBasePointer(ctx, thread)
        : rts.GetNonGCThreadStaticsBasePointer(ctx, thread);
    if (@base == TargetPointer.Null)
        return false;

    address = rts.GetFieldDescThreadStaticAddress(fdAddr, thread);
    return true;
}

bool TryGetFieldDesc(string fqn, string fieldName, out TargetPointer fdAddr)
{
    if (!TryResolveType(fqn, out ITypeHandle th, out _, out _))
    {
        fdAddr = TargetPointer.Null;
        return false;
    }
    fdAddr = target.Contracts.RuntimeTypeSystem.GetFieldDescByName(th, fieldName);
    return fdAddr != TargetPointer.Null;
}
```
