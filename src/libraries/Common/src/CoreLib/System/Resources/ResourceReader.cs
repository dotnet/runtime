// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Default way to read streams of resources on 
** demand.
**
**         Version 2 support on October 6, 2003
** 
===========================================================*/

namespace System.Resources
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Security;
    using System.Globalization;
    using System.Configuration.Assemblies;
    using System.Runtime.Versioning;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Threading;

    // Provides the default implementation of IResourceReader, reading
    // .resources file from the system default binary format.  This class
    // can be treated as an enumerator once.
    // 
    // See the RuntimeResourceSet overview for details on the system 
    // default file format.
    // 

    internal struct ResourceLocator
    {
        internal object _value;  // Can be null.  Consider WeakReference instead?
        internal int _dataPos;

        internal ResourceLocator(int dataPos, object value)
        {
            _dataPos = dataPos;
            _value = value;
        }

        internal int DataPosition
        {
            get { return _dataPos; }
            //set { _dataPos = value; }
        }

        // Allows adding in profiling data in a future version, or a special
        // resource profiling build.  We could also use WeakReference.
        internal object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        internal static bool CanCache(ResourceTypeCode value)
        {
            Debug.Assert(value >= 0, "negative ResourceTypeCode.  What?");
            return value <= ResourceTypeCode.LastPrimitive;
        }
    }

    public sealed class ResourceReader : IResourceReader
    {
        // A reasonable default buffer size for reading from files, especially
        // when we will likely be seeking frequently.  Could be smaller, but does
        // it make sense to use anything less than one page?
        private const int DefaultFileStreamBufferSize = 4096;

        private BinaryReader _store;    // backing store we're reading from.
        // Used by RuntimeResourceSet and this class's enumerator.  Maps
        // resource name to a value, a ResourceLocator, or a 
        // LooselyLinkedManifestResource.
        internal Dictionary<string, ResourceLocator> _resCache;
        private long _nameSectionOffset;  // Offset to name section of file.
        private long _dataSectionOffset;  // Offset to Data section of file.

        // Note this class is tightly coupled with UnmanagedMemoryStream.
        // At runtime when getting an embedded resource from an assembly, 
        // we're given an UnmanagedMemoryStream referring to the mmap'ed portion
        // of the assembly.  The pointers here are pointers into that block of
        // memory controlled by the OS's loader.
        private int[] _nameHashes;    // hash values for all names.
        private unsafe int* _nameHashesPtr;  // In case we're using UnmanagedMemoryStream
        private int[] _namePositions; // relative locations of names
        private unsafe int* _namePositionsPtr;  // If we're using UnmanagedMemoryStream
        private Type[] _typeTable;    // Lazy array of Types for resource values.
        private int[] _typeNamePositions;  // To delay initialize type table
        private int _numResources;    // Num of resources files, in case arrays aren't allocated.

        private readonly bool _permitDeserialization;  // can deserialize BinaryFormatted resources
        private object _binaryFormatter; // binary formatter instance to use for deserializing

        // statics used to dynamically call into BinaryFormatter
        // When successfully located s_binaryFormatterType will point to the BinaryFormatter type
        // and s_deserializeMethod will point to an unbound delegate to the deserialize method.
        private static Type s_binaryFormatterType;
        private static Func<object, Stream, object> s_deserializeMethod;

        // We'll include a separate code path that uses UnmanagedMemoryStream to
        // avoid allocating String objects and the like.
        private UnmanagedMemoryStream _ums;

        // Version number of .resources file, for compatibility
        private int _version;


        public ResourceReader(string fileName)
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

        public ResourceReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable);

            _resCache = new Dictionary<string, ResourceLocator>(FastResourceComparer.Default);
            _store = new BinaryReader(stream, Encoding.UTF8);
            // We have a faster code path for reading resource files from an assembly.
            _ums = stream as UnmanagedMemoryStream;

            ReadResources();
        }

        // This is the constructor the RuntimeResourceSet calls,
        // passing in the stream to read from and the RuntimeResourceSet's 
        // internal hash table (hash table of names with file offsets
        // and values, coupled to this ResourceReader).
        internal ResourceReader(Stream stream, Dictionary<string, ResourceLocator> resCache, bool permitDeserialization)
        {
            Debug.Assert(stream != null, "Need a stream!");
            Debug.Assert(stream.CanRead, "Stream should be readable!");
            Debug.Assert(resCache != null, "Need a Dictionary!");

            _resCache = resCache;
            _store = new BinaryReader(stream, Encoding.UTF8);

            _ums = stream as UnmanagedMemoryStream;

            _permitDeserialization = permitDeserialization;

            ReadResources();
        }


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
                    _store = null;
                    if (copyOfStore != null)
                        copyOfStore.Close();
                }
                _store = null;
                _namePositions = null;
                _nameHashes = null;
                _ums = null;
                _namePositionsPtr = null;
                _nameHashesPtr = null;
            }
        }

        internal static unsafe int ReadUnalignedI4(int* p)
        {
            byte* buffer = (byte*)p;
            // Unaligned, little endian format
            return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
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
            Debug.Assert(index >= 0 && index < _numResources, "Bad index into hash array.  index: " + index);
            Debug.Assert((_ums == null && _nameHashes != null && _nameHashesPtr == null) ||
                            (_ums != null && _nameHashes == null && _nameHashesPtr != null), "Internal state mangled.");
            if (_ums == null)
                return _nameHashes[index];
            else
                return ReadUnalignedI4(&_nameHashesPtr[index]);
        }

        private unsafe int GetNamePosition(int index)
        {
            Debug.Assert(index >= 0 && index < _numResources, "Bad index into name position array.  index: " + index);
            Debug.Assert((_ums == null && _namePositions != null && _namePositionsPtr == null) ||
                            (_ums != null && _namePositions == null && _namePositionsPtr != null), "Internal state mangled.");
            int r;
            if (_ums == null)
                r = _namePositions[index];
            else
                r = ReadUnalignedI4(&_namePositionsPtr[index]);
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
                //return FastResourceComparer.CompareOrdinal((char*)bytes, byteLen/2, name) == 0;
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

                    string s = null;
                    char* charPtr = (char*)_ums.PositionPointer;

                    s = new string(charPtr, 0, byteLen / 2);

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
        private object GetValueForNameIndex(int index)
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
                ResourceTypeCode junk;
                if (_version == 1)
                    return LoadObjectV1(dataPos);
                else
                    return LoadObjectV2(dataPos, out junk);
            }
        }

        // This takes a virtual offset into the data section and reads a String
        // from that location.
        // Anyone who calls LoadObject should make sure they take a lock so 
        // no one can cause us to do a seek in here.
        internal string LoadString(int pos)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            _store.BaseStream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            string s = null;
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
                    string typeString;
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
        internal object LoadObject(int pos)
        {
            if (_version == 1)
                return LoadObjectV1(pos);
            ResourceTypeCode typeCode;
            return LoadObjectV2(pos, out typeCode);
        }

        internal object LoadObject(int pos, out ResourceTypeCode typeCode)
        {
            if (_version == 1)
            {
                object o = LoadObjectV1(pos);
                typeCode = (o is string) ? ResourceTypeCode.String : ResourceTypeCode.StartOfUserTypes;
                return o;
            }
            return LoadObjectV2(pos, out typeCode);
        }

        // This takes a virtual offset into the data section and reads an Object
        // from that location.
        // Anyone who calls LoadObject should make sure they take a lock so 
        // no one can cause us to do a seek in here.
        internal object LoadObjectV1(int pos)
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

        private object _LoadObjectV1(int pos)
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
                int[] bits = new int[4];
                for (int i = 0; i < bits.Length; i++)
                    bits[i] = _store.ReadInt32();
                return new decimal(bits);
            }
            else
            {
                return DeserializeObject(typeIndex);
            }
        }

        internal object LoadObjectV2(int pos, out ResourceTypeCode typeCode)
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

        private object _LoadObjectV2(int pos, out ResourceTypeCode typeCode)
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

        private object DeserializeObject(int typeIndex)
        {
            if (!_permitDeserialization)
            {
                throw new NotSupportedException(SR.NotSupported_ResourceObjectSerialization);
            }

            if (_binaryFormatter == null)
            {
                InitializeBinaryFormatter();
            }

            Type type = FindType(typeIndex);
  
            object graph = s_deserializeMethod(_binaryFormatter, _store.BaseStream);
            
            // guard against corrupted resources
            if (graph.GetType() != type)
                throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResType_SerBlobMismatch, type.FullName, graph.GetType().FullName));
 
            return graph;
        }

        private void InitializeBinaryFormatter()
        {
            LazyInitializer.EnsureInitialized(ref s_binaryFormatterType, () =>
                Type.GetType("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter, System.Runtime.Serialization.Formatters, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                throwOnError: true));

            LazyInitializer.EnsureInitialized(ref s_deserializeMethod, () =>
               {
                   MethodInfo binaryFormatterDeserialize = s_binaryFormatterType.GetMethod("Deserialize", new Type[] { typeof(Stream) });

                    // create an unbound delegate that can accept a BinaryFormatter instance as object
                    return (Func<object, Stream, object>)typeof(ResourceReader)
                            .GetMethod(nameof(CreateUntypedDelegate), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(s_binaryFormatterType)
                            .Invoke(null, new object[] { binaryFormatterDeserialize });
               });

            _binaryFormatter = Activator.CreateInstance(s_binaryFormatterType);
        }

        // generic method that we specialize at runtime once we've loaded the BinaryFormatter type
        // permits creating an unbound delegate so that we can avoid reflection after the initial
        // lightup code completes.
        private static Func<object, Stream, object> CreateUntypedDelegate<TInstance>(MethodInfo method)
        {
            Func<TInstance, Stream, object> typedDelegate = (Func<TInstance, Stream, object>)Delegate.CreateDelegate(typeof(Func<TInstance, Stream, object>), null, method);

            return (obj, stream) => typedDelegate((TInstance)obj, stream);
        }

        // Reads in the header information for a .resources file.  Verifies some
        // of the assumptions about this resource set, and builds the class table
        // for the default resource file format.
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
                readerType = System.CoreLib.FixupCoreLibName(readerType);
                AssemblyName mscorlib = new AssemblyName(ResourceManager.MscorlibName);

                if (!ResourceManager.CompareNames(readerType, ResourceManager.ResReaderTypeName, mscorlib))
                    throw new NotSupportedException(SR.Format(SR.NotSupported_WrongResourceReader_Type, readerType));

                // Skip over type name for a suitable ResourceSet
                SkipString();
            }

            // Read RuntimeResourceSet header
            // Do file version check
            int version = _store.ReadInt32();
            if (version != RuntimeResourceSet.Version && version != 1)
                throw new ArgumentException(SR.Format(SR.Arg_ResourceFileUnsupportedVersion, RuntimeResourceSet.Version, version));
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
                    byte* junk = _ums.PositionPointer;
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
                    byte* junk = _ums.PositionPointer;
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
        private Type FindType(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= _typeTable.Length)
            {
                throw new BadImageFormatException(SR.BadImageFormat_InvalidType);
            }
            if (_typeTable[typeIndex] == null)
            {
                long oldPos = _store.BaseStream.Position;
                try
                {
                    _store.BaseStream.Position = _typeNamePositions[typeIndex];
                    string typeName = _store.ReadString();
                    _typeTable[typeIndex] = Type.GetType(typeName, true);
                }
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
                catch (FileNotFoundException)
                {
                    throw new NotSupportedException(SR.NotSupported_ResourceObjectSerialization);
                }
                finally
                {
                    _store.BaseStream.Position = oldPos;
                }
            }
            Debug.Assert(_typeTable[typeIndex] != null, "Should have found a type!");
            return _typeTable[typeIndex];
        }

        public void GetResourceData(string resourceName, out string resourceType, out byte[] resourceData)
        {
            if (resourceName == null)
                throw new ArgumentNullException(nameof(resourceName));
            if (_resCache == null)
                throw new InvalidOperationException(SR.ResourceReaderIsClosed);

            // Get the type information from the data section.  Also,
            // sort all of the data section's indexes to compute length of
            // the serialized data for this type (making sure to subtract
            // off the length of the type code).
            int[] sortedDataPositions = new int[_numResources];
            int dataPos = FindPosForResource(resourceName);
            if (dataPos == -1)
            {
                throw new ArgumentException(SR.Format(SR.Arg_ResourceNameNotExist, resourceName));
            }

            lock (this)
            {
                // Read all the positions of data within the data section.
                for (int i = 0; i < _numResources; i++)
                {
                    _store.BaseStream.Position = _nameSectionOffset + GetNamePosition(i);
                    // Skip over name of resource
                    int numBytesToSkip = _store.Read7BitEncodedInt();
                    if (numBytesToSkip < 0)
                    {
                        throw new FormatException(SR.Format(SR.BadImageFormat_ResourcesNameInvalidOffset, numBytesToSkip));
                    }
                    _store.BaseStream.Position += numBytesToSkip;

                    int dPos = _store.ReadInt32();
                    if (dPos < 0 || dPos >= _store.BaseStream.Length - _dataSectionOffset)
                    {
                        throw new FormatException(SR.Format(SR.BadImageFormat_ResourcesDataInvalidOffset, dPos));
                    }
                    sortedDataPositions[i] = dPos;
                }
                Array.Sort(sortedDataPositions);

                int index = Array.BinarySearch(sortedDataPositions, dataPos);
                Debug.Assert(index >= 0 && index < _numResources, "Couldn't find data position within sorted data positions array!");
                long nextData = (index < _numResources - 1) ? sortedDataPositions[index + 1] + _dataSectionOffset : _store.BaseStream.Length;
                int len = (int)(nextData - (dataPos + _dataSectionOffset));
                Debug.Assert(len >= 0 && len <= (int)_store.BaseStream.Length - dataPos + _dataSectionOffset, "Length was negative or outside the bounds of the file!");

                // Read type code then byte[]
                _store.BaseStream.Position = _dataSectionOffset + dataPos;
                ResourceTypeCode typeCode = (ResourceTypeCode)_store.Read7BitEncodedInt();
                if (typeCode < 0 || typeCode >= ResourceTypeCode.StartOfUserTypes + _typeTable.Length)
                {
                    throw new BadImageFormatException(SR.BadImageFormat_InvalidType);
                }
                resourceType = TypeNameFromTypeCode(typeCode);

                // The length must be adjusted to subtract off the number 
                // of bytes in the 7 bit encoded type code.
                len -= (int)(_store.BaseStream.Position - (_dataSectionOffset + dataPos));
                byte[] bytes = _store.ReadBytes(len);
                if (bytes.Length != len)
                    throw new FormatException(SR.BadImageFormat_ResourceNameCorrupted);
                resourceData = bytes;
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

            private ResourceReader _reader;
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

            public object Current
            {
                get
                {
                    return Entry;
                }
            }

            // Warning: This requires that you call the Key or Entry property FIRST before calling it!
            internal int DataPosition
            {
                get
                {
                    return _dataPosition;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    if (_currentName == ENUM_DONE) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    if (!_currentIsValid) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                    if (_reader._resCache == null) throw new InvalidOperationException(SR.ResourceReaderIsClosed);

                    string key;
                    object value = null;
                    lock (_reader)
                    { // locks should be taken in the same order as in RuntimeResourceSet.GetObject to avoid deadlock
                        lock (_reader._resCache)
                        {
                            key = _reader.AllocateStringForNameIndex(_currentName, out _dataPosition); // AllocateStringForNameIndex could lock on _reader
                            ResourceLocator locator;
                            if (_reader._resCache.TryGetValue(key, out locator))
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

            public object Value
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
