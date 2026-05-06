// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    /// <summary>
    /// Describes the types of the IL instructions.
    /// </summary>
    public enum OpCodeType
    {
        [Obsolete("OpCodeType.Annotation has been deprecated and is not supported.")]
        Annotation = 0,
        Macro = 1,
        Nternal = 2,
        Objmodel = 3,
        Prefix = 4,
        Primitive = 5,
    }
}
