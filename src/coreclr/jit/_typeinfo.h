// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          _typeInfo                                         XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************
 This header file is named _typeInfo.h to be distinguished from typeinfo.h
 in the NT SDK
******************************************************************************/

#ifndef _TYPEINFO_H_
#define _TYPEINFO_H_

//
// Captures information about a method pointer
//
// m_token is the CORINFO_RESOLVED_TOKEN from the IL, potentially with a more
//         precise method handle from getCallInfo
// m_tokenConstraint is the constraint if this was a constrained ldftn.
//
//
class methodPointerInfo
{
public:
    CORINFO_RESOLVED_TOKEN m_token;
    mdToken                m_tokenConstraint;
};

// Declares the typeInfo class, which represents the type of an entity on the stack.
//
class typeInfo
{
private:
    var_types m_type;

    union {
        CORINFO_CLASS_HANDLE m_cls;               // Valid, but not always available, for TYP_REFs.
        methodPointerInfo*   m_methodPointerInfo; // Valid only for function pointers.
    };

public:
    typeInfo() : m_type(TYP_UNDEF), m_cls(NO_CLASS_HANDLE)
    {
    }

    typeInfo(var_types type) : m_type(type), m_cls(NO_CLASS_HANDLE)
    {
    }

    typeInfo(CORINFO_CLASS_HANDLE cls) : m_type(TYP_REF), m_cls(cls)
    {
    }

    typeInfo(methodPointerInfo* methodPointerInfo) : m_type(TYP_I_IMPL), m_methodPointerInfo(methodPointerInfo)
    {
        assert(methodPointerInfo != nullptr);
        assert(methodPointerInfo->m_token.hMethod != nullptr);
    }

public:
    CORINFO_CLASS_HANDLE GetClassHandleForObjRef() const
    {
        assert((m_type == TYP_REF) || (m_type == TYP_UNDEF));
        return m_cls;
    }

    CORINFO_METHOD_HANDLE GetMethod() const
    {
        assert(IsMethod());
        return m_methodPointerInfo->m_token.hMethod;
    }

    methodPointerInfo* GetMethodPointerInfo() const
    {
        assert(IsMethod());
        return m_methodPointerInfo;
    }

    var_types GetType() const
    {
        return m_type;
    }

    bool IsType(var_types type) const
    {
        return m_type == type;
    }

    // Returns whether this is a method desc
    bool IsMethod() const
    {
        return IsType(TYP_I_IMPL) && (m_methodPointerInfo != nullptr);
    }
};
#endif // _TYPEINFO_H_
