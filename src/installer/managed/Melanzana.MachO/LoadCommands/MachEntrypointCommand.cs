// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Melanzana.MachO.BinaryFormat;

namespace Melanzana.MachO
{
    public class MachEntrypointCommand : MachLoadCommand
    {
        public ulong FileOffset { get; set; }

        public ulong StackSize { get; set; }
    }
}
