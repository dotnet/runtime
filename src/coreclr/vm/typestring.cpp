// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// typestring.cpp
// ---------------------------------------------------------------------------
//

//
// This module contains a helper function used to produce string
// representations of types, with options to control the appearance of
// namespace and assembly information.  Its primary use is in
// reflection (Type.Name, Type.FullName, Type.ToString, etc) but over
// time it could replace the use of TypeHandle.GetName etc for
// diagnostic messages.
//
// See the header file for more details
// ---------------------------------------------------------------------------


#include "common.h"
#include "class.h"
#include "typehandle.h"
#include "sstring.h"
#include "sigformat.h"
#include "typeparse.h"
#include "typestring.h"
#include "ex.h"
#include "typedesc.h"

//
// TypeNameBuilder
//
TypeNameBuilder::TypeNameBuilder(SString* pStr, ParseState parseState /*= ParseStateSTART*/) :
    m_pStr(NULL)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    Clear();
    m_pStr = pStr;
    m_parseState = parseState;
}

void TypeNameBuilder::PushOpenGenericArgument()
{
    WRAPPER_NO_CONTRACT;

    m_stack.Push(m_pStr->GetCount());
}

void TypeNameBuilder::PopOpenGenericArgument()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    COUNT_T index = m_stack.Pop();

    if (!m_bHasAssemblySpec)
        m_pStr->Delete(m_pStr->Begin() + index - 1, 1);

    m_bHasAssemblySpec = FALSE;
}

/* This method escapes szName and appends it to this TypeNameBuilder */
void TypeNameBuilder::EscapeName(LPCWSTR szName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (TypeString::ContainsReservedChar(szName))
    {
        while (* szName)
        {
            WCHAR c = * szName ++;

            if (IsTypeNameReservedChar(c))
                Append(W('\\'));

            Append(c);
        }
    }
    else
    {
        Append(szName);
    }
}

void TypeNameBuilder::EscapeAssemblyName(LPCWSTR szName)
{
    WRAPPER_NO_CONTRACT;

    Append(szName);
}

void TypeNameBuilder::EscapeEmbeddedAssemblyName(LPCWSTR szName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPCWSTR itr = szName;
    bool bContainsReservedChar = false;

    while (*itr)
    {
        if (W(']') == *itr++)
        {
            bContainsReservedChar = true;
            break;
        }
    }

    if (bContainsReservedChar)
    {
        itr = szName;
        while (*itr)
        {
            WCHAR c = *itr++;
            if (c == ']')
                Append(W('\\'));

            Append(c);
        }
    }
    else
    {
        Append(szName);
    }
}

HRESULT TypeNameBuilder::OpenGenericArgument()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!CheckParseState(ParseStateSTART))
        return Fail();

    if (m_instNesting == 0)
        return Fail();

    HRESULT hr = S_OK;

    m_parseState = ParseStateSTART;
    m_bNestedName = FALSE;

    if (!m_bFirstInstArg)
        Append(W(','));

    m_bFirstInstArg = FALSE;

    if (m_bUseAngleBracketsForGenerics)
        Append(W('<'));
    else
        Append(W('['));
    PushOpenGenericArgument();

    return hr;
}

HRESULT TypeNameBuilder::AddName(LPCWSTR szName)
{
    WRAPPER_NO_CONTRACT;
    return AddName(szName, NULL);
}

HRESULT TypeNameBuilder::AddName(LPCWSTR szName, LPCWSTR szNamespace)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!szName)
        return Fail();

    if (!CheckParseState(ParseStateSTART | ParseStateNAME))
        return Fail();

    HRESULT hr = S_OK;

    m_parseState = ParseStateNAME;

    if (m_bNestedName)
        Append(W('+'));

    m_bNestedName = TRUE;

    if (szNamespace && *szNamespace)
    {
        EscapeName(szNamespace);
        Append(W('.'));
    }

    EscapeName(szName);

    return hr;
}

HRESULT TypeNameBuilder::OpenGenericArguments()
{
    WRAPPER_NO_CONTRACT;

    if (!CheckParseState(ParseStateNAME))
        return Fail();

    HRESULT hr = S_OK;

    m_parseState = ParseStateSTART;
    m_instNesting ++;
    m_bFirstInstArg = TRUE;

    if (m_bUseAngleBracketsForGenerics)
        Append(W('<'));
    else
        Append(W('['));

    return hr;
}

