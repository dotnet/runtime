// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace System.Resources
{
    partial class ResourceReader
    {
        private readonly bool _permitDeserialization;  // can deserialize BinaryFormatted resources
        private object? _binaryFormatter; // binary formatter instance to use for deserializing

        // statics used to dynamically call into BinaryFormatter
        // When successfully located s_binaryFormatterType will point to the BinaryFormatter type
        // and s_deserializeMethod will point to an unbound delegate to the deserialize method.
        private static Type? s_binaryFormatterType;
        private static Func<object?, Stream, object>? s_deserializeMethod;

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

            object graph = s_deserializeMethod!(_binaryFormatter, _store.BaseStream);

            // guard against corrupted resources
            if (graph.GetType() != type)
                throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResType_SerBlobMismatch, type.FullName, graph.GetType().FullName));

            return graph;
        }

        private void InitializeBinaryFormatter()
        {
#pragma warning disable CS8634 // TODO-NULLABLE: Remove warning disable when nullable attributes are respected
            LazyInitializer.EnsureInitialized(ref s_binaryFormatterType, () =>
                Type.GetType("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter, System.Runtime.Serialization.Formatters, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                throwOnError: true));

            LazyInitializer.EnsureInitialized(ref s_deserializeMethod, () =>
            {
                MethodInfo binaryFormatterDeserialize = s_binaryFormatterType!.GetMethod("Deserialize", new Type[] { typeof(Stream) })!;

                // create an unbound delegate that can accept a BinaryFormatter instance as object
                return (Func<object?, Stream, object>)typeof(ResourceReader)
                        .GetMethod(nameof(CreateUntypedDelegate), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(s_binaryFormatterType)
                        .Invoke(null, new object[] { binaryFormatterDeserialize })!;
            });
#pragma warning restore CS8634

            _binaryFormatter = Activator.CreateInstance(s_binaryFormatterType!)!;
        }

        // generic method that we specialize at runtime once we've loaded the BinaryFormatter type
        // permits creating an unbound delegate so that we can avoid reflection after the initial
        // lightup code completes.
        private static Func<object, Stream, object> CreateUntypedDelegate<TInstance>(MethodInfo method)
        {
            Func<TInstance, Stream, object> typedDelegate = (Func<TInstance, Stream, object>)Delegate.CreateDelegate(typeof(Func<TInstance, Stream, object>), null, method);

            return (obj, stream) => typedDelegate((TInstance)obj, stream);
        }

        private bool ValidateReaderType(string readerType)
        {
            return ResourceManager.IsDefaultType(readerType, ResourceManager.ResReaderTypeName);
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
    }
}
