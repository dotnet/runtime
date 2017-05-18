// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: WinRTTypeNameConverter.cpp
//

//

//
// ============================================================================

#include "common.h"

#ifdef FEATURE_COMINTEROP
#include "winrttypenameconverter.h"
#include "typeresolution.h"

struct RedirectedTypeNames
{
    LPCSTR  szClrNamespace;
    LPCSTR  szClrName;
    WinMDAdapter::FrameworkAssemblyIndex assembly;
    WinMDAdapter::WinMDTypeKind kind;
};

#define DEFINE_PROJECTED_TYPE(szWinRTNS, szWinRTName, szClrNS, szClrName, nClrAsmIdx, ncontractAsmIndex, nWinRTIndex, nClrIndex, nWinMDTypeKind) \
    { szClrNS, szClrName, WinMDAdapter::FrameworkAssembly_ ## nClrAsmIdx, WinMDAdapter::WinMDTypeKind_ ## nWinMDTypeKind },

static const RedirectedTypeNames g_redirectedTypeNames[WinMDAdapter::RedirectedTypeIndex_Count] = 
{
#include "winrtprojectedtypes.h"
};

#undef DEFINE_PROJECTED_TYPE

struct RedirectedTypeNamesKey
{
    RedirectedTypeNamesKey(LPCSTR szNamespace, LPCSTR szName) :
        m_szNamespace(szNamespace),
        m_szName(szName)
    {
        LIMITED_METHOD_CONTRACT;
    }

    LPCSTR  m_szNamespace;
    LPCSTR  m_szName;
};

class RedirectedTypeNamesTraits : public NoRemoveSHashTraits< DefaultSHashTraits<const RedirectedTypeNames *> >
{
public:
    typedef RedirectedTypeNamesKey key_t;

    static key_t GetKey(element_t e)
    { 
        LIMITED_METHOD_CONTRACT;
        return RedirectedTypeNamesKey(e->szClrNamespace, e->szClrName);
    }
    static BOOL Equals(key_t k1, key_t k2) 
    { 
        LIMITED_METHOD_CONTRACT;
        return (strcmp(k1.m_szName, k2.m_szName) == 0) && (strcmp(k1.m_szNamespace, k2.m_szNamespace) == 0);
    }
    static count_t Hash(key_t k) 
    {
        LIMITED_METHOD_CONTRACT;
        // Only use the Name when calculating the hash value. Many redirected types share the same namespace so
        // there isn't a lot of value in using the namespace when calculating the hash value.
        return HashStringA(k.m_szName);
    }

    static const element_t Null() { LIMITED_METHOD_CONTRACT; return NULL; }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e == NULL; }
};

typedef SHash< RedirectedTypeNamesTraits > RedirectedTypeNamesHashTable;
static RedirectedTypeNamesHashTable * s_pRedirectedTypeNamesHashTable = NULL;

//
// Return the redirection index and type kind if the MethodTable* is a redirected type
//
bool WinRTTypeNameConverter::ResolveRedirectedType(MethodTable *pMT, WinMDAdapter::RedirectedTypeIndex * pIndex, WinMDAdapter::WinMDTypeKind * pKind /*=NULL*/)
{
    LIMITED_METHOD_CONTRACT;

    WinMDAdapter::RedirectedTypeIndex index = pMT->GetClass()->GetWinRTRedirectedTypeIndex();
    if (index == WinMDAdapter::RedirectedTypeIndex_Invalid)
        return false;

    if (pIndex != NULL)
        *pIndex = index;

    if (pKind != NULL)
        *pKind = g_redirectedTypeNames[index].kind;

    return true;
}

#ifndef DACCESS_COMPILE

class MethodTableListNode;

// Information to help in generating a runtimeclass name for a managed type
// implementing a generic WinRT interface
struct WinRTTypeNameInfo
{
    MethodTableListNode*    PreviouslyVisitedTypes;
    CorGenericParamAttr     CurrentTypeParameterVariance;

    WinRTTypeNameInfo(MethodTableListNode* pPreviouslyVisitedTypes) :
        PreviouslyVisitedTypes(pPreviouslyVisitedTypes),
        CurrentTypeParameterVariance(gpNonVariant)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pPreviouslyVisitedTypes != nullptr);
    }
};

