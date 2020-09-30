// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ILCompiler.Win32Resources
{
    public unsafe partial class ResourceData
    {
        private void ReadResourceData(BlobReader resourceReader, PEReader peFile, Func<object, object, ushort, bool> resourceFilter)
        {
            DoResourceDirectoryRead(resourceReader, 0, ProcessOuterResource);
            return;

            void ProcessOuterResource(object typeName, uint offset, bool isTypeDictionaryEntry)
            {
                if (!isTypeDictionaryEntry)
                    throw new ArgumentException();

                DoResourceDirectoryRead(resourceReader, offset, ProcessNameList);
                return;

                void ProcessNameList(object name, uint offsetOfLanguageList, bool isNameListDictionaryEntry)
                {
                    if (!isNameListDictionaryEntry)
                        throw new ArgumentException();

                    DoResourceDirectoryRead(resourceReader, offsetOfLanguageList, ProcessLanguageList);
                    return;

                    void ProcessLanguageList(object languageName, uint offsetOfLanguageListEntry, bool isLanguageListEntryIsDictionaryEntry)
                    {
                        if (languageName is string)
                            throw new ArgumentException();

                        if (isLanguageListEntryIsDictionaryEntry)
                            throw new ArgumentException();

                        resourceReader.Offset = checked((int)offsetOfLanguageListEntry);
                        IMAGE_RESOURCE_DATA_ENTRY resourceData = new IMAGE_RESOURCE_DATA_ENTRY(ref resourceReader);

                        // The actual resource data offset is relative to the start address of the file
                        BlobReader resourceDataBlob = peFile.GetSectionData(checked((int)resourceData.OffsetToData)).GetReader(0, checked((int)resourceData.Size));
                        byte[] data = resourceDataBlob.ReadBytes((int)resourceData.Size);

                        if (resourceFilter != null)
                        {
                            // If the filter returns false, don't add this resource to the model
                            if (!resourceFilter(typeName, name, (ushort)languageName))
                                return;
                        }
                        AddResource(typeName, name, (ushort)languageName, data);
                    }
                }
            }
        }

        private static void DoResourceDirectoryRead(BlobReader resourceReaderExternal, uint startOffset, Action<object, uint, bool> entry)
        {
            // Create a copy of the Mu, so that we don't allow the delegate to affect its state
            BlobReader resourceReader = resourceReaderExternal;
            resourceReader.Offset = checked((int)startOffset);
            IMAGE_RESOURCE_DIRECTORY directory = new IMAGE_RESOURCE_DIRECTORY(ref resourceReader);
            for (uint i = 0; i < directory.NumberOfNamedEntries + directory.NumberOfIdEntries; i++)
            {
                IMAGE_RESOURCE_DIRECTORY_ENTRY directoryEntry = new IMAGE_RESOURCE_DIRECTORY_ENTRY(ref resourceReader);

                object name;
                if ((directoryEntry.Name & 0x80000000) != 0)
                {
                    int oldPosition = resourceReader.Offset;

                    // This is a named entry, read the string
                    uint nameOffset = directoryEntry.Name & ~0x80000000;

                    resourceReader.Offset = checked((int)nameOffset);
                    ushort stringLen = resourceReader.ReadUInt16();
                    char[] newStringData = new char[stringLen];
                    for (int iStr = 0; iStr < stringLen; iStr++)
                    {
                        newStringData[iStr] = (char)resourceReader.ReadUInt16();
                    }
                    name = new string(newStringData);
                    // And then reset back to read more things.
                    resourceReader.Offset = oldPosition;
                }
                else
                {
                    name = checked((ushort)directoryEntry.Name);
                }

                uint offset = directoryEntry.OffsetToData;
                bool isDirectory = false;
                if ((offset & 0x80000000) != 0)
                {
                    offset &= ~0x80000000;
                    isDirectory = true;
                }

                entry(name, offset, isDirectory);
            }
        }
    }
}
