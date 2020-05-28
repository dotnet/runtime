// This blog post originally appeared on David Broman's blog on 10/13/2005

#include "SigFormat.cpp"
 
 
 // ---------------------------------------------------------------------
 // ---------------------------------------------------------------------
 // This file does not compile on its own. It contains snippets of code you can add
 // to a working profiler, so that your profiler will invoke instances of the SigFormat
 // object to parse and pretty-print all the types in all modules as they're loaded.
 //
 // The functions are ordered from callees to callers (so no forward declarations are
 // necessary). If you prefer a top-down approach to learning code, then start
 // at the bottom of the file.
 // ---------------------------------------------------------------------
 // ---------------------------------------------------------------------
 
 
 // ****************************************************************
 // HELPERS TO READ THROUGH METADATA, FIND SIGNATURES, AND INVOKE THE PARSER
 // ****************************************************************
 
 // Simple wrapper to create an instance of SigFormat and invoke it
HRESULT DoParse(sig_byte * sig, ULONG cbSig)
{
    SigFormat sf;
    HRESULT hr;
    bool fRet = sf.Parse(sig, cbSig);
    if (!fRet)
    {
        hr = E_FAIL;
        goto Error;
    }

    hr = S_OK;

    Cleanup:
    return hr;

    Error:
    goto Cleanup;
}

 // Takes an mdProperty, prints an intro line, then invokes the parser / printer
HRESULT PrintProperty(ModuleID moduleID, IMetaDataImport* pMDImport, LPCWSTR wszClassName, mdProperty md)
{
    HRESULT hr;
    mdTypeDef td;
    WCHAR wszName[500];
    ULONG cchName;
    PCCOR_SIGNATURE sigMember;
    ULONG cbSigMember;
    DWORD dwAttr;
    DWORD dwCPlusTypeFlag;
    UVCP_CONSTANT pValue;
    ULONG cchValue;
    mdMethodDef mdSetter;
    mdMethodDef mdGetter;
    mdMethodDef aOtherMethods[100];
    ULONG cOtherMethods;

    hr = pMDImport->GetPropertyProps(md, // The member for which to get props. 
                                     &td, // Put member's class here. 
                                     wszName, // Put member's name here. 
                                     dimensionof(wszName), // Size of szMember buffer in wide chars. 
                                     &cchName, // Put actual size here 
                                     &dwAttr, // Put flags here. 
                                     &sigMember, // [OUT] point to the blob value of meta data 
                                     &cbSigMember, // [OUT] actual size of signature blob 
                                     &dwCPlusTypeFlag, // [OUT] flag for value type. selected ELEMENT_TYPE_* 
                                     &pValue, // [OUT] constant value 
                                     &cchValue,
                                     &mdSetter, // [OUT] setter method of the property 
                                     &mdGetter, // [OUT] getter method of the property 
                                     aOtherMethods, // [OUT] other method of the property 
                                     dimensionof(aOtherMethods), // [IN] size of rmdOtherMethod
                                     &cOtherMethods); // [OUT] total number of other method of this property
    if (FAILED(hr))
    {
        goto Error;
    }

    printf("\n%S.%S (0x%x)\n", wszClassName, wszName, md);
    DoParse((sig_byte *) sigMember, cbSigMember);

    hr = S_OK;

    Cleanup:
    return hr;

    Error:
    goto Cleanup;

}


 // Takes a field token, prints an intro line, then invokes the parser / printer
HRESULT PrintField(ModuleID moduleID, IMetaDataImport* pMDImport, LPCWSTR wszClassName, mdToken md)
{
    HRESULT hr;
    mdTypeDef td;
    WCHAR wszName[500];
    ULONG cchName;
    PCCOR_SIGNATURE sigMember;
    ULONG cbSigMember;
    DWORD dwAttr;
    DWORD dwCPlusTypeFlag;
    UVCP_CONSTANT pValue;
    ULONG cchValue;

    hr = pMDImport->GetFieldProps(md, // The member for which to get props. 
                                  &td, // Put member's class here. 
                                  wszName, // Put member's name here. 
                                  dimensionof(wszName), // Size of szMember buffer in wide chars. 
                                  &cchName, // Put actual size here 
                                  &dwAttr, // Put flags here. 
                                  &sigMember, // [OUT] point to the blob value of meta data 
                                  &cbSigMember, // [OUT] actual size of signature blob 
                                  &dwCPlusTypeFlag, // [OUT] flag for value type. selected ELEMENT_TYPE_* 
                                  &pValue, // [OUT] constant value 
                                  &cchValue); // [OUT] size of constant string in chars, 0 for non-strings.
    if (FAILED(hr))
    {
        goto Error;
    }

    printf("\n%S.%S (0x%x)\n", wszClassName, wszName, md);
    DoParse((sig_byte *) sigMember, cbSigMember);

    hr = S_OK;

    Cleanup:
    return hr;

    Error:
    goto Cleanup;

}

 // Takes an mdMethodDef, prints an intro line, then invokes the parser / printer on its signature and its locals
