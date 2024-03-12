// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#include "common.h"
#include "customattribute.h"
#include "invokeutil.h"
#include "method.hpp"
#include "threads.h"
#include "excep.h"
#include "corerror.h"
#include "classnames.h"
#include "assemblynative.hpp"
#include "typeparse.h"
#include "reflectioninvocation.h"
#include "runtimehandles.h"
#include "typestring.h"

static TypeHandle GetTypeForEnum(LPCUTF8 szEnumName, COUNT_T cbEnumName, DomainAssembly* pDomainAssembly)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pDomainAssembly));
        PRECONDITION(CheckPointer(szEnumName));
        PRECONDITION(cbEnumName);
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    StackSString sszEnumName(SString::Utf8, szEnumName, cbEnumName);
    return TypeName::GetTypeReferencedByCustomAttribute(sszEnumName.GetUnicode(), pDomainAssembly->GetAssembly());
}

static HRESULT ParseCaType(
    CustomAttributeParser &ca,
    CaType* pCaType,
    DomainAssembly* pDomainAssembly,
    StackSString* ss = NULL)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;

    IfFailGo(::ParseEncodedType(ca, pCaType));

    if (pCaType->tag == SERIALIZATION_TYPE_ENUM
        || (pCaType->tag == SERIALIZATION_TYPE_SZARRAY && pCaType->arrayType == SERIALIZATION_TYPE_ENUM ))
    {
        TypeHandle th = GetTypeForEnum(pCaType->szEnumName, pCaType->cEnumName, pDomainAssembly);

        if (!th.IsNull() && th.IsEnum())
        {
            pCaType->enumType = (CorSerializationType)th.GetVerifierCorElementType();

            // The assembly qualified name of th might not equal pCaType->szEnumName.
            // e.g. th could be "MyEnum, MyAssembly, Version=4.0.0.0" while
            // pCaType->szEnumName is "MyEnum, MyAssembly, Version=3.0.0.0"
            if (ss)
            {
                DWORD format = TypeString::FormatNamespace | TypeString::FormatFullInst | TypeString::FormatAssembly;
                TypeString::AppendType(*ss, th, format);
            }
        }
        else
        {
            IfFailGo(PostError(META_E_CA_UNEXPECTED_TYPE, pCaType->cEnumName, pCaType->szEnumName));
        }
    }

ErrExit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Helper to parse the values for the ctor argument list and the named argument list.
//