// Helper data structure to build a stack allocated reverse linked list of MethodTables that we're examining
// while building up WinRT runtimeclass name
class MethodTableListNode
{
    MethodTable* m_pMT;                   // Type examined while building the runtimeclass name
    MethodTableListNode* m_pPrevious;     // Previous node in the list

public:
    MethodTableListNode(MethodTable* pMT, WinRTTypeNameInfo* pCurrent)
        : m_pMT(pMT),
          m_pPrevious(nullptr)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(pMT != nullptr);

        if (pCurrent != nullptr)
        {
            m_pPrevious = pCurrent->PreviouslyVisitedTypes;
        }
    }

    bool Contains(MethodTable* pMT)
    {
        LIMITED_METHOD_CONTRACT;

        if (pMT == m_pMT)
        {
            return true;
        }
        else if (m_pPrevious == nullptr)
        {
            return false;
        }
        else
        {
            return m_pPrevious->Contains(pMT);
        }
    }
};

//
// Append WinRT type name for the specified type handle
//
bool WinRTTypeNameConverter::AppendWinRTTypeNameForManagedType(
    TypeHandle      thManagedType,
    SString         &strWinRTTypeName,
    bool            bForGetRuntimeClassName,
    bool            *pbIsPrimitive)
{
    WRAPPER_NO_CONTRACT;
    return AppendWinRTTypeNameForManagedType(thManagedType, strWinRTTypeName, bForGetRuntimeClassName, pbIsPrimitive, nullptr);
}

