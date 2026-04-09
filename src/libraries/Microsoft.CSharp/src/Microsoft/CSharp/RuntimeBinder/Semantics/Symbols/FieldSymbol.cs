// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // FieldSymbol
    //
    // FieldSymbol - a symbol representing a member variable of a class. Parent
    // is a struct or class.
    //
    // ----------------------------------------------------------------------------

    internal sealed class FieldSymbol : VariableSymbol
    {
        public new bool isStatic;               // Static member?
        public bool isReadOnly;            // Can only be changed from within constructor.
        public bool isEvent;               // This field is the implementation for an event.
        public FieldInfo AssociatedFieldInfo;

        // If fixedAgg is non-null, the ant of the fixed buffer length

        public void SetType(CType pType)
        {
            type = pType;
        }

        public new CType GetType()
        {
            return type;
        }

        public AggregateSymbol getClass() => parent as AggregateSymbol;

        public EventSymbol getEvent()
        {
            Debug.Assert(isEvent);
            return SymbolLoader.LookupAggMember(name, getClass(), symbmask_t.MASK_EventSymbol) as EventSymbol;
        }
    }
}
