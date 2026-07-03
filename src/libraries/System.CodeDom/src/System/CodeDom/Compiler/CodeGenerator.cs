// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Reflection;

namespace System.CodeDom.Compiler
{
    public abstract class CodeGenerator : ICodeGenerator
    {
        private const int ParameterMultilineThreshold = 15;
        private ExposedTabStringIndentedTextWriter _output;
        private CodeGeneratorOptions _options;

        private CodeTypeDeclaration _currentClass;
        private CodeTypeMember _currentMember;

        private bool _inNestedBinary;

        protected CodeTypeDeclaration CurrentClass => _currentClass;

        protected string CurrentTypeName => _currentClass != null ? _currentClass.Name : "<% unknown %>";

        protected CodeTypeMember CurrentMember => _currentMember;

        protected string CurrentMemberName => _currentMember != null ? _currentMember.Name : "<% unknown %>";

        protected bool IsCurrentInterface => _currentClass != null && _currentClass is not CodeTypeDelegate ? _currentClass.IsInterface : false;

        protected bool IsCurrentClass => _currentClass != null && _currentClass is not CodeTypeDelegate ? _currentClass.IsClass : false;

        protected bool IsCurrentStruct => _currentClass != null && _currentClass is not CodeTypeDelegate ? _currentClass.IsStruct : false;

        protected bool IsCurrentEnum => _currentClass != null && _currentClass is not CodeTypeDelegate ? _currentClass.IsEnum : false;

        protected bool IsCurrentDelegate => _currentClass != null && _currentClass is CodeTypeDelegate;

        protected int Indent
        {
            get => _output.Indent;
            set => _output.Indent = value;
        }

        protected abstract string NullToken { get; }

        protected TextWriter Output => _output;

        protected CodeGeneratorOptions Options => _options;

        private void GenerateType(CodeTypeDeclaration e)
        {
            _currentClass = e;

            if (e.StartDirectives.Count > 0)
            {
                GenerateDirectives(e.StartDirectives);
            }

            GenerateCommentStatements(e.Comments);

            if (e.LinePragma != null)
            {
                GenerateLinePragmaStart(e.LinePragma);
            }

            GenerateTypeStart(e);

            if (Options.VerbatimOrder)
            {
                foreach (CodeTypeMember member in e.Members)
                {
                    GenerateTypeMember(member, e);
                }
            }
            else
            {
                GenerateFields(e);
                GenerateSnippetMembers(e);
                GenerateTypeConstructors(e);
                GenerateConstructors(e);
                GenerateProperties(e);
                GenerateEvents(e);
                GenerateMethods(e);
                GenerateNestedTypes(e);
            }

            // Nested types clobber the current class, so reset it.
            _currentClass = e;

            GenerateTypeEnd(e);
            if (e.LinePragma != null)
            {
                GenerateLinePragmaEnd(e.LinePragma);
            }

            if (e.EndDirectives.Count > 0)
            {
                GenerateDirectives(e.EndDirectives);
            }
        }

        protected virtual void GenerateDirectives(CodeDirectiveCollection directives)
        {
        }

        private void GenerateTypeMember(CodeTypeMember member, CodeTypeDeclaration declaredType)
        {
            if (_options.BlankLinesBetweenMembers)
            {
                Output.WriteLine();
            }

            if (member is CodeTypeDeclaration codeTypeDeclaration)
            {
                ((ICodeGenerator)this).GenerateCodeFromType(codeTypeDeclaration, _output.InnerWriter, _options);

                // Nested types clobber the current class, so reset it.
                _currentClass = declaredType;

                // For nested types, comments and line pragmas are handled separately, so return here
                return;
            }

            if (member.StartDirectives.Count > 0)
            {
                GenerateDirectives(member.StartDirectives);
            }

            GenerateCommentStatements(member.Comments);

            if (member.LinePragma != null)
            {
                GenerateLinePragmaStart(member.LinePragma);
            }

            if (member is CodeMemberField codeMemberField)
            {
                GenerateField(codeMemberField);
            }
            else if (member is CodeMemberProperty codeMemberProperty)
            {
                GenerateProperty(codeMemberProperty, declaredType);
            }
            else if (member is CodeMemberMethod codeMemberMethod)
            {
                if (member is CodeConstructor codeConstructor)
                {
                    GenerateConstructor(codeConstructor, declaredType);
                }
                else if (member is CodeTypeConstructor codeTypeConstructor)
                {
                    GenerateTypeConstructor(codeTypeConstructor);
                }
                else if (member is CodeEntryPointMethod codeEntryPointMethod)
                {
                    GenerateEntryPointMethod(codeEntryPointMethod, declaredType);
                }
                else
                {
                    GenerateMethod(codeMemberMethod, declaredType);
                }
            }
            else if (member is CodeMemberEvent codeMemberEvent)
            {
                GenerateEvent(codeMemberEvent, declaredType);
            }
            else if (member is CodeSnippetTypeMember codeSnippetTypeMember)
            {
                // Don't indent snippets, in order to preserve the column
                // information from the original code.  This improves the debugging
                // experience.
                int savedIndent = Indent;
                Indent = 0;

                GenerateSnippetMember(codeSnippetTypeMember);

                // Restore the indent
                Indent = savedIndent;

                // Generate an extra new line at the end of the snippet.
                // If the snippet is comment and this type only contains comments.
                // The generated code will not compile.
                Output.WriteLine();
            }

            if (member.LinePragma != null)
            {
                GenerateLinePragmaEnd(member.LinePragma);
            }

            if (member.EndDirectives.Count > 0)
            {
                GenerateDirectives(member.EndDirectives);
            }
        }

