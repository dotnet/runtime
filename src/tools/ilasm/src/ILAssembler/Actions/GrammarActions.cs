// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    /// <summary>
    /// Resets the semantic state that must not flow from one document to the next.
    /// </summary>
    /// <remarks>
    /// The same <see cref="GrammarActions"/> instance compiles every document of a compilation so
    /// that they share an entity registry. Every rule that introduces namespace, type, method or
    /// scope state releases it from its own <c>finally</c> block, so this is only a safety net for
    /// release builds.
    /// </remarks>
    internal void BeginDocument()
    {
        Debug.Assert(
            _currentMethod is null
                && _typeOwners.Count == 0
                && _namespaceOwners.Count == 0
                && _scopeStack.Count == 0
                && _parameterDirectiveFrames.Count == 0
                && _exceptionBlockFrames.Count == 0
                && _exceptionClauseListFrames.Count == 0
                && _catchClauseFrames.Count == 0
                && _semanticRootFrames.Count == 0
                && _dottedNameFrames.Count == 0
                && _slashedNameFrames.Count == 0
                && _typeSignatureFrames.Count == 0
                && _typeArgumentsFrames.Count == 0
                && _boundsFrames.Count == 0
                && _signatureArgumentsFrames.Count == 0
                && _parameterAttributesFrames.Count == 0
                && _marshalBlobFrames.Count == 0
                && _nativeTypeFrames.Count == 0
                && _variantTypeFrames.Count == 0
                && _methodHeaderFrames.Count == 0
                && _pInvokeFrames.Count == 0
                && _genericTypeListFrames.Count == 0
                && _genericParameterAttributesFrames.Count == 0
                && _genericParametersFrames.Count == 0
                && _fieldDeclarationFrames.Count == 0
                && _propertyHeaderFrames.Count == 0
                && _eventHeaderFrames.Count == 0
                && _propertyBodyFrames.Count == 0
                && _eventBodyFrames.Count == 0
                && _classGenericDirectiveFrames.Count == 0
                && _namespaceHeaderFrames.Count == 0
                && _classHeaderFrames.Count == 0
                && _interfaceListFrames.Count == 0
                && _customBlobArgumentFrames.Count == 0
                && _customBlobNamedArgumentFrames.Count == 0
                && _serializationSequenceFrames.Count == 0
                && _dataDeclarationFrames.Count == 0
                && _assemblyDeclarationFrames.Count == 0
                && _assemblyReferenceDeclarationFrames.Count == 0
                && _fileDeclarationFrames.Count == 0
                && _exportedTypeDeclarationFrames.Count == 0
                && _manifestResourceDeclarationFrames.Count == 0
                && _securityAttributeSetFrames.Count == 0
                && _securityNameValuePairFrames.Count == 0
                && _pendingClassMethodOverrides.Count == 0,
            "Per-document semantic state must be released by the owning rule's finally block.");
        EndMethod();
        ResetTypeScopes();
        ClearPendingCustomAttributeOwners();
        _semanticRootFrames.Clear();
        _dottedNameFrames.Clear();
        _slashedNameFrames.Clear();
        _typeSignatureFrames.Clear();
        _typeArgumentsFrames.Clear();
        _boundsFrames.Clear();
        _signatureArgumentsFrames.Clear();
        _parameterAttributesFrames.Clear();
        _marshalBlobFrames.Clear();
        _nativeTypeFrames.Clear();
        _variantTypeFrames.Clear();
        _methodHeaderFrames.Clear();
        _pInvokeFrames.Clear();
        _genericTypeListFrames.Clear();
        _genericParameterAttributesFrames.Clear();
        _genericParametersFrames.Clear();
        _fieldDeclarationFrames.Clear();
        _propertyHeaderFrames.Clear();
        _eventHeaderFrames.Clear();
        _propertyBodyFrames.Clear();
        _eventBodyFrames.Clear();
        _classGenericDirectiveFrames.Clear();
        _namespaceHeaderFrames.Clear();
        _classHeaderFrames.Clear();
        _interfaceListFrames.Clear();
        _customBlobArgumentFrames.Clear();
        _customBlobNamedArgumentFrames.Clear();
        _serializationSequenceFrames.Clear();
        _dataDeclarationFrames.Clear();
        _assemblyDeclarationFrames.Clear();
        _assemblyReferenceDeclarationFrames.Clear();
        _fileDeclarationFrames.Clear();
        _exportedTypeDeclarationFrames.Clear();
        _manifestResourceDeclarationFrames.Clear();
        _securityAttributeSetFrames.Clear();
        _securityNameValuePairFrames.Clear();
        _pendingClassMethodOverrides.Clear();
        _currentDocumentPath = null;
        _syntaxErrorCount = 0;
    }
}