bool WinRTTypeNameConverter::AppendWinRTTypeNameForManagedType(
    TypeHandle          thManagedType,
    SString             &strWinRTTypeName,
    bool                bForGetRuntimeClassName,
    bool               *pbIsPrimitive,
    WinRTTypeNameInfo  *pCurrentTypeInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(!thManagedType.IsNull());
        PRECONDITION(CheckPointer(pbIsPrimitive, NULL_OK));
        PRECONDITION(CheckPointer(pCurrentTypeInfo, NULL_OK));
    }
    CONTRACTL_END;

    if (pbIsPrimitive)
        *pbIsPrimitive = false;

    MethodTable *pMT = thManagedType.GetMethodTable();
    BOOL fIsIReference = FALSE, fIsIReferenceArray = FALSE;
    if (pMT->GetNumGenericArgs() == 1)
    {
        fIsIReference = pMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__CLRIREFERENCEIMPL));
        fIsIReferenceArray = pMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__CLRIREFERENCEARRAYIMPL));
    }
    
    WinMDAdapter::RedirectedTypeIndex index;
    if (ResolveRedirectedType(pMT, &index))
    {   
        // Redirected types
        // Use the redirected WinRT name
        strWinRTTypeName.Append(WinMDAdapter::GetRedirectedTypeFullWinRTName(index));
    }
    else if (fIsIReference || fIsIReferenceArray)
    {
        //
        // Convert CLRIReferenceImpl<T>/CLRIReferenceArrayImpl<T> to a WinRT Type
        //
        // If GetRuntimeClassName = true, return IReference<T>/IReferenceArray<T>
        // Otherwise, return T/IReferenceArray`1<T>
        //
        Instantiation inst = pMT->GetInstantiation();
        _ASSERTE(inst.GetNumArgs() == 1);
        TypeHandle th = inst[0];

        // I'm sure there are ways to avoid duplication here but I prefer this way - it is easier to understand
        if (fIsIReference)
        {
            if (bForGetRuntimeClassName)
            {
                //
                // IReference<T>
                //
                strWinRTTypeName.Append(W("Windows.Foundation.IReference`1<"));

                if (!AppendWinRTTypeNameForManagedType(
                    th,
                    strWinRTTypeName, 
                    bForGetRuntimeClassName,
                    NULL
                    ))
                    return false;

                strWinRTTypeName.Append(W('>'));

                return true;
            }
            else
            {
                //
                // T
                //
                return AppendWinRTTypeNameForManagedType(
                    th,
                    strWinRTTypeName, 
                    bForGetRuntimeClassName,
                    pbIsPrimitive
                    );
            }
        }
        else
        {
            //
            // IReferenceArray<T>
            //
            strWinRTTypeName.Append(W("Windows.Foundation.IReferenceArray`1<"));
            
            if (!AppendWinRTTypeNameForManagedType(
                th,
                strWinRTTypeName, 
                bForGetRuntimeClassName,
                NULL))
                return false;

            strWinRTTypeName.Append(W('>'));

            return true;
        }        
    }
    else if (pMT->IsProjectedFromWinRT() || pMT->IsExportedToWinRT())
    {
        //
        // WinRT type
        //
        SString strTypeName;
        pMT->_GetFullyQualifiedNameForClassNestedAware(strTypeName);
        strWinRTTypeName.Append(strTypeName);
    }
    else if (AppendWinRTNameForPrimitiveType(pMT, strWinRTTypeName))
    {
        //
        // WinRT primitive type, return immediately
        //
        if (pbIsPrimitive)
            *pbIsPrimitive = true;
        return true;
    }
    else if (pMT->IsArray())
    {
        if (bForGetRuntimeClassName)
        {
            //
            // An array is not a valid WinRT type - it must be wrapped in IReferenceArray to be a valid
            // WinRT type
            //
            return false;
        }
        else
        {
            //
            // System.Type marshaling - convert array type into IReferenceArray<T>
            //
            strWinRTTypeName.Append(W("Windows.Foundation.IReferenceArray`1<"));

            if (!AppendWinRTTypeNameForManagedType(thManagedType.AsArray()->GetArrayElementTypeHandle(), strWinRTTypeName, bForGetRuntimeClassName, NULL))
                return false;
        
            strWinRTTypeName.Append(W('>'));
        }
    }
    else if (bForGetRuntimeClassName)
    {
        //
        // Not a WinRT type or a WinRT Primitive type,
        // but if it implements a WinRT interface we will return the interface name.
        // Which interface should we return if it implements multiple WinRT interfaces?
        // For now we return the top most interface. And if there are more than one
        // top most interfaces, we return the first one we encounter during the interface enumeration.
        //
        //
        // We also need to keep track of the types we've already considered, so we don't wind up in an
        // infinite recursion processing generic interfaces.
        // For example, in the case where we have:
        //
        //   class ManagedType : IEnumerable<ManagedType>
        //
        // We do not want to keep recursing on the ManagedType type parameter.  Instead, we should
        // discover that we've already attempted to figure out what the best representation for
        // ManagedType is, and bail out.
        //
        // This is a linear search, however that shouldn't generally be a problem, since generic
        // nesting should not be very large in the common case.

        if (pCurrentTypeInfo != nullptr && pCurrentTypeInfo->PreviouslyVisitedTypes->Contains(pMT))
        {
            // We should only be restricting this recursion on non-WinRT types that may have WinRT interfaces
            _ASSERTE(!pMT->IsProjectedFromWinRT() && !pMT->IsExportedToWinRT() && !pMT->IsTruePrimitive());

            // We have two choices.  If this is a reference type and the interface parameter is covariant, we
            // can use IInspectable as the closure.  Otherwise, we need to simply fail out with no possible
            // type name.
            if (pCurrentTypeInfo->CurrentTypeParameterVariance == gpCovariant &&
                thManagedType.IsBoxedAndCanCastTo(TypeHandle(g_pObjectClass), nullptr))
            {
                // Object is used in runtime class names for generics closed over IInspectable at the ABI
                strWinRTTypeName.Append(W("Object"));
                return true;
            }
            else
            {
                return false;
            }
        }

        // This is the "top" most redirected interface implemented by pMT.
        // E.g. if pMT implements both IList`1 and IEnumerable`1, we pick IList`1.

        MethodTable* pTopIfaceMT = NULL;
        WinMDAdapter::RedirectedTypeIndex idxTopIface = (WinMDAdapter::RedirectedTypeIndex)-1;

        MethodTable::InterfaceMapIterator it = pMT->IterateInterfaceMap();
        while (it.Next())
        {
            MethodTable* pIfaceMT = it.GetInterface();
            if (ResolveRedirectedType(pIfaceMT, &index) ||
                pIfaceMT->IsProjectedFromWinRT())
            {
                if (pTopIfaceMT == NULL || pIfaceMT->ImplementsInterface(pTopIfaceMT))
                {
                    pTopIfaceMT = pIfaceMT;

                    // If pIfaceMT is not a redirected type, idxTopIface will contain garbage.
                    // But that is fine because we will only use idxTopIface if pTopIfaceMT
                    // is a redirected type.
                    idxTopIface = index;
                }
            }
        }

        if (pTopIfaceMT != NULL)
        {
            if (pTopIfaceMT->IsProjectedFromWinRT())
            {
                // Mscorlib contains copies of WinRT interfaces - don't return their names,
                // instead return names of the corresponding interfaces in Windows.Foundation.winmd.

                if (pTopIfaceMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IKEYVALUEPAIR)))
                    strWinRTTypeName.Append(W("Windows.Foundation.Collections.IKeyValuePair`2"));
                else if (pTopIfaceMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IITERATOR)))
                    strWinRTTypeName.Append(W("Windows.Foundation.Collections.IIterator`1"));
                else if (pTopIfaceMT->HasSameTypeDefAs(MscorlibBinder::GetClass(CLASS__IPROPERTYVALUE)))
                    strWinRTTypeName.Append(W("Windows.Foundation.IPropertyValue"));
                else
                {
                    SString strTypeName;
                    pTopIfaceMT->_GetFullyQualifiedNameForClassNestedAware(strTypeName);
                    strWinRTTypeName.Append(strTypeName);
                }
            }
            else
                strWinRTTypeName.Append(WinMDAdapter::GetRedirectedTypeFullWinRTName(idxTopIface));

            // Since we are returning the typeName for the pTopIfaceMT we should use the same interfaceType 
            // to check for instantiation and creating the closed generic.
            pMT = pTopIfaceMT;
        }
        else
            return false;
    }
    else
    {
        //
        // Non-WinRT type, Non-WinRT-Primitive type
        //

        return false;
    }

    // We allow typeName generation for only open types or completely instantiated types.
    // In case it is a generic type definition like IList<T> we return the typeName as IVector'1 only
    // and hence we do not need to visit the arguments.
    if (pMT->HasInstantiation() && (!pMT->IsGenericTypeDefinition()))
    {
        // Add the current type we're trying to get a runtimeclass name for to the list of types
        // we've already seen, so we can check for infinite recursion on the generic parameters.
        MethodTableListNode examinedTypeList(thManagedType.GetMethodTable(), pCurrentTypeInfo);


        strWinRTTypeName.Append(W('<'));

        //
        // Convert each arguments
        //
        Instantiation inst = pMT->GetInstantiation();
        for (DWORD i = 0; i < inst.GetNumArgs(); ++i)
        {
            TypeHandle th = inst[i];

            // We have a partial open type with us and hence we should throw.
            if(th.ContainsGenericVariables())
                COMPlusThrowArgumentException(W("th"), W("Argument_TypeNotValid"));

            if (i > 0)
                strWinRTTypeName.Append(W(','));
            
            // In the recursive case, we can sometimes do a better job of getting a runtimeclass name if
            // the actual instantiated type can be substitued for a different type due to variance on the
            // generic type parameter.  In order to allow that to occur when processing this parameter,
            // make a note of the variance properties to pass along with the previously examined type list
            WinRTTypeNameInfo currentParameterInfo(&examinedTypeList);
            if (pMT->HasVariance())
            {
                currentParameterInfo.CurrentTypeParameterVariance = pMT->GetClass()->GetVarianceOfTypeParameter(i);
            }

            // Convert to WinRT type name
            // If it is not a WinRT type, return immediately
            if (!AppendWinRTTypeNameForManagedType(th, strWinRTTypeName, bForGetRuntimeClassName, NULL, &currentParameterInfo))
                return false;
        }

        strWinRTTypeName.Append(W('>'));
    }
    
    return true;
}

