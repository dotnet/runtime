// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    /// <summary>
    /// Resets the semantic state that must not flow from one document to the next, and defines the input file
    /// as a PDB document and makes it the current document.
    /// </summary>
    /// <param name="path">The input file's name as the PDB records it (<see cref="SourceText.Path"/>).</param>
    /// <remarks>
    /// The same <see cref="GrammarActions"/> instance compiles every document of a compilation so
    /// that they share an entity registry. Every rule that introduces namespace, type, method or
    /// scope state releases it from its own <c>finally</c> block, so this is only a safety net for
    /// release builds. The <c>.language</c> state carries over from the previous input file, as in native ilasm.
    /// </remarks>
    internal void BeginDocument(string path)
    {
        Debug.Assert(
            _currentMethod is null
                && _typeOwners.Count == 0
                && _namespaceOwners.Count == 0
                && _suppressedDeclarationOwners.Count == 0
                && _scopeStack.Count == 0
                && _pendingClassMethodOverrides.Count == 0,
            "Nested compiler state must be released by its owning declaration.");
        EndMethod();
        ResetTypeScopes();
        ClearPendingCustomAttributeOwners();
        _pendingClassMethodOverrides.Clear();
        _currentDocument = _pdbDocuments.GetOrAdd(path, _currentLanguageGuid);
        _syntaxErrorCount = 0;
    }
}