HRESULT PrintMethodDef(ModuleID moduleID, IMetaDataImport* pMDImport, LPCWSTR wszClassName, mdMethodDef md)
{
    HRESULT hr;
    mdTypeDef td;
    WCHAR wszMethod[500];
    ULONG cchMethod;
    DWORD dwAttr;
    PCCOR_SIGNATURE sigParam;
    PCCOR_SIGNATURE sigLocal;
    ULONG cbSigParam;
    ULONG cbSigLocal;
    ULONG ulCodeRVA;
    DWORD dwImplFlags;
    BOOL fMore;
    LPCBYTE pMethodHeader = NULL;
    ULONG cbMethodSize;
    IMAGE_COR_ILMETHOD_TINY* pimt = NULL;
    IMAGE_COR_ILMETHOD_FAT* pimf = NULL;

    hr = pMDImport->GetMethodProps(md, // The method for which to get props. 
                                   &td, // Put method's class here. 
                                   wszMethod, // Put method's name here. 
                                   dimensionof(wszMethod), // Size of szMethod buffer in wide chars. 
                                   &cchMethod, // Put actual size here 
                                   &dwAttr, // Put flags here. 
                                   &sigParam, // [OUT] point to the blob value of meta data 
                                   &cbSigParam, // [OUT] actual size of signature blob 
                                   &ulCodeRVA, // [OUT] codeRVA 
                                   &dwImplFlags); // [OUT] Impl. Flags 
    if (FAILED(hr))
    {
        goto Error;
    }

    printf("\n%S.%S (0x%x)\n", wszClassName, wszMethod, md);

     // Method prototype signature parse
    DoParse((sig_byte *) sigParam, cbSigParam);

     // Method locals signature parse
    hr = g_pProfilerInfo->GetILFunctionBody(moduleID,
                                            md,
                                            &pMethodHeader,
                                            &cbMethodSize);
    if (FAILED(hr))
    {
        goto EndLocal;
    }

    // The following odd-looking lines of code decode the method header, ensure
    // it is in a format that contains local variables, and then grabs the local
    // variable signature out of the header.

    pimt = (IMAGE_COR_ILMETHOD_TINY*) pMethodHeader;
    if ((pimt->Flags_CodeSize & (CorILMethod_FormatMask >> 1)) != CorILMethod_FatFormat)
    {
        goto EndLocal;
    }

    pimf = (IMAGE_COR_ILMETHOD_FAT*) pMethodHeader;
    if (pimf->LocalVarSigTok == 0)
    {
        goto EndLocal;
    }

    hr = pMDImport->GetSigFromToken(pimf->LocalVarSigTok,
                                    &sigLocal,
                                    &cbSigLocal);

    DoParse((sig_byte *) sigLocal, cbSigLocal);

    EndLocal:

    hr = S_OK;

    Cleanup:
    return hr;

    Error:
    goto Cleanup;
}


 // Simple helper to print an intro line for a class
void PrintHeader(LPCWSTR wszClassName, mdTypeDef td, LPCSTR szCategory)
{
    printf("\n--------------------------------------------\n");
    printf("%S (0x%x):\t%s\n", wszClassName, td, szCategory);
    printf("--------------------------------------------\n\n");
}


 // Combines above functions to print the methods, properties, and fields of a class
