// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // VariableSymbol
    //
    // VariableSymbol - a symbol representing a variable. Specific subclasses are 
    // used - FieldSymbol for member variables, LocalVariableSymbol for local variables
    // and formal parameters, 
    // ----------------------------------------------------------------------------

    internal class VariableSymbol : Symbol
    {
        protected CType type;                       // CType of the field.
    }
}