//
// Lookup table : CorElementType -> WinRT primitive type name
//
LPCWSTR const s_wszCorElementTypeToWinRTNameMapping[] =
{
    NULL,           // ELEMENT_TYPE_END                        = 0x0,
    NULL,           // ELEMENT_TYPE_VOID                       = 0x1,
    W("Boolean"),     // ELEMENT_TYPE_BOOLEAN                    = 0x2,
    W("Char16"),      // ELEMENT_TYPE_CHAR                       = 0x3,
    NULL,           // ELEMENT_TYPE_I1                         = 0x4,
    W("UInt8"),       // ELEMENT_TYPE_U1                         = 0x5,
    W("Int16"),       // ELEMENT_TYPE_I2                         = 0x6,
    W("UInt16"),      // ELEMENT_TYPE_U2                         = 0x7,
    W("Int32"),       // ELEMENT_TYPE_I4                         = 0x8,
    W("UInt32"),      // ELEMENT_TYPE_U4                         = 0x9,
    W("Int64"),       // ELEMENT_TYPE_I8                         = 0xa,
    W("UInt64"),      // ELEMENT_TYPE_U8                         = 0xb,
    W("Single"),      // ELEMENT_TYPE_R4                         = 0xc,
    W("Double"),      // ELEMENT_TYPE_R8                         = 0xd,
    W("String"),      // ELEMENT_TYPE_STRING                     = 0xe,
    NULL,           // ELEMENT_TYPE_PTR                        = 0xf,
    NULL,           // ELEMENT_TYPE_BYREF                      = 0x10,
    NULL,           // ELEMENT_TYPE_VALUETYPE                  = 0x11,
    NULL,           // ELEMENT_TYPE_CLASS                      = 0x12,
    NULL,           // ???                                     = 0x13,
    NULL,           // ELEMENT_TYPE_ARRAY                      = 0x14,
    NULL,           // ???                                     = 0x15,
    NULL,           // ELEMENT_TYPE_TYPEDBYREF                 = 0x16,
    NULL,           // ELEMENT_TYPE_I                          = 0x18,
    NULL,           // ELEMENT_TYPE_U                          = 0x19,
    NULL,           // ???                                     = 0x1A,
    NULL,           // ELEMENT_TYPE_FNPTR                      = 0x1B,
    W("Object"),      // ELEMENT_TYPE_OBJECT                     = 0x1C,
};