HRESULT TypeNameBuilder::CloseGenericArguments()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!m_instNesting)
        return Fail();
    if (!CheckParseState(ParseStateSTART))
        return Fail();

    HRESULT hr = S_OK;

    m_parseState = ParseStateGENARGS;

    m_instNesting --;

    if (m_bFirstInstArg)
    {
        m_pStr->Truncate(m_pStr->End() - 1);
    }
    else
    {
        if (m_bUseAngleBracketsForGenerics)
            Append(W('>'));
        else
            Append(W(']'));
    }

    return hr;
}

HRESULT TypeNameBuilder::AddPointer()
{
    WRAPPER_NO_CONTRACT;

    if (!CheckParseState(ParseStateNAME | ParseStateGENARGS | ParseStatePTRARR))
        return Fail();

    m_parseState = ParseStatePTRARR;

    Append(W('*'));

    return S_OK;
}

HRESULT TypeNameBuilder::AddByRef()
{
    WRAPPER_NO_CONTRACT;

    if (!CheckParseState(ParseStateNAME | ParseStateGENARGS | ParseStatePTRARR))
        return Fail();

    m_parseState = ParseStateBYREF;

    Append(W('&'));

    return S_OK;
}

HRESULT TypeNameBuilder::AddSzArray()
{
    WRAPPER_NO_CONTRACT;

    if (!CheckParseState(ParseStateNAME | ParseStateGENARGS | ParseStatePTRARR))
        return Fail();

    m_parseState = ParseStatePTRARR;

    Append(W("[]"));

    return S_OK;
}

HRESULT TypeNameBuilder::AddArray(DWORD rank)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!CheckParseState(ParseStateNAME | ParseStateGENARGS | ParseStatePTRARR))
        return Fail();

    m_parseState = ParseStatePTRARR;

    if (rank <= 0)
        return E_INVALIDARG;

    if (rank == 1)
    {
        Append(W("[*]"));
    }
    else if (rank > 64)
    {
        // Only taken in an error path, runtime will not load arrays of more than 32 dimensions
        const UTF8 fmt[] = "[%d]";
        UTF8 strTmp[ARRAY_SIZE(fmt) + MaxUnsigned32BitDecString];
        _snprintf_s(strTmp, ARRAY_SIZE(strTmp), _TRUNCATE, fmt, rank);
        Append(strTmp);
    }
    else
    {
        WCHAR* wzDim = (WCHAR*)_alloca(sizeof(WCHAR) * (rank+3));

        WCHAR* pwz = wzDim+1;
        *wzDim = W('[');
        for(COUNT_T i = 1; i < rank; i++, pwz++)
            *pwz=',';
        *pwz = W(']');
        *(++pwz) = W('\0');
        Append(wzDim);
    }

    return S_OK;
}

HRESULT TypeNameBuilder::CloseGenericArgument()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!CheckParseState(ParseStateNAME | ParseStateGENARGS | ParseStatePTRARR | ParseStateBYREF | ParseStateASSEMSPEC))
        return Fail();

    if (m_instNesting == 0)
        return Fail();

    m_parseState = ParseStateSTART;

    if (m_bHasAssemblySpec)
    {
        if (m_bUseAngleBracketsForGenerics)
            Append(W('>'));
        else
            Append(W(']'));
    }

    PopOpenGenericArgument();

    return S_OK;
}

HRESULT TypeNameBuilder::AddAssemblySpec(LPCWSTR szAssemblySpec)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!CheckParseState(ParseStateNAME | ParseStateGENARGS | ParseStatePTRARR | ParseStateBYREF))
        return Fail();

    HRESULT hr = S_OK;

    m_parseState = ParseStateASSEMSPEC;

    if (szAssemblySpec && *szAssemblySpec)
    {
        Append(W(", "));

        if (m_instNesting > 0)
        {
            EscapeEmbeddedAssemblyName(szAssemblySpec);
        }
        else
        {
            EscapeAssemblyName(szAssemblySpec);
        }

        m_bHasAssemblySpec = TRUE;
        hr = S_OK;
    }

    return hr;
}

