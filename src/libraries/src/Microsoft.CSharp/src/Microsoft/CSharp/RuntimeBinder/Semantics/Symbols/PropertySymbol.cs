// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // PropertySymbol
    //
    // PropertySymbol - a symbol representing a property. Parent is a struct, interface
    // or class (aggregate). No children.
    // ----------------------------------------------------------------------------

    internal class PropertySymbol : MethodOrPropertySymbol
    {
        public MethodSymbol GetterMethod { get; set; } // (always has same parent)

        public MethodSymbol SetterMethod { get; set; } // (always has same parent)

        public PropertyInfo AssociatedPropertyInfo { get; set; }

        public bool Bogus { get; set; }
    }
}
