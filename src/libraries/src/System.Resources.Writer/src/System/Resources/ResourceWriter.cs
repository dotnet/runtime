// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:  ResourceWriter
** 
**
**
** Purpose: Default way to write strings to a CLR resource 
** file.
**
** 
===========================================================*/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Resources
{
    // Generates a binary .resources file in the system default format 
    // from name and value pairs.  Create one with a unique file name,
    // call AddResource() at least once, then call Generate() to write
    // the .resources file to disk, then call Dispose() to close the file.
    // 
    // The resources generally aren't written out in the same order 
    // they were added.
    // 
    // See the RuntimeResourceSet overview for details on the system 
    // default file format.
    // 
    public sealed class ResourceWriter : System.IDisposable
    {
        // An initial size for our internal sorted list, to avoid extra resizes.
        private const int AverageNameSize = 20 * 2;  // chars in little endian Unicode
        private const int AverageValueSize = 40;
        private const string ResourceReaderFullyQualifiedName = "System.Resources.ResourceReader, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string ResSetTypeName = "System.Resources.RuntimeResourceSet";
        private const int ResSetVersion = 2;
        private const int ResourceTypeCodeString = 1;
        private const int ResourceManagerMagicNumber = unchecked((int)0xBEEFCACE);
        private const int ResourceManagerHeaderVersionNumber = 1;

        private SortedDictionary<String, String> _resourceList;
        private Stream _output;
        private Dictionary<String, String> _caseInsensitiveDups;

        public ResourceWriter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException(SR.Argument_StreamNotWritable);
            Contract.EndContractBlock();
            _output = stream;
            _resourceList = new SortedDictionary<String, String>(FastResourceComparer.Default);
            _caseInsensitiveDups = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        }

        // Adds a string resource to the list of resources to be written to a file.
        // They aren't written until Generate() is called.
        // 
        public void AddResource(String name, String value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            Contract.EndContractBlock();
            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);

            // Check for duplicate resources whose names vary only by case.
            _caseInsensitiveDups.Add(name, null);
            _resourceList.Add(name, value);
        }

        public void Dispose()
        {
            if (_resourceList != null)
            {
                Generate();
            }
            if (_output != null)
            {
                _output.Dispose();
                GC.SuppressFinalize((object)_output);
            }

            _output = null;
            _caseInsensitiveDups = null;
        }

        // After calling AddResource, Generate() writes out all resources to the 
        // output stream in the system default format.
        // If an exception occurs during object serialization or during IO,
        // the .resources file is closed and deleted, since it is most likely
        // invalid.
        public void Generate()
        {
            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);

            BinaryWriter bw = new BinaryWriter(_output, Encoding.UTF8);

            // Write out the ResourceManager header
            // Write out magic number
            bw.Write(ResourceManagerMagicNumber);

            // Write out ResourceManager header version number
            bw.Write(ResourceManagerHeaderVersionNumber);

            MemoryStream resMgrHeaderBlob = new MemoryStream(240);
            BinaryWriter resMgrHeaderPart = new BinaryWriter(resMgrHeaderBlob);

            // Write out class name of IResourceReader capable of handling 
            // this file.
            resMgrHeaderPart.Write(ResourceReaderFullyQualifiedName);

            // Write out class name of the ResourceSet class best suited to
            // handling this file.
            // This needs to be the same even with multi-targeting. It's the 
            // full name -- not the assembly qualified name.
            resMgrHeaderPart.Write(ResSetTypeName);
            resMgrHeaderPart.Flush();

            // Write number of bytes to skip over to get past ResMgr header
            bw.Write((int)resMgrHeaderBlob.Length);

            // Write the rest of the ResMgr header
            resMgrHeaderBlob.Seek(0, SeekOrigin.Begin);
            resMgrHeaderBlob.CopyTo(bw.BaseStream, (int)resMgrHeaderBlob.Length);
            // End ResourceManager header


            // Write out the RuntimeResourceSet header
            // Version number
            bw.Write(ResSetVersion);

            // number of resources
            int numResources = _resourceList.Count;
            bw.Write(numResources);

            // Store values in temporary streams to write at end of file.
            int[] nameHashes = new int[numResources];
            int[] namePositions = new int[numResources];
            int curNameNumber = 0;
            MemoryStream nameSection = new MemoryStream(numResources * AverageNameSize);
            BinaryWriter names = new BinaryWriter(nameSection, Encoding.Unicode);

            Stream dataSection = new MemoryStream();  // Either a FileStream or a MemoryStream

            using (dataSection)
            {
                BinaryWriter data = new BinaryWriter(dataSection, Encoding.UTF8);

                // We've stored our resources internally in a Hashtable, which 
                // makes no guarantees about the ordering while enumerating.  
                // While we do our own sorting of the resource names based on their
                // hash values, that's only sorting the nameHashes and namePositions
                // arrays.  That's all that is strictly required for correctness,
                // but for ease of generating a patch in the future that 
                // modifies just .resources files, we should re-sort them.


                // Write resource name and position to the file, and the value
                // to our temporary buffer.  Save Type as well.
                foreach (var items in _resourceList)
                {
                    nameHashes[curNameNumber] = FastResourceComparer.HashFunction((String)items.Key);
                    namePositions[curNameNumber++] = (int)names.Seek(0, SeekOrigin.Current);
                    names.Write((String)items.Key); // key
                    names.Write((int)data.Seek(0, SeekOrigin.Current)); // virtual offset of value.

                    String value = items.Value;

                    // Write out type code
                    Write7BitEncodedInt(data, ResourceTypeCodeString);

                    // Write out value
                    data.Write(value);
                }

                // At this point, the ResourceManager header has been written.
                // Finish RuntimeResourceSet header
                // The reader expects a list of user defined type names 
                // following the size of the list, write 0 for this 
                // writer implementation
                bw.Write((int)0);

                // Write out the name-related items for lookup.
                //  Note that the hash array and the namePositions array must
                //  be sorted in parallel.
                Array.Sort(nameHashes, namePositions);


                //  Prepare to write sorted name hashes (alignment fixup)
                //   Note: For 64-bit machines, these MUST be aligned on 8 byte 
                //   boundaries!  Pointers on IA64 must be aligned!  And we'll
                //   run faster on X86 machines too.
                bw.Flush();
                int alignBytes = ((int)bw.BaseStream.Position) & 7;
                if (alignBytes > 0)
                {
                    for (int i = 0; i < 8 - alignBytes; i++)
                        bw.Write("PAD"[i % 3]);
                }

                //  Write out sorted name hashes.
                //   Align to 8 bytes.
                Debug.Assert((bw.BaseStream.Position & 7) == 0, "ResourceWriter: Name hashes array won't be 8 byte aligned!  Ack!");

                foreach (int hash in nameHashes)
                    bw.Write(hash);

                //  Write relative positions of all the names in the file.
                //   Note: this data is 4 byte aligned, occurring immediately 
                //   after the 8 byte aligned name hashes (whose length may 
                //   potentially be odd).
                Debug.Assert((bw.BaseStream.Position & 3) == 0, "ResourceWriter: Name positions array won't be 4 byte aligned!  Ack!");

                foreach (int pos in namePositions)
                    bw.Write(pos);

                // Flush all BinaryWriters to their underlying streams.
                bw.Flush();
                names.Flush();
                data.Flush();

                // Write offset to data section
                int startOfDataSection = (int)(bw.Seek(0, SeekOrigin.Current) + nameSection.Length);
                startOfDataSection += 4;  // We're writing an int to store this data, adding more bytes to the header
                bw.Write(startOfDataSection);

                // Write name section.
                nameSection.Seek(0, SeekOrigin.Begin);
                nameSection.CopyTo(bw.BaseStream, (int)nameSection.Length);
                names.Dispose();

                // Write data section.
                Debug.Assert(startOfDataSection == bw.Seek(0, SeekOrigin.Current), "ResourceWriter::Generate - start of data section is wrong!");
                dataSection.Position = 0;
                dataSection.CopyTo(bw.BaseStream);
                data.Dispose();
            } // using(dataSection)  <--- Closes dataSection, which was opened w/ FileOptions.DeleteOnClose
            bw.Flush();

            // Indicate we've called Generate
            _resourceList = null;
        }

        private static void Write7BitEncodedInt(BinaryWriter store, int value)
        {
            Contract.Requires(store != null);
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                store.Write((byte)(v | 0x80));
                v >>= 7;
            }
            store.Write((byte)v);
        }
    }
}

