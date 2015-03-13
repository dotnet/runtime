// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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