HRESULT TypeNameBuilder::Clear()
{
    CONTRACTL
    {
        THROWS; // TypeNameBuilder::Stack::Clear might throw.
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pStr)
    {
        m_pStr->Clear();
    }
    m_bNestedName = FALSE;
    m_instNesting = 0;
    m_bFirstInstArg = FALSE;
    m_parseState = ParseStateSTART;
    m_bHasAssemblySpec = FALSE;
    m_bUseAngleBracketsForGenerics = FALSE;
    m_stack.Clear();

    return S_OK;
}



// Append the name of the type td to the string
// The following flags in the FormatFlags argument are significant: FormatNamespace
void TypeString::AppendTypeDef(SString& ss, IMDInternalImport *pImport, mdTypeDef td, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        GC_NOTRIGGER;
        THROWS;
    }
    CONTRACT_END

    {
        TypeNameBuilder tnb(&ss, TypeNameBuilder::ParseStateNAME);
        AppendTypeDef(tnb, pImport, td, format);
    }

    RETURN;
}


void TypeString::AppendTypeDef(TypeNameBuilder& tnb, IMDInternalImport *pImport, mdTypeDef td, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        GC_NOTRIGGER;
        THROWS;
        PRECONDITION(CheckPointer(pImport));
        PRECONDITION(TypeFromToken(td) == mdtTypeDef);
    }
    CONTRACT_END

    LPCUTF8 szName;
    LPCUTF8 szNameSpace;
    IfFailThrow(pImport->GetNameOfTypeDef(td, &szName, &szNameSpace));

    const WCHAR *wszNameSpace = NULL;

    InlineSString<128> ssName(SString::Utf8, szName);
    InlineSString<128> ssNameSpace;

    if (format & FormatNamespace)
    {
        ssNameSpace.SetUTF8(szNameSpace);
        wszNameSpace = ssNameSpace.GetUnicode();
    }

    tnb.AddName(ssName.GetUnicode(), wszNameSpace);

    RETURN;
}

void TypeString::AppendNestedTypeDef(TypeNameBuilder& tnb, IMDInternalImport *pImport, mdTypeDef td, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        GC_NOTRIGGER;
        THROWS;
        PRECONDITION(CheckPointer(pImport));
        PRECONDITION(TypeFromToken(td) == mdtTypeDef);
    }
    CONTRACT_END

    DWORD dwAttr;
    IfFailThrow(pImport->GetTypeDefProps(td, &dwAttr, NULL));

    StackSArray<mdTypeDef> arNames;
    arNames.Append(td);
    if (format & FormatNamespace && IsTdNested(dwAttr))
    {
        while (SUCCEEDED(pImport->GetNestedClassProps(td, &td)))
            arNames.Append(td);
    }

    for(SCOUNT_T i = arNames.GetCount() - 1; i >= 0; i --)
        AppendTypeDef(tnb, pImport, arNames[i], format);

    RETURN;
}

// Append a square-bracket-enclosed, comma-separated list of n type parameters in inst to the string s
// and enclose each parameter in square brackets to disambiguate the commas
// The following flags in the FormatFlags argument are significant: FormatNamespace FormatFullInst FormatAssembly FormatNoVersion
void TypeString::AppendInst(SString& ss, Instantiation inst, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        if (format & (FormatAssembly|FormatFullInst)) GC_TRIGGERS; else GC_NOTRIGGER;
        THROWS;
    }
    CONTRACT_END

    {
        TypeNameBuilder tnb(&ss, TypeNameBuilder::ParseStateNAME);
        if ((format & FormatAngleBrackets) != 0)
            tnb.SetUseAngleBracketsForGenerics(TRUE);
        AppendInst(tnb, inst, format);
    }

    RETURN;
}

void TypeString::AppendInst(TypeNameBuilder& tnb, Instantiation inst, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        THROWS;
        if (format & (FormatAssembly|FormatFullInst)) GC_TRIGGERS; else GC_NOTRIGGER;
        PRECONDITION(!inst.IsEmpty());
    }
    CONTRACT_END

    tnb.OpenGenericArguments();

    for (DWORD i = 0; i < inst.GetNumArgs(); i++)
    {
        tnb.OpenGenericArgument();

        TypeHandle thArg = inst[i];

        if ((format & FormatFullInst) != 0 && !thArg.IsGenericVariable())
        {
            AppendType(tnb, thArg, Instantiation(), format | FormatNamespace | FormatAssembly);
        }
        else
        {
            AppendType(tnb, thArg, Instantiation(), format & (FormatNamespace | FormatAngleBrackets
#ifdef _DEBUG
                                               | FormatDebug
#endif
                                               ));
        }

        tnb.CloseGenericArgument();
    }

    tnb.CloseGenericArguments();

    RETURN;
}

