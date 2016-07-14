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
    using System.Diagnostics;
    using System.Xml;

    /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="SoapCodeExporter"]/*' />
    /// <internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class SoapCodeExporter : CodeExporter
    {
        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="SoapCodeExporter.SoapCodeExporter"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal SoapCodeExporter(CodeNamespace codeNamespace) : base(codeNamespace, null, null, CodeGenerationOptions.GenerateProperties, null) { }
        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="SoapCodeExporter.SoapCodeExporter1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal SoapCodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit) : base(codeNamespace, codeCompileUnit, null, CodeGenerationOptions.GenerateProperties, null) { }

        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="SoapCodeExporter.SoapCodeExporter2"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal SoapCodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit, CodeGenerationOptions options) : base(codeNamespace, codeCompileUnit, null, CodeGenerationOptions.GenerateProperties, null) { }

        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.SoapCodeExporter3"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal SoapCodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit, CodeGenerationOptions options, Hashtable mappings)
            : base(codeNamespace, codeCompileUnit, null, options, mappings)
        { }

        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.SoapCodeExporter4"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal SoapCodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit, CodeDomProvider codeProvider, CodeGenerationOptions options, Hashtable mappings)
            : base(codeNamespace, codeCompileUnit, codeProvider, options, mappings)
        { }

        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="SoapCodeExporter.ExportTypeMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void ExportTypeMapping(XmlTypeMapping xmlTypeMapping)
        {
            xmlTypeMapping.CheckShallow();
            CheckScope(xmlTypeMapping.Scope);
            ExportElement(xmlTypeMapping.Accessor);
        }

        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="SoapCodeExporter.ExportMembersMapping"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void ExportMembersMapping(XmlMembersMapping xmlMembersMapping)
        {
            xmlMembersMapping.CheckShallow();
            CheckScope(xmlMembersMapping.Scope);
            for (int i = 0; i < xmlMembersMapping.Count; i++)
            {
                ExportElement((ElementAccessor)xmlMembersMapping[i].Accessor);
            }
        }

        private void ExportElement(ElementAccessor element)
        {
            ExportType(element.Mapping);
        }

        private void ExportType(TypeMapping mapping)
        {
            if (mapping.IsReference)
                return;
            if (ExportedMappings[mapping] == null)
            {
                CodeTypeDeclaration codeClass = null;
                ExportedMappings.Add(mapping, mapping);
                if (mapping is EnumMapping)
                {
                    codeClass = ExportEnum((EnumMapping)mapping, typeof(SoapEnumAttribute));
                }
                else if (mapping is StructMapping)
                {
                    codeClass = ExportStruct((StructMapping)mapping);
                }
                else if (mapping is ArrayMapping)
                {
                    EnsureTypesExported(((ArrayMapping)mapping).Elements, null);
                }
                if (codeClass != null)
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
                    AddTypeMetadata(codeClass.CustomAttributes, typeof(SoapTypeAttribute), mapping.TypeDesc.Name, Accessor.UnescapeName(mapping.TypeName), mapping.Namespace, mapping.IncludeInSchema);
                    ExportedClasses.Add(mapping, codeClass);
                }
            }
        }

        private CodeTypeDeclaration ExportStruct(StructMapping mapping)
        {
            if (mapping.TypeDesc.IsRoot)
            {
                ExportRoot(mapping, typeof(SoapIncludeAttribute));
                return null;
            }
            if (!mapping.IncludeInSchema)
                return null;

            string className = mapping.TypeDesc.Name;
            string baseName = mapping.TypeDesc.BaseTypeDesc == null ? string.Empty : mapping.TypeDesc.BaseTypeDesc.Name;

            CodeTypeDeclaration codeClass = new CodeTypeDeclaration(className);
            codeClass.IsPartial = CodeProvider.Supports(GeneratorSupport.PartialTypes);
            codeClass.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));

            CodeNamespace.Types.Add(codeClass);

            if (baseName != null && baseName.Length > 0)
            {
                codeClass.BaseTypes.Add(baseName);
            }
            else
                AddPropertyChangedNotifier(codeClass);

            codeClass.TypeAttributes |= TypeAttributes.Public;
            if (mapping.TypeDesc.IsAbstract)
                codeClass.TypeAttributes |= TypeAttributes.Abstract;

            CodeExporter.AddIncludeMetadata(codeClass.CustomAttributes, mapping, typeof(SoapIncludeAttribute));

            if (GenerateProperties)
            {
                for (int i = 0; i < mapping.Members.Length; i++)
                {
                    ExportProperty(codeClass, mapping.Members[i], mapping.Scope);
                }
            }
            else
            {
                for (int i = 0; i < mapping.Members.Length; i++)
                {
                    ExportMember(codeClass, mapping.Members[i]);
                }
            }
            for (int i = 0; i < mapping.Members.Length; i++)
            {
                EnsureTypesExported(mapping.Members[i].Elements, null);
            }

            if (mapping.BaseMapping != null)
                ExportType(mapping.BaseMapping);

            ExportDerivedStructs(mapping);
            CodeGenerator.ValidateIdentifiers(codeClass);
            return codeClass;
        }

        internal override void ExportDerivedStructs(StructMapping mapping)
        {
            for (StructMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
                ExportType(derived);
        }

        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="SoapCodeExporter.AddMappingMetadata"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal void AddMappingMetadata(CodeAttributeDeclarationCollection metadata, XmlMemberMapping member, bool forceUseMemberName)
        {
            AddMemberMetadata(metadata, member.Mapping, forceUseMemberName);
        }

        /// <include file='doc\SoapCodeExporter.uex' path='docs/doc[@for="SoapCodeExporter.AddMappingMetadata1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal void AddMappingMetadata(CodeAttributeDeclarationCollection metadata, XmlMemberMapping member)
        {
            AddMemberMetadata(metadata, member.Mapping, false);
        }

        private void AddElementMetadata(CodeAttributeDeclarationCollection metadata, string elementName, TypeDesc typeDesc, bool isNullable)
        {
            CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(SoapElementAttribute).FullName);
            if (elementName != null)
            {
                attribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(elementName)));
            }
            if (typeDesc != null && typeDesc.IsAmbiguousDataType)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("DataType", new CodePrimitiveExpression(typeDesc.DataType.Name)));
            }
            if (isNullable)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("IsNullable", new CodePrimitiveExpression(true)));
            }
            metadata.Add(attribute);
        }

        private void AddMemberMetadata(CodeAttributeDeclarationCollection metadata, MemberMapping member, bool forceUseMemberName)
        {
            if (member.Elements.Length == 0) return;
            ElementAccessor element = member.Elements[0];
            TypeMapping mapping = (TypeMapping)element.Mapping;
            string elemName = Accessor.UnescapeName(element.Name);
            bool sameName = ((elemName == member.Name) && !forceUseMemberName);

            if (!sameName || mapping.TypeDesc.IsAmbiguousDataType || element.IsNullable)
            {
                AddElementMetadata(metadata, sameName ? null : elemName, mapping.TypeDesc.IsAmbiguousDataType ? mapping.TypeDesc : null, element.IsNullable);
            }
        }

        private void ExportMember(CodeTypeDeclaration codeClass, MemberMapping member)
        {
            string fieldType = member.GetTypeName(CodeProvider);
            CodeMemberField field = new CodeMemberField(fieldType, member.Name);
            field.Attributes = (field.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
            field.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
            codeClass.Members.Add(field);
            AddMemberMetadata(field.CustomAttributes, member, false);

            if (member.CheckSpecified != SpecifiedAccessor.None)
            {
                field = new CodeMemberField(typeof(bool).FullName, member.Name + "Specified");
                field.Attributes = (field.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
                field.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
                CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(SoapIgnoreAttribute).FullName);
                field.CustomAttributes.Add(attribute);
                codeClass.Members.Add(field);
            }
        }

        private void ExportProperty(CodeTypeDeclaration codeClass, MemberMapping member, CodeIdentifiers memberScope)
        {
            string fieldName = memberScope.AddUnique(CodeExporter.MakeFieldName(member.Name), member);
            string fieldType = member.GetTypeName(CodeProvider);
            // need to create a private field
            CodeMemberField field = new CodeMemberField(fieldType, fieldName);
            field.Attributes = MemberAttributes.Private;
            codeClass.Members.Add(field);

            CodeMemberProperty prop = CreatePropertyDeclaration(field, member.Name, fieldType);
            prop.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
            AddMemberMetadata(prop.CustomAttributes, member, false);
            codeClass.Members.Add(prop);

            if (member.CheckSpecified != SpecifiedAccessor.None)
            {
                field = new CodeMemberField(typeof(bool).FullName, fieldName + "Specified");
                field.Attributes = MemberAttributes.Private;
                codeClass.Members.Add(field);

                prop = CreatePropertyDeclaration(field, member.Name + "Specified", typeof(bool).FullName);
                prop.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
                CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(typeof(SoapIgnoreAttribute).FullName);
                prop.CustomAttributes.Add(attribute);
                codeClass.Members.Add(prop);
            }
        }

        internal override void EnsureTypesExported(Accessor[] accessors, string ns)
        {
            if (accessors == null) return;
            for (int i = 0; i < accessors.Length; i++)
                ExportType(accessors[i].Mapping);
        }
    }
}
