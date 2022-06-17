// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;

#if CODEDOM
#else
using CodeObject = System.Runtime.Serialization.CodeObject;
#endif


namespace System.CodeDom.Compiler
{
    public class CodeDomProvider { public string FileExtension = ""; public bool Supports(GeneratorSupport gs) { return true; } }
    public class CodeGenerator { public static bool IsValidLanguageIndependentIdentifier(string id) { return true; } public static bool ValidateIdentifiers(CodeCompileUnit ccu) { return true; } }
    public enum GeneratorSupport {
        ArraysOfArrays = 0x1,
        EntryPointMethod = 0x2,
        GotoStatements = 0x4,
        MultidimensionalArrays = 0x8,
        StaticConstructors = 0x10,
        TryCatchStatements = 0x20,
        ReturnTypeAttributes = 0x40,
        DeclareValueTypes = 0x80,
        DeclareEnums = 0x0100,
        DeclareDelegates = 0x0200,
        DeclareInterfaces = 0x0400,
        DeclareEvents = 0x0800,
        AssemblyAttributes = 0x1000,
        ParameterAttributes = 0x2000,
        ReferenceParameters = 0x4000,
        ChainedConstructorArguments = 0x8000,
        NestedTypes = 0x00010000,
        MultipleInterfaceMembers = 0x00020000,
        PublicStaticMembers = 0x00040000,
        ComplexExpressions = 0x00080000,
        Win32Resources = 0x00100000,
        Resources = 0x00200000,
        PartialTypes = 0x00400000,
        GenericTypeReference = 0x00800000,
        GenericTypeDeclaration = 0x01000000,
        DeclareIndexerProperties = 0x02000000,
    }
}

namespace System.CodeDom
{
    public class GenCollection<T> : CollectionBase, IList<T>
    {
        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(T item) => throw new NotImplementedException();
        public bool Contains(T item) => throw new NotImplementedException();
        public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();
        public int IndexOf(T item) => throw new NotImplementedException();
        public void Insert(int index, T item) => throw new NotImplementedException();
        public bool Remove(T item) => throw new NotImplementedException();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
    }

    // CodeTypeReference - already included internally with different namespace... but it's internal. And we expose it publicly. So create another version here. Or just define CODEDOM and fix our DataContract code to be aware-ish of both.
    // CodeObject also comes along already... but since it's internal, we can't use it with our initial sweep of "public" Codedom stuff we're fleshing out here. Since it's only referenced in this file, it is not ambiguous with the internal version available elsewhere.
#if CODEDOM
    //public class CodeObject { public IDictionary UserData = new System.Collections.Specialized.ListDictionary(); }
#endif
    // Going the #if CODEDOM route however means we need to add a little extra to satisfy the code files we have in our project borrowed from the CodeDom project:
    public class CodeTypeParameter : CodeObject { public string Name = ""; }


    // ==========================================================================================================
    // Stuff we need to bring back one way or another below here

    public class CodeCompileUnit
    {
        public List<object?> AssemblyCustomAttributes = new List<object?>();
        public CodeNamespaceCollection Namespaces = new CodeNamespaceCollection();
        public List<string> ReferencedAssemblies = new List<string>();
    }

    public class CodeTypeMember : CodeObject {
        public string Name = "";
        public MemberAttributes Attributes;
        public List<object?> CustomAttributes = new List<object?>();
    }
    public class CodeTypeDeclaration : CodeTypeMember
    {
        public CodeTypeDeclaration(object one) { }

        public List<object?> BaseTypes = new List<object?>();
        public bool IsClass;
        public bool IsEnum;
        public bool IsPartial;
        public bool IsStruct;
        public GenCollection<CodeTypeMember?> Members = new GenCollection<CodeTypeMember?>();
        public TypeAttributes TypeAttributes;
    }

    public enum MemberAttributes
    {
        Abstract = 0x0001,
        Final = 0x0002,
        Static = 0x0003,
        Override = 0x0004,
        Const = 0x0005,
        New = 0x0010,
        Overloaded = 0x0100,
        Assembly = 0x1000,
        FamilyAndAssembly = 0x2000,
        Family = 0x3000,
        FamilyOrAssembly = 0x4000,
        Private = 0x5000,
        Public = 0x6000,
        AccessMask = 0xF000,
        ScopeMask = 0x000F,
        VTableMask = 0x00F0
    }

