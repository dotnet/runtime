// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILVerify;
using Internal.IL;

namespace Internal.TypeSystem.Ecma
{
    public partial struct EcmaSignatureParser
    {
        partial void ReportInvalidTypeSpecEncoding(EntityHandle typeSpecHandle) =>
            ThrowBadTypeSpec(typeSpecHandle, "The TypeSpec signature encoding is invalid.");

        partial void ReportTypeSpecUsedAsCustomModifier(EntityHandle typeSpecHandle) =>
            ThrowBadTypeSpec(typeSpecHandle, "A TypeSpec token is not valid as a custom modifier type.");

        private void ThrowBadTypeSpec(EntityHandle typeSpecHandle, string message)
        {
            throw new VerifierException(
                VerifierError.BadTypeSpec,
                $"{message} ([{_ecmaModule}]0x{MetadataTokens.GetToken(typeSpecHandle):X8})");
        }
    }
}
