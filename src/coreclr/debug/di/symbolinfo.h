// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// callbacks for diasymreader when using SymConverter


#ifndef SYMBOLINFO_H
#define SYMBOLINFO_H
#include "arraylist.h"
#include "shash.h"
#include "corsym.h"
#include "sstring.h"

class SymbolInfo:  IMetaDataEmit,  IMetaDataImport
{

    struct ScopeMap
    {
        ULONG32 left;
        ULONG32 right;
        ScopeMap(ULONG32 l, ULONG32 r):left(l), right(r){};
        ULONG32 GetKey() {return left;};
        static COUNT_T Hash(ULONG32 key) {return key;};
    };

    struct MethodProps
    {
        mdMethodDef method;
        mdTypeDef cls;
        SString wszName;
        MethodProps() : method(mdTokenNil)  {};
    };

    struct ClassProps
    {
        mdTypeDef cls;
        DWORD flags;
        SString wszName;
        mdTypeDef tkEnclosing;
        ClassProps(mdToken c, DWORD f, LPCWSTR name, mdToken tkE):
            cls(c),flags(f),wszName(name),tkEnclosing(tkE) {wszName.Normalize();};

        mdTypeDef GetKey() {return cls;};
        static COUNT_T Hash(mdTypeDef key) {return key;};
    };

    struct SignatureProps
    {
        SBuffer sig;
        DWORD  tkSig;
        SignatureProps(SBuffer& signature, DWORD token):
            tkSig(token) {sig.Set(signature);};
        SBuffer& GetKey() {return sig;};
        static COUNT_T Hash(SBuffer& key) {return HashBytes(key,key.GetSize());};
    };

    Volatile<LONG> m_cRef;
    ArrayList m_Documents;
    PtrSHashWithCleanup<ScopeMap,ULONG32> m_Scopes;
    PtrSHashWithCleanup<SignatureProps,SBuffer&> m_Signatures;
    PtrSHashWithCleanup<ClassProps,mdTypeDef> m_Classes;

    MethodProps m_LastMethod; // diasymreader supports only one open method at a time

    ClassProps* FindClass(mdToken cls);
    SignatureProps* FindSignature(SBuffer& sig);

    virtual ~SymbolInfo(); // protected, b/c the lifetime is controlled by refcount

public:
    SymbolInfo();

    HRESULT AddDocument(DWORD id, ISymUnmanagedDocumentWriter* Document);
    HRESULT MapDocument(DWORD id, ISymUnmanagedDocumentWriter** Document);
    HRESULT AddScope(ULONG32 left, ULONG32 right);
    HRESULT MapScope(ULONG32 left, ULONG32* pRight);
    HRESULT SetMethodProps(mdToken method, mdTypeDef cls, LPCWSTR wszName);
    HRESULT SetClassProps(mdTypeDef cls, DWORD flags, LPCWSTR wszName, mdTypeDef enclosingCls);
    HRESULT AddSignature(SBuffer& sig, mdSignature token);

    // IUnknown methods
    STDMETHOD(QueryInterface) (REFIID riid, LPVOID * ppvObj);
    STDMETHOD_(ULONG,AddRef) ();
    STDMETHOD_(ULONG,Release) ();


    // IMetaDataImport functions

    STDMETHOD_(void, CloseEnum)(HCORENUM hEnum);
    STDMETHOD(CountEnum)(HCORENUM hEnum, ULONG *pulCount);
    STDMETHOD(ResetEnum)(HCORENUM hEnum, ULONG ulPos);
    STDMETHOD(EnumTypeDefs)(HCORENUM *phEnum, mdTypeDef rTypeDefs[],
                            ULONG cMax, ULONG *pcTypeDefs);
    STDMETHOD(EnumInterfaceImpls)(HCORENUM *phEnum, mdTypeDef td,
                            mdInterfaceImpl rImpls[], ULONG cMax,
                            ULONG* pcImpls);
    STDMETHOD(EnumTypeRefs)(HCORENUM *phEnum, mdTypeRef rTypeRefs[],
                            ULONG cMax, ULONG* pcTypeRefs);

    STDMETHOD(FindTypeDefByName)(           // S_OK or error.
        LPCWSTR     szTypeDef,              // [IN] Name of the Type.
        mdToken     tkEnclosingClass,       // [IN] TypeDef/TypeRef for Enclosing class.
        mdTypeDef   *ptd);             // [OUT] Put the TypeDef token here.

    STDMETHOD(GetScopeProps)(               // S_OK or error.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Put the name here.
        ULONG       cchName,                // [IN] Size of name buffer in wide chars.
        ULONG       *pchName,               // [OUT] Put size of name (wide chars) here.
        GUID        *pmvid);           // [OUT, OPTIONAL] Put MVID here.

    STDMETHOD(GetModuleFromScope)(          // S_OK.
        mdModule    *pmd);             // [OUT] Put mdModule token here.

