// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// typestring.cpp
// ---------------------------------------------------------------------------
//

//
// This module contains all helper functions required to produce
// string representations of types, with options to control the
// appearance of namespace and assembly information.  Its primary use
// is in reflection (Type.Name, Type.FullName, Type.ToString, etc) but
// over time it could replace the use of TypeHandle.GetName etc for
// diagnostic messages.
//
// ---------------------------------------------------------------------------


#ifndef TYPESTRING_H
#define TYPESTRING_H

#include "common.h"
#include "class.h"
#include "typehandle.h"
#include "sstring.h"
#include "typekey.h"
#include "typeparse.h"
#include "field.h"

class TypeString;

class TypeNameBuilder
{
private:
    friend class TypeString;
    HRESULT OpenGenericArguments();
    HRESULT CloseGenericArguments();
    HRESULT OpenGenericArgument();
    HRESULT CloseGenericArgument();
    HRESULT AddName(LPCWSTR szName);
    HRESULT AddName(LPCWSTR szName, LPCWSTR szNamespace);
    HRESULT AddNameNoEscaping(LPCWSTR szName);
    HRESULT AddPointer();
    HRESULT AddByRef();
    HRESULT AddSzArray();
    HRESULT AddArray(DWORD rank);
    HRESULT AddAssemblySpec(LPCWSTR szAssemblySpec);
    HRESULT Clear();

private:
    class Stack
    {
    public:
        Stack() : m_depth(0) { LIMITED_METHOD_CONTRACT; }

    public:
        COUNT_T Push(COUNT_T element) { WRAPPER_NO_CONTRACT; *m_stack.Append() = element; m_depth++; return Tos(); }
        COUNT_T Pop()
        {
            CONTRACTL
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_ANY;
                PRECONDITION(GetDepth() > 0);
            }
            CONTRACTL_END;

            COUNT_T tos = Tos();
            m_stack.Delete(m_stack.End() - 1);
            m_depth--;
            return tos;
        }
        COUNT_T Tos() { WRAPPER_NO_CONTRACT; return m_stack.End()[-1]; }
        void Clear() { WRAPPER_NO_CONTRACT; while(GetDepth()) Pop(); }
        COUNT_T GetDepth() { WRAPPER_NO_CONTRACT; return m_depth; }

    private:
        INT32 m_depth;
        InlineSArray<COUNT_T, 16> m_stack;
    };


public:
    typedef enum
    {
        ParseStateSTART         = 0x0001,
        ParseStateNAME          = 0x0004,
        ParseStateGENARGS       = 0x0008,
        ParseStatePTRARR        = 0x0010,
        ParseStateBYREF         = 0x0020,
        ParseStateASSEMSPEC     = 0x0080,
        ParseStateERROR         = 0x0100,
    }
    ParseState;

public:
    TypeNameBuilder(SString* pStr, ParseState parseState = ParseStateSTART);
    TypeNameBuilder() { WRAPPER_NO_CONTRACT; m_pStr = &m_str; Clear(); }
    void SetUseAngleBracketsForGenerics(BOOL value) { m_bUseAngleBracketsForGenerics = value; }
    void Append(LPCWSTR pStr) { WRAPPER_NO_CONTRACT; m_pStr->Append(pStr); }
    void Append(WCHAR c) { WRAPPER_NO_CONTRACT; m_pStr->Append(c); }
    void Append(LPCUTF8 pStr) { WRAPPER_NO_CONTRACT; m_pStr->AppendUTF8(pStr); }
    void Append(UTF8 c) { WRAPPER_NO_CONTRACT; m_pStr->AppendUTF8(c); }
    SString* GetString() { WRAPPER_NO_CONTRACT; return m_pStr; }

private:
    void EscapeName(LPCWSTR szName);
    void EscapeAssemblyName(LPCWSTR szName);
    void EscapeEmbeddedAssemblyName(LPCWSTR szName);
    BOOL CheckParseState(int validState) { WRAPPER_NO_CONTRACT; return ((int)m_parseState & validState) != 0; }
    //BOOL CheckParseState(int validState) { WRAPPER_NO_CONTRACT; ASSERT(((int)m_parseState & validState) != 0); return TRUE; }
    HRESULT Fail() { WRAPPER_NO_CONTRACT; m_parseState = ParseStateERROR; return E_FAIL; }
    void PushOpenGenericArgument();
    void PopOpenGenericArgument();

private:
    ParseState m_parseState;
    SString* m_pStr;
    InlineSString<256> m_str;
    DWORD m_instNesting;
    BOOL m_bFirstInstArg;
    BOOL m_bNestedName;
    BOOL m_bHasAssemblySpec;
    BOOL m_bUseAngleBracketsForGenerics;
    Stack m_stack;
};

// --------------------------------------------------------------------------
// This type can generate names for types. It is used by reflection methods
// like System.RuntimeType.RuntimeTypeCache.ConstructName
//

