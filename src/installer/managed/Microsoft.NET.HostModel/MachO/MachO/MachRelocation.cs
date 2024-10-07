// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.NET.HostModel.MachO
{
    internal sealed class MachRelocation
    {
        /// <summary>
        /// Address from the start of the section.
        /// </summary>
        internal int Address { get; init; }

        /// <summary>
        /// Symbol index (for internal symbols) or section index (for external symbols).
        /// </summary>
        internal uint SymbolOrSectionIndex { get; init; }

        /// <summary>
        /// Specifies whether the relocation is program counter relative.
        /// </summary>
        internal bool IsPCRelative { get; init; }

        /// <summary>
        /// Specifies whether the relocation is external.
        /// </summary>
        internal bool IsExternal { get; init; }

        /// <summary>
        /// Length of the relocation in bytes (valid values are 1, 2, 4, and 8).
        /// </summary>
        internal byte Length { get; init; }

        /// <summary>
        /// Machine specific relocation type.
        /// </summary>
        internal MachRelocationType RelocationType { get; init; }
    }
}
