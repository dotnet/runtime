// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Mono.HotReload;

internal sealed class InstanceFieldTable
{
    // Q: Does CoreCLR EnC allow adding fields to a valuetype?
    // A: No, see EEClass::AddField - if the type has layout or is a valuetype, you can't add fields to it.

    // See EnCAddedField::Allocate for a description of the CoreCLR version of this.
    //
    // This is substantially the same design, except instead of using dependent handles
    // (ephemerons) directly from native (storing a linked list of ephemerons off the sync block
    // of the instance), we use a ConditionalWeakTable from managed that's keyed on the
    // instances (so the value dies when the instance dies) and whose value is another
    // dictionary, keyed on the fielddef token with values that are storage for the actual field values.
    //
    // for reference types, the storage just stores it as an object.  For valuetypes and
    // primitives, the storage stores the value as a boxed value.
    //
    // The whole thing is basically a ConditionalWeakTable<object,Dictionary<uint,FieldStore>> but
    // with locking on the inner dictionary.
    //

    // This should behave somewhat like EditAndContinueModule::ResolveOrAddField (and EnCAddedField::Allocate)
    //   we want to create some storage space that has the same lifetime as the instance object.

    internal static FieldStore GetInstanceFieldFieldStore(object inst, IntPtr type, uint fielddef_token)
    {
        return _singleton.GetOrCreateInstanceFields(inst).LookupOrAdd(new RuntimeTypeHandle(type), fielddef_token);
    }

    private static readonly InstanceFieldTable _singleton = new();

    private readonly ConditionalWeakTable<object, InstanceFields> _table;

    private InstanceFieldTable()
    {
        _table = new();
    }

    private InstanceFields GetOrCreateInstanceFields(object key)
        => _table.GetOrCreateValue(key);

    private sealed class InstanceFields
    {
        private readonly Dictionary<uint, FieldStore> _fields;
        private readonly object _lock;

        public InstanceFields()
        {
            _fields = new();
            _lock = new();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Hot reload required untrimmed apps")]
        public FieldStore LookupOrAdd(RuntimeTypeHandle type, uint key)
        {
            if (_fields.TryGetValue(key, out FieldStore? v))
                return v;
            lock (_lock)
            {
                if (_fields.TryGetValue(key, out FieldStore? v2))
                    return v2;

                FieldStore s = FieldStore.Create(type);
                _fields.Add(key, s);
                return s;
            }
        }
    }

}

// This is similar to System.Diagnostics.EditAndContinueHelper in CoreCLR, except instead of
// having the allocation logic in native (see EditAndContinueModule::ResolveOrAllocateField,
// and EnCSyncBlockInfo::ResolveOrAllocateField), the logic is in managed.
//
// Additionally Mono uses this for storing added static fields.
[StructLayout(LayoutKind.Sequential)]
internal sealed class FieldStore
{
    // keep in sync with hot_reload-internals.h
    private readonly object? _loc;

    private FieldStore(object? loc)
    {
        _loc = loc;
    }

    [RequiresUnreferencedCode("Hot reload required untrimmed apps")]
    public static FieldStore Create(RuntimeTypeHandle type)
    {
        Type t = Type.GetTypeFromHandle(type) ?? throw new ArgumentException(SR.Arg_InvalidHandle, nameof(type));
        object? loc;
        if (t.IsPrimitive || t.IsValueType)
            loc = RuntimeHelpers.GetUninitializedObject(t);
        else if (t.IsClass || t.IsInterface)
            loc = null;
        else
            throw new ArgumentException(SR.Arg_EnC_ExpectedPrimitive);
        /* FIXME: do we want FieldStore to be pinned? */
        return new FieldStore(loc);
    }
}
