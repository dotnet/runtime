// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization
{
    using System;
    using System.Collections;
    using System.IO;
    using System.ComponentModel;
    using System.Xml.Schema;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Reflection;
    using System.Globalization;
    using System.Diagnostics;
    using System.Xml.Serialization.Advanced;

    /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlCodeExporter : CodeExporter
    {
        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.XmlCodeExporter"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal XmlCodeExporter(CodeNamespace codeNamespace) : base(codeNamespace, null, null, CodeGenerationOptions.GenerateProperties, null) { }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.XmlCodeExporter1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal XmlCodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit) : base(codeNamespace, codeCompileUnit, null, CodeGenerationOptions.GenerateProperties, null) { }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.XmlCodeExporter2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal XmlCodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit, CodeGenerationOptions options)
            : base(codeNamespace, codeCompileUnit, null, options, null)
        { }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.XmlCodeExporter3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal XmlCodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit, CodeGenerationOptions options, Hashtable mappings)
            : base(codeNamespace, codeCompileUnit, null, options, mappings)
        { }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.XmlCodeExporter4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal XmlCodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit, CodeDomProvider codeProvider, CodeGenerationOptions options, Hashtable mappings)
            : base(codeNamespace, codeCompileUnit, codeProvider, options, mappings)
        { }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.ExportTypeMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void ExportTypeMapping(XmlTypeMapping xmlTypeMapping)
        {
            xmlTypeMapping.CheckShallow();
            CheckScope(xmlTypeMapping.Scope);
            if (xmlTypeMapping.Accessor.Any) throw new InvalidOperationException(SR.XmlIllegalWildcard);

            ExportElement(xmlTypeMapping.Accessor);
        }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.ExportMembersMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void ExportMembersMapping(XmlMembersMapping xmlMembersMapping)
        {
            xmlMembersMapping.CheckShallow();
            CheckScope(xmlMembersMapping.Scope);

            for (int i = 0; i < xmlMembersMapping.Count; i++)
            {
                AccessorMapping mapping = xmlMembersMapping[i].Mapping;
                if (mapping.Xmlns == null)
                {
                    if (mapping.Attribute != null)
                    {
                        ExportType(mapping.Attribute.Mapping, Accessor.UnescapeName(mapping.Attribute.Name), mapping.Attribute.Namespace, null, false);
                    }
                    if (mapping.Elements != null)
                    {
                        for (int j = 0; j < mapping.Elements.Length; j++)
                        {
                            ElementAccessor element = mapping.Elements[j];
                            ExportType(element.Mapping, Accessor.UnescapeName(element.Name), element.Namespace, null, false);
                        }
                    }
                    if (mapping.Text != null)
                    {
                        ExportType(mapping.Text.Mapping, Accessor.UnescapeName(mapping.Text.Name), mapping.Text.Namespace, null, false);
                    }
                }
            }
        }

        private void ExportElement(ElementAccessor element)
        {
            ExportType(element.Mapping, Accessor.UnescapeName(element.Name), element.Namespace, element, true);
        }

        private void ExportType(TypeMapping mapping, string ns)
        {
            ExportType(mapping, null, ns, null, true);
        }

        private void ExportType(TypeMapping mapping, string name, string ns, ElementAccessor rootElement, bool checkReference)
        {
            if (mapping.IsReference && mapping.Namespace != Soap.Encoding)
                return;

            if (mapping is StructMapping && checkReference && ((StructMapping)mapping).ReferencedByTopLevelElement && rootElement == null)
                return;

            if (mapping is ArrayMapping && rootElement != null && rootElement.IsTopLevelInSchema && ((ArrayMapping)mapping).TopLevelMapping != null)
            {
                mapping = ((ArrayMapping)mapping).TopLevelMapping;
            }

            CodeTypeDeclaration codeClass = null;

            if (ExportedMappings[mapping] == null)
            {
                ExportedMappings.Add(mapping, mapping);
                if (mapping.TypeDesc.IsMappedType)
                {
                    codeClass = mapping.TypeDesc.ExtendedType.ExportTypeDefinition(CodeNamespace, CodeCompileUnit);
                }
                else if (mapping is EnumMapping)
                {
                    codeClass = ExportEnum((EnumMapping)mapping, typeof(XmlEnumAttribute));
                }
                else if (mapping is StructMapping)
                {
                    codeClass = ExportStruct((StructMapping)mapping);
                }
                else if (mapping is ArrayMapping)
                {
                    EnsureTypesExported(((ArrayMapping)mapping).Elements, ns);
                }
                if (codeClass != null)
                {
                    if (!mapping.TypeDesc.IsMappedType)
                    {
                        // Add [GeneratedCodeAttribute(Tool=.., Version=..)]
                        codeClass.CustomAttributes.Add(GeneratedCodeAttribute);

                        //// Add [SerializableAttribute]
                        //codeClass.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(SerializableAttribute).FullName));

                        if (!codeClass.IsEnum)
                        {
                            // Add [DebuggerStepThrough]
                            codeClass.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(DebuggerStepThroughAttribute).FullName));
                            //// Add [DesignerCategory("code")]
                            //codeClass.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(DesignerCategoryAttribute).FullName, new CodeAttributeArgument[] { new CodeAttributeArgument(new CodePrimitiveExpression("code")) }));
                        }
                        AddTypeMetadata(codeClass.CustomAttributes, typeof(XmlTypeAttribute), mapping.TypeDesc.Name, Accessor.UnescapeName(mapping.TypeName), mapping.Namespace, mapping.IncludeInSchema);
                    }
                    else if (FindAttributeDeclaration(typeof(GeneratedCodeAttribute), codeClass.CustomAttributes) == null)
                    {
                        // Add [GeneratedCodeAttribute(Tool=.., Version=..)]
                        codeClass.CustomAttributes.Add(GeneratedCodeAttribute);
                    }
                    ExportedClasses.Add(mapping, codeClass);
                }
            }
            else
                codeClass = (CodeTypeDeclaration)ExportedClasses[mapping];

            if (codeClass != null && rootElement != null)
                AddRootMetadata(codeClass.CustomAttributes, mapping, name, ns, rootElement);
        }

        private void AddRootMetadata(CodeAttributeDeclarationCollection metadata, TypeMapping typeMapping, string name, string ns, ElementAccessor rootElement)
        {
            string rootAttrName = typeof(XmlRootAttribute).FullName;

            // check that we haven't already added a root attribute since we can only add one
            foreach (CodeAttributeDeclaration attr in metadata)
            {
                if (attr.Name == rootAttrName) return;
            }

            CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(rootAttrName);
            if (typeMapping.TypeDesc.Name != name)
            {
                attribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(name)));
            }
            if (ns != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(ns)));
            }
            if (typeMapping.TypeDesc != null && typeMapping.TypeDesc.IsAmbiguousDataType)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("DataType", new CodePrimitiveExpression(typeMapping.TypeDesc.DataType.Name)));
            }
            if ((object)(rootElement.IsNullable) != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("IsNullable", new CodePrimitiveExpression((bool)rootElement.IsNullable)));
            }
            metadata.Add(attribute);
        }

        private CodeAttributeArgument[] GetDefaultValueArguments(PrimitiveMapping mapping, object value, out CodeExpression initExpression)
        {
            initExpression = null;
            if (value == null) return null;

            CodeExpression valueExpression = null;
            CodeExpression typeofValue = null;
            Type type = value.GetType();
            CodeAttributeArgument[] arguments = null;

            if (mapping is EnumMapping)
            {
#if DEBUG
                    // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                    if (value.GetType() != typeof(string)) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Invalid enumeration type " + value.GetType().Name));
#endif

                if (((EnumMapping)mapping).IsFlags)
                {
                    string[] values = ((string)value).Split(null);
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (values[i].Length == 0) continue;
                        CodeExpression enumRef = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(mapping.TypeDesc.FullName), values[i]);
                        if (valueExpression != null)
                            valueExpression = new CodeBinaryOperatorExpression(valueExpression, CodeBinaryOperatorType.BitwiseOr, enumRef);
                        else
                            valueExpression = enumRef;
                    }
                }
                else
                {
                    valueExpression = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(mapping.TypeDesc.FullName), (string)value);
                }
                initExpression = valueExpression;
                arguments = new CodeAttributeArgument[] { new CodeAttributeArgument(valueExpression) };
            }
            else if (type == typeof(bool) ||
                type == typeof(Int32) ||
                type == typeof(string) ||
                type == typeof(double))
            {
                initExpression = valueExpression = new CodePrimitiveExpression(value);
                arguments = new CodeAttributeArgument[] { new CodeAttributeArgument(valueExpression) };
            }
            else if (type == typeof(Int16) ||
                type == typeof(Int64) ||
                type == typeof(float) ||
                type == typeof(byte) ||
                type == typeof(decimal))
            {
                valueExpression = new CodePrimitiveExpression(Convert.ToString(value, NumberFormatInfo.InvariantInfo));
                typeofValue = new CodeTypeOfExpression(type.FullName);
                arguments = new CodeAttributeArgument[] { new CodeAttributeArgument(typeofValue), new CodeAttributeArgument(valueExpression) };
                initExpression = new CodeCastExpression(type.FullName, new CodePrimitiveExpression(value));
            }
            else if (type == typeof(sbyte) ||
                type == typeof(UInt16) ||
                type == typeof(UInt32) ||
                type == typeof(UInt64))
            {
                // need to promote the non-CLS complient types

                value = PromoteType(type, value);

                valueExpression = new CodePrimitiveExpression(Convert.ToString(value, NumberFormatInfo.InvariantInfo));
                typeofValue = new CodeTypeOfExpression(type.FullName);
                arguments = new CodeAttributeArgument[] { new CodeAttributeArgument(typeofValue), new CodeAttributeArgument(valueExpression) };
                initExpression = new CodeCastExpression(type.FullName, new CodePrimitiveExpression(value));
            }
            else if (type == typeof(DateTime))
            {
                DateTime dt = (DateTime)value;
                string dtString;
                long ticks;
                if (mapping.TypeDesc.FormatterName == "Date")
                {
                    dtString = XmlCustomFormatter.FromDate(dt);
                    ticks = (new DateTime(dt.Year, dt.Month, dt.Day)).Ticks;
                }
                else if (mapping.TypeDesc.FormatterName == "Time")
                {
                    dtString = XmlCustomFormatter.FromDateTime(dt);
                    ticks = dt.Ticks;
                }
                else
                {
                    dtString = XmlCustomFormatter.FromDateTime(dt);
                    ticks = dt.Ticks;
                }
                valueExpression = new CodePrimitiveExpression(dtString);
                typeofValue = new CodeTypeOfExpression(type.FullName);
                arguments = new CodeAttributeArgument[] { new CodeAttributeArgument(typeofValue), new CodeAttributeArgument(valueExpression) };
                initExpression = new CodeObjectCreateExpression(new CodeTypeReference(typeof(DateTime)), new CodeExpression[] { new CodePrimitiveExpression(ticks) });
            }
            else if (type == typeof(Guid))
            {
                valueExpression = new CodePrimitiveExpression(Convert.ToString(value, NumberFormatInfo.InvariantInfo));
                typeofValue = new CodeTypeOfExpression(type.FullName);
                arguments = new CodeAttributeArgument[] { new CodeAttributeArgument(typeofValue), new CodeAttributeArgument(valueExpression) };
                initExpression = new CodeObjectCreateExpression(new CodeTypeReference(typeof(Guid)), new CodeExpression[] { valueExpression });
            }
            if (mapping.TypeDesc.FullName != type.ToString() && !(mapping is EnumMapping))
            {
                // generate cast
                initExpression = new CodeCastExpression(mapping.TypeDesc.FullName, initExpression);
            }
            return arguments;
        }

        private object ImportDefault(TypeMapping mapping, string defaultValue)
        {
            if (defaultValue == null)
                return null;

            if (mapping.IsList)
            {
                string[] vals = defaultValue.Trim().Split(null);

                // count all non-zero length values;
                int count = 0;
                for (int i = 0; i < vals.Length; i++)
                {
                    if (vals[i] != null && vals[i].Length > 0) count++;
                }

                object[] values = new object[count];
                count = 0;
                for (int i = 0; i < vals.Length; i++)
                {
                    if (vals[i] != null && vals[i].Length > 0)
                    {
                        values[count++] = ImportDefaultValue(mapping, vals[i]);
                    }
                }
                return values;
            }
            return ImportDefaultValue(mapping, defaultValue);
        }

        private object ImportDefaultValue(TypeMapping mapping, string defaultValue)
        {
            if (defaultValue == null)
                return null;
            if (!(mapping is PrimitiveMapping))
                return DBNull.Value;

            if (mapping is EnumMapping)
            {
                EnumMapping em = (EnumMapping)mapping;
                ConstantMapping[] c = em.Constants;

                if (em.IsFlags)
                {
                    Hashtable values = new Hashtable();
                    string[] names = new string[c.Length];
                    long[] ids = new long[c.Length];

                    for (int i = 0; i < c.Length; i++)
                    {
                        ids[i] = em.IsFlags ? 1L << i : (long)i;
                        names[i] = c[i].Name;
                        values.Add(c[i].Name, ids[i]);
                    }
                    // this validates the values
                    long val = XmlCustomFormatter.ToEnum(defaultValue, values, em.TypeName, true);
                    return XmlCustomFormatter.FromEnum(val, names, ids, em.TypeDesc.FullName);
                }
                else
                {
                    for (int i = 0; i < c.Length; i++)
                    {
                        if (c[i].XmlName == defaultValue)
                            return c[i].Name;
                    }
                }
                throw new InvalidOperationException(SR.Format(SR.XmlInvalidDefaultValue, defaultValue, em.TypeDesc.FullName));
            }

            // Primitive mapping
            PrimitiveMapping pm = (PrimitiveMapping)mapping;

            if (!pm.TypeDesc.HasCustomFormatter)
            {
                if (pm.TypeDesc.FormatterName == "String")
                    return defaultValue;
                if (pm.TypeDesc.FormatterName == "DateTime")
                    return XmlCustomFormatter.ToDateTime(defaultValue);

                Type formatter = typeof(XmlConvert);

                MethodInfo format = formatter.GetMethod("To" + pm.TypeDesc.FormatterName, new Type[] { typeof(string) });
                if (format != null)
                {
                    return format.Invoke(formatter, new Object[] { defaultValue });
                }
#if DEBUG
                Debug.WriteLineIf(DiagnosticsSwitches.XmlSerialization.TraceVerbose, "XmlSerialization::Failed to GetMethod " + formatter.Name + ".To" + pm.TypeDesc.FormatterName);
#endif
            }
            else
            {
                if (pm.TypeDesc.HasDefaultSupport)
                {
                    return XmlCustomFormatter.ToDefaultValue(defaultValue, pm.TypeDesc.FormatterName);
                }
            }
            return DBNull.Value;
        }

        private void AddDefaultValueAttribute(CodeMemberField field, CodeAttributeDeclarationCollection metadata, object defaultValue, TypeMapping mapping, CodeCommentStatementCollection comments, TypeDesc memberTypeDesc, Accessor accessor, CodeConstructor ctor)
        {
            string attributeName = accessor.IsFixed ? "fixed" : "default";
            if (!memberTypeDesc.HasDefaultSupport)
            {
                if (comments != null && defaultValue is string)
                {
                    DropDefaultAttribute(accessor, comments, memberTypeDesc.FullName);
                    // do not generate intializers for the user prefered types if they do not have default capability
                    AddWarningComment(comments, SR.Format(SR.XmlDropAttributeValue, attributeName, mapping.TypeName, defaultValue.ToString()));
                }
                return;
            }
            if (memberTypeDesc.IsArrayLike && accessor is ElementAccessor)
            {
                if (comments != null && defaultValue is string)
                {
                    DropDefaultAttribute(accessor, comments, memberTypeDesc.FullName);
                    // do not generate intializers for array-like types
                    AddWarningComment(comments, SR.Format(SR.XmlDropArrayAttributeValue, attributeName, defaultValue.ToString(), ((ElementAccessor)accessor).Name));
                }
                return;
            }
            if (mapping.TypeDesc.IsMappedType && field != null && defaultValue is string)
            {
                SchemaImporterExtension extension = mapping.TypeDesc.ExtendedType.Extension;
                CodeExpression init = extension.ImportDefaultValue((string)defaultValue, mapping.TypeDesc.FullName);

                if (init != null)
                {
                    if (ctor != null)
                    {
                        AddInitializationStatement(ctor, field, init);
                    }
                    else
                    {
                        field.InitExpression = extension.ImportDefaultValue((string)defaultValue, mapping.TypeDesc.FullName);
                    }
                }
                if (comments != null)
                {
                    DropDefaultAttribute(accessor, comments, mapping.TypeDesc.FullName);
                    if (init == null)
                    {
                        AddWarningComment(comments, SR.Format(SR.XmlNotKnownDefaultValue, extension.GetType().FullName, attributeName, (string)defaultValue, mapping.TypeName, mapping.Namespace));
                    }
                }
                return;
            }
            object value = null;
            if (defaultValue is string || defaultValue == null)
            {
                value = ImportDefault(mapping, (string)defaultValue);
            }
            if (value == null) return;
            if (!(mapping is PrimitiveMapping))
            {
                if (comments != null)
                {
                    DropDefaultAttribute(accessor, comments, memberTypeDesc.FullName);
                    AddWarningComment(comments, SR.Format(SR.XmlDropNonPrimitiveAttributeValue, attributeName, defaultValue.ToString()));
                }
                return;
            }
            PrimitiveMapping pm = (PrimitiveMapping)mapping;

            if (comments != null && !pm.TypeDesc.HasDefaultSupport && pm.TypeDesc.IsMappedType)
            {
                // do not generate intializers for the user prefered types if they do not have default capability
                DropDefaultAttribute(accessor, comments, pm.TypeDesc.FullName);
                return;
            }
            if (value == DBNull.Value)
            {
                if (comments != null)
                {
                    AddWarningComment(comments, SR.Format(SR.XmlDropAttributeValue, attributeName, pm.TypeName, defaultValue.ToString()));
                }
                return;
            }
            CodeAttributeArgument[] arguments = null;
            CodeExpression initExpression = null;

            if (pm.IsList)
            {
#if DEBUG
                    // use exception in the place of Debug.Assert to avoid throwing asserts from a server process such as aspnet_ewp.exe
                    if (value.GetType() != typeof(object[])) throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Default value for list should be object[], not " + value.GetType().Name));
#endif

                object[] vals = (object[])value;
                CodeExpression[] initializers = new CodeExpression[vals.Length];
                for (int i = 0; i < vals.Length; i++)
                {
                    GetDefaultValueArguments(pm, vals[i], out initializers[i]);
                }
                initExpression = new CodeArrayCreateExpression(field.Type, initializers);
            }
            else
            {
                arguments = GetDefaultValueArguments(pm, value, out initExpression);
            }

            if (field != null)
            {
                if (ctor != null)
                {
                    AddInitializationStatement(ctor, field, initExpression);
                }
                else
                {
                    field.InitExpression = initExpression;
                }
            }
            if (arguments != null && pm.TypeDesc.HasDefaultSupport && accessor.IsOptional && !accessor.IsFixed)
            {
                // Add [DefaultValueAttribute]
                CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(DefaultValueAttribute).FullName, arguments);
                metadata.Add(attribute);
            }
            else if (comments != null)
            {
                DropDefaultAttribute(accessor, comments, memberTypeDesc.FullName);
            }
        }

        private static void AddInitializationStatement(CodeConstructor ctor, CodeMemberField field, CodeExpression init)
        {
            CodeAssignStatement assign = new CodeAssignStatement();
            assign.Left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), field.Name);
            assign.Right = init;
            ctor.Statements.Add(assign);
        }

        private static void DropDefaultAttribute(Accessor accessor, CodeCommentStatementCollection comments, string type)
        {
            if (!accessor.IsFixed && accessor.IsOptional)
            {
                AddWarningComment(comments, SR.Format(SR.XmlDropDefaultAttribute, type));
            }
        }

        private CodeTypeDeclaration ExportStruct(StructMapping mapping)
        {
            if (mapping.TypeDesc.IsRoot)
            {
                ExportRoot(mapping, typeof(XmlIncludeAttribute));
                return null;
            }

            string className = mapping.TypeDesc.Name;
            string baseName = mapping.TypeDesc.BaseTypeDesc == null || mapping.TypeDesc.BaseTypeDesc.IsRoot ? string.Empty : mapping.TypeDesc.BaseTypeDesc.FullName;

            CodeTypeDeclaration codeClass = new CodeTypeDeclaration(className);
            codeClass.IsPartial = CodeProvider.Supports(GeneratorSupport.PartialTypes);
            codeClass.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));

            CodeNamespace.Types.Add(codeClass);

            CodeConstructor ctor = new CodeConstructor();
            ctor.Attributes = (ctor.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            codeClass.Members.Add(ctor);
            if (mapping.TypeDesc.IsAbstract)
            {
                ctor.Attributes |= MemberAttributes.Abstract;
            }

            if (baseName != null && baseName.Length > 0)
            {
                codeClass.BaseTypes.Add(baseName);
            }
            else
                AddPropertyChangedNotifier(codeClass);

            codeClass.TypeAttributes |= TypeAttributes.Public;
            if (mapping.TypeDesc.IsAbstract)
            {
                codeClass.TypeAttributes |= TypeAttributes.Abstract;
            }

            AddIncludeMetadata(codeClass.CustomAttributes, mapping, typeof(XmlIncludeAttribute));

            if (mapping.IsSequence)
            {
                int generatedSequence = 0;
                for (int i = 0; i < mapping.Members.Length; i++)
                {
                    MemberMapping member = mapping.Members[i];
                    if (member.IsParticle && member.SequenceId < 0)
                        member.SequenceId = generatedSequence++;
                }
            }

            if (GenerateProperties)
            {
                for (int i = 0; i < mapping.Members.Length; i++)
                {
                    ExportProperty(codeClass, mapping.Members[i], mapping.Namespace, mapping.Scope, ctor);
                }
            }
            else
            {
                for (int i = 0; i < mapping.Members.Length; i++)
                {
                    ExportMember(codeClass, mapping.Members[i], mapping.Namespace, ctor);
                }
            }

            for (int i = 0; i < mapping.Members.Length; i++)
            {
                if (mapping.Members[i].Xmlns != null)
                    continue;
                EnsureTypesExported(mapping.Members[i].Elements, mapping.Namespace);
                EnsureTypesExported(mapping.Members[i].Attribute, mapping.Namespace);
                EnsureTypesExported(mapping.Members[i].Text, mapping.Namespace);
            }

            if (mapping.BaseMapping != null)
                ExportType(mapping.BaseMapping, null, mapping.Namespace, null, false);

            ExportDerivedStructs(mapping);
            CodeGenerator.ValidateIdentifiers(codeClass);
            if (ctor.Statements.Count == 0) codeClass.Members.Remove(ctor);
            return codeClass;
        }

        internal override void ExportDerivedStructs(StructMapping mapping)
        {
            for (StructMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
                ExportType(derived, mapping.Namespace);
        }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.AddMappingMetadata"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal void AddMappingMetadata(CodeAttributeDeclarationCollection metadata, XmlTypeMapping mapping, string ns)
        {
            mapping.CheckShallow();
            CheckScope(mapping.Scope);
            // For struct or enum mappings, we generate the XmlRoot on the struct/class/enum.  For primitives 
            // or arrays, there is nowhere to generate the XmlRoot, so we generate it elsewhere (on the 
            // method for web services get/post). 
            if (mapping.Mapping is StructMapping || mapping.Mapping is EnumMapping) return;
            AddRootMetadata(metadata, mapping.Mapping, Accessor.UnescapeName(mapping.Accessor.Name), mapping.Accessor.Namespace, mapping.Accessor);
        }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.AddMappingMetadata1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal void AddMappingMetadata(CodeAttributeDeclarationCollection metadata, XmlMemberMapping member, string ns, bool forceUseMemberName)
        {
            AddMemberMetadata(null, metadata, member.Mapping, ns, forceUseMemberName, null, null);
        }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.AddMappingMetadata2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal void AddMappingMetadata(CodeAttributeDeclarationCollection metadata, XmlMemberMapping member, string ns)
        {
            AddMemberMetadata(null, metadata, member.Mapping, ns, false, null, null);
        }

        private void ExportArrayElements(CodeAttributeDeclarationCollection metadata, ArrayMapping array, string ns, TypeDesc elementTypeDesc, int nestingLevel)
        {
            for (int i = 0; i < array.Elements.Length; i++)
            {
                ElementAccessor arrayElement = array.Elements[i];
                TypeMapping elementMapping = arrayElement.Mapping;
                string elementName = Accessor.UnescapeName(arrayElement.Name);
                bool sameName = arrayElement.Mapping.TypeDesc.IsArray ? false : elementName == arrayElement.Mapping.TypeName;
                bool sameElementType = elementMapping.TypeDesc == elementTypeDesc;
                bool sameElementNs = arrayElement.Form == XmlSchemaForm.Unqualified || arrayElement.Namespace == ns;
                bool sameNullable = arrayElement.IsNullable == elementMapping.TypeDesc.IsNullable;
                bool defaultForm = arrayElement.Form != XmlSchemaForm.Unqualified;
                if (!sameName || !sameElementType || !sameElementNs || !sameNullable || !defaultForm || nestingLevel > 0)
                    ExportArrayItem(metadata, sameName ? null : elementName, sameElementNs ? null : arrayElement.Namespace, sameElementType ? null : elementMapping.TypeDesc, elementMapping.TypeDesc, arrayElement.IsNullable, defaultForm ? XmlSchemaForm.None : arrayElement.Form, nestingLevel);
                if (elementMapping is ArrayMapping)
                    ExportArrayElements(metadata, (ArrayMapping)elementMapping, ns, elementTypeDesc.ArrayElementTypeDesc, nestingLevel + 1);
            }
        }

        private void AddMemberMetadata(CodeMemberField field, CodeAttributeDeclarationCollection metadata, MemberMapping member, string ns, bool forceUseMemberName, CodeCommentStatementCollection comments, CodeConstructor ctor)
        {
            if (member.Xmlns != null)
            {
                CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(XmlNamespaceDeclarationsAttribute).FullName);
                metadata.Add(attribute);
            }
            else if (member.Attribute != null)
            {
                AttributeAccessor attribute = member.Attribute;
                if (attribute.Any)
                    ExportAnyAttribute(metadata);
                else
                {
                    TypeMapping mapping = (TypeMapping)attribute.Mapping;
                    string attrName = Accessor.UnescapeName(attribute.Name);
                    bool sameType = mapping.TypeDesc == member.TypeDesc ||
                        (member.TypeDesc.IsArrayLike && mapping.TypeDesc == member.TypeDesc.ArrayElementTypeDesc);
                    bool sameName = attrName == member.Name && !forceUseMemberName;
                    bool sameNs = attribute.Namespace == ns;
                    bool defaultForm = attribute.Form != XmlSchemaForm.Qualified;
                    ExportAttribute(metadata,
                        sameName ? null : attrName,
                        sameNs || defaultForm ? null : attribute.Namespace,
                        sameType ? null : mapping.TypeDesc,
                        mapping.TypeDesc,
                        defaultForm ? XmlSchemaForm.None : attribute.Form);

                    AddDefaultValueAttribute(field, metadata, attribute.Default, mapping, comments, member.TypeDesc, attribute, ctor);
                }
            }
            else
            {
                if (member.Text != null)
                {
                    TypeMapping mapping = (TypeMapping)member.Text.Mapping;
                    bool sameType = mapping.TypeDesc == member.TypeDesc ||
                        (member.TypeDesc.IsArrayLike && mapping.TypeDesc == member.TypeDesc.ArrayElementTypeDesc);
                    ExportText(metadata, sameType ? null : mapping.TypeDesc, mapping.TypeDesc.IsAmbiguousDataType ? mapping.TypeDesc.DataType.Name : null);
                }
                if (member.Elements.Length == 1)
                {
                    ElementAccessor element = member.Elements[0];
                    TypeMapping mapping = (TypeMapping)element.Mapping;
                    string elemName = Accessor.UnescapeName(element.Name);
                    bool sameName = ((elemName == member.Name) && !forceUseMemberName);
                    bool isArray = mapping is ArrayMapping;
                    bool sameNs = element.Namespace == ns;
                    bool defaultForm = element.Form != XmlSchemaForm.Unqualified;

                    if (element.Any)
                        ExportAnyElement(metadata, elemName, element.Namespace, member.SequenceId);
                    else if (isArray)
                    {
                        bool sameType = mapping.TypeDesc == member.TypeDesc;
                        ArrayMapping array = (ArrayMapping)mapping;
                        if (!sameName || !sameNs || element.IsNullable || !defaultForm || member.SequenceId != -1)
                            ExportArray(metadata, sameName ? null : elemName, sameNs ? null : element.Namespace, element.IsNullable, defaultForm ? XmlSchemaForm.None : element.Form, member.SequenceId);
                        else if (mapping.TypeDesc.ArrayElementTypeDesc == new TypeScope().GetTypeDesc(typeof(byte)))
                        {
                            // special case for byte[]. It can be a primitive (base64Binary or hexBinary), or it can
                            // be an array of bytes. Our default is primitive; specify [XmlArray] to get array behavior.
                            ExportArray(metadata, null, null, false, XmlSchemaForm.None, member.SequenceId);
                        }
                        ExportArrayElements(metadata, array, element.Namespace, member.TypeDesc.ArrayElementTypeDesc, 0);
                    }
                    else
                    {
                        bool sameType = mapping.TypeDesc == member.TypeDesc ||
                            (member.TypeDesc.IsArrayLike && mapping.TypeDesc == member.TypeDesc.ArrayElementTypeDesc);
                        if (member.TypeDesc.IsArrayLike)
                            sameName = false;
                        ExportElement(metadata, sameName ? null : elemName, sameNs ? null : element.Namespace, sameType ? null : mapping.TypeDesc, mapping.TypeDesc, element.IsNullable, defaultForm ? XmlSchemaForm.None : element.Form, member.SequenceId);
                    }
                    AddDefaultValueAttribute(field, metadata, element.Default, mapping, comments, member.TypeDesc, element, ctor);
                }
                else
                {
                    for (int i = 0; i < member.Elements.Length; i++)
                    {
                        ElementAccessor element = member.Elements[i];
                        string elemName = Accessor.UnescapeName(element.Name);
                        bool sameNs = element.Namespace == ns;
                        if (element.Any)
                            ExportAnyElement(metadata, elemName, element.Namespace, member.SequenceId);
                        else
                        {
                            bool defaultForm = element.Form != XmlSchemaForm.Unqualified;
                            ExportElement(metadata, elemName, sameNs ? null : element.Namespace, ((TypeMapping)element.Mapping).TypeDesc, ((TypeMapping)element.Mapping).TypeDesc, element.IsNullable, defaultForm ? XmlSchemaForm.None : element.Form, member.SequenceId);
                        }
                    }
                }
                if (member.ChoiceIdentifier != null)
                {
                    CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(XmlChoiceIdentifierAttribute).FullName);
                    attribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(member.ChoiceIdentifier.MemberName)));
                    metadata.Add(attribute);
                }
                if (member.Ignore)
                {
                    CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(XmlIgnoreAttribute).FullName);
                    metadata.Add(attribute);
                }
            }
        }

        private void ExportMember(CodeTypeDeclaration codeClass, MemberMapping member, string ns, CodeConstructor ctor)
        {
            string fieldType = member.GetTypeName(CodeProvider);
            CodeMemberField field = new CodeMemberField(fieldType, member.Name);
            field.Attributes = (field.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            field.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
            codeClass.Members.Add(field);
            AddMemberMetadata(field, field.CustomAttributes, member, ns, false, field.Comments, ctor);

            if (member.CheckSpecified != SpecifiedAccessor.None)
            {
                field = new CodeMemberField(typeof(bool).FullName, member.Name + "Specified");
                field.Attributes = (field.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                field.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
                CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(XmlIgnoreAttribute).FullName);
                field.CustomAttributes.Add(attribute);
                codeClass.Members.Add(field);
            }
        }

        private void ExportProperty(CodeTypeDeclaration codeClass, MemberMapping member, string ns, CodeIdentifiers memberScope, CodeConstructor ctor)
        {
            string fieldName = memberScope.AddUnique(MakeFieldName(member.Name), member);
            string fieldType = member.GetTypeName(CodeProvider);
            // need to create a private field
            CodeMemberField field = new CodeMemberField(fieldType, fieldName);
            field.Attributes = MemberAttributes.Private;
            codeClass.Members.Add(field);

            CodeMemberProperty prop = CreatePropertyDeclaration(field, member.Name, fieldType);
            prop.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
            AddMemberMetadata(field, prop.CustomAttributes, member, ns, false, prop.Comments, ctor);
            codeClass.Members.Add(prop);

            if (member.CheckSpecified != SpecifiedAccessor.None)
            {
                field = new CodeMemberField(typeof(bool).FullName, fieldName + "Specified");
                field.Attributes = MemberAttributes.Private;
                codeClass.Members.Add(field);

                prop = CreatePropertyDeclaration(field, member.Name + "Specified", typeof(bool).FullName);
                prop.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
                CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(XmlIgnoreAttribute).FullName);
                prop.CustomAttributes.Add(attribute);
                codeClass.Members.Add(prop);
            }
        }

        private void ExportText(CodeAttributeDeclarationCollection metadata, TypeDesc typeDesc, string dataType)
        {
            CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(XmlTextAttribute).FullName);
            if (typeDesc != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(typeDesc.FullName)));
            }
            if (dataType != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("DataType", new CodePrimitiveExpression(dataType)));
            }
            metadata.Add(attribute);
        }

        private void ExportAttribute(CodeAttributeDeclarationCollection metadata, string name, string ns, TypeDesc typeDesc, TypeDesc dataTypeDesc, XmlSchemaForm form)
        {
            ExportMetadata(metadata, typeof(XmlAttributeAttribute), name, ns, typeDesc, dataTypeDesc, null, form, 0, -1);
        }

        private void ExportArrayItem(CodeAttributeDeclarationCollection metadata, string name, string ns, TypeDesc typeDesc, TypeDesc dataTypeDesc, bool isNullable, XmlSchemaForm form, int nestingLevel)
        {
            ExportMetadata(metadata, typeof(XmlArrayItemAttribute), name, ns, typeDesc, dataTypeDesc, isNullable ? null : (object)false, form, nestingLevel, -1);
        }

        private void ExportElement(CodeAttributeDeclarationCollection metadata, string name, string ns, TypeDesc typeDesc, TypeDesc dataTypeDesc, bool isNullable, XmlSchemaForm form, int sequenceId)
        {
            ExportMetadata(metadata, typeof(XmlElementAttribute), name, ns, typeDesc, dataTypeDesc, isNullable ? (object)true : null, form, 0, sequenceId);
        }

        private void ExportArray(CodeAttributeDeclarationCollection metadata, string name, string ns, bool isNullable, XmlSchemaForm form, int sequenceId)
        {
            ExportMetadata(metadata, typeof(XmlArrayAttribute), name, ns, null, null, isNullable ? (object)true : null, form, 0, sequenceId);
        }

        private void ExportMetadata(CodeAttributeDeclarationCollection metadata, Type attributeType, string name, string ns, TypeDesc typeDesc, TypeDesc dataTypeDesc, object isNullable, XmlSchemaForm form, int nestingLevel, int sequenceId)
        {
            CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(attributeType.FullName);
            if (name != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(name)));
            }
            if (typeDesc != null)
            {
                if (isNullable != null && (bool)isNullable && typeDesc.IsValueType && !typeDesc.IsMappedType && CodeProvider.Supports(GeneratorSupport.GenericTypeReference))
                {
                    attribute.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression("System.Nullable`1[" + typeDesc.FullName + "]")));
                    isNullable = null;
                }
                else
                {
                    attribute.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(typeDesc.FullName)));
                }
            }
            if (form != XmlSchemaForm.None)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Form", new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(XmlSchemaForm).FullName), Enum.Format(typeof(XmlSchemaForm), form, "G"))));

                if (form == XmlSchemaForm.Unqualified && ns != null && ns.Length == 0)
                {
                    ns = null;
                }
            }
            if (ns != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(ns)));
            }
            if (dataTypeDesc != null && dataTypeDesc.IsAmbiguousDataType && !dataTypeDesc.IsMappedType)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("DataType", new CodePrimitiveExpression(dataTypeDesc.DataType.Name)));
            }
            if (isNullable != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("IsNullable", new CodePrimitiveExpression((bool)isNullable)));
            }
            if (nestingLevel > 0)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("NestingLevel", new CodePrimitiveExpression(nestingLevel)));
            }
            if (sequenceId >= 0)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Order", new CodePrimitiveExpression(sequenceId)));
            }
            if (attribute.Arguments.Count == 0 && attributeType == typeof(XmlElementAttribute)) return;
            metadata.Add(attribute);
        }

        private void ExportAnyElement(CodeAttributeDeclarationCollection metadata, string name, string ns, int sequenceId)
        {
            CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(XmlAnyElementAttribute).FullName);
            if (name != null && name.Length > 0)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Name", new CodePrimitiveExpression(name)));
            }
            if (ns != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(ns)));
            }
            if (sequenceId >= 0)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Order", new CodePrimitiveExpression(sequenceId)));
            }
            metadata.Add(attribute);
        }

        private void ExportAnyAttribute(CodeAttributeDeclarationCollection metadata)
        {
            metadata.Add(new CodeAttributeDeclaration(typeof(XmlAnyAttributeAttribute).FullName));
        }

        internal override void EnsureTypesExported(Accessor[] accessors, string ns)
        {
            if (accessors == null) return;
            for (int i = 0; i < accessors.Length; i++)
                EnsureTypesExported(accessors[i], ns);
        }

        private void EnsureTypesExported(Accessor accessor, string ns)
        {
            if (accessor == null) return;
            ExportType(accessor.Mapping, null, ns, null, false);
        }
    }
}
