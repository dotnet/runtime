// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace System.Resources.Extensions
{
    internal sealed class UnknownType { }

    public partial class PreserializedResourceWriter
    {
        // indicates if the types of resources saved will require the DeserializingResourceReader
        // in order to read them.
        private bool _requiresDeserializingResourceReader;

        // use hard-coded strings rather than typeof so that the version doesn't leak into resources files
        internal const string DeserializingResourceReaderFullyQualifiedName = "System.Resources.Extensions.DeserializingResourceReader, System.Resources.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";
        internal const string RuntimeResourceSetFullyQualifiedName = "System.Resources.Extensions.RuntimeResourceSet, System.Resources.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";

        // an internal type name used to represent an unknown resource type, explicitly omit version to save
        // on size and avoid changes in user resources.  This works since we only ever load this type name
        // from calls to GetType from this assembly.
        private static readonly string UnknownObjectTypeName = typeof(UnknownType).FullName!;

        private string ResourceReaderTypeName => _requiresDeserializingResourceReader ?
            DeserializingResourceReaderFullyQualifiedName :
            ResourceReaderFullyQualifiedName;

        private string ResourceSetTypeName => _requiresDeserializingResourceReader ?
            RuntimeResourceSetFullyQualifiedName :
            ResSetTypeName;

        // a collection of primitive types in a dictionary, indexed by type name
        // using a comparer which handles type name comparisons similar to what
        // is done by reflection
        private static readonly IReadOnlyDictionary<string, Type> s_primitiveTypes = new Dictionary<string, Type>(16, TypeNameComparer.Instance)
        {
            { typeof(string).FullName!, typeof(string) },
            { typeof(int).FullName!, typeof(int) },
            { typeof(bool).FullName!, typeof(bool) },
            { typeof(char).FullName!, typeof(char) },
            { typeof(byte).FullName!, typeof(byte) },
            { typeof(sbyte).FullName!, typeof(sbyte) },
            { typeof(short).FullName!, typeof(short) },
            { typeof(long).FullName!, typeof(long) },
            { typeof(ushort).FullName!, typeof(ushort) },
            { typeof(uint).FullName!, typeof(uint) },
            { typeof(ulong).FullName!, typeof(ulong) },
            { typeof(float).FullName!, typeof(float) },
            { typeof(double).FullName!, typeof(double) },
            { typeof(decimal).FullName!, typeof(decimal) },
            { typeof(DateTime).FullName!, typeof(DateTime) },
            { typeof(TimeSpan).FullName!, typeof(TimeSpan) }
            // byte[] and Stream are primitive types but do not define a conversion from string
        };

        /// <summary>
        /// Adds a resource of specified type represented by a string value.
        /// If the type is a primitive type, the value will be converted using TypeConverter by the writer
        /// to that primitive type and stored in the resources in binary format.
        /// If the type is not a primitive type, the string value will be stored in the resources as a
        /// string and converted with a TypeConverter for the type when reading the resource.
        /// This is done to avoid activating arbitrary types during resource writing.
        /// </summary>
        /// <param name="name">Resource name</param>
        /// <param name="value">Value of the resource in string form understood by the type's TypeConverter</param>
        /// <param name="typeName">Assembly qualified type name of the resource</param>
        public void AddResource(string name, string value, string typeName)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            // determine if the type is a primitive type
            if (s_primitiveTypes.TryGetValue(typeName, out Type? primitiveType))
            {
                // directly add strings
                if (primitiveType == typeof(string))
                {
                    AddResource(name, value);
                }
                else
                {
                    // for primitive types that are not strings, convert the string value to the
                    // primitive type value.
                    // we intentionally avoid calling GetType on the user provided type name
                    // and instead will only ever convert to one of the known types.
                    TypeConverter converter = TypeDescriptor.GetConverter(primitiveType);

                    if (converter == null)
                    {
                        throw new TypeLoadException(SR.Format(SR.TypeLoadException_CannotLoadConverter, primitiveType));
                    }

                    object primitiveValue = converter.ConvertFromInvariantString(value)!;

                    Debug.Assert(primitiveValue.GetType() == primitiveType);

                    AddResource(name, primitiveValue);
                }
            }
            else
            {
                AddResourceData(name, typeName, new ResourceDataRecord(SerializationFormat.TypeConverterString, value));
                _requiresDeserializingResourceReader = true;
            }
        }

        /// <summary>
        /// Adds a resource of specified type represented by a byte[] value which will be
        /// passed to the type's TypeConverter when reading the resource.
        /// </summary>
        /// <param name="name">Resource name</param>
        /// <param name="value">Value of the resource in byte[] form understood by the type's TypeConverter</param>
        /// <param name="typeName">Assembly qualified type name of the resource</param>
        public void AddTypeConverterResource(string name, byte[] value, string typeName)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            AddResourceData(name, typeName, new ResourceDataRecord(SerializationFormat.TypeConverterByteArray, value));

            _requiresDeserializingResourceReader = true;
        }

        /// <summary>
        /// Adds a resource of specified type represented by a byte[] value which will be
        /// passed to BinaryFormatter when reading the resource.
        /// </summary>
        /// <param name="name">Resource name</param>
        /// <param name="value">Value of the resource in byte[] form understood by BinaryFormatter</param>
        /// <param name="typeName">Assembly qualified type name of the resource</param>
        public void AddBinaryFormattedResource(string name, byte[] value, string? typeName = null)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (typeName == null)
            {
                // Some resx-files are missing type information for binary-formatted resources.
                // These would have previously been handled by deserializing once, capturing the type
                // and reserializing when writing the resources.  We don't want to do that so instead
                // we just omit the type.
                typeName = UnknownObjectTypeName;
            }

            AddResourceData(name, typeName, new ResourceDataRecord(SerializationFormat.BinaryFormatter, value));

            // Even though ResourceReader can handle BinaryFormatted resources, the resource may contain
            // type names that were mangled by the ResXWriter's SerializationBinder, which we need to fix

            _requiresDeserializingResourceReader = true;
        }

        /// <summary>
        /// Adds a resource of specified type represented by a Stream value which will be
        /// passed to the type's constructor when reading the resource.
        /// </summary>
        /// <param name="name">Resource name</param>
        /// <param name="value">Value of the resource in Stream form understood by the types constructor</param>
        /// <param name="typeName">Assembly qualified type name of the resource</param>
        /// <param name="closeAfterWrite">Indicates that the stream should be closed after resources have been written</param>
        public void AddActivatorResource(string name, Stream value, string typeName, bool closeAfterWrite = false)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (!value.CanSeek)
                throw new ArgumentException(SR.NotSupported_UnseekableStream);

            AddResourceData(name, typeName, new ResourceDataRecord(SerializationFormat.ActivatorStream, value, closeAfterWrite));

            _requiresDeserializingResourceReader = true;
        }

        private sealed class ResourceDataRecord
        {
            internal readonly SerializationFormat Format;
            internal readonly object Data;
            internal readonly bool CloseAfterWrite;

            internal ResourceDataRecord(SerializationFormat format, object data, bool closeAfterWrite = false)
            {
                Format = format;
                Data = data;
                CloseAfterWrite = closeAfterWrite;
            }
        }

        private void WriteData(BinaryWriter writer, object dataContext)
        {
            ResourceDataRecord? record = dataContext as ResourceDataRecord;

            Debug.Assert(record != null);

            // Only write the format if we resources are in DeserializingResourceReader format
            if (_requiresDeserializingResourceReader)
            {
                writer.Write7BitEncodedInt((int)record.Format);
            }

            try
            {
                switch (record.Format)
                {
                    case SerializationFormat.BinaryFormatter:
                        {
                            byte[] data = (byte[])record.Data;

                            // only write length if using DeserializingResourceReader, ResourceReader
                            // doesn't constrain binaryFormatter
                            if (_requiresDeserializingResourceReader)
                            {
                                writer.Write7BitEncodedInt(data.Length);
                            }

                            writer.Write(data);
                            break;
                        }
                    case SerializationFormat.ActivatorStream:
                        {
                            Stream stream = (Stream)record.Data;

                            if (stream.Length > int.MaxValue)
                                throw new ArgumentException(SR.ArgumentOutOfRange_StreamLength);

                            stream.Position = 0;

                            writer.Write7BitEncodedInt((int)stream.Length);

                            stream.CopyTo(writer.BaseStream);

                            break;
                        }
                    case SerializationFormat.TypeConverterByteArray:
                        {
                            byte[] data = (byte[])record.Data;
                            writer.Write7BitEncodedInt(data.Length);
                            writer.Write(data);
                            break;
                        }
                    case SerializationFormat.TypeConverterString:
                        {
                            string data = (string)record.Data;
                            writer.Write(data);
                            break;
                        }
                    default:
                        // unreachable: indicates inconsistency in this class
                        throw new ArgumentException(nameof(ResourceDataRecord.Format));
                }
            }
            finally
            {
                if (record.Data is IDisposable disposable && record.CloseAfterWrite)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