    STDMETHOD(GetTypeDefProps)(             // S_OK or error.
        mdTypeDef   td,                     // [IN] TypeDef token for inquiry.
      _Out_writes_to_opt_(cchTypeDef, *pchTypeDef)
        LPWSTR      szTypeDef,              // [OUT] Put name here.
        ULONG       cchTypeDef,             // [IN] size of name buffer in wide chars.
        ULONG       *pchTypeDef,            // [OUT] put size of name (wide chars) here.
        DWORD       *pdwTypeDefFlags,       // [OUT] Put flags here.
        mdToken     *ptkExtends);      // [OUT] Put base class TypeDef/TypeRef here.

    STDMETHOD(GetInterfaceImplProps)(       // S_OK or error.
        mdInterfaceImpl iiImpl,             // [IN] InterfaceImpl token.
        mdTypeDef   *pClass,                // [OUT] Put implementing class token here.
        mdToken     *ptkIface);        // [OUT] Put implemented interface token here.

    STDMETHOD(GetTypeRefProps)(             // S_OK or error.
        mdTypeRef   tr,                     // [IN] TypeRef token.
        mdToken     *ptkResolutionScope,    // [OUT] Resolution scope, ModuleRef or AssemblyRef.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Name of the TypeRef.
        ULONG       cchName,                // [IN] Size of buffer.
        ULONG       *pchName);         // [OUT] Size of Name.

    STDMETHOD(ResolveTypeRef)(mdTypeRef tr, REFIID riid, IUnknown **ppIScope, mdTypeDef *ptd);

