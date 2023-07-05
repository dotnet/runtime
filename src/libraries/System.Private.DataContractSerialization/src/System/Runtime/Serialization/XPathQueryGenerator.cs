// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization
{


    public static class XPathQueryGenerator
    {
        private const string XPathSeparator = "/";
        private const string NsSeparator = ":";

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public static string CreateFromDataContractSerializer(Type type, MemberInfo[] pathToMember, out XmlNamespaceManager namespaces)
        {
            return CreateFromDataContractSerializer(type, pathToMember, null, out namespaces);
        }

        // Here you can provide your own root element Xpath which will replace the Xpath of the top level element
        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public static string CreateFromDataContractSerializer(Type type, MemberInfo[] pathToMember, StringBuilder? rootElementXpath, out XmlNamespaceManager namespaces)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(pathToMember);

            DataContract currentContract = DataContract.GetDataContract(type);
            ExportContext context;

            if (rootElementXpath == null)
            {
                context = new ExportContext(currentContract);
            }
            else
            {
                // use the provided xpath for top level element
                context = new ExportContext(rootElementXpath);
            }

            for (int pathToMemberIndex = 0; pathToMemberIndex < pathToMember.Length; pathToMemberIndex++)
            {
                currentContract = ProcessDataContract(currentContract, context, pathToMember[pathToMemberIndex]);
            }

            namespaces = context.Namespaces;
            return context.XPath;
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static DataContract ProcessDataContract(DataContract contract, ExportContext context, MemberInfo memberNode)
        {
            if (contract is ClassDataContract)
            {
                return ProcessClassDataContract((ClassDataContract)contract, context, memberNode);
            }
            throw XmlObjectSerializer.CreateSerializationException(SR.QueryGeneratorPathToMemberNotFound);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private static DataContract ProcessClassDataContract(ClassDataContract contract, ExportContext context, MemberInfo memberNode)
        {
            string prefix = context.SetNamespace(contract.Namespace!.Value);
            foreach (DataMember member in GetDataMembers(contract))
            {
                if (member.MemberInfo.Name == memberNode.Name && member.MemberInfo.DeclaringType!.IsAssignableFrom(memberNode.DeclaringType))
                {
                    context.WriteChildToContext(member, prefix);
                    return member.MemberTypeContract;
                }
            }
            throw XmlObjectSerializer.CreateSerializationException(SR.QueryGeneratorPathToMemberNotFound);
        }

        private static IEnumerable<DataMember> GetDataMembers(ClassDataContract contract)
        {
            if (contract.BaseClassContract != null)
            {
                foreach (DataMember baseClassMember in GetDataMembers(contract.BaseClassContract))
                {
                    yield return baseClassMember;
                }
            }
            if (contract.Members != null)
            {
                foreach (DataMember member in contract.Members)
                {
                    yield return member;
                }
            }
        }

        private sealed class ExportContext
        {
            private readonly XmlNamespaceManager _namespaces;
            private int _nextPrefix;
            private readonly StringBuilder _xPathBuilder;

            public ExportContext(DataContract rootContract)
            {
                _namespaces = new XmlNamespaceManager(new NameTable());
                string prefix = SetNamespace(rootContract.TopLevelElementNamespace!.Value);
                _xPathBuilder = new StringBuilder(XPathQueryGenerator.XPathSeparator + prefix + XPathQueryGenerator.NsSeparator + rootContract.TopLevelElementName!.Value);
            }

            public ExportContext(StringBuilder rootContractXPath)
            {
                _namespaces = new XmlNamespaceManager(new NameTable());
                _xPathBuilder = rootContractXPath;
            }

            public void WriteChildToContext(DataMember contextMember, string prefix)
            {
                _xPathBuilder.Append(XPathQueryGenerator.XPathSeparator + prefix + XPathQueryGenerator.NsSeparator + contextMember.Name);
            }

            public XmlNamespaceManager Namespaces
            {
                get
                {
                    return _namespaces;
                }
            }

            public string XPath
            {
                get
                {
                    return _xPathBuilder.ToString();
                }
            }

            public string SetNamespace(string ns)
            {
                string? prefix = _namespaces.LookupPrefix(ns);
                if (string.IsNullOrEmpty(prefix))
                {
                    prefix = "xg" + (_nextPrefix++).ToString(NumberFormatInfo.InvariantInfo);
                    Namespaces.AddNamespace(prefix, ns);
                }
                return prefix;
            }
        }
    }
}