//
// Get predefined WinRT name for a primitive type
//
bool WinRTTypeNameConverter::GetWinRTNameForPrimitiveType(MethodTable *pMT, SString *pName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pMT, NULL_OK));
    }
    CONTRACTL_END;

    CorElementType elemType = TypeHandle(pMT).GetSignatureCorElementType();

    //
    // Try to find it in a lookup table
    //
    if (elemType >= 0 && elemType < _countof(s_wszCorElementTypeToWinRTNameMapping))
    {
        LPCWSTR wszName = s_wszCorElementTypeToWinRTNameMapping[elemType];
        
        if (wszName != NULL)
        {
            if (pName != NULL)
            {
                pName->SetLiteral(wszName);
            }
            
            return true;
        }
    }

    if (elemType == ELEMENT_TYPE_VALUETYPE)
    {
        if (pMT->GetModule()->IsSystem() && 
            IsTypeRefOrDef(g_GuidClassName, pMT->GetModule(), pMT->GetCl()))
        {
            if (pName != NULL)
            {
                pName->SetLiteral(W("Guid"));
            }
            
            return true;
        }
    }
    else if (elemType == ELEMENT_TYPE_CLASS)
    {
        if (pMT == g_pObjectClass)
        {
            if (pName != NULL)
            {
                pName->SetLiteral(W("Object"));
            }
            
            return true;
        }
        if (pMT == g_pStringClass)
        {
            if (pName != NULL)
            {
                pName->SetLiteral(W("String"));
            }
            
            return true;
        }
    }

    // it's not a primitive
    return false;
}

//
// Append the WinRT type name for the method table, if it is a WinRT primitive type
//
bool WinRTTypeNameConverter::AppendWinRTNameForPrimitiveType(MethodTable *pMT, SString &strName)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pMT, NULL_OK));
    }
    CONTRACTL_END;
    
    SString strPrimitiveTypeName;
    if (GetWinRTNameForPrimitiveType(pMT, &strPrimitiveTypeName))
    {
        strName.Append(strPrimitiveTypeName);
        return true;
    }

    return false;
}

// static 
bool WinRTTypeNameConverter::IsRedirectedType(MethodTable *pMT, WinMDAdapter::WinMDTypeKind kind)
{
    LIMITED_METHOD_CONTRACT;

    WinMDAdapter::RedirectedTypeIndex index;
    return (ResolveRedirectedType(pMT, &index) && (g_redirectedTypeNames[index].kind == kind));
}

//
// Determine if the given type redirected only by doing name comparisons.  This is used to
// calculate the redirected type index at EEClass creation time.
//

