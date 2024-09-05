// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Melanzana.MachO
{
    public class MachSymbol
    {
        public string Name { get; init; } = string.Empty;
        public MachSymbolType Type { get; init; }
        public MachSection? Section { get; init; }
        public MachSymbolDescriptor Descriptor { get; init; }
        public ulong Value { get; init; }

        public bool IsExternal => Type.HasFlag(MachSymbolType.External);
        public bool IsUndefined => (Type & MachSymbolType.TypeMask) == MachSymbolType.Undefined;
    }
}
