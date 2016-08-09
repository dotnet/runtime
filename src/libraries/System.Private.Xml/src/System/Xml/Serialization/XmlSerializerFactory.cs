// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System.Reflection;
    using System.Collections;
    using System.IO;
    using System.Xml.Schema;
    using System;
    using System.Text;
    using System.Threading;
    using System.Globalization;
    using System.Security.Policy;
    using System.Xml.Serialization.Configuration;
    using System.Diagnostics;
    using System.CodeDom.Compiler;


    /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSerializerFactory
    {
        private static TempAssemblyCache s_cache = new TempAssemblyCache();

        /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory.CreateSerializer"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace)
        {
            return CreateSerializer(type, overrides, extraTypes, root, defaultNamespace, null);
        }

        /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory.CreateSerializer2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer CreateSerializer(Type type, XmlRootAttribute root)
        {
            return CreateSerializer(type, null, new Type[0], root, null, null);
        }

        /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory.CreateSerializer3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer CreateSerializer(Type type, Type[] extraTypes)
        {
            return CreateSerializer(type, null, extraTypes, null, null, null);
        }

        /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory.CreateSerializer4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides overrides)
        {
            return CreateSerializer(type, overrides, new Type[0], null, null, null);
        }

        /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory.CreateSerializer5"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer CreateSerializer(XmlTypeMapping xmlTypeMapping)
        {
            TempAssembly tempAssembly = XmlSerializer.GenerateTempAssembly(xmlTypeMapping);
            return (XmlSerializer)tempAssembly.Contract.TypedSerializers[xmlTypeMapping.Key];
        }

        /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory.CreateSerializer6"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer CreateSerializer(Type type)
        {
            return CreateSerializer(type, (string)null);
        }

        /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory.CreateSerializer1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSerializer CreateSerializer(Type type, string defaultNamespace)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            TempAssembly tempAssembly = s_cache[defaultNamespace, type];
            XmlTypeMapping mapping = null;
            if (tempAssembly == null)
            {
                lock (s_cache)
                {
                    tempAssembly = s_cache[defaultNamespace, type];
                    if (tempAssembly == null)
                    {
                        XmlSerializerImplementation contract;
                        Assembly assembly = TempAssembly.LoadGeneratedAssembly(type, defaultNamespace, out contract);
                        if (assembly == null)
                        {
                            // need to reflect and generate new serialization assembly
                            XmlReflectionImporter importer = new XmlReflectionImporter(defaultNamespace);
                            mapping = importer.ImportTypeMapping(type, null, defaultNamespace);
                            tempAssembly = XmlSerializer.GenerateTempAssembly(mapping, type, defaultNamespace);
                        }
                        else
                        {
                            tempAssembly = new TempAssembly(contract);
                        }
                        s_cache.Add(defaultNamespace, type, tempAssembly);
                    }
                }
            }
            if (mapping == null)
            {
                mapping = XmlReflectionImporter.GetTopLevelMapping(type, defaultNamespace);
            }
            return (XmlSerializer)tempAssembly.Contract.GetSerializer(type);
        }

        public XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace, string location)
        {
#pragma warning disable 618 // Passing through null evidence to centralize the CreateSerializer implementation
            return CreateSerializer(type, overrides, extraTypes, root, defaultNamespace, location, null);
#pragma warning restore 618
        }

        /// <include file='doc\XmlSerializerFactory.uex' path='docs/doc[@for="XmlSerializerFactory.CreateSerializer7"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        [Obsolete("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateSerializer which does not take an Evidence parameter. See http://go2.microsoft.com/fwlink/?LinkId=131738 for more information.")]
        internal XmlSerializer CreateSerializer(Type type, XmlAttributeOverrides overrides, Type[] extraTypes, XmlRootAttribute root, string defaultNamespace, string location, Evidence evidence)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (location != null || evidence != null)
            {
                DemandForUserLocationOrEvidence();
            }

            XmlReflectionImporter importer = new XmlReflectionImporter(overrides, defaultNamespace);
            for (int i = 0; i < extraTypes.Length; i++)
                importer.IncludeType(extraTypes[i]);
            XmlTypeMapping mapping = importer.ImportTypeMapping(type, root, defaultNamespace);
            TempAssembly tempAssembly = XmlSerializer.GenerateTempAssembly(mapping, type, defaultNamespace, location, evidence);
            return (XmlSerializer)tempAssembly.Contract.TypedSerializers[mapping.Key];
        }

        private void DemandForUserLocationOrEvidence()
        {
            // Ensure full trust before asserting full file access to the user-provided location or evidence
        }
    }
}
