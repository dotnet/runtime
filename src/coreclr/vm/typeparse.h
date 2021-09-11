// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ---------------------------------------------------------------------------
// typeparse.h
// ---------------------------------------------------------------------------
//

//


#ifndef TYPEPARSE_H
#define TYPEPARSE_H

#include "common.h"
#include "class.h"
#include "typehandle.h"

//#define TYPE_NAME_RESERVED_CHAR W(",[]&*+\\")

bool inline IsTypeNameReservedChar(WCHAR ch)
{
    LIMITED_METHOD_CONTRACT;

    switch (ch)
    {
    case W(','):
    case W('['):
    case W(']'):
    case W('&'):
    case W('*'):
    case W('+'):
    case W('\\'):
        return true;

    default:
        return false;
    }
}


DomainAssembly * LoadDomainAssembly(
    SString *  psszAssemblySpec,
    Assembly * pRequestingAssembly,
    AssemblyBinder * pBinder,
    BOOL       bThrowIfNotFound);

class TypeName
{
private:
    template<typename PRODUCT>
    class Factory
    {
    public:
        const static DWORD MAX_PRODUCT = 4;

    public:
        Factory() : m_cProduct(0), m_next(NULL) { LIMITED_METHOD_CONTRACT; }
        ~Factory()
        {
            CONTRACTL
            {
                NOTHROW;
            }
            CONTRACTL_END;

            if (m_next)
                delete m_next;
          }

        PRODUCT* Create()
            { WRAPPER_NO_CONTRACT; if (m_cProduct == (INT32)MAX_PRODUCT) return GetNext()->Create(); return &m_product[m_cProduct++]; }

    private:
        Factory* GetNext() { if (!m_next) m_next = new Factory<PRODUCT>(); return m_next; }

    private:
        PRODUCT m_product[MAX_PRODUCT];
        INT32 m_cProduct;
        Factory* m_next;
    };
    friend class TypeName::Factory<TypeName>;
    friend class TypeNameBuilder;

private:
    class TypeNameParser
    {
        TypeNameParser(LPCWSTR szTypeName, TypeName* pTypeName)
        {
            CONTRACTL
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_ANY;
            }
            CONTRACTL_END;

            if (szTypeName == NULL)
            {
                szTypeName = W("");
            }

            m_currentToken = TypeNameEmpty;
            m_nextToken = TypeNameEmpty;

            m_pTypeName = pTypeName;
            m_sszTypeName = szTypeName;
            m_currentItr = m_itr = m_sszTypeName;
        }

    private:
        friend class TypeName;

    private:
        typedef enum {
            //
            // TOKENS
            //
            TypeNameEmpty               = 0x8000,
            TypeNameIdentifier          = 0x0001,
            TypeNamePostIdentifier      = 0x0002,
            TypeNameOpenSqBracket       = 0x0004,
            TypeNameCloseSqBracket      = 0x0008,
            TypeNameComma               = 0x0010,
            TypeNamePlus                = 0x0020,
            TypeNameAstrix              = 0x0040,
            TypeNameAmpersand           = 0x0080,
            TypeNameBackSlash           = 0x0100,
            TypeNameEnd                 = 0x4000,

            //
            // 1 TOKEN LOOK AHEAD
            //
            TypeNameNAME                = TypeNameIdentifier,
            TypeNameNESTNAME            = TypeNameIdentifier,
            TypeNameASSEMSPEC           = TypeNameIdentifier,
            TypeNameGENPARAM            = TypeNameOpenSqBracket | TypeNameEmpty,
            TypeNameFULLNAME            = TypeNameNAME,
            TypeNameAQN                 = TypeNameFULLNAME | TypeNameEnd,
            TypeNameASSEMBLYSPEC        = TypeNameIdentifier,
            TypeNameGENARG              = TypeNameOpenSqBracket | TypeNameFULLNAME,
            TypeNameGENARGS             = TypeNameGENARG,
            TypeNameEAQN                = TypeNameIdentifier,
            TypeNameEASSEMSPEC          = TypeNameIdentifier,
            TypeNameARRAY               = TypeNameOpenSqBracket,
            TypeNameQUALIFIER           = TypeNameAmpersand | TypeNameAstrix | TypeNameARRAY | TypeNameEmpty,
            TypeNameRANK                = TypeNameComma | TypeNameEmpty,
        } TypeNameTokens;

        typedef enum {
            TypeNameNone                = 0x00,
            TypeNameId                  = 0x01,
            TypeNameFusionName          = 0x02,
            TypeNameEmbeddedFusionName  = 0x03,
        } TypeNameIdentifiers;