        private void GenerateTypeConstructors(CodeTypeDeclaration e)
        {
            foreach (CodeTypeMember current in e.Members)
            {
                if (current is CodeTypeConstructor codeTypeConstructor)
                {
                    _currentMember = current;

                    if (_options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (_currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(_currentMember.Comments);
                    CodeTypeConstructor imp = codeTypeConstructor;
                    if (imp.LinePragma != null) GenerateLinePragmaStart(imp.LinePragma);
                    GenerateTypeConstructor(imp);
                    if (imp.LinePragma != null) GenerateLinePragmaEnd(imp.LinePragma);
                    if (_currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.EndDirectives);
                    }
                }
            }
        }

        protected void GenerateNamespaces(CodeCompileUnit e)
        {
            foreach (CodeNamespace n in e.Namespaces)
            {
                ((ICodeGenerator)this).GenerateCodeFromNamespace(n, _output.InnerWriter, _options);
            }
        }

        protected void GenerateTypes(CodeNamespace e)
        {
            ArgumentNullException.ThrowIfNull(e);

            foreach (CodeTypeDeclaration c in e.Types)
            {
                if (_options.BlankLinesBetweenMembers)
                {
                    Output.WriteLine();
                }
                ((ICodeGenerator)this).GenerateCodeFromType(c, _output.InnerWriter, _options);
            }
        }

        bool ICodeGenerator.Supports(GeneratorSupport support) => Supports(support);

        void ICodeGenerator.GenerateCodeFromType(CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (_output != null && w != _output.InnerWriter)
            {
                throw new InvalidOperationException(SR.CodeGenOutputWriter);
            }
            if (_output == null)
            {
                setLocal = true;
                _options = o ?? new CodeGeneratorOptions();
                _output = new ExposedTabStringIndentedTextWriter(w, _options.IndentString);
            }

            try
            {
                GenerateType(e);
            }
            finally
            {
                if (setLocal)
                {
                    _output = null;
                    _options = null;
                }
            }
        }

        void ICodeGenerator.GenerateCodeFromExpression(CodeExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (_output != null && w != _output.InnerWriter)
            {
                throw new InvalidOperationException(SR.CodeGenOutputWriter);
            }
            if (_output == null)
            {
                setLocal = true;
                _options = o ?? new CodeGeneratorOptions();
                _output = new ExposedTabStringIndentedTextWriter(w, _options.IndentString);
            }

            try
            {
                GenerateExpression(e);
            }
            finally
            {
                if (setLocal)
                {
                    _output = null;
                    _options = null;
                }
            }
        }

        void ICodeGenerator.GenerateCodeFromCompileUnit(CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (_output != null && w != _output.InnerWriter)
            {
                throw new InvalidOperationException(SR.CodeGenOutputWriter);
            }
            if (_output == null)
            {
                setLocal = true;
                _options = o ?? new CodeGeneratorOptions();
                _output = new ExposedTabStringIndentedTextWriter(w, _options.IndentString);
            }

            try
            {
                if (e is CodeSnippetCompileUnit codeSnippetCompileUnit)
                {
                    GenerateSnippetCompileUnit(codeSnippetCompileUnit);
                }
                else
                {
                    GenerateCompileUnit(e);
                }
            }
            finally
            {
                if (setLocal)
                {
                    _output = null;
                    _options = null;
                }
            }
        }

        void ICodeGenerator.GenerateCodeFromNamespace(CodeNamespace e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (_output != null && w != _output.InnerWriter)
            {
                throw new InvalidOperationException(SR.CodeGenOutputWriter);
            }
            if (_output == null)
            {
                setLocal = true;
                _options = o ?? new CodeGeneratorOptions();
                _output = new ExposedTabStringIndentedTextWriter(w, _options.IndentString);
            }

            try
            {
                GenerateNamespace(e);
            }
            finally
            {
                if (setLocal)
                {
                    _output = null;
                    _options = null;
                }
            }
        }

        void ICodeGenerator.GenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o)
        {
            bool setLocal = false;
            if (_output != null && w != _output.InnerWriter)
            {
                throw new InvalidOperationException(SR.CodeGenOutputWriter);
            }
            if (_output == null)
            {
                setLocal = true;
                _options = o ?? new CodeGeneratorOptions();
                _output = new ExposedTabStringIndentedTextWriter(w, _options.IndentString);
            }

            try
            {
                GenerateStatement(e);
            }
            finally
            {
                if (setLocal)
                {
                    _output = null;
                    _options = null;
                }
            }
        }

        public virtual void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options)
        {
            ArgumentNullException.ThrowIfNull(member);

            if (_output != null)
            {
                throw new InvalidOperationException(SR.CodeGenReentrance);
            }
            _options = options ?? new CodeGeneratorOptions();
            _output = new ExposedTabStringIndentedTextWriter(writer, _options.IndentString);

            try
            {
                CodeTypeDeclaration dummyClass = new CodeTypeDeclaration();
                _currentClass = dummyClass;
                GenerateTypeMember(member, dummyClass);
            }
            finally
            {
                _currentClass = null;
                _output = null;
                _options = null;
            }
        }

