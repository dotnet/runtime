// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /*************************************************************************************************
         This is the base class for this set of EXPRs. When binding a type, the result
         must be a type or a namespace. This EXPR encapsulates that fact. The lhs member is the EXPR 
         tree that was bound to resolve the type or namespace.
     *************************************************************************************************/
    internal abstract class EXPRTYPEORNAMESPACE : EXPR
    {
        public ITypeOrNamespace TypeOrNamespace;
    }
}