    //
    // LEXIFIER
    //
    private:
        TypeNameTokens LexAToken(BOOL ignorePlus = FALSE);
        BOOL GetIdentifier(SString* sszId, TypeNameIdentifiers identiferType);
        void NextToken()  { WRAPPER_NO_CONTRACT; m_currentToken = m_nextToken; m_currentItr = m_itr; m_nextToken = LexAToken(); }
        BOOL NextTokenIs(TypeNameTokens token) { LIMITED_METHOD_CONTRACT; return !!(m_nextToken & token); }
        BOOL TokenIs(TypeNameTokens token) { LIMITED_METHOD_CONTRACT; return !!(m_currentToken & token); }
        BOOL TokenIs(int token) { LIMITED_METHOD_CONTRACT; return TokenIs((TypeNameTokens)token); }

    //
    // PRODUCTIONS
    //
    private:
        BOOL START();

        BOOL AQN();
        // /* empty */
        // FULLNAME ',' ASSEMSPEC
        // FULLNAME

        BOOL ASSEMSPEC();
        // fusionName

        BOOL FULLNAME();
        // NAME GENPARAMS QUALIFIER

        BOOL GENPARAMS();
        // *empty*
        // '[' GENARGS ']'

        BOOL GENARGS();
        // GENARG
        // GENARG ',' GENARGS

        BOOL GENARG();
        // '[' EAQN ']'
        // FULLNAME

        BOOL EAQN();
        // FULLNAME ',' EASSEMSPEC
        // FULLNAME

        BOOL EASSEMSPEC();
        // embededFusionName

        BOOL QUALIFIER();
        // *empty*
        // '&'
        // *' QUALIFIER
        // ARRAY QUALIFIER

        BOOL ARRAY();
        // '[' RANK ']'
        // '[' '*' ']'

        BOOL RANK(DWORD* pdwRank);
        // *empty*
        // ',' RANK

        BOOL NAME();
        // id
        // id '+' NESTNAME

        BOOL NESTNAME();
        // id
        // id '+' NESTNAME

    public:
        void Parse(DWORD* pError)
        {
            CONTRACTL
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_ANY;
            }
            CONTRACTL_END;

            *pError = (DWORD)-1;
            if (!START())
                *pError = (DWORD)(m_currentItr - m_sszTypeName) - 1;
        }

    private:
        TypeName* m_pTypeName;
        LPCWSTR m_sszTypeName;
        LPCWSTR m_itr;
        LPCWSTR m_currentItr;
        TypeNameTokens m_currentToken;
        TypeNameTokens m_nextToken;
    };
    friend class TypeName::TypeNameParser;

public:
    ULONG AddRef();
    ULONG Release();

public:
    TypeName(LPCWSTR szTypeName, DWORD* pError) : m_bIsGenericArgument(FALSE), m_count(0)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        TypeNameParser parser(szTypeName, this);
        parser.Parse(pError);
    }

    virtual ~TypeName();

public:
    static void QCALLTYPE QCreateTypeNameParser (LPCWSTR wszTypeName, QCall::ObjectHandleOnStack pNames, BOOL throwOnError);
    static void QCALLTYPE QReleaseTypeNameParser(TypeName * pTypeName);
    static void QCALLTYPE QGetNames             (TypeName * pTypeName, QCall::ObjectHandleOnStack pNames);
    static void QCALLTYPE QGetTypeArguments     (TypeName * pTypeName, QCall::ObjectHandleOnStack pTypeArguments);
    static void QCALLTYPE QGetModifiers         (TypeName * pTypeName, QCall::ObjectHandleOnStack pModifiers);
    static void QCALLTYPE QGetAssemblyName      (TypeName * pTypeName, QCall::StringHandleOnStack pAssemblyName);

    //-------------------------------------------------------------------------------------------
    // Retrieves a type from an assembly. It requires the caller to know which assembly
    // the type is in.
    //-------------------------------------------------------------------------------------------
    static TypeHandle GetTypeFromAssembly(LPCWSTR szTypeName, Assembly *pAssembly, BOOL bThrowIfNotFound = TRUE);

    TypeHandle GetTypeFromAsm();

    //-------------------------------------------------------------------------------------------
    // Retrieves a type. Will assert if the name is not fully qualified.
    //-------------------------------------------------------------------------------------------
    static TypeHandle GetTypeFromAsmQualifiedName(LPCWSTR szFullyQualifiedName);


    //-------------------------------------------------------------------------------------------
    // This version is used for resolving types named in custom attributes such as those used
    // for interop. Thus, it follows a well-known multistage set of rules for determining which
    // assembly the type is in. It will also enforce that the requesting assembly has access
    // rights to the type being loaded.
    //
    // The search logic is:
    //
    //    if szTypeName is ASM-qualified, only that assembly will be searched.
    //    if szTypeName is not ASM-qualified, we will search for the types in the following order:
    //       - in pRequestingAssembly (if not NULL). pRequestingAssembly is the assembly that contained
    //         the custom attribute from which the typename was derived.
    //       - in CoreLib
    //       - raise an AssemblyResolveEvent() in the current appdomain
    //
    // pRequestingAssembly may be NULL. In that case, the "visibility" check will simply check that
    // the loaded type has public access.
    //
    //--------------------------------------------------------------------------------------------
    static TypeHandle GetTypeUsingCASearchRules(LPCUTF8 szTypeName, Assembly *pRequestingAssembly, BOOL *pfTypeNameWasQualified = NULL, BOOL bDoVisibilityChecks = TRUE);
    static TypeHandle GetTypeUsingCASearchRules(LPCWSTR szTypeName, Assembly *pRequestingAssembly, BOOL *pfTypeNameWasQualified = NULL, BOOL bDoVisibilityChecks = TRUE);


    //--------------------------------------------------------------------------------------------------------------
    // This everything-but-the-kitchen-sink version is what used to be called "GetType()". It exposes all the
    // funky knobs needed for implementing the specific requirements of the managed Type.GetType() apis and friends.
    // Really that knowledge shouldn't even be embedded in the TypeParse class at all but for now, we'll
    // settle for giving this entrypoint a really ugly name so that only the two FCALL's that really need it will call
    // it.
    //--------------------------------------------------------------------------------------------------------------
    static TypeHandle GetTypeManaged(
        LPCWSTR szTypeName,
        DomainAssembly* pAssemblyGetType,
        BOOL bThrowIfNotFound,
        BOOL bIgnoreCase,
        BOOL bProhibitAssemblyQualifiedName,
        Assembly* pRequestingAssembly,
        OBJECTREF *pKeepAlive,
        AssemblyBinder * pBinder = nullptr);