static HRESULT ParseCaValue(
    CustomAttributeParser &ca,
    CaValue* pCaArg,
    CaType* pCaParam,
    CaValueArrayFactory* pCaValueArrayFactory,
    DomainAssembly* pDomainAssembly)
{
    CONTRACTL
    {
        PRECONDITION(CheckPointer(pCaArg));
        PRECONDITION(CheckPointer(pCaParam));
        PRECONDITION(CheckPointer(pCaValueArrayFactory));
        THROWS;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    CorSerializationType underlyingType;
    CaType elementType;

    if (pCaParam->tag == SERIALIZATION_TYPE_TAGGED_OBJECT)
        IfFailGo(ParseCaType(ca, &pCaArg->type, pDomainAssembly));
    else
        pCaArg->type = *pCaParam;

    underlyingType = pCaArg->type.tag == SERIALIZATION_TYPE_ENUM ? pCaArg->type.enumType : pCaArg->type.tag;

    // Grab the value.
    switch (underlyingType)
    {
    case SERIALIZATION_TYPE_BOOLEAN:
    case SERIALIZATION_TYPE_I1:
    case SERIALIZATION_TYPE_U1:
        IfFailGo(ca.GetU1(&pCaArg->u1));
        break;

    case SERIALIZATION_TYPE_CHAR:
    case SERIALIZATION_TYPE_I2:
    case SERIALIZATION_TYPE_U2:
        IfFailGo(ca.GetU2(&pCaArg->u2));
        break;

    case SERIALIZATION_TYPE_I4:
    case SERIALIZATION_TYPE_U4:
        IfFailGo(ca.GetU4(&pCaArg->u4));
        break;

    case SERIALIZATION_TYPE_I8:
    case SERIALIZATION_TYPE_U8:
        IfFailGo(ca.GetU8(&pCaArg->u8));
        break;

    case SERIALIZATION_TYPE_R4:
        IfFailGo(ca.GetR4(&pCaArg->r4));
        break;

    case SERIALIZATION_TYPE_R8:
        IfFailGo(ca.GetR8(&pCaArg->r8));
        break;

    case SERIALIZATION_TYPE_STRING:
    case SERIALIZATION_TYPE_TYPE:
        IfFailGo(ca.GetString(&pCaArg->str.pStr, &pCaArg->str.cbStr));
        break;

    case SERIALIZATION_TYPE_SZARRAY:
        UINT32 len;
        IfFailGo(ca.GetU4(&len));
        pCaArg->arr.length = len;
        pCaArg->arr.pSArray = NULL;
        if (pCaArg->arr.length == (ULONG)-1)
            break;

        IfNullGo(pCaArg->arr.pSArray = pCaValueArrayFactory->Create());
        elementType.Init(pCaArg->type.arrayType, SERIALIZATION_TYPE_UNDEFINED,
            pCaArg->type.enumType, pCaArg->type.szEnumName, pCaArg->type.cEnumName);
        for (ULONG i = 0; i < pCaArg->arr.length; i++)
            IfFailGo(ParseCaValue(ca, &*pCaArg->arr.pSArray->Append(), &elementType, pCaValueArrayFactory, pDomainAssembly));

        break;

    default:
        // The format of the custom attribute record is invalid.
        hr = E_FAIL;
        break;
    } // End switch

ErrExit:
    return hr;
}

static HRESULT ParseCaCtorArgs(
    CustomAttributeParser &ca,
    CaArg* pArgs,
    ULONG cArgs,
    CaValueArrayFactory* pCaValueArrayFactory,
    DomainAssembly* pDomainAssembly)
{
    WRAPPER_NO_CONTRACT;

    HRESULT     hr = S_OK;              // A result.
    ULONG       ix;                     // Loop control.

    // If there is a blob, check the prolog.
    if (FAILED(ca.ValidateProlog()))
    {
        IfFailGo(PostError(META_E_CA_INVALID_BLOB));
    }

    // For each expected arg...
    for (ix=0; ix<cArgs; ++ix)
    {
        CaArg* pArg = &pArgs[ix];
        IfFailGo(ParseCaValue(ca, &pArg->val, &pArg->type, pCaValueArrayFactory, pDomainAssembly));
    }

ErrExit:
    return hr;
}

//---------------------------------------------------------------------------------------
//
// Because ParseKnowCaNamedArgs MD cannot have VM dependency, we have our own implementation here:
//   1. It needs to load the assemblies that contain the enum types for the named arguments,
//   2. It Compares the enum type name with that of the loaded enum type, not the one in the CA record.
//

static HRESULT ParseCaNamedArgs(
    CustomAttributeParser &ca,
    CaNamedArg *pNamedParams,
    ULONG cNamedParams,
    CaValueArrayFactory* pCaValueArrayFactory,
    DomainAssembly* pDomainAssembly)
{
    CONTRACTL {
        PRECONDITION(CheckPointer(pCaValueArrayFactory));
        PRECONDITION(CheckPointer(pDomainAssembly));
        THROWS;
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    ULONG ixParam;
    INT32 ixArg;
    INT16 cActualArgs;
    CaNamedArgCtor namedArg;
    CaNamedArg* pNamedParam;

    // Get actual count of named arguments.
    if (FAILED(ca.GetI2(&cActualArgs)))
        cActualArgs = 0; // Everett behavior

    for (ixParam = 0; ixParam < cNamedParams; ixParam++)
        pNamedParams[ixParam].val.type.tag = SERIALIZATION_TYPE_UNDEFINED;

    // For each named argument...
    for (ixArg = 0; ixArg < cActualArgs; ixArg++)
    {
        // Field or property?
        IfFailGo(ca.GetTag(&namedArg.propertyOrField));
        if (namedArg.propertyOrField != SERIALIZATION_TYPE_FIELD && namedArg.propertyOrField != SERIALIZATION_TYPE_PROPERTY)
            IfFailGo(PostError(META_E_CA_INVALID_ARGTYPE));

        // Get argument type information
        CaType* pNamedArgType = &namedArg.type;
        StackSString ss;
        IfFailGo(ParseCaType(ca, pNamedArgType, pDomainAssembly, &ss));

        LPCSTR szLoadedEnumName = NULL;

        if (pNamedArgType->tag == SERIALIZATION_TYPE_ENUM ||
            (pNamedArgType->tag == SERIALIZATION_TYPE_SZARRAY && pNamedArgType->arrayType == SERIALIZATION_TYPE_ENUM ))
        {
            szLoadedEnumName = ss.GetUTF8();
        }

        // Get name of Arg.
        if (FAILED(ca.GetNonEmptyString(&namedArg.szName, &namedArg.cName)))
            IfFailGo(PostError(META_E_CA_INVALID_BLOB));

        // Match arg by name and type
        for (ixParam = 0; ixParam < cNamedParams; ixParam++)
        {
            pNamedParam = &pNamedParams[ixParam];

            // Match type
            if (pNamedParam->type.tag != SERIALIZATION_TYPE_TAGGED_OBJECT)
            {
                if (namedArg.type.tag != pNamedParam->type.tag)
                    continue;

                // Match array type
                if (namedArg.type.tag == SERIALIZATION_TYPE_SZARRAY &&
                    pNamedParam->type.arrayType != SERIALIZATION_TYPE_TAGGED_OBJECT &&
                    namedArg.type.arrayType != pNamedParam->type.arrayType)
                    continue;
            }

            // Match name (and its length to avoid substring matching)
            if ((pNamedParam->cName != namedArg.cName) ||
                (strncmp(pNamedParam->szName, namedArg.szName, namedArg.cName) != 0))
            {
                continue;
            }

            // If enum, match enum name.
            if (pNamedParam->type.tag == SERIALIZATION_TYPE_ENUM ||
                (pNamedParam->type.tag == SERIALIZATION_TYPE_SZARRAY && pNamedParam->type.arrayType == SERIALIZATION_TYPE_ENUM ))
            {
                // pNamedParam->type.szEnumName: module->CA record->ctor token->loaded type->field/property->field/property type->field/property type name
                // namedArg.type.szEnumName:     module->CA record->named arg->enum type name
                // szLoadedEnumName:             module->CA record->named arg->enum type name->loaded enum type->loaded enum type name

                // Comparing pNamedParam->type.szEnumName against namedArg.type.szEnumName could fail if we loaded a different version
                // of the enum type than the one specified in the CA record. So we are comparing it against szLoadedEnumName instead.
                if (strncmp(pNamedParam->type.szEnumName, szLoadedEnumName, pNamedParam->type.cEnumName) != 0)
                    continue;

                if (namedArg.type.enumType != pNamedParam->type.enumType)
                {
                    IfFailGo(PostError(META_E_CA_UNEXPECTED_TYPE, pNamedParam->type.cEnumName, pNamedParam->type.szEnumName));
                }

                // TODO: For now assume the property\field array size is correct - later we should verify this
            }

            // Found a match.
            break;
        }

        // Better have found an argument.
        if (ixParam == cNamedParams)
        {
            IfFailGo(PostError(META_E_CA_UNKNOWN_ARGUMENT, namedArg.cName, namedArg.szName));
        }

        // Argument had better not have been seen already.
        if (pNamedParams[ixParam].val.type.tag != SERIALIZATION_TYPE_UNDEFINED)
        {
            IfFailGo(PostError(META_E_CA_REPEATED_ARG, namedArg.cName, namedArg.szName));
        }

        IfFailGo(ParseCaValue(ca, &pNamedParams[ixParam].val, &namedArg.type, pCaValueArrayFactory, pDomainAssembly));
    }

ErrExit:
    return hr;
}

/*static*/
HRESULT Attribute::ParseAttributeArgumentValues(
    void* pCa,
    INT32 cCa,
    CaValueArrayFactory* pCaValueArrayFactory,
    CaArg* pCaArgs,
    COUNT_T cArgs,
    CaNamedArg* pCaNamedArgs,
    COUNT_T cNamedArgs,
    DomainAssembly* pDomainAssembly)
{
    WRAPPER_NO_CONTRACT;

    HRESULT hr = S_OK;
    CustomAttributeParser cap(pCa, cCa);

    IfFailGo(ParseCaCtorArgs(cap, pCaArgs, cArgs, pCaValueArrayFactory, pDomainAssembly));
    IfFailGo(ParseCaNamedArgs(cap, pCaNamedArgs, cNamedArgs, pCaValueArrayFactory, pDomainAssembly));

ErrExit:
    return hr;
}

FCIMPL6(LPVOID, COMCustomAttribute::CreateCaObject, ReflectModuleBaseObject* pAttributedModuleUNSAFE, ReflectClassBaseObject* pCaTypeUNSAFE, ReflectMethodObject *pMethodUNSAFE, BYTE** ppBlob, BYTE* pEndBlob, INT32* pcNamedArgs)
{
    FCALL_CONTRACT;

    struct
    {
        REFLECTCLASSBASEREF refCaType;
        OBJECTREF ca;
        REFLECTMETHODREF refCtor;
        REFLECTMODULEBASEREF refAttributedModule;
    } gc;
    gc.refCaType = (REFLECTCLASSBASEREF)ObjectToOBJECTREF(pCaTypeUNSAFE);
    TypeHandle th = gc.refCaType->GetType();

    gc.ca = NULL;
    gc.refCtor = (REFLECTMETHODREF)ObjectToOBJECTREF(pMethodUNSAFE);
    gc.refAttributedModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pAttributedModuleUNSAFE);

    if(gc.refAttributedModule == NULL)
        FCThrowRes(kArgumentNullException, W("Arg_InvalidHandle"));

    MethodDesc* pCtorMD = gc.refCtor->GetMethod();

    HELPER_METHOD_FRAME_BEGIN_RET_PROTECT(gc);
    {
        MethodDescCallSite ctorCallSite(pCtorMD, th);
        MetaSig* pSig = ctorCallSite.GetMetaSig();
        BYTE* pBlob = *ppBlob;

        // get the number of arguments and allocate an array for the args
        ARG_SLOT *args = NULL;
        UINT cArgs = pSig->NumFixedArgs() + 1; // make room for the this pointer
        UINT i = 1; // used to flag that we actually get the right number of arg from the blob

        args = (ARG_SLOT*)_alloca(cArgs * sizeof(ARG_SLOT));
        memset((void*)args, 0, cArgs * sizeof(ARG_SLOT));

        OBJECTREF *argToProtect = (OBJECTREF*)_alloca(cArgs * sizeof(OBJECTREF));
        memset((void*)argToProtect, 0, cArgs * sizeof(OBJECTREF));

        // load the this pointer
        argToProtect[0] = gc.refCaType->GetType().GetMethodTable()->Allocate(); // this is the value to return after the ctor invocation

        if (pBlob)
        {
            if (pBlob < pEndBlob)
            {
                if (pBlob + 2 > pEndBlob)
                {
                    COMPlusThrow(kCustomAttributeFormatException);
                }
                INT16 prolog = GET_UNALIGNED_VAL16(pBlob);
                if (prolog != 1)
                    COMPlusThrow(kCustomAttributeFormatException);
                pBlob += 2;
            }

            if (cArgs > 1)
            {
                GCPROTECT_ARRAY_BEGIN(*argToProtect, cArgs);
                {
                    // loop through the args
                    for (i = 1; i < cArgs; i++) {
                        CorElementType type = pSig->NextArg();
                        if (type == ELEMENT_TYPE_END)
                            break;
                        BOOL bObjectCreated = FALSE;
                        TypeHandle th = pSig->GetLastTypeHandleThrowing();
                        if (th.IsArray())
                            // get the array element
                            th = th.GetArrayElementTypeHandle();
                        ARG_SLOT data = GetDataFromBlob(pCtorMD->GetAssembly(), (CorSerializationType)type, th, &pBlob, pEndBlob, gc.refAttributedModule->GetModule(), &bObjectCreated);
                        if (bObjectCreated)
                            argToProtect[i] = ArgSlotToObj(data);
                        else
                            args[i] = data;
                    }
                }
                GCPROTECT_END();

                // We have borrowed the signature from MethodDescCallSite. We have to put it back into the initial position
                // because of that's where MethodDescCallSite expects to find it below.
                pSig->Reset();

                for (i = 1; i < cArgs; i++)
                {
                    if (argToProtect[i] != NULL)
                    {
                        _ASSERTE(args[i] == NULL);
                        args[i] = ObjToArgSlot(argToProtect[i]);
                    }
                }
            }
        }
        args[0] = ObjToArgSlot(argToProtect[0]);

        if (i != cArgs)
            COMPlusThrow(kCustomAttributeFormatException);

        // check if there are any named properties to invoke,
        // if so set the by ref int passed in to point
        // to the blob position where name properties start
        *pcNamedArgs = 0;

        if (pBlob && pBlob != pEndBlob)
        {
            if (pBlob + 2 > pEndBlob)
                COMPlusThrow(kCustomAttributeFormatException);

            *pcNamedArgs = GET_UNALIGNED_VAL16(pBlob);

            pBlob += 2;
        }

        *ppBlob = pBlob;

        if (*pcNamedArgs == 0 && pBlob != pEndBlob)
            COMPlusThrow(kCustomAttributeFormatException);

        // make the invocation to the ctor
        gc.ca = ArgSlotToObj(args[0]);
        if (pCtorMD->GetMethodTable()->IsValueType())
            args[0] = PtrToArgSlot(OBJECTREFToObject(gc.ca)->UnBox());

        ctorCallSite.CallWithValueTypes(args);
    }
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(gc.ca);
}
FCIMPLEND

FCIMPL5(VOID, COMCustomAttribute::ParseAttributeUsageAttribute, PVOID pData, ULONG cData, ULONG* pTargets, CLR_BOOL* pInherited, CLR_BOOL* pAllowMultiple)
{
    FCALL_CONTRACT;

    int inherited = 0;
    int allowMultiple = 1;

    {
        CustomAttributeParser ca(pData, cData);

        CaArg args[1];
        args[0].InitEnum(SERIALIZATION_TYPE_I4, 0);
        if (FAILED(::ParseKnownCaArgs(ca, args, ARRAY_SIZE(args))))
        {
            HELPER_METHOD_FRAME_BEGIN_0();
            COMPlusThrow(kCustomAttributeFormatException);
            HELPER_METHOD_FRAME_END();
        }

        *pTargets = args[0].val.u4;

        CaNamedArg namedArgs[2];
        CaType namedArgTypes[2];
        namedArgTypes[inherited].Init(SERIALIZATION_TYPE_BOOLEAN);
        namedArgTypes[allowMultiple].Init(SERIALIZATION_TYPE_BOOLEAN);
        namedArgs[inherited].Init("Inherited", SERIALIZATION_TYPE_PROPERTY, namedArgTypes[inherited], TRUE);
        namedArgs[allowMultiple].Init("AllowMultiple", SERIALIZATION_TYPE_PROPERTY, namedArgTypes[allowMultiple], FALSE);
        if (FAILED(::ParseKnownCaNamedArgs(ca, namedArgs, ARRAY_SIZE(namedArgs))))
        {
            HELPER_METHOD_FRAME_BEGIN_0();
            COMPlusThrow(kCustomAttributeFormatException);
            HELPER_METHOD_FRAME_END();
        }

        *pInherited = namedArgs[inherited].val.boolean == TRUE;
        *pAllowMultiple = namedArgs[allowMultiple].val.boolean == TRUE;
    }
}
FCIMPLEND


FCIMPL7(void, COMCustomAttribute::GetPropertyOrFieldData, ReflectModuleBaseObject *pModuleUNSAFE, BYTE** ppBlobStart, BYTE* pBlobEnd, STRINGREF* pName, CLR_BOOL* pbIsProperty, OBJECTREF* pType, OBJECTREF* value)
{
    FCALL_CONTRACT;

    BYTE* pBlob = *ppBlobStart;
    *pType = NULL;

    REFLECTMODULEBASEREF refModule = (REFLECTMODULEBASEREF)ObjectToOBJECTREF(pModuleUNSAFE);

    if(refModule == NULL)
        FCThrowResVoid(kArgumentNullException, W("Arg_InvalidHandle"));

    Module *pModule = refModule->GetModule();

    HELPER_METHOD_FRAME_BEGIN_1(refModule);
    {
        Assembly *pCtorAssembly = NULL;

        MethodTable *pMTValue = NULL;
        CorSerializationType arrayType = SERIALIZATION_TYPE_BOOLEAN;
        BOOL bObjectCreated = FALSE;
        TypeHandle nullTH;

        if (pBlob + 2 > pBlobEnd)
            COMPlusThrow(kCustomAttributeFormatException);

        // get whether it is a field or a property
        CorSerializationType propOrField = (CorSerializationType)*pBlob;
        pBlob++;
        if (propOrField == SERIALIZATION_TYPE_FIELD)
            *pbIsProperty = FALSE;
        else if (propOrField == SERIALIZATION_TYPE_PROPERTY)
            *pbIsProperty = TRUE;
        else
            COMPlusThrow(kCustomAttributeFormatException);

        // get the type of the field
        CorSerializationType fieldType = (CorSerializationType)*pBlob;
        pBlob++;
        if (fieldType == SERIALIZATION_TYPE_SZARRAY)
        {
            arrayType = (CorSerializationType)*pBlob;

            if (pBlob + 1 > pBlobEnd)
                COMPlusThrow(kCustomAttributeFormatException);

            pBlob++;
        }
        if (fieldType == SERIALIZATION_TYPE_ENUM || arrayType == SERIALIZATION_TYPE_ENUM)
        {
            // get the enum type
            ReflectClassBaseObject *pEnum =
                (ReflectClassBaseObject*)OBJECTREFToObject(ArgSlotToObj(GetDataFromBlob(
                    pCtorAssembly, SERIALIZATION_TYPE_TYPE, nullTH, &pBlob, pBlobEnd, pModule, &bObjectCreated)));

            if (pEnum == NULL)
                COMPlusThrow(kCustomAttributeFormatException);

            _ASSERTE(bObjectCreated);

            TypeHandle th = pEnum->GetType();
            _ASSERTE(th.IsEnum());

            pMTValue = th.AsMethodTable();
            if (fieldType == SERIALIZATION_TYPE_ENUM)
                // load the enum type to pass it back
                *pType = th.GetManagedClassObject();
            else
                nullTH = th;
        }

        // get the string representing the field/property name
        *pName = ArgSlotToString(GetDataFromBlob(
            pCtorAssembly, SERIALIZATION_TYPE_STRING, nullTH, &pBlob, pBlobEnd, pModule, &bObjectCreated));
        _ASSERTE(bObjectCreated || *pName == NULL);

        // create the object and return it
        switch (fieldType)
        {
            case SERIALIZATION_TYPE_TAGGED_OBJECT:
                *pType = g_pObjectClass->GetManagedClassObject();
                FALLTHROUGH;
            case SERIALIZATION_TYPE_TYPE:
            case SERIALIZATION_TYPE_STRING:
                *value = ArgSlotToObj(GetDataFromBlob(
                    pCtorAssembly, fieldType, nullTH, &pBlob, pBlobEnd, pModule, &bObjectCreated));
                _ASSERTE(bObjectCreated || *value == NULL);

                if (*value == NULL)
                {
                    // load the proper type so that code in managed knows which property to load
                    if (fieldType == SERIALIZATION_TYPE_STRING)
                        *pType = CoreLibBinder::GetElementType(ELEMENT_TYPE_STRING)->GetManagedClassObject();
                    else if (fieldType == SERIALIZATION_TYPE_TYPE)
                        *pType = CoreLibBinder::GetClass(CLASS__TYPE)->GetManagedClassObject();
                }
                break;
            case SERIALIZATION_TYPE_SZARRAY:
            {
                *value = NULL;
                int arraySize = (int)GetDataFromBlob(pCtorAssembly, SERIALIZATION_TYPE_I4, nullTH, &pBlob, pBlobEnd, pModule, &bObjectCreated);

                if (arraySize != -1)
                {
                    _ASSERTE(!bObjectCreated);
                    if (arrayType == SERIALIZATION_TYPE_STRING)
                        nullTH = TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_STRING));
                    else if (arrayType == SERIALIZATION_TYPE_TYPE)
                        nullTH = TypeHandle(CoreLibBinder::GetClass(CLASS__TYPE));
                    else if (arrayType == SERIALIZATION_TYPE_TAGGED_OBJECT)
                        nullTH = TypeHandle(g_pObjectClass);
                    ReadArray(pCtorAssembly, arrayType, arraySize, nullTH, &pBlob, pBlobEnd, pModule, (BASEARRAYREF*)value);
                }
                if (*value == NULL)
                {
                    TypeHandle arrayTH;
                    switch (arrayType)
                    {
                        case SERIALIZATION_TYPE_STRING:
                            arrayTH = TypeHandle(CoreLibBinder::GetElementType(ELEMENT_TYPE_STRING));
                            break;
                        case SERIALIZATION_TYPE_TYPE:
                            arrayTH = TypeHandle(CoreLibBinder::GetClass(CLASS__TYPE));
                            break;
                        case SERIALIZATION_TYPE_TAGGED_OBJECT:
                            arrayTH = TypeHandle(g_pObjectClass);
                            break;
                        default:
                            if (SERIALIZATION_TYPE_BOOLEAN <= arrayType && arrayType <= SERIALIZATION_TYPE_R8)
                                arrayTH = TypeHandle(CoreLibBinder::GetElementType((CorElementType)arrayType));
                    }
                    if (!arrayTH.IsNull())
                    {
                        arrayTH = ClassLoader::LoadArrayTypeThrowing(arrayTH);
                        *pType = arrayTH.GetManagedClassObject();
                    }
                }
                break;
            }
            default:
                if (SERIALIZATION_TYPE_BOOLEAN <= fieldType && fieldType <= SERIALIZATION_TYPE_R8)
                    pMTValue = CoreLibBinder::GetElementType((CorElementType)fieldType);
                else if(fieldType == SERIALIZATION_TYPE_ENUM)
                    fieldType = (CorSerializationType)pMTValue->GetInternalCorElementType();
                else
                    COMPlusThrow(kCustomAttributeFormatException);

                ARG_SLOT val = GetDataFromBlob(pCtorAssembly, fieldType, nullTH, &pBlob, pBlobEnd, pModule, &bObjectCreated);
                _ASSERTE(!bObjectCreated);

                *value = pMTValue->Box((void*)ArgSlotEndiannessFixup(&val, pMTValue->GetNumInstanceFieldBytes()));
        }

        *ppBlobStart = pBlob;
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

/*static*/
TypeHandle COMCustomAttribute::GetTypeHandleFromBlob(Assembly *pCtorAssembly,
                                    CorSerializationType objType,
                                    BYTE **pBlob,
                                    const BYTE *endBlob,
                                    Module *pModule)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    // we must box which means we must get the method table, switch again on the element type
    MethodTable *pMTType = NULL;
    TypeHandle nullTH;
    TypeHandle RtnTypeHnd;

    switch ((DWORD)objType) {
    case SERIALIZATION_TYPE_BOOLEAN:
    case SERIALIZATION_TYPE_I1:
    case SERIALIZATION_TYPE_U1:
    case SERIALIZATION_TYPE_CHAR:
    case SERIALIZATION_TYPE_I2:
    case SERIALIZATION_TYPE_U2:
    case SERIALIZATION_TYPE_I4:
    case SERIALIZATION_TYPE_U4:
    case SERIALIZATION_TYPE_R4:
    case SERIALIZATION_TYPE_I8:
    case SERIALIZATION_TYPE_U8:
    case SERIALIZATION_TYPE_R8:
    case SERIALIZATION_TYPE_STRING:
        pMTType = CoreLibBinder::GetElementType((CorElementType)objType);
        RtnTypeHnd = TypeHandle(pMTType);
        break;

    case ELEMENT_TYPE_CLASS:
        pMTType = CoreLibBinder::GetClass(CLASS__TYPE);
        RtnTypeHnd = TypeHandle(pMTType);
        break;

    case SERIALIZATION_TYPE_TAGGED_OBJECT:
        pMTType = g_pObjectClass;
        RtnTypeHnd = TypeHandle(pMTType);
        break;

    case SERIALIZATION_TYPE_TYPE:
    {
        int size = GetStringSize(pBlob, endBlob);
        if (size == -1)
            return nullTH;

        if ((size+1 <= 1) || (size > endBlob - *pBlob))
            COMPlusThrow(kCustomAttributeFormatException);

        LPUTF8 szName = (LPUTF8)_alloca(size + 1);
        memcpy(szName, *pBlob, size);
        *pBlob += size;
        szName[size] = 0;

        RtnTypeHnd = TypeName::GetTypeReferencedByCustomAttribute(szName, pModule->GetAssembly());
        break;
    }

    case SERIALIZATION_TYPE_ENUM:
    {
        // get the enum type
        BOOL isObject = FALSE;
        ReflectClassBaseObject *pType = (ReflectClassBaseObject*)OBJECTREFToObject(ArgSlotToObj(GetDataFromBlob(pCtorAssembly,
                                                                                                                SERIALIZATION_TYPE_TYPE,
                                                                                                                nullTH,
                                                                                                                pBlob,
                                                                                                                endBlob,
                                                                                                                pModule,
                                                                                                                &isObject)));
        if (pType != NULL)
        {
            _ASSERTE(isObject);
            RtnTypeHnd = pType->GetType();
            _ASSERTE((objType == SERIALIZATION_TYPE_ENUM) ? RtnTypeHnd.GetMethodTable()->IsEnum() : TRUE);
        }
        else
        {
            RtnTypeHnd = TypeHandle();
        }
        break;
    }

    default:
        COMPlusThrow(kCustomAttributeFormatException);
    }

    return RtnTypeHnd;
}

