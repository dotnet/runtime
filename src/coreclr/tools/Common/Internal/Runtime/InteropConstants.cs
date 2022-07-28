// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime
{
    internal static class InteropDataConstants
    {
        /// <summary>
        /// Flag set in RuntimeInteropData if the entry is for type with marshallers
        /// </summary>
        public const int HasMarshallers = 0x1;

        /// <summary>
        /// Flag set in RuntimeInteropData if the entry is for type with invalid interop layout
        /// </summary>
        public const int HasInvalidLayout = 0x2;

        /// <summary>
        /// Shift used to encode field count
        /// </summary>
        public const int FieldCountShift = 2;

        /// <summary>
        /// Flag set in ModuleFixupCell if DllImportSearchPath is specified for the DllImport.
        /// </summary>
        public const uint HasDllImportSearchPath = 0x80000000;
    }
}