    STDMETHOD(EnumMembers)(                 // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(EnumMembersWithName)(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdToken     rMembers[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(EnumMethods)(                 // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdMethodDef rMethods[],             // [OUT] Put MethodDefs here.
        ULONG       cMax,                   // [IN] Max MethodDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(EnumMethodsWithName)(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdMethodDef rMethods[],             // [OU] Put MethodDefs here.
        ULONG       cMax,                   // [IN] Max MethodDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(EnumFields)(                  // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        mdFieldDef  rFields[],              // [OUT] Put FieldDefs here.
        ULONG       cMax,                   // [IN] Max FieldDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(EnumFieldsWithName)(          // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   cl,                     // [IN] TypeDef to scope the enumeration.
        LPCWSTR     szName,                 // [IN] Limit results to those with this name.
        mdFieldDef  rFields[],              // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.


    STDMETHOD(EnumParams)(                  // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration.
        mdParamDef  rParams[],              // [OUT] Put ParamDefs here.
        ULONG       cMax,                   // [IN] Max ParamDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(EnumMemberRefs)(              // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     tkParent,               // [IN] Parent token to scope the enumeration.
        mdMemberRef rMemberRefs[],          // [OUT] Put MemberRefs here.
        ULONG       cMax,                   // [IN] Max MemberRefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(EnumMethodImpls)(             // S_OK, S_FALSE, or error
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdToken     rMethodBody[],          // [OUT] Put Method Body tokens here.
        mdToken     rMethodDecl[],          // [OUT] Put Method Declaration tokens here.
        ULONG       cMax,                   // [IN] Max tokens to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(EnumPermissionSets)(          // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     tk,                     // [IN] if !NIL, token to scope the enumeration.
        DWORD       dwActions,              // [IN] if !0, return only these actions.
        mdPermission rPermission[],         // [OUT] Put Permissions here.
        ULONG       cMax,                   // [IN] Max Permissions to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(FindMember)(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdToken     *pmb);             // [OUT] matching memberdef

    STDMETHOD(FindMethod)(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMethodDef *pmb);             // [OUT] matching memberdef

    STDMETHOD(FindField)(
        mdTypeDef   td,                     // [IN] given typedef
        LPCWSTR     szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdFieldDef  *pmb);             // [OUT] matching memberdef

    STDMETHOD(FindMemberRef)(
        mdTypeRef   td,                     // [IN] given typeRef
        LPCWSTR     szName,                 // [IN] member name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMemberRef *pmr);             // [OUT] matching memberref

    STDMETHOD (GetMethodProps)(
        mdMethodDef mb,                     // The method for which to get props.
        mdTypeDef   *pClass,                // Put method's class here.
      _Out_writes_to_opt_(cchMethod, *pchMethod)
        LPWSTR      szMethod,               // Put method's name here.
        ULONG       cchMethod,              // Size of szMethod buffer in wide chars.
        ULONG       *pchMethod,             // Put actual size here
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        ULONG       *pulCodeRVA,            // [OUT] codeRVA
        DWORD       *pdwImplFlags);    // [OUT] Impl. Flags

    STDMETHOD(GetMemberRefProps)(           // S_OK or error.
        mdMemberRef mr,                     // [IN] given memberref
        mdToken     *ptk,                   // [OUT] Put classref or classdef here.
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,               // [OUT] buffer to fill for member's name
        ULONG       cchMember,              // [IN] the count of char of szMember
        ULONG       *pchMember,             // [OUT] actual count of char in member name
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to meta data blob value
        ULONG       *pbSig);           // [OUT] actual size of signature blob

    STDMETHOD(EnumProperties)(              // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdProperty  rProperties[],          // [OUT] Put Properties here.
        ULONG       cMax,                   // [IN] Max properties to put.
        ULONG       *pcProperties);    // [OUT] Put # put here.

    STDMETHOD(EnumEvents)(                  // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdTypeDef   td,                     // [IN] TypeDef to scope the enumeration.
        mdEvent     rEvents[],              // [OUT] Put events here.
        ULONG       cMax,                   // [IN] Max events to put.
        ULONG       *pcEvents);        // [OUT] Put # put here.

    STDMETHOD(GetEventProps)(               // S_OK, S_FALSE, or error.
        mdEvent     ev,                     // [IN] event token
        mdTypeDef   *pClass,                // [OUT] typedef containing the event declarion.
        LPCWSTR     szEvent,                // [OUT] Event name
        ULONG       cchEvent,               // [IN] the count of wchar of szEvent
        ULONG       *pchEvent,              // [OUT] actual count of wchar for event's name
        DWORD       *pdwEventFlags,         // [OUT] Event flags.
        mdToken     *ptkEventType,          // [OUT] EventType class
        mdMethodDef *pmdAddOn,              // [OUT] AddOn method of the event
        mdMethodDef *pmdRemoveOn,           // [OUT] RemoveOn method of the event
        mdMethodDef *pmdFire,               // [OUT] Fire method of the event
        mdMethodDef rmdOtherMethod[],       // [OUT] other method of the event
        ULONG       cMax,                   // [IN] size of rmdOtherMethod
        ULONG       *pcOtherMethod);   // [OUT] total number of other method of this event

    STDMETHOD(EnumMethodSemantics)(         // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdMethodDef mb,                     // [IN] MethodDef to scope the enumeration.
        mdToken     rEventProp[],           // [OUT] Put Event/Property here.
        ULONG       cMax,                   // [IN] Max properties to put.
        ULONG       *pcEventProp);     // [OUT] Put # put here.

    STDMETHOD(GetMethodSemantics)(          // S_OK, S_FALSE, or error.
        mdMethodDef mb,                     // [IN] method token
        mdToken     tkEventProp,            // [IN] event/property token.
        DWORD       *pdwSemanticsFlags); // [OUT] the role flags for the method/propevent pair

    STDMETHOD(GetClassLayout) (
        mdTypeDef   td,                     // [IN] give typedef
        DWORD       *pdwPackSize,           // [OUT] 1, 2, 4, 8, or 16
        COR_FIELD_OFFSET rFieldOffset[],    // [OUT] field offset array
        ULONG       cMax,                   // [IN] size of the array
        ULONG       *pcFieldOffset,         // [OUT] needed array size
        ULONG       *pulClassSize);        // [OUT] the size of the class

    STDMETHOD(GetFieldMarshal) (
        mdToken     tk,                     // [IN] given a field's memberdef
        PCCOR_SIGNATURE *ppvNativeType,     // [OUT] native type of this field
        ULONG       *pcbNativeType);   // [OUT] the count of bytes of *ppvNativeType

    STDMETHOD(GetRVA)(                      // S_OK or error.
        mdToken     tk,                     // Member for which to set offset
        ULONG       *pulCodeRVA,            // The offset
        DWORD       *pdwImplFlags);    // the implementation flags

    STDMETHOD(GetPermissionSetProps) (
        mdPermission pm,                    // [IN] the permission token.
        DWORD       *pdwAction,             // [OUT] CorDeclSecurity.
        void const  **ppvPermission,        // [OUT] permission blob.
        ULONG       *pcbPermission);   // [OUT] count of bytes of pvPermission.

    STDMETHOD(GetSigFromToken)(             // S_OK or error.
        mdSignature mdSig,                  // [IN] Signature token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to token.
        ULONG       *pcbSig);          // [OUT] return size of signature.

    STDMETHOD(GetModuleRefProps)(           // S_OK or error.
        mdModuleRef mur,                    // [IN] moduleref token.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] buffer to fill with the moduleref name.
        ULONG       cchName,                // [IN] size of szName in wide characters.
        ULONG       *pchName);         // [OUT] actual count of characters in the name.

    STDMETHOD(EnumModuleRefs)(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdModuleRef rModuleRefs[],          // [OUT] put modulerefs here.
        ULONG       cmax,                   // [IN] max memberrefs to put.
        ULONG       *pcModuleRefs);    // [OUT] put # put here.

    STDMETHOD(GetTypeSpecFromToken)(        // S_OK or error.
        mdTypeSpec typespec,                // [IN] TypeSpec token.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] return pointer to TypeSpec signature
        ULONG       *pcbSig);          // [OUT] return size of signature.

    STDMETHOD(GetNameFromToken)(            // Not Recommended! May be removed!
        mdToken     tk,                     // [IN] Token to get name from.  Must have a name.
        MDUTF8CSTR  *pszUtf8NamePtr);  // [OUT] Return pointer to UTF8 name in heap.

    STDMETHOD(EnumUnresolvedMethods)(       // S_OK, S_FALSE, or error.
        HCORENUM    *phEnum,                // [IN|OUT] Pointer to the enum.
        mdToken     rMethods[],             // [OUT] Put MemberDefs here.
        ULONG       cMax,                   // [IN] Max MemberDefs to put.
        ULONG       *pcTokens);        // [OUT] Put # put here.

    STDMETHOD(GetUserString)(               // S_OK or error.
        mdString    stk,                    // [IN] String token.
      _Out_writes_to_opt_(cchString, *pchString)
        LPWSTR      szString,               // [OUT] Copy of string.
        ULONG       cchString,              // [IN] Max chars of room in szString.
        ULONG       *pchString);       // [OUT] How many chars in actual string.

    STDMETHOD(GetPinvokeMap)(               // S_OK or error.
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       *pdwMappingFlags,       // [OUT] Flags used for mapping.
      _Out_writes_to_opt_(cchImportName, *pchImportName)
        LPWSTR      szImportName,           // [OUT] Import name.
        ULONG       cchImportName,          // [IN] Size of the name buffer.
        ULONG       *pchImportName,         // [OUT] Actual number of characters stored.
        mdModuleRef *pmrImportDLL);    // [OUT] ModuleRef token for the target DLL.

    STDMETHOD(EnumSignatures)(              // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdSignature rSignatures[],          // [OUT] put signatures here.
        ULONG       cmax,                   // [IN] max signatures to put.
        ULONG       *pcSignatures);    // [OUT] put # put here.

    STDMETHOD(EnumTypeSpecs)(               // S_OK or error.
        HCORENUM    *phEnum,                // [IN|OUT] pointer to the enum.
        mdTypeSpec  rTypeSpecs[],           // [OUT] put TypeSpecs here.
        ULONG       cmax,                   // [IN] max TypeSpecs to put.
        ULONG       *pcTypeSpecs);     // [OUT] put # put here.

    STDMETHOD(EnumUserStrings)(             // S_OK or error.
        HCORENUM    *phEnum,                // [IN/OUT] pointer to the enum.
        mdString    rStrings[],             // [OUT] put Strings here.
        ULONG       cmax,                   // [IN] max Strings to put.
        ULONG       *pcStrings);       // [OUT] put # put here.

    STDMETHOD(GetParamForMethodIndex)(      // S_OK or error.
        mdMethodDef md,                     // [IN] Method token.
        ULONG       ulParamSeq,             // [IN] Parameter sequence.
        mdParamDef  *ppd);             // [IN] Put Param token here.

    STDMETHOD(EnumCustomAttributes)(        // S_OK or error.
        HCORENUM    *phEnum,                // [IN, OUT] COR enumerator.
        mdToken     tk,                     // [IN] Token to scope the enumeration, 0 for all.
        mdToken     tkType,                 // [IN] Type of interest, 0 for all.
        mdCustomAttribute rCustomAttributes[], // [OUT] Put custom attribute tokens here.
        ULONG       cMax,                   // [IN] Size of rCustomAttributes.
        ULONG       *pcCustomAttributes);  // [OUT, OPTIONAL] Put count of token values here.

    STDMETHOD(GetCustomAttributeProps)(     // S_OK or error.
        mdCustomAttribute cv,               // [IN] CustomAttribute token.
        mdToken     *ptkObj,                // [OUT, OPTIONAL] Put object token here.
        mdToken     *ptkType,               // [OUT, OPTIONAL] Put AttrType token here.
        void const  **ppBlob,               // [OUT, OPTIONAL] Put pointer to data here.
        ULONG       *pcbSize);         // [OUT, OPTIONAL] Put size of date here.

    STDMETHOD(FindTypeRef)(
        mdToken     tkResolutionScope,      // [IN] ModuleRef, AssemblyRef or TypeRef.
        LPCWSTR     szName,                 // [IN] TypeRef Name.
        mdTypeRef   *ptr);             // [OUT] matching TypeRef.

    STDMETHOD(GetMemberProps)(
        mdToken     mb,                     // The member for which to get props.
        mdTypeDef   *pClass,                // Put member's class here.
      _Out_writes_to_opt_(cchMember, *pchMember)
        LPWSTR      szMember,               // Put member's name here.
        ULONG       cchMember,              // Size of szMember buffer in wide chars.
        ULONG       *pchMember,             // Put actual size here
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        ULONG       *pulCodeRVA,            // [OUT] codeRVA
        DWORD       *pdwImplFlags,          // [OUT] Impl. Flags
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppValue,             // [OUT] constant value
        ULONG       *pcchValue);       // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetFieldProps)(
        mdFieldDef  mb,                     // The field for which to get props.
        mdTypeDef   *pClass,                // Put field's class here.
      _Out_writes_to_opt_(cchField, *pchField)
        LPWSTR      szField,                // Put field's name here.
        ULONG       cchField,               // Size of szField buffer in wide chars.
        ULONG       *pchField,              // Put actual size here
        DWORD       *pdwAttr,               // Put flags here.
        PCCOR_SIGNATURE *ppvSigBlob,        // [OUT] point to the blob value of meta data
        ULONG       *pcbSigBlob,            // [OUT] actual size of signature blob
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppValue,             // [OUT] constant value
        ULONG       *pcchValue);       // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetPropertyProps)(            // S_OK, S_FALSE, or error.
        mdProperty  prop,                   // [IN] property token
        mdTypeDef   *pClass,                // [OUT] typedef containing the property declarion.
        LPCWSTR     szProperty,             // [OUT] Property name
        ULONG       cchProperty,            // [IN] the count of wchar of szProperty
        ULONG       *pchProperty,           // [OUT] actual count of wchar for property name
        DWORD       *pdwPropFlags,          // [OUT] property flags.
        PCCOR_SIGNATURE *ppvSig,            // [OUT] property type. pointing to meta data internal blob
        ULONG       *pbSig,                 // [OUT] count of bytes in *ppvSig
        DWORD       *pdwCPlusTypeFlag,      // [OUT] flag for value type. selected ELEMENT_TYPE_*
        UVCP_CONSTANT *ppDefaultValue,      // [OUT] constant value
        ULONG       *pcchDefaultValue,      // [OUT] size of constant string in chars, 0 for non-strings.
        mdMethodDef *pmdSetter,             // [OUT] setter method of the property
        mdMethodDef *pmdGetter,             // [OUT] getter method of the property
        mdMethodDef rmdOtherMethod[],       // [OUT] other method of the property
        ULONG       cMax,                   // [IN] size of rmdOtherMethod
        ULONG       *pcOtherMethod);   // [OUT] total number of other method of this property

    STDMETHOD(GetParamProps)(               // S_OK or error.
        mdParamDef  tk,                     // [IN]The Parameter.
        mdMethodDef *pmd,                   // [OUT] Parent Method token.
        ULONG       *pulSequence,           // [OUT] Parameter sequence.
      _Out_writes_to_opt_(cchName, *pchName)
        LPWSTR      szName,                 // [OUT] Put name here.
        ULONG       cchName,                // [OUT] Size of name buffer.
        ULONG       *pchName,               // [OUT] Put actual size of name here.
        DWORD       *pdwAttr,               // [OUT] Put flags here.
        DWORD       *pdwCPlusTypeFlag,      // [OUT] Flag for value type. selected ELEMENT_TYPE_*.
        UVCP_CONSTANT *ppValue,             // [OUT] Constant value.
        ULONG       *pcchValue);       // [OUT] size of constant string in chars, 0 for non-strings.

    STDMETHOD(GetCustomAttributeByName)(    // S_OK or error.
        mdToken     tkObj,                  // [IN] Object with Custom Attribute.
        LPCWSTR     szName,                 // [IN] Name of desired Custom Attribute.
        const void  **ppData,               // [OUT] Put pointer to data here.
        ULONG       *pcbData);         // [OUT] Put size of data here.

    STDMETHOD_(BOOL, IsValidToken)(         // True or False.
        mdToken     tk);               // [IN] Given token.

    STDMETHOD(GetNestedClassProps)(         // S_OK or error.
        mdTypeDef   tdNestedClass,          // [IN] NestedClass token.
        mdTypeDef   *ptdEnclosingClass); // [OUT] EnclosingClass token.

    STDMETHOD(GetNativeCallConvFromSig)(    // S_OK or error.
        void const  *pvSig,                 // [IN] Pointer to signature.
        ULONG       cbSig,                  // [IN] Count of signature bytes.
        ULONG       *pCallConv);       // [OUT] Put calling conv here (see CorPinvokemap).

    STDMETHOD(IsGlobal)(                    // S_OK or error.
        mdToken     pd,                     // [IN] Type, Field, or Method token.
        int         *pbGlobal);        // [OUT] Put 1 if global, 0 otherwise.


    // IMetaDataEmit functions

    STDMETHOD(SetModuleProps)(              // S_OK or error.
        LPCWSTR     szName);           // [IN] If not NULL, the name of the module to set.

    STDMETHOD(Save)(                        // S_OK or error.
        LPCWSTR     szFile,                 // [IN] The filename to save to.
        DWORD       dwSaveFlags);      // [IN] Flags for the save.

    STDMETHOD(SaveToStream)(                // S_OK or error.
        IStream     *pIStream,              // [IN] A writable stream to save to.
        DWORD       dwSaveFlags);      // [IN] Flags for the save.

    STDMETHOD(GetSaveSize)(                 // S_OK or error.
        CorSaveSize fSave,                  // [IN] cssAccurate or cssQuick.
        DWORD       *pdwSaveSize);     // [OUT] Put the size here.

    STDMETHOD(DefineTypeDef)(               // S_OK or error.
        LPCWSTR     szTypeDef,              // [IN] Name of TypeDef
        DWORD       dwTypeDefFlags,         // [IN] CustomAttribute flags
        mdToken     tkExtends,              // [IN] extends this TypeDef or typeref
        mdToken     rtkImplements[],        // [IN] Implements interfaces
        mdTypeDef   *ptd);             // [OUT] Put TypeDef token here

    STDMETHOD(DefineNestedType)(            // S_OK or error.
        LPCWSTR     szTypeDef,              // [IN] Name of TypeDef
        DWORD       dwTypeDefFlags,         // [IN] CustomAttribute flags
        mdToken     tkExtends,              // [IN] extends this TypeDef or typeref
        mdToken     rtkImplements[],        // [IN] Implements interfaces
        mdTypeDef   tdEncloser,             // [IN] TypeDef token of the enclosing type.
        mdTypeDef   *ptd);             // [OUT] Put TypeDef token here

    STDMETHOD(SetHandler)(                  // S_OK.
        IUnknown    *pUnk);            // [IN] The new error handler.

    STDMETHOD(DefineMethod)(                // S_OK or error.
        mdTypeDef   td,                     // Parent TypeDef
        LPCWSTR     szName,                 // Name of member
        DWORD       dwMethodFlags,          // Member attributes
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        ULONG       ulCodeRVA,
        DWORD       dwImplFlags,
        mdMethodDef *pmd);             // Put member token here

    STDMETHOD(DefineMethodImpl)(            // S_OK or error.
        mdTypeDef   td,                     // [IN] The class implementing the method
        mdToken     tkBody,                 // [IN] Method body - MethodDef or MethodRef
        mdToken     tkDecl);           // [IN] Method declaration - MethodDef or MethodRef

    STDMETHOD(DefineTypeRefByName)(         // S_OK or error.
        mdToken     tkResolutionScope,      // [IN] ModuleRef, AssemblyRef or TypeRef.
        LPCWSTR     szName,                 // [IN] Name of the TypeRef.
        mdTypeRef   *ptr);             // [OUT] Put TypeRef token here.

    STDMETHOD(DefineImportType)(            // S_OK or error.
        IMetaDataAssemblyImport *pAssemImport,  // [IN] Assembly containing the TypeDef.
        const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
        ULONG       cbHashValue,            // [IN] Count of bytes.
        IMetaDataImport *pImport,           // [IN] Scope containing the TypeDef.
        mdTypeDef   tdImport,               // [IN] The imported TypeDef.
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] Assembly into which the TypeDef is imported.
        mdTypeRef   *ptr);             // [OUT] Put TypeRef token here.

