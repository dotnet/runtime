﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Contains the storage and type information for an argument or return value on the native stack.
    /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
    /// </summary>
    [SupportedOSPlatform("browser")]
    [CLSCompliant(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial struct JSMarshalerArgument
    {
#pragma warning disable CS0649 // temporary until we have implementation
        internal JSMarshalerArgumentImpl slot;
#pragma warning restore CS0649

        [StructLayout(LayoutKind.Explicit, Pack = 16, Size = 16)]
        internal struct JSMarshalerArgumentImpl
        {
        }

        /// <summary>
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Initialize()
        {
            throw new NotImplementedException();
        }
    }
}
