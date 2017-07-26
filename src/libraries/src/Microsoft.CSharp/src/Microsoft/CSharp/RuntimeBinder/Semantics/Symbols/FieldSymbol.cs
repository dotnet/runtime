// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal class FieldSymbol : VariableSymbol
    {
        public new bool isStatic;               // Static member?
        public bool isReadOnly;            // Can only be changed from within constructor.
        public bool isEvent;               // This field is the implementation for an event.

        public bool isAssigned;              // Has this ever been assigned by the user?
        // Set if the field's ibit (for definite assignment checking) varies depending on the generic
        // instantiation of the containing type. For example:
        //    struct S<T> { T x; int y; }
        // The ibit value for y depends on what T is bound to. For S<Point>, y's ibit is 2. For S<int>, y's
        // ibit is 1. This flag is set the first time a calculated ibit for the member is found to not
        // match the return result of GetIbitInst().
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

        public EventSymbol getEvent(SymbolLoader symbolLoader)
        {
            Debug.Assert(isEvent);
            return symbolLoader.LookupAggMember(name, getClass(), symbmask_t.MASK_EventSymbol) as EventSymbol;
        }
    }
}