class TypeString
{
    // -----------------------------------------------------------------------
    // WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING WARNING
    // -----------------------------------------------------------------------
    // Do no change the formatting of these strings as they are used by
    // serialization, and it would break serialization backwards-compatibility.

public:

  typedef enum
  {
      FormatBasic         = 0x00000000, // Not a bitmask, simply the tersest flag settings possible
      FormatNamespace     = 0x00000001, // Include namespace and/or enclosing class names in type names
      FormatFullInst      = 0x00000002, // Include namespace and assembly in generic types (regardless of other flag settings)
      FormatAssembly      = 0x00000004, // Include assembly display name in type names
      FormatSignature     = 0x00000008, // Include signature in method names
      FormatNoVersion     = 0x00000010, // Suppress version and culture information in all assembly names
#ifdef _DEBUG
      FormatDebug         = 0x00000020, // For debug printing of types only
#endif
      FormatAngleBrackets = 0x00000040, // Whether generic types are C<T> or C[T]
      FormatStubInfo      = 0x00000080, // Include stub info like {unbox-stub}
      FormatGenericParam  = 0x00000100, // Use !name and !!name for generic type and method parameters
  }
  FormatFlags;

public:
    // Append the name of the type td to the string
    // The following flags in the FormatFlags argument are significant: FormatNamespace
    static void AppendTypeDef(SString& tnb, IMDInternalImport *pImport, mdTypeDef td, DWORD format = FormatNamespace);

    // Append a square-bracket-enclosed, comma-separated list of n type parameters in inst to the string s
    // and enclose each parameter in square brackets to disambiguate the commas
    // The following flags in the FormatFlags argument are significant: FormatNamespace FormatFullInst FormatAssembly FormatNoVersion
    static void AppendInst(SString& s, Instantiation inst, DWORD format = FormatNamespace);

    // Append a representation of the type t to the string s
    // The following flags in the FormatFlags argument are significant: FormatNamespace FormatFullInst FormatAssembly FormatNoVersion
    static void AppendType(SString& s, TypeHandle t, DWORD format = FormatNamespace);

    // Append a representation of the type t to the string s, using the generic
    // instantiation info provided, instead of the instantiation in the TypeHandle.
    static void AppendType(SString& s, TypeHandle t, Instantiation typeInstantiation, DWORD format = FormatNamespace);

    static void AppendTypeKey(SString& s, TypeKey *pTypeKey, DWORD format = FormatNamespace);

    // Appends the method name and generic instantiation info.  This might
    // look like "Namespace.ClassName[T].Foo[U, V]()"
    static void AppendMethod(SString& s, MethodDesc *pMD, Instantiation typeInstantiation, const DWORD format = FormatNamespace|FormatSignature);

    // Append a representation of the method m to the string s
    // The following flags in the FormatFlags argument are significant: FormatNamespace FormatFullInst FormatAssembly FormatSignature  FormatNoVersion
    static void AppendMethodInternal(SString& s, MethodDesc *pMD, const DWORD format = FormatNamespace|FormatSignature|FormatStubInfo);

    // Append the field name and generic instantiation info.
    static void AppendField(SString& s, FieldDesc *pFD, Instantiation typeInstantiation, const DWORD format = FormatNamespace);
#ifdef _DEBUG
    // These versions are NOTHROWS. They are meant for diagnostic purposes only
    // as they may leave "s" in a bad state if there are any problems/exceptions.
    static void AppendMethodDebug(SString& s, MethodDesc *pMD);
    static void AppendTypeDebug(SString& s, TypeHandle t);
    static void AppendTypeKeyDebug(SString& s, TypeKey* pTypeKey);
#endif

private:
    friend class TypeNameBuilder;
    static void AppendMethodImpl(SString& s, MethodDesc *pMD, Instantiation typeInstantiation, const DWORD format);
    static void AppendTypeDef(TypeNameBuilder& tnb, IMDInternalImport *pImport, mdTypeDef td, DWORD format = FormatNamespace);
    static void AppendNestedTypeDef(TypeNameBuilder& tnb, IMDInternalImport *pImport, mdTypeDef td, DWORD format = FormatNamespace);
    static void AppendInst(TypeNameBuilder& tnb, Instantiation inst, DWORD format = FormatNamespace);
    static void AppendType(TypeNameBuilder& tnb, TypeHandle t, Instantiation typeInstantiation, DWORD format = FormatNamespace); // ????
    static void AppendTypeKey(TypeNameBuilder& tnb, TypeKey *pTypeKey, DWORD format = FormatNamespace);
    static void AppendParamTypeQualifier(TypeNameBuilder& tnb, CorElementType kind, DWORD rank);
    static void EscapeSimpleTypeName(SString* ssTypeName, SString* ssEscapedTypeName);
    static bool ContainsReservedChar(LPCWSTR pTypeName);
};

#endif
