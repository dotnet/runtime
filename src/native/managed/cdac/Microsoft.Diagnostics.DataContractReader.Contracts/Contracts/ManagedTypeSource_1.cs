// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class ManagedTypeSource_1 : IManagedTypeSource
{
    private readonly Target _target;
    private readonly Dictionary<string, Target.TypeInfo?> _typeInfoCache = new();
    private readonly Dictionary<string, ITypeHandle> _typeHandleCache = new();
    private readonly Dictionary<(string Fqn, string FieldName), TargetPointer> _fieldDescCache = new();
    private bool _inSearch;

    public ManagedTypeSource_1(Target target)
    {
        _target = target;
    }

    public void Flush(FlushScope scope)
    {
        // RuntimeTypeSystem invalidates its canonical ITypeHandle instances on every
        // flush, so this cache must be cleared even when the underlying CoreLib types
        // remain loaded and immutable.
        _typeHandleCache.Clear();

        // Type layouts and field descriptors are safe to retain across
        // FlushScope.ForwardExecution because ManagedTypeSource_1 only resolves names
        // in System.Private.CoreLib, which is loaded into the non-collectible default
        // AssemblyLoadContext at runtime startup and whose ECMA metadata never changes.
        if (scope != FlushScope.All)
            return;

        _typeInfoCache.Clear();
        _fieldDescCache.Clear();
    }

    public Target.TypeInfo GetTypeInfo(string fullyQualifiedName)
    {
        if (!TryGetTypeInfo(fullyQualifiedName, out Target.TypeInfo info))
            throw new InvalidOperationException($"Managed type '{fullyQualifiedName}' is not resolvable through {nameof(ManagedTypeSource_1)}.");

        return info;
    }

    public bool TryGetTypeInfo(string fullyQualifiedName, out Target.TypeInfo info)
    {
        if (_typeInfoCache.TryGetValue(fullyQualifiedName, out Target.TypeInfo? cached))
        {
            info = cached ?? default;
            return cached.HasValue;
        }

        // Re-entrancy guard: if we're already searching for a type and we recurse
        // (e.g., LayoutSet -> ManagedTypeSource -> IData -> LayoutSet), short-circuit
        // to break the cycle. Do NOT cache the negative result here — the outer search
        // may legitimately succeed for this same name once the recursion unwinds.
        if (_inSearch)
        {
            info = default;
            return false;
        }

        _inSearch = true;
        try
        {
            if (!TryBuildTypeInfo(fullyQualifiedName, out info))
            {
                _typeInfoCache[fullyQualifiedName] = null;
                return false;
            }

            _typeInfoCache[fullyQualifiedName] = info;
            return true;
        }
        finally
        {
            _inSearch = false;
        }
    }

    public ITypeHandle GetTypeHandle(string fullyQualifiedName)
    {
        if (!TryGetTypeHandle(fullyQualifiedName, out ITypeHandle typeHandle))
            throw new InvalidOperationException($"Managed type '{fullyQualifiedName}' is not resolvable through {nameof(ManagedTypeSource_1)}.");

        return typeHandle;
    }

    public bool TryGetTypeHandle(string fullyQualifiedName, out ITypeHandle typeHandle)
    {
        if (_typeHandleCache.TryGetValue(fullyQualifiedName, out var cached))
        {
            typeHandle = cached;
            return !typeHandle.IsNull;
        }

        if (!TryResolveType(fullyQualifiedName, out typeHandle, out _, out _))
        {
            typeHandle = ITypeHandle.Null;
            _typeHandleCache[fullyQualifiedName] = ITypeHandle.Null;
            return false;
        }

        _typeHandleCache[fullyQualifiedName] = typeHandle;
        return true;
    }

    public TargetPointer GetStaticFieldAddress(string fullyQualifiedName, string fieldName)
    {
        if (!TryGetStaticFieldAddress(fullyQualifiedName, fieldName, out TargetPointer address))
            throw new InvalidOperationException($"Static field '{fieldName}' on managed type '{fullyQualifiedName}' is not resolvable through {nameof(ManagedTypeSource_1)}.");

        return address;
    }

    public bool TryGetStaticFieldAddress(string fullyQualifiedName, string fieldName, out TargetPointer address)
    {
        address = TargetPointer.Null;
        if (!TryGetFieldDesc(fullyQualifiedName, fieldName, out TargetPointer fieldDescAddr))
            return false;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

        // Thread-statics return a per-thread offset, not an absolute address — use the
        // dedicated thread-static API for those.
        if (rts.IsFieldDescThreadStatic(fieldDescAddr))
            return false;

        // Gate on the statics base being allocated for the enclosing class so callers cannot
        // dereference a small offset-from-zero when the class has not been initialized.
        TargetPointer enclosingMT = rts.GetMTOfEnclosingClass(fieldDescAddr);
        ITypeHandle ctx = rts.GetTypeHandle(enclosingMT);
        CorElementType type = rts.GetFieldDescType(fieldDescAddr);
        bool isGC = type is CorElementType.Class or CorElementType.ValueType;
        TargetPointer @base = isGC ? rts.GetGCStaticsBasePointer(ctx) : rts.GetNonGCStaticsBasePointer(ctx);
        if (@base == TargetPointer.Null)
            return false;

        address = rts.GetFieldDescStaticAddress(fieldDescAddr);
        return true;
    }

    public TargetPointer GetThreadStaticFieldAddress(string fullyQualifiedName, string fieldName, TargetPointer thread)
    {
        if (!TryGetThreadStaticFieldAddress(fullyQualifiedName, fieldName, thread, out TargetPointer address))
            throw new InvalidOperationException($"Thread-static field '{fieldName}' on managed type '{fullyQualifiedName}' is not resolvable through {nameof(ManagedTypeSource_1)}.");

        return address;
    }

    public bool TryGetThreadStaticFieldAddress(string fullyQualifiedName, string fieldName, TargetPointer thread, out TargetPointer address)
    {
        address = TargetPointer.Null;
        if (!TryGetFieldDesc(fullyQualifiedName, fieldName, out TargetPointer fieldDescAddr))
            return false;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

        // Non-thread-statics have an absolute address — use the dedicated static API.
        if (!rts.IsFieldDescThreadStatic(fieldDescAddr))
            return false;

        // Gate on the per-thread base being allocated for the enclosing class so callers
        // cannot dereference a small offset-from-zero when this thread has not initialized
        // thread-static storage for the type.
        TargetPointer enclosingMT = rts.GetMTOfEnclosingClass(fieldDescAddr);
        ITypeHandle ctx = rts.GetTypeHandle(enclosingMT);
        CorElementType type = rts.GetFieldDescType(fieldDescAddr);
        bool isGC = type is CorElementType.Class or CorElementType.ValueType;
        TargetPointer @base = isGC
            ? rts.GetGCThreadStaticsBasePointer(ctx, thread)
            : rts.GetNonGCThreadStaticsBasePointer(ctx, thread);
        if (@base == TargetPointer.Null)
            return false;

        address = rts.GetFieldDescThreadStaticAddress(fieldDescAddr, thread);
        return true;
    }

    private bool TryGetFieldDesc(string fullyQualifiedName, string fieldName, out TargetPointer fieldDescAddr)
    {
        (string Fqn, string FieldName) key = (fullyQualifiedName, fieldName);
        if (_fieldDescCache.TryGetValue(key, out fieldDescAddr))
            return fieldDescAddr != TargetPointer.Null;

        if (!TryResolveType(fullyQualifiedName, out ITypeHandle th, out _, out _))
        {
            fieldDescAddr = TargetPointer.Null;
            _fieldDescCache[key] = TargetPointer.Null;
            return false;
        }

        fieldDescAddr = _target.Contracts.RuntimeTypeSystem.GetFieldDescByName(th, fieldName);
        _fieldDescCache[key] = fieldDescAddr;
        return fieldDescAddr != TargetPointer.Null;
    }

    private bool TryBuildTypeInfo(string managedFqName, out Target.TypeInfo info)
    {
        info = default;

        if (!TryResolveType(managedFqName, out ITypeHandle th, out MetadataReader? mdReader, out TypeDefinition typeDef))
            return false;

        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

        // For reference types, FieldDesc offsets are relative to the end of the
        // Object portion (after the MT pointer), so we pre-adjust by Object.Size
        // to make offsets relative to the instance address.
        bool isValueType = rts.IsValueType(th);
        ulong objectSize = 0;
        if (!isValueType)
        {
            Target.TypeInfo objType = _target.GetTypeInfo(DataType.Object);
            objectSize = objType.Size
                ?? throw new InvalidOperationException(
                    "The 'Object' data descriptor must have a known Size to compute managed reference-type field offsets.");
        }

        Dictionary<string, Target.FieldInfo> instanceFields = new();

        foreach (FieldDefinitionHandle fieldHandle in typeDef.GetFields())
        {
            FieldDefinition fieldDef = mdReader.GetFieldDefinition(fieldHandle);
            string fieldName = mdReader.GetString(fieldDef.Name);

            if ((fieldDef.Attributes & FieldAttributes.Static) != 0)
                continue;

            TargetPointer fieldDescAddr = rts.GetFieldDescByName(th, fieldName);
            if (fieldDescAddr == TargetPointer.Null)
                continue;

            uint fdOffset = rts.GetFieldDescOffset(fieldDescAddr, fieldDef);
            CorElementType elementType = rts.GetFieldDescType(fieldDescAddr);
            instanceFields[fieldName] = new Target.FieldInfo
            {
                Offset = (int)(fdOffset + objectSize),
                TypeName = MapCorElementTypeToDescriptorName(elementType),
            };
        }

        info = new Target.TypeInfo
        {
            Fields = instanceFields,
        };
        return true;
    }

    private bool TryResolveType(string managedFqName, out ITypeHandle th, [NotNullWhen(true)] out MetadataReader? mdReader, out TypeDefinition typeDef)
    {
        th = ITypeHandle.Null;
        typeDef = default;

        ILoader loader = _target.Contracts.Loader;
        TargetPointer systemAssembly = loader.GetSystemAssembly();
        if (systemAssembly == TargetPointer.Null)
        {
            mdReader = null;
            return false;
        }

        ModuleHandle moduleHandle = loader.GetModuleHandleFromAssemblyPtr(systemAssembly);

        if (!TryFindTypeDefinition(moduleHandle, managedFqName, out mdReader, out TypeDefinitionHandle typeDefHandle))
            return false;

        // Look up the cDAC ITypeHandle via the module's TypeDef → MethodTable map.
        int token = MetadataTokens.GetToken((EntityHandle)typeDefHandle);
        TargetPointer typeDefToMethodTable = loader.GetLookupTables(moduleHandle).TypeDefToMethodTable;
        TargetPointer typeHandlePtr = loader.GetModuleLookupMapElement(typeDefToMethodTable, (uint)token, out _);
        if (typeHandlePtr == TargetPointer.Null)
            return false;

        th = _target.Contracts.RuntimeTypeSystem.GetTypeHandle(typeHandlePtr);
        typeDef = mdReader.GetTypeDefinition(typeDefHandle);
        return true;
    }

    /// <summary>
    /// Walks the metadata of <paramref name="moduleHandle"/> to locate the
    /// <see cref="TypeDefinitionHandle"/> for the supplied fully-qualified type name. Nested
    /// types are encoded with <c>+</c> separators (e.g. <c>Outer+Inner</c>); the outer-most
    /// segment is matched against <c>Namespace + "." + Name</c> on each top-level type, which
    /// avoids any fragility around dots within type or namespace names.
    /// Assembly forwarders are not followed — all managed types resolved through this contract
    /// are expected to live in <c>System.Private.CoreLib</c>.
    /// </summary>
    private bool TryFindTypeDefinition(
        ModuleHandle moduleHandle,
        string fullyQualifiedName,
        [NotNullWhen(true)] out MetadataReader? mdReader,
        out TypeDefinitionHandle typeDefHandle)
    {
        mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle);
        typeDefHandle = default;
        if (mdReader is null)
            return false;

        string[] parts = fullyQualifiedName.Split('+');
        string outerFqn = parts[0];
        TypeDefinitionHandle currentHandle = default;

        foreach (TypeDefinitionHandle handle in mdReader.TypeDefinitions)
        {
            TypeDefinition typedef = mdReader.GetTypeDefinition(handle);
            // Nested types have an empty Namespace in metadata; the enclosing type owns the
            // namespace. Skip them — they are only reachable via GetNestedTypes() below.
            if (typedef.IsNested)
                continue;

            string ns = mdReader.GetString(typedef.Namespace);
            string name = mdReader.GetString(typedef.Name);
            string candidate = ns.Length == 0 ? name : ns + "." + name;
            if (candidate == outerFqn)
            {
                currentHandle = handle;
                break;
            }
        }

        if (currentHandle == default)
            return false;

        // Walk down nested types.
        for (int i = 1; i < parts.Length; i++)
        {
            string nestedName = parts[i];
            bool found = false;
            foreach (TypeDefinitionHandle nestedHandle in mdReader.GetTypeDefinition(currentHandle).GetNestedTypes())
            {
                TypeDefinition nestedDef = mdReader.GetTypeDefinition(nestedHandle);
                if (mdReader.GetString(nestedDef.Name) == nestedName)
                {
                    currentHandle = nestedHandle;
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
        }

        typeDefHandle = currentHandle;
        return true;
    }

    /// <summary>
    /// Maps an ECMA-335 <see cref="CorElementType"/> to a descriptor-type-name string consumed
    /// by <see cref="TargetFieldExtensions"/> debug assertions. Returns null when no precise
    /// mapping applies (the assertions treat null/empty as "skip validation").
    /// </summary>
    private static string? MapCorElementTypeToDescriptorName(CorElementType type) => type switch
    {
        CorElementType.Boolean => "bool",
        CorElementType.I1 => "int8",
        CorElementType.U1 => "uint8",
        CorElementType.Char or CorElementType.U2 => "uint16",
        CorElementType.I2 => "int16",
        CorElementType.I4 => "int32",
        CorElementType.U4 => "uint32",
        CorElementType.I8 => "int64",
        CorElementType.U8 => "uint64",
        CorElementType.I => "nint",
        CorElementType.U => "nuint",
        CorElementType.String
            or CorElementType.Ptr
            or CorElementType.Byref
            or CorElementType.Class
            or CorElementType.Array
            or CorElementType.SzArray
            or CorElementType.GenericInst
            or CorElementType.Object
            or CorElementType.Var
            or CorElementType.MVar
            or CorElementType.FnPtr => "pointer",
        _ => null,
    };
}
