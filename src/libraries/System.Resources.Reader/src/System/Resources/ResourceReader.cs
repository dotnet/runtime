// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Resources
{
    public sealed class ResourceReader : IDisposable
    {
        private const int ResSetVersion = 2;
        private const int ResourceTypeCodeString = 1;
        private const int ResourceManagerMagicNumber = unchecked((int)0xBEEFCACE);
        private const int ResourceManagerHeaderVersionNumber = 1;

        private Stream _stream;    // backing store we're reading from.
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
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable);

            _stream = stream;

            ReadResources();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe void Dispose(bool disposing)
        {
            if (_stream != null) {
                if (disposing) {
                    Stream stream = _stream;
                    _stream = null;
                    if (stream != null) {
                        stream.Dispose();
                    }
                    _namePositions = null;
                }
            }
        }

        private void SkipString()
        {
            int stringLength;
            if (!_stream.TryReadInt327BitEncoded(out stringLength) || stringLength < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_NegativeStringLength);
            }
            _stream.Seek(stringLength, SeekOrigin.Current);
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
            if (_stream == null)
                throw new InvalidOperationException(SR.ResourceReaderIsClosed);
            return new ResourceEnumerator(this);
        }

        private void SuccessElseEosException(bool condition)
        {
            if (!condition) {
                throw new EndOfStreamException();
            }
        }

        private unsafe String AllocateStringForNameIndex(int index, out int dataOffset)
        {
            Debug.Assert(_stream != null, "ResourceReader is closed!");
            byte[] bytes;
            int byteLen;
            long nameVA = GetNamePosition(index);
            lock (this)
            {
                _stream.Seek(nameVA + _nameSectionOffset, SeekOrigin.Begin);
                // Can't use _store.ReadString, since it's using UTF-8!
                if (!_stream.TryReadInt327BitEncoded(out byteLen) || byteLen < 0)
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
                    int n = _stream.Read(bytes, byteLen - count, count);
                    if (n == 0)
                        throw new EndOfStreamException(SR.BadImageFormat_ResourceNameCorrupted_NameIndex + index);
                    count -= n;
                }

                SuccessElseEosException(_stream.TryReadInt32(out dataOffset));
                if (dataOffset < 0 || dataOffset >= _stream.Length - _dataSectionOffset)
                {
                    throw new FormatException(SR.BadImageFormat_ResourcesDataInvalidOffset + " offset :" + dataOffset);
                }
            }
            return Encoding.Unicode.GetString(bytes, 0, byteLen);
        }

        private string GetValueForNameIndex(int index)
        {
            Debug.Assert(_stream != null, "ResourceReader is closed!");
            long nameVA = GetNamePosition(index);
            lock (this)
            {
                _stream.Seek(nameVA + _nameSectionOffset, SeekOrigin.Begin);
                SkipString();

                int dataPos;
                SuccessElseEosException(_stream.TryReadInt32(out dataPos));
                if (dataPos < 0 || dataPos >= _stream.Length - _dataSectionOffset)
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

            _stream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            int typeIndex;
            
            if(!_stream.TryReadInt327BitEncoded(out typeIndex))
            {
                throw new BadImageFormatException(SR.BadImageFormat_TypeMismatch);
            }

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

            return _stream.ReadString();

        }

        private void ReadResources()
        {
            Debug.Assert(_stream != null, "ResourceReader is closed!");

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
            int magicNum;
            SuccessElseEosException(_stream.TryReadInt32(out magicNum));

            if (magicNum != ResourceManagerMagicNumber)
                throw new ArgumentException(SR.Resources_StreamNotValid);

            // Assuming this is ResourceManager header V1 or greater, hopefully
            // after the version number there is a number of bytes to skip
            // to bypass the rest of the ResMgr header. For V2 or greater, we
            // use this to skip to the end of the header

            int resMgrHeaderVersion;
            SuccessElseEosException(_stream.TryReadInt32(out resMgrHeaderVersion));

            //number of bytes to skip over to get past ResMgr header
            int numBytesToSkip;
            SuccessElseEosException(_stream.TryReadInt32(out numBytesToSkip));

            if (numBytesToSkip < 0 || resMgrHeaderVersion < 0) {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }

            if (resMgrHeaderVersion > 1) {
                _stream.Seek(numBytesToSkip, SeekOrigin.Current);
            }
            else {
                // We don't care about numBytesToSkip; read the rest of the header
                //Due to legacy : this field is always a variant of  System.Resources.ResourceReader, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
                //So we Skip the type name for resourcereader unlike Desktop
                SkipString();

                // Skip over type name for a suitable ResourceSet
                SkipString();
            }

            // Read RuntimeResourceSet header
            // Do file version check
            SuccessElseEosException(_stream.TryReadInt32(out _version));
            if (_version != ResSetVersion && _version != 1) {
                throw new ArgumentException(SR.Arg_ResourceFileUnsupportedVersion + "Expected:" + ResSetVersion + "but got:" + _version);
            }

            // number of resources
            SuccessElseEosException(_stream.TryReadInt32(out _numResources));
            if (_numResources < 0) {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }

            // Read type positions into type positions array.
            // But delay initialize the type table.
            int numTypes;
            SuccessElseEosException(_stream.TryReadInt32(out numTypes));
            if (numTypes < 0) {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }

            _typeTable = new Type[numTypes];
            _typeNamePositions = new int[numTypes];

            for (int i = 0; i < numTypes; i++)
            {
                _typeNamePositions[i] = (int)_stream.Position;

                // Skip over the Strings in the file.  Don't create types.
                SkipString();
            }

            // Prepare to read in the array of name hashes
            //  Note that the name hashes array is aligned to 8 bytes so 
            //  we can use pointers into it on 64 bit machines. (4 bytes 
            //  may be sufficient, but let's plan for the future)
            //  Skip over alignment stuff.  All public .resources files
            //  should be aligned   No need to verify the byte values.
            long pos = _stream.Position;
            int alignBytes = ((int)pos) & 7;
            if (alignBytes != 0)
            {
                for (int i = 0; i < 8 - alignBytes; i++)
                {
                    _stream.ReadByte();
                }
            }

            //Skip over the  array of name hashes

            for (int i = 0; i < _numResources; i++)
            {
                int hash;
                SuccessElseEosException(_stream.TryReadInt32(out hash));
            }

            // Read in the array of relative positions for all the names.
            _namePositions = new int[_numResources];
            for (int i = 0; i < _numResources; i++)
            {
                int namePosition;
                SuccessElseEosException(_stream.TryReadInt32(out namePosition));
                if (namePosition < 0)
                {
                    throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
                }

                _namePositions[i] = namePosition;
            }

            // Read location of data section.
            int dataSectionOffset;
            SuccessElseEosException(_stream.TryReadInt32(out dataSectionOffset));
            if (dataSectionOffset < 0)
            {
                throw new BadImageFormatException(SR.BadImageFormat_ResourcesHeaderCorrupted);
            }
            _dataSectionOffset = dataSectionOffset;

            // Store current location as start of name section
            _nameSectionOffset = _stream.Position;

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
                long oldPos = _stream.Position;
                try
                {
                    _stream.Position = _typeNamePositions[typeIndex];
                    string typeName = _stream.ReadString();
                    _typeTable[typeIndex] = Type.GetType(typeName, true);
                }

                finally
                {
                    _stream.Position = oldPos;
                }
            }
            Debug.Assert(_typeTable[typeIndex] != null, "Should have found a type!");
            return _typeTable[typeIndex];
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

                if (_reader._stream == null) throw new InvalidOperationException(SR.ResourceReaderIsClosed);
                _currentIsValid = false;
                _currentName = ENUM_NOT_STARTED;
            }
        }
    }
}
