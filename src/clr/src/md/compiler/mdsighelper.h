// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************

// 
// MDSigHelp.h
//
// contains utility code for signature parsing and comparisons.
//
// This is needed for validator support, especially because it may need to compare signatures
// across multiple metadata scopes.
//*****************************************************************************

#ifndef __mdsighelper_h_
#define __mdsighelper_h_

#include "regmeta.h"


//****************************************************************************
//****************************************************************************
class MDSigParser : public SigParser
{
    friend class MDSigComparer;

  public:
    //------------------------------------------------------------------------
    // Constructor.
    //------------------------------------------------------------------------
    FORCEINLINE MDSigParser(PCCOR_SIGNATURE ptr, DWORD len)
        : SigParser(ptr, len)
    { }

    FORCEINLINE MDSigParser(const MDSigParser &sig)
        : SigParser(sig.m_ptr, sig.m_dwLen)
    { }
};

//****************************************************************************
//****************************************************************************
class MDSigComparer
{
  public:
    //------------------------------------------------------------------------
    // This is the base type used to provide callback comparison functionality.
    //------------------------------------------------------------------------
    class MDSigComparerBaseType
    {
      public:
        //------------------------------------------------------------------------
        // Returns S_OK if the tokens are equivalent, E_FAIL if they are not, or
        // error.
        //------------------------------------------------------------------------
        virtual HRESULT CompareToken(const mdToken &tok1, const mdToken &tok2) = 0;
    };

    //------------------------------------------------------------------------
    // Ctor
    //------------------------------------------------------------------------
    MDSigComparer(const MDSigParser &sig1,
                  const MDSigParser &sig2,
                  MDSigComparerBaseType &comparer)
        : m_sig1(sig1), m_sig2(sig2), m_comparer(comparer)
    { }

    //------------------------------------------------------------------------
    // Returns S_OK if the signatures are equivalent, E_FAIL if they are not,
    // or error.
    //------------------------------------------------------------------------
    HRESULT CompareMethodSignature();

  protected:
    MDSigParser            m_sig1;
    MDSigParser            m_sig2;
    MDSigComparerBaseType &m_comparer;

    // This will compare exactly one type in each signature to determine
    // if they are equal
    HRESULT _CompareMethodSignature();
    HRESULT _CompareExactlyOne();
    HRESULT _CompareData(ULONG *pulData);
    HRESULT _CompareMethodSignatureHeader(ULONG &cArgs);
};

//****************************************************************************
//****************************************************************************
class UnifiedAssemblySigComparer : public MDSigComparer::MDSigComparerBaseType
{
  public:
    //------------------------------------------------------------------------
    // Ctor
    //------------------------------------------------------------------------
    UnifiedAssemblySigComparer(const RegMeta &regMeta)
        : m_pRegMeta(const_cast<RegMeta*>(&regMeta))
    { }

    //------------------------------------------------------------------------
    // Returns S_OK if the tokens are equivalent, E_FAIL if they are not, or
    // error.
    //------------------------------------------------------------------------
    virtual HRESULT CompareToken(const mdToken &tok1, const mdToken &tok2);

  protected:
    RegMeta *m_pRegMeta;

    HRESULT _CompareAssemblies(mdToken tkAsmRef1,mdToken tkAsmRef2, BOOL* pfEquivalent);

    HRESULT _CreateTypeNameFromTypeRef(
        mdToken tkTypeRef,
        SString &ssName,
        mdToken &tkParent);

    HRESULT _CreateFullyQualifiedTypeNameFromTypeRef(
        mdToken tkTypeRef,
        SString &ssFullName,
        mdToken &tkParent);
};


#endif // __mdsighelper_h_

