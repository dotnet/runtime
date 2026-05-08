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

    public static TargetCodePointer GetNativeCodeAnyVersion(this ICodeVersions cv, TargetPointer methodDesc)
    {
        foreach (ILCodeVersionHandle ilCodeVersionHandle in cv.GetILCodeVersions(methodDesc))
        {
            foreach (NativeCodeVersionHandle nativeCodeVersionHandle in cv.GetNativeCodeVersions(methodDesc, ilCodeVersionHandle))
            {
                if (cv.GetNativeCode(nativeCodeVersionHandle) != TargetCodePointer.Null)
                {
                    return cv.GetNativeCode(nativeCodeVersionHandle);
                }
            }
        }
        return TargetCodePointer.Null;
    }

    public static bool HasNativeCodeAnyVersion(this ICodeVersions cv, TargetPointer methodDesc)
        => cv.GetNativeCodeAnyVersion(methodDesc) != TargetCodePointer.Null;
}
