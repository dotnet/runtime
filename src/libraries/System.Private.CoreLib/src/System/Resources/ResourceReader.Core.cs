// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace System.Resources
{
    public partial class ResourceReader
    {
        private readonly bool _permitDeserialization;  // can deserialize BinaryFormatted resources
        private object? _binaryFormatter; // binary formatter instance to use for deserializing

        // statics used to dynamically call into BinaryFormatter
        // When successfully located s_binaryFormatterType will point to the BinaryFormatter type
        // and s_deserializeMethod will point to an unbound delegate to the deserialize method.
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
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

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "InitializeBinaryFormatter will get trimmed out when AllowCustomResourceTypes is set to false. " +
            "When set to true, we will already throw a warning for this feature switch, so we suppress this one in order for" +
            "the user to only get one error.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "InitializeBinaryFormatter will get trimmed out when AllowCustomResourceTypes is set to false. " +
            "When set to true, we will already throw a warning for this feature switch, so we suppress this one in order for" +
            "the user to only get one error.")]
        private object DeserializeObject(int typeIndex)
        {
            if (!AllowCustomResourceTypes)
            {
                throw new NotSupportedException(SR.ResourceManager_ReflectionNotAllowed);
            }

            if (!_permitDeserialization)
            {
                throw new NotSupportedException(SR.NotSupported_ResourceObjectSerialization);
            }

            if (Volatile.Read(ref _binaryFormatter) is null)
            {
                if (!InitializeBinaryFormatter())
                {
                    // The linker trimmed away the BinaryFormatter implementation and we can't call into it.
                    // We'll throw an exception with the same text that BinaryFormatter would have thrown
                    // had we been able to call into it. Keep this resource string in sync with the same
                    // resource from the Formatters assembly.
                    throw new NotSupportedException(SR.BinaryFormatter_SerializationDisallowed);
                }
            }

            Type type = FindType(typeIndex);

            object graph = s_deserializeMethod!(_binaryFormatter, _store.BaseStream);

            // guard against corrupted resources
            if (graph.GetType() != type)
                throw new BadImageFormatException(SR.Format(SR.BadImageFormat_ResType_SerBlobMismatch, type.FullName, graph.GetType().FullName));

            return graph;
        }

        // Issue https://github.com/dotnet/runtime/issues/39290 tracks finding an alternative to BinaryFormatter
        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("The CustomResourceTypesSupport feature switch has been enabled for this app which is being trimmed. " +
            "Custom readers as well as custom objects on the resources file are not observable by the trimmer and so required assemblies, types and members may be removed.")]
        private bool InitializeBinaryFormatter()
        {
            // If BinaryFormatter support is disabled for the app, the linker will replace this entire
            // method body with "return false;", skipping all reflection code below.

            if (Volatile.Read(ref s_binaryFormatterType) is null || Volatile.Read(ref s_deserializeMethod) is null)
            {
                Type binaryFormatterType = Type.GetType("System.Runtime.Serialization.Formatters.Binary.BinaryFormatter, System.Runtime.Serialization.Formatters", throwOnError: true)!;
                MethodInfo? binaryFormatterDeserialize = binaryFormatterType.GetMethod("Deserialize", new[] { typeof(Stream) });
                Func<object?, Stream, object>? deserializeMethod = (Func<object?, Stream, object>?)
                    typeof(ResourceReader)
                        .GetMethod(nameof(CreateUntypedDelegate), BindingFlags.NonPublic | BindingFlags.Static)
                        ?.MakeGenericMethod(binaryFormatterType)
                        .Invoke(null, new[] { binaryFormatterDeserialize });

                Interlocked.CompareExchange(ref s_binaryFormatterType, binaryFormatterType, null);
                Interlocked.CompareExchange(ref s_deserializeMethod, deserializeMethod, null);
            }

            Volatile.Write(ref _binaryFormatter, Activator.CreateInstance(s_binaryFormatterType!));

            return s_deserializeMethod != null;
        }

        // generic method that we specialize at runtime once we've loaded the BinaryFormatter type
        // permits creating an unbound delegate so that we can avoid reflection after the initial
        // lightup code completes.
        private static Func<object, Stream, object> CreateUntypedDelegate<TInstance>(MethodInfo method)
        {
            Func<TInstance, Stream, object> typedDelegate = (Func<TInstance, Stream, object>)Delegate.CreateDelegate(typeof(Func<TInstance, Stream, object>), null, method);

            return (obj, stream) => typedDelegate((TInstance)obj, stream);
        }

        private static bool ValidateReaderType(string readerType)
        {
            return ResourceManager.IsDefaultType(readerType, ResourceManager.ResReaderTypeName);
        }

        public void GetResourceData(string resourceName, out string resourceType, out byte[] resourceData)
        {
            ArgumentNullException.ThrowIfNull(resourceName);

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
