// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// StaticAllocationHelpers.inl -
//

//
// Helpers used to determine static offset allocation. Placed into an inl file so as to be shareable between
// mdilbind and the vm codebases.
//
//
#ifndef StaticAllocationHelpers_INL
#define StaticAllocationHelpers_INL

// Will return underlying type if it's an enum
//             ELEMENT_TYPE_VALUETYPE if it is a non enum
//             ELEMENT_TYPE_END if it doesn't know (we may not want to load other assemblies)
#ifdef CLR_STANDALONE_BINDER
static CorElementType ParseMetadataForStaticsIsValueTypeEnum(MdilModule * pModule, IMetaDataImport2 *pImport, mdToken tk)
#else
static CorElementType ParseMetadataForStaticsIsValueTypeEnum(Module * pModule, IMDInternalImport *pImport, mdToken tk)
#endif
{
    STANDARD_VM_CONTRACT;

    if (TypeFromToken(tk) != mdtTypeDef)
    {
        // At this point, we would have to load other assemblies. The only one we have guaranteed
        // to be there is mscorlib.
        return ELEMENT_TYPE_END;
    }
        
    // The only condition we will be checking is that the parent of the type is System.Enum
    // Rest of the checks will be handed by class loader, which will fail to load if it's malformed
    // hence, no need to do all the checks here.
    mdToken tkParent = 0;
    DWORD dwParentAttr = 0;
    
#ifdef CLR_STANDALONE_BINDER
    if (FAILED(pImport->GetTypeDefProps(tk, NULL, 0, NULL, &dwParentAttr, &tkParent)))
#else
    if (FAILED(pImport->GetTypeDefProps(tk, &dwParentAttr, &tkParent)))
#endif
    {
        return ELEMENT_TYPE_END;
    }
    
    if (RidFromToken(tkParent) == 0)
    {
        return ELEMENT_TYPE_END;
    }
    
#ifdef CLR_STANDALONE_BINDER
    WCHAR wszTypeName[MAX_CLASS_NAME];
    ULONG cchTypeName;
#else
    LPCSTR szName      = NULL;
    LPCSTR szNamespace = NULL;
#endif
    
    switch (TypeFromToken(tkParent))
    {
        case mdtTypeDef:
#ifdef CLR_STANDALONE_BINDER
            if (FAILED(pImport->GetTypeDefProps(tkParent, wszTypeName, _countof(wszTypeName), &cchTypeName, NULL, NULL)))
#else
            if (FAILED(pImport->GetNameOfTypeDef(tkParent, &szName, &szNamespace)))
#endif
            {
                return ELEMENT_TYPE_END;
            }
            break;
        case mdtTypeRef:
#ifdef CLR_STANDALONE_BINDER
            if (FAILED(pImport->GetTypeRefProps(tkParent, NULL, wszTypeName, _countof(wszTypeName), &cchTypeName)))
#else
            if (FAILED(pImport->GetNameOfTypeRef(tkParent, &szNamespace, &szName)))
#endif
            {
                return ELEMENT_TYPE_END;
            }
            break;
        default:
            return ELEMENT_TYPE_END;
    }
    
#ifndef CLR_STANDALONE_BINDER
    if (szName == NULL || szNamespace == NULL)
    {
        return ELEMENT_TYPE_END;
    }
#endif
    
    // If it doesn't inherit from System.Enum, then it must be a value type
    // Note that loader will not load malformed types so this check is enough
#ifdef CLR_STANDALONE_BINDER
    if (wcscmp(wszTypeName, L"System.Enum") != 0)
#else
    if (strcmp(szName,"Enum") != 0 || strcmp(szNamespace,"System") != 0)
#endif
    {
        return ELEMENT_TYPE_VALUETYPE;
    }
    
    // OK, it's an enum; find its instance field and get its type
#ifdef CLR_STANDALONE_BINDER
    HCORENUM hEnumFields = NULL;
    CloseHCORENUMOnDestruct hEnumFieldsDestruct(pImport, &hEnumFields);
    ULONG cFields;
    HRESULT hr;
#else
    HENUMInternalHolder   hEnum(pImport);
#endif
    mdToken tkField;
#ifdef CLR_STANDALONE_BINDER
    while (S_OK == (hr = pImport->EnumFields(&hEnumFields, tk, &tkField, 1, &cFields)))
#else
    hEnum.EnumInit(mdtFieldDef,tk);
    while (pImport->EnumNext(&hEnum,&tkField))
#endif
    {
#ifdef CLR_STANDALONE_BINDER
        _ASSERTE(cFields == 1);
#endif
        PCCOR_SIGNATURE pMemberSignature;
        DWORD           cMemberSignature;
        
        // Get the type of the static field.
        DWORD dwMemberAttribs;

#ifdef CLR_STANDALONE_BINDER
        IfFailThrow(pImport->GetFieldProps(tkField, NULL, NULL, 0, NULL, &dwMemberAttribs, &pMemberSignature, &cMemberSignature, NULL, NULL, NULL));
#else
        IfFailThrow(pImport->GetFieldDefProps(tkField, &dwMemberAttribs));
#endif
        
        if (!IsFdStatic(dwMemberAttribs))
        {
#ifndef CLR_STANDALONE_BINDER
            IfFailThrow(pImport->GetSigOfFieldDef(tkField, &cMemberSignature, &pMemberSignature));
            
            IfFailThrow(validateTokenSig(tkField,pMemberSignature,cMemberSignature,dwMemberAttribs,pImport));
#endif
            
            SigTypeContext typeContext;
            MetaSig fsig(pMemberSignature, cMemberSignature, pModule, &typeContext, MetaSig::sigField);
            CorElementType ElementType = fsig.NextArg();
            return ElementType;
        }
    }
    
#ifdef CLR_STANDALONE_BINDER
    IfFailThrow(hr);
#endif

    // no instance field found -- error!
    return ELEMENT_TYPE_END;
}

