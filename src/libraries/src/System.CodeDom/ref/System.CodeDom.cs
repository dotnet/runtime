// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.CSharp
{
    public partial class CSharpCodeProvider : System.CodeDom.Compiler.CodeDomProvider
    {
        public CSharpCodeProvider() { }
        public CSharpCodeProvider(System.Collections.Generic.IDictionary<string, string> providerOptions) { }
        public override string FileExtension { get { throw null; } }
        [System.ObsoleteAttribute("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override System.CodeDom.Compiler.ICodeCompiler CreateCompiler() { throw null; }
        [System.ObsoleteAttribute("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override System.CodeDom.Compiler.ICodeGenerator CreateGenerator() { throw null; }
        public override void GenerateCodeFromMember(System.CodeDom.CodeTypeMember member, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        public override System.ComponentModel.TypeConverter GetConverter(System.Type type) { throw null; }
    }
}
namespace Microsoft.VisualBasic
{
    public partial class VBCodeProvider : System.CodeDom.Compiler.CodeDomProvider
    {
        public VBCodeProvider() { }
        public VBCodeProvider(System.Collections.Generic.IDictionary<string, string> providerOptions) { }
        public override string FileExtension { get { throw null; } }
        public override System.CodeDom.Compiler.LanguageOptions LanguageOptions { get { throw null; } }
        [System.ObsoleteAttribute("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override System.CodeDom.Compiler.ICodeCompiler CreateCompiler() { throw null; }
        [System.ObsoleteAttribute("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override System.CodeDom.Compiler.ICodeGenerator CreateGenerator() { throw null; }
        public override void GenerateCodeFromMember(System.CodeDom.CodeTypeMember member, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        public override System.ComponentModel.TypeConverter GetConverter(System.Type type) { throw null; }
    }
}
namespace System.CodeDom
{
    public partial class CodeArgumentReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodeArgumentReferenceExpression() { }
        public CodeArgumentReferenceExpression(string parameterName) { }
        public string ParameterName { get { throw null; } set { } }
    }
    public partial class CodeArrayCreateExpression : System.CodeDom.CodeExpression
    {
        public CodeArrayCreateExpression() { }
        public CodeArrayCreateExpression(System.CodeDom.CodeTypeReference createType, System.CodeDom.CodeExpression size) { }
        public CodeArrayCreateExpression(System.CodeDom.CodeTypeReference createType, params System.CodeDom.CodeExpression[] initializers) { }
        public CodeArrayCreateExpression(System.CodeDom.CodeTypeReference createType, int size) { }
        public CodeArrayCreateExpression(string createType, System.CodeDom.CodeExpression size) { }
        public CodeArrayCreateExpression(string createType, params System.CodeDom.CodeExpression[] initializers) { }
        public CodeArrayCreateExpression(string createType, int size) { }
        public CodeArrayCreateExpression(System.Type createType, System.CodeDom.CodeExpression size) { }
        public CodeArrayCreateExpression(System.Type createType, params System.CodeDom.CodeExpression[] initializers) { }
        public CodeArrayCreateExpression(System.Type createType, int size) { }
        public System.CodeDom.CodeTypeReference CreateType { get { throw null; } set { } }
        public System.CodeDom.CodeExpressionCollection Initializers { get { throw null; } }
        public int Size { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeExpression SizeExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeArrayIndexerExpression : System.CodeDom.CodeExpression
    {
        public CodeArrayIndexerExpression() { }
        public CodeArrayIndexerExpression(System.CodeDom.CodeExpression targetObject, params System.CodeDom.CodeExpression[] indices) { }
        public System.CodeDom.CodeExpressionCollection Indices { get { throw null; } }
        public System.CodeDom.CodeExpression TargetObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeAssignStatement : System.CodeDom.CodeStatement
    {
        public CodeAssignStatement() { }
        public CodeAssignStatement(System.CodeDom.CodeExpression left, System.CodeDom.CodeExpression right) { }
        public System.CodeDom.CodeExpression Left { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeExpression Right { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeAttachEventStatement : System.CodeDom.CodeStatement
    {
        public CodeAttachEventStatement() { }
        public CodeAttachEventStatement(System.CodeDom.CodeEventReferenceExpression eventRef, System.CodeDom.CodeExpression listener) { }
        public CodeAttachEventStatement(System.CodeDom.CodeExpression targetObject, string eventName, System.CodeDom.CodeExpression listener) { }
        public System.CodeDom.CodeEventReferenceExpression Event { get { throw null; } set { } }
        public System.CodeDom.CodeExpression Listener { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeAttributeArgument
    {
        public CodeAttributeArgument() { }
        public CodeAttributeArgument(System.CodeDom.CodeExpression value) { }
        public CodeAttributeArgument(string name, System.CodeDom.CodeExpression value) { }
        public string Name { get { throw null; } set { } }
        public System.CodeDom.CodeExpression Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeAttributeArgumentCollection : System.Collections.CollectionBase
    {
        public CodeAttributeArgumentCollection() { }
        public CodeAttributeArgumentCollection(System.CodeDom.CodeAttributeArgument[] value) { }
        public CodeAttributeArgumentCollection(System.CodeDom.CodeAttributeArgumentCollection value) { }
        public System.CodeDom.CodeAttributeArgument this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeAttributeArgument value) { throw null; }
        public void AddRange(System.CodeDom.CodeAttributeArgument[] value) { }
        public void AddRange(System.CodeDom.CodeAttributeArgumentCollection value) { }
        public bool Contains(System.CodeDom.CodeAttributeArgument value) { throw null; }
        public void CopyTo(System.CodeDom.CodeAttributeArgument[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeAttributeArgument value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeAttributeArgument value) { }
        public void Remove(System.CodeDom.CodeAttributeArgument value) { }
    }
    public partial class CodeAttributeDeclaration
    {
        public CodeAttributeDeclaration() { }
        public CodeAttributeDeclaration(System.CodeDom.CodeTypeReference attributeType) { }
        public CodeAttributeDeclaration(System.CodeDom.CodeTypeReference attributeType, params System.CodeDom.CodeAttributeArgument[] arguments) { }
        public CodeAttributeDeclaration(string name) { }
        public CodeAttributeDeclaration(string name, params System.CodeDom.CodeAttributeArgument[] arguments) { }
        public System.CodeDom.CodeAttributeArgumentCollection Arguments { get { throw null; } }
        public System.CodeDom.CodeTypeReference AttributeType { get { throw null; } }
        public string Name { get { throw null; } set { } }
    }
    public partial class CodeAttributeDeclarationCollection : System.Collections.CollectionBase
    {
        public CodeAttributeDeclarationCollection() { }
        public CodeAttributeDeclarationCollection(System.CodeDom.CodeAttributeDeclaration[] value) { }
        public CodeAttributeDeclarationCollection(System.CodeDom.CodeAttributeDeclarationCollection value) { }
        public System.CodeDom.CodeAttributeDeclaration this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeAttributeDeclaration value) { throw null; }
        public void AddRange(System.CodeDom.CodeAttributeDeclaration[] value) { }
        public void AddRange(System.CodeDom.CodeAttributeDeclarationCollection value) { }
        public bool Contains(System.CodeDom.CodeAttributeDeclaration value) { throw null; }
        public void CopyTo(System.CodeDom.CodeAttributeDeclaration[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeAttributeDeclaration value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeAttributeDeclaration value) { }
        public void Remove(System.CodeDom.CodeAttributeDeclaration value) { }
    }
    public partial class CodeBaseReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodeBaseReferenceExpression() { }
    }
    public partial class CodeBinaryOperatorExpression : System.CodeDom.CodeExpression
    {
        public CodeBinaryOperatorExpression() { }
        public CodeBinaryOperatorExpression(System.CodeDom.CodeExpression left, System.CodeDom.CodeBinaryOperatorType op, System.CodeDom.CodeExpression right) { }
        public System.CodeDom.CodeExpression Left { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeBinaryOperatorType Operator { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeExpression Right { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public enum CodeBinaryOperatorType
    {
        Add = 0,
        Assign = 5,
        BitwiseAnd = 10,
        BitwiseOr = 9,
        BooleanAnd = 12,
        BooleanOr = 11,
        Divide = 3,
        GreaterThan = 15,
        GreaterThanOrEqual = 16,
        IdentityEquality = 7,
        IdentityInequality = 6,
        LessThan = 13,
        LessThanOrEqual = 14,
        Modulus = 4,
        Multiply = 2,
        Subtract = 1,
        ValueEquality = 8,
    }
    public partial class CodeCastExpression : System.CodeDom.CodeExpression
    {
        public CodeCastExpression() { }
        public CodeCastExpression(System.CodeDom.CodeTypeReference targetType, System.CodeDom.CodeExpression expression) { }
        public CodeCastExpression(string targetType, System.CodeDom.CodeExpression expression) { }
        public CodeCastExpression(System.Type targetType, System.CodeDom.CodeExpression expression) { }
        public System.CodeDom.CodeExpression Expression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeTypeReference TargetType { get { throw null; } set { } }
    }
    public partial class CodeCatchClause
    {
        public CodeCatchClause() { }
        public CodeCatchClause(string localName) { }
        public CodeCatchClause(string localName, System.CodeDom.CodeTypeReference catchExceptionType) { }
        public CodeCatchClause(string localName, System.CodeDom.CodeTypeReference catchExceptionType, params System.CodeDom.CodeStatement[] statements) { }
        public System.CodeDom.CodeTypeReference CatchExceptionType { get { throw null; } set { } }
        public string LocalName { get { throw null; } set { } }
        public System.CodeDom.CodeStatementCollection Statements { get { throw null; } }
    }
    public partial class CodeCatchClauseCollection : System.Collections.CollectionBase
    {
        public CodeCatchClauseCollection() { }
        public CodeCatchClauseCollection(System.CodeDom.CodeCatchClause[] value) { }
        public CodeCatchClauseCollection(System.CodeDom.CodeCatchClauseCollection value) { }
        public System.CodeDom.CodeCatchClause this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeCatchClause value) { throw null; }
        public void AddRange(System.CodeDom.CodeCatchClause[] value) { }
        public void AddRange(System.CodeDom.CodeCatchClauseCollection value) { }
        public bool Contains(System.CodeDom.CodeCatchClause value) { throw null; }
        public void CopyTo(System.CodeDom.CodeCatchClause[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeCatchClause value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeCatchClause value) { }
        public void Remove(System.CodeDom.CodeCatchClause value) { }
    }
    public partial class CodeChecksumPragma : System.CodeDom.CodeDirective
    {
        public CodeChecksumPragma() { }
        public CodeChecksumPragma(string fileName, System.Guid checksumAlgorithmId, byte[] checksumData) { }
        public System.Guid ChecksumAlgorithmId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public byte[] ChecksumData { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string FileName { get { throw null; } set { } }
    }
    public partial class CodeComment : System.CodeDom.CodeObject
    {
        public CodeComment() { }
        public CodeComment(string text) { }
        public CodeComment(string text, bool docComment) { }
        public bool DocComment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Text { get { throw null; } set { } }
    }
    public partial class CodeCommentStatement : System.CodeDom.CodeStatement
    {
        public CodeCommentStatement() { }
        public CodeCommentStatement(System.CodeDom.CodeComment comment) { }
        public CodeCommentStatement(string text) { }
        public CodeCommentStatement(string text, bool docComment) { }
        public System.CodeDom.CodeComment Comment { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeCommentStatementCollection : System.Collections.CollectionBase
    {
        public CodeCommentStatementCollection() { }
        public CodeCommentStatementCollection(System.CodeDom.CodeCommentStatement[] value) { }
        public CodeCommentStatementCollection(System.CodeDom.CodeCommentStatementCollection value) { }
        public System.CodeDom.CodeCommentStatement this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeCommentStatement value) { throw null; }
        public void AddRange(System.CodeDom.CodeCommentStatement[] value) { }
        public void AddRange(System.CodeDom.CodeCommentStatementCollection value) { }
        public bool Contains(System.CodeDom.CodeCommentStatement value) { throw null; }
        public void CopyTo(System.CodeDom.CodeCommentStatement[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeCommentStatement value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeCommentStatement value) { }
        public void Remove(System.CodeDom.CodeCommentStatement value) { }
    }
    public partial class CodeCompileUnit : System.CodeDom.CodeObject
    {
        public CodeCompileUnit() { }
        public System.CodeDom.CodeAttributeDeclarationCollection AssemblyCustomAttributes { get { throw null; } }
        public System.CodeDom.CodeDirectiveCollection EndDirectives { get { throw null; } }
        public System.CodeDom.CodeNamespaceCollection Namespaces { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Specialized.StringCollection ReferencedAssemblies { get { throw null; } }
        public System.CodeDom.CodeDirectiveCollection StartDirectives { get { throw null; } }
    }
    public partial class CodeConditionStatement : System.CodeDom.CodeStatement
    {
        public CodeConditionStatement() { }
        public CodeConditionStatement(System.CodeDom.CodeExpression condition, params System.CodeDom.CodeStatement[] trueStatements) { }
        public CodeConditionStatement(System.CodeDom.CodeExpression condition, System.CodeDom.CodeStatement[] trueStatements, System.CodeDom.CodeStatement[] falseStatements) { }
        public System.CodeDom.CodeExpression Condition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeStatementCollection FalseStatements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeStatementCollection TrueStatements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class CodeConstructor : System.CodeDom.CodeMemberMethod
    {
        public CodeConstructor() { }
        public System.CodeDom.CodeExpressionCollection BaseConstructorArgs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeExpressionCollection ChainedConstructorArgs { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class CodeDefaultValueExpression : System.CodeDom.CodeExpression
    {
        public CodeDefaultValueExpression() { }
        public CodeDefaultValueExpression(System.CodeDom.CodeTypeReference type) { }
        public System.CodeDom.CodeTypeReference Type { get { throw null; } set { } }
    }
    public partial class CodeDelegateCreateExpression : System.CodeDom.CodeExpression
    {
        public CodeDelegateCreateExpression() { }
        public CodeDelegateCreateExpression(System.CodeDom.CodeTypeReference delegateType, System.CodeDom.CodeExpression targetObject, string methodName) { }
        public System.CodeDom.CodeTypeReference DelegateType { get { throw null; } set { } }
        public string MethodName { get { throw null; } set { } }
        public System.CodeDom.CodeExpression TargetObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeDelegateInvokeExpression : System.CodeDom.CodeExpression
    {
        public CodeDelegateInvokeExpression() { }
        public CodeDelegateInvokeExpression(System.CodeDom.CodeExpression targetObject) { }
        public CodeDelegateInvokeExpression(System.CodeDom.CodeExpression targetObject, params System.CodeDom.CodeExpression[] parameters) { }
        public System.CodeDom.CodeExpressionCollection Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeExpression TargetObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeDirectionExpression : System.CodeDom.CodeExpression
    {
        public CodeDirectionExpression() { }
        public CodeDirectionExpression(System.CodeDom.FieldDirection direction, System.CodeDom.CodeExpression expression) { }
        public System.CodeDom.FieldDirection Direction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeExpression Expression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeDirective : System.CodeDom.CodeObject
    {
        public CodeDirective() { }
    }
    public partial class CodeDirectiveCollection : System.Collections.CollectionBase
    {
        public CodeDirectiveCollection() { }
        public CodeDirectiveCollection(System.CodeDom.CodeDirective[] value) { }
        public CodeDirectiveCollection(System.CodeDom.CodeDirectiveCollection value) { }
        public System.CodeDom.CodeDirective this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeDirective value) { throw null; }
        public void AddRange(System.CodeDom.CodeDirective[] value) { }
        public void AddRange(System.CodeDom.CodeDirectiveCollection value) { }
        public bool Contains(System.CodeDom.CodeDirective value) { throw null; }
        public void CopyTo(System.CodeDom.CodeDirective[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeDirective value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeDirective value) { }
        public void Remove(System.CodeDom.CodeDirective value) { }
    }
    public partial class CodeEntryPointMethod : System.CodeDom.CodeMemberMethod
    {
        public CodeEntryPointMethod() { }
    }
    public partial class CodeEventReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodeEventReferenceExpression() { }
        public CodeEventReferenceExpression(System.CodeDom.CodeExpression targetObject, string eventName) { }
        public string EventName { get { throw null; } set { } }
        public System.CodeDom.CodeExpression TargetObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeExpression : System.CodeDom.CodeObject
    {
        public CodeExpression() { }
    }
    public partial class CodeExpressionCollection : System.Collections.CollectionBase
    {
        public CodeExpressionCollection() { }
        public CodeExpressionCollection(System.CodeDom.CodeExpression[] value) { }
        public CodeExpressionCollection(System.CodeDom.CodeExpressionCollection value) { }
        public System.CodeDom.CodeExpression this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeExpression value) { throw null; }
        public void AddRange(System.CodeDom.CodeExpression[] value) { }
        public void AddRange(System.CodeDom.CodeExpressionCollection value) { }
        public bool Contains(System.CodeDom.CodeExpression value) { throw null; }
        public void CopyTo(System.CodeDom.CodeExpression[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeExpression value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeExpression value) { }
        public void Remove(System.CodeDom.CodeExpression value) { }
    }
    public partial class CodeExpressionStatement : System.CodeDom.CodeStatement
    {
        public CodeExpressionStatement() { }
        public CodeExpressionStatement(System.CodeDom.CodeExpression expression) { }
        public System.CodeDom.CodeExpression Expression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeFieldReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodeFieldReferenceExpression() { }
        public CodeFieldReferenceExpression(System.CodeDom.CodeExpression targetObject, string fieldName) { }
        public string FieldName { get { throw null; } set { } }
        public System.CodeDom.CodeExpression TargetObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeGotoStatement : System.CodeDom.CodeStatement
    {
        public CodeGotoStatement() { }
        public CodeGotoStatement(string label) { }
        public string Label { get { throw null; } set { } }
    }
    public partial class CodeIndexerExpression : System.CodeDom.CodeExpression
    {
        public CodeIndexerExpression() { }
        public CodeIndexerExpression(System.CodeDom.CodeExpression targetObject, params System.CodeDom.CodeExpression[] indices) { }
        public System.CodeDom.CodeExpressionCollection Indices { get { throw null; } }
        public System.CodeDom.CodeExpression TargetObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeIterationStatement : System.CodeDom.CodeStatement
    {
        public CodeIterationStatement() { }
        public CodeIterationStatement(System.CodeDom.CodeStatement initStatement, System.CodeDom.CodeExpression testExpression, System.CodeDom.CodeStatement incrementStatement, params System.CodeDom.CodeStatement[] statements) { }
        public System.CodeDom.CodeStatement IncrementStatement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeStatement InitStatement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeStatementCollection Statements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeExpression TestExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeLabeledStatement : System.CodeDom.CodeStatement
    {
        public CodeLabeledStatement() { }
        public CodeLabeledStatement(string label) { }
        public CodeLabeledStatement(string label, System.CodeDom.CodeStatement statement) { }
        public string Label { get { throw null; } set { } }
        public System.CodeDom.CodeStatement Statement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeLinePragma
    {
        public CodeLinePragma() { }
        public CodeLinePragma(string fileName, int lineNumber) { }
        public string FileName { get { throw null; } set { } }
        public int LineNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeMemberEvent : System.CodeDom.CodeTypeMember
    {
        public CodeMemberEvent() { }
        public System.CodeDom.CodeTypeReferenceCollection ImplementationTypes { get { throw null; } }
        public System.CodeDom.CodeTypeReference PrivateImplementationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeTypeReference Type { get { throw null; } set { } }
    }
    public partial class CodeMemberField : System.CodeDom.CodeTypeMember
    {
        public CodeMemberField() { }
        public CodeMemberField(System.CodeDom.CodeTypeReference type, string name) { }
        public CodeMemberField(string type, string name) { }
        public CodeMemberField(System.Type type, string name) { }
        public System.CodeDom.CodeExpression InitExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeTypeReference Type { get { throw null; } set { } }
    }
    public partial class CodeMemberMethod : System.CodeDom.CodeTypeMember
    {
        public CodeMemberMethod() { }
        public System.CodeDom.CodeTypeReferenceCollection ImplementationTypes { get { throw null; } }
        public System.CodeDom.CodeParameterDeclarationExpressionCollection Parameters { get { throw null; } }
        public System.CodeDom.CodeTypeReference PrivateImplementationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeTypeReference ReturnType { get { throw null; } set { } }
        public System.CodeDom.CodeAttributeDeclarationCollection ReturnTypeCustomAttributes { get { throw null; } }
        public System.CodeDom.CodeStatementCollection Statements { get { throw null; } }
        public System.CodeDom.CodeTypeParameterCollection TypeParameters { get { throw null; } }
        public event System.EventHandler PopulateImplementationTypes { add { } remove { } }
        public event System.EventHandler PopulateParameters { add { } remove { } }
        public event System.EventHandler PopulateStatements { add { } remove { } }
    }
    public partial class CodeMemberProperty : System.CodeDom.CodeTypeMember
    {
        public CodeMemberProperty() { }
        public System.CodeDom.CodeStatementCollection GetStatements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool HasGet { get { throw null; } set { } }
        public bool HasSet { get { throw null; } set { } }
        public System.CodeDom.CodeTypeReferenceCollection ImplementationTypes { get { throw null; } }
        public System.CodeDom.CodeParameterDeclarationExpressionCollection Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeTypeReference PrivateImplementationType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeStatementCollection SetStatements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeTypeReference Type { get { throw null; } set { } }
    }
    public partial class CodeMethodInvokeExpression : System.CodeDom.CodeExpression
    {
        public CodeMethodInvokeExpression() { }
        public CodeMethodInvokeExpression(System.CodeDom.CodeExpression targetObject, string methodName, params System.CodeDom.CodeExpression[] parameters) { }
        public CodeMethodInvokeExpression(System.CodeDom.CodeMethodReferenceExpression method, params System.CodeDom.CodeExpression[] parameters) { }
        public System.CodeDom.CodeMethodReferenceExpression Method { get { throw null; } set { } }
        public System.CodeDom.CodeExpressionCollection Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class CodeMethodReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodeMethodReferenceExpression() { }
        public CodeMethodReferenceExpression(System.CodeDom.CodeExpression targetObject, string methodName) { }
        public CodeMethodReferenceExpression(System.CodeDom.CodeExpression targetObject, string methodName, params System.CodeDom.CodeTypeReference[] typeParameters) { }
        public string MethodName { get { throw null; } set { } }
        public System.CodeDom.CodeExpression TargetObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeTypeReferenceCollection TypeArguments { get { throw null; } }
    }
    public partial class CodeMethodReturnStatement : System.CodeDom.CodeStatement
    {
        public CodeMethodReturnStatement() { }
        public CodeMethodReturnStatement(System.CodeDom.CodeExpression expression) { }
        public System.CodeDom.CodeExpression Expression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeNamespace : System.CodeDom.CodeObject
    {
        public CodeNamespace() { }
        public CodeNamespace(string name) { }
        public System.CodeDom.CodeCommentStatementCollection Comments { get { throw null; } }
        public System.CodeDom.CodeNamespaceImportCollection Imports { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public System.CodeDom.CodeTypeDeclarationCollection Types { get { throw null; } }
        public event System.EventHandler PopulateComments { add { } remove { } }
        public event System.EventHandler PopulateImports { add { } remove { } }
        public event System.EventHandler PopulateTypes { add { } remove { } }
    }
    public partial class CodeNamespaceCollection : System.Collections.CollectionBase
    {
        public CodeNamespaceCollection() { }
        public CodeNamespaceCollection(System.CodeDom.CodeNamespace[] value) { }
        public CodeNamespaceCollection(System.CodeDom.CodeNamespaceCollection value) { }
        public System.CodeDom.CodeNamespace this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeNamespace value) { throw null; }
        public void AddRange(System.CodeDom.CodeNamespace[] value) { }
        public void AddRange(System.CodeDom.CodeNamespaceCollection value) { }
        public bool Contains(System.CodeDom.CodeNamespace value) { throw null; }
        public void CopyTo(System.CodeDom.CodeNamespace[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeNamespace value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeNamespace value) { }
        public void Remove(System.CodeDom.CodeNamespace value) { }
    }
    public partial class CodeNamespaceImport : System.CodeDom.CodeObject
    {
        public CodeNamespaceImport() { }
        public CodeNamespaceImport(string nameSpace) { }
        public System.CodeDom.CodeLinePragma LinePragma { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Namespace { get { throw null; } set { } }
    }
    public partial class CodeNamespaceImportCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public CodeNamespaceImportCollection() { }
        public int Count { get { throw null; } }
        public System.CodeDom.CodeNamespaceImport this[int index] { get { throw null; } set { } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object System.Collections.IList.this[int index] { get { throw null; } set { } }
        public void Add(System.CodeDom.CodeNamespaceImport value) { }
        public void AddRange(System.CodeDom.CodeNamespaceImport[] value) { }
        public void Clear() { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object value) { throw null; }
        int System.Collections.IList.IndexOf(object value) { throw null; }
        void System.Collections.IList.Insert(int index, object value) { }
        void System.Collections.IList.Remove(object value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
    public partial class CodeObject
    {
        public CodeObject() { }
        public System.Collections.IDictionary UserData { get { throw null; } }
    }
    public partial class CodeObjectCreateExpression : System.CodeDom.CodeExpression
    {
        public CodeObjectCreateExpression() { }
        public CodeObjectCreateExpression(System.CodeDom.CodeTypeReference createType, params System.CodeDom.CodeExpression[] parameters) { }
        public CodeObjectCreateExpression(string createType, params System.CodeDom.CodeExpression[] parameters) { }
        public CodeObjectCreateExpression(System.Type createType, params System.CodeDom.CodeExpression[] parameters) { }
        public System.CodeDom.CodeTypeReference CreateType { get { throw null; } set { } }
        public System.CodeDom.CodeExpressionCollection Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class CodeParameterDeclarationExpression : System.CodeDom.CodeExpression
    {
        public CodeParameterDeclarationExpression() { }
        public CodeParameterDeclarationExpression(System.CodeDom.CodeTypeReference type, string name) { }
        public CodeParameterDeclarationExpression(string type, string name) { }
        public CodeParameterDeclarationExpression(System.Type type, string name) { }
        public System.CodeDom.CodeAttributeDeclarationCollection CustomAttributes { get { throw null; } set { } }
        public System.CodeDom.FieldDirection Direction { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { get { throw null; } set { } }
        public System.CodeDom.CodeTypeReference Type { get { throw null; } set { } }
    }
    public partial class CodeParameterDeclarationExpressionCollection : System.Collections.CollectionBase
    {
        public CodeParameterDeclarationExpressionCollection() { }
        public CodeParameterDeclarationExpressionCollection(System.CodeDom.CodeParameterDeclarationExpression[] value) { }
        public CodeParameterDeclarationExpressionCollection(System.CodeDom.CodeParameterDeclarationExpressionCollection value) { }
        public System.CodeDom.CodeParameterDeclarationExpression this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeParameterDeclarationExpression value) { throw null; }
        public void AddRange(System.CodeDom.CodeParameterDeclarationExpression[] value) { }
        public void AddRange(System.CodeDom.CodeParameterDeclarationExpressionCollection value) { }
        public bool Contains(System.CodeDom.CodeParameterDeclarationExpression value) { throw null; }
        public void CopyTo(System.CodeDom.CodeParameterDeclarationExpression[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeParameterDeclarationExpression value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeParameterDeclarationExpression value) { }
        public void Remove(System.CodeDom.CodeParameterDeclarationExpression value) { }
    }
    public partial class CodePrimitiveExpression : System.CodeDom.CodeExpression
    {
        public CodePrimitiveExpression() { }
        public CodePrimitiveExpression(object value) { }
        public object Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodePropertyReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodePropertyReferenceExpression() { }
        public CodePropertyReferenceExpression(System.CodeDom.CodeExpression targetObject, string propertyName) { }
        public string PropertyName { get { throw null; } set { } }
        public System.CodeDom.CodeExpression TargetObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodePropertySetValueReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodePropertySetValueReferenceExpression() { }
    }
    public partial class CodeRegionDirective : System.CodeDom.CodeDirective
    {
        public CodeRegionDirective() { }
        public CodeRegionDirective(System.CodeDom.CodeRegionMode regionMode, string regionText) { }
        public System.CodeDom.CodeRegionMode RegionMode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string RegionText { get { throw null; } set { } }
    }
    public enum CodeRegionMode
    {
        End = 2,
        None = 0,
        Start = 1,
    }
    public partial class CodeRemoveEventStatement : System.CodeDom.CodeStatement
    {
        public CodeRemoveEventStatement() { }
        public CodeRemoveEventStatement(System.CodeDom.CodeEventReferenceExpression eventRef, System.CodeDom.CodeExpression listener) { }
        public CodeRemoveEventStatement(System.CodeDom.CodeExpression targetObject, string eventName, System.CodeDom.CodeExpression listener) { }
        public System.CodeDom.CodeEventReferenceExpression Event { get { throw null; } set { } }
        public System.CodeDom.CodeExpression Listener { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeSnippetCompileUnit : System.CodeDom.CodeCompileUnit
    {
        public CodeSnippetCompileUnit() { }
        public CodeSnippetCompileUnit(string value) { }
        public System.CodeDom.CodeLinePragma LinePragma { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Value { get { throw null; } set { } }
    }
    public partial class CodeSnippetExpression : System.CodeDom.CodeExpression
    {
        public CodeSnippetExpression() { }
        public CodeSnippetExpression(string value) { }
        public string Value { get { throw null; } set { } }
    }
    public partial class CodeSnippetStatement : System.CodeDom.CodeStatement
    {
        public CodeSnippetStatement() { }
        public CodeSnippetStatement(string value) { }
        public string Value { get { throw null; } set { } }
    }
    public partial class CodeSnippetTypeMember : System.CodeDom.CodeTypeMember
    {
        public CodeSnippetTypeMember() { }
        public CodeSnippetTypeMember(string text) { }
        public string Text { get { throw null; } set { } }
    }
    public partial class CodeStatement : System.CodeDom.CodeObject
    {
        public CodeStatement() { }
        public System.CodeDom.CodeDirectiveCollection EndDirectives { get { throw null; } }
        public System.CodeDom.CodeLinePragma LinePragma { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeDirectiveCollection StartDirectives { get { throw null; } }
    }
    public partial class CodeStatementCollection : System.Collections.CollectionBase
    {
        public CodeStatementCollection() { }
        public CodeStatementCollection(System.CodeDom.CodeStatement[] value) { }
        public CodeStatementCollection(System.CodeDom.CodeStatementCollection value) { }
        public System.CodeDom.CodeStatement this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeExpression value) { throw null; }
        public int Add(System.CodeDom.CodeStatement value) { throw null; }
        public void AddRange(System.CodeDom.CodeStatement[] value) { }
        public void AddRange(System.CodeDom.CodeStatementCollection value) { }
        public bool Contains(System.CodeDom.CodeStatement value) { throw null; }
        public void CopyTo(System.CodeDom.CodeStatement[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeStatement value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeStatement value) { }
        public void Remove(System.CodeDom.CodeStatement value) { }
    }
    public partial class CodeThisReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodeThisReferenceExpression() { }
    }
    public partial class CodeThrowExceptionStatement : System.CodeDom.CodeStatement
    {
        public CodeThrowExceptionStatement() { }
        public CodeThrowExceptionStatement(System.CodeDom.CodeExpression toThrow) { }
        public System.CodeDom.CodeExpression ToThrow { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CodeTryCatchFinallyStatement : System.CodeDom.CodeStatement
    {
        public CodeTryCatchFinallyStatement() { }
        public CodeTryCatchFinallyStatement(System.CodeDom.CodeStatement[] tryStatements, System.CodeDom.CodeCatchClause[] catchClauses) { }
        public CodeTryCatchFinallyStatement(System.CodeDom.CodeStatement[] tryStatements, System.CodeDom.CodeCatchClause[] catchClauses, System.CodeDom.CodeStatement[] finallyStatements) { }
        public System.CodeDom.CodeCatchClauseCollection CatchClauses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeStatementCollection FinallyStatements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeStatementCollection TryStatements { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class CodeTypeConstructor : System.CodeDom.CodeMemberMethod
    {
        public CodeTypeConstructor() { }
    }
    public partial class CodeTypeDeclaration : System.CodeDom.CodeTypeMember
    {
        public CodeTypeDeclaration() { }
        public CodeTypeDeclaration(string name) { }
        public System.CodeDom.CodeTypeReferenceCollection BaseTypes { get { throw null; } }
        public bool IsClass { get { throw null; } set { } }
        public bool IsEnum { get { throw null; } set { } }
        public bool IsInterface { get { throw null; } set { } }
        public bool IsPartial { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsStruct { get { throw null; } set { } }
        public System.CodeDom.CodeTypeMemberCollection Members { get { throw null; } }
        public System.Reflection.TypeAttributes TypeAttributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeTypeParameterCollection TypeParameters { get { throw null; } }
        public event System.EventHandler PopulateBaseTypes { add { } remove { } }
        public event System.EventHandler PopulateMembers { add { } remove { } }
    }
    public partial class CodeTypeDeclarationCollection : System.Collections.CollectionBase
    {
        public CodeTypeDeclarationCollection() { }
        public CodeTypeDeclarationCollection(System.CodeDom.CodeTypeDeclaration[] value) { }
        public CodeTypeDeclarationCollection(System.CodeDom.CodeTypeDeclarationCollection value) { }
        public System.CodeDom.CodeTypeDeclaration this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeTypeDeclaration value) { throw null; }
        public void AddRange(System.CodeDom.CodeTypeDeclaration[] value) { }
        public void AddRange(System.CodeDom.CodeTypeDeclarationCollection value) { }
        public bool Contains(System.CodeDom.CodeTypeDeclaration value) { throw null; }
        public void CopyTo(System.CodeDom.CodeTypeDeclaration[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeTypeDeclaration value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeTypeDeclaration value) { }
        public void Remove(System.CodeDom.CodeTypeDeclaration value) { }
    }
    public partial class CodeTypeDelegate : System.CodeDom.CodeTypeDeclaration
    {
        public CodeTypeDelegate() { }
        public CodeTypeDelegate(string name) { }
        public System.CodeDom.CodeParameterDeclarationExpressionCollection Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeTypeReference ReturnType { get { throw null; } set { } }
    }
    public partial class CodeTypeMember : System.CodeDom.CodeObject
    {
        public CodeTypeMember() { }
        public System.CodeDom.MemberAttributes Attributes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeCommentStatementCollection Comments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.CodeDom.CodeAttributeDeclarationCollection CustomAttributes { get { throw null; } set { } }
        public System.CodeDom.CodeDirectiveCollection EndDirectives { get { throw null; } }
        public System.CodeDom.CodeLinePragma LinePragma { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { get { throw null; } set { } }
        public System.CodeDom.CodeDirectiveCollection StartDirectives { get { throw null; } }
    }
    public partial class CodeTypeMemberCollection : System.Collections.CollectionBase
    {
        public CodeTypeMemberCollection() { }
        public CodeTypeMemberCollection(System.CodeDom.CodeTypeMember[] value) { }
        public CodeTypeMemberCollection(System.CodeDom.CodeTypeMemberCollection value) { }
        public System.CodeDom.CodeTypeMember this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeTypeMember value) { throw null; }
        public void AddRange(System.CodeDom.CodeTypeMember[] value) { }
        public void AddRange(System.CodeDom.CodeTypeMemberCollection value) { }
        public bool Contains(System.CodeDom.CodeTypeMember value) { throw null; }
        public void CopyTo(System.CodeDom.CodeTypeMember[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeTypeMember value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeTypeMember value) { }
        public void Remove(System.CodeDom.CodeTypeMember value) { }
    }
    public partial class CodeTypeOfExpression : System.CodeDom.CodeExpression
    {
        public CodeTypeOfExpression() { }
        public CodeTypeOfExpression(System.CodeDom.CodeTypeReference type) { }
        public CodeTypeOfExpression(string type) { }
        public CodeTypeOfExpression(System.Type type) { }
        public System.CodeDom.CodeTypeReference Type { get { throw null; } set { } }
    }
    public partial class CodeTypeParameter : System.CodeDom.CodeObject
    {
        public CodeTypeParameter() { }
        public CodeTypeParameter(string name) { }
        public System.CodeDom.CodeTypeReferenceCollection Constraints { get { throw null; } }
        public System.CodeDom.CodeAttributeDeclarationCollection CustomAttributes { get { throw null; } }
        public bool HasConstructorConstraint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { get { throw null; } set { } }
    }
    public partial class CodeTypeParameterCollection : System.Collections.CollectionBase
    {
        public CodeTypeParameterCollection() { }
        public CodeTypeParameterCollection(System.CodeDom.CodeTypeParameter[] value) { }
        public CodeTypeParameterCollection(System.CodeDom.CodeTypeParameterCollection value) { }
        public System.CodeDom.CodeTypeParameter this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeTypeParameter value) { throw null; }
        public void Add(string value) { }
        public void AddRange(System.CodeDom.CodeTypeParameter[] value) { }
        public void AddRange(System.CodeDom.CodeTypeParameterCollection value) { }
        public bool Contains(System.CodeDom.CodeTypeParameter value) { throw null; }
        public void CopyTo(System.CodeDom.CodeTypeParameter[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeTypeParameter value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeTypeParameter value) { }
        public void Remove(System.CodeDom.CodeTypeParameter value) { }
    }
    public partial class CodeTypeReference : System.CodeDom.CodeObject
    {
        public CodeTypeReference() { }
        public CodeTypeReference(System.CodeDom.CodeTypeParameter typeParameter) { }
        public CodeTypeReference(System.CodeDom.CodeTypeReference arrayType, int rank) { }
        public CodeTypeReference(string typeName) { }
        public CodeTypeReference(string typeName, params System.CodeDom.CodeTypeReference[] typeArguments) { }
        public CodeTypeReference(string typeName, System.CodeDom.CodeTypeReferenceOptions codeTypeReferenceOption) { }
        public CodeTypeReference(string baseType, int rank) { }
        public CodeTypeReference(System.Type type) { }
        public CodeTypeReference(System.Type type, System.CodeDom.CodeTypeReferenceOptions codeTypeReferenceOption) { }
        public System.CodeDom.CodeTypeReference ArrayElementType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int ArrayRank { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string BaseType { get { throw null; } set { } }
        public System.CodeDom.CodeTypeReferenceOptions Options { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.CodeTypeReferenceCollection TypeArguments { get { throw null; } }
    }
    public partial class CodeTypeReferenceCollection : System.Collections.CollectionBase
    {
        public CodeTypeReferenceCollection() { }
        public CodeTypeReferenceCollection(System.CodeDom.CodeTypeReference[] value) { }
        public CodeTypeReferenceCollection(System.CodeDom.CodeTypeReferenceCollection value) { }
        public System.CodeDom.CodeTypeReference this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.CodeTypeReference value) { throw null; }
        public void Add(string value) { }
        public void Add(System.Type value) { }
        public void AddRange(System.CodeDom.CodeTypeReference[] value) { }
        public void AddRange(System.CodeDom.CodeTypeReferenceCollection value) { }
        public bool Contains(System.CodeDom.CodeTypeReference value) { throw null; }
        public void CopyTo(System.CodeDom.CodeTypeReference[] array, int index) { }
        public int IndexOf(System.CodeDom.CodeTypeReference value) { throw null; }
        public void Insert(int index, System.CodeDom.CodeTypeReference value) { }
        public void Remove(System.CodeDom.CodeTypeReference value) { }
    }
    public partial class CodeTypeReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodeTypeReferenceExpression() { }
        public CodeTypeReferenceExpression(System.CodeDom.CodeTypeReference type) { }
        public CodeTypeReferenceExpression(string type) { }
        public CodeTypeReferenceExpression(System.Type type) { }
        public System.CodeDom.CodeTypeReference Type { get { throw null; } set { } }
    }
    [System.FlagsAttribute]
    public enum CodeTypeReferenceOptions
    {
        GenericTypeParameter = 2,
        GlobalReference = 1,
    }
    public partial class CodeVariableDeclarationStatement : System.CodeDom.CodeStatement
    {
        public CodeVariableDeclarationStatement() { }
        public CodeVariableDeclarationStatement(System.CodeDom.CodeTypeReference type, string name) { }
        public CodeVariableDeclarationStatement(System.CodeDom.CodeTypeReference type, string name, System.CodeDom.CodeExpression initExpression) { }
        public CodeVariableDeclarationStatement(string type, string name) { }
        public CodeVariableDeclarationStatement(string type, string name, System.CodeDom.CodeExpression initExpression) { }
        public CodeVariableDeclarationStatement(System.Type type, string name) { }
        public CodeVariableDeclarationStatement(System.Type type, string name, System.CodeDom.CodeExpression initExpression) { }
        public System.CodeDom.CodeExpression InitExpression { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { get { throw null; } set { } }
        public System.CodeDom.CodeTypeReference Type { get { throw null; } set { } }
    }
    public partial class CodeVariableReferenceExpression : System.CodeDom.CodeExpression
    {
        public CodeVariableReferenceExpression() { }
        public CodeVariableReferenceExpression(string variableName) { }
        public string VariableName { get { throw null; } set { } }
    }
    public enum FieldDirection
    {
        In = 0,
        Out = 1,
        Ref = 2,
    }
    public enum MemberAttributes
    {
        Abstract = 1,
        AccessMask = 61440,
        Assembly = 4096,
        Const = 5,
        Family = 12288,
        FamilyAndAssembly = 8192,
        FamilyOrAssembly = 16384,
        Final = 2,
        New = 16,
        Overloaded = 256,
        Override = 4,
        Private = 20480,
        Public = 24576,
        ScopeMask = 15,
        Static = 3,
        VTableMask = 240,
    }
}
namespace System.CodeDom.Compiler
{
    public abstract partial class CodeCompiler : System.CodeDom.Compiler.CodeGenerator, System.CodeDom.Compiler.ICodeCompiler
    {
        protected CodeCompiler() { }
        protected abstract string CompilerName { get; }
        protected abstract string FileExtension { get; }
        protected abstract string CmdArgsFromParameters(System.CodeDom.Compiler.CompilerParameters options);
        protected virtual System.CodeDom.Compiler.CompilerResults FromDom(System.CodeDom.Compiler.CompilerParameters options, System.CodeDom.CodeCompileUnit e) { throw null; }
        protected virtual System.CodeDom.Compiler.CompilerResults FromDomBatch(System.CodeDom.Compiler.CompilerParameters options, System.CodeDom.CodeCompileUnit[] ea) { throw null; }
        protected virtual System.CodeDom.Compiler.CompilerResults FromFile(System.CodeDom.Compiler.CompilerParameters options, string fileName) { throw null; }
        protected virtual System.CodeDom.Compiler.CompilerResults FromFileBatch(System.CodeDom.Compiler.CompilerParameters options, string[] fileNames) { throw null; }
        protected virtual System.CodeDom.Compiler.CompilerResults FromSource(System.CodeDom.Compiler.CompilerParameters options, string source) { throw null; }
        protected virtual System.CodeDom.Compiler.CompilerResults FromSourceBatch(System.CodeDom.Compiler.CompilerParameters options, string[] sources) { throw null; }
        protected virtual string GetResponseFileCmdArgs(System.CodeDom.Compiler.CompilerParameters options, string cmdArgs) { throw null; }
        protected static string JoinStringArray(string[] sa, string separator) { throw null; }
        protected abstract void ProcessCompilerOutputLine(System.CodeDom.Compiler.CompilerResults results, string line);
        System.CodeDom.Compiler.CompilerResults System.CodeDom.Compiler.ICodeCompiler.CompileAssemblyFromDom(System.CodeDom.Compiler.CompilerParameters options, System.CodeDom.CodeCompileUnit e) { throw null; }
        System.CodeDom.Compiler.CompilerResults System.CodeDom.Compiler.ICodeCompiler.CompileAssemblyFromDomBatch(System.CodeDom.Compiler.CompilerParameters options, System.CodeDom.CodeCompileUnit[] ea) { throw null; }
        System.CodeDom.Compiler.CompilerResults System.CodeDom.Compiler.ICodeCompiler.CompileAssemblyFromFile(System.CodeDom.Compiler.CompilerParameters options, string fileName) { throw null; }
        System.CodeDom.Compiler.CompilerResults System.CodeDom.Compiler.ICodeCompiler.CompileAssemblyFromFileBatch(System.CodeDom.Compiler.CompilerParameters options, string[] fileNames) { throw null; }
        System.CodeDom.Compiler.CompilerResults System.CodeDom.Compiler.ICodeCompiler.CompileAssemblyFromSource(System.CodeDom.Compiler.CompilerParameters options, string source) { throw null; }
        System.CodeDom.Compiler.CompilerResults System.CodeDom.Compiler.ICodeCompiler.CompileAssemblyFromSourceBatch(System.CodeDom.Compiler.CompilerParameters options, string[] sources) { throw null; }
    }
    public abstract partial class CodeDomProvider : System.ComponentModel.Component
    {
        protected CodeDomProvider() { }
        public virtual string FileExtension { get { throw null; } }
        public virtual System.CodeDom.Compiler.LanguageOptions LanguageOptions { get { throw null; } }
        public virtual System.CodeDom.Compiler.CompilerResults CompileAssemblyFromDom(System.CodeDom.Compiler.CompilerParameters options, params System.CodeDom.CodeCompileUnit[] compilationUnits) { throw null; }
        public virtual System.CodeDom.Compiler.CompilerResults CompileAssemblyFromFile(System.CodeDom.Compiler.CompilerParameters options, params string[] fileNames) { throw null; }
        public virtual System.CodeDom.Compiler.CompilerResults CompileAssemblyFromSource(System.CodeDom.Compiler.CompilerParameters options, params string[] sources) { throw null; }
        [System.ObsoleteAttribute("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public abstract System.CodeDom.Compiler.ICodeCompiler CreateCompiler();
        public virtual string CreateEscapedIdentifier(string value) { throw null; }
        [System.ObsoleteAttribute("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public abstract System.CodeDom.Compiler.ICodeGenerator CreateGenerator();
        public virtual System.CodeDom.Compiler.ICodeGenerator CreateGenerator(System.IO.TextWriter output) { throw null; }
        public virtual System.CodeDom.Compiler.ICodeGenerator CreateGenerator(string fileName) { throw null; }
        [System.ObsoleteAttribute("Callers should not use the ICodeParser interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public virtual System.CodeDom.Compiler.ICodeParser CreateParser() { throw null; }
        public static System.CodeDom.Compiler.CodeDomProvider CreateProvider(string language) { throw null; }
        public static System.CodeDom.Compiler.CodeDomProvider CreateProvider(string language, System.Collections.Generic.IDictionary<string, string> providerOptions) { throw null; }
        public virtual string CreateValidIdentifier(string value) { throw null; }
        public virtual void GenerateCodeFromCompileUnit(System.CodeDom.CodeCompileUnit compileUnit, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        public virtual void GenerateCodeFromExpression(System.CodeDom.CodeExpression expression, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        public virtual void GenerateCodeFromMember(System.CodeDom.CodeTypeMember member, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        public virtual void GenerateCodeFromNamespace(System.CodeDom.CodeNamespace codeNamespace, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        public virtual void GenerateCodeFromStatement(System.CodeDom.CodeStatement statement, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        public virtual void GenerateCodeFromType(System.CodeDom.CodeTypeDeclaration codeType, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        public static System.CodeDom.Compiler.CompilerInfo[] GetAllCompilerInfo() { throw null; }
        public static System.CodeDom.Compiler.CompilerInfo GetCompilerInfo(string language) { throw null; }
        public virtual System.ComponentModel.TypeConverter GetConverter(System.Type type) { throw null; }
        public static string GetLanguageFromExtension(string extension) { throw null; }
        public virtual string GetTypeOutput(System.CodeDom.CodeTypeReference type) { throw null; }
        public static bool IsDefinedExtension(string extension) { throw null; }
        public static bool IsDefinedLanguage(string language) { throw null; }
        public virtual bool IsValidIdentifier(string value) { throw null; }
        public virtual System.CodeDom.CodeCompileUnit Parse(System.IO.TextReader codeStream) { throw null; }
        public virtual bool Supports(System.CodeDom.Compiler.GeneratorSupport generatorSupport) { throw null; }
    }
    public abstract partial class CodeGenerator : System.CodeDom.Compiler.ICodeGenerator
    {
        protected CodeGenerator() { }
        protected System.CodeDom.CodeTypeDeclaration CurrentClass { get { throw null; } }
        protected System.CodeDom.CodeTypeMember CurrentMember { get { throw null; } }
        protected string CurrentMemberName { get { throw null; } }
        protected string CurrentTypeName { get { throw null; } }
        protected int Indent { get { throw null; } set { } }
        protected bool IsCurrentClass { get { throw null; } }
        protected bool IsCurrentDelegate { get { throw null; } }
        protected bool IsCurrentEnum { get { throw null; } }
        protected bool IsCurrentInterface { get { throw null; } }
        protected bool IsCurrentStruct { get { throw null; } }
        protected abstract string NullToken { get; }
        protected System.CodeDom.Compiler.CodeGeneratorOptions Options { get { throw null; } }
        protected System.IO.TextWriter Output { get { throw null; } }
        protected virtual void ContinueOnNewLine(string st) { }
        protected abstract string CreateEscapedIdentifier(string value);
        protected abstract string CreateValidIdentifier(string value);
        protected abstract void GenerateArgumentReferenceExpression(System.CodeDom.CodeArgumentReferenceExpression e);
        protected abstract void GenerateArrayCreateExpression(System.CodeDom.CodeArrayCreateExpression e);
        protected abstract void GenerateArrayIndexerExpression(System.CodeDom.CodeArrayIndexerExpression e);
        protected abstract void GenerateAssignStatement(System.CodeDom.CodeAssignStatement e);
        protected abstract void GenerateAttachEventStatement(System.CodeDom.CodeAttachEventStatement e);
        protected abstract void GenerateAttributeDeclarationsEnd(System.CodeDom.CodeAttributeDeclarationCollection attributes);
        protected abstract void GenerateAttributeDeclarationsStart(System.CodeDom.CodeAttributeDeclarationCollection attributes);
        protected abstract void GenerateBaseReferenceExpression(System.CodeDom.CodeBaseReferenceExpression e);
        protected virtual void GenerateBinaryOperatorExpression(System.CodeDom.CodeBinaryOperatorExpression e) { }
        protected abstract void GenerateCastExpression(System.CodeDom.CodeCastExpression e);
        public virtual void GenerateCodeFromMember(System.CodeDom.CodeTypeMember member, System.IO.TextWriter writer, System.CodeDom.Compiler.CodeGeneratorOptions options) { }
        protected abstract void GenerateComment(System.CodeDom.CodeComment e);
        protected virtual void GenerateCommentStatement(System.CodeDom.CodeCommentStatement e) { }
        protected virtual void GenerateCommentStatements(System.CodeDom.CodeCommentStatementCollection e) { }
        protected virtual void GenerateCompileUnit(System.CodeDom.CodeCompileUnit e) { }
        protected virtual void GenerateCompileUnitEnd(System.CodeDom.CodeCompileUnit e) { }
        protected virtual void GenerateCompileUnitStart(System.CodeDom.CodeCompileUnit e) { }
        protected abstract void GenerateConditionStatement(System.CodeDom.CodeConditionStatement e);
        protected abstract void GenerateConstructor(System.CodeDom.CodeConstructor e, System.CodeDom.CodeTypeDeclaration c);
        protected virtual void GenerateDecimalValue(decimal d) { }
        protected virtual void GenerateDefaultValueExpression(System.CodeDom.CodeDefaultValueExpression e) { }
        protected abstract void GenerateDelegateCreateExpression(System.CodeDom.CodeDelegateCreateExpression e);
        protected abstract void GenerateDelegateInvokeExpression(System.CodeDom.CodeDelegateInvokeExpression e);
        protected virtual void GenerateDirectionExpression(System.CodeDom.CodeDirectionExpression e) { }
        protected virtual void GenerateDirectives(System.CodeDom.CodeDirectiveCollection directives) { }
        protected virtual void GenerateDoubleValue(double d) { }
        protected abstract void GenerateEntryPointMethod(System.CodeDom.CodeEntryPointMethod e, System.CodeDom.CodeTypeDeclaration c);
        protected abstract void GenerateEvent(System.CodeDom.CodeMemberEvent e, System.CodeDom.CodeTypeDeclaration c);
        protected abstract void GenerateEventReferenceExpression(System.CodeDom.CodeEventReferenceExpression e);
        protected void GenerateExpression(System.CodeDom.CodeExpression e) { }
        protected abstract void GenerateExpressionStatement(System.CodeDom.CodeExpressionStatement e);
        protected abstract void GenerateField(System.CodeDom.CodeMemberField e);
        protected abstract void GenerateFieldReferenceExpression(System.CodeDom.CodeFieldReferenceExpression e);
        protected abstract void GenerateGotoStatement(System.CodeDom.CodeGotoStatement e);
        protected abstract void GenerateIndexerExpression(System.CodeDom.CodeIndexerExpression e);
        protected abstract void GenerateIterationStatement(System.CodeDom.CodeIterationStatement e);
        protected abstract void GenerateLabeledStatement(System.CodeDom.CodeLabeledStatement e);
        protected abstract void GenerateLinePragmaEnd(System.CodeDom.CodeLinePragma e);
        protected abstract void GenerateLinePragmaStart(System.CodeDom.CodeLinePragma e);
        protected abstract void GenerateMethod(System.CodeDom.CodeMemberMethod e, System.CodeDom.CodeTypeDeclaration c);
        protected abstract void GenerateMethodInvokeExpression(System.CodeDom.CodeMethodInvokeExpression e);
        protected abstract void GenerateMethodReferenceExpression(System.CodeDom.CodeMethodReferenceExpression e);
        protected abstract void GenerateMethodReturnStatement(System.CodeDom.CodeMethodReturnStatement e);
        protected virtual void GenerateNamespace(System.CodeDom.CodeNamespace e) { }
        protected abstract void GenerateNamespaceEnd(System.CodeDom.CodeNamespace e);
        protected abstract void GenerateNamespaceImport(System.CodeDom.CodeNamespaceImport e);
        protected void GenerateNamespaceImports(System.CodeDom.CodeNamespace e) { }
        protected void GenerateNamespaces(System.CodeDom.CodeCompileUnit e) { }
        protected abstract void GenerateNamespaceStart(System.CodeDom.CodeNamespace e);
        protected abstract void GenerateObjectCreateExpression(System.CodeDom.CodeObjectCreateExpression e);
        protected virtual void GenerateParameterDeclarationExpression(System.CodeDom.CodeParameterDeclarationExpression e) { }
        protected virtual void GeneratePrimitiveExpression(System.CodeDom.CodePrimitiveExpression e) { }
        protected abstract void GenerateProperty(System.CodeDom.CodeMemberProperty e, System.CodeDom.CodeTypeDeclaration c);
        protected abstract void GeneratePropertyReferenceExpression(System.CodeDom.CodePropertyReferenceExpression e);
        protected abstract void GeneratePropertySetValueReferenceExpression(System.CodeDom.CodePropertySetValueReferenceExpression e);
        protected abstract void GenerateRemoveEventStatement(System.CodeDom.CodeRemoveEventStatement e);
        protected virtual void GenerateSingleFloatValue(float s) { }
        protected virtual void GenerateSnippetCompileUnit(System.CodeDom.CodeSnippetCompileUnit e) { }
        protected abstract void GenerateSnippetExpression(System.CodeDom.CodeSnippetExpression e);
        protected abstract void GenerateSnippetMember(System.CodeDom.CodeSnippetTypeMember e);
        protected virtual void GenerateSnippetStatement(System.CodeDom.CodeSnippetStatement e) { }
        protected void GenerateStatement(System.CodeDom.CodeStatement e) { }
        protected void GenerateStatements(System.CodeDom.CodeStatementCollection stms) { }
        protected abstract void GenerateThisReferenceExpression(System.CodeDom.CodeThisReferenceExpression e);
        protected abstract void GenerateThrowExceptionStatement(System.CodeDom.CodeThrowExceptionStatement e);
        protected abstract void GenerateTryCatchFinallyStatement(System.CodeDom.CodeTryCatchFinallyStatement e);
        protected abstract void GenerateTypeConstructor(System.CodeDom.CodeTypeConstructor e);
        protected abstract void GenerateTypeEnd(System.CodeDom.CodeTypeDeclaration e);
        protected virtual void GenerateTypeOfExpression(System.CodeDom.CodeTypeOfExpression e) { }
        protected virtual void GenerateTypeReferenceExpression(System.CodeDom.CodeTypeReferenceExpression e) { }
        protected void GenerateTypes(System.CodeDom.CodeNamespace e) { }
        protected abstract void GenerateTypeStart(System.CodeDom.CodeTypeDeclaration e);
        protected abstract void GenerateVariableDeclarationStatement(System.CodeDom.CodeVariableDeclarationStatement e);
        protected abstract void GenerateVariableReferenceExpression(System.CodeDom.CodeVariableReferenceExpression e);
        protected abstract string GetTypeOutput(System.CodeDom.CodeTypeReference value);
        protected abstract bool IsValidIdentifier(string value);
        public static bool IsValidLanguageIndependentIdentifier(string value) { throw null; }
        protected virtual void OutputAttributeArgument(System.CodeDom.CodeAttributeArgument arg) { }
        protected virtual void OutputAttributeDeclarations(System.CodeDom.CodeAttributeDeclarationCollection attributes) { }
        protected virtual void OutputDirection(System.CodeDom.FieldDirection dir) { }
        protected virtual void OutputExpressionList(System.CodeDom.CodeExpressionCollection expressions) { }
        protected virtual void OutputExpressionList(System.CodeDom.CodeExpressionCollection expressions, bool newlineBetweenItems) { }
        protected virtual void OutputFieldScopeModifier(System.CodeDom.MemberAttributes attributes) { }
        protected virtual void OutputIdentifier(string ident) { }
        protected virtual void OutputMemberAccessModifier(System.CodeDom.MemberAttributes attributes) { }
        protected virtual void OutputMemberScopeModifier(System.CodeDom.MemberAttributes attributes) { }
        protected virtual void OutputOperator(System.CodeDom.CodeBinaryOperatorType op) { }
        protected virtual void OutputParameters(System.CodeDom.CodeParameterDeclarationExpressionCollection parameters) { }
        protected abstract void OutputType(System.CodeDom.CodeTypeReference typeRef);
        protected virtual void OutputTypeAttributes(System.Reflection.TypeAttributes attributes, bool isStruct, bool isEnum) { }
        protected virtual void OutputTypeNamePair(System.CodeDom.CodeTypeReference typeRef, string name) { }
        protected abstract string QuoteSnippetString(string value);
        protected abstract bool Supports(System.CodeDom.Compiler.GeneratorSupport support);
        string System.CodeDom.Compiler.ICodeGenerator.CreateEscapedIdentifier(string value) { throw null; }
        string System.CodeDom.Compiler.ICodeGenerator.CreateValidIdentifier(string value) { throw null; }
        void System.CodeDom.Compiler.ICodeGenerator.GenerateCodeFromCompileUnit(System.CodeDom.CodeCompileUnit e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o) { }
        void System.CodeDom.Compiler.ICodeGenerator.GenerateCodeFromExpression(System.CodeDom.CodeExpression e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o) { }
        void System.CodeDom.Compiler.ICodeGenerator.GenerateCodeFromNamespace(System.CodeDom.CodeNamespace e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o) { }
        void System.CodeDom.Compiler.ICodeGenerator.GenerateCodeFromStatement(System.CodeDom.CodeStatement e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o) { }
        void System.CodeDom.Compiler.ICodeGenerator.GenerateCodeFromType(System.CodeDom.CodeTypeDeclaration e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o) { }
        string System.CodeDom.Compiler.ICodeGenerator.GetTypeOutput(System.CodeDom.CodeTypeReference type) { throw null; }
        bool System.CodeDom.Compiler.ICodeGenerator.IsValidIdentifier(string value) { throw null; }
        bool System.CodeDom.Compiler.ICodeGenerator.Supports(System.CodeDom.Compiler.GeneratorSupport support) { throw null; }
        void System.CodeDom.Compiler.ICodeGenerator.ValidateIdentifier(string value) { }
        protected virtual void ValidateIdentifier(string value) { }
        public static void ValidateIdentifiers(System.CodeDom.CodeObject e) { }
    }
    public partial class CodeGeneratorOptions
    {
        public CodeGeneratorOptions() { }
        public bool BlankLinesBetweenMembers { get { throw null; } set { } }
        public string BracingStyle { get { throw null; } set { } }
        public bool ElseOnClosing { get { throw null; } set { } }
        public string IndentString { get { throw null; } set { } }
        public object this[string index] { get { throw null; } set { } }
        public bool VerbatimOrder { get { throw null; } set { } }
    }
    public abstract partial class CodeParser : System.CodeDom.Compiler.ICodeParser
    {
        protected CodeParser() { }
        public abstract System.CodeDom.CodeCompileUnit Parse(System.IO.TextReader codeStream);
    }
    public partial class CompilerError
    {
        public CompilerError() { }
        public CompilerError(string fileName, int line, int column, string errorNumber, string errorText) { }
        public int Column { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ErrorNumber { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ErrorText { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string FileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IsWarning { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int Line { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override string ToString() { throw null; }
    }
    public partial class CompilerErrorCollection : System.Collections.CollectionBase
    {
        public CompilerErrorCollection() { }
        public CompilerErrorCollection(System.CodeDom.Compiler.CompilerError[] value) { }
        public CompilerErrorCollection(System.CodeDom.Compiler.CompilerErrorCollection value) { }
        public bool HasErrors { get { throw null; } }
        public bool HasWarnings { get { throw null; } }
        public System.CodeDom.Compiler.CompilerError this[int index] { get { throw null; } set { } }
        public int Add(System.CodeDom.Compiler.CompilerError value) { throw null; }
        public void AddRange(System.CodeDom.Compiler.CompilerError[] value) { }
        public void AddRange(System.CodeDom.Compiler.CompilerErrorCollection value) { }
        public bool Contains(System.CodeDom.Compiler.CompilerError value) { throw null; }
        public void CopyTo(System.CodeDom.Compiler.CompilerError[] array, int index) { }
        public int IndexOf(System.CodeDom.Compiler.CompilerError value) { throw null; }
        public void Insert(int index, System.CodeDom.Compiler.CompilerError value) { }
        public void Remove(System.CodeDom.Compiler.CompilerError value) { }
    }
    public sealed partial class CompilerInfo
    {
        internal CompilerInfo() { }
        public System.Type CodeDomProviderType { get { throw null; } }
        public bool IsCodeDomProviderTypeValid { get { throw null; } }
        public System.CodeDom.Compiler.CompilerParameters CreateDefaultCompilerParameters() { throw null; }
        public System.CodeDom.Compiler.CodeDomProvider CreateProvider() { throw null; }
        public System.CodeDom.Compiler.CodeDomProvider CreateProvider(System.Collections.Generic.IDictionary<string, string> providerOptions) { throw null; }
        public override bool Equals(object o) { throw null; }
        public string[] GetExtensions() { throw null; }
        public override int GetHashCode() { throw null; }
        public string[] GetLanguages() { throw null; }
    }
    public partial class CompilerParameters
    {
        public CompilerParameters() { }
        public CompilerParameters(string[] assemblyNames) { }
        public CompilerParameters(string[] assemblyNames, string outputName) { }
        public CompilerParameters(string[] assemblyNames, string outputName, bool includeDebugInformation) { }
        public string CompilerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string CoreAssemblyFileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Specialized.StringCollection EmbeddedResources { get { throw null; } }
        public bool GenerateExecutable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool GenerateInMemory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IncludeDebugInformation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Specialized.StringCollection LinkedResources { get { throw null; } }
        public string MainClass { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string OutputAssembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Specialized.StringCollection ReferencedAssemblies { get { throw null; } }
        public System.CodeDom.Compiler.TempFileCollection TempFiles { get { throw null; } set { } }
        public bool TreatWarningsAsErrors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IntPtr UserToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int WarningLevel { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Win32Resource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CompilerResults
    {
        public CompilerResults(System.CodeDom.Compiler.TempFileCollection tempFiles) { }
        public System.Reflection.Assembly CompiledAssembly { get { throw null; } set { } }
        public System.CodeDom.Compiler.CompilerErrorCollection Errors { get { throw null; } }
        public int NativeCompilerReturnValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Specialized.StringCollection Output { get { throw null; } }
        public string PathToAssembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.CodeDom.Compiler.TempFileCollection TempFiles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public static partial class Executor
    {
        public static void ExecWait(string cmd, System.CodeDom.Compiler.TempFileCollection tempFiles) { }
        public static int ExecWaitWithCapture(System.IntPtr userToken, string cmd, System.CodeDom.Compiler.TempFileCollection tempFiles, ref string outputName, ref string errorName) { throw null; }
        public static int ExecWaitWithCapture(System.IntPtr userToken, string cmd, string currentDir, System.CodeDom.Compiler.TempFileCollection tempFiles, ref string outputName, ref string errorName) { throw null; }
        public static int ExecWaitWithCapture(string cmd, System.CodeDom.Compiler.TempFileCollection tempFiles, ref string outputName, ref string errorName) { throw null; }
        public static int ExecWaitWithCapture(string cmd, string currentDir, System.CodeDom.Compiler.TempFileCollection tempFiles, ref string outputName, ref string errorName) { throw null; }
    }
    [System.FlagsAttribute]
    public enum GeneratorSupport
    {
        ArraysOfArrays = 1,
        AssemblyAttributes = 4096,
        ChainedConstructorArguments = 32768,
        ComplexExpressions = 524288,
        DeclareDelegates = 512,
        DeclareEnums = 256,
        DeclareEvents = 2048,
        DeclareIndexerProperties = 33554432,
        DeclareInterfaces = 1024,
        DeclareValueTypes = 128,
        EntryPointMethod = 2,
        GenericTypeDeclaration = 16777216,
        GenericTypeReference = 8388608,
        GotoStatements = 4,
        MultidimensionalArrays = 8,
        MultipleInterfaceMembers = 131072,
        NestedTypes = 65536,
        ParameterAttributes = 8192,
        PartialTypes = 4194304,
        PublicStaticMembers = 262144,
        ReferenceParameters = 16384,
        Resources = 2097152,
        ReturnTypeAttributes = 64,
        StaticConstructors = 16,
        TryCatchStatements = 32,
        Win32Resources = 1048576,
    }
    public partial interface ICodeCompiler
    {
        System.CodeDom.Compiler.CompilerResults CompileAssemblyFromDom(System.CodeDom.Compiler.CompilerParameters options, System.CodeDom.CodeCompileUnit compilationUnit);
        System.CodeDom.Compiler.CompilerResults CompileAssemblyFromDomBatch(System.CodeDom.Compiler.CompilerParameters options, System.CodeDom.CodeCompileUnit[] compilationUnits);
        System.CodeDom.Compiler.CompilerResults CompileAssemblyFromFile(System.CodeDom.Compiler.CompilerParameters options, string fileName);
        System.CodeDom.Compiler.CompilerResults CompileAssemblyFromFileBatch(System.CodeDom.Compiler.CompilerParameters options, string[] fileNames);
        System.CodeDom.Compiler.CompilerResults CompileAssemblyFromSource(System.CodeDom.Compiler.CompilerParameters options, string source);
        System.CodeDom.Compiler.CompilerResults CompileAssemblyFromSourceBatch(System.CodeDom.Compiler.CompilerParameters options, string[] sources);
    }
    public partial interface ICodeGenerator
    {
        string CreateEscapedIdentifier(string value);
        string CreateValidIdentifier(string value);
        void GenerateCodeFromCompileUnit(System.CodeDom.CodeCompileUnit e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o);
        void GenerateCodeFromExpression(System.CodeDom.CodeExpression e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o);
        void GenerateCodeFromNamespace(System.CodeDom.CodeNamespace e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o);
        void GenerateCodeFromStatement(System.CodeDom.CodeStatement e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o);
        void GenerateCodeFromType(System.CodeDom.CodeTypeDeclaration e, System.IO.TextWriter w, System.CodeDom.Compiler.CodeGeneratorOptions o);
        string GetTypeOutput(System.CodeDom.CodeTypeReference type);
        bool IsValidIdentifier(string value);
        bool Supports(System.CodeDom.Compiler.GeneratorSupport supports);
        void ValidateIdentifier(string value);
    }
    public partial interface ICodeParser
    {
        System.CodeDom.CodeCompileUnit Parse(System.IO.TextReader codeStream);
    }
    [System.FlagsAttribute]
    public enum LanguageOptions
    {
        CaseInsensitive = 1,
        None = 0,
    }
    public partial class TempFileCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.IDisposable
    {
        public TempFileCollection() { }
        public TempFileCollection(string tempDir) { }
        public TempFileCollection(string tempDir, bool keepFiles) { }
        public string BasePath { get { throw null; } }
        public int Count { get { throw null; } }
        public bool KeepFiles { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public string TempDir { get { throw null; } }
        public string AddExtension(string fileExtension) { throw null; }
        public string AddExtension(string fileExtension, bool keepFile) { throw null; }
        public void AddFile(string fileName, bool keepFile) { }
        public void CopyTo(string[] fileNames, int start) { }
        public void Delete() { }
        protected virtual void Dispose(bool disposing) { }
        ~TempFileCollection() { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int start) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        void System.IDisposable.Dispose() { }
    }
}