void TypeString::AppendParamTypeQualifier(TypeNameBuilder& tnb, CorElementType kind, DWORD rank)
{
    CONTRACTL
    {
        MODE_ANY;
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(CorTypeInfo::IsModifier(kind));
    }
    CONTRACTL_END

    switch (kind)
    {
    case ELEMENT_TYPE_BYREF :
        tnb.AddByRef();
        break;
    case ELEMENT_TYPE_PTR :
        tnb.AddPointer();
        break;
    case ELEMENT_TYPE_SZARRAY :
        tnb.AddSzArray();
        break;
    case ELEMENT_TYPE_ARRAY :
        tnb.AddArray(rank);
        break;
    default :
        break;
    }
}

// Append a representation of the type t to the string s
// The following flags in the FormatFlags argument are significant: FormatNamespace FormatFullInst FormatAssembly FormatNoVersion

void TypeString::AppendType(SString& ss, TypeHandle ty, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        if (format & (FormatAssembly|FormatFullInst)) GC_TRIGGERS; else GC_NOTRIGGER;
        THROWS;
    }
    CONTRACT_END

    AppendType(ss, ty, Instantiation(), format);

    RETURN;
}

void TypeString::AppendType(SString& ss, TypeHandle ty, Instantiation typeInstantiation, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        if (format & (FormatAssembly|FormatFullInst)) GC_TRIGGERS; else GC_NOTRIGGER;
        THROWS;
    }
    CONTRACT_END

    {
        TypeNameBuilder tnb(&ss);
        if ((format & FormatAngleBrackets) != 0)
            tnb.SetUseAngleBracketsForGenerics(TRUE);
        AppendType(tnb, ty, typeInstantiation, format);
    }

    RETURN;
}

