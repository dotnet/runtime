//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// File: typedesc.inl
//


//

//
// ============================================================================


#ifndef _TYPEDESC_INL_
#define _TYPEDESC_INL_

inline PTR_MethodTable  TypeDesc::GetMethodTable() {

    LIMITED_METHOD_DAC_CONTRACT;

    if (IsGenericVariable())
        return NULL;

    if (GetInternalCorElementType() == ELEMENT_TYPE_FNPTR)
        return MscorlibBinder::GetElementType(ELEMENT_TYPE_U);

    _ASSERTE(HasTypeParam());
    ParamTypeDesc* asParam = dac_cast<PTR_ParamTypeDesc>(this);
    
    if (GetInternalCorElementType() == ELEMENT_TYPE_VALUETYPE)
        return dac_cast<PTR_MethodTable>(asParam->m_Arg.AsMethodTable());
    else
        return(asParam->m_TemplateMT.GetValue());
}

inline TypeHandle TypeDesc::GetTypeParam() {
    LIMITED_METHOD_DAC_CONTRACT;

    if (IsGenericVariable() || IsFnPtr())
        return TypeHandle();

    _ASSERTE(HasTypeParam());
    ParamTypeDesc* asParam = dac_cast<PTR_ParamTypeDesc>(this);
    return(asParam->m_Arg);
}

inline TypeHandle ParamTypeDesc::GetTypeParam() {
    LIMITED_METHOD_DAC_CONTRACT;

    return(this->m_Arg);
}

inline Instantiation TypeDesc::GetClassOrArrayInstantiation() {
    LIMITED_METHOD_DAC_CONTRACT;

    if (GetInternalCorElementType() != ELEMENT_TYPE_FNPTR)
    {
        ParamTypeDesc* asParam = dac_cast<PTR_ParamTypeDesc>(this);
        return Instantiation(&asParam->m_Arg, 1);
    }
    else
        return Instantiation();
}


#endif  // _TYPEDESC_INL_



