// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Diagnostics
{
    public partial class StackFrame
    {
        public const int OFFSET_UNKNOWN = -1;
        public StackFrame() { }
        public StackFrame(bool needFileInfo) { }
        public StackFrame(int skipFrames) { }
        public StackFrame(int skipFrames, bool needFileInfo) { }
        public StackFrame(string? fileName, int lineNumber) { }
        public StackFrame(string? fileName, int lineNumber, int colNumber) { }
        public virtual int GetFileColumnNumber() { throw null; }
        public virtual int GetFileLineNumber() { throw null; }
        public virtual string? GetFileName() { throw null; }
        public virtual int GetILOffset() { throw null; }
        public virtual System.Reflection.MethodBase? GetMethod() { throw null; }
        public virtual int GetNativeOffset() { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class StackFrameExtensions
    {
        public static System.IntPtr GetNativeImageBase(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static System.IntPtr GetNativeIP(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static bool HasILOffset(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static bool HasMethod(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static bool HasNativeImage(this System.Diagnostics.StackFrame stackFrame) { throw null; }
        public static bool HasSource(this System.Diagnostics.StackFrame stackFrame) { throw null; }
    }
    public partial class StackTrace
    {
        public const int METHODS_TO_SKIP = 0;
        public StackTrace() { }
        public StackTrace(bool fNeedFileInfo) { }
        public StackTrace(System.Diagnostics.StackFrame frame) { }
        public StackTrace(System.Exception e) { }
        public StackTrace(System.Exception e, bool fNeedFileInfo) { }
        public StackTrace(System.Exception e, int skipFrames) { }
        public StackTrace(System.Exception e, int skipFrames, bool fNeedFileInfo) { }
        public StackTrace(int skipFrames) { }
        public StackTrace(int skipFrames, bool fNeedFileInfo) { }
        public virtual int FrameCount { get { throw null; } }
        public virtual System.Diagnostics.StackFrame? GetFrame(int index) { throw null; }
        public virtual System.Diagnostics.StackFrame[] GetFrames() { throw null; }
        public override string ToString() { throw null; }
    }
}
namespace System.Diagnostics.SymbolStore
{
    public partial interface ISymbolBinder
    {
        [System.ObsoleteAttribute("The recommended alternative is ISymbolBinder1.GetReader. ISymbolBinder1.GetReader takes the importer interface pointer as an IntPtr instead of an Int32, and thus works on both 32-bit and 64-bit architectures. https://go.microsoft.com/fwlink/?linkid=14202=14202")]
        System.Diagnostics.SymbolStore.ISymbolReader? GetReader(int importer, string filename, string searchPath);
    }
    public partial interface ISymbolBinder1
    {
        System.Diagnostics.SymbolStore.ISymbolReader? GetReader(System.IntPtr importer, string filename, string searchPath);
    }
    public partial interface ISymbolDocument
    {
        System.Guid CheckSumAlgorithmId { get; }
        System.Guid DocumentType { get; }
        bool HasEmbeddedSource { get; }
        System.Guid Language { get; }
        System.Guid LanguageVendor { get; }
        int SourceLength { get; }
        string URL { get; }
        int FindClosestLine(int line);
        byte[] GetCheckSum();
        byte[] GetSourceRange(int startLine, int startColumn, int endLine, int endColumn);
    }
    public partial interface ISymbolDocumentWriter
    {
        void SetCheckSum(System.Guid algorithmId, byte[] checkSum);
        void SetSource(byte[] source);
    }
    public partial interface ISymbolMethod
    {
        System.Diagnostics.SymbolStore.ISymbolScope RootScope { get; }
        int SequencePointCount { get; }
        System.Diagnostics.SymbolStore.SymbolToken Token { get; }
        System.Diagnostics.SymbolStore.ISymbolNamespace GetNamespace();
        int GetOffset(System.Diagnostics.SymbolStore.ISymbolDocument document, int line, int column);
        System.Diagnostics.SymbolStore.ISymbolVariable[] GetParameters();
        int[] GetRanges(System.Diagnostics.SymbolStore.ISymbolDocument document, int line, int column);
        System.Diagnostics.SymbolStore.ISymbolScope GetScope(int offset);
        void GetSequencePoints(int[]? offsets, System.Diagnostics.SymbolStore.ISymbolDocument[]? documents, int[]? lines, int[]? columns, int[]? endLines, int[]? endColumns);
        bool GetSourceStartEnd(System.Diagnostics.SymbolStore.ISymbolDocument[]? docs, int[]? lines, int[]? columns);
    }
    public partial interface ISymbolNamespace
    {
        string Name { get; }
        System.Diagnostics.SymbolStore.ISymbolNamespace[] GetNamespaces();
        System.Diagnostics.SymbolStore.ISymbolVariable[] GetVariables();
    }
    public partial interface ISymbolReader
    {
        System.Diagnostics.SymbolStore.SymbolToken UserEntryPoint { get; }
        System.Diagnostics.SymbolStore.ISymbolDocument? GetDocument(string url, System.Guid language, System.Guid languageVendor, System.Guid documentType);
        System.Diagnostics.SymbolStore.ISymbolDocument[] GetDocuments();
        System.Diagnostics.SymbolStore.ISymbolVariable[] GetGlobalVariables();
        System.Diagnostics.SymbolStore.ISymbolMethod? GetMethod(System.Diagnostics.SymbolStore.SymbolToken method);
        System.Diagnostics.SymbolStore.ISymbolMethod? GetMethod(System.Diagnostics.SymbolStore.SymbolToken method, int version);
        System.Diagnostics.SymbolStore.ISymbolMethod GetMethodFromDocumentPosition(System.Diagnostics.SymbolStore.ISymbolDocument document, int line, int column);
        System.Diagnostics.SymbolStore.ISymbolNamespace[] GetNamespaces();
        byte[] GetSymAttribute(System.Diagnostics.SymbolStore.SymbolToken parent, string name);
        System.Diagnostics.SymbolStore.ISymbolVariable[] GetVariables(System.Diagnostics.SymbolStore.SymbolToken parent);
    }
    public partial interface ISymbolScope
    {
        int EndOffset { get; }
        System.Diagnostics.SymbolStore.ISymbolMethod Method { get; }
        System.Diagnostics.SymbolStore.ISymbolScope Parent { get; }
        int StartOffset { get; }
        System.Diagnostics.SymbolStore.ISymbolScope[] GetChildren();
        System.Diagnostics.SymbolStore.ISymbolVariable[] GetLocals();
        System.Diagnostics.SymbolStore.ISymbolNamespace[] GetNamespaces();
    }
    public partial interface ISymbolVariable
    {
        int AddressField1 { get; }
        int AddressField2 { get; }
        int AddressField3 { get; }
        System.Diagnostics.SymbolStore.SymAddressKind AddressKind { get; }
        object Attributes { get; }
        int EndOffset { get; }
        string Name { get; }
        int StartOffset { get; }
        byte[] GetSignature();
    }
    public partial interface ISymbolWriter
    {
        void Close();
        void CloseMethod();
        void CloseNamespace();
        void CloseScope(int endOffset);
        System.Diagnostics.SymbolStore.ISymbolDocumentWriter DefineDocument(string url, System.Guid language, System.Guid languageVendor, System.Guid documentType);
        void DefineField(System.Diagnostics.SymbolStore.SymbolToken parent, string name, System.Reflection.FieldAttributes attributes, byte[] signature, System.Diagnostics.SymbolStore.SymAddressKind addrKind, int addr1, int addr2, int addr3);
        void DefineGlobalVariable(string name, System.Reflection.FieldAttributes attributes, byte[] signature, System.Diagnostics.SymbolStore.SymAddressKind addrKind, int addr1, int addr2, int addr3);
        void DefineLocalVariable(string name, System.Reflection.FieldAttributes attributes, byte[] signature, System.Diagnostics.SymbolStore.SymAddressKind addrKind, int addr1, int addr2, int addr3, int startOffset, int endOffset);
        void DefineParameter(string name, System.Reflection.ParameterAttributes attributes, int sequence, System.Diagnostics.SymbolStore.SymAddressKind addrKind, int addr1, int addr2, int addr3);
        void DefineSequencePoints(System.Diagnostics.SymbolStore.ISymbolDocumentWriter document, int[] offsets, int[] lines, int[] columns, int[] endLines, int[] endColumns);
        void Initialize(System.IntPtr emitter, string filename, bool fFullBuild);
        void OpenMethod(System.Diagnostics.SymbolStore.SymbolToken method);
        void OpenNamespace(string name);
        int OpenScope(int startOffset);
        void SetMethodSourceRange(System.Diagnostics.SymbolStore.ISymbolDocumentWriter startDoc, int startLine, int startColumn, System.Diagnostics.SymbolStore.ISymbolDocumentWriter endDoc, int endLine, int endColumn);
        void SetScopeRange(int scopeID, int startOffset, int endOffset);
        void SetSymAttribute(System.Diagnostics.SymbolStore.SymbolToken parent, string name, byte[] data);
        void SetUnderlyingWriter(System.IntPtr underlyingWriter);
        void SetUserEntryPoint(System.Diagnostics.SymbolStore.SymbolToken entryMethod);
        void UsingNamespace(string fullName);
    }
    public enum SymAddressKind
    {
        ILOffset = 1,
        NativeRVA = 2,
        NativeRegister = 3,
        NativeRegisterRelative = 4,
        NativeOffset = 5,
        NativeRegisterRegister = 6,
        NativeRegisterStack = 7,
        NativeStackRegister = 8,
        BitField = 9,
        NativeSectionOffset = 10,
    }
    public readonly partial struct SymbolToken
    {
        private readonly int _dummyPrimitive;
        public SymbolToken(int val) { throw null; }
        public bool Equals(System.Diagnostics.SymbolStore.SymbolToken obj) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetToken() { throw null; }
        public static bool operator ==(System.Diagnostics.SymbolStore.SymbolToken a, System.Diagnostics.SymbolStore.SymbolToken b) { throw null; }
        public static bool operator !=(System.Diagnostics.SymbolStore.SymbolToken a, System.Diagnostics.SymbolStore.SymbolToken b) { throw null; }
    }
    public partial class SymDocumentType
    {
        public static readonly System.Guid Text;
        public SymDocumentType() { }
    }
    public partial class SymLanguageType
    {
        public static readonly System.Guid Basic;
        public static readonly System.Guid C;
        public static readonly System.Guid Cobol;
        public static readonly System.Guid CPlusPlus;
        public static readonly System.Guid CSharp;
        public static readonly System.Guid ILAssembly;
        public static readonly System.Guid Java;
        public static readonly System.Guid JScript;
        public static readonly System.Guid MCPlusPlus;
        public static readonly System.Guid Pascal;
        public static readonly System.Guid SMC;
        public SymLanguageType() { }
    }
    public partial class SymLanguageVendor
    {
        public static readonly System.Guid Microsoft;
        public SymLanguageVendor() { }
    }
}
