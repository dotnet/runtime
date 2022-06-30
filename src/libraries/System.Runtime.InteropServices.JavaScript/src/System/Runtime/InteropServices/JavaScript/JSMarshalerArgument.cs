// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents slot of the marshaling stack frame.
    /// It's used by JSImport code generator and should not be used by developers in source code.
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
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Initialize()
        {
            throw new NotImplementedException();
        }
    }
}
