// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// typekey.h
//

// ---------------------------------------------------------------------------
//
// Support for type lookups based on components of the type (as opposed to string)
// Used in
// * Table of constructed types (Module::m_pAvailableParamTypes)
// * Types currently being loaded (ClassLoader::m_pUnresolvedClassHash)
//
// Type handles are in one-to-one correspondence with TypeKeys
// In particular, note that tokens in the key are resolved TypeDefs
//
// ---------------------------------------------------------------------------


#ifndef _H_TYPEKEY
#define _H_TYPEKEY

class TypeKey
{
    // ELEMENT_TYPE_CLASS for ordinary classes and generic instantiations (including value types)
    // ELEMENT_TYPE_ARRAY and ELEMENT_TYPE_SZARRAY for array types
    // ELEMENT_TYPE_PTR and ELEMENT_TYPE_BYREF for pointer types
    // ELEMENT_TYPE_FNPTR for function pointer types
    // ELEMENT_TYPE_VALUETYPE for native value types (used in IL stubs)
    CorElementType m_kind;

    union
    {
        // m_kind = CLASS
        struct
        {
            Module *       m_pModule;
            mdToken        m_typeDef;
            DWORD          m_numGenericArgs; // 0 for non-generic types
            TypeHandle *   m_pGenericArgs;   // NULL for non-generic types
            // Note that for DAC builds, m_pGenericArgs is a host allocated buffer (eg. by in SigPointer::GetTypeHandleThrowing),
            // not a copy of an object marshalled by DAC.
        } asClass;

        // m_kind = ARRAY, SZARRAY, PTR or BYREF
        struct
        {
            TADDR m_paramType;   // The element type (actually a TypeHandle, but we don't want its constructor
                                 // run on a C++ union)
            DWORD m_rank;        // Non-zero for ARRAY, 1 for SZARRAY, 0 for PTR or BYREF
        } asParamType;

        // m_kind = FNPTR
        struct
        {
            BYTE m_callConv;
            DWORD m_numArgs;
            TypeHandle* m_pRetAndArgTypes;
        } asFnPtr;
    } u;

public:

