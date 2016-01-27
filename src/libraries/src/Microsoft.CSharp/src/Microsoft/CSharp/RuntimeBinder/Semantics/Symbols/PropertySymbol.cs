// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
        public MethodSymbol methGet;            // Getter method (always has same parent)
        public MethodSymbol methSet;            // Setter method (always has same parent)
        public PropertyInfo AssociatedPropertyInfo;

        public bool isIndexer()
        {
            return isOperator;
        }

        public IndexerSymbol AsIndexerSymbol()
        {
            Debug.Assert(isIndexer());
            return (IndexerSymbol)this;
        }
    }
}
