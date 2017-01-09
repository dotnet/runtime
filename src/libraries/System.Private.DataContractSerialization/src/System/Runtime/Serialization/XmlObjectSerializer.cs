// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Xml;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Security;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;

    public abstract class XmlObjectSerializer
    {
        public abstract void WriteStartObject(XmlDictionaryWriter writer, object graph);
        public abstract void WriteObjectContent(XmlDictionaryWriter writer, object graph);
        public abstract void WriteEndObject(XmlDictionaryWriter writer);

        public virtual void WriteObject(Stream stream, object graph)
        {
            CheckNull(stream, nameof(stream));
            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, false /*ownsStream*/);
            WriteObject(writer, graph);
            writer.Flush();
        }

        public virtual void WriteObject(XmlWriter writer, object graph)
        {
            CheckNull(writer, nameof(writer));
            WriteObject(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
        }

        public virtual void WriteStartObject(XmlWriter writer, object graph)
        {
            CheckNull(writer, nameof(writer));
            WriteStartObject(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
        }

        public virtual void WriteObjectContent(XmlWriter writer, object graph)
        {
            CheckNull(writer, nameof(writer));
            WriteObjectContent(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
        }

        public virtual void WriteEndObject(XmlWriter writer)
        {
            CheckNull(writer, nameof(writer));
            WriteEndObject(XmlDictionaryWriter.CreateDictionaryWriter(writer));
        }

        public virtual void WriteObject(XmlDictionaryWriter writer, object graph)
        {
            WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        internal void WriteObjectHandleExceptions(XmlWriterDelegator writer, object graph)
        {
            WriteObjectHandleExceptions(writer, graph, null);
        }

        internal void WriteObjectHandleExceptions(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
        {
            try
            {
                CheckNull(writer, nameof(writer));
                {
                    InternalWriteObject(writer, graph, dataContractResolver);
                }
            }
            catch (XmlException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorSerializing, GetSerializeType(graph), ex), ex));
            }
            catch (FormatException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorSerializing, GetSerializeType(graph), ex), ex));
            }
        }

        internal virtual DataContractDictionary KnownDataContracts
        {
            get
            {
                return null;
            }
        }

        internal virtual void InternalWriteObject(XmlWriterDelegator writer, object graph)
        {
            WriteStartObject(writer.Writer, graph);
            WriteObjectContent(writer.Writer, graph);
            WriteEndObject(writer.Writer);
        }

        internal virtual void InternalWriteObject(XmlWriterDelegator writer, object graph, DataContractResolver dataContractResolver)
        {
            InternalWriteObject(writer, graph);
        }

        internal virtual void InternalWriteStartObject(XmlWriterDelegator writer, object graph)
        {
            DiagnosticUtility.DebugAssert("XmlObjectSerializer.InternalWriteStartObject should never get called");
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }
        internal virtual void InternalWriteObjectContent(XmlWriterDelegator writer, object graph)
        {
            DiagnosticUtility.DebugAssert("XmlObjectSerializer.InternalWriteObjectContent should never get called");
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }
        internal virtual void InternalWriteEndObject(XmlWriterDelegator writer)
        {
            DiagnosticUtility.DebugAssert("XmlObjectSerializer.InternalWriteEndObject should never get called");
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        internal void WriteStartObjectHandleExceptions(XmlWriterDelegator writer, object graph)
        {
            try
            {
                CheckNull(writer, nameof(writer));
                InternalWriteStartObject(writer, graph);
            }
            catch (XmlException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorWriteStartObject, GetSerializeType(graph), ex), ex));
            }
            catch (FormatException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorWriteStartObject, GetSerializeType(graph), ex), ex));
            }
        }

        internal void WriteObjectContentHandleExceptions(XmlWriterDelegator writer, object graph)
        {
            try
            {
                CheckNull(writer, nameof(writer));
                {
                    if (writer.WriteState != WriteState.Element)
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.XmlWriterMustBeInElement, writer.WriteState)));
                    InternalWriteObjectContent(writer, graph);
                }
            }
            catch (XmlException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorSerializing, GetSerializeType(graph), ex), ex));
            }
            catch (FormatException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorSerializing, GetSerializeType(graph), ex), ex));
            }
        }

        internal void WriteEndObjectHandleExceptions(XmlWriterDelegator writer)
        {
            try
            {
                CheckNull(writer, nameof(writer));
                InternalWriteEndObject(writer);
            }
            catch (XmlException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorWriteEndObject, null, ex), ex));
            }
            catch (FormatException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorWriteEndObject, null, ex), ex));
            }
        }

        internal void WriteRootElement(XmlWriterDelegator writer, DataContract contract, XmlDictionaryString name, XmlDictionaryString ns, bool needsContractNsAtRoot)
        {
            if (name == null) // root name not set explicitly
            {
                if (!contract.HasRoot)
                    return;
                contract.WriteRootElement(writer, contract.TopLevelElementName, contract.TopLevelElementNamespace);
            }
            else
            {
                contract.WriteRootElement(writer, name, ns);
                if (needsContractNsAtRoot)
                {
                    writer.WriteNamespaceDecl(contract.Namespace);
                }
            }
        }

        internal bool CheckIfNeedsContractNsAtRoot(XmlDictionaryString name, XmlDictionaryString ns, DataContract contract)
        {
            if (name == null)
                return false;

            if (contract.IsBuiltInDataContract || !contract.CanContainReferences)
            {
                return false;
            }

            string contractNs = XmlDictionaryString.GetString(contract.Namespace);
            if (string.IsNullOrEmpty(contractNs) || contractNs == XmlDictionaryString.GetString(ns))
                return false;

            return true;
        }

        internal static void WriteNull(XmlWriterDelegator writer)
        {
            writer.WriteAttributeBool(Globals.XsiPrefix, DictionaryGlobals.XsiNilLocalName, DictionaryGlobals.SchemaInstanceNamespace, true);
        }

        internal static bool IsContractDeclared(DataContract contract, DataContract declaredContract)
        {
            return (object.ReferenceEquals(contract.Name, declaredContract.Name) && object.ReferenceEquals(contract.Namespace, declaredContract.Namespace))
                || (contract.Name.Value == declaredContract.Name.Value && contract.Namespace.Value == declaredContract.Namespace.Value);
        }

        public virtual object ReadObject(Stream stream)
        {
            CheckNull(stream, nameof(stream));
            return ReadObject(XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max));
        }

        public virtual object ReadObject(XmlReader reader)
        {
            CheckNull(reader, nameof(reader));
            return ReadObject(XmlDictionaryReader.CreateDictionaryReader(reader));
        }

        public virtual object ReadObject(XmlDictionaryReader reader)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), true /*verifyObjectName*/);
        }

        public virtual object ReadObject(XmlReader reader, bool verifyObjectName)
        {
            CheckNull(reader, nameof(reader));
            return ReadObject(XmlDictionaryReader.CreateDictionaryReader(reader), verifyObjectName);
        }

        public abstract object ReadObject(XmlDictionaryReader reader, bool verifyObjectName);

        public virtual bool IsStartObject(XmlReader reader)
        {
            CheckNull(reader, nameof(reader));
            return IsStartObject(XmlDictionaryReader.CreateDictionaryReader(reader));
        }

        public abstract bool IsStartObject(XmlDictionaryReader reader);

        internal virtual object InternalReadObject(XmlReaderDelegator reader, bool verifyObjectName)
        {
            return ReadObject(reader.UnderlyingReader, verifyObjectName);
        }

        internal virtual object InternalReadObject(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver dataContractResolver)
        {
            return InternalReadObject(reader, verifyObjectName);
        }

        internal virtual bool InternalIsStartObject(XmlReaderDelegator reader)
        {
            DiagnosticUtility.DebugAssert("XmlObjectSerializer.InternalIsStartObject should never get called");
            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException());
        }

        internal object ReadObjectHandleExceptions(XmlReaderDelegator reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(reader, verifyObjectName, null);
        }

        internal object ReadObjectHandleExceptions(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver dataContractResolver)
        {
            try
            {
                CheckNull(reader, nameof(reader));
                return InternalReadObject(reader, verifyObjectName, dataContractResolver);
            }
            catch (XmlException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorDeserializing, GetDeserializeType(), ex), ex));
            }
            catch (FormatException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorDeserializing, GetDeserializeType(), ex), ex));
            }
        }

        internal bool IsStartObjectHandleExceptions(XmlReaderDelegator reader)
        {
            try
            {
                CheckNull(reader, nameof(reader));
                return InternalIsStartObject(reader);
            }
            catch (XmlException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorIsStartObject, GetDeserializeType(), ex), ex));
            }
            catch (FormatException ex)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorIsStartObject, GetDeserializeType(), ex), ex));
            }
        }

        internal bool IsRootXmlAny(XmlDictionaryString rootName, DataContract contract)
        {
            return (rootName == null) && !contract.HasRoot;
        }

        internal bool IsStartElement(XmlReaderDelegator reader)
        {
            return (reader.MoveToElement() || reader.IsStartElement());
        }

        internal bool IsRootElement(XmlReaderDelegator reader, DataContract contract, XmlDictionaryString name, XmlDictionaryString ns)
        {
            reader.MoveToElement();
            if (name != null) // root name set explicitly
            {
                return reader.IsStartElement(name, ns);
            }
            else
            {
                if (!contract.HasRoot)
                    return reader.IsStartElement();

                if (reader.IsStartElement(contract.TopLevelElementName, contract.TopLevelElementNamespace))
                    return true;

                ClassDataContract classContract = contract as ClassDataContract;
                if (classContract != null)
                    classContract = classContract.BaseContract;
                while (classContract != null)
                {
                    if (reader.IsStartElement(classContract.TopLevelElementName, classContract.TopLevelElementNamespace))
                        return true;
                    classContract = classContract.BaseContract;
                }
                if (classContract == null)
                {
                    DataContract objectContract = PrimitiveDataContract.GetPrimitiveDataContract(Globals.TypeOfObject);
                    if (reader.IsStartElement(objectContract.TopLevelElementName, objectContract.TopLevelElementNamespace))
                        return true;
                }
                return false;
            }
        }

        internal static void CheckNull(object obj, string name)
        {
            if (obj == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(name));
        }

        internal static string TryAddLineInfo(XmlReaderDelegator reader, string errorMessage)
        {
            if (reader.HasLineInfo())
                return String.Format(CultureInfo.InvariantCulture, "{0} {1}", SR.Format(SR.ErrorInLine, reader.LineNumber, reader.LinePosition), errorMessage);
            return errorMessage;
        }

        internal static Exception CreateSerializationExceptionWithReaderDetails(string errorMessage, XmlReaderDelegator reader)
        {
            return XmlObjectSerializer.CreateSerializationException(TryAddLineInfo(reader, SR.Format(SR.EncounteredWithNameNamespace, errorMessage, reader.NodeType, reader.LocalName, reader.NamespaceURI)));
        }

        internal static SerializationException CreateSerializationException(string errorMessage)
        {
            return XmlObjectSerializer.CreateSerializationException(errorMessage, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static SerializationException CreateSerializationException(string errorMessage, Exception innerException)
        {
            return new SerializationException(errorMessage, innerException);
        }
        internal static string GetTypeInfoError(string errorMessage, Type type, Exception innerException)
        {
            string typeInfo = (type == null) ? string.Empty : SR.Format(SR.ErrorTypeInfo, DataContract.GetClrTypeFullName(type));
            string innerExceptionMessage = (innerException == null) ? string.Empty : innerException.Message;
            return SR.Format(errorMessage, typeInfo, innerExceptionMessage);
        }

        internal virtual Type GetSerializeType(object graph)
        {
            return (graph == null) ? null : graph.GetType();
        }

        internal virtual Type GetDeserializeType()
        {
            return null;
        }

        private static IFormatterConverter s_formatterConverter;
        internal static IFormatterConverter FormatterConverter
        {
            get
            {
                if (s_formatterConverter == null)
                {
                    s_formatterConverter = new FormatterConverter();
                }

                return s_formatterConverter;
            }
        }
    }
}