// static
WinMDAdapter::RedirectedTypeIndex WinRTTypeNameConverter::GetRedirectedTypeIndexByName(
    Module *pModule, 
    mdTypeDef token)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    // If the redirected type hashtable has not been initialized initialize it
    if (s_pRedirectedTypeNamesHashTable == NULL)
    {
        NewHolder<RedirectedTypeNamesHashTable> pRedirectedTypeNamesHashTable = new RedirectedTypeNamesHashTable();
        pRedirectedTypeNamesHashTable->Reallocate(2 * COUNTOF(g_redirectedTypeNames));

        for (int i = 0; i < COUNTOF(g_redirectedTypeNames); ++i)
        {
            pRedirectedTypeNamesHashTable->Add(&(g_redirectedTypeNames[i]));
        }

        if (InterlockedCompareExchangeT(&s_pRedirectedTypeNamesHashTable, pRedirectedTypeNamesHashTable.GetValue(), NULL) == NULL)
        {
            pRedirectedTypeNamesHashTable.SuppressRelease();
        }
    }

    IMDInternalImport *pInternalImport = pModule->GetMDImport();
    LPCSTR szName;
    LPCSTR szNamespace;
    IfFailThrow(pInternalImport->GetNameOfTypeDef(token, &szName, &szNamespace));

    const RedirectedTypeNames *const * ppRedirectedNames = s_pRedirectedTypeNamesHashTable->LookupPtr(RedirectedTypeNamesKey(szNamespace, szName));
    if (ppRedirectedNames == NULL)
    {
        return WinMDAdapter::RedirectedTypeIndex_Invalid;
    }

    UINT redirectedTypeIndex = (UINT)(*ppRedirectedNames - g_redirectedTypeNames);
    _ASSERTE(redirectedTypeIndex < COUNTOF(g_redirectedTypeNames));

    // If the redirected assembly is mscorlib just compare it directly. This is necessary because 
    // WinMDAdapter::GetExtraAssemblyRefProps does not support mscorlib
    if (g_redirectedTypeNames[redirectedTypeIndex].assembly == WinMDAdapter::FrameworkAssembly_Mscorlib)
    {
        return MscorlibBinder::GetModule()->GetAssembly() == pModule->GetAssembly() ?
            (WinMDAdapter::RedirectedTypeIndex)redirectedTypeIndex :
            WinMDAdapter::RedirectedTypeIndex_Invalid;
    }

    LPCSTR pSimpleName;
    AssemblyMetaDataInternal context;
    const BYTE * pbKeyToken;
    DWORD cbKeyTokenLength;
    DWORD dwFlags;
    WinMDAdapter::GetExtraAssemblyRefProps(
        g_redirectedTypeNames[redirectedTypeIndex].assembly,
        &pSimpleName,
        &context,
        &pbKeyToken,
        &cbKeyTokenLength,
        &dwFlags);

    AssemblySpec spec;
    IfFailThrow(spec.Init(
        pSimpleName, 
        &context,
        pbKeyToken, 
        cbKeyTokenLength, 
        dwFlags));
    Assembly* pRedirectedAssembly = spec.LoadAssembly(
        FILE_LOADED,
        FALSE); // fThrowOnFileNotFound

    if (pRedirectedAssembly == NULL)
    {
        return WinMDAdapter::RedirectedTypeIndex_Invalid;
    }
            
    // Resolve the name in the redirected assembly to the actual type def and assembly
    NameHandle nameHandle(szNamespace, szName);
    nameHandle.SetTokenNotToLoad(tdAllTypes);
    Module * pTypeDefModule;
    mdTypeDef typeDefToken;

    if (ClassLoader::ResolveNameToTypeDefThrowing(
        pRedirectedAssembly->GetManifestModule(),
        &nameHandle,
        &pTypeDefModule,
        &typeDefToken,
        Loader::DontLoad))
    {
        // Finally check if the assembly from this type def token mathes the assembly type forwareded from the
        // redirected assembly
        if (pTypeDefModule->GetAssembly() == pModule->GetAssembly())
        {
            return (WinMDAdapter::RedirectedTypeIndex)redirectedTypeIndex;
        }
    }

    return WinMDAdapter::RedirectedTypeIndex_Invalid;
}

struct WinRTPrimitiveTypeMapping
{

    BinderClassID   binderID;
    LPCWSTR         wszWinRTName;
};

#define DEFINE_PRIMITIVE_TYPE_MAPPING(elementType, winrtTypeName) { elementType, L##winrtTypeName },

//
// Pre-sorted mapping : WinRT primitive type string -> BinderClassID
//
const WinRTPrimitiveTypeMapping s_winRTPrimitiveTypeMapping[] = 
{
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__BOOLEAN, "Boolean")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__CHAR,    "Char16")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__DOUBLE,  "Double")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__GUID,    "Guid")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__INT16,   "Int16")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__INT32,   "Int32")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__INT64,   "Int64")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__OBJECT,  "Object")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__SINGLE,  "Single")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__STRING,  "String")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__UINT16,  "UInt16")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__UINT32,  "UInt32")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__UINT64,  "UInt64")
    DEFINE_PRIMITIVE_TYPE_MAPPING(CLASS__BYTE,    "UInt8")   
};

