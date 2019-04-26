// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Resources.Extensions
{
    public partial class DeserializingResourceReader
    {
        private bool _assumeBinaryFormatter = false;
        private BinaryFormatter _formatter = null;

        private bool ValidateReaderType(string readerType)
        {
            // our format?
            if (CompareNames(readerType, PreserializedResourceWriter.DeserializingResourceReaderFullyQualifiedName))
            {
                return true;
            }

            // default format?
            if (CompareNames(readerType, PreserializedResourceWriter.ResourceReaderFullyQualifiedName))
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

        // Compare two type names ignoring version
        private static bool CompareNames(string typeName1, string typeName2)
        {
            // First, compare type names
            int comma1 = typeName1.IndexOf(',');
            int comma2 = typeName2.IndexOf(',');
            if (comma1 != comma2)
                return false;

            // both are missing assembly name, compare entire string as type name
            if (comma1 == -1)
                return string.Equals(typeName1, typeName2, StringComparison.Ordinal);

            // compare the type name portion
            ReadOnlySpan<char> type1 = typeName1.AsSpan(0, comma1);
            ReadOnlySpan<char> type2 = typeName2.AsSpan(0, comma2);
            if (!type1.Equals(type2, StringComparison.Ordinal))
                return false;

            // Now, compare assembly display names (IGNORES VERSION AND PROCESSORARCHITECTURE)
            // also, for  mscorlib ignores everything, since that's what the binder is going to do
            while (Char.IsWhiteSpace(typeName1[++comma1]))
                ;
            while (Char.IsWhiteSpace(typeName2[++comma2]))
                ;

            // case insensitive
            AssemblyName an1 = new AssemblyName(typeName1.Substring(comma1));
            AssemblyName an2 = new AssemblyName(typeName2.Substring(comma2));
            if (!string.Equals(an1.Name, an2.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            // to match IsMscorlib() in VM
            if (string.Equals(an1.Name, "mscorlib", StringComparison.OrdinalIgnoreCase))
                return true;

            if (an1.CultureInfo?.LCID != an2.CultureInfo?.LCID)
                return false;

            byte[] pkt1 = an1.GetPublicKeyToken();
            byte[] pkt2 = an2.GetPublicKeyToken();
            return pkt1.AsSpan().SequenceEqual(pkt2);
        }
    }
}
