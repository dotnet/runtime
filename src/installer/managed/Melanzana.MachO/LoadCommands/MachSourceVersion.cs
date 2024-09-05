// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    [GenerateReaderWriter]
    public partial class MachSourceVersion : MachLoadCommand
    {
        /// <summary>
        /// A.B.C.D.E packed as a24.b10.c10.d10.e10.
        /// </summary>
        public ulong Version { get; set; }
    }
}
