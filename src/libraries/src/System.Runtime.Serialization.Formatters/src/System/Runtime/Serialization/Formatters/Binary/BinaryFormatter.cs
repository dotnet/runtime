// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization.Formatters.Binary
{
    public sealed class BinaryFormatter : IFormatter
    {
        private static readonly Dictionary<Type, TypeInformation> s_typeNameCache = new Dictionary<Type, TypeInformation>();

        internal ISurrogateSelector _surrogates;
        internal StreamingContext _context;
        internal SerializationBinder _binder;
        internal FormatterTypeStyle _typeFormat = FormatterTypeStyle.TypesAlways; // For version resiliency, always put out types
        internal FormatterAssemblyStyle _assemblyFormat = FormatterAssemblyStyle.Simple;
        internal TypeFilterLevel _securityLevel = TypeFilterLevel.Full;
        internal object[] _crossAppDomainArray = null;

        public FormatterTypeStyle TypeFormat { get { return _typeFormat; } set { _typeFormat = value; } }
        public FormatterAssemblyStyle AssemblyFormat { get { return _assemblyFormat; } set { _assemblyFormat = value; } }
        public TypeFilterLevel FilterLevel { get { return _securityLevel; } set { _securityLevel = value; } }
        public ISurrogateSelector SurrogateSelector { get { return _surrogates; } set { _surrogates = value; } }
        public SerializationBinder Binder { get { return _binder; } set { _binder = value; } }
        public StreamingContext Context { get { return _context; } set { _context = value; } }

        public BinaryFormatter() : this(null, new StreamingContext(StreamingContextStates.All))
        {
        }

        public BinaryFormatter(ISurrogateSelector selector, StreamingContext context)
        {
            _surrogates = selector;
            _context = context;
        }

        public object Deserialize(Stream serializationStream) => Deserialize(serializationStream, true);

        internal object Deserialize(Stream serializationStream, bool check)
        {
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
            var parser = new BinaryParser(serializationStream, reader);
            return reader.Deserialize(parser, check);
        }
        public void Serialize(Stream serializationStream, object graph) => 
            Serialize(serializationStream, graph, true);

        internal void Serialize(Stream serializationStream, object graph, bool check)
        {
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
            sow.Serialize(graph, binaryWriter, check);
            _crossAppDomainArray = sow._crossAppDomainArray;
        }


        internal static TypeInformation GetTypeInformation(Type type)
        {
            lock (s_typeNameCache)
            {
                TypeInformation typeInformation;
                if (!s_typeNameCache.TryGetValue(type, out typeInformation))
                {
                    bool hasTypeForwardedFrom;
                    string assemblyName = FormatterServices.GetClrAssemblyName(type, out hasTypeForwardedFrom);
                    typeInformation = new TypeInformation(FormatterServices.GetClrTypeFullName(type), assemblyName, hasTypeForwardedFrom);
                    s_typeNameCache.Add(type, typeInformation);
                }
                return typeInformation;
            }
        }
    }
}