// retrieve the string size in a CA blob. Advance the blob pointer to point to
// the beginning of the string immediately following the size
/*static*/
int COMCustomAttribute::GetStringSize(BYTE **pBlob, const BYTE *endBlob)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    if (*pBlob >= endBlob )
    {   // No buffer at all, or buffer overrun
        COMPlusThrow(kCustomAttributeFormatException);
    }

    if (**pBlob == 0xFF)
    {   // Special case null string.
        ++(*pBlob);
        return -1;
    }

    ULONG ulSize;
    if (FAILED(CPackedLen::SafeGetData((BYTE const *)*pBlob, (BYTE const *)endBlob, (ULONG *)&ulSize, (BYTE const **)pBlob)))
    {
        COMPlusThrow(kCustomAttributeFormatException);
    }

    return (int)ulSize;
}

// copy the values of an array of integers from a CA blob
// (i.e., always stored in little-endian, and needs not be aligned).
// Returns TRUE on success, FALSE if the blob was not big enough.
// Advances *pBlob by the amount copied.
/*static*/
template < typename T >
BOOL COMCustomAttribute::CopyArrayVAL(BASEARRAYREF pArray, int nElements, BYTE **pBlob, const BYTE *endBlob)
{
    int sizeData;   // = size * 2; with integer overflow check
    if (!ClrSafeInt<int>::multiply(nElements, sizeof(T), sizeData))
        return FALSE;
    if (sizeData > endBlob - *pBlob)     // integer overflow check
        return FALSE;
#if BIGENDIAN
    T *ptDest = reinterpret_cast<T *>(pArray->GetDataPtr());
    for (int iElement = 0; iElement < nElements; iElement++)
    {
        T tValue;
        BYTE *pbSrc = *pBlob + iElement * sizeof(T);
        BYTE *pbDest = reinterpret_cast<BYTE *>(&tValue);
        for (size_t iByte = 0; iByte < sizeof(T); iByte++)
        {
            pbDest[sizeof(T) - 1 - iByte] = pbSrc[iByte];
        }
        ptDest[iElement] = tValue;
    }
#else // BIGENDIAN
    memcpyNoGCRefs(pArray->GetDataPtr(), *pBlob, sizeData);
#endif // BIGENDIAN
    *pBlob += sizeData;
    return TRUE;
}