//
// Return MethodTable* for the specified WinRT primitive type name
//
bool WinRTTypeNameConverter::GetMethodTableFromWinRTPrimitiveType(LPCWSTR wszTypeName, UINT32 uTypeNameLen, MethodTable **ppMT)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ppMT));
    }
    CONTRACTL_END;

    if (uTypeNameLen >= 4 && uTypeNameLen <= 7)
    {
        //
        // Binary search the lookup table
        //
        int begin = 0, end = _countof(s_winRTPrimitiveTypeMapping) - 1;
        while (begin <= end)
        {
            _ASSERTE(begin >= 0 && begin <= _countof(s_winRTPrimitiveTypeMapping) - 1);
            _ASSERTE(end >= 0 && end <= _countof(s_winRTPrimitiveTypeMapping) - 1);
            
            int mid = (begin + end) / 2;
            int ret = wcscmp(wszTypeName, s_winRTPrimitiveTypeMapping[mid].wszWinRTName);
            if (ret == 0)
            {
                *ppMT = MscorlibBinder::GetClass(s_winRTPrimitiveTypeMapping[mid].binderID);
                return true;
            }
            else if (ret > 0)
            {
                begin = mid + 1;
            }
            else
            {
                end = mid - 1;
            }
        }
    }
    
    // it's not a primitive
    return false;
}

// Is the specified MethodTable a redirected WinRT type?
bool WinRTTypeNameConverter::IsRedirectedWinRTSourceType(MethodTable *pMT)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pMT->IsProjectedFromWinRT())
        return false;

    // redirected types are hidden (made internal) by the adapter
    if (IsTdPublic(pMT->GetClass()->GetProtection()))
        return false;

    DefineFullyQualifiedNameForClassW();
    LPCWSTR pszName = GetFullyQualifiedNameForClassW_WinRT(pMT);
    
    return !!WinMDAdapter::ConvertWellKnownFullTypeNameFromWinRTToClr(&pszName, NULL);
}

//
// Get TypeHandle from a WinRT type name
// Parse the WinRT type name in the form of WinRTType=TypeName[<WinRTType[, WinRTType, ...]>]
//
TypeHandle WinRTTypeNameConverter::GetManagedTypeFromWinRTTypeName(LPCWSTR wszWinRTTypeName, bool *pbIsPrimitive)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(wszWinRTTypeName));
        PRECONDITION(CheckPointer(pbIsPrimitive, NULL_OK));
    }
    CONTRACTL_END;

    SString ssTypeName(SString::Literal, wszWinRTTypeName);
    
    TypeHandle th = GetManagedTypeFromWinRTTypeNameInternal(&ssTypeName, pbIsPrimitive);
    if (th.IsNull())
    {
        COMPlusThrowArgumentException(W("typeName"), NULL);    
    }

    return th;
}

// Helper used by code:GetWinRTType to compose a generic type from an array of components.
// For example [IDictionary`2, int, IList`1, string] yields IDictionary`2<int, IList`1<string>>.
static TypeHandle ComposeTypeRecursively(CQuickArray<TypeHandle> &rqPartTypes, DWORD *pIndex)
{
    CONTRACTL
    {
        MODE_ANY;
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(*pIndex < rqPartTypes.Size());
    }
    CONTRACTL_END;

    DWORD index = (*pIndex)++;
    TypeHandle th = rqPartTypes[index];

    if (th.HasInstantiation())
    {
        DWORD dwArgCount = th.GetNumGenericArgs();
        for (DWORD i = 0; i < dwArgCount; i++)
        {
            // we scan rqPartTypes linearly so we know that the elements can be reused
            rqPartTypes[i + index] = ComposeTypeRecursively(rqPartTypes, pIndex);
        }

        Instantiation inst(rqPartTypes.Ptr() + index, dwArgCount);
        th = th.Instantiate(inst);
    }
    else if (th == g_pArrayClass)
    {    
        // Support for arrays
        rqPartTypes[index] = ComposeTypeRecursively(rqPartTypes, pIndex);
        th = ClassLoader::LoadArrayTypeThrowing(rqPartTypes[index], ELEMENT_TYPE_SZARRAY, 1);
    }

    return th;
}

#ifdef CROSSGEN_COMPILE
//
// In crossgen, we use a mockup of RoParseTypeName since we need to run on pre-Win8 machines.
//
extern "C" HRESULT WINAPI CrossgenRoParseTypeName(SString* typeName, DWORD *partsCount, SString **typeNameParts);
#endif

