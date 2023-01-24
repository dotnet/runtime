// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Versioning;

namespace System.Diagnostics.SymbolStore
{
    public interface ISymbolWriter
    {
        // Set the IMetadataEmitter that this symbol writer is associated
        // with. This must be done only once before any other ISymbolWriter
        // methods are called.
        void Initialize(IntPtr emitter, string filename, bool fFullBuild);

        // Define a source document. Guid's will be provided for the
        // languages, vendors, and document types that we currently know
        // about.
        ISymbolDocumentWriter DefineDocument(string url,
                                             Guid language,
                                             Guid languageVendor,
                                             Guid documentType);

        // Define the method that the user has defined as their entrypoint
        // for this module. This would be, perhaps, the user's main method
        // rather than compiler generated stubs before main.
        void SetUserEntryPoint(SymbolToken entryMethod);

        // Open a method to emit symbol information into. The given method
        // becomes the current method for calls do define sequence points,
        // parameters and lexical scopes. There is an implicit lexical
        // scope around the entire method. Re-opening a method that has
        // been previously closed effectivley erases any previously
        // defined symbols for that method.
        //
        // There can be only one open method at a time.
        void OpenMethod(SymbolToken method);

        // Close the current method. Once a method is closed, no more
        // symbols can be defined within it.
        void CloseMethod();

        // Define a group of sequence points within the current method.
        // Each line/column defines the start of a statement within a
        // method. The arrays should be sorted by offset. The offset is
        // always the offset from the start of the method, in bytes.
        void DefineSequencePoints(ISymbolDocumentWriter document,
                                  int[] offsets,
                                  int[] lines,
                                  int[] columns,
                                  int[] endLines,
                                  int[] endColumns);

        // Open a new lexical scope in the current method. The scope
        // becomes the new current scope and is effectivley pushed onto a
        // stack of scopes. startOffset is the offset, in bytes from the
        // beginning of the method, of the first instruction in the
        // lexical scope. Scopes must form a hierarchy. Siblings are not
        // allowed to overlap.
        //
        // OpenScope returns an opaque scope id that can be used with
        // SetScopeRange to define a scope's start/end offset at a later
        // time. In this case, the offsets passed to OpenScope and
        // CloseScope are ignored.
        //
        // Note: scope id's are only valid in the current method.
        //
        // <TODO>@todo: should we require that startOffset and endOffset for
        // scopes also be defined as sequence points?</TODO>
        int OpenScope(int startOffset);

        // Close the current lexical scope. Once a scope is closed no more
        // variables can be defined within it. endOffset points past the
        // last instruction in the scope.
        void CloseScope(int endOffset);

        // Define the offset range for a given lexical scope.
        void SetScopeRange(int scopeID, int startOffset, int endOffset);

        // Define a single variable in the current lexical
        // scope. startOffset and endOffset are optional. If 0, then they
        // are ignored and the variable is defined over the entire
        // scope. If non-zero, then they must fall within the offsets of
        // the current scope. This can be called multiple times for a
        // variable of the same name that has multiple homes throughout a
        // scope. (Note: start/end offsets must not overlap in such a
        // case.)
        void DefineLocalVariable(string name,
                                 FieldAttributes attributes,
                                 byte[] signature,
                                 SymAddressKind addrKind,
                                 int addr1,
                                 int addr2,
                                 int addr3,
                                 int startOffset,
                                 int endOffset);

        // Define a single parameter in the current method. The type of
        // each parameter is taken from its position (sequence) within the
        // method's signature.
        //
        // Note: if parameters are defined in the metadata for a given
        // method, then clearly one would not have to define them again
        // with calls to this method. The symbol readers will have to be
        // smart enough to check the normal metadata for these first then
        // fall back to the symbol store.
        void DefineParameter(string name,
                             ParameterAttributes attributes,
                             int sequence,
                             SymAddressKind addrKind,
                             int addr1,
                             int addr2,
                             int addr3);

        // Define a single variable not within a method. This is used for
        // certain fields in classes, bitfields, etc.
        void DefineField(SymbolToken parent,
                         string name,
                         FieldAttributes attributes,
                         byte[] signature,
                         SymAddressKind addrKind,
                         int addr1,
                         int addr2,
                         int addr3);

        // Define a single global variable.
        void DefineGlobalVariable(string name,
                                  FieldAttributes attributes,
                                  byte[] signature,
                                  SymAddressKind addrKind,
                                  int addr1,
                                  int addr2,
                                  int addr3);

        // Close will close the ISymbolWriter and commit the symbols
        // to the symbol store. The ISymbolWriter becomes invalid
        // after this call for further updates.
        void Close();

        // Defines a custom attribute based upon its name. Not to be
        // confused with Metadata custom attributes, these attributes are
        // held in the symbol store.
        void SetSymAttribute(SymbolToken parent, string name, byte[] data);

        // Opens a new namespace. Call this before defining methods or
        // variables that live within a namespace. Namespaces can be nested.
        void OpenNamespace(string name);

        // Close the most recently opened namespace.
        void CloseNamespace();

        // Specifies that the given, fully qualified namespace name is
        // being used within the currently open lexical scope. Closing the
        // current scope will also stop using the namespace, and the
        // namespace will be in use in all scopes that inherit from the
        // currently open scope.
        void UsingNamespace(string fullName);

        // Specifies the true start and end of a method within a source
        // file. Use this to specify the extent of a method independently
        // of what sequence points exist within the method.
        void SetMethodSourceRange(ISymbolDocumentWriter startDoc,
                                  int startLine,
                                  int startColumn,
                                  ISymbolDocumentWriter endDoc,
                                  int endLine,
                                  int endColumn);

        // Used to set the underlying ISymUnmanagedWriter that a
        // managed ISymbolWriter may use to emit symbols with.
        void SetUnderlyingWriter(IntPtr underlyingWriter);
    }
}
