// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // EventSymbol
    //
    // EventSymbol - a symbol representing an event. The symbol points to the AddOn and RemoveOn methods
    // that handle adding and removing delegates to the event. If the event wasn't imported, it
    // also points to the "implementation" of the event -- a field or property symbol that is always
    // private.
    // ----------------------------------------------------------------------------

    internal class EventSymbol : Symbol
    {
        public EventInfo AssociatedEventInfo;

        public new bool isStatic;        // Static member?

        // If this is true then tell the user to call the accessors directly.

        public bool isOverride;

        public CType type;               // Type of the event.

        public MethodSymbol methAdd;            // Adder method (always has same parent)
        public MethodSymbol methRemove;         // Remover method (always has same parent)

        public AggregateDeclaration declaration;       // containing declaration

        public bool IsWindowsRuntimeEvent { get; set; }

        // ----------------------------------------------------------------------------
        // EventSymbol
        // ----------------------------------------------------------------------------

        public AggregateDeclaration containingDeclaration()
        {
            return declaration;
        }
    }
}
