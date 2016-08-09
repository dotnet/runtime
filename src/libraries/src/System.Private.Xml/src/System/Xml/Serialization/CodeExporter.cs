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

    /// <include file='doc\CodeExporter.uex' path='docs/doc[@for="CodeExporter"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class CodeExporter
    {
        private Hashtable _exportedMappings;
        private Hashtable _exportedClasses; // TypeMapping -> CodeTypeDeclaration
        private CodeNamespace _codeNamespace;
        private CodeCompileUnit _codeCompileUnit;
        private bool _rootExported;
        private TypeScope _scope;
        private CodeAttributeDeclarationCollection _includeMetadata = new CodeAttributeDeclarationCollection();
        private CodeGenerationOptions _options;
        private CodeDomProvider _codeProvider;
        private CodeAttributeDeclaration _generatedCodeAttribute;

        internal CodeExporter(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit, CodeDomProvider codeProvider, CodeGenerationOptions options, Hashtable exportedMappings)
        {
            if (codeNamespace != null)
                CodeGenerator.ValidateIdentifiers(codeNamespace);
            _codeNamespace = codeNamespace;
            if (codeCompileUnit != null)
            {
                if (!codeCompileUnit.ReferencedAssemblies.Contains("System.dll"))
                    codeCompileUnit.ReferencedAssemblies.Add("System.dll");
                if (!codeCompileUnit.ReferencedAssemblies.Contains("System.Xml.dll"))
                    codeCompileUnit.ReferencedAssemblies.Add("System.Xml.dll");
            }
            _codeCompileUnit = codeCompileUnit;
            _options = options;
            _exportedMappings = exportedMappings;
            _codeProvider = codeProvider;
        }

        internal CodeCompileUnit CodeCompileUnit
        {
            get { return _codeCompileUnit; }
        }

        internal CodeNamespace CodeNamespace
        {
            get
            {
                if (_codeNamespace == null)
                    _codeNamespace = new CodeNamespace();
                return _codeNamespace;
            }
        }
        internal CodeDomProvider CodeProvider
        {
            get
            {
                if (_codeProvider == null)
                    _codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
                return _codeProvider;
            }
        }

        internal Hashtable ExportedClasses
        {
            get
            {
                if (_exportedClasses == null)
                    _exportedClasses = new Hashtable();
                return _exportedClasses;
            }
        }

        internal Hashtable ExportedMappings
        {
            get
            {
                if (_exportedMappings == null)
                    _exportedMappings = new Hashtable();
                return _exportedMappings;
            }
        }

        internal bool GenerateProperties
        {
            get { return (_options & CodeGenerationOptions.GenerateProperties) != 0; }
        }

        internal CodeAttributeDeclaration GeneratedCodeAttribute
        {
            get
            {
                //BinCompat TODO
                throw new NotImplementedException();
            }
        }

        internal static CodeAttributeDeclaration FindAttributeDeclaration(Type type, CodeAttributeDeclarationCollection metadata)
        {
            foreach (CodeAttributeDeclaration attribute in metadata)
            {
                if (attribute.Name == type.FullName || attribute.Name == type.Name)
                {
                    return attribute;
                }
            }
            return null;
        }

        /// <include file='doc\XmlCodeExporter.uex' path='docs/doc[@for="XmlCodeExporter.IncludeMetadata"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        internal CodeAttributeDeclarationCollection IncludeMetadata
        {
            get { return _includeMetadata; }
        }

        internal TypeScope Scope
        {
            get { return _scope; }
        }

        internal void CheckScope(TypeScope scope)
        {
            if (_scope == null)
            {
                _scope = scope;
            }
            else if (_scope != scope)
            {
                throw new InvalidOperationException(SR.XmlMappingsScopeMismatch);
            }
        }

        internal abstract void ExportDerivedStructs(StructMapping mapping);
        internal abstract void EnsureTypesExported(Accessor[] accessors, string ns);

        internal static void AddWarningComment(CodeCommentStatementCollection comments, string text)
        {
            Debug.Assert(comments != null);
            comments.Add(new CodeCommentStatement(SR.Format(SR.XmlCodegenWarningDetails, text), false));
        }

        internal void ExportRoot(StructMapping mapping, Type includeType)
        {
            if (!_rootExported)
            {
                _rootExported = true;
                ExportDerivedStructs(mapping);

                for (StructMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
                {
                    if (!derived.ReferencedByElement && derived.IncludeInSchema && !derived.IsAnonymousType)
                    {
                        CodeAttributeDeclaration include = new CodeAttributeDeclaration(includeType.FullName);
                        include.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(derived.TypeDesc.FullName)));
                        _includeMetadata.Add(include);
                    }
                }
                Hashtable typesIncluded = new Hashtable();
                foreach (TypeMapping m in Scope.TypeMappings)
                {
                    if (m is ArrayMapping)
                    {
                        ArrayMapping arrayMapping = (ArrayMapping)m;
                        if (ShouldInclude(arrayMapping) && !typesIncluded.Contains(arrayMapping.TypeDesc.FullName))
                        {
                            CodeAttributeDeclaration include = new CodeAttributeDeclaration(includeType.FullName);
                            include.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(arrayMapping.TypeDesc.FullName)));
                            _includeMetadata.Add(include);
                            typesIncluded.Add(arrayMapping.TypeDesc.FullName, string.Empty);
                            EnsureTypesExported(arrayMapping.Elements, arrayMapping.Namespace);
                        }
                    }
                }
            }
        }

        private static bool ShouldInclude(ArrayMapping arrayMapping)
        {
            if (arrayMapping.ReferencedByElement)
                return false;
            if (arrayMapping.Next != null)
                return false;
            if (arrayMapping.Elements.Length == 1)
            {
                TypeKind kind = arrayMapping.Elements[0].Mapping.TypeDesc.Kind;
                if (kind == TypeKind.Node)
                    return false;
            }

            for (int i = 0; i < arrayMapping.Elements.Length; i++)
            {
                if (arrayMapping.Elements[i].Name != arrayMapping.Elements[i].Mapping.DefaultElementName)
                {
                    // in the case we need custom attributes to serialize an array instance, we cannot include arrau mapping without explicit reference.
                    return false;
                }
            }
            return true;
        }

        internal CodeTypeDeclaration ExportEnum(EnumMapping mapping, Type type)
        {
            CodeTypeDeclaration codeClass = new CodeTypeDeclaration(mapping.TypeDesc.Name);

            codeClass.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
            codeClass.IsEnum = true;
            if (mapping.IsFlags && mapping.Constants.Length > 31)
            {
                codeClass.BaseTypes.Add(new CodeTypeReference(typeof(long)));
            }
            codeClass.TypeAttributes |= TypeAttributes.Public;
            CodeNamespace.Types.Add(codeClass);
            for (int i = 0; i < mapping.Constants.Length; i++)
            {
                ExportConstant(codeClass, mapping.Constants[i], type, mapping.IsFlags, 1L << i);
            }
            if (mapping.IsFlags)
            {
                // Add [FlagsAttribute]
                CodeAttributeDeclaration flags = new CodeAttributeDeclaration(typeof(FlagsAttribute).FullName);
                codeClass.CustomAttributes.Add(flags);
            }
            CodeGenerator.ValidateIdentifiers(codeClass);
            return codeClass;
        }

        internal void AddTypeMetadata(CodeAttributeDeclarationCollection metadata, Type type, string defaultName, string name, string ns, bool includeInSchema)
        {
            CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(type.FullName);
            if (name == null || name.Length == 0)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("AnonymousType", new CodePrimitiveExpression(true)));
            }
            else
            {
                if (defaultName != name)
                {
                    attribute.Arguments.Add(new CodeAttributeArgument("TypeName", new CodePrimitiveExpression(name)));
                }
            }
            if (ns != null && ns.Length != 0)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("Namespace", new CodePrimitiveExpression(ns)));
            }
            if (!includeInSchema)
            {
                attribute.Arguments.Add(new CodeAttributeArgument("IncludeInSchema", new CodePrimitiveExpression(false)));
            }
            if (attribute.Arguments.Count > 0)
            {
                metadata.Add(attribute);
            }
        }

        internal static void AddIncludeMetadata(CodeAttributeDeclarationCollection metadata, StructMapping mapping, Type type)
        {
            if (mapping.IsAnonymousType)
                return;
            for (StructMapping derived = mapping.DerivedMappings; derived != null; derived = derived.NextDerivedMapping)
            {
                CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(type.FullName);
                attribute.Arguments.Add(new CodeAttributeArgument(new CodeTypeOfExpression(derived.TypeDesc.FullName)));
                metadata.Add(attribute);
                AddIncludeMetadata(metadata, derived, type);
            }
        }

        internal static void ExportConstant(CodeTypeDeclaration codeClass, ConstantMapping constant, Type type, bool init, long enumValue)
        {
            CodeMemberField field = new CodeMemberField(typeof(int).FullName, constant.Name);
            field.Comments.Add(new CodeCommentStatement(SR.XmlRemarks, true));
            if (init)
                field.InitExpression = new CodePrimitiveExpression(enumValue);
            codeClass.Members.Add(field);
            if (constant.XmlName != constant.Name)
            {
                CodeAttributeDeclaration attribute = new CodeAttributeDeclaration(type.FullName);
                attribute.Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(constant.XmlName)));
                field.CustomAttributes.Add(attribute);
            }
        }

        internal static object PromoteType(Type type, object value)
        {
            if (type == typeof(sbyte))
            {
                return ((IConvertible)value).ToInt16(null);
            }
            else if (type == typeof(UInt16))
            {
                return ((IConvertible)value).ToInt32(null);
            }
            else if (type == typeof(UInt32))
            {
                return ((IConvertible)value).ToInt64(null);
            }
            else if (type == typeof(UInt64))
            {
                return ((IConvertible)value).ToDecimal(null);
            }
            else
            {
                return value;
            }
        }

        internal CodeMemberProperty CreatePropertyDeclaration(CodeMemberField field, string name, string typeName)
        {
            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Type = new CodeTypeReference(typeName);
            prop.Name = name;
            prop.Attributes = (prop.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Public;

            //add get
            CodeMethodReturnStatement ret = new CodeMethodReturnStatement();
            ret.Expression = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), field.Name);
            prop.GetStatements.Add(ret);

            CodeAssignStatement propertySet = new CodeAssignStatement();
            CodeExpression left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), field.Name);
            CodeExpression right = new CodePropertySetValueReferenceExpression();
            propertySet.Left = left;
            propertySet.Right = right;

            if (EnableDataBinding)
            {
                prop.SetStatements.Add(propertySet);
                prop.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), RaisePropertyChangedEventMethod.Name, new CodePrimitiveExpression(name)));
            }
            else
                prop.SetStatements.Add(propertySet);

            return prop;
        }

        internal static string MakeFieldName(string name)
        {
            return CodeIdentifier.MakeCamel(name) + "Field";
        }

        internal void AddPropertyChangedNotifier(CodeTypeDeclaration codeClass)
        {
            if (EnableDataBinding && codeClass != null)
            {
                if (codeClass.BaseTypes.Count == 0)
                {
                    codeClass.BaseTypes.Add(typeof(object));
                }
                codeClass.BaseTypes.Add(new CodeTypeReference(typeof(System.ComponentModel.INotifyPropertyChanged)));
                codeClass.Members.Add(PropertyChangedEvent);
                codeClass.Members.Add(RaisePropertyChangedEventMethod);
            }
        }

        private bool EnableDataBinding
        {
            get { return (_options & CodeGenerationOptions.EnableDataBinding) != 0; }
        }

        internal static CodeMemberMethod RaisePropertyChangedEventMethod
        {
            get
            {
                CodeMemberMethod raisePropertyChangedEventMethod = new CodeMemberMethod();
                raisePropertyChangedEventMethod.Name = "RaisePropertyChanged";
                raisePropertyChangedEventMethod.Attributes = MemberAttributes.Family | MemberAttributes.Final;
                CodeArgumentReferenceExpression propertyName = new CodeArgumentReferenceExpression("propertyName");
                raisePropertyChangedEventMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), propertyName.ParameterName));
                CodeVariableReferenceExpression propertyChanged = new CodeVariableReferenceExpression("propertyChanged");
                raisePropertyChangedEventMethod.Statements.Add(new CodeVariableDeclarationStatement(typeof(PropertyChangedEventHandler), propertyChanged.VariableName, new CodeEventReferenceExpression(new CodeThisReferenceExpression(), PropertyChangedEvent.Name)));
                CodeConditionStatement ifStatement = new CodeConditionStatement(new CodeBinaryOperatorExpression(propertyChanged, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)));
                raisePropertyChangedEventMethod.Statements.Add(ifStatement);
                ifStatement.TrueStatements.Add(new CodeDelegateInvokeExpression(propertyChanged, new CodeThisReferenceExpression(), new CodeObjectCreateExpression(typeof(PropertyChangedEventArgs), propertyName)));
                return raisePropertyChangedEventMethod;
            }
        }

        internal static CodeMemberEvent PropertyChangedEvent
        {
            get
            {
                CodeMemberEvent propertyChangedEvent = new CodeMemberEvent();
                propertyChangedEvent.Attributes = MemberAttributes.Public;
                propertyChangedEvent.Name = "PropertyChanged";
                propertyChangedEvent.Type = new CodeTypeReference(typeof(PropertyChangedEventHandler));
                propertyChangedEvent.ImplementationTypes.Add(typeof(System.ComponentModel.INotifyPropertyChanged));
                return propertyChangedEvent;
            }
        }
    }
}
