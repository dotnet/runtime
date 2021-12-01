// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Win32Resources
{
    /// <summary>
    /// Resource abstraction to allow examination
    /// of a PE file that contains resources.
    /// </summary>
    public unsafe partial class ResourceData
    {
        /// <summary>
        /// Initialize a ResourceData instance from a PE file
        /// </summary>
        /// <param name="ecmaModule"></param>
        public ResourceData(EcmaModule ecmaModule, Func<object, object, ushort, bool> resourceFilter = null)
        {
            System.Collections.Immutable.ImmutableArray<byte> ecmaData = ecmaModule.PEReader.GetEntireImage().GetContent();
            PEReader peFile = ecmaModule.PEReader;

            DirectoryEntry resourceDirectory = peFile.PEHeaders.PEHeader.ResourceTableDirectory;
            if (resourceDirectory.Size != 0)
            {
                BlobReader resourceDataBlob = ecmaModule.PEReader.GetSectionData(resourceDirectory.RelativeVirtualAddress).GetReader(0, resourceDirectory.Size);
                ReadResourceData(resourceDataBlob, peFile, resourceFilter);
            }
        }

        /// <summary>
        /// Find a resource in the resource data
        /// </summary>
        public byte[] FindResource(string name, string type, ushort language)
        {
            return FindResourceInternal(name, type, language);
        }

        /// <summary>
        /// Find a resource in the resource data
        /// </summary>
        public byte[] FindResource(ushort name, string type, ushort language)
        {
            return FindResourceInternal(name, type, language);
        }

        /// <summary>
        /// Find a resource in the resource data
        /// </summary>
        public byte[] FindResource(string name, ushort type, ushort language)
        {
            return FindResourceInternal(name, type, language);
        }

        /// <summary>
        /// Find a resource in the resource data
        /// </summary>
        public byte[] FindResource(ushort name, ushort type, ushort language)
        {
            return FindResourceInternal(name, type, language);
        }

        public bool IsEmpty
        {
            get
            {
                if (_resTypeHeadID.Count > 0)
                    return false;

                if (_resTypeHeadName.Count > 0)
                    return false;

                return true;
            }
        }

        public void WriteResources(ISymbolNode nodeAssociatedWithDataBuilder, ref ObjectDataBuilder dataBuilder)
        {
            Debug.Assert(dataBuilder.CountBytes == 0);

            SortedDictionary<string, List<ObjectDataBuilder.Reservation>> nameTable = new SortedDictionary<string, List<ObjectDataBuilder.Reservation>>();
            Dictionary<ResLanguage, int> dataEntryTable = new Dictionary<ResLanguage, int>();
            List<Tuple<ResType, ObjectDataBuilder.Reservation>> resTypes = new List<Tuple<ResType, ObjectDataBuilder.Reservation>>();
            List<Tuple<ResName, ObjectDataBuilder.Reservation>> resNames = new List<Tuple<ResName, ObjectDataBuilder.Reservation>>();
            List<Tuple<ResLanguage, ObjectDataBuilder.Reservation>> resLanguages = new List<Tuple<ResLanguage, ObjectDataBuilder.Reservation>>();

            IMAGE_RESOURCE_DIRECTORY.Write(ref dataBuilder, checked((ushort)_resTypeHeadName.Count), checked((ushort)_resTypeHeadID.Count));
            foreach (KeyValuePair<string, ResType> res in _resTypeHeadName)
            {
                resTypes.Add(new Tuple<ResType, ObjectDataBuilder.Reservation>(res.Value, IMAGE_RESOURCE_DIRECTORY_ENTRY.Write(ref dataBuilder, res.Key, nameTable)));
            }
            foreach (KeyValuePair<ushort, ResType> res in _resTypeHeadID)
            {
                resTypes.Add(new Tuple<ResType, ObjectDataBuilder.Reservation>(res.Value, IMAGE_RESOURCE_DIRECTORY_ENTRY.Write(ref dataBuilder, res.Key)));
            }

            foreach (Tuple<ResType, ObjectDataBuilder.Reservation> type in resTypes)
            {
                dataBuilder.EmitUInt(type.Item2, (uint)dataBuilder.CountBytes | 0x80000000);
                IMAGE_RESOURCE_DIRECTORY.Write(ref dataBuilder, checked((ushort)type.Item1.NameHeadName.Count), checked((ushort)type.Item1.NameHeadID.Count));

                foreach (KeyValuePair<string, ResName> res in type.Item1.NameHeadName)
                {
                    resNames.Add(new Tuple<ResName, ObjectDataBuilder.Reservation>(res.Value, IMAGE_RESOURCE_DIRECTORY_ENTRY.Write(ref dataBuilder, res.Key, nameTable)));
                }
                foreach (KeyValuePair<ushort, ResName> res in type.Item1.NameHeadID)
                {
                    resNames.Add(new Tuple<ResName, ObjectDataBuilder.Reservation>(res.Value, IMAGE_RESOURCE_DIRECTORY_ENTRY.Write(ref dataBuilder, res.Key)));
                }
            }

            foreach (Tuple<ResName, ObjectDataBuilder.Reservation> type in resNames)
            {
                dataBuilder.EmitUInt(type.Item2, (uint)dataBuilder.CountBytes | 0x80000000);
                IMAGE_RESOURCE_DIRECTORY.Write(ref dataBuilder, 0, checked((ushort)type.Item1.Languages.Count));
                foreach (KeyValuePair<ushort, ResLanguage> res in type.Item1.Languages)
                {
                    resLanguages.Add(new Tuple<ResLanguage, ObjectDataBuilder.Reservation>(res.Value, IMAGE_RESOURCE_DIRECTORY_ENTRY.Write(ref dataBuilder, res.Key)));
                }
            }

            // Emit name table
            dataBuilder.PadAlignment(2); // name table is 2 byte aligned
            foreach (KeyValuePair<string, List<ObjectDataBuilder.Reservation>> name in nameTable)
            {
                foreach (ObjectDataBuilder.Reservation reservation in name.Value)
                {
                    dataBuilder.EmitUInt(reservation, (uint)dataBuilder.CountBytes | 0x80000000);
                }

                dataBuilder.EmitUShort(checked((ushort)name.Key.Length));
                foreach (char c in name.Key)
                {
                    dataBuilder.EmitUShort((ushort)c);
                }
            }

            // Emit byte arrays of resource data, capture the offsets
            foreach (Tuple<ResLanguage, ObjectDataBuilder.Reservation> language in resLanguages)
            {
                dataBuilder.PadAlignment(4); // Data in resource files is 4 byte aligned
                dataEntryTable.Add(language.Item1, dataBuilder.CountBytes);
                dataBuilder.EmitBytes(language.Item1.DataEntry);
            }

            dataBuilder.PadAlignment(4); // resource data entries are 4 byte aligned
            foreach (Tuple<ResLanguage, ObjectDataBuilder.Reservation> language in resLanguages)
            {
                dataBuilder.EmitInt(language.Item2, dataBuilder.CountBytes);
                IMAGE_RESOURCE_DATA_ENTRY.Write(ref dataBuilder, nodeAssociatedWithDataBuilder, dataEntryTable[language.Item1], language.Item1.DataEntry.Length);
            }
            dataBuilder.PadAlignment(4); // resource data entries are 4 byte aligned
        }
    }
}
