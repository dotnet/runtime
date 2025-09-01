// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class CodeVersionsFactory : IContractFactory<ICodeVersions>
{
    ICodeVersions IContractFactory<ICodeVersions>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new CodeVersions_1(target),
            _ => default(CodeVersions),
        };
    }
}