#ifdef CLR_STANDALONE_BINDER
#define g_ThreadStaticAttributeClassName L"System.ThreadStaticAttribute"
static BOOL GetStaticFieldElementTypeForFieldDef(MdilModule * pModule, IMetaDataImport2 *pImport, mdToken field, CorElementType *pElementType, mdToken *ptkValueTypeToken, int *pkk)
#else
static BOOL GetStaticFieldElementTypeForFieldDef(Module * pModule, IMDInternalImport *pImport, mdToken field, CorElementType *pElementType, mdToken *ptkValueTypeToken, int *pkk)
#endif
{
    STANDARD_VM_CONTRACT;

    PCCOR_SIGNATURE pMemberSignature;
    DWORD       cMemberSignature;
    DWORD dwMemberAttribs;
#ifdef CLR_STANDALONE_BINDER
    IfFailThrow(pImport->GetFieldProps(field, NULL, NULL, 0, NULL, &dwMemberAttribs, &pMemberSignature, &cMemberSignature, NULL, NULL, NULL));
#else
    IfFailThrow(pImport->GetFieldDefProps(field, &dwMemberAttribs));
#endif
                                
    // Skip non-static and literal fields
    if (!IsFdStatic(dwMemberAttribs) || IsFdLiteral(dwMemberAttribs))
        return TRUE;

    // We need to do an extra check to see if this field is ThreadStatic
    HRESULT hr = pImport->GetCustomAttributeByName((mdToken)field,
                                                    g_ThreadStaticAttributeClassName,
                                                    NULL, NULL);

#if defined(FEATURE_LEGACYNETCF) && defined(CLR_STANDALONE_BINDER)
    // Replicate quirk from code:CMiniMd::CommonGetCustomAttributeByNameEx
    if (FAILED(hr) && RuntimeIsLegacyNetCF(0))
        hr = S_FALSE;
#endif

    IfFailThrow(hr);

    // Use one set of variables for regular statics, and the other set for thread statics
    *pkk = (hr == S_OK) ? 1 : 0;

                    
    // Get the type of the static field.
#ifndef CLR_STANDALONE_BINDER
    IfFailThrow(pImport->GetSigOfFieldDef(field, &cMemberSignature, &pMemberSignature));
    IfFailThrow(validateTokenSig(field,pMemberSignature,cMemberSignature,dwMemberAttribs,pImport));
#endif
                    
    SigTypeContext typeContext; // <TODO> this is an empty type context: is this right? Should we be explicitly excluding all generic types from this iteration? </TODO>
    MetaSig fsig(pMemberSignature, cMemberSignature, pModule, &typeContext, MetaSig::sigField);
    CorElementType ElementType = fsig.NextArg();

    if (ElementType == ELEMENT_TYPE_VALUETYPE)
    {
        // See if we can figure out what the value type is
#ifdef CLR_STANDALONE_BINDER
        MdilModule *pTokenModule;
        mdToken tk = PeekValueTypeTokenClosed(&fsig.GetArgProps(), pModule, &typeContext, &pTokenModule);
#else
        Module *pTokenModule;
        mdToken tk = fsig.GetArgProps().PeekValueTypeTokenClosed(pModule, &typeContext, &pTokenModule);
#endif

        *ptkValueTypeToken = tk;

        // As the current class is not generic, this should never happen, but if it did happen, we
        // would have a problem.
        if (pTokenModule != pModule)
        {
#ifdef CLR_STANDALONE_BINDER
            IfFailThrow(COR_E_BADIMAGEFORMAT);
#else
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_METADATA_CORRUPT);
#endif
        }

        ElementType = ParseMetadataForStaticsIsValueTypeEnum(pModule, pImport, tk);
    }

    *pElementType = ElementType;
    return FALSE;
}
#endif // StaticAllocationHelpers_INL
