// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// StaticAllocationHelpers.inl -
//

//
// Helpers used to determine static offset allocation.
//
//
#ifndef StaticAllocationHelpers_INL
#define StaticAllocationHelpers_INL

// Will return underlying type if it's an enum
//             ELEMENT_TYPE_VALUETYPE if it is a non enum
//             ELEMENT_TYPE_END if it doesn't know (we may not want to load other assemblies)
static CorElementType ParseMetadataForStaticsIsValueTypeEnum(Module * pModule, IMDInternalImport *pImport, mdToken tk)
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
    
    if (FAILED(pImport->GetTypeDefProps(tk, &dwParentAttr, &tkParent)))
    {
        return ELEMENT_TYPE_END;
    }
    
    if (RidFromToken(tkParent) == 0)
    {
        return ELEMENT_TYPE_END;
    }
    
    LPCSTR szName      = NULL;
    LPCSTR szNamespace = NULL;
    
    switch (TypeFromToken(tkParent))
    {
        case mdtTypeDef:
            if (FAILED(pImport->GetNameOfTypeDef(tkParent, &szName, &szNamespace)))
            {
                return ELEMENT_TYPE_END;
            }
            break;
        case mdtTypeRef:
            if (FAILED(pImport->GetNameOfTypeRef(tkParent, &szNamespace, &szName)))
            {
                return ELEMENT_TYPE_END;
            }
            break;
        default:
            return ELEMENT_TYPE_END;
    }
    
    if (szName == NULL || szNamespace == NULL)
    {
        return ELEMENT_TYPE_END;
    }

    // If it doesn't inherit from System.Enum, then it must be a value type
    // Note that loader will not load malformed types so this check is enough
    if (strcmp(szName,"Enum") != 0 || strcmp(szNamespace,"System") != 0)
    {
        return ELEMENT_TYPE_VALUETYPE;
    }
    
    // OK, it's an enum; find its instance field and get its type
    HENUMInternalHolder   hEnum(pImport);
    mdToken tkField;
    hEnum.EnumInit(mdtFieldDef,tk);
    while (pImport->EnumNext(&hEnum,&tkField))
    {
        PCCOR_SIGNATURE pMemberSignature;
        DWORD           cMemberSignature;
        
        // Get the type of the static field.
        DWORD dwMemberAttribs;

        IfFailThrow(pImport->GetFieldDefProps(tkField, &dwMemberAttribs));
        
        if (!IsFdStatic(dwMemberAttribs))
        {
            IfFailThrow(pImport->GetSigOfFieldDef(tkField, &cMemberSignature, &pMemberSignature));
            
            IfFailThrow(validateTokenSig(tkField,pMemberSignature,cMemberSignature,dwMemberAttribs,pImport));

            SigTypeContext typeContext;
            MetaSig fsig(pMemberSignature, cMemberSignature, pModule, &typeContext, MetaSig::sigField);
            CorElementType ElementType = fsig.NextArg();
            return ElementType;
        }
    }

    // no instance field found -- error!
    return ELEMENT_TYPE_END;
}

static BOOL GetStaticFieldElementTypeForFieldDef(Module * pModule, IMDInternalImport *pImport, mdToken field, CorElementType *pElementType, mdToken *ptkValueTypeToken, int *pkk)
{
    STANDARD_VM_CONTRACT;

    PCCOR_SIGNATURE pMemberSignature;
    DWORD       cMemberSignature;
    DWORD dwMemberAttribs;
    IfFailThrow(pImport->GetFieldDefProps(field, &dwMemberAttribs));

    // Skip non-static and literal fields
    if (!IsFdStatic(dwMemberAttribs) || IsFdLiteral(dwMemberAttribs))
        return TRUE;

    // We need to do an extra check to see if this field is ThreadStatic
    HRESULT hr = pModule->GetCustomAttribute((mdToken)field,
                                                    WellKnownAttribute::ThreadStatic,
                                                    NULL, NULL);
    IfFailThrow(hr);

    // Use one set of variables for regular statics, and the other set for thread statics
    *pkk = (hr == S_OK) ? 1 : 0;

                    
    // Get the type of the static field.
    IfFailThrow(pImport->GetSigOfFieldDef(field, &cMemberSignature, &pMemberSignature));
    IfFailThrow(validateTokenSig(field,pMemberSignature,cMemberSignature,dwMemberAttribs,pImport));

    SigTypeContext typeContext; // <TODO> this is an empty type context: is this right? Should we be explicitly excluding all generic types from this iteration? </TODO>
    MetaSig fsig(pMemberSignature, cMemberSignature, pModule, &typeContext, MetaSig::sigField);
    CorElementType ElementType = fsig.NextArg();

    if (ElementType == ELEMENT_TYPE_VALUETYPE)
    {
        // See if we can figure out what the value type is
        Module *pTokenModule;
        mdToken tk = fsig.GetArgProps().PeekValueTypeTokenClosed(pModule, &typeContext, &pTokenModule);

        *ptkValueTypeToken = tk;

        // As the current class is not generic, this should never happen, but if it did happen, we
        // would have a problem.
        if (pTokenModule != pModule)
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_METADATA_CORRUPT);
        }

        ElementType = ParseMetadataForStaticsIsValueTypeEnum(pModule, pImport, tk);
    }

    *pElementType = ElementType;
    return FALSE;
}
#endif // StaticAllocationHelpers_INL
