// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

public static class ICodeVersionsExtensions
{
    public static NativeCodeVersionHandle GetActiveNativeCodeVersion(this ICodeVersions cv, TargetPointer methodDesc)
    {
        ILCodeVersionHandle ilCodeVersionHandle = cv.GetActiveILCodeVersion(methodDesc);
        return cv.GetActiveNativeCodeVersionForILCodeVersion(methodDesc, ilCodeVersionHandle);
    }
}