void TypeString::AppendType(TypeNameBuilder& tnb, TypeHandle ty, Instantiation typeInstantiation, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;

        /* This method calls Assembly::GetDisplayName. Since that function
        uses Fusion which takes some Crsts in some places, it is GC_TRIGGERS.
        It could be made GC_NOTRIGGER by factoring out Assembly::GetDisplayName.
        However, its better to leave stuff as GC_TRIGGERS unless really needed,
        as GC_NOTRIGGER ties your hands up. */
        if (format & (FormatAssembly|FormatFullInst)) GC_TRIGGERS; else GC_NOTRIGGER;
        THROWS;
    }
    CONTRACT_END

    BOOL bToString = (format & (FormatNamespace|FormatFullInst|FormatAssembly)) == FormatNamespace;

    // It's null!
    if (ty.IsNull())
    {
        tnb.AddName(W("(null)"));
    }
    else

    // It's an array, with format
    //   element_ty[] (1-d, SZARRAY)
    //   element_ty[*] (1-d, ARRAY)
    //   element_ty[,] (2-d, ARRAY) etc
    // or a pointer (*) or byref (&)
    if (ty.HasTypeParam())
    {
        if (ty.GetSignatureCorElementType() != ELEMENT_TYPE_VALUETYPE)
        {
            DWORD rank;
            TypeHandle elemType;

            rank = ty.IsArray() ? ty.GetRank() : 0;
            elemType = ty.GetTypeParam();

            _ASSERTE(!elemType.IsNull());
            AppendType(tnb, elemType, Instantiation(), format & ~FormatAssembly);
            AppendParamTypeQualifier(tnb, ty.GetSignatureCorElementType(), rank);
        }
        else
        {
            tnb.Append(W("VALUETYPE"));
            TypeHandle elemType = ty.GetTypeParam();
            AppendType(tnb, elemType, Instantiation(), format & ~FormatAssembly);
        }
    }

    // ...or type parameter
    else if (ty.IsGenericVariable())
    {
        PTR_TypeVarTypeDesc tyvar = dac_cast<PTR_TypeVarTypeDesc>(ty.AsTypeDesc());

        mdGenericParam token = tyvar->GetToken();

        LPCSTR szName = NULL;
        mdToken mdOwner;

        IfFailThrow(ty.GetModule()->GetMDImport()->GetGenericParamProps(token, NULL, NULL, &mdOwner, NULL, &szName));

        _ASSERTE(TypeFromToken(mdOwner) == mdtTypeDef || TypeFromToken(mdOwner) == mdtMethodDef);

        LPCSTR szPrefix;
        if (!(format & FormatGenericParam))
            szPrefix = "";
        else if (TypeFromToken(mdOwner) == mdtTypeDef)
            szPrefix = "!";
        else
            szPrefix = "!!";

        SmallStackSString pName(SString::Utf8, szPrefix);
        pName.AppendUTF8(szName);
        tnb.AddName(pName.GetUnicode());

        format &= ~FormatAssembly;
    }

    // ...or function pointer
    else if (ty.IsFnPtrType())
    {
        // Don't attempt to format this currently, it may trigger GC due to fixups.
        tnb.AddName(W("(fnptr)"));
    }

    // ...otherwise it's just a plain type def or an instantiated type
    else
    {
        // Get the TypeDef token and attributes
        IMDInternalImport *pImport = ty.GetMethodTable()->GetMDImport();
        mdTypeDef td = ty.GetCl();
        if (IsNilToken(td)) {
            // This type does not exist in metadata. Simply append "dynamicClass".
            tnb.AddName(W("(dynamicClass)"));
        }
        else
        {
#ifdef _DEBUG
            if (format & FormatDebug)
            {
                UTF8 buffer[128];
                _snprintf_s(buffer, ARRAY_SIZE(buffer), _TRUNCATE, "(%p)", (VOID *)dac_cast<TADDR>(ty.AsPtr()));
                MAKE_WIDEPTR_FROMUTF8(pointerName, buffer);
                tnb.AddName(pointerName);
            }
#endif
            AppendNestedTypeDef(tnb, pImport, td, format);
        }

        // Append the instantiation
        if ((format & (FormatNamespace|FormatAssembly)) && ty.HasInstantiation() && (!ty.IsGenericTypeDefinition() || bToString))
        {
            if (typeInstantiation.IsEmpty())
                AppendInst(tnb, ty.GetInstantiation(), format);
            else
                AppendInst(tnb, typeInstantiation, format);
        }
    }

    // Now append the assembly
    if (format & FormatAssembly)
    {
        Assembly* pAssembly = ty.GetAssembly();
        _ASSERTE(pAssembly != NULL);

        StackSString pAssemblyName;
#ifdef DACCESS_COMPILE
        pAssemblyName.SetUTF8(pAssembly->GetSimpleName());
#else
        pAssembly->GetDisplayName(pAssemblyName,
                                  ASM_DISPLAYF_PUBLIC_KEY_TOKEN | ASM_DISPLAYF_CONTENT_TYPE |
                                  (format & FormatNoVersion ? 0 : ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE));
#endif

        tnb.AddAssemblySpec(pAssemblyName.GetUnicode());

    }

    RETURN;
}

void TypeString::AppendMethod(SString& s, MethodDesc *pMD, Instantiation typeInstantiation, const DWORD format)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(s.Check());
    }
    CONTRACTL_END

    AppendMethodImpl(s, pMD, typeInstantiation, format);
}

void TypeString::AppendMethodInternal(SString& s, MethodDesc *pMD, const DWORD format)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        SUPPORTS_DAC;
        THROWS;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(s.Check());
    }
    CONTRACTL_END

    AppendMethodImpl(s, pMD, Instantiation(), format);
}

