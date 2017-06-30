// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal class Attributes
    {
        private static XmlDictionaryString[] s_serializationLocalNames;

        private static XmlDictionaryString[] s_schemaInstanceLocalNames;

        static Attributes()
        {
            s_serializationLocalNames = new XmlDictionaryString[]
            {
                DictionaryGlobals.IdLocalName,
                DictionaryGlobals.ArraySizeLocalName,
                DictionaryGlobals.RefLocalName,
                DictionaryGlobals.ClrTypeLocalName,
                DictionaryGlobals.ClrAssemblyLocalName,
                DictionaryGlobals.ISerializableFactoryTypeLocalName
            };

            s_schemaInstanceLocalNames = new XmlDictionaryString[]
            {
                DictionaryGlobals.XsiNilLocalName,
                DictionaryGlobals.XsiTypeLocalName
            };
        }

        internal string Id;
        internal string Ref;
        internal string XsiTypeName;
        internal string XsiTypeNamespace;
        internal string XsiTypePrefix;
        internal bool XsiNil;
        internal string ClrAssembly;
        internal string ClrType;
        internal int ArraySZSize;
        internal string FactoryTypeName;
        internal string FactoryTypeNamespace;
        internal string FactoryTypePrefix;
        internal bool UnrecognizedAttributesFound;

        internal void Read(XmlReaderDelegator reader)
        {
            Reset();

            while (reader.MoveToNextAttribute())
            {
                switch (reader.IndexOfLocalName(s_serializationLocalNames, DictionaryGlobals.SerializationNamespace))
                {
                    case 0:
                        ReadId(reader);
                        break;
                    case 1:
                        ReadArraySize(reader);
                        break;
                    case 2:
                        ReadRef(reader);
                        break;
                    case 3:
                        ClrType = reader.Value;
                        break;
                    case 4:
                        ClrAssembly = reader.Value;
                        break;
                    case 5:
                        ReadFactoryType(reader);
                        break;
                    default:
                        switch (reader.IndexOfLocalName(s_schemaInstanceLocalNames, DictionaryGlobals.SchemaInstanceNamespace))
                        {
                            case 0:
                                ReadXsiNil(reader);
                                break;
                            case 1:
                                ReadXsiType(reader);
                                break;
                            default:
                                if (!reader.IsNamespaceUri(DictionaryGlobals.XmlnsNamespace))
                                    UnrecognizedAttributesFound = true;
                                break;
                        }
                        break;
                }
            }
            reader.MoveToElement();
        }

        internal void Reset()
        {
            Id = Globals.NewObjectId;
            Ref = Globals.NewObjectId;
            XsiTypeName = null;
            XsiTypeNamespace = null;
            XsiTypePrefix = null;
            XsiNil = false;
            ClrAssembly = null;
            ClrType = null;
            ArraySZSize = -1;
            FactoryTypeName = null;
            FactoryTypeNamespace = null;
            FactoryTypePrefix = null;
            UnrecognizedAttributesFound = false;
        }

        private void ReadId(XmlReaderDelegator reader)
        {
            Id = reader.ReadContentAsString();
            if (string.IsNullOrEmpty(Id))
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.InvalidXsIdDefinition, Id)));
            }
        }

        private void ReadRef(XmlReaderDelegator reader)
        {
            Ref = reader.ReadContentAsString();
            if (string.IsNullOrEmpty(Ref))
            {
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.InvalidXsRefDefinition, Ref)));
            }
        }

        private void ReadXsiNil(XmlReaderDelegator reader)
        {
            XsiNil = reader.ReadContentAsBoolean();
        }

        private void ReadArraySize(XmlReaderDelegator reader)
        {
            ArraySZSize = reader.ReadContentAsInt();
            if (ArraySZSize < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.InvalidSizeDefinition, ArraySZSize)));
        }

        private void ReadXsiType(XmlReaderDelegator reader)
        {
            string xsiTypeString = reader.Value;
            if (xsiTypeString != null && xsiTypeString.Length > 0)
                XmlObjectSerializerReadContext.ParseQualifiedName(xsiTypeString, reader, out XsiTypeName, out XsiTypeNamespace, out XsiTypePrefix);
        }

        private void ReadFactoryType(XmlReaderDelegator reader)
        {
            string factoryTypeString = reader.Value;
            if (factoryTypeString != null && factoryTypeString.Length > 0)
                XmlObjectSerializerReadContext.ParseQualifiedName(factoryTypeString, reader, out FactoryTypeName, out FactoryTypeNamespace, out FactoryTypePrefix);
        }
    }
}