    public class CodeArgumentReferenceExpression : CodeExpression { public CodeArgumentReferenceExpression(object one) { } public string ParameterName = ""; }
    public class CodeAssignStatement { public CodeAssignStatement() { } public CodeAssignStatement(object one, object two) { } public object? Left; public object? Right; }
    public class CodeAttributeArgument { public CodeAttributeArgument(object one) { } public CodeAttributeArgument(object one, object two) { } }
    public class CodeAttributeArgumentCollection : List<CodeAttributeArgument> { }
    public class CodeAttributeDeclaration {
        public CodeAttributeDeclaration(object one) { }
        public CodeAttributeDeclaration(object one, object two) { }
        public CodeAttributeDeclaration(object one, object two, object three) { }
        public CodeAttributeArgumentCollection Arguments = new CodeAttributeArgumentCollection();
    }
    public enum CodeBinaryOperatorType { IdentityEquality, IdentityInequality }
    public class CodeBinaryOperatorExpression : CodeExpression { public CodeBinaryOperatorExpression(object one, object two, object three) { } }
    public class CodeConditionStatement { public CodeConditionStatement() { } public CodeConditionStatement(object one) { } public object? Condition; public List<object?> TrueStatements = new List<object?>(); }
    public class CodeConstructor : CodeMemberMethod {
        public List<object?> BaseConstructorArgs = new List<object?>();
    }
    public class CodeDelegateInvokeExpression : CodeExpression { public CodeDelegateInvokeExpression(object one, object two, object three) { } }
    public class CodeEventReferenceExpression : CodeExpression { public CodeEventReferenceExpression(object one, object two) { } }
    public class CodeExpression { }
    public class CodeExpressionStatement { public object? Expression; }
    public class CodeFieldReferenceExpression : CodeExpression { public CodeFieldReferenceExpression(object? one, object two) { } }
    public class CodeIterationStatement {
        public object? IncrementStatement;
        public object? InitStatement;
        public List<object?> Statements = new List<object?>();
        public object? TestExpression;
    }
    public class CodeMemberEvent : CodeTypeMember {
        public List<object?> ImplementationTypes = new List<object?>();
        public object? Type;
    }
    public class CodeMemberField : CodeTypeMember {
        public CodeMemberField() { }
        public CodeMemberField(object one, object two) { }
        public object? InitExpression;
        public object? Type;
    }
    public class CodeMemberMethod : CodeTypeMember {
        public List<object?> ImplementationTypes = new List<object?>();
        public List<object?> Parameters = new List<object?>();
        public CodeTypeReference ReturnType = new CodeTypeReference(typeof(void).FullName);
        public List<object?> Statements = new List<object?>();
    }
    public class CodeMemberProperty : CodeTypeMember {
        public List<object?> GetStatements = new List<object?>();
        public List<object?> ImplementationTypes = new List<object?>();
        public List<object?> SetStatements = new List<object?>();
        public object? Type;
    }
    public class CodeMethodInvokeExpression : CodeExpression {
        public CodeMethodInvokeExpression(object one, object two) { }
        public CodeMethodInvokeExpression(object one, object two, object three) { }
        public CodeMethodInvokeExpression(object one, object two, object three, object four) { }
    }
    public class CodeMethodReturnStatement { public CodeMethodReturnStatement() { } public CodeMethodReturnStatement(object one) { } public object? Expression; }
    public class CodeNamespace
    {
        public CodeNamespace(object one) { }
        public CodeNamespaceImportCollection Imports = new CodeNamespaceImportCollection();
        public string Name = "";
        public GenCollection<CodeTypeDeclaration?> Types = new GenCollection<CodeTypeDeclaration?>();
    }
    public class CodeNamespaceCollection : List<CodeNamespace> { }
    public class CodeNamespaceImport { public CodeNamespaceImport(object one) { } public string Namespace = ""; }
    public class CodeNamespaceImportCollection : List<CodeNamespaceImport> { }
    public class CodeObjectCreateExpression : CodeExpression { public CodeObjectCreateExpression(object one, object two) { } public CodeObjectCreateExpression(object one, object two, object three) { } }
    public class CodeParameterDeclarationExpression : CodeExpression { public CodeParameterDeclarationExpression(object one, object two) { } public string Name = ""; }
    public class CodePrimitiveExpression : CodeExpression { public CodePrimitiveExpression(object? one) { } }
    public class CodePropertyReferenceExpression : CodeExpression { public CodePropertyReferenceExpression(object one, object two) { } }
    public class CodePropertySetValueReferenceExpression : CodeExpression { }
    public class CodeSnippetExpression : CodeExpression { public CodeSnippetExpression(object one) { } }
    public class CodeSnippetStatement { public CodeSnippetStatement(object one) { } }
    public class CodeThisReferenceExpression : CodeExpression { }
    public class CodeTypeOfExpression : CodeExpression { public CodeTypeOfExpression(object one) { } }
    public class CodeTypeReferenceExpression : CodeExpression { public CodeTypeReferenceExpression(object one) { } }
    public class CodeVariableDeclarationStatement {
        public CodeVariableDeclarationStatement() { }
        public CodeVariableDeclarationStatement(object one, object two, object three) { }
        public object? InitExpression;
        public string Name = "";
        public object? Type;
    }
    public class CodeVariableReferenceExpression : CodeExpression { public CodeVariableReferenceExpression(object one) { } public string VariableName = ""; }
}
