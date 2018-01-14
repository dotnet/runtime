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
using System.Runtime.Serialization;
using System.Diagnostics;

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
    public sealed class ResourceWriter : IResourceWriter
    {
        // An initial size for our internal sorted list, to avoid extra resizes.
        private const int AverageNameSize = 20 * 2;  // chars in little endian Unicode
        private const int AverageValueSize = 40;
        private const string ResourceReaderFullyQualifiedName = "System.Resources.ResourceReader, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        private const string ResSetTypeName = "System.Resources.RuntimeResourceSet";
        private const int ResSetVersion = 2;

        private SortedDictionary<string, object> _resourceList;
        private Stream _output;
        private Dictionary<string, object> _caseInsensitiveDups;
        private Dictionary<string, PrecannedResource> _preserializedData;

        // Set this delegate to allow multi-targeting for .resources files.
        public Func<Type, string> TypeNameConverter { get; set; }

        public ResourceWriter(string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            _output = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            _resourceList = new SortedDictionary<string, object>(FastResourceComparer.Default);
            _caseInsensitiveDups = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public ResourceWriter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException(SR.Argument_StreamNotWritable);

            _output = stream;
            _resourceList = new SortedDictionary<string, object>(FastResourceComparer.Default);
            _caseInsensitiveDups = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        // Adds a string resource to the list of resources to be written to a file.
        // They aren't written until Generate() is called.
        // 
        public void AddResource(string name, string value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);

            // Check for duplicate resources whose names vary only by case.
            _caseInsensitiveDups.Add(name, null);
            _resourceList.Add(name, value);
        }

        // Adds a resource of type Object to the list of resources to be 
        // written to a file.  They aren't written until Generate() is called.
        // 
        public void AddResource(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);
 
            // needed for binary compat
            if (value != null && value is Stream)
            {
                AddResourceInternal(name, (Stream)value, false);
            }
            else
            {
                // Check for duplicate resources whose names vary only by case.
                _caseInsensitiveDups.Add(name, null);
                _resourceList.Add(name, value);
            }
        }

        // Adds a resource of type Stream to the list of resources to be 
        // written to a file.  They aren't written until Generate() is called.
        // Doesn't close the Stream when done.
        //
        public void AddResource(string name, Stream value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);
 
            AddResourceInternal(name, value, false);
        }
 
        // Adds a resource of type Stream to the list of resources to be 
        // written to a file.  They aren't written until Generate() is called.
        // closeAfterWrite parameter indicates whether to close the stream when done.
        // 
        public void AddResource(string name, Stream value, bool closeAfterWrite)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);
 
            AddResourceInternal(name, value, closeAfterWrite);
        }
 
        private void AddResourceInternal(string name, Stream value, bool closeAfterWrite)
        {
            if (value == null)
            {
                // Check for duplicate resources whose names vary only by case.
                _caseInsensitiveDups.Add(name, null);
                _resourceList.Add(name, value);
            }
            else
            {
                // make sure the Stream is seekable
                if (!value.CanSeek)
                    throw new ArgumentException(SR.NotSupported_UnseekableStream);
 
                // Check for duplicate resources whose names vary only by case.
                _caseInsensitiveDups.Add(name, null);
                _resourceList.Add(name, new StreamWrapper(value, closeAfterWrite));
            }
        }

        // Adds a named byte array as a resource to the list of resources to 
        // be written to a file. They aren't written until Generate() is called.
        // 
        public void AddResource(string name, byte[] value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);
 
            // Check for duplicate resources whose names vary only by case.
            _caseInsensitiveDups.Add(name, null);
            _resourceList.Add(name, value);
        }
        
        public void AddResourceData(string name, string typeName, byte[] serializedData)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));
            if (serializedData == null)
                throw new ArgumentNullException(nameof(serializedData));

            if (_resourceList == null)
                throw new InvalidOperationException(SR.InvalidOperation_ResourceWriterSaved);
 
            // Check for duplicate resources whose names vary only by case.
            _caseInsensitiveDups.Add(name, null);
            if (_preserializedData == null)
                _preserializedData = new Dictionary<string, PrecannedResource>(FastResourceComparer.Default);
 
            _preserializedData.Add(name, new PrecannedResource(typeName, serializedData));
        }

        // For cases where users can't create an instance of the deserialized 
        // type in memory, and need to pass us serialized blobs instead.
        // LocStudio's managed code parser will do this in some cases.
        private class PrecannedResource
        {
            internal readonly string TypeName;
            internal readonly byte[] Data;
 
            internal PrecannedResource(string typeName, byte[] data)
            {
                TypeName = typeName;
                Data = data;
            }
        }
 
        private class StreamWrapper
        {
            internal readonly Stream Stream;
            internal readonly bool CloseAfterWrite;
 
            internal StreamWrapper(Stream s, bool closeAfterWrite)
            {
                Stream = s;
                CloseAfterWrite = closeAfterWrite;
            }
        }

        public void Close()
        {
            Dispose(true);
        }
        
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_resourceList != null)
                {
                    Generate();
                }
                if (_output != null)
                {
                    _output.Dispose();
                }
            }

            _output = null;
            _caseInsensitiveDups = null;
        }

        public void Dispose()
        {
            Dispose(true);
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
            List<string> typeNames = new List<string>();

            // Write out the ResourceManager header
            // Write out magic number
            bw.Write(ResourceManager.MagicNumber);

            // Write out ResourceManager header version number
            bw.Write(ResourceManager.HeaderVersionNumber);

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
            Debug.Assert(resMgrHeaderBlob.Length > 0, "ResourceWriter: Expected non empty header");
            resMgrHeaderBlob.Seek(0, SeekOrigin.Begin);
            resMgrHeaderBlob.CopyTo(bw.BaseStream, (int)resMgrHeaderBlob.Length);
            // End ResourceManager header


            // Write out the RuntimeResourceSet header
            // Version number
            bw.Write(ResSetVersion);

            // number of resources
            int numResources = _resourceList.Count;
            if (_preserializedData != null)
                numResources += _preserializedData.Count;
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

                if (_preserializedData != null)
                {
                    foreach (KeyValuePair<string, PrecannedResource> entry in _preserializedData)
                    {
                        _resourceList.Add(entry.Key, entry.Value);
                    }
                }

                // Write resource name and position to the file, and the value
                // to our temporary buffer.  Save Type as well.
                foreach (var item in _resourceList)
                {
                    nameHashes[curNameNumber] = FastResourceComparer.HashFunction(item.Key);
                    namePositions[curNameNumber++] = (int)names.Seek(0, SeekOrigin.Current);
                    names.Write(item.Key); // key
                    names.Write((int)data.Seek(0, SeekOrigin.Current)); // virtual offset of value.

                    object value = item.Value;
                    ResourceTypeCode typeCode = FindTypeCode(value, typeNames);

                    // Write out type code
                    Write7BitEncodedInt(data, (int)typeCode);

                    var userProvidedResource = value as PrecannedResource;
                    if (userProvidedResource != null)
                    {
                        data.Write(userProvidedResource.Data);
                    }
                    else
                    {
                        WriteValue(typeCode, value, data);
                    }
                }

                // At this point, the ResourceManager header has been written.
                // Finish RuntimeResourceSet header
                // The reader expects a list of user defined type names 
                // following the size of the list, write 0 for this 
                // writer implementation
                bw.Write(typeNames.Count);
                foreach (var typeName in typeNames)
                {
                    bw.Write(typeName);
                }

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
                {
                    bw.Write(hash);
                }

                //  Write relative positions of all the names in the file.
                //   Note: this data is 4 byte aligned, occurring immediately 
                //   after the 8 byte aligned name hashes (whose length may 
                //   potentially be odd).
                Debug.Assert((bw.BaseStream.Position & 3) == 0, "ResourceWriter: Name positions array won't be 4 byte aligned!  Ack!");

                foreach (int pos in namePositions)
                {
                    bw.Write(pos);
                }

                // Flush all BinaryWriters to their underlying streams.
                bw.Flush();
                names.Flush();
                data.Flush();

                // Write offset to data section
                int startOfDataSection = (int)(bw.Seek(0, SeekOrigin.Current) + nameSection.Length);
                startOfDataSection += 4;  // We're writing an int to store this data, adding more bytes to the header
                bw.Write(startOfDataSection);

                // Write name section.
                if (nameSection.Length > 0)
                {
                    nameSection.Seek(0, SeekOrigin.Begin);
                    nameSection.CopyTo(bw.BaseStream, (int)nameSection.Length);
                }
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
            Debug.Assert(store != null);
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

        // Finds the ResourceTypeCode for a type, or adds this type to the
        // types list.
        private ResourceTypeCode FindTypeCode(object value, List<string> types)
        {
            if (value == null)
                return ResourceTypeCode.Null;
 
            Type type = value.GetType();
            if (type == typeof(string))
                return ResourceTypeCode.String;
            else if (type == typeof(int))
                return ResourceTypeCode.Int32;
            else if (type == typeof(bool))
                return ResourceTypeCode.Boolean;
            else if (type == typeof(char))
                return ResourceTypeCode.Char;
            else if (type == typeof(byte))
                return ResourceTypeCode.Byte;
            else if (type == typeof(sbyte))
                return ResourceTypeCode.SByte;
            else if (type == typeof(short))
                return ResourceTypeCode.Int16;
            else if (type == typeof(long))
                return ResourceTypeCode.Int64;
            else if (type == typeof(ushort))
                return ResourceTypeCode.UInt16;
            else if (type == typeof(uint))
                return ResourceTypeCode.UInt32;
            else if (type == typeof(ulong))
                return ResourceTypeCode.UInt64;
            else if (type == typeof(float))
                return ResourceTypeCode.Single;
            else if (type == typeof(double))
                return ResourceTypeCode.Double;
            else if (type == typeof(decimal))
                return ResourceTypeCode.Decimal;
            else if (type == typeof(DateTime))
                return ResourceTypeCode.DateTime;
            else if (type == typeof(TimeSpan))
                return ResourceTypeCode.TimeSpan;
            else if (type == typeof(byte[]))
                return ResourceTypeCode.ByteArray;
            else if (type == typeof(StreamWrapper))
                return ResourceTypeCode.Stream;


            // This is a user type, or a precanned resource.  Find type 
            // table index.  If not there, add new element.
            string typeName;
            if (type == typeof(PrecannedResource)) {
                typeName = ((PrecannedResource)value).TypeName;
                if (typeName.StartsWith("ResourceTypeCode.", StringComparison.Ordinal)) {
                    typeName = typeName.Substring(17);  // Remove through '.'
                    ResourceTypeCode typeCode = (ResourceTypeCode)Enum.Parse(typeof(ResourceTypeCode), typeName);
                    return typeCode;
                }
            }
            else 
            {
                typeName = MultitargetingHelpers.GetAssemblyQualifiedName(type, TypeNameConverter);
            }
 
            int typeIndex = types.IndexOf(typeName);
            if (typeIndex == -1) {
                typeIndex = types.Count;
                types.Add(typeName);
            }
 
            return (ResourceTypeCode)(typeIndex + ResourceTypeCode.StartOfUserTypes);
        }

        private void WriteValue(ResourceTypeCode typeCode, object value, BinaryWriter writer)
        {
            Debug.Assert(writer != null);

            switch (typeCode)
            {
                case ResourceTypeCode.Null:
                    break;

                case ResourceTypeCode.String:
                    writer.Write((string)value);
                    break;

                case ResourceTypeCode.Boolean:
                    writer.Write((bool)value);
                    break;

                case ResourceTypeCode.Char:
                    writer.Write((ushort)(char)value);
                    break;

                case ResourceTypeCode.Byte:
                    writer.Write((byte)value);
                    break;

                case ResourceTypeCode.SByte:
                    writer.Write((sbyte)value);
                    break;

                case ResourceTypeCode.Int16:
                    writer.Write((short)value);
                    break;

                case ResourceTypeCode.UInt16:
                    writer.Write((ushort)value);
                    break;

                case ResourceTypeCode.Int32:
                    writer.Write((int)value);
                    break;

                case ResourceTypeCode.UInt32:
                    writer.Write((uint)value);
                    break;

                case ResourceTypeCode.Int64:
                    writer.Write((long)value);
                    break;

                case ResourceTypeCode.UInt64:
                    writer.Write((ulong)value);
                    break;

                case ResourceTypeCode.Single:
                    writer.Write((float)value);
                    break;

                case ResourceTypeCode.Double:
                    writer.Write((double)value);
                    break;

                case ResourceTypeCode.Decimal:
                    writer.Write((decimal)value);
                    break;

                case ResourceTypeCode.DateTime:
                    // Use DateTime's ToBinary & FromBinary.
                    long data = ((DateTime)value).ToBinary();
                    writer.Write(data);
                    break;

                case ResourceTypeCode.TimeSpan:
                    writer.Write(((TimeSpan)value).Ticks);
                    break;

                // Special Types
                case ResourceTypeCode.ByteArray:
                    {
                        byte[] bytes = (byte[])value;
                        writer.Write(bytes.Length);
                        writer.Write(bytes, 0, bytes.Length);
                        break;
                    }

                case ResourceTypeCode.Stream:
                    {
                        StreamWrapper sw = (StreamWrapper)value;
                        if (sw.Stream.GetType() == typeof(MemoryStream))
                        {
                            MemoryStream ms = (MemoryStream)sw.Stream;
                            if (ms.Length > int.MaxValue)
                                throw new ArgumentException(SR.ArgumentOutOfRange_StreamLength);
                            byte[] arr = ms.ToArray();
                            writer.Write(arr.Length);
                            writer.Write(arr, 0, arr.Length);
                        }
                        else
                        {
                            Stream s = sw.Stream;
                            // we've already verified that the Stream is seekable
                            if (s.Length > int.MaxValue)
                                throw new ArgumentException(SR.ArgumentOutOfRange_StreamLength);

                            s.Position = 0;
                            writer.Write((int)s.Length);
                            byte[] buffer = new byte[4096];
                            int read = 0;
                            while ((read = s.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                writer.Write(buffer, 0, read);
                            }
                            if (sw.CloseAfterWrite)
                            {
                                s.Close();
                            }
                        }
                        break;
                    }

                default:
                    Debug.Assert(typeCode >= ResourceTypeCode.StartOfUserTypes, string.Format(CultureInfo.InvariantCulture, "ResourceReader: Unsupported ResourceTypeCode in .resources file!  {0}", typeCode));
                    throw new PlatformNotSupportedException(SR.NotSupported_BinarySerializedResources);
            }
        }
    }

    internal enum ResourceTypeCode {
        // Primitives
        Null = 0,
        String = 1,
        Boolean = 2,
        Char = 3,
        Byte = 4,
        SByte = 5,
        Int16 = 6,
        UInt16 = 7,
        Int32 = 8,
        UInt32 = 9,
        Int64 = 0xa,
        UInt64 = 0xb,
        Single = 0xc,
        Double = 0xd,
        Decimal = 0xe,
        DateTime = 0xf,
        TimeSpan = 0x10,
 
        // A meta-value - change this if you add new primitives
        LastPrimitive = TimeSpan,
 
        // Types with a special representation, like byte[] and Stream
        ByteArray = 0x20,
        Stream = 0x21,
 
        // User types - serialized using the binary formatter.
        StartOfUserTypes = 0x40
    }
}

