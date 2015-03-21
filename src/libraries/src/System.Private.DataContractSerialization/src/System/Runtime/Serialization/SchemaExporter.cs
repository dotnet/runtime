// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------
//------------------------------------------------------------

namespace System.Runtime.Serialization
{
    using System;
    using System.Linq;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using System.Reflection;
    using XmlSchemaType = System.Object;
    using XmlSchemaComplexType = System.Object;

    internal class SchemaExporter
    {
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


        private static XmlSchemaComplexType CreateAnyElementType()
        {
            return new Object();
        }

        internal static bool IsSpecialXmlType(Type type, out XmlQualifiedName typeName, out XmlSchemaType xsdType, out bool hasRoot)
        {
            xsdType = null;
            hasRoot = true;
            typeName = null;
            return false;
        }
    }
}

