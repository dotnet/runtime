// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // ParameterModifierType
    //
    // ParameterModifierType - a symbol representing parameter modifier -- either
    // out or ref.
    //
    // ----------------------------------------------------------------------------

    internal sealed class ParameterModifierType : CType
    {
        public ParameterModifierType(CType parameterType, bool isOut)
            : base(TypeKind.TK_ParameterModifierType)
        {
            ParameterType = parameterType;
            IsOut = isOut;
        }

        public bool IsOut { get; }  // True for out parameter, false for ref parameter.

        public CType ParameterType { get; }

        public override Type AssociatedSystemType
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get => ParameterType.AssociatedSystemType.MakeByRefType();
        }

        public override CType BaseOrParameterOrElementType => ParameterType;
    }
}