// read the whole array as a chunk
/*static*/
void COMCustomAttribute::ReadArray(Assembly *pCtorAssembly,
               CorSerializationType arrayType,
               int size,
               TypeHandle th,
               BYTE **pBlob,
               const BYTE *endBlob,
               Module *pModule,
               BASEARRAYREF *pArray)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    ARG_SLOT element = 0;

    switch ((DWORD)arrayType) {
    case SERIALIZATION_TYPE_BOOLEAN:
    case SERIALIZATION_TYPE_I1:
    case SERIALIZATION_TYPE_U1:
        *pArray = (BASEARRAYREF)AllocatePrimitiveArray((CorElementType)arrayType, size);
        if (!CopyArrayVAL<BYTE>(*pArray, size, pBlob, endBlob))
            goto badBlob;
        break;

    case SERIALIZATION_TYPE_CHAR:
    case SERIALIZATION_TYPE_I2:
    case SERIALIZATION_TYPE_U2:
    {
        *pArray = (BASEARRAYREF)AllocatePrimitiveArray((CorElementType)arrayType, size);
        if (!CopyArrayVAL<UINT16>(*pArray, size, pBlob, endBlob))
            goto badBlob;
        break;
    }
    case SERIALIZATION_TYPE_I4:
    case SERIALIZATION_TYPE_U4:
    case SERIALIZATION_TYPE_R4:
    {
        *pArray = (BASEARRAYREF)AllocatePrimitiveArray((CorElementType)arrayType, size);
        if (!CopyArrayVAL<UINT32>(*pArray, size, pBlob, endBlob))
            goto badBlob;
        break;
    }
    case SERIALIZATION_TYPE_I8:
    case SERIALIZATION_TYPE_U8:
    case SERIALIZATION_TYPE_R8:
    {
        *pArray = (BASEARRAYREF)AllocatePrimitiveArray((CorElementType)arrayType, size);
        if (!CopyArrayVAL<UINT64>(*pArray, size, pBlob, endBlob))
            goto badBlob;
        break;
    }
    case ELEMENT_TYPE_CLASS:
    case SERIALIZATION_TYPE_TYPE:
    case SERIALIZATION_TYPE_STRING:
    case SERIALIZATION_TYPE_SZARRAY:
    case SERIALIZATION_TYPE_TAGGED_OBJECT:
    {
        BOOL isObject;

        // If we haven't figured out the type of the array, throw bad blob exception
        if (th.IsNull())
            goto badBlob;

        *pArray = (BASEARRAYREF)AllocateObjectArray(size, th);
        if (arrayType == SERIALIZATION_TYPE_SZARRAY)
            // switch the th to be the proper one
            th = th.GetArrayElementTypeHandle();
        for (int i = 0; i < size; i++) {
            element = GetDataFromBlob(pCtorAssembly, arrayType, th, pBlob, endBlob, pModule, &isObject);
            _ASSERTE(isObject || element == NULL);
            ((PTRARRAYREF)(*pArray))->SetAt(i, ArgSlotToObj(element));
        }
        break;
    }

    case SERIALIZATION_TYPE_ENUM:
    {
        INT32 bounds = size;

        // If we haven't figured out the type of the array, throw bad blob exception
        if (th.IsNull())
            goto badBlob;

        unsigned elementSize = th.GetSize();
        TypeHandle arrayHandle = ClassLoader::LoadArrayTypeThrowing(th);
        if (arrayHandle.IsNull())
            goto badBlob;
        *pArray = (BASEARRAYREF)AllocateSzArray(arrayHandle, bounds);
        BOOL fSuccess;
        switch (elementSize)
        {
        case 1:
            fSuccess = CopyArrayVAL<BYTE>(*pArray, size, pBlob, endBlob);
            break;
        case 2:
            fSuccess = CopyArrayVAL<UINT16>(*pArray, size, pBlob, endBlob);
            break;
        case 4:
            fSuccess = CopyArrayVAL<UINT32>(*pArray, size, pBlob, endBlob);
            break;
        case 8:
            fSuccess = CopyArrayVAL<UINT64>(*pArray, size, pBlob, endBlob);
            break;
        default:
            fSuccess = FALSE;
        }
        if (!fSuccess)
            goto badBlob;
        break;
    }

    default:
    badBlob:
        COMPlusThrow(kCustomAttributeFormatException);
    }

}

