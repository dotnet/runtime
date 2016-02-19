// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Resources
{
    using System;
    using System.IO;
    using System.Globalization;
    using System.Collections;
    using System.Text;
    using System.Threading;
    using System.Collections.Generic;
    using System.Diagnostics;
    public sealed class ResourceReader : IDisposable
    {

        private const int ResSetVersion = 2;
        private const int ResourceTypeCodeString = 1;
        private const int ResourceManagerMagicNumber = unchecked((int)0xBEEFCACE);
        private const int ResourceManagerHeaderVersionNumber = 1;

        private BinaryReader _store;    // backing store we're reading from.
        private long _nameSectionOffset;  // Offset to name section of file.
        private long _dataSectionOffset;  // Offset to Data section of file.

        private int[] _namePositions; // relative locations of names
        private Type[] _typeTable;    // Lazy array of Types for resource values.
        private int[] _typeNamePositions;  // To delay initialize type table
        private int _numResources;    // Num of resources files, in case arrays aren't allocated.      
        private int _version;


        public ResourceReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable);

            _store = new BinaryReader(stream, Encoding.UTF8);

            ReadResources();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void Dispose(bool disposing)
        {
            if (_store != null)
            {

                if (disposing)
                {
                    // Close the stream in a thread-safe way.  This fix means 
                    // that we may call Close n times, but that's safe.
                    BinaryReader copyOfStore = _store;
                    _store = null;
                    if (copyOfStore != null)
                        copyOfStore.Dispose();
                }
                _store = null;
                _namePositions = null;

            }
        }
        private void SkipString()
        {
            int stringLength = Read7BitEncodedInt();
            if (stringLength < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_NegativeStringLength);
            }
            _store.BaseStream.Seek(stringLength, SeekOrigin.Current);
        }

        private unsafe int GetNamePosition(int index)
        {
            Debug.Assert(index >= 0 && index < _numResources, "Bad index into name position array.  index: " + index);

            int r;

            r = _namePositions[index];
            if (r < 0 || r > _dataSectionOffset - _nameSectionOffset)
            {
                throw new FormatException(SR.BadImageFormat_ResourcesNameInvalidOffset + ":" + r);
            }
            return r;
        }
        public IDictionaryEnumerator GetEnumerator()
        {
            if (_store == null)
                throw new InvalidOperationException(SR.ResourceReaderIsClosed);
            return new ResourceEnumerator(this);
        }


        private unsafe String AllocateStringForNameIndex(int index, out int dataOffset)
        {
            Debug.Assert(_store != null, "ResourceReader is closed!");
            byte[] bytes;
            int byteLen;
            long nameVA = GetNamePosition(index);
            lock (this)
            {
                _store.BaseStream.Seek(nameVA + _nameSectionOffset, SeekOrigin.Begin);
                // Can't use _store.ReadString, since it's using UTF-8!
                byteLen = Read7BitEncodedInt();
                if (byteLen < 0)
                {
                    throw new BadImageFormatException(SR.BadImageFormat_NegativeStringLength);
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
                        throw new EndOfStreamException(SR.BadImageFormat_ResourceNameCorrupted_NameIndex + index);
                    count -= n;
                }
                dataOffset = _store.ReadInt32();
                if (dataOffset < 0 || dataOffset >= _store.BaseStream.Length - _dataSectionOffset)
                {
                    throw new FormatException(SR.BadImageFormat_ResourcesDataInvalidOffset + " offset :" + dataOffset);
                }
            }
            return Encoding.Unicode.GetString(bytes, 0, byteLen);
        }

        private string GetValueForNameIndex(int index)
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
                    throw new FormatException(SR.BadImageFormat_ResourcesDataInvalidOffset + dataPos);
                }

                try
                {

                    return LoadString(dataPos);
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
        }
        
        //returns null if the resource is not a string
        private string LoadString(int pos)
        {

            _store.BaseStream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            int typeIndex = Read7BitEncodedInt();

            if (_version == 1)
            {
                Type typeinStream = FindType(typeIndex);

                if (typeIndex == -1 || !typeinStream.Equals(typeof(String)))
                    return null;
            }
            else
            {
                if (ResourceTypeCodeString != typeIndex)
                {
                    return null;
                }
            }

            return _store.ReadString();

        }

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
            // Read out the ResourceManager header
            // Read out magic number
            int magicNum = _store.ReadInt32();
            if (magicNum != ResourceManagerMagicNumber)
                throw new ArgumentException(SR.Resources_StreamNotValid);

            // Assuming this is ResourceManager header V1 or greater, hopefully
            // after the version number there is a number of bytes to skip
            // to bypass the rest of the ResMgr header. For V2 or greater, we
            // use this to skip to the end of the header

            int resMgrHeaderVersion = _store.ReadInt32();
            //number of bytes to skip over to get past ResMgr header
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
                //Due to legacy : this field is always a variant of  System.Resources.ResourceReader, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
                //So we Skip the type name for resourcereader unlike Desktop
                SkipString();

                // Skip over type name for a suitable ResourceSet
                SkipString();
            }

            // Read RuntimeResourceSet header
            // Do file version check
            int version = _store.ReadInt32();
            if (version != ResSetVersion && version != 1)
                throw new ArgumentException(SR.Arg_ResourceFileUnsupportedVersion + "Expected:" + ResSetVersion + "but got:" + version);
            _version = version;
            // number of resources
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

            //Skip over the  array of name hashes

            for (int i = 0; i < _numResources; i++)
            {
                _store.ReadInt32();
            }

            // Read in the array of relative positions for all the names.
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
                    String typeName = _store.ReadString();
                    _typeTable[typeIndex] = Type.GetType(typeName, true);
                }

                finally
                {
                    _store.BaseStream.Position = oldPos;
                }
            }
            Debug.Assert(_typeTable[typeIndex] != null, "Should have found a type!");
            return _typeTable[typeIndex];
        }

        //blatantly copied over from BinaryReader
        private int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException(SR.Format_Bad7BitInt32);

                // ReadByte handles end of stream cases for us.
                b = _store.ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        internal sealed class ResourceEnumerator : IDictionaryEnumerator
        {
            private const int ENUM_DONE = Int32.MinValue;
            private const int ENUM_NOT_STARTED = -1;

            private ResourceReader _reader;
            private bool _currentIsValid;
            private int _currentName;
            

            internal ResourceEnumerator(ResourceReader reader)
            {
                _currentName = ENUM_NOT_STARTED;
                _reader = reader;
               
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

            public Object Key
            {
                [System.Security.SecuritySafeCritical]  // auto-generated
                get
                {
                    if (_currentName == ENUM_DONE) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    if (!_currentIsValid) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);

                    int _dataPosition;
                    return _reader.AllocateStringForNameIndex(_currentName, out _dataPosition);
                }
            }

            public Object Current
            {
                get
                {
                    return Entry;
                }
            }


            public DictionaryEntry Entry
            {
                [System.Security.SecuritySafeCritical]  // auto-generated
                get
                {
                    if (_currentName == ENUM_DONE) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    if (!_currentIsValid) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);


                    String key;
                    Object value = null;
                    int _dataPosition;
                    key = _reader.AllocateStringForNameIndex(_currentName, out _dataPosition);


                    value = _reader.GetValueForNameIndex(_currentName);

                    return new DictionaryEntry(key, value);
                }
            }

            public Object Value
            {
                get
                {
                    if (_currentName == ENUM_DONE) throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    if (!_currentIsValid) throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);


                    return _reader.GetValueForNameIndex(_currentName);
                }

            }
            public void Reset()
            {

                if (_reader._store == null) throw new InvalidOperationException(SR.ResourceReaderIsClosed);
                _currentIsValid = false;
                _currentName = ENUM_NOT_STARTED;
            }
        }
    }
}
