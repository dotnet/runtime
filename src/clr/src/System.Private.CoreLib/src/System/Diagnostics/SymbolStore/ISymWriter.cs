// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Represents a symbol writer for managed code. Provides methods to
** define documents, sequence points, lexical scopes, and variables.
**
** 
===========================================================*/

using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Diagnostics.SymbolStore
{
    // Interface does not need to be marked with the serializable attribute
    internal interface ISymbolWriter
    {
        // Define a source document. Guid's will be provided for the
        // languages, vendors, and document types that we currently know
        // about.
        ISymbolDocumentWriter? DefineDocument(string url,
                                          Guid language,
                                          Guid languageVendor,
                                          Guid documentType);

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

        int OpenScope(int startOffset);

        // Close the current lexical scope. Once a scope is closed no more
        // variables can be defined within it. endOffset points past the
        // last instruction in the scope.
        void CloseScope(int endOffset);

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

        // Defines a custom attribute based upon its name. Not to be
        // confused with Metadata custom attributes, these attributes are
        // held in the symbol store.
        void SetSymAttribute(SymbolToken parent, string name, byte[] data);

        // Specifies that the given, fully qualified namespace name is
        // being used within the currently open lexical scope. Closing the
        // current scope will also stop using the namespace, and the
        // namespace will be in use in all scopes that inherit from the
        // currently open scope.
        void UsingNamespace(string fullName);
    }
}
