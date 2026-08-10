// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILVerify;
using Internal.IL;

namespace Internal.TypeSystem.Ecma
{
    public partial struct EcmaSignatureParser
    {
        partial void ReportInvalidTypeSpec(EntityHandle typeSpecHandle, InvalidTypeSpecReason reason)
        {
            string message = reason switch
            {
                InvalidTypeSpecReason.InvalidEncoding => "The TypeSpec signature encoding is invalid.",
                InvalidTypeSpecReason.UsedAsCustomModifierType => "A TypeSpec token is not valid as a custom modifier type.",
                _ => throw new ArgumentOutOfRangeException(nameof(reason)),
            };

            throw new VerifierException(
                VerifierError.BadTypeSpec,
                $"{message} ([{_ecmaModule}]0x{MetadataTokens.GetToken(typeSpecHandle):X8})");
        }
    }
}
