// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Resources
#if RESOURCES_EXTENSIONS
    .Extensions
#endif
{
#pragma warning disable IDE0065
#if RESOURCES_EXTENSIONS
    using ResourceReader = DeserializingResourceReader;
#endif
#pragma warning restore IDE0065

    // Provides the default implementation of IResourceReader, reading
    // .resources file from the system default binary format.  This class
    // can be treated as an enumerator once.
    //
    // See the RuntimeResourceSet overview for details on the system
    // default file format.
    //

    internal readonly struct ResourceLocator
    {
        internal ResourceLocator(int dataPos, object? value)
        {
            DataPosition = dataPos;
            Value = value;
        }

        internal int DataPosition { get; }
        internal object? Value { get; }

        internal static bool CanCache(ResourceTypeCode value)
        {
            Debug.Assert(value >= 0, "negative ResourceTypeCode.  What?");
            return value <= ResourceTypeCode.LastPrimitive;
        }
    }

    public sealed partial class
#if RESOURCES_EXTENSIONS
        DeserializingResourceReader
#else
        ResourceReader
#endif
        : IResourceReader
    {
        // A reasonable default buffer size for reading from files, especially
        // when we will likely be seeking frequently.  Could be smaller, but does
        // it make sense to use anything less than one page?
        private const int DefaultFileStreamBufferSize = 4096;

        private BinaryReader _store;    // backing store we're reading from.
        // Used by RuntimeResourceSet and this class's enumerator.  Maps
        // resource name to a value, a ResourceLocator, or a
        // LooselyLinkedManifestResource.
        internal Dictionary<string, ResourceLocator>? _resCache;
        private long _nameSectionOffset;  // Offset to name section of file.
        private long _dataSectionOffset;  // Offset to Data section of file.

        // Note this class is tightly coupled with UnmanagedMemoryStream.
        // At runtime when getting an embedded resource from an assembly,
        // we're given an UnmanagedMemoryStream referring to the mmap'ed portion
        // of the assembly.  The pointers here are pointers into that block of
        // memory controlled by the OS's loader.
        private int[]? _nameHashes;    // hash values for all names.
        private unsafe int* _nameHashesPtr;  // In case we're using UnmanagedMemoryStream
        private int[]? _namePositions; // relative locations of names
        private unsafe int* _namePositionsPtr;  // If we're using UnmanagedMemoryStream
        private Type?[] _typeTable;    // Lazy array of Types for resource values.
        private int[] _typeNamePositions;  // To delay initialize type table
        private int _numResources;    // Num of resources files, in case arrays aren't allocated.

        // We'll include a separate code path that uses UnmanagedMemoryStream to
        // avoid allocating String objects and the like.
        private UnmanagedMemoryStream? _ums;

        // Version number of .resources file, for compatibility
        private int _version;


        public
#if RESOURCES_EXTENSIONS
        DeserializingResourceReader(string fileName)
#else
        ResourceReader(string fileName)
#endif
        {
            _resCache = new Dictionary<string, ResourceLocator>(FastResourceComparer.Default);
            _store = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.RandomAccess), Encoding.UTF8);

            try
            {
                ReadResources();
            }
            catch
            {
                _store.Close(); // If we threw an exception, close the file.
                throw;
            }
        }

        public
#if RESOURCES_EXTENSIONS
        DeserializingResourceReader(Stream stream)
#else
        ResourceReader(Stream stream)
