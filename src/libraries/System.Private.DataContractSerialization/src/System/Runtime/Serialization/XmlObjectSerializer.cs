// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.DataContracts;
using System.Security;
using System.Text;
using System.Xml;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContracts.DataContract>;

namespace System.Runtime.Serialization
{
    public abstract class XmlObjectSerializer
    {
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public abstract void WriteStartObject(XmlDictionaryWriter writer, object? graph);
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public abstract void WriteObjectContent(XmlDictionaryWriter writer, object? graph);
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public abstract void WriteEndObject(XmlDictionaryWriter writer);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual void WriteObject(Stream stream, object? graph)
        {
            ArgumentNullException.ThrowIfNull(stream);

            XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(stream, Encoding.UTF8, false /*ownsStream*/);
            WriteObject(writer, graph);
            writer.Flush();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual void WriteObject(XmlWriter writer, object? graph)
        {
            ArgumentNullException.ThrowIfNull(writer);

            WriteObject(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual void WriteStartObject(XmlWriter writer, object? graph)
        {
            ArgumentNullException.ThrowIfNull(writer);

            WriteStartObject(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual void WriteObjectContent(XmlWriter writer, object? graph)
        {
            ArgumentNullException.ThrowIfNull(writer);

            WriteObjectContent(XmlDictionaryWriter.CreateDictionaryWriter(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual void WriteEndObject(XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            WriteEndObject(XmlDictionaryWriter.CreateDictionaryWriter(writer));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual void WriteObject(XmlDictionaryWriter writer, object? graph)
        {
            WriteObjectHandleExceptions(new XmlWriterDelegator(writer), graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteObjectHandleExceptions(XmlWriterDelegator writer, object? graph)
        {
            WriteObjectHandleExceptions(writer, graph, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteObjectHandleExceptions(XmlWriterDelegator writer, object? graph, DataContractResolver? dataContractResolver)
        {
            ArgumentNullException.ThrowIfNull(writer);

            try
            {
                InternalWriteObject(writer, graph, dataContractResolver);
            }
            catch (XmlException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorSerializing, GetSerializeType(graph), ex), ex);
            }
            catch (FormatException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorSerializing, GetSerializeType(graph), ex), ex);
            }
        }

        internal virtual DataContractDictionary? KnownDataContracts
        {
            [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                return null;
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void InternalWriteObject(XmlWriterDelegator writer, object? graph)
        {
            WriteStartObject(writer.Writer, graph);
            WriteObjectContent(writer.Writer, graph);
            WriteEndObject(writer.Writer);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void InternalWriteObject(XmlWriterDelegator writer, object? graph, DataContractResolver? dataContractResolver)
        {
            InternalWriteObject(writer, graph);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void InternalWriteStartObject(XmlWriterDelegator writer, object? graph)
        {
            Debug.Fail("XmlObjectSerializer.InternalWriteStartObject should never get called");
            throw new NotSupportedException();
        }
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void InternalWriteObjectContent(XmlWriterDelegator writer, object? graph)
        {
            Debug.Fail("XmlObjectSerializer.InternalWriteObjectContent should never get called");
            throw new NotSupportedException();
        }
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual void InternalWriteEndObject(XmlWriterDelegator writer)
        {
            Debug.Fail("XmlObjectSerializer.InternalWriteEndObject should never get called");
            throw new NotSupportedException();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteStartObjectHandleExceptions(XmlWriterDelegator writer, object? graph)
        {
            ArgumentNullException.ThrowIfNull(writer);

            try
            {
                InternalWriteStartObject(writer, graph);
            }
            catch (XmlException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorWriteStartObject, GetSerializeType(graph), ex), ex);
            }
            catch (FormatException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorWriteStartObject, GetSerializeType(graph), ex), ex);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteObjectContentHandleExceptions(XmlWriterDelegator writer, object? graph)
        {
            ArgumentNullException.ThrowIfNull(writer);

            try
            {
                if (writer.WriteState != WriteState.Element)
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.XmlWriterMustBeInElement, writer.WriteState));
                InternalWriteObjectContent(writer, graph);
            }
            catch (XmlException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorSerializing, GetSerializeType(graph), ex), ex);
            }
            catch (FormatException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorSerializing, GetSerializeType(graph), ex), ex);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal void WriteEndObjectHandleExceptions(XmlWriterDelegator writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            try
            {
                InternalWriteEndObject(writer);
            }
            catch (XmlException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorWriteEndObject, null, ex), ex);
            }
            catch (FormatException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorWriteEndObject, null, ex), ex);
            }
        }

        internal static void WriteRootElement(XmlWriterDelegator writer, DataContract contract, XmlDictionaryString? name, XmlDictionaryString? ns, bool needsContractNsAtRoot)
        {
            if (name == null) // root name not set explicitly
            {
                if (!contract.HasRoot)
                    return;
                contract.WriteRootElement(writer, contract.TopLevelElementName!, contract.TopLevelElementNamespace);
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

        internal static bool CheckIfNeedsContractNsAtRoot(XmlDictionaryString? name, XmlDictionaryString? ns, DataContract contract)
        {
            if (name == null)
                return false;

            if (contract.IsBuiltInDataContract || !contract.CanContainReferences || contract.IsISerializable)
            {
                return false;
            }

            string? contractNs = XmlDictionaryString.GetString(contract.Namespace);
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual object? ReadObject(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            return ReadObject(XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual object? ReadObject(XmlReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return ReadObject(XmlDictionaryReader.CreateDictionaryReader(reader));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual object? ReadObject(XmlDictionaryReader reader)
        {
            return ReadObjectHandleExceptions(new XmlReaderDelegator(reader), true /*verifyObjectName*/);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual object? ReadObject(XmlReader reader, bool verifyObjectName)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return ReadObject(XmlDictionaryReader.CreateDictionaryReader(reader), verifyObjectName);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public abstract object? ReadObject(XmlDictionaryReader reader, bool verifyObjectName);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public virtual bool IsStartObject(XmlReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return IsStartObject(XmlDictionaryReader.CreateDictionaryReader(reader));
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public abstract bool IsStartObject(XmlDictionaryReader reader);

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual object? InternalReadObject(XmlReaderDelegator reader, bool verifyObjectName)
        {
            return ReadObject(reader.UnderlyingReader, verifyObjectName);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual object? InternalReadObject(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver? dataContractResolver)
        {
            return InternalReadObject(reader, verifyObjectName);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal virtual bool InternalIsStartObject(XmlReaderDelegator reader)
        {
            Debug.Fail("XmlObjectSerializer.InternalIsStartObject should never get called");
            throw new NotSupportedException();
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal object? ReadObjectHandleExceptions(XmlReaderDelegator reader, bool verifyObjectName)
        {
            return ReadObjectHandleExceptions(reader, verifyObjectName, null);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal object? ReadObjectHandleExceptions(XmlReaderDelegator reader, bool verifyObjectName, DataContractResolver? dataContractResolver)
        {
            ArgumentNullException.ThrowIfNull(reader);

            try
            {
                return InternalReadObject(reader, verifyObjectName, dataContractResolver);
            }
            catch (XmlException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorDeserializing, GetDeserializeType(), ex), ex);
            }
            catch (FormatException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorDeserializing, GetDeserializeType(), ex), ex);
            }
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool IsStartObjectHandleExceptions(XmlReaderDelegator reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            try
            {
                return InternalIsStartObject(reader);
            }
            catch (XmlException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorIsStartObject, GetDeserializeType(), ex), ex);
            }
            catch (FormatException ex)
            {
                throw XmlObjectSerializer.CreateSerializationException(GetTypeInfoError(SR.ErrorIsStartObject, GetDeserializeType(), ex), ex);
            }
        }

        internal static bool IsRootXmlAny(XmlDictionaryString? rootName, DataContract contract)
        {
            return (rootName == null) && !contract.HasRoot;
        }

        internal static bool IsStartElement(XmlReaderDelegator reader)
        {
            return (reader.MoveToElement() || reader.IsStartElement());
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal static bool IsRootElement(XmlReaderDelegator reader, DataContract contract, XmlDictionaryString? name, XmlDictionaryString? ns)
        {
            reader.MoveToElement();
            if (name != null) // root name set explicitly
            {
                return reader.IsStartElement(name, ns ?? XmlDictionaryString.Empty);
            }
            else
            {
                if (!contract.HasRoot)
                    return reader.IsStartElement();

                if (reader.IsStartElement(contract.TopLevelElementName!, contract.TopLevelElementNamespace!))
                    return true;

                ClassDataContract? classContract = contract as ClassDataContract;
                if (classContract != null)
                    classContract = classContract.BaseClassContract;
                while (classContract != null)
                {
                    if (reader.IsStartElement(classContract.TopLevelElementName!, classContract.TopLevelElementNamespace!))
                        return true;
                    classContract = classContract.BaseClassContract;
                }
                if (classContract == null)
                {
                    DataContract objectContract = PrimitiveDataContract.GetPrimitiveDataContract(Globals.TypeOfObject)!;
                    if (reader.IsStartElement(objectContract.TopLevelElementName!, objectContract.TopLevelElementNamespace!))
                        return true;
                }
                return false;
            }
        }

        internal static string TryAddLineInfo(XmlReaderDelegator reader, string errorMessage)
        {
            if (reader.HasLineInfo())
                return string.Create(CultureInfo.InvariantCulture, $"{SR.Format(SR.ErrorInLine, reader.LineNumber, reader.LinePosition)} {errorMessage}");
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
        internal static SerializationException CreateSerializationException(string errorMessage, Exception? innerException)
        {
            return new SerializationException(errorMessage, innerException);
        }
        internal static string GetTypeInfoError(string errorMessage, Type? type, Exception innerException)
        {
            string typeInfo = (type == null) ? string.Empty : SR.Format(SR.ErrorTypeInfo, DataContract.GetClrTypeFullName(type));
            string innerExceptionMessage = (innerException == null) ? string.Empty : innerException.Message;
            return SR.Format(errorMessage, typeInfo, innerExceptionMessage);
        }

        internal virtual Type? GetSerializeType(object? graph)
        {
            return graph?.GetType();
        }

        internal virtual Type? GetDeserializeType()
        {
            return null;
        }

#pragma warning disable SYSLIB0050 // IFormatterConverter is obsolete
        private static IFormatterConverter? s_formatterConverter;
        internal static IFormatterConverter FormatterConverter => s_formatterConverter ??= new FormatterConverter();
#pragma warning restore SYSLIB0050
    }
}
