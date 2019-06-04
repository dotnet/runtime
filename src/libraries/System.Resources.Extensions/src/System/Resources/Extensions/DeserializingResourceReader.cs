// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Resources.Extensions
{
    public partial class DeserializingResourceReader
    {
        private bool _assumeBinaryFormatter = false;
        private BinaryFormatter? _formatter = null;

        private bool ValidateReaderType(string readerType)
        {
            // our format?
            if (TypeNameComparer.Instance.Equals(readerType, PreserializedResourceWriter.DeserializingResourceReaderFullyQualifiedName))
            {
                return true;
            }

            // default format?
            if (TypeNameComparer.Instance.Equals(readerType, PreserializedResourceWriter.ResourceReaderFullyQualifiedName))
            {
                // we can read the default format, we just assume BinaryFormatter and don't
                // read the SerializationFormat
                _assumeBinaryFormatter = true;
                return true;
            }

            return false;
        }

        private object ReadBinaryFormattedObject()
        {
            if (_formatter == null)
            {
                _formatter = new BinaryFormatter();
            }

            return _formatter.Deserialize(_store.BaseStream);
        }

        private object DeserializeObject(int typeIndex)
        {
            Type type = FindType(typeIndex);

            if (_assumeBinaryFormatter)
            {
                return ReadBinaryFormattedObject();
            }

            // read type
            SerializationFormat format = (SerializationFormat)_store.Read7BitEncodedInt();

            object value;

            // read data
            switch (format)
            {
                case SerializationFormat.BinaryFormatter:
                    {
                        // read length
                        int length = _store.Read7BitEncodedInt();
                        if (length < 0)
                        {
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, length));
                        }
                        
                        long originalPosition = _store.BaseStream.Position;

                        value = ReadBinaryFormattedObject();

                        if (type == typeof(UnknownType))
                        {
                            // type information was omitted at the time of writing
                            // allow the payload to define the type
                            type = value.GetType();
                        }

                        long bytesRead = _store.BaseStream.Position - originalPosition;

                        // Ensure BF read what we expected.
                        if (bytesRead != length)
                        {
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, length));
                        }
                        break;
                    }
                case SerializationFormat.TypeConverterByteArray:
                    {
                        // read length
                        int length = _store.Read7BitEncodedInt();
                        if (length < 0)
                        {
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, length));
                        }

                        byte[] data = _store.ReadBytes(length);

                        TypeConverter converter = TypeDescriptor.GetConverter(type);

                        if (converter == null)
                        {
                            throw new TypeLoadException(SR.Format(SR.TypeLoadException_CannotLoadConverter, type));
                        }

                        value = converter.ConvertFrom(data);
                        break;
                    }
                case SerializationFormat.TypeConverterString:
                    {
                        string stringData = _store.ReadString();

                        TypeConverter converter = TypeDescriptor.GetConverter(type);

                        if (converter == null)
                        {
                            throw new TypeLoadException(SR.Format(SR.TypeLoadException_CannotLoadConverter, type));
                        }

                        value = converter.ConvertFromInvariantString(stringData);
                        break;
                    }
                case SerializationFormat.ActivatorStream:
                    {
                        // read length
                        int length = _store.Read7BitEncodedInt();
                        if (length < 0)
                        {
                            throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResourceDataLengthInvalid, length));
                        }
                        Stream stream;

                        if (_store.BaseStream is UnmanagedMemoryStream ums)
                        {
                            // For the case that we've memory mapped in the .resources
                            // file, just return a Stream pointing to that block of memory.
                            unsafe
                            {
                                stream = new UnmanagedMemoryStream(ums.PositionPointer, length, length, FileAccess.Read);
                            }
                        }
                        else
                        {

                            byte[] bytes = _store.ReadBytes(length);
                            // Lifetime of memory == lifetime of this stream.
                            stream = new MemoryStream(bytes, false);
                        }

                        value = Activator.CreateInstance(type, new object[] { stream });
                        break;
                    }
                default:
                    throw new BadImageFormatException(SR.BadImageFormat_TypeMismatch);
            }

            // Make sure we deserialized the type that we expected.  
            // This protects against bad typeconverters or bad binaryformatter payloads.
            if (value.GetType() != type)
                throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResType_SerBlobMismatch, type.FullName, value.GetType().FullName));

            return value;
        }

    }
}
