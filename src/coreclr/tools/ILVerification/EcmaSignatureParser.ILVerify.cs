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
        partial void ReportInvalidTypeSpec(EntityHandle typeSpecHandle)
        {
            throw new VerifierException(
                VerifierError.BadTypeSpec,
                $"TypeSpec is not valid in this signature context. ([{_ecmaModule}]0x{MetadataTokens.GetToken(typeSpecHandle):X8})");
        }
    }
}
