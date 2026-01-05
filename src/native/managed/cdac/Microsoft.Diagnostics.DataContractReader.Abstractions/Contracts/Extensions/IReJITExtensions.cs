// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

public static class IReJITExtensions
{
    public static IEnumerable<TargetNUInt> GetRejitIds(this IReJIT rejit, Target target, TargetPointer methodDesc)
    {
        ICodeVersions cv = target.Contracts.CodeVersions;

        IEnumerable<ILCodeVersionHandle> ilCodeVersions = cv.GetILCodeVersions(methodDesc);

        foreach (ILCodeVersionHandle ilCodeVersionHandle in ilCodeVersions)
        {
            if (rejit.GetRejitState(ilCodeVersionHandle) == RejitState.Active)
            {
                yield return rejit.GetRejitId(ilCodeVersionHandle);
            }
        }
    }
}
