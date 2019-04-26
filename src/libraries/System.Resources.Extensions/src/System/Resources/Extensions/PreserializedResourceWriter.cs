// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;

namespace System.Resources.Extensions
{
    partial class PreserializedResourceWriter
    {
        // indicates if the types of resources saved will require the DeserializingResourceReader
        // in order to read them.
        bool _requiresDeserializingResourceReader = false;

        // use hard-coded strings rather than typeof so that the version doesn't leak into resources files 
        internal const string DeserializingResourceReaderFullyQualifiedName = "System.Resources.Extensions.DeserializingResourceReader, System.Resources.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";
        internal const string RuntimeResourceSetFullyQualifiedName = "System.Resources.Extensions.RuntimeResourceSet, System.Resources.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";

        private string ResourceReaderTypeName => _requiresDeserializingResourceReader ?
            DeserializingResourceReaderFullyQualifiedName :
            ResourceReaderFullyQualifiedName;

        private string ResourceSetTypeName => _requiresDeserializingResourceReader ?
            RuntimeResourceSetFullyQualifiedName :
            ResSetTypeName;

        /// <summary>
        /// Adds a resource of specified type represented by a string value which will be 
        /// passed to the type's TypeConverter when reading the resource.
        /// </summary>
        /// <param name="name">Resource name</param>
        /// <param name="typeName">Assembly qualified type name of the resource</param>
        /// <param name="value">Value of the resource in string form understood by the type's TypeConverter</param>
        public void AddTypeConverterResource(string name, string typeName, string value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            AddResourceData(name, typeName, new ResourceDataRecord(SerializationFormat.TypeConverterString, value));

            _requiresDeserializingResourceReader = true;
        }

        /// <summary>
        /// Adds a resource of specified type represented by a byte[] value which will be 
        /// passed to the type's TypeConverter when reading the resource.
        /// </summary>
        /// <param name="name">Resource name</param>
        /// <param name="typeName">Assembly qualified type name of the resource</param>
        /// <param name="value">Value of the resource in byte[] form understood by the type's TypeConverter</param>
        public void AddTypeConverterResource(string name, string typeName, byte[] value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            AddResourceData(name, typeName, new ResourceDataRecord(SerializationFormat.TypeConverterByteArray, value));

            _requiresDeserializingResourceReader = true;
        }

        /// <summary>
        /// Adds a resource of specified type represented by a byte[] value which will be 
        /// passed to BinaryFormatter when reading the resource.
        /// </summary>
        /// <param name="name">Resource name</param>
        /// <param name="typeName">Assembly qualified type name of the resource</param>
        /// <param name="value">Value of the resource in byte[] form understood by BinaryFormatter</param>
        public void AddBinaryFormattedResource(string name, string typeName, byte[] value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            AddResourceData(name, typeName, new ResourceDataRecord(SerializationFormat.BinaryFormatter, value));
        }

        /// <summary>
        /// Adds a resource of specified type represented by a Stream value which will be 
        /// passed to the type's constructor when reading the resource.
        /// </summary>
        /// <param name="name">Resource name</param>
        /// <param name="typeName">Assembly qualified type name of the resource</param>
        /// <param name="value">Value of the resource in Stream form understood by the types constructor</param>
        /// <param name="closeAfterWrite">Indicates that the stream should be closed after resources have been written</param>
        public void AddActivatorResource(string name, string typeName, Stream value, bool closeAfterWrite = false)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (typeName == null)
                throw new ArgumentNullException(nameof(typeName));
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!value.CanSeek)
                throw new ArgumentException(SR.NotSupported_UnseekableStream);

            AddResourceData(name, typeName, new ResourceDataRecord(SerializationFormat.ActivatorStream, value, closeAfterWrite));

            _requiresDeserializingResourceReader = true;
        }

        private class ResourceDataRecord
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
            ResourceDataRecord record = dataContext as ResourceDataRecord;

            Debug.Assert(record != null);

            // Only write the format if we resources are in DeserializingResourceReader format
            if (_requiresDeserializingResourceReader)
            {
                Write7BitEncodedInt(writer, (int)record.Format);
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
                                Write7BitEncodedInt(writer, data.Length);
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

                            Write7BitEncodedInt(writer, (int)stream.Length);

                            stream.CopyTo(writer.BaseStream);

                            break;
                        }
                    case SerializationFormat.TypeConverterByteArray:
                        {
                            byte[] data = (byte[])record.Data;
                            Write7BitEncodedInt(writer, data.Length);
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