public:
    SString* GetAssembly() { WRAPPER_NO_CONTRACT; return &m_assembly; }

private:
    TypeName() : m_bIsGenericArgument(FALSE), m_count(0) { LIMITED_METHOD_CONTRACT; }
    TypeName* AddGenericArgument();

    SString* AddName()
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        return m_names.AppendEx(m_nestNameFactory.Create());
    }

    SArray<SString*>& GetNames() { WRAPPER_NO_CONTRACT; return m_names; }
    SArray<TypeName*>& GetGenericArguments() { WRAPPER_NO_CONTRACT; return m_genericArguments; }
    SArray<DWORD>& GetSignature() { WRAPPER_NO_CONTRACT; return m_signature; }
    void SetByRef() { WRAPPER_NO_CONTRACT; m_signature.Append(ELEMENT_TYPE_BYREF); }
    void SetPointer() { WRAPPER_NO_CONTRACT;  m_signature.Append(ELEMENT_TYPE_PTR); }
    void SetSzArray() { WRAPPER_NO_CONTRACT; m_signature.Append(ELEMENT_TYPE_SZARRAY); }

    void SetArray(DWORD rank)
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;
        m_signature.Append(ELEMENT_TYPE_ARRAY);
        m_signature.Append(rank);
    }

    SString* ToString(SString* pBuf, BOOL bAssemblySpec = FALSE, BOOL bSignature = FALSE, BOOL bGenericArguments = FALSE);

private:
    //----------------------------------------------------------------------------------------------------------------
    // This is the "uber" GetType() that all public GetType() funnels through. It's main job is to figure out which
    // Assembly to load the type from and then invoke GetTypeHaveAssembly.
    //
    // It's got a highly baroque interface partly for historical reasons and partly because it's the uber-function
    // for all of the possible GetTypes.
    //----------------------------------------------------------------------------------------------------------------
    TypeHandle GetTypeWorker(
        BOOL bThrowIfNotFound,
        BOOL bIgnoreCase,
        Assembly* pAssemblyGetType,

        BOOL fEnableCASearchRules,

        BOOL bProhibitAssemblyQualifiedName,

        Assembly* pRequestingAssembly,
        AssemblyBinder * pBinder,
        OBJECTREF *pKeepAlive);

    //----------------------------------------------------------------------------------------------------------------
    // These functions are the ones that actually loads the type once we've pinned down the Assembly it's in.
    //----------------------------------------------------------------------------------------------------------------
    TypeHandle GetTypeHaveAssembly(Assembly* pAssembly, BOOL bThrowIfNotFound, BOOL bIgnoreCase, OBJECTREF *pKeepAlive)
    {
        return GetTypeHaveAssemblyHelper(pAssembly, bThrowIfNotFound, bIgnoreCase, pKeepAlive, TRUE);
    }
    TypeHandle GetTypeHaveAssemblyHelper(Assembly* pAssembly, BOOL bThrowIfNotFound, BOOL bIgnoreCase, OBJECTREF *pKeepAlive, BOOL bRecurse);
    SAFEHANDLE GetSafeHandle();

private:
    BOOL m_bIsGenericArgument;
    DWORD m_count;
    InlineSArray<DWORD, 128> m_signature;
    InlineSArray<TypeName*, 16> m_genericArguments;
    InlineSArray<SString*, 16> m_names;
    InlineSString<128> m_assembly;
    Factory<InlineSString<128> > m_nestNameFactory;
};

#endif