        bool ICodeGenerator.IsValidIdentifier(string value) => IsValidIdentifier(value);

        void ICodeGenerator.ValidateIdentifier(string value) => ValidateIdentifier(value);

        string ICodeGenerator.CreateEscapedIdentifier(string value) => CreateEscapedIdentifier(value);

        string ICodeGenerator.CreateValidIdentifier(string value) => CreateValidIdentifier(value);

        string ICodeGenerator.GetTypeOutput(CodeTypeReference type) => GetTypeOutput(type);

        private void GenerateConstructors(CodeTypeDeclaration e)
        {
            foreach (CodeTypeMember current in e.Members)
            {
                if (current is CodeConstructor codeConstructor)
                {
                    _currentMember = current;

                    if (_options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (_currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(_currentMember.Comments);
                    CodeConstructor imp = codeConstructor;
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaStart(imp.LinePragma);
                    }
                    GenerateConstructor(imp, e);
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaEnd(imp.LinePragma);
                    }
                    if (_currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.EndDirectives);
                    }
                }
            }
        }

        private void GenerateEvents(CodeTypeDeclaration e)
        {
            foreach (CodeTypeMember current in e.Members)
            {
                if (current is CodeMemberEvent codeMemberEvent)
                {
                    _currentMember = current;

                    if (_options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (_currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(_currentMember.Comments);
                    CodeMemberEvent imp = codeMemberEvent;
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaStart(imp.LinePragma);
                    }
                    GenerateEvent(imp, e);
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaEnd(imp.LinePragma);
                    }
                    if (_currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.EndDirectives);
                    }
                }
            }
        }

        protected void GenerateExpression(CodeExpression e)
        {
            if (e is CodeArrayCreateExpression codeArrayCreateExpression)
            {
                GenerateArrayCreateExpression(codeArrayCreateExpression);
            }
            else if (e is CodeBaseReferenceExpression codeBaseReferenceExpression)
            {
                GenerateBaseReferenceExpression(codeBaseReferenceExpression);
            }
            else if (e is CodeBinaryOperatorExpression codeBinaryOperatorExpression)
            {
                GenerateBinaryOperatorExpression(codeBinaryOperatorExpression);
            }
            else if (e is CodeCastExpression codeCastExpression)
            {
                GenerateCastExpression(codeCastExpression);
            }
            else if (e is CodeDelegateCreateExpression codeDelegateCreateExpression)
            {
                GenerateDelegateCreateExpression(codeDelegateCreateExpression);
            }
            else if (e is CodeFieldReferenceExpression codeFieldReferenceExpression)
            {
                GenerateFieldReferenceExpression(codeFieldReferenceExpression);
            }
            else if (e is CodeArgumentReferenceExpression codeArgumentReferenceExpression)
            {
                GenerateArgumentReferenceExpression(codeArgumentReferenceExpression);
            }
            else if (e is CodeVariableReferenceExpression codeVariableReferenceExpression)
            {
                GenerateVariableReferenceExpression(codeVariableReferenceExpression);
            }
            else if (e is CodeIndexerExpression codeIndexerExpression)
            {
                GenerateIndexerExpression(codeIndexerExpression);
            }
            else if (e is CodeArrayIndexerExpression codeArrayIndexerExpression)
            {
                GenerateArrayIndexerExpression(codeArrayIndexerExpression);
            }
            else if (e is CodeSnippetExpression codeSnippetExpression)
            {
                GenerateSnippetExpression(codeSnippetExpression);
            }
            else if (e is CodeMethodInvokeExpression codeMethodInvokeExpression)
            {
                GenerateMethodInvokeExpression(codeMethodInvokeExpression);
            }
            else if (e is CodeMethodReferenceExpression codeMethodReferenceExpression)
            {
                GenerateMethodReferenceExpression(codeMethodReferenceExpression);
            }
            else if (e is CodeEventReferenceExpression codeEventReferenceExpression)
            {
                GenerateEventReferenceExpression(codeEventReferenceExpression);
            }
            else if (e is CodeDelegateInvokeExpression codeDelegateInvokeExpression)
            {
                GenerateDelegateInvokeExpression(codeDelegateInvokeExpression);
            }
            else if (e is CodeObjectCreateExpression codeObjectCreateExpression)
            {
                GenerateObjectCreateExpression(codeObjectCreateExpression);
            }
            else if (e is CodeParameterDeclarationExpression codeParameterDeclarationExpression)
            {
                GenerateParameterDeclarationExpression(codeParameterDeclarationExpression);
            }
            else if (e is CodeDirectionExpression codeDirectionExpression)
            {
                GenerateDirectionExpression(codeDirectionExpression);
            }
            else if (e is CodePrimitiveExpression codePrimitiveExpression)
            {
                GeneratePrimitiveExpression(codePrimitiveExpression);
            }
            else if (e is CodePropertyReferenceExpression codePropertyReferenceExpression)
            {
                GeneratePropertyReferenceExpression(codePropertyReferenceExpression);
            }
            else if (e is CodePropertySetValueReferenceExpression codePropertySetValueReferenceExpression)
            {
                GeneratePropertySetValueReferenceExpression(codePropertySetValueReferenceExpression);
            }
            else if (e is CodeThisReferenceExpression codeThisReferenceExpression)
            {
                GenerateThisReferenceExpression(codeThisReferenceExpression);
            }
            else if (e is CodeTypeReferenceExpression codeTypeReferenceExpression)
            {
                GenerateTypeReferenceExpression(codeTypeReferenceExpression);
            }
            else if (e is CodeTypeOfExpression codeTypeOfExpression)
            {
                GenerateTypeOfExpression(codeTypeOfExpression);
            }
            else if (e is CodeDefaultValueExpression codeDefaultValueExpression)
            {
                GenerateDefaultValueExpression(codeDefaultValueExpression);
            }
            else
            {
                ArgumentNullException.ThrowIfNull(e);
                throw new ArgumentException(SR.Format(SR.InvalidElementType, e.GetType().FullName), nameof(e));
            }
        }

        private void GenerateFields(CodeTypeDeclaration e)
        {
            foreach (CodeTypeMember current in e.Members)
            {
                if (current is CodeMemberField codeMemberField)
                {
                    _currentMember = current;

                    if (_options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (_currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(_currentMember.Comments);
                    CodeMemberField imp = codeMemberField;
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaStart(imp.LinePragma);
                    }
                    GenerateField(imp);
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaEnd(imp.LinePragma);
                    }
                    if (_currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.EndDirectives);
                    }
                }
            }
        }

        private void GenerateSnippetMembers(CodeTypeDeclaration e)
        {
            bool hasSnippet = false;
            foreach (CodeTypeMember current in e.Members)
            {
                if (current is CodeSnippetTypeMember codeSnippetTypeMember)
                {
                    hasSnippet = true;
                    _currentMember = current;

                    if (_options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (_currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(_currentMember.Comments);
                    CodeSnippetTypeMember imp = codeSnippetTypeMember;
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaStart(imp.LinePragma);
                    }

                    // Don't indent snippets, in order to preserve the column
                    // information from the original code.  This improves the debugging
                    // experience.
                    int savedIndent = Indent;
                    Indent = 0;

                    GenerateSnippetMember(imp);

                    // Restore the indent
                    Indent = savedIndent;

                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaEnd(imp.LinePragma);
                    }
                    if (_currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.EndDirectives);
                    }
                }
            }
            // Generate an extra new line at the end of the snippet.
            // If the snippet is comment and this type only contains comments.
            // The generated code will not compile.
            if (hasSnippet)
            {
                Output.WriteLine();
            }
        }

        protected virtual void GenerateSnippetCompileUnit(CodeSnippetCompileUnit e)
        {
            ArgumentNullException.ThrowIfNull(e);

            GenerateDirectives(e.StartDirectives);

            if (e.LinePragma != null)
            {
                GenerateLinePragmaStart(e.LinePragma);
            }
            Output.WriteLine(e.Value);
            if (e.LinePragma != null)
            {
                GenerateLinePragmaEnd(e.LinePragma);
            }

            if (e.EndDirectives.Count > 0)
            {
                GenerateDirectives(e.EndDirectives);
            }
        }

        private void GenerateMethods(CodeTypeDeclaration e)
        {
            foreach (CodeTypeMember current in e.Members)
            {
                if (current is CodeMemberMethod && current is not CodeTypeConstructor && current is not CodeConstructor)
                {
                    _currentMember = current;

                    if (_options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (_currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(_currentMember.Comments);
                    CodeMemberMethod imp = (CodeMemberMethod)current;
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaStart(imp.LinePragma);
                    }
                    if (current is CodeEntryPointMethod codeEntryPointMethod)
                    {
                        GenerateEntryPointMethod(codeEntryPointMethod, e);
                    }
                    else
                    {
                        GenerateMethod(imp, e);
                    }
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaEnd(imp.LinePragma);
                    }
                    if (_currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.EndDirectives);
                    }
                }
            }
        }

        private void GenerateNestedTypes(CodeTypeDeclaration e)
        {
            foreach (CodeTypeMember current in e.Members)
            {
                if (current is CodeTypeDeclaration codeTypeDeclaration)
                {
                    if (_options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    CodeTypeDeclaration currentClass = codeTypeDeclaration;
                    ((ICodeGenerator)this).GenerateCodeFromType(currentClass, _output.InnerWriter, _options);
                }
            }
        }

        protected virtual void GenerateCompileUnit(CodeCompileUnit e)
        {
            GenerateCompileUnitStart(e);
            GenerateNamespaces(e);
            GenerateCompileUnitEnd(e);
        }

        protected virtual void GenerateNamespace(CodeNamespace e)
        {
            ArgumentNullException.ThrowIfNull(e);

            GenerateCommentStatements(e.Comments);
            GenerateNamespaceStart(e);

            GenerateNamespaceImports(e);
            Output.WriteLine();

            GenerateTypes(e);
            GenerateNamespaceEnd(e);
        }

        protected void GenerateNamespaceImports(CodeNamespace e)
        {
            ArgumentNullException.ThrowIfNull(e);

            foreach (CodeNamespaceImport imp in e.Imports)
            {
                if (imp.LinePragma != null)
                {
                    GenerateLinePragmaStart(imp.LinePragma);
                }

                GenerateNamespaceImport(imp);
                if (imp.LinePragma != null)
                {
                    GenerateLinePragmaEnd(imp.LinePragma);
                }
            }
        }

        private void GenerateProperties(CodeTypeDeclaration e)
        {
            foreach (CodeTypeMember current in e.Members)
            {
                if (current is CodeMemberProperty codeMemberProperty)
                {
                    _currentMember = current;

                    if (_options.BlankLinesBetweenMembers)
                    {
                        Output.WriteLine();
                    }
                    if (_currentMember.StartDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.StartDirectives);
                    }
                    GenerateCommentStatements(_currentMember.Comments);
                    CodeMemberProperty imp = codeMemberProperty;
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaStart(imp.LinePragma);
                    }
                    GenerateProperty(imp, e);
                    if (imp.LinePragma != null)
                    {
                        GenerateLinePragmaEnd(imp.LinePragma);
                    }
                    if (_currentMember.EndDirectives.Count > 0)
                    {
                        GenerateDirectives(_currentMember.EndDirectives);
                    }
                }
            }
        }

        protected void GenerateStatement(CodeStatement e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (e.StartDirectives.Count > 0)
            {
                GenerateDirectives(e.StartDirectives);
            }

            if (e.LinePragma != null)
            {
                GenerateLinePragmaStart(e.LinePragma);
            }

            if (e is CodeCommentStatement codeCommentStatement)
            {
                GenerateCommentStatement(codeCommentStatement);
            }
            else if (e is CodeMethodReturnStatement codeMethodReturnStatement)
            {
                GenerateMethodReturnStatement(codeMethodReturnStatement);
            }
            else if (e is CodeConditionStatement codeConditionStatement)
            {
                GenerateConditionStatement(codeConditionStatement);
            }
            else if (e is CodeTryCatchFinallyStatement codeTryCatchFinallyStatement)
            {
                GenerateTryCatchFinallyStatement(codeTryCatchFinallyStatement);
            }
            else if (e is CodeAssignStatement codeAssignStatement)
            {
                GenerateAssignStatement(codeAssignStatement);
            }
            else if (e is CodeExpressionStatement codeExpressionStatement)
            {
                GenerateExpressionStatement(codeExpressionStatement);
            }
            else if (e is CodeIterationStatement codeIterationStatement)
            {
                GenerateIterationStatement(codeIterationStatement);
            }
            else if (e is CodeThrowExceptionStatement codeThrowExceptionStatement)
            {
                GenerateThrowExceptionStatement(codeThrowExceptionStatement);
            }
            else if (e is CodeSnippetStatement codeSnippetStatement)
            {
                // Don't indent snippet statements, in order to preserve the column
                // information from the original code.  This improves the debugging
                // experience.
                int savedIndent = Indent;
                Indent = 0;

                GenerateSnippetStatement(codeSnippetStatement);

                // Restore the indent
                Indent = savedIndent;
            }
            else if (e is CodeVariableDeclarationStatement codeVariableDeclarationStatement)
            {
                GenerateVariableDeclarationStatement(codeVariableDeclarationStatement);
            }
            else if (e is CodeAttachEventStatement codeAttachEventStatement)
            {
                GenerateAttachEventStatement(codeAttachEventStatement);
            }
            else if (e is CodeRemoveEventStatement codeRemoveEventStatement)
            {
                GenerateRemoveEventStatement(codeRemoveEventStatement);
            }
            else if (e is CodeGotoStatement codeGotoStatement)
            {
                GenerateGotoStatement(codeGotoStatement);
            }
            else if (e is CodeLabeledStatement codeLabeledStatement)
            {
                GenerateLabeledStatement(codeLabeledStatement);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.InvalidElementType, e.GetType().FullName), nameof(e));
            }

            if (e.LinePragma != null)
            {
                GenerateLinePragmaEnd(e.LinePragma);
            }
            if (e.EndDirectives.Count > 0)
            {
                GenerateDirectives(e.EndDirectives);
            }
        }

        protected void GenerateStatements(CodeStatementCollection stmts)
        {
            ArgumentNullException.ThrowIfNull(stmts);

            foreach (CodeStatement stmt in stmts)
            {
                ((ICodeGenerator)this).GenerateCodeFromStatement(stmt, _output.InnerWriter, _options);
            }
        }

        protected virtual void OutputAttributeDeclarations(CodeAttributeDeclarationCollection attributes)
        {
            ArgumentNullException.ThrowIfNull(attributes);

            if (attributes.Count == 0)
            {
                return;
            }

            GenerateAttributeDeclarationsStart(attributes);
            bool first = true;
            foreach (CodeAttributeDeclaration current in attributes)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    ContinueOnNewLine(", ");
                }

                Output.Write(current.Name);
                Output.Write('(');

                bool firstArg = true;
                foreach (CodeAttributeArgument arg in current.Arguments)
                {
                    if (firstArg)
                    {
                        firstArg = false;
                    }
                    else
                    {
                        Output.Write(", ");
                    }

                    OutputAttributeArgument(arg);
                }

                Output.Write(')');
            }
            GenerateAttributeDeclarationsEnd(attributes);
        }

        protected virtual void OutputAttributeArgument(CodeAttributeArgument arg)
        {
            ArgumentNullException.ThrowIfNull(arg);

            if (!string.IsNullOrEmpty(arg.Name))
            {
                OutputIdentifier(arg.Name);
                Output.Write('=');
            }
            ((ICodeGenerator)this).GenerateCodeFromExpression(arg.Value, _output.InnerWriter, _options);
        }

        protected virtual void OutputDirection(FieldDirection dir)
        {
            switch (dir)
            {
                case FieldDirection.In:
                    break;
                case FieldDirection.Out:
                    Output.Write("out ");
                    break;
                case FieldDirection.Ref:
                    Output.Write("ref ");
                    break;
            }
        }

        protected virtual void OutputFieldScopeModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.VTableMask)
            {
                case MemberAttributes.New:
                    Output.Write("new ");
                    break;
            }

            switch (attributes & MemberAttributes.ScopeMask)
            {
                case MemberAttributes.Final:
                    break;
                case MemberAttributes.Static:
                    Output.Write("static ");
                    break;
                case MemberAttributes.Const:
                    Output.Write("const ");
                    break;
                default:
                    break;
            }
        }

        protected virtual void OutputMemberAccessModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.AccessMask)
            {
                case MemberAttributes.Assembly:
                    Output.Write("internal ");
                    break;
                case MemberAttributes.FamilyAndAssembly:
                    Output.Write("internal ");  /*FamANDAssem*/
                    break;
                case MemberAttributes.Family:
                    Output.Write("protected ");
                    break;
                case MemberAttributes.FamilyOrAssembly:
                    Output.Write("protected internal ");
                    break;
                case MemberAttributes.Private:
                    Output.Write("private ");
                    break;
                case MemberAttributes.Public:
                    Output.Write("public ");
                    break;
            }
        }

        protected virtual void OutputMemberScopeModifier(MemberAttributes attributes)
        {
            switch (attributes & MemberAttributes.VTableMask)
            {
                case MemberAttributes.New:
                    Output.Write("new ");
                    break;
            }

            switch (attributes & MemberAttributes.ScopeMask)
            {
                case MemberAttributes.Abstract:
                    Output.Write("abstract ");
                    break;
                case MemberAttributes.Final:
                    Output.Write("");
                    break;
                case MemberAttributes.Static:
                    Output.Write("static ");
                    break;
                case MemberAttributes.Override:
                    Output.Write("override ");
                    break;
                default:
                    switch (attributes & MemberAttributes.AccessMask)
                    {
                        case MemberAttributes.Family:
                        case MemberAttributes.Public:
                            Output.Write("virtual ");
                            break;
                        default:
                            // nothing;
                            break;
                    }
                    break;
            }
        }

        protected abstract void OutputType(CodeTypeReference typeRef);

        protected virtual void OutputTypeAttributes(TypeAttributes attributes, bool isStruct, bool isEnum)
        {
            switch (attributes & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public:
                case TypeAttributes.NestedPublic:
                    Output.Write("public ");
                    break;
                case TypeAttributes.NestedPrivate:
                    Output.Write("private ");
                    break;
            }

            if (isStruct)
            {
                Output.Write("struct ");
            }
            else if (isEnum)
            {
                Output.Write("enum ");
            }
            else
            {
                switch (attributes & TypeAttributes.ClassSemanticsMask)
                {
                    case TypeAttributes.Class:
                        if ((attributes & TypeAttributes.Sealed) == TypeAttributes.Sealed)
                        {
                            Output.Write("sealed ");
                        }
                        if ((attributes & TypeAttributes.Abstract) == TypeAttributes.Abstract)
                        {
                            Output.Write("abstract ");
                        }
                        Output.Write("class ");
                        break;
                    case TypeAttributes.Interface:
                        Output.Write("interface ");
                        break;
                }
            }
        }

        protected virtual void OutputTypeNamePair(CodeTypeReference typeRef, string name)
        {
            OutputType(typeRef);
            Output.Write(' ');
            OutputIdentifier(name);
        }

        protected virtual void OutputIdentifier(string ident) => Output.Write(ident);

        protected virtual void OutputExpressionList(CodeExpressionCollection expressions)
        {
            OutputExpressionList(expressions, newlineBetweenItems: false);
        }

        protected virtual void OutputExpressionList(CodeExpressionCollection expressions, bool newlineBetweenItems)
        {
            bool first = true;
            Indent++;
            foreach (CodeExpression current in expressions)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    if (newlineBetweenItems)
                        ContinueOnNewLine(",");
                    else
                        Output.Write(", ");
                }
                ((ICodeGenerator)this).GenerateCodeFromExpression(current, _output.InnerWriter, _options);
            }
            Indent--;
        }

        protected virtual void OutputOperator(CodeBinaryOperatorType op)
        {
            switch (op)
            {
                case CodeBinaryOperatorType.Add:
                    Output.Write('+');
                    break;
                case CodeBinaryOperatorType.Subtract:
                    Output.Write('-');
                    break;
                case CodeBinaryOperatorType.Multiply:
                    Output.Write('*');
                    break;
                case CodeBinaryOperatorType.Divide:
                    Output.Write('/');
                    break;
                case CodeBinaryOperatorType.Modulus:
                    Output.Write('%');
                    break;
                case CodeBinaryOperatorType.Assign:
                    Output.Write('=');
                    break;
                case CodeBinaryOperatorType.IdentityInequality:
                    Output.Write("!=");
                    break;
                case CodeBinaryOperatorType.IdentityEquality:
                    Output.Write("==");
                    break;
                case CodeBinaryOperatorType.ValueEquality:
                    Output.Write("==");
                    break;
                case CodeBinaryOperatorType.BitwiseOr:
                    Output.Write('|');
                    break;
                case CodeBinaryOperatorType.BitwiseAnd:
                    Output.Write('&');
                    break;
                case CodeBinaryOperatorType.BooleanOr:
                    Output.Write("||");
                    break;
                case CodeBinaryOperatorType.BooleanAnd:
                    Output.Write("&&");
                    break;
                case CodeBinaryOperatorType.LessThan:
                    Output.Write('<');
                    break;
                case CodeBinaryOperatorType.LessThanOrEqual:
                    Output.Write("<=");
                    break;
                case CodeBinaryOperatorType.GreaterThan:
                    Output.Write('>');
                    break;
                case CodeBinaryOperatorType.GreaterThanOrEqual:
                    Output.Write(">=");
                    break;
            }
        }

        protected virtual void OutputParameters(CodeParameterDeclarationExpressionCollection parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);

            bool first = true;
            bool multiline = parameters.Count > ParameterMultilineThreshold;
            if (multiline)
            {
                Indent += 3;
            }
            foreach (CodeParameterDeclarationExpression current in parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    Output.Write(", ");
                }
                if (multiline)
                {
                    ContinueOnNewLine("");
                }
                GenerateExpression(current);
            }
            if (multiline)
            {
                Indent -= 3;
            }
        }

        protected abstract void GenerateArrayCreateExpression(CodeArrayCreateExpression e);
        protected abstract void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e);

        protected virtual void GenerateBinaryOperatorExpression(CodeBinaryOperatorExpression e)
        {
            ArgumentNullException.ThrowIfNull(e);

            bool indentedExpression = false;
            Output.Write('(');

            GenerateExpression(e.Left);
            Output.Write(' ');

            if (e.Left is CodeBinaryOperatorExpression || e.Right is CodeBinaryOperatorExpression)
            {
                // In case the line gets too long with nested binary operators, we need to output them on
                // different lines. However we want to indent them to maintain readability, but this needs
                // to be done only once;
                if (!_inNestedBinary)
                {
                    indentedExpression = true;
                    _inNestedBinary = true;
                    Indent += 3;
                }
                ContinueOnNewLine("");
            }

            OutputOperator(e.Operator);

            Output.Write(' ');
            GenerateExpression(e.Right);

            Output.Write(')');
            if (indentedExpression)
            {
                Indent -= 3;
                _inNestedBinary = false;
            }
        }

        protected virtual void ContinueOnNewLine(string st) => Output.WriteLine(st);

        protected abstract void GenerateCastExpression(CodeCastExpression e);
        protected abstract void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e);
        protected abstract void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e);
        protected abstract void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e);
        protected abstract void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e);
        protected abstract void GenerateIndexerExpression(CodeIndexerExpression e);
        protected abstract void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e);
        protected abstract void GenerateSnippetExpression(CodeSnippetExpression e);
        protected abstract void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e);
        protected abstract void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e);
        protected abstract void GenerateEventReferenceExpression(CodeEventReferenceExpression e);
        protected abstract void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e);
        protected abstract void GenerateObjectCreateExpression(CodeObjectCreateExpression e);

        protected virtual void GenerateParameterDeclarationExpression(CodeParameterDeclarationExpression e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (e.CustomAttributes.Count > 0)
            {
                OutputAttributeDeclarations(e.CustomAttributes);
                Output.Write(' ');
            }

            OutputDirection(e.Direction);
            OutputTypeNamePair(e.Type, e.Name);
        }

        protected virtual void GenerateDirectionExpression(CodeDirectionExpression e)
        {
            ArgumentNullException.ThrowIfNull(e);

            OutputDirection(e.Direction);
            GenerateExpression(e.Expression);
        }

        protected virtual void GeneratePrimitiveExpression(CodePrimitiveExpression e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (e.Value == null)
            {
                Output.Write(NullToken);
            }
            else if (e.Value is string str)
            {
                Output.Write(QuoteSnippetString(str));
            }
            else if (e.Value is char)
            {
                Output.Write("'" + e.Value.ToString() + "'");
            }
            else if (e.Value is byte b2)
            {
                Output.Write(b2.ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is short num3)
            {
                Output.Write(num3.ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is int num2)
            {
                Output.Write(num2.ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is long num)
            {
                Output.Write(num.ToString(CultureInfo.InvariantCulture));
            }
            else if (e.Value is float f)
            {
                GenerateSingleFloatValue(f);
            }
            else if (e.Value is double d2)
            {
                GenerateDoubleValue(d2);
            }
            else if (e.Value is decimal d)
            {
                GenerateDecimalValue(d);
            }
            else if (e.Value is bool b)
            {
                if (b)
                {
                    Output.Write("true");
                }
                else
                {
                    Output.Write("false");
                }
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.InvalidPrimitiveType, e.Value.GetType()), nameof(e));
            }
        }

        protected virtual void GenerateSingleFloatValue(float s) => Output.Write(s.ToString("R", CultureInfo.InvariantCulture));

        protected virtual void GenerateDoubleValue(double d) => Output.Write(d.ToString("R", CultureInfo.InvariantCulture));

        protected virtual void GenerateDecimalValue(decimal d) => Output.Write(d.ToString(CultureInfo.InvariantCulture));

        protected virtual void GenerateDefaultValueExpression(CodeDefaultValueExpression e)
        {
        }

        protected abstract void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e);

        protected abstract void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e);

        protected abstract void GenerateThisReferenceExpression(CodeThisReferenceExpression e);

        protected virtual void GenerateTypeReferenceExpression(CodeTypeReferenceExpression e)
        {
            ArgumentNullException.ThrowIfNull(e);

            OutputType(e.Type);
        }

        protected virtual void GenerateTypeOfExpression(CodeTypeOfExpression e)
        {
            ArgumentNullException.ThrowIfNull(e);

            Output.Write("typeof(");
            OutputType(e.Type);
            Output.Write(')');
        }

        protected abstract void GenerateExpressionStatement(CodeExpressionStatement e);
        protected abstract void GenerateIterationStatement(CodeIterationStatement e);
        protected abstract void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e);
        protected virtual void GenerateCommentStatement(CodeCommentStatement e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (e.Comment == null)
            {
                throw new ArgumentException(SR.Format(SR.Argument_NullComment, nameof(e)), nameof(e));
            }
            GenerateComment(e.Comment);
        }

        protected virtual void GenerateCommentStatements(CodeCommentStatementCollection e)
        {
            ArgumentNullException.ThrowIfNull(e);

            foreach (CodeCommentStatement comment in e)
            {
                GenerateCommentStatement(comment);
            }
        }

        protected abstract void GenerateComment(CodeComment e);
        protected abstract void GenerateMethodReturnStatement(CodeMethodReturnStatement e);
        protected abstract void GenerateConditionStatement(CodeConditionStatement e);
        protected abstract void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e);
        protected abstract void GenerateAssignStatement(CodeAssignStatement e);
        protected abstract void GenerateAttachEventStatement(CodeAttachEventStatement e);
        protected abstract void GenerateRemoveEventStatement(CodeRemoveEventStatement e);
        protected abstract void GenerateGotoStatement(CodeGotoStatement e);
        protected abstract void GenerateLabeledStatement(CodeLabeledStatement e);

        protected virtual void GenerateSnippetStatement(CodeSnippetStatement e)
        {
            ArgumentNullException.ThrowIfNull(e);

            Output.WriteLine(e.Value);
        }

        protected abstract void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e);
        protected abstract void GenerateLinePragmaStart(CodeLinePragma e);
        protected abstract void GenerateLinePragmaEnd(CodeLinePragma e);
        protected abstract void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c);
        protected abstract void GenerateField(CodeMemberField e);
        protected abstract void GenerateSnippetMember(CodeSnippetTypeMember e);
        protected abstract void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c);
        protected abstract void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c);
        protected abstract void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c);
        protected abstract void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c);
        protected abstract void GenerateTypeConstructor(CodeTypeConstructor e);
        protected abstract void GenerateTypeStart(CodeTypeDeclaration e);
        protected abstract void GenerateTypeEnd(CodeTypeDeclaration e);

        protected virtual void GenerateCompileUnitStart(CodeCompileUnit e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (e.StartDirectives.Count > 0)
            {
                GenerateDirectives(e.StartDirectives);
            }
        }

        protected virtual void GenerateCompileUnitEnd(CodeCompileUnit e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (e.EndDirectives.Count > 0)
            {
                GenerateDirectives(e.EndDirectives);
            }
        }

        protected abstract void GenerateNamespaceStart(CodeNamespace e);
        protected abstract void GenerateNamespaceEnd(CodeNamespace e);
        protected abstract void GenerateNamespaceImport(CodeNamespaceImport e);
        protected abstract void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes);
        protected abstract void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes);
        protected abstract bool Supports(GeneratorSupport support);
        protected abstract bool IsValidIdentifier(string value);

        protected virtual void ValidateIdentifier(string value)
        {
            if (!IsValidIdentifier(value))
            {
                throw new ArgumentException(SR.Format(SR.InvalidIdentifier, value), nameof(value));
            }
        }

        protected abstract string CreateEscapedIdentifier(string value);
        protected abstract string CreateValidIdentifier(string value);
        protected abstract string GetTypeOutput(CodeTypeReference value);
        protected abstract string QuoteSnippetString(string value);

        public static bool IsValidLanguageIndependentIdentifier(string value) => CSharpHelpers.IsValidTypeNameOrIdentifier(value, false);

        internal static bool IsValidLanguageIndependentTypeName(string value) => CSharpHelpers.IsValidTypeNameOrIdentifier(value, true);

        public static void ValidateIdentifiers(CodeObject e)
        {
            CodeValidator codeValidator = new CodeValidator(); // This has internal state and hence is not static
            codeValidator.ValidateIdentifiers(e);
        }
    }
}
