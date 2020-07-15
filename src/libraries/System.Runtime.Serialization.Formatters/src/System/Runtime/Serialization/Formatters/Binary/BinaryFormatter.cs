// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.IO;

namespace System.Runtime.Serialization.Formatters.Binary
{
    public sealed class BinaryFormatter : IFormatter
    {
        private static readonly ConcurrentDictionary<Type, TypeInformation> s_typeNameCache = new ConcurrentDictionary<Type, TypeInformation>();

        internal ISurrogateSelector? _surrogates;
        internal StreamingContext _context;
        internal SerializationBinder? _binder;
        internal FormatterTypeStyle _typeFormat = FormatterTypeStyle.TypesAlways; // For version resiliency, always put out types
        internal FormatterAssemblyStyle _assemblyFormat = FormatterAssemblyStyle.Simple;
        internal TypeFilterLevel _securityLevel = TypeFilterLevel.Full;
        internal object[]? _crossAppDomainArray;

        public FormatterTypeStyle TypeFormat { get { return _typeFormat; } set { _typeFormat = value; } }
        public FormatterAssemblyStyle AssemblyFormat { get { return _assemblyFormat; } set { _assemblyFormat = value; } }
        public TypeFilterLevel FilterLevel { get { return _securityLevel; } set { _securityLevel = value; } }
        public ISurrogateSelector? SurrogateSelector { get { return _surrogates; } set { _surrogates = value; } }
        public SerializationBinder? Binder { get { return _binder; } set { _binder = value; } }
        public StreamingContext Context { get { return _context; } set { _context = value; } }

        public BinaryFormatter() : this(null, new StreamingContext(StreamingContextStates.All))
        {
        }

        public BinaryFormatter(ISurrogateSelector? selector, StreamingContext context)
        {
            _surrogates = selector;
            _context = context;
        }

        public object Deserialize(Stream serializationStream)
        {
            // don't refactor the 'throw' into a helper method; linker will have difficulty trimming
            if (!SerializationInfo.BinaryFormatterEnabled)
            {
                throw CreateBinaryFormatterDisallowedException();
            }

            if (serializationStream == null)
            {
                throw new ArgumentNullException(nameof(serializationStream));
            }
            if (serializationStream.CanSeek && (serializationStream.Length == 0))
            {
                throw new SerializationException(SR.Serialization_Stream);
            }

            var formatterEnums = new InternalFE()
            {
                _typeFormat = _typeFormat,
                _serializerTypeEnum = InternalSerializerTypeE.Binary,
                _assemblyFormat = _assemblyFormat,
                _securityLevel = _securityLevel,
            };

            var reader = new ObjectReader(serializationStream, _surrogates, _context, formatterEnums, _binder)
            {
                _crossAppDomainArray = _crossAppDomainArray
            };
            try
            {
                var parser = new BinaryParser(serializationStream, reader);
                return reader.Deserialize(parser);
            }
            catch (SerializationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SerializationException(SR.Serialization_CorruptedStream, e);
            }
        }

        public void Serialize(Stream serializationStream, object graph)
        {
            // don't refactor the 'throw' into a helper method; linker will have difficulty trimming
            if (!SerializationInfo.BinaryFormatterEnabled)
            {
                throw CreateBinaryFormatterDisallowedException();
            }

            if (serializationStream == null)
            {
                throw new ArgumentNullException(nameof(serializationStream));
            }

            var formatterEnums = new InternalFE()
            {
                _typeFormat = _typeFormat,
                _serializerTypeEnum = InternalSerializerTypeE.Binary,
                _assemblyFormat = _assemblyFormat,
            };

            var sow = new ObjectWriter(_surrogates, _context, formatterEnums, _binder);
            BinaryFormatterWriter binaryWriter = new BinaryFormatterWriter(serializationStream, sow, _typeFormat);
            sow.Serialize(graph, binaryWriter);
            _crossAppDomainArray = sow._crossAppDomainArray;
        }

        internal static TypeInformation GetTypeInformation(Type type) =>
            s_typeNameCache.GetOrAdd(type, t =>
            {
                string assemblyName = FormatterServices.GetClrAssemblyName(t, out bool hasTypeForwardedFrom);
                return new TypeInformation(FormatterServices.GetClrTypeFullName(t), assemblyName, hasTypeForwardedFrom);
            });

        private static Exception CreateBinaryFormatterDisallowedException()
        {
            // If we're in an environment where BinaryFormatter never has a
            // chance of succeeding, use PNSE. Otherwise use regular NSE.

            string message = SR.BinaryFormatter_SerializationDisallowed;

#if BINARYFORMATTER_PNSE
            throw new PlatformNotSupportedException(message);
#else
            throw new NotSupportedException(message);
#endif
        }
    }
}
