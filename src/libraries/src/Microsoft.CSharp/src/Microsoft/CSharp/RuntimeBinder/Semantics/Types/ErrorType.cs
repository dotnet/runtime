// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // ErrorType
    //
    // ErrorType - a symbol representing an error that has been reported.
    // ----------------------------------------------------------------------------

    internal sealed class ErrorType : CType
    {
        public Name nameText;
        public TypeArray typeArgs;

        // ErrorTypes are always either the per-TypeManager singleton ErrorType
        // that has a null nameText and no namespace parent, or else have a
        // non-null nameText and have the root namespace as the namespace parent,
        // so checking that nameText isn't null is equivalent to checking if the
        // type has a parent.
        public bool HasParent => nameText != null;
    }
}
