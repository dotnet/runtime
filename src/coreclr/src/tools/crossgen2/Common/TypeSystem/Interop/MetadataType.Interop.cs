// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Internal.TypeSystem
{
    public enum PInvokeStringFormat
    {
        /// <summary>
        /// LPTSTR is interpreted as ANSI in this class.
        /// </summary>
        AnsiClass = 0x00000000,

        /// <summary>
        /// LPTSTR is interpreted as UNICODE.
        /// </summary>
        UnicodeClass = 0x00010000,

        /// <summary>
        /// LPTSTR is interpreted automatically.
        /// </summary>
        AutoClass = 0x00020000,
    }

    public partial class MetadataType
    {
        /// <summary>
        /// Gets a value indicating how strings should be handled for native interop.
        /// </summary>
        public abstract PInvokeStringFormat PInvokeStringFormat
        {
            get;
        }

        /// <summary>
        /// Retrieves the MarshalAsDescriptor of each field of the type
        /// </summary>
        public virtual MarshalAsDescriptor[] GetFieldMarshalAsDescriptors()
        {
            return Array.Empty<MarshalAsDescriptor>();
        }
    }
}