void TypeString::AppendMethodImpl(SString& ss, MethodDesc *pMD, Instantiation typeInstantiation, const DWORD format)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        SUPPORTS_DAC;
        THROWS;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(ss.Check());
    }
    CONTRACTL_END

    {
        TypeHandle th;

        if (pMD->IsDynamicMethod())
        {
            if (pMD->IsLCGMethod())
            {
                SString sss(SString::Literal, "DynamicClass");
                ss += sss;
            }
            else if (pMD->IsILStub())
            {
                SString sss(SString::Literal, ILStubResolver::GetStubClassName(pMD));
                ss += sss;
            }
        }
        else
        {
            th = TypeHandle(pMD->GetMethodTable());
            AppendType(ss, th, typeInstantiation, format);
        }

        SString sss1(SString::Literal, NAMESPACE_SEPARATOR_STR);
        ss += sss1;
        SString sss2(SString::Utf8, pMD->GetName());
        ss += sss2;

        if (pMD->HasMethodInstantiation() && !pMD->IsGenericMethodDefinition())
        {
            AppendInst(ss, pMD->GetMethodInstantiation(), format);
        }

        if (format & FormatSignature)
        {
            // @TODO: The argument list should be formatted nicely using AppendType()

            SigFormat sigFormatter(pMD, th);
            const char* sigStr = sigFormatter.GetCStringParmsOnly();
            SString sss(SString::Utf8, sigStr);
            ss += sss;
        }

        if (format & FormatStubInfo) {
            if (pMD->IsInstantiatingStub())
            {
                SString sss(SString::Literal, "{inst-stub}");
                ss += sss;
            }
            if (pMD->IsUnboxingStub())
            {
                SString sss(SString::Literal, "{unbox-stub}");
                ss += sss;
            }
            if (pMD->IsSharedByGenericMethodInstantiations())
            {
                SString sss(SString::Literal, "{method-shared}");
                ss += sss;
            }
            else if (pMD->IsSharedByGenericInstantiations())
            {
                SString sss(SString::Literal, "{shared}");
                ss += sss;
            }
            if (pMD->RequiresInstMethodTableArg())
            {
                SString sss(SString::Literal, "{requires-mt-arg}");
                ss += sss;
            }
            if (pMD->RequiresInstMethodDescArg())
            {
                SString sss(SString::Literal, "{requires-mdesc-arg}");
                ss += sss;
            }
        }
    }
}

void TypeString::AppendField(SString& s, FieldDesc *pFD, Instantiation typeInstantiation, const DWORD format /* = FormatNamespace */)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(CheckPointer(pFD));
        PRECONDITION(s.Check());
    }
    CONTRACTL_END;

    {
        TypeHandle th(pFD->GetApproxEnclosingMethodTable());
        AppendType(s, th, typeInstantiation, format);

        s.AppendUTF8(NAMESPACE_SEPARATOR_STR);
        s.AppendUTF8(pFD->GetName());
    }
}

#ifdef _DEBUG
void TypeString::AppendMethodDebug(SString& ss, MethodDesc *pMD)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        NOTHROW;
        PRECONDITION(CheckPointer(pMD));
        PRECONDITION(ss.Check());
    }
    CONTRACTL_END

#ifndef DACCESS_COMPILE
    EX_TRY
    {
        AppendMethodInternal(ss, pMD, FormatSignature | FormatNamespace);
    }
    EX_CATCH
    {
        // This function is only used as diagnostic aid in debug builds.
        // If we run out of memory or hit some other problem,
        // tough luck for the debugger.

        // Should we set ss to Empty
    }
    EX_END_CATCH(SwallowAllExceptions);
#endif
}

void TypeString::AppendTypeDebug(SString& ss, TypeHandle t)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        PRECONDITION(CheckPointer(t));
        PRECONDITION(ss.Check());
    }
    CONTRACTL_END

#ifndef DACCESS_COMPILE
    {
        EX_TRY
        {
            AppendType(ss, t, FormatNamespace | FormatDebug);
        }
        EX_CATCH
        {
            // This function is only used as diagnostic aid in debug builds.
            // If we run out of memory or hit some other problem,
            // tough luck for the debugger.
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
#endif
}

void TypeString::AppendTypeKeyDebug(SString& ss, TypeKey *pTypeKey)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
        PRECONDITION(CheckPointer(pTypeKey));
        PRECONDITION(ss.Check());
    }
    CONTRACTL_END

#ifndef DACCESS_COMPILE
    {
        EX_TRY
        {
            AppendTypeKey(ss, pTypeKey, FormatNamespace | FormatDebug);
        }
        EX_CATCH
        {
            // This function is only used as diagnostic aid in debug builds.
            // If we run out of memory or hit some other problem,
            // tough luck for the debugger.
        }
        EX_END_CATCH(SwallowAllExceptions);
    }