//
// Return TypeHandle for the specified WinRT type name (supports generic type)
// Updates wszWinRTTypeName pointer as it parse the string    
//
TypeHandle WinRTTypeNameConverter::GetManagedTypeFromWinRTTypeNameInternal(SString *ssTypeName, bool *pbIsPrimitive)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ssTypeName));
        PRECONDITION(CheckPointer(pbIsPrimitive, NULL_OK));
    }
    CONTRACTL_END;

    if (pbIsPrimitive)
        *pbIsPrimitive = false;
    
    if (ssTypeName->IsEmpty())
        return TypeHandle();
    
    TypeHandle typeHandle;

    SString::Iterator it = ssTypeName->Begin();
    if (ssTypeName->Find(it, W('<')))
    {
        // this is a generic type - use RoParseTypeName to break it down into components
        CQuickArray<TypeHandle> rqPartTypes;

#ifndef CROSSGEN_COMPILE

        DWORD dwPartsCount = 0;
        HSTRING *rhsPartNames;

        CoTaskMemHSTRINGArrayHolder hsNamePartsHolder;
        IfFailThrow(RoParseTypeName(WinRtStringRef(ssTypeName->GetUnicode(), ssTypeName->GetCount()), &dwPartsCount, &rhsPartNames));
        hsNamePartsHolder.Init(rhsPartNames, dwPartsCount);

        rqPartTypes.AllocThrows(dwPartsCount);

        // load the components
        for (DWORD i = 0; i < dwPartsCount; i++)
        {
            UINT32 cchPartLength;
            PCWSTR wszPart = WindowsGetStringRawBuffer(rhsPartNames[i], &cchPartLength);

            StackSString ssPartName(wszPart, cchPartLength);
            rqPartTypes[i] = GetManagedTypeFromSimpleWinRTNameInternal(&ssPartName, NULL);
        }

#else //CROSSGEN_COMPILE

        //
        // In crossgen, we use a mockup of RoParseTypeName since we need to run on pre-Win8 machines.
        //
        DWORD dwPartsCount = 0;
        SString *rhsPartNames;

        IfFailThrow(CrossgenRoParseTypeName(ssTypeName, &dwPartsCount, &rhsPartNames));
        _ASSERTE(rhsPartNames != nullptr);

        rqPartTypes.AllocThrows(dwPartsCount);

        // load the components
        for (DWORD i = 0; i < dwPartsCount; i++)
        {
            rqPartTypes[i] = GetManagedTypeFromSimpleWinRTNameInternal(&rhsPartNames[i], NULL);
        }

        delete[] rhsPartNames;

#endif //CROSSGEN_COMPILE

        // and instantiate the generic type
        DWORD dwIndex = 0;
        typeHandle = ComposeTypeRecursively(rqPartTypes, &dwIndex);

        _ASSERTE(dwIndex == rqPartTypes.Size());

        return typeHandle;
    }
    else
    {
        return GetManagedTypeFromSimpleWinRTNameInternal(ssTypeName, pbIsPrimitive);
    }
}

//
// Return MethodTable* for the specified WinRT primitive type name (non-generic type)
// Updates wszWinRTTypeName pointer as it parse the string
//
TypeHandle WinRTTypeNameConverter::GetManagedTypeFromSimpleWinRTNameInternal(SString *ssTypeName, bool *pbIsPrimitive)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(ssTypeName));
        PRECONDITION(CheckPointer(pbIsPrimitive, NULL_OK));
    }
    CONTRACTL_END;

    if (pbIsPrimitive)
        *pbIsPrimitive = false;

    if (ssTypeName->IsEmpty())
        return TypeHandle();
    
    //
    // Redirection
    //
    LPCWSTR pwszTypeName = ssTypeName->GetUnicode();
    WinMDAdapter::RedirectedTypeIndex uIndex;
    MethodTable *pMT = NULL;

    if (WinMDAdapter::ConvertWellKnownFullTypeNameFromWinRTToClr(&pwszTypeName, &uIndex))
    {
        //
        // Well known redirected types
        //
        return TypeHandle(GetAppDomain()->GetRedirectedType(uIndex));
    }
    else if (GetMethodTableFromWinRTPrimitiveType(pwszTypeName, ssTypeName->GetCount(), &pMT))
    {

        //
        // Primitive type
        //
        if (pbIsPrimitive)
            *pbIsPrimitive = true;
        return TypeHandle(pMT);
    }
    else if (wcscmp(pwszTypeName, W("Windows.Foundation.IReferenceArray`1")) == 0)
    {
        //
        // Handle array case - return the array and we'll create the array later
        //
        return TypeHandle(g_pArrayClass);
    }
    else
    {
        //
        // A regular WinRT type
        //
        return GetWinRTType(ssTypeName, TRUE);
    }
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_COMINTEROP