    // Constructor for BYREF/PTR/ARRAY/SZARRAY types
    TypeKey(CorElementType etype, TypeHandle paramType, DWORD rank = 0)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(rank > 0 && etype == ELEMENT_TYPE_ARRAY ||
                     rank == 1 && etype == ELEMENT_TYPE_SZARRAY ||
                     rank == 0 && (etype == ELEMENT_TYPE_PTR || etype == ELEMENT_TYPE_BYREF || etype == ELEMENT_TYPE_VALUETYPE));
        PRECONDITION(CheckPointer(paramType));
        m_kind = etype;
        u.asParamType.m_paramType = paramType.AsTAddr();
        u.asParamType.m_rank = rank;
    }

    // Constructor for instantiated types
    TypeKey(Module *pModule, mdTypeDef token, Instantiation inst = Instantiation())
    {
        WRAPPER_NO_CONTRACT;
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(TypeFromToken(token) == mdtTypeDef);
        PRECONDITION(!IsNilToken(token));
        m_kind = ELEMENT_TYPE_CLASS;
        u.asClass.m_pModule = pModule;
        u.asClass.m_typeDef = token;
        u.asClass.m_numGenericArgs = inst.GetNumArgs();
        u.asClass.m_pGenericArgs = inst.GetRawArgs();
    }

    // Constructor for function pointer type
    TypeKey(BYTE callConv, DWORD numArgs, TypeHandle* retAndArgTypes)
    {
        WRAPPER_NO_CONTRACT;
        PRECONDITION(CheckPointer(retAndArgTypes));
        m_kind = ELEMENT_TYPE_FNPTR;
        u.asFnPtr.m_callConv = callConv;
        u.asFnPtr.m_numArgs = numArgs;
        u.asFnPtr.m_pRetAndArgTypes = retAndArgTypes;
    }

    CorElementType GetKind() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return m_kind;
    }

    // Accessors on array/pointer types
    DWORD GetRank() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(CorTypeInfo::IsArray_NoThrow(m_kind));
        return u.asParamType.m_rank;
    }

    TypeHandle GetElementType() const
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(CorTypeInfo::IsModifier_NoThrow(m_kind) || m_kind == ELEMENT_TYPE_VALUETYPE);
        return TypeHandle::FromTAddr(u.asParamType.m_paramType);
    }

    BOOL IsConstructed() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return !(m_kind == ELEMENT_TYPE_CLASS && u.asClass.m_numGenericArgs == 0);
    }

    // Accessors on instantiated types
    PTR_Module GetModule() const
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;
        if (m_kind == ELEMENT_TYPE_CLASS)
            return PTR_Module(u.asClass.m_pModule);
        else if (CorTypeInfo::IsModifier_NoThrow(m_kind) || m_kind == ELEMENT_TYPE_VALUETYPE)
            return GetElementType().GetModule();
        else
            return NULL;
    }

    mdTypeDef GetTypeToken() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(m_kind == ELEMENT_TYPE_CLASS);
        return u.asClass.m_typeDef;
    }

    // Get the type parameters for this CLASS type.
    // This is an array (host-allocated in DAC builds) of length GetNumGenericArgs().
    Instantiation GetInstantiation() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(m_kind == ELEMENT_TYPE_CLASS);
        return Instantiation(u.asClass.m_pGenericArgs, u.asClass.m_numGenericArgs);
    }

    DWORD GetNumGenericArgs() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(m_kind == ELEMENT_TYPE_CLASS);
        return u.asClass.m_numGenericArgs;
    }

    BOOL HasInstantiation() const
    {
        LIMITED_METHOD_CONTRACT;
        return m_kind == ELEMENT_TYPE_CLASS && u.asClass.m_numGenericArgs != 0;
    }

    // Accessors on function pointer types
    DWORD GetNumArgs() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(m_kind == ELEMENT_TYPE_FNPTR);
        return u.asFnPtr.m_numArgs;
    }

    BYTE GetCallConv() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(m_kind == ELEMENT_TYPE_FNPTR);
        return u.asFnPtr.m_callConv;
    }

    TypeHandle* GetRetAndArgTypes() const
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        PRECONDITION(m_kind == ELEMENT_TYPE_FNPTR);
        return u.asFnPtr.m_pRetAndArgTypes;
    }

    BOOL Equals(TypeKey *pKey) const
    {
        WRAPPER_NO_CONTRACT;
        return TypeKey::Equals(this, pKey);
    }

    static BOOL Equals(const TypeKey *pKey1, const TypeKey *pKey2)
    {
        WRAPPER_NO_CONTRACT;
        if (pKey1->m_kind != pKey2->m_kind)
        {
            return FALSE;
        }
        if (pKey1->m_kind == ELEMENT_TYPE_CLASS)
        {
            if (pKey1->u.asClass.m_typeDef != pKey2->u.asClass.m_typeDef ||
                pKey1->u.asClass.m_pModule != pKey2->u.asClass.m_pModule ||
                pKey1->u.asClass.m_numGenericArgs != pKey2->u.asClass.m_numGenericArgs)
            {
                return FALSE;
            }
            for (DWORD i = 0; i < pKey1->u.asClass.m_numGenericArgs; i++)
            {
                if (pKey1->u.asClass.m_pGenericArgs[i] != pKey2->u.asClass.m_pGenericArgs[i])
                    return FALSE;
            }
            return TRUE;
        }
        else if (CorTypeInfo::IsModifier_NoThrow(pKey1->m_kind) || pKey1->m_kind == ELEMENT_TYPE_VALUETYPE)
        {
            return pKey1->u.asParamType.m_paramType == pKey2->u.asParamType.m_paramType
                && pKey1->u.asParamType.m_rank == pKey2->u.asParamType.m_rank;
        }
        else
        {
            _ASSERTE(pKey1->m_kind == ELEMENT_TYPE_FNPTR);
            if (pKey1->u.asFnPtr.m_callConv != pKey2->u.asFnPtr.m_callConv ||
                pKey1->u.asFnPtr.m_numArgs != pKey2->u.asFnPtr.m_numArgs)
                return FALSE;

            // Includes return type
            for (DWORD i = 0; i <= pKey1->u.asFnPtr.m_numArgs; i++)
            {
                if (pKey1->u.asFnPtr.m_pRetAndArgTypes[i] != pKey2->u.asFnPtr.m_pRetAndArgTypes[i])
                    return FALSE;
            }
            return TRUE;
        }
    }
};


#endif /* _H_TYPEKEY */