HRESULT PrintTypedef(ModuleID moduleID, IMetaDataImport* pMDImport, mdTypeDef td)
{
    HRESULT hr;
    HCORENUM hEnum = NULL;
    mdMethodDef aMethods[100];
    mdFieldDef aFields[100];
    mdFieldDef aProperties[100];
    ULONG cMethodDefs;
    ULONG cFields;
    ULONG cProperties;
    ULONG i;
    WCHAR wszTdName[200];
    ULONG cchTdName;
    DWORD dwTypeDefFlags;
    mdToken tkExtends;
    BOOL fMore;

    hr = pMDImport->GetTypeDefProps(td, // [IN] TypeDef token for inquiry.
                                    wszTdName, // [OUT] Put name here.
                                    dimensionof(wszTdName), // [IN] size of name buffer in wide chars.
                                    &cchTdName, // [OUT] put size of name (wide chars) here.
                                    &dwTypeDefFlags, // [OUT] Put flags here.
                                    &tkExtends); // [OUT] Put base class TypeDef/TypeRef here.
    if (FAILED(hr))
    {
        goto Error;
    }

    PrintHeader(wszTdName, td, "METHODDEFS");
    fMore = TRUE;
    while (fMore)
    {
        hr = pMDImport->EnumMethods(&hEnum,
                                    td, // [IN] TypeDef to scope the enumeration. 
                                    aMethods, // [OUT] Put MethodDefs here. 
                                    dimensionof(aMethods), // [IN] Max MethodDefs to put. 
                                    &cMethodDefs); // [OUT] Put # put here. 
        if (FAILED(hr))
        {
            goto Error;
        }

        if (hr == S_FALSE)
        {
            fMore = FALSE;
        }

        for (i=0; i < cMethodDefs; i++)
        {
            hr = PrintMethodDef(moduleID, pMDImport, wszTdName, aMethods[i]);
            if (FAILED(hr))
            {
                 // do you care? If so, do something about this.
            }
        }
    }

    pMDImport->CloseEnum(hEnum);
    hEnum = NULL;

    PrintHeader(wszTdName, td, "FIELDS");
    fMore = TRUE;
    while (fMore)
    {
        hr = pMDImport->EnumFields(&hEnum,
                                   td, 
                                   Fields, 
                                   dimensionof(aFields),
                                   &cFields);

        if (FAILED(hr))
        {
            goto Error;
        }

        if (hr == S_FALSE)
        {
            fMore = FALSE;
        }

        for (i=0; i < cFields; i++)
        {
            hr = PrintField(moduleID, pMDImport, wszTdName, aFields[i]);
            if (FAILED(hr))
            {
 				// do you care? If so, do something about this.
            }
        }
    }

    pMDImport->CloseEnum(hEnum);
    hEnum = NULL;

    PrintHeader(wszTdName, td, "PROPERTIES");
    fMore = TRUE;
    while (fMore)
    {
        hr = pMDImport->EnumProperties(&hEnum,
									   td, 
									   aProperties, 
									   dimensionof(aProperties),
									   &cProperties); 
        if (FAILED(hr))
        {
            goto Error;
        }

        if (hr == S_FALSE)
        {
            fMore = FALSE;
        }

        for (i=0; i < cProperties; i++)
        {
            hr = PrintProperty(moduleID, pMDImport, wszTdName, aProperties[i]);
            if (FAILED(hr))
            {
 				// do you care? If so, do something about this.
            }
        }
    }

    pMDImport->CloseEnum(hEnum);
    hEnum = NULL;

    hr = S_OK;

    Cleanup:
    if (hEnum != NULL)
    {
        pMDImport->CloseEnum(hEnum);
    }
    return hr;

    Error:
    goto Cleanup;
}


 // Enumerates the typedefs in a module via the metadata interface, and calls PrintTypedef
 // on each one
HRESULT PrintMetadata(ModuleID moduleID, IMetaDataImport* pMDImport)
{
    HRESULT hr;
    HCORENUM hEnum = NULL;
    mdTypeDef aTypeDefs[100];
    ULONG cTypeDefs;
    ULONG i;
    BOOL fMoreTypeDefs = TRUE;

    while (fMoreTypeDefs)
    {
        hr = pMDImport->EnumTypeDefs(&hEnum,
							         aTypeDefs,
							         dimensionof(aTypeDefs),
							         &cTypeDefs);
        if (FAILED(hr))
        {
            goto Error;
        }

        if (hr == S_FALSE)
        {
            fMoreTypeDefs = FALSE;
        }

        for (i=0; i < cTypeDefs; i++)
        {
            hr = PrintTypedef(moduleID, pMDImport, aTypeDefs[i]);
            if (FAILED(hr))
            {
 				// do you care? If so, do something about this.
            }
        }
    }

    hr = S_OK;

    Cleanup:
    if (hEnum != NULL)
    {
        pMDImport->CloseEnum(hEnum);
    }
    return hr;

    Error:
    goto Cleanup;
}


 // ****************************************************************
 // Add this to your profiler's ICorProfilerCallback2::ModuleLoadFinished implementation.
 // It is assumed your copy of the ICorProfilerInfo2 interface may be accessed via
 // g_pProfilerInfo. Change the code to fit your profiler as appropriate.
 // ****************************************************************
 //
 // As a module gets loaded, this callback implementation initiates the pretty-printer to
 // log all the types to stdout.
HRESULT CYourProfImpl::ModuleLoadFinished( ModuleID moduleID, HRESULT hrStatus )
{
    HRESULT hr;
    LPCBYTE pbBaseLoadAddr;
    WCHAR wszName[300];
    ULONG cchNameIn = dimensionof(wszName);
    ULONG cchNameOut;
    AssemblyID assemblyID;

    hr = g_pProfilerInfo->GetModuleInfo(moduleID,
									    &pbBaseLoadAddr,
									    cchNameIn,
									    &cchNameOut,
									    wszName,
									    &assemblyID);
    if (FAILED(hr))
    {
        return hr;
    }

    printf("MODULE LOAD FINISHED: %S\n", wszName);

    IMetaDataImport *pMDImport = NULL;
    hr = g_pProfilerInfo->GetModuleMetaData(moduleID,
									        ofRead,
									        IID_IMetaDataImport,
									        (IUnknown **)&pMDImport );
    if (FAILED(hr))
    {
        return hr;
    }

    hr = PrintMetadata(moduleID, pMDImport);
    if (FAILED(hr))
    {
 		// Do any error handling as appropriate
    }

    hr = S_OK;

    Cleanup:
    return hr;

    Error:
    goto Cleanup;
}