// get data out of the blob according to a CorElementType
/*static*/
ARG_SLOT COMCustomAttribute::GetDataFromBlob(Assembly *pCtorAssembly,
                      CorSerializationType type,
                      TypeHandle th,
                      BYTE **pBlob,
                      const BYTE *endBlob,
                      Module *pModule,
                      BOOL *bObjectCreated)
{
    CONTRACTL
    {
        THROWS;
    }
    CONTRACTL_END;

    ARG_SLOT retValue = 0;
    *bObjectCreated = FALSE;
    TypeHandle nullTH;
    TypeHandle typeHnd;

    switch ((DWORD)type) {

    case SERIALIZATION_TYPE_BOOLEAN:
    case SERIALIZATION_TYPE_I1:
    case SERIALIZATION_TYPE_U1:
        if (*pBlob + 1 <= endBlob) {
            retValue = (ARG_SLOT)**pBlob;
            *pBlob += 1;
            break;
        }
        goto badBlob;

    case SERIALIZATION_TYPE_CHAR:
    case SERIALIZATION_TYPE_I2:
    case SERIALIZATION_TYPE_U2:
        if (*pBlob + 2 <= endBlob) {
            retValue = (ARG_SLOT)GET_UNALIGNED_VAL16(*pBlob);
            *pBlob += 2;
            break;
        }
        goto badBlob;

    case SERIALIZATION_TYPE_I4:
    case SERIALIZATION_TYPE_U4:
    case SERIALIZATION_TYPE_R4:
        if (*pBlob + 4 <= endBlob) {
            retValue = (ARG_SLOT)GET_UNALIGNED_VAL32(*pBlob);
            *pBlob += 4;
            break;
        }
        goto badBlob;

    case SERIALIZATION_TYPE_I8:
    case SERIALIZATION_TYPE_U8:
    case SERIALIZATION_TYPE_R8:
        if (*pBlob + 8 <= endBlob) {
            retValue = (ARG_SLOT)GET_UNALIGNED_VAL64(*pBlob);
            *pBlob += 8;
            break;
        }
        goto badBlob;

    case SERIALIZATION_TYPE_STRING:
    stringType:
    {
        int size = GetStringSize(pBlob, endBlob);
        *bObjectCreated = TRUE;
        if (size > 0) {
            if (size > endBlob - *pBlob)     // integer overflow check
                goto badBlob;
            retValue = ObjToArgSlot(StringObject::NewString((LPCUTF8)*pBlob, size));
            *pBlob += size;
        }
        else if (size == 0)
            retValue = ObjToArgSlot(StringObject::NewString(0));
        else
            *bObjectCreated = FALSE;

        break;
    }

    // this is coming back from sig but it's not a serialization type,
    // essentialy the type in the blob and the type in the sig don't match
    case ELEMENT_TYPE_VALUETYPE:
    {
        if (!th.IsEnum())
            goto badBlob;
        CorSerializationType enumType = (CorSerializationType)th.GetInternalCorElementType();
        BOOL cannotBeObject = FALSE;
        retValue = GetDataFromBlob(pCtorAssembly, enumType, nullTH, pBlob, endBlob, pModule, &cannotBeObject);
        _ASSERTE(!cannotBeObject);
        break;
    }

    // this is coming back from sig but it's not a serialization type,
    // essentialy the type in the blob and the type in the sig don't match
    case ELEMENT_TYPE_CLASS:
        if (th.IsArray())
            goto typeArray;
        else {
            MethodTable *pMT = th.AsMethodTable();
            if (pMT == g_pStringClass)
                goto stringType;
            else if (pMT == g_pObjectClass)
                goto typeObject;
            else if (CoreLibBinder::IsClass(pMT, CLASS__TYPE))
                goto typeType;
        }

        goto badBlob;

    case SERIALIZATION_TYPE_TYPE:
    typeType:
    {
        typeHnd = GetTypeHandleFromBlob(pCtorAssembly, SERIALIZATION_TYPE_TYPE, pBlob, endBlob, pModule);
        if (!typeHnd.IsNull())
            retValue = ObjToArgSlot(typeHnd.GetManagedClassObject());
        *bObjectCreated = TRUE;
        break;
    }

    // this is coming back from sig but it's not a serialization type,
    // essentialy the type in the blob and the type in the sig don't match
    case ELEMENT_TYPE_OBJECT:
    case SERIALIZATION_TYPE_TAGGED_OBJECT:
    typeObject:
    {
        // get the byte representing the real type and call GetDataFromBlob again
        if (*pBlob + 1 > endBlob)
            goto badBlob;
        CorSerializationType objType = (CorSerializationType)**pBlob;
        *pBlob += 1;
        switch (objType) {
        case SERIALIZATION_TYPE_SZARRAY:
        {
            if (*pBlob + 1 > endBlob)
                goto badBlob;
            CorSerializationType arrayType = (CorSerializationType)**pBlob;
            *pBlob += 1;
            if (arrayType == SERIALIZATION_TYPE_TYPE)
                arrayType = (CorSerializationType)ELEMENT_TYPE_CLASS;
            // grab the array type and make a type handle for it
            nullTH = GetTypeHandleFromBlob(pCtorAssembly, arrayType, pBlob, endBlob, pModule);
            FALLTHROUGH;
        }
        case SERIALIZATION_TYPE_TYPE:
        case SERIALIZATION_TYPE_STRING:
            // notice that the nullTH is actually not null in the array case (see case above)
            retValue = GetDataFromBlob(pCtorAssembly, objType, nullTH, pBlob, endBlob, pModule, bObjectCreated);
            _ASSERTE(*bObjectCreated || retValue == 0);
            break;
        case SERIALIZATION_TYPE_ENUM:
        {
            //
            // get the enum type
            typeHnd = GetTypeHandleFromBlob(pCtorAssembly, SERIALIZATION_TYPE_ENUM, pBlob, endBlob, pModule);
            _ASSERTE(typeHnd.IsTypeDesc() == false);

            // ok we have the class, now we go and read the data
            MethodTable *pMT = typeHnd.AsMethodTable();
            PREFIX_ASSUME(pMT != NULL);
            CorSerializationType objNormType = (CorSerializationType)pMT->GetInternalCorElementType();
            BOOL isObject = FALSE;
            retValue = GetDataFromBlob(pCtorAssembly, objNormType, nullTH, pBlob, endBlob, pModule, &isObject);
            _ASSERTE(!isObject);
            retValue= ObjToArgSlot(pMT->Box((void*)&retValue));
            *bObjectCreated = TRUE;
            break;
        }
        default:
        {
            // the common primitive type case. We need to box the primitive
            typeHnd = GetTypeHandleFromBlob(pCtorAssembly, objType, pBlob, endBlob, pModule);
            _ASSERTE(typeHnd.IsTypeDesc() == false);
            retValue = GetDataFromBlob(pCtorAssembly, objType, nullTH, pBlob, endBlob, pModule, bObjectCreated);
            _ASSERTE(!*bObjectCreated);
            retValue= ObjToArgSlot(typeHnd.AsMethodTable()->Box((void*)&retValue));
            *bObjectCreated = TRUE;
            break;
        }
        }
        break;
    }

    case SERIALIZATION_TYPE_SZARRAY:
    typeArray:
    {
        // read size
        BOOL isObject = FALSE;
        int size = (int)GetDataFromBlob(pCtorAssembly, SERIALIZATION_TYPE_I4, nullTH, pBlob, endBlob, pModule, &isObject);
        _ASSERTE(!isObject);

        if (size != -1) {
            CorSerializationType arrayType;
            if (th.IsEnum())
                arrayType = SERIALIZATION_TYPE_ENUM;
            else
                arrayType = (CorSerializationType)th.GetInternalCorElementType();

            BASEARRAYREF array = NULL;
            GCPROTECT_BEGIN(array);
            ReadArray(pCtorAssembly, arrayType, size, th, pBlob, endBlob, pModule, &array);
            retValue = ObjToArgSlot(array);
            GCPROTECT_END();
        }
        *bObjectCreated = TRUE;
        break;
    }

    default:
    badBlob:
        //<TODO> generate a reasonable text string ("invalid blob or constructor")</TODO>
        COMPlusThrow(kCustomAttributeFormatException);
    }

    return retValue;
}
