// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.PortableExecutable
{
    public sealed class CoffHeader
    {
        /// <summary>
        /// The type of target machine.
        /// </summary>
        public Machine Machine { get; private set; }

        /// <summary>
        /// The number of sections. This indicates the size of the section table, which immediately follows the headers.
        /// </summary>
        public short NumberOfSections { get; private set; }

        /// <summary>
        /// The low 32 bits of the number of seconds since 00:00 January 1, 1970, that indicates when the file was created.
        /// </summary>
        public int TimeDateStamp { get; private set; }

        /// <summary>
        /// The file pointer to the COFF symbol table, or zero if no COFF symbol table is present. 
        /// This value should be zero for a PE image.
        /// </summary>
        public int PointerToSymbolTable { get; private set; }

        /// <summary>
        /// The number of entries in the symbol table. This data can be used to locate the string table, 
        /// which immediately follows the symbol table. This value should be zero for a PE image.
        /// </summary>
        public int NumberOfSymbols { get; private set; }

        /// <summary>
        /// The size of the optional header, which is required for executable files but not for object files. 
        /// This value should be zero for an object file. 
        /// </summary>
        public short SizeOfOptionalHeader { get; private set; }

        /// <summary>
        /// The flags that indicate the attributes of the file. 
        /// </summary>
        public Characteristics Characteristics { get; private set; }

        internal CoffHeader(ref PEBinaryReader reader)
        {
            Machine = (Machine)reader.ReadUInt16();
            NumberOfSections = reader.ReadInt16();
            TimeDateStamp = reader.ReadInt32();
            PointerToSymbolTable = reader.ReadInt32();
            NumberOfSymbols = reader.ReadInt32();
            SizeOfOptionalHeader = reader.ReadInt16();
            Characteristics = (Characteristics)reader.ReadUInt16();
        }
    }
}