#endif
}

#endif // _DEBUG


void TypeString::AppendTypeKey(TypeNameBuilder& tnb, TypeKey *pTypeKey, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        THROWS;
        if (format & (FormatAssembly|FormatFullInst)) GC_TRIGGERS; else GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pTypeKey));
    }
    CONTRACT_END

    Module *pModule = NULL;

    // It's an array, with format
    //   element_ty[] (1-d, SZARRAY)
    //   element_ty[*] (1-d, ARRAY)
    //   element_ty[,] (2-d, ARRAY) etc
    // or a pointer (*) or byref (&)
    CorElementType kind = pTypeKey->GetKind();
    if (CorTypeInfo::IsModifier(kind))
    {
        DWORD rank = 0;
        TypeHandle elemType = pTypeKey->GetElementType();
        if (CorTypeInfo::IsArray(kind))
        {
            rank = pTypeKey->GetRank();
        }

        AppendType(tnb, elemType, Instantiation(), format);
        AppendParamTypeQualifier(tnb, kind, rank);
        pModule = elemType.GetModule();
    }
    else if (kind == ELEMENT_TYPE_VALUETYPE)
    {
        tnb.Append(W("VALUETYPE"));
        TypeHandle elemType = pTypeKey->GetElementType();
        AppendType(tnb, elemType, Instantiation(), format);
        pModule = elemType.GetModule();
    }
    else if (kind == ELEMENT_TYPE_FNPTR)
    {
        RETURN;
    }

    // ...otherwise it's just a plain type def or an instantiated type
    else
    {
        // Get the TypeDef token and attributes
        pModule = pTypeKey->GetModule();
        if (pModule != NULL)
        {
            IMDInternalImport *pImport = pModule->GetMDImport();
            mdTypeDef td = pTypeKey->GetTypeToken();
            _ASSERTE(!IsNilToken(td));

            AppendNestedTypeDef(tnb, pImport, td, format);

            // Append the instantiation
            if ((format & (FormatNamespace|FormatAssembly)) && pTypeKey->HasInstantiation())
                AppendInst(tnb, pTypeKey->GetInstantiation(), format);
        }

    }

    // Now append the assembly
    if (pModule != NULL && (format & FormatAssembly))
    {
        Assembly* pAssembly = pModule->GetAssembly();
        _ASSERTE(pAssembly != NULL);

        StackSString pAssemblyName;
#ifdef DACCESS_COMPILE
        pAssemblyName.SetUTF8(pAssembly->GetSimpleName());
#else
        pAssembly->GetDisplayName(pAssemblyName,
                                  ASM_DISPLAYF_PUBLIC_KEY_TOKEN | ASM_DISPLAYF_CONTENT_TYPE |
                                  (format & FormatNoVersion ? 0 : ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE));
#endif
        tnb.AddAssemblySpec(pAssemblyName.GetUnicode());
    }

    RETURN;
}

void TypeString::AppendTypeKey(SString& ss, TypeKey *pTypeKey, DWORD format)
{
    CONTRACT_VOID
    {
        MODE_ANY;
        if (format & (FormatAssembly|FormatFullInst)) GC_TRIGGERS; else GC_NOTRIGGER;
        THROWS;
        PRECONDITION(CheckPointer(pTypeKey));
    }
    CONTRACT_END

    {
        TypeNameBuilder tnb(&ss);
        AppendTypeKey(tnb, pTypeKey, format);
    }

    RETURN;
}

/*static*/
void TypeString::EscapeSimpleTypeName(SString* ssTypeName, SString* ssEscapedTypeName)
{
    SString::Iterator itr = ssTypeName->Begin();
    WCHAR c;
    while ((c = *itr++) != W('\0'))
    {
        if (IsTypeNameReservedChar(c))
            ssEscapedTypeName->Append(W("\\"));

        ssEscapedTypeName->Append(c);
    }
}

/*static*/
bool TypeString::ContainsReservedChar(LPCWSTR pTypeName)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    WCHAR c;
    while ((c = * pTypeName++) != W('\0'))
    {
        if (IsTypeNameReservedChar(c))
        {
            return true;
        }
    }

    return false;
}
