// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Reflection;

namespace System.Runtime.Serialization
{
    
    internal class SchemaExporter
    {
        private XmlSchemaSet _schemas;
        private DataContractSet _dataContractSet;
        //XmlDocument _xmlDoc;

        internal SchemaExporter(XmlSchemaSet schemas, DataContractSet dataContractSet)
        {
            _schemas = schemas;
            _dataContractSet = dataContractSet;
        }

        internal void Export()
        {
            throw new PlatformNotSupportedException();
        }

        internal static void GetXmlTypeInfo(Type type, out XmlQualifiedName stableName, out XmlSchemaType xsdType, out bool hasRoot)
        {
            if (IsSpecialXmlType(type, out stableName, out xsdType, out hasRoot))
                return;
            XmlSchemaSet schemas = null;
            InvokeSchemaProviderMethod(type, schemas, out stableName, out xsdType, out hasRoot);
            if (stableName.Name == null || stableName.Name.Length == 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidXmlDataContractName, DataContract.GetClrTypeFullName(type))));
        }

        private static bool InvokeSchemaProviderMethod(Type clrType, XmlSchemaSet schemas, out XmlQualifiedName stableName, out XmlSchemaType xsdType, out bool hasRoot)
        {
            xsdType = null;
            hasRoot = true;
            object[] attrs = clrType.GetTypeInfo().GetCustomAttributes(Globals.TypeOfXmlSchemaProviderAttribute, false).ToArray();
            if (attrs == null || attrs.Length == 0)
            {
                stableName = DataContract.GetDefaultStableName(clrType);
                return false;
            }

            XmlSchemaProviderAttribute provider = (XmlSchemaProviderAttribute)attrs[0];
            if (provider.IsAny)
            {
                xsdType = CreateAnyElementType();
                hasRoot = false;
            }
            string methodName = provider.MethodName;
            if (methodName == null || methodName.Length == 0)
            {
                if (!provider.IsAny)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidGetSchemaMethod, DataContract.GetClrTypeFullName(clrType))));
                stableName = DataContract.GetDefaultStableName(clrType);
            }
            else
            {
                MethodInfo getMethod = clrType.GetMethod(methodName,  /*BindingFlags.DeclaredOnly |*/ BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, new Type[] { typeof(XmlSchemaSet) });
                if (getMethod == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.MissingGetSchemaMethod, DataContract.GetClrTypeFullName(clrType), methodName)));

                if (!(Globals.TypeOfXmlQualifiedName.IsAssignableFrom(getMethod.ReturnType)))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidReturnTypeOnGetSchemaMethod, DataContract.GetClrTypeFullName(clrType), methodName, DataContract.GetClrTypeFullName(getMethod.ReturnType), DataContract.GetClrTypeFullName(Globals.TypeOfXmlQualifiedName))));

                object typeInfo = getMethod.Invoke(null, new object[] { schemas });

                if (provider.IsAny)
                {
                    if (typeInfo != null)
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidDataContractException(SR.Format(SR.InvalidNonNullReturnValueByIsAny, DataContract.GetClrTypeFullName(clrType), methodName)));
                    stableName = DataContract.GetDefaultStableName(clrType);
                }
                else if (typeInfo == null)
                {
                    xsdType = CreateAnyElementType();
                    hasRoot = false;
                    stableName = DataContract.GetDefaultStableName(clrType);
                }
                else
                {
                    stableName = (XmlQualifiedName)typeInfo;
                }
            }
            return true;
        }

        internal static void AddDefaultXmlType(XmlSchemaSet schemas, string localName, string ns)
        {
            XmlSchemaComplexType defaultXmlType = CreateAnyType();
            defaultXmlType.Name = localName;
            XmlSchema schema = SchemaHelper.GetSchema(ns, schemas);
            schema.Items.Add(defaultXmlType);
            schemas.Reprocess(schema);
        }

        private static XmlSchemaComplexType CreateAnyElementType()
        {
            XmlSchemaComplexType anyElementType = new XmlSchemaComplexType();
            anyElementType.IsMixed = false;
            anyElementType.Particle = new XmlSchemaSequence();
            XmlSchemaAny any = new XmlSchemaAny();
            any.MinOccurs = 0;
            any.ProcessContents = XmlSchemaContentProcessing.Lax;
            ((XmlSchemaSequence)anyElementType.Particle).Items.Add(any);
            return anyElementType;
        }

        private static XmlSchemaComplexType CreateAnyType()
        {
            XmlSchemaComplexType anyType = new XmlSchemaComplexType();
            anyType.IsMixed = true;
            anyType.Particle = new XmlSchemaSequence();
            XmlSchemaAny any = new XmlSchemaAny();
            any.MinOccurs = 0;
            any.MaxOccurs = Decimal.MaxValue;
            any.ProcessContents = XmlSchemaContentProcessing.Lax;
            ((XmlSchemaSequence)anyType.Particle).Items.Add(any);
            anyType.AnyAttribute = new XmlSchemaAnyAttribute();
            return anyType;
        }

        internal static bool IsSpecialXmlType(Type type, out XmlQualifiedName typeName, out XmlSchemaType xsdType, out bool hasRoot)
        {
            xsdType = null;
            hasRoot = true;
            if (type == Globals.TypeOfXmlElement || type == Globals.TypeOfXmlNodeArray)
            {
                string name = null;
                if (type == Globals.TypeOfXmlElement)
                {
                    xsdType = CreateAnyElementType();
                    name = "XmlElement";
                    hasRoot = false;
                }
                else
                {
                    xsdType = CreateAnyType();
                    name = "ArrayOfXmlNode";
                    hasRoot = true;
                }
                typeName = new XmlQualifiedName(name, DataContract.GetDefaultStableNamespace(type));
                return true;
            }
            typeName = null;
            return false;
        }
    }
}

