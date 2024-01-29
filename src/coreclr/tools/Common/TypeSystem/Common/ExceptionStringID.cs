// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents an ID of a localized exception string.
    /// </summary>
    public enum ExceptionStringID
    {
        // TypeLoadException
        ClassLoadGeneral,
        ClassLoadExplicitGeneric,
        ClassLoadBadFormat,
        ClassLoadExplicitLayout,
        ClassLoadValueClassTooLarge,
        ClassLoadRankTooLarge,

        ClassLoadInlineArrayFieldCount,
        ClassLoadInlineArrayLength,
        ClassLoadInlineArrayExplicit,

        // MissingMethodException
        MissingMethod,

        // MissingFieldException
        MissingField,

        // FileNotFoundException
        FileLoadErrorGeneric,

        // InvalidProgramException
        InvalidProgramDefault,
        InvalidProgramSpecific,
        InvalidProgramVararg,
        InvalidProgramCallVirtFinalize,
        InvalidProgramNonStaticMethod,
        InvalidProgramGenericMethod,
        InvalidProgramNonBlittableTypes,
        InvalidProgramMultipleCallConv,

        // BadImageFormatException
        BadImageFormatGeneric,

        // MarshalDirectiveException
        MarshalDirectiveGeneric,

        // AmbiguousMatchException
        AmbiguousMatchUnsafeAccessor,
    }
}
