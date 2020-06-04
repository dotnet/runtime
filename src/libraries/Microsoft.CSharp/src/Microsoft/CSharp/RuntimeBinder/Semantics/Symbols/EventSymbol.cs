// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal sealed class EventSymbol : Symbol
    {
        public EventInfo AssociatedEventInfo;

        public new bool isStatic;        // Static member?

        // If this is true then tell the user to call the accessors directly.

        public bool isOverride;

        public CType type;               // Type of the event.

        public MethodSymbol methAdd;            // Adder method (always has same parent)
        public MethodSymbol methRemove;         // Remover method (always has same parent)

        public bool IsWindowsRuntimeEvent { get; set; }
    }
}
