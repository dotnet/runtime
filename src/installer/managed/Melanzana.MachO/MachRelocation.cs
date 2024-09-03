using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Melanzana.MachO
{
    public class MachRelocation
    {
        /// <summary>
        /// Address from the start of the section.
        /// </summary>
        public int Address { get; init; }

        /// <summary>
        /// Symbol index (for internal symbols) or section index (for external symbols).
        /// </summary>
        public uint SymbolOrSectionIndex { get; init; }

        /// <summary>
        /// Specifies whether the relocation is program counter relative.
        /// </summary>
        public bool IsPCRelative { get; init; }

        /// <summary>
        /// Specifies whether the relocation is external.
        /// </summary>
        public bool IsExternal { get; init; }

        /// <summary>
        /// Length of the relocation in bytes (valid values are 1, 2, 4, and 8).
        /// </summary>
        public byte Length { get; init; }

        /// <summary>
        /// Machine specific relocation type.
        /// </summary>
        public MachRelocationType RelocationType { get; init; }
    }
}