#endif
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable);
            }

            _resCache = new Dictionary<string, ResourceLocator>(FastResourceComparer.Default);
            _store = new BinaryReader(stream, Encoding.UTF8);
            // We have a faster code path for reading resource files from an assembly.
            _ums = stream as UnmanagedMemoryStream;

            ReadResources();
        }

        internal static bool AllowCustomResourceTypes { get; } = AppContext.TryGetSwitch("System.Resources.ResourceManager.AllowCustomResourceTypes", out bool allowReflection) ? allowReflection : true;

        public void Close()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Close();
        }

        private unsafe void Dispose(bool disposing)
        {
            if (_store != null)
            {
                _resCache = null;
                if (disposing)
                {
                    // Close the stream in a thread-safe way.  This fix means
                    // that we may call Close n times, but that's safe.
                    BinaryReader copyOfStore = _store;
                    _store = null!;
                    copyOfStore?.Close();
                }
                _store = null!;
                _namePositions = null;
                _nameHashes = null;
                _ums = null;
                _namePositionsPtr = null;
                _nameHashesPtr = null;
            }
        }

        internal static unsafe int ReadUnalignedI4(int* p)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(p, sizeof(int)));
        }

        private void SkipString()
        {
            int stringLength = _store.Read7BitEncodedInt();
            if (stringLength < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_NegativeStringLength);
            }
            _store.BaseStream.Seek(stringLength, SeekOrigin.Current);
        }

        private unsafe int GetNameHash(int index)
        {
            Debug.Assert(index >= 0 && index < _numResources, $"Bad index into hash array.  index: {index}");

            if (_ums == null)
            {
                Debug.Assert(_nameHashes != null && _nameHashesPtr == null, "Internal state mangled.");
                return _nameHashes[index];
            }
            else
            {
                Debug.Assert(_nameHashes == null && _nameHashesPtr != null, "Internal state mangled.");
                return ReadUnalignedI4(&_nameHashesPtr[index]);
            }
        }

        private unsafe int GetNamePosition(int index)
        {
            Debug.Assert(index >= 0 && index < _numResources, $"Bad index into name position array.  index: {index}");
            int r;
            if (_ums == null)
            {
                Debug.Assert(_namePositions != null && _namePositionsPtr == null, "Internal state mangled.");
                r = _namePositions[index];
            }
            else
            {
                Debug.Assert(_namePositions == null && _namePositionsPtr != null, "Internal state mangled.");
                r = ReadUnalignedI4(&_namePositionsPtr[index]);
            }

            if (r < 0 || r > _dataSectionOffset - _nameSectionOffset)
            {
                throw new FormatException(SR.Format(SR.BadImageFormat_ResourcesNameInvalidOffset, r));
            }
            return r;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            if (_resCache == null)
                throw new InvalidOperationException(SR.ResourceReaderIsClosed);
            return new ResourceEnumerator(this);
        }

        internal ResourceEnumerator GetEnumeratorInternal()
        {
            return new ResourceEnumerator(this);
        }

        // From a name, finds the associated virtual offset for the data.
        // To read the data, seek to _dataSectionOffset + dataPos, then
        // read the resource type & data.
        // This does a binary search through the names.
        internal int FindPosForResource(string name)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            int hash = FastResourceComparer.HashFunction(name);

            // Binary search over the hashes.  Use the _namePositions array to
            // determine where they exist in the underlying stream.
            int lo = 0;
            int hi = _numResources - 1;
            int index = -1;
            bool success = false;
            while (lo <= hi)
            {
                index = (lo + hi) >> 1;
                // Do NOT use subtraction here, since it will wrap for large
                // negative numbers.
                int currentHash = GetNameHash(index);
                int c;
                if (currentHash == hash)
                    c = 0;
                else if (currentHash < hash)
                    c = -1;
                else
                    c = 1;

                if (c == 0)
                {
                    success = true;
                    break;
                }
                if (c < 0)
                    lo = index + 1;
                else
                    hi = index - 1;
            }
            if (!success)
            {
                return -1;
            }

            // index is the location in our hash array that corresponds with a
            // value in the namePositions array.
            // There could be collisions in our hash function.  Check on both sides
            // of index to find the range of hash values that are equal to the
            // target hash value.
            if (lo != index)
            {
                lo = index;
                while (lo > 0 && GetNameHash(lo - 1) == hash)
                    lo--;
            }
            if (hi != index)
            {
                hi = index;
                while (hi < _numResources - 1 && GetNameHash(hi + 1) == hash)
                    hi++;
            }

            lock (this)
            {
                for (int i = lo; i <= hi; i++)
                {
                    _store.BaseStream.Seek(_nameSectionOffset + GetNamePosition(i), SeekOrigin.Begin);
                    if (CompareStringEqualsName(name))
                    {
                        int dataPos = _store.ReadInt32();
                        if (dataPos < 0 || dataPos >= _store.BaseStream.Length - _dataSectionOffset)
                        {
                            throw new FormatException(SR.Format(SR.BadImageFormat_ResourcesDataInvalidOffset, dataPos));
                        }
                        return dataPos;
                    }
                }
            }
            return -1;
        }

        // This compares the String in the .resources file at the current position
        // with the string you pass in.
        // Whoever calls this method should make sure that they take a lock
        // so no one else can cause us to seek in the stream.
        private unsafe bool CompareStringEqualsName(string name)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            int byteLen = _store.Read7BitEncodedInt();
            if (byteLen < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_NegativeStringLength);
            }
            if (_ums != null)
            {
                byte* bytes = _ums.PositionPointer;
                // Skip over the data in the Stream, positioning ourselves right after it.
                _ums.Seek(byteLen, SeekOrigin.Current);
                if (_ums.Position > _ums.Length)
                {
                    throw new BadImageFormatException(SR.BadImageFormat_ResourcesNameTooLong);
                }

                // On 64-bit machines, these char*'s may be misaligned.  Use a
                // byte-by-byte comparison instead.
                return FastResourceComparer.CompareOrdinal(bytes, byteLen, name) == 0;
            }
            else
            {
                // This code needs to be fast
                byte[] bytes = new byte[byteLen];
                int numBytesToRead = byteLen;
                while (numBytesToRead > 0)
                {
                    int n = _store.Read(bytes, byteLen - numBytesToRead, numBytesToRead);
                    if (n == 0)
                        throw new BadImageFormatException(SR.BadImageFormat_ResourceNameCorrupted);
                    numBytesToRead -= n;
                }
                return FastResourceComparer.CompareOrdinal(bytes, byteLen / 2, name) == 0;
            }
        }

        // This is used in the enumerator.  The enumerator iterates from 0 to n
        // of our resources and this returns the resource name for a particular
        // index.  The parameter is NOT a virtual offset.
        private unsafe string AllocateStringForNameIndex(int index, out int dataOffset)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            byte[] bytes;
            int byteLen;
            long nameVA = GetNamePosition(index);
            lock (this)
            {
                _store.BaseStream.Seek(nameVA + _nameSectionOffset, SeekOrigin.Begin);
                // Can't use _store.ReadString, since it's using UTF-8!
                byteLen = _store.Read7BitEncodedInt();
                if (byteLen < 0)
                {
                    throw new BadImageFormatException(SR.BadImageFormat_NegativeStringLength);
                }

                if (_ums != null)
                {
                    if (_ums.Position > _ums.Length - byteLen)
                        throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourcesIndexTooLong, index));

                    string? s = null;
                    char* charPtr = (char*)_ums.PositionPointer;

                    if (BitConverter.IsLittleEndian)
                    {
                        s = new string(charPtr, 0, byteLen / 2);
                    }
                    else
                    {
                        char[] arr = new char[byteLen / 2];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (char)BinaryPrimitives.ReverseEndianness((short)charPtr[i]);
                        }
                        s = new string(arr);
                    }

                    _ums.Position += byteLen;
                    dataOffset = _store.ReadInt32();
                    if (dataOffset < 0 || dataOffset >= _store.BaseStream.Length - _dataSectionOffset)
                    {
                        throw new FormatException(SR.Format(SR.BadImageFormat_ResourcesDataInvalidOffset, dataOffset));
                    }
                    return s;
                }

                bytes = new byte[byteLen];
                // We must read byteLen bytes, or we have a corrupted file.
                // Use a blocking read in case the stream doesn't give us back
                // everything immediately.
                int count = byteLen;
                while (count > 0)
                {
                    int n = _store.Read(bytes, byteLen - count, count);
                    if (n == 0)
                        throw new EndOfStreamException(SR.Format(SR.BadImageFormat_ResourceNameCorrupted_NameIndex, index));
                    count -= n;
                }
                dataOffset = _store.ReadInt32();
                if (dataOffset < 0 || dataOffset >= _store.BaseStream.Length - _dataSectionOffset)
                {
                    throw new FormatException(SR.Format(SR.BadImageFormat_ResourcesDataInvalidOffset, dataOffset));
                }
            }
            return Encoding.Unicode.GetString(bytes, 0, byteLen);
        }

        // This is used in the enumerator.  The enumerator iterates from 0 to n
        // of our resources and this returns the resource value for a particular
        // index.  The parameter is NOT a virtual offset.
        private object? GetValueForNameIndex(int index)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            long nameVA = GetNamePosition(index);
            lock (this)
            {
                _store.BaseStream.Seek(nameVA + _nameSectionOffset, SeekOrigin.Begin);
                SkipString();

                int dataPos = _store.ReadInt32();
                if (dataPos < 0 || dataPos >= _store.BaseStream.Length - _dataSectionOffset)
                {
                    throw new FormatException(SR.Format(SR.BadImageFormat_ResourcesDataInvalidOffset, dataPos));
                }

                if (_version == 1)
                    return LoadObjectV1(dataPos);
                else
                    return LoadObjectV2(dataPos, out _);
            }
        }

        // This takes a virtual offset into the data section and reads a String
        // from that location.
        // Anyone who calls LoadObject should make sure they take a lock so
        // no one can cause us to do a seek in here.
        internal string? LoadString(int pos)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            _store.BaseStream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            string? s = null;
            int typeIndex = _store.Read7BitEncodedInt();
            if (_version == 1)
            {
                if (typeIndex == -1)
                    return null;
                if (FindType(typeIndex) != typeof(string))
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Type, FindType(typeIndex).FullName));
                s = _store.ReadString();
            }
            else
            {
                ResourceTypeCode typeCode = (ResourceTypeCode)typeIndex;
                if (typeCode != ResourceTypeCode.String && typeCode != ResourceTypeCode.Null)
                {
                    string? typeString;
                    if (typeCode < ResourceTypeCode.StartOfUserTypes)
                        typeString = typeCode.ToString();
                    else
                        typeString = FindType(typeCode - ResourceTypeCode.StartOfUserTypes).FullName;
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ResourceNotString_Type, typeString));
                }
                if (typeCode == ResourceTypeCode.String) // ignore Null
                    s = _store.ReadString();
            }
            return s;
        }

        // Called from RuntimeResourceSet
        internal object? LoadObject(int pos)
        {
            if (_version == 1)
                return LoadObjectV1(pos);
            return LoadObjectV2(pos, out _);
        }

        internal object? LoadObject(int pos, out ResourceTypeCode typeCode)
        {
            if (_version == 1)
            {
                object? o = LoadObjectV1(pos);
                typeCode = (o is string) ? ResourceTypeCode.String : ResourceTypeCode.StartOfUserTypes;
                return o;
            }
            return LoadObjectV2(pos, out typeCode);
        }

        // This takes a virtual offset into the data section and reads an Object
        // from that location.
        // Anyone who calls LoadObject should make sure they take a lock so
        // no one can cause us to do a seek in here.
        internal object? LoadObjectV1(int pos)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            Debug.Assert(_version == 1, ".resources file was not a V1 .resources file!");

            try
            {
                // mega try-catch performs exceptionally bad on x64; factored out body into
                // _LoadObjectV1 and wrap here.
                return _LoadObjectV1(pos);
            }
            catch (EndOfStreamException eof)
            {
                throw new BadImageFormatException(SR.BadImageFormat_TypeMismatch, eof);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new BadImageFormatException(SR.BadImageFormat_TypeMismatch, e);
            }
        }

        private object? _LoadObjectV1(int pos)
        {
            _store.BaseStream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            int typeIndex = _store.Read7BitEncodedInt();
            if (typeIndex == -1)
                return null;
            Type type = FindType(typeIndex);
            // Consider putting in logic to see if this type is a
            // primitive or a value type first, so we can reach the
            // deserialization code faster for arbitrary objects.

            if (type == typeof(string))
                return _store.ReadString();
            else if (type == typeof(int))
                return _store.ReadInt32();
            else if (type == typeof(byte))
                return _store.ReadByte();
            else if (type == typeof(sbyte))
                return _store.ReadSByte();
            else if (type == typeof(short))
                return _store.ReadInt16();
            else if (type == typeof(long))
                return _store.ReadInt64();
            else if (type == typeof(ushort))
                return _store.ReadUInt16();
            else if (type == typeof(uint))
                return _store.ReadUInt32();
            else if (type == typeof(ulong))
                return _store.ReadUInt64();
            else if (type == typeof(float))
                return _store.ReadSingle();
            else if (type == typeof(double))
                return _store.ReadDouble();
            else if (type == typeof(DateTime))
            {
                // Ideally we should use DateTime's ToBinary & FromBinary,
                // but we can't for compatibility reasons.
                return new DateTime(_store.ReadInt64());
            }
            else if (type == typeof(TimeSpan))
                return new TimeSpan(_store.ReadInt64());
            else if (type == typeof(decimal))
            {
#if RESOURCES_EXTENSIONS
                int[] bits = new int[4];
#else
                Span<int> bits = stackalloc int[4];
#endif
                for (int i = 0; i < bits.Length; i++)
                    bits[i] = _store.ReadInt32();
                return new decimal(bits);
            }
            else
            {
                return DeserializeObject(typeIndex);
            }
        }

        internal object? LoadObjectV2(int pos, out ResourceTypeCode typeCode)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            Debug.Assert(_version >= 2, ".resources file was not a V2 (or higher) .resources file!");

            try
            {
                // mega try-catch performs exceptionally bad on x64; factored out body into
                // _LoadObjectV2 and wrap here.
                return _LoadObjectV2(pos, out typeCode);
            }
            catch (EndOfStreamException eof)
            {
                throw new BadImageFormatException(SR.BadImageFormat_TypeMismatch, eof);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new BadImageFormatException(SR.BadImageFormat_TypeMismatch, e);
            }
        }

        private object? _LoadObjectV2(int pos, out ResourceTypeCode typeCode)
        {
            _store.BaseStream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            typeCode = (ResourceTypeCode)_store.Read7BitEncodedInt();

            switch (typeCode)
            {
                case ResourceTypeCode.Null:
                    return null;

                case ResourceTypeCode.String:
                    return _store.ReadString();

                case ResourceTypeCode.Boolean:
                    return _store.ReadBoolean();

                case ResourceTypeCode.Char:
                    return (char)_store.ReadUInt16();

                case ResourceTypeCode.Byte:
                    return _store.ReadByte();

                case ResourceTypeCode.SByte:
                    return _store.ReadSByte();

                case ResourceTypeCode.Int16:
                    return _store.ReadInt16();

                case ResourceTypeCode.UInt16:
                    return _store.ReadUInt16();

                case ResourceTypeCode.Int32:
                    return _store.ReadInt32();

                case ResourceTypeCode.UInt32:
                    return _store.ReadUInt32();

                case ResourceTypeCode.Int64:
                    return _store.ReadInt64();

                case ResourceTypeCode.UInt64:
                    return _store.ReadUInt64();

                case ResourceTypeCode.Single:
                    return _store.ReadSingle();

                case ResourceTypeCode.Double:
                    return _store.ReadDouble();

                case ResourceTypeCode.Decimal:
                    return _store.ReadDecimal();

                case ResourceTypeCode.DateTime:
                    // Use DateTime's ToBinary & FromBinary.
                    long data = _store.ReadInt64();
                    return DateTime.FromBinary(data);

                case ResourceTypeCode.TimeSpan:
                    long ticks = _store.ReadInt64();
                    return new TimeSpan(ticks);

                // Special types
                case ResourceTypeCode.ByteArray:
                    {
                        int len = _store.ReadInt32();
                        if (len < 0)
                        {
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, len));
                        }

                        if (_ums == null)
                        {
                            if (len > _store.BaseStream.Length)
                            {
                                throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, len));
                            }
                            return _store.ReadBytes(len);
                        }

                        if (len > _ums.Length - _ums.Position)
                        {
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, len));
                        }

                        byte[] bytes = new byte[len];
                        int r = _ums.Read(bytes, 0, len);
                        Debug.Assert(r == len, "ResourceReader needs to use a blocking read here.  (Call _store.ReadBytes(len)?)");
                        return bytes;
                    }

                case ResourceTypeCode.Stream:
                    {
                        int len = _store.ReadInt32();
                        if (len < 0)
                        {
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, len));
                        }
                        if (_ums == null)
                        {
                            byte[] bytes = _store.ReadBytes(len);
                            // Lifetime of memory == lifetime of this stream.
                            return new PinnedBufferMemoryStream(bytes);
                        }

                        // make sure we don't create an UnmanagedMemoryStream that is longer than the resource stream.
                        if (len > _ums.Length - _ums.Position)
                        {
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, len));
                        }

                        // For the case that we've memory mapped in the .resources
                        // file, just return a Stream pointing to that block of memory.
                        unsafe
                        {
                            return new UnmanagedMemoryStream(_ums.PositionPointer, len, len, FileAccess.Read);
                        }
                    }

                default:
                    if (typeCode < ResourceTypeCode.StartOfUserTypes)
                    {
                        throw new BadImageFormatException(SR.BadImageFormat_TypeMismatch);
                    }
                    break;
            }

            // Normal serialized objects
            int typeIndex = typeCode - ResourceTypeCode.StartOfUserTypes;
            return DeserializeObject(typeIndex);
        }

        // Reads in the header information for a .resources file.  Verifies some
        // of the assumptions about this resource set, and builds the class table
        // for the default resource file format.
        [MemberNotNull(nameof(_typeTable))]
        [MemberNotNull(nameof(_typeNamePositions))]
        private void ReadResources()
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");

            try
            {
                // mega try-catch performs exceptionally bad on x64; factored out body into
                // _ReadResources and wrap here.
                _ReadResources();
            }
            catch (EndOfStreamException eof)
            {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted, eof);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted, e);
            }
        }

        [MemberNotNull(nameof(_typeTable))]
        [MemberNotNull(nameof(_typeNamePositions))]
        private void _ReadResources()
        {
            // Read ResourceManager header
            // Check for magic number
            int magicNum = _store.ReadInt32();
            if (magicNum != ResourceManager.MagicNumber)
                throw new ArgumentException(SR.Resources_StreamNotValid);
            // Assuming this is ResourceManager header V1 or greater, hopefully
            // after the version number there is a number of bytes to skip
            // to bypass the rest of the ResMgr header. For V2 or greater, we
            // use this to skip to the end of the header
            int resMgrHeaderVersion = _store.ReadInt32();
            int numBytesToSkip = _store.ReadInt32();
            if (numBytesToSkip < 0 || resMgrHeaderVersion < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }
            if (resMgrHeaderVersion > 1)
            {
                _store.BaseStream.Seek(numBytesToSkip, SeekOrigin.Current);
            }
            else
            {
                // We don't care about numBytesToSkip; read the rest of the header

                // Read in type name for a suitable ResourceReader
                // Note ResourceWriter & InternalResGen use different Strings.
                string readerType = _store.ReadString();

                if (!ValidateReaderType(readerType))
                    throw new NotSupportedException(SR.Format(SR.NotSupported_WrongResourceReader_Type, readerType));

                // Skip over type name for a suitable ResourceSet
                SkipString();
            }

            // Read RuntimeResourceSet header
            // Do file version check
            int version = _store.ReadInt32();

            // File format version number
            const int CurrentVersion = 2;

            if (version != CurrentVersion && version != 1)
                throw new ArgumentException(SR.Format(SR.Arg_ResourceFileUnsupportedVersion, CurrentVersion, version));
            _version = version;

            _numResources = _store.ReadInt32();
            if (_numResources < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }

            // Read type positions into type positions array.
            // But delay initialize the type table.
            int numTypes = _store.ReadInt32();
            if (numTypes < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }
            _typeTable = new Type[numTypes];
            _typeNamePositions = new int[numTypes];
            for (int i = 0; i < numTypes; i++)
            {
                _typeNamePositions[i] = (int)_store.BaseStream.Position;

                // Skip over the Strings in the file.  Don't create types.
                SkipString();
            }

            // Prepare to read in the array of name hashes
            //  Note that the name hashes array is aligned to 8 bytes so
            //  we can use pointers into it on 64 bit machines. (4 bytes
            //  may be sufficient, but let's plan for the future)
            //  Skip over alignment stuff.  All public .resources files
            //  should be aligned   No need to verify the byte values.
            long pos = _store.BaseStream.Position;
            int alignBytes = ((int)pos) & 7;
            if (alignBytes != 0)
            {
                for (int i = 0; i < 8 - alignBytes; i++)
                {
                    _store.ReadByte();
                }
            }

            // Read in the array of name hashes
            if (_ums == null)
            {
                _nameHashes = new int[_numResources];
                for (int i = 0; i < _numResources; i++)
                {
                    _nameHashes[i] = _store.ReadInt32();
                }
            }
            else
            {
                int seekPos = unchecked(4 * _numResources);
                if (seekPos < 0)
                {
                    throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
                }
                unsafe
                {
                    _nameHashesPtr = (int*)_ums.PositionPointer;
                    // Skip over the array of nameHashes.
                    _ums.Seek(seekPos, SeekOrigin.Current);
                    // get the position pointer once more to check that the whole table is within the stream
                    _ = _ums.PositionPointer;
                }
            }

            // Read in the array of relative positions for all the names.
            if (_ums == null)
            {
                _namePositions = new int[_numResources];
                for (int i = 0; i < _numResources; i++)
                {
                    int namePosition = _store.ReadInt32();
                    if (namePosition < 0)
                    {
                        throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
                    }

                    _namePositions[i] = namePosition;
                }
            }
            else
            {
                int seekPos = unchecked(4 * _numResources);
                if (seekPos < 0)
                {
                    throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
                }
                unsafe
                {
                    _namePositionsPtr = (int*)_ums.PositionPointer;
                    // Skip over the array of namePositions.
                    _ums.Seek(seekPos, SeekOrigin.Current);
                    // get the position pointer once more to check that the whole table is within the stream
                    _ = _ums.PositionPointer;
                }
            }

            // Read location of data section.
            _dataSectionOffset = _store.ReadInt32();
            if (_dataSectionOffset < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }

            // Store current location as start of name section
            _nameSectionOffset = _store.BaseStream.Position;

            // _nameSectionOffset should be <= _dataSectionOffset; if not, it's corrupt
            if (_dataSectionOffset < _nameSectionOffset)
            {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }
        }

        // This allows us to delay-initialize the Type[].  This might be a
        // good startup time savings, since we might have to load assemblies
        // and initialize Reflection.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "UseReflectionToGetType will get trimmed out when AllowCustomResourceTypes is set to false. " +
            "When set to true, we will already throw a warning for this feature switch, so we suppress this one in order for" +
            "the user to only get one error.")]
        private Type FindType(int typeIndex)
        {
            if (!AllowCustomResourceTypes)
            {
                throw new NotSupportedException(SR.ResourceManager_ReflectionNotAllowed);
            }

            if (typeIndex < 0 || typeIndex >= _typeTable.Length)
            {
                throw new BadImageFormatException(SR.BadImageFormat_InvalidType);
            }

            return _typeTable[typeIndex] ?? UseReflectionToGetType(typeIndex);
        }

        [RequiresUnreferencedCode("The CustomResourceTypesSupport feature switch has been enabled for this app which is being trimmed. " +
            "Custom readers as well as custom objects on the resources file are not observable by the trimmer and so required assemblies, types and members may be removed.")]
        private Type UseReflectionToGetType(int typeIndex)
        {
            long oldPos = _store.BaseStream.Position;
            try
            {
                _store.BaseStream.Position = _typeNamePositions[typeIndex];
                string typeName = _store.ReadString();
                _typeTable[typeIndex] = Type.GetType(typeName, true);
                Debug.Assert(_typeTable[typeIndex] != null, "Should have found a type!");
                return _typeTable[typeIndex]!;
            }
// If-defing this coud out from Resources Extensions since they will by definition always support deserialization
// So we shouldn't attempt to wrap the original exception with a NotSupportedException since that can be misleading.
// For that reason, the bellow code is only relevant when building CoreLib's ResourceReader.
#if !RESOURCES_EXTENSIONS
            // If serialization isn't supported, we convert FileNotFoundException to
            // NotSupportedException for consistency with v2. This is a corner-case, but the
            // idea is that we want to give the user a more accurate error message. Even if
            // the dependency were found, we know it will require serialization since it
            // can't be one of the types we special case. So if the dependency were found,
            // it would go down the serialization code path, resulting in NotSupported for
            // SKUs without serialization.
            //
            // We don't want to regress the expected case by checking the type info before
            // getting to Type.GetType -- this is costly with v1 resource formats.
            catch (FileNotFoundException fileNotFoundException) when (!_permitDeserialization)
            {
                // Include the FileNotFoundException as an inner exception to make it more diagnosable
                throw new NotSupportedException(SR.NotSupported_ResourceObjectSerialization, fileNotFoundException);
            }
#endif
            finally
            {
                _store.BaseStream.Position = oldPos;
            }
        }

        private string TypeNameFromTypeCode(ResourceTypeCode typeCode)
        {
            Debug.Assert(typeCode >= 0, "can't be negative");
            if (typeCode < ResourceTypeCode.StartOfUserTypes)
            {
                Debug.Assert(!string.Equals(typeCode.ToString(), "LastPrimitive"), "Change ResourceTypeCode metadata order so LastPrimitive isn't what Enum.ToString prefers.");
                return "ResourceTypeCode." + typeCode.ToString();
            }
            else
            {
                int typeIndex = typeCode - ResourceTypeCode.StartOfUserTypes;
                Debug.Assert(typeIndex >= 0 && typeIndex < _typeTable.Length, "TypeCode is broken or corrupted!");
                long oldPos = _store.BaseStream.Position;
                try
                {
                    _store.BaseStream.Position = _typeNamePositions[typeIndex];
                    return _store.ReadString();
                }
                finally
                {
                    _store.BaseStream.Position = oldPos;
                }
            }
        }

        internal sealed class ResourceEnumerator : IDictionaryEnumerator
        {
            private const int ENUM_DONE = int.MinValue;
            private const int ENUM_NOT_STARTED = -1;

            private readonly ResourceReader _reader;
            private bool _currentIsValid;
            private int _currentName;
            private int _dataPosition; // cached for case-insensitive table

            internal ResourceEnumerator(ResourceReader reader)
            {
                _currentName = ENUM_NOT_STARTED;
                _reader = reader;
                _dataPosition = -2;
            }

            public bool MoveNext()
            {
                if (_currentName == _reader._numResources - 1 || _currentName == ENUM_DONE)
                {
                    _currentIsValid = false;
                    _currentName = ENUM_DONE;
                    return false;
                }
                _currentIsValid = true;
                _currentName++;
                return true;
            }

            public object Key
            {
                get
                {
                    if (_currentName == ENUM_DONE) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    if (!_currentIsValid) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                    if (_reader._resCache == null) throw new InvalidOperationException(SR.ResourceReaderIsClosed);

                    return _reader.AllocateStringForNameIndex(_currentName, out _dataPosition);
                }
            }

            public object Current => Entry;

            // Warning: This requires that you call the Key or Entry property FIRST before calling it!
            internal int DataPosition => _dataPosition;

            public DictionaryEntry Entry
            {
                get
                {
                    if (_currentName == ENUM_DONE) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    if (!_currentIsValid) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                    if (_reader._resCache == null) throw new InvalidOperationException(SR.ResourceReaderIsClosed);

                    string key;
                    object? value = null;
                    lock (_reader)
                    { // locks should be taken in the same order as in RuntimeResourceSet.GetObject to avoid deadlock
                        lock (_reader._resCache)
                        {
                            key = _reader.AllocateStringForNameIndex(_currentName, out _dataPosition); // AllocateStringForNameIndex could lock on _reader
                            if (_reader._resCache.TryGetValue(key, out ResourceLocator locator))
                            {
                                value = locator.Value;
                            }
                            if (value == null)
                            {
                                if (_dataPosition == -1)
                                    value = _reader.GetValueForNameIndex(_currentName);
                                else
                                    value = _reader.LoadObject(_dataPosition);
                                // If enumeration and subsequent lookups happen very
                                // frequently in the same process, add a ResourceLocator
                                // to _resCache here.  But WinForms enumerates and
                                // just about everyone else does lookups.  So caching
                                // here may bloat working set.
                            }
                        }
                    }
                    return new DictionaryEntry(key, value);
                }
            }

            public object? Value
            {
                get
                {
                    if (_currentName == ENUM_DONE) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    if (!_currentIsValid) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                    if (_reader._resCache == null) throw new InvalidOperationException(SR.ResourceReaderIsClosed);

                    // Consider using _resCache here, eventually, if
                    // this proves to be an interesting perf scenario.
                    // But mixing lookups and enumerators shouldn't be
                    // particularly compelling.
                    return _reader.GetValueForNameIndex(_currentName);
                }
            }

            public void Reset()
            {
                if (_reader._resCache == null) throw new InvalidOperationException(SR.ResourceReaderIsClosed);
                _currentIsValid = false;
                _currentName = ENUM_NOT_STARTED;
            }
        }
    }
}