    STDMETHOD(DefineMemberRef)(             // S_OK or error
        mdToken     tkImport,               // [IN] ClassRef or ClassDef importing a member.
        LPCWSTR     szName,                 // [IN] member's name
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        mdMemberRef *pmr);             // [OUT] memberref token

    STDMETHOD(DefineImportMember)(          // S_OK or error.
        IMetaDataAssemblyImport *pAssemImport,  // [IN] Assembly containing the Member.
        const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
        ULONG       cbHashValue,            // [IN] Count of bytes.
        IMetaDataImport *pImport,           // [IN] Import scope, with member.
        mdToken     mbMember,               // [IN] Member in import scope.
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] Assembly into which the Member is imported.
        mdToken     tkParent,               // [IN] Classref or classdef in emit scope.
        mdMemberRef *pmr);             // [OUT] Put member ref here.

    STDMETHOD(DefineEvent) (
        mdTypeDef   td,                     // [IN] the class/interface on which the event is being defined
        LPCWSTR     szEvent,                // [IN] Name of the event
        DWORD       dwEventFlags,           // [IN] CorEventAttr
        mdToken     tkEventType,            // [IN] a reference (mdTypeRef or mdTypeRef) to the Event class
        mdMethodDef mdAddOn,                // [IN] required add method
        mdMethodDef mdRemoveOn,             // [IN] required remove method
        mdMethodDef mdFire,                 // [IN] optional fire method
        mdMethodDef rmdOtherMethods[],      // [IN] optional array of other methods associate with the event
        mdEvent     *pmdEvent);        // [OUT] output event token

    STDMETHOD(SetClassLayout) (
        mdTypeDef   td,                     // [IN] typedef
        DWORD       dwPackSize,             // [IN] packing size specified as 1, 2, 4, 8, or 16
        COR_FIELD_OFFSET rFieldOffsets[],   // [IN] array of layout specification
        ULONG       ulClassSize);      // [IN] size of the class

    STDMETHOD(DeleteClassLayout) (
        mdTypeDef   td);               // [IN] typedef whose layout is to be deleted.

    STDMETHOD(SetFieldMarshal) (
        mdToken     tk,                     // [IN] given a fieldDef or paramDef token
        PCCOR_SIGNATURE pvNativeType,       // [IN] native type specification
        ULONG       cbNativeType);     // [IN] count of bytes of pvNativeType

    STDMETHOD(DeleteFieldMarshal) (
        mdToken     tk);               // [IN] given a fieldDef or paramDef token

    STDMETHOD(DefinePermissionSet) (
        mdToken     tk,                     // [IN] the object to be decorated.
        DWORD       dwAction,               // [IN] CorDeclSecurity.
        void const  *pvPermission,          // [IN] permission blob.
        ULONG       cbPermission,           // [IN] count of bytes of pvPermission.
        mdPermission *ppm);            // [OUT] returned permission token.

    STDMETHOD(SetRVA)(                      // S_OK or error.
        mdMethodDef md,                     // [IN] Method for which to set offset
        ULONG       ulRVA);            // [IN] The offset

    STDMETHOD(GetTokenFromSig)(             // S_OK or error.
        PCCOR_SIGNATURE pvSig,              // [IN] Signature to define.
        ULONG       cbSig,                  // [IN] Size of signature data.
        mdSignature *pmsig);           // [OUT] returned signature token.

    STDMETHOD(DefineModuleRef)(             // S_OK or error.
        LPCWSTR     szName,                 // [IN] DLL name
        mdModuleRef *pmur);            // [OUT] returned

    // <TODO>This should go away once everyone starts using SetMemberRefProps.</TODO>
    STDMETHOD(SetParent)(                   // S_OK or error.
        mdMemberRef mr,                     // [IN] Token for the ref to be fixed up.
        mdToken     tk);               // [IN] The ref parent.

    STDMETHOD(GetTokenFromTypeSpec)(        // S_OK or error.
        PCCOR_SIGNATURE pvSig,              // [IN] TypeSpec Signature to define.
        ULONG       cbSig,                  // [IN] Size of signature data.
        mdTypeSpec *ptypespec);        // [OUT] returned TypeSpec token.

    STDMETHOD(SaveToMemory)(                // S_OK or error.
        void        *pbData,                // [OUT] Location to write data.
        ULONG       cbData);           // [IN] Max size of data buffer.

    STDMETHOD(DefineUserString)(            // Return code.
        LPCWSTR szString,                   // [IN] User literal string.
        ULONG       cchString,              // [IN] Length of string.
        mdString    *pstk);            // [OUT] String token.

    STDMETHOD(DeleteToken)(                 // Return code.
        mdToken     tkObj);            // [IN] The token to be deleted

    STDMETHOD(SetMethodProps)(              // S_OK or error.
        mdMethodDef md,                     // [IN] The MethodDef.
        DWORD       dwMethodFlags,          // [IN] Method attributes.
        ULONG       ulCodeRVA,              // [IN] Code RVA.
        DWORD       dwImplFlags);      // [IN] Impl flags.

    STDMETHOD(SetTypeDefProps)(             // S_OK or error.
        mdTypeDef   td,                     // [IN] The TypeDef.
        DWORD       dwTypeDefFlags,         // [IN] TypeDef flags.
        mdToken     tkExtends,              // [IN] Base TypeDef or TypeRef.
        mdToken     rtkImplements[]);  // [IN] Implemented interfaces.

    STDMETHOD(SetEventProps)(               // S_OK or error.
        mdEvent     ev,                     // [IN] The event token.
        DWORD       dwEventFlags,           // [IN] CorEventAttr.
        mdToken     tkEventType,            // [IN] A reference (mdTypeRef or mdTypeRef) to the Event class.
        mdMethodDef mdAddOn,                // [IN] Add method.
        mdMethodDef mdRemoveOn,             // [IN] Remove method.
        mdMethodDef mdFire,                 // [IN] Fire method.
        mdMethodDef rmdOtherMethods[]);// [IN] Array of other methods associate with the event.

    STDMETHOD(SetPermissionSetProps)(       // S_OK or error.
        mdToken     tk,                     // [IN] The object to be decorated.
        DWORD       dwAction,               // [IN] CorDeclSecurity.
        void const  *pvPermission,          // [IN] Permission blob.
        ULONG       cbPermission,           // [IN] Count of bytes of pvPermission.
        mdPermission *ppm);            // [OUT] Permission token.

    STDMETHOD(DefinePinvokeMap)(            // Return code.
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       dwMappingFlags,         // [IN] Flags used for mapping.
        LPCWSTR     szImportName,           // [IN] Import name.
        mdModuleRef mrImportDLL);      // [IN] ModuleRef token for the target DLL.

    STDMETHOD(SetPinvokeMap)(               // Return code.
        mdToken     tk,                     // [IN] FieldDef or MethodDef.
        DWORD       dwMappingFlags,         // [IN] Flags used for mapping.
        LPCWSTR     szImportName,           // [IN] Import name.
        mdModuleRef mrImportDLL);      // [IN] ModuleRef token for the target DLL.

    STDMETHOD(DeletePinvokeMap)(            // Return code.
        mdToken     tk);               // [IN] FieldDef or MethodDef.

    // New CustomAttribute functions.
    STDMETHOD(DefineCustomAttribute)(       // Return code.
        mdToken     tkObj,                  // [IN] The object to put the value on.
        mdToken     tkType,                 // [IN] Type of the CustomAttribute (TypeRef/TypeDef).
        void const  *pCustomAttribute,      // [IN] The custom value data.
        ULONG       cbCustomAttribute,      // [IN] The custom value data length.
        mdCustomAttribute *pcv);       // [OUT] The custom value token value on return.

    STDMETHOD(SetCustomAttributeValue)(     // Return code.
        mdCustomAttribute pcv,              // [IN] The custom value token whose value to replace.
        void const  *pCustomAttribute,      // [IN] The custom value data.
        ULONG       cbCustomAttribute);// [IN] The custom value data length.

    STDMETHOD(DefineField)(                 // S_OK or error.
        mdTypeDef   td,                     // Parent TypeDef
        LPCWSTR     szName,                 // Name of member
        DWORD       dwFieldFlags,           // Member attributes
        PCCOR_SIGNATURE pvSigBlob,          // [IN] point to a blob value of CLR signature
        ULONG       cbSigBlob,              // [IN] count of bytes in the signature blob
        DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] constant value
        ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
        mdFieldDef  *pmd);             // [OUT] Put member token here

    STDMETHOD(DefineProperty)(
        mdTypeDef   td,                     // [IN] the class/interface on which the property is being defined
        LPCWSTR     szProperty,             // [IN] Name of the property
        DWORD       dwPropFlags,            // [IN] CorPropertyAttr
        PCCOR_SIGNATURE pvSig,              // [IN] the required type signature
        ULONG       cbSig,                  // [IN] the size of the type signature blob
        DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] constant value
        ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
        mdMethodDef mdSetter,               // [IN] optional setter of the property
        mdMethodDef mdGetter,               // [IN] optional getter of the property
        mdMethodDef rmdOtherMethods[],      // [IN] an optional array of other methods
        mdProperty  *pmdProp);         // [OUT] output property token

    STDMETHOD(DefineParam)(
        mdMethodDef md,                     // [IN] Owning method
        ULONG       ulParamSeq,             // [IN] Which param
        LPCWSTR     szName,                 // [IN] Optional param name
        DWORD       dwParamFlags,           // [IN] Optional param flags
        DWORD       dwCPlusTypeFlag,        // [IN] flag for value type. selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] constant value
        ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
        mdParamDef  *ppd);             // [OUT] Put param token here

    STDMETHOD(SetFieldProps)(               // S_OK or error.
        mdFieldDef  fd,                     // [IN] The FieldDef.
        DWORD       dwFieldFlags,           // [IN] Field attributes.
        DWORD       dwCPlusTypeFlag,        // [IN] Flag for the value type, selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] Constant value.
        ULONG       cchValue);         // [IN] size of constant value (string, in wide chars).

    STDMETHOD(SetPropertyProps)(            // S_OK or error.
        mdProperty  pr,                     // [IN] Property token.
        DWORD       dwPropFlags,            // [IN] CorPropertyAttr.
        DWORD       dwCPlusTypeFlag,        // [IN] Flag for value type, selected ELEMENT_TYPE_*
        void const  *pValue,                // [IN] Constant value.
        ULONG       cchValue,               // [IN] size of constant value (string, in wide chars).
        mdMethodDef mdSetter,               // [IN] Setter of the property.
        mdMethodDef mdGetter,               // [IN] Getter of the property.
        mdMethodDef rmdOtherMethods[]);// [IN] Array of other methods.

    STDMETHOD(SetParamProps)(               // Return code.
        mdParamDef  pd,                     // [IN] Param token.
        LPCWSTR     szName,                 // [IN] Param name.
        DWORD       dwParamFlags,           // [IN] Param flags.
        DWORD       dwCPlusTypeFlag,        // [IN] Flag for value type. selected ELEMENT_TYPE_*.
        void const  *pValue,                // [OUT] Constant value.
        ULONG       cchValue);         // [IN] size of constant value (string, in wide chars).

    // Specialized Custom Attributes for security.
    STDMETHOD(DefineSecurityAttributeSet)(  // Return code.
        mdToken     tkObj,                  // [IN] Class or method requiring security attributes.
        COR_SECATTR rSecAttrs[],            // [IN] Array of security attribute descriptions.
        ULONG       cSecAttrs,              // [IN] Count of elements in above array.
        ULONG       *pulErrorAttr);    // [OUT] On error, index of attribute causing problem.

    STDMETHOD(ApplyEditAndContinue)(        // S_OK or error.
        IUnknown    *pImport);         // [IN] Metadata from the delta PE.

    STDMETHOD(TranslateSigWithScope)(
        IMetaDataAssemblyImport *pAssemImport, // [IN] importing assembly interface
        const void  *pbHashValue,           // [IN] Hash Blob for Assembly.
        ULONG       cbHashValue,            // [IN] Count of bytes.
        IMetaDataImport *import,            // [IN] importing interface
        PCCOR_SIGNATURE pbSigBlob,          // [IN] signature in the importing scope
        ULONG       cbSigBlob,              // [IN] count of bytes of signature
        IMetaDataAssemblyEmit *pAssemEmit,  // [IN] emit assembly interface
        IMetaDataEmit *emit,                // [IN] emit interface
        PCOR_SIGNATURE pvTranslatedSig,     // [OUT] buffer to hold translated signature
        ULONG       cbTranslatedSigMax,
        ULONG       *pcbTranslatedSig);// [OUT] count of bytes in the translated signature

    STDMETHOD(SetMethodImplFlags)(          // [IN] S_OK or error.
        mdMethodDef md,                     // [IN] Method for which to set ImplFlags
        DWORD       dwImplFlags);

    STDMETHOD(SetFieldRVA)(                 // [IN] S_OK or error.
        mdFieldDef  fd,                     // [IN] Field for which to set offset
        ULONG       ulRVA);            // [IN] The offset

    STDMETHOD(Merge)(                       // S_OK or error.
        IMetaDataImport *pImport,           // [IN] The scope to be merged.
        IMapToken   *pHostMapToken,         // [IN] Host IMapToken interface to receive token remap notification
        IUnknown    *pHandler);        // [IN] An object to receive to receive error notification.

    STDMETHOD(MergeEnd)();             // S_OK or error.

};

#endif // SYMBOLINFO_H
