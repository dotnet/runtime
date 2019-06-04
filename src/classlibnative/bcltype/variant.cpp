// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: Variant.cpp
//

//
// Purpose: Native Implementation of the Variant Class
//

//

#include "common.h"

#ifdef FEATURE_COMINTEROP

#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "variant.h"
#include "string.h"
#include "field.h"

// The following values are used to represent underlying
//  type of the Enum..
#define EnumI1          0x100000
#define EnumU1          0x200000
#define EnumI2          0x300000
#define EnumU2          0x400000
#define EnumI4          0x500000
#define EnumU4          0x600000
#define EnumI8          0x700000
#define EnumU8          0x800000
#define EnumMask        0xF00000


/*===============================SetFieldsObject================================
**
==============================================================================*/
FCIMPL2(void, COMVariant::SetFieldsObject, VariantData* var, Object* vVal)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(var));
        PRECONDITION(CheckPointer(vVal));
    }
    CONTRACTL_END;

    OBJECTREF val = ObjectToOBJECTREF(vVal);

    HELPER_METHOD_FRAME_BEGIN_1(val);
    GCPROTECT_BEGININTERIOR(var)

    CVTypes cvt = CV_EMPTY;
    TypeHandle typeHandle;

    MethodTable *valMT = val->GetMethodTable();

    //If this isn't a value class, we should just skip out because we're not going
    //to do anything special with it.
    if (!valMT->IsValueType())
    {
        var->SetObjRef(val);
        typeHandle = TypeHandle(valMT);
        
        if (typeHandle==GetTypeHandleForCVType(CV_MISSING))
        {
            var->SetType(CV_MISSING);
        }
        else if (typeHandle==GetTypeHandleForCVType(CV_NULL))
        {
            var->SetType(CV_NULL);
        }
        else if (typeHandle==GetTypeHandleForCVType(CV_EMPTY))
        {
            var->SetType(CV_EMPTY);
            var->SetObjRef(NULL);
        }
        else
        {
            var->SetType(CV_OBJECT);
        }
    }
    else if (IsTypeRefOrDef(g_ColorClassName, valMT->GetModule(), valMT->GetCl()))
    {
        // System.Drawing.Color is converted to UInt32
        var->SetDataAsUInt32(ConvertSystemColorToOleColor(&val));
        var->SetType(CV_U4);
    }
    else
    {
        //If this is a primitive type, we need to unbox it, get the value and create a variant
        //with just those values.
        void *UnboxData = val->UnBox();

        ClearObjectReference(var->GetObjRefPtr());
        typeHandle = TypeHandle(valMT);
        CorElementType cet = typeHandle.GetSignatureCorElementType();
        
        if (cet>=ELEMENT_TYPE_BOOLEAN && cet<=ELEMENT_TYPE_STRING)
        {
            cvt = (CVTypes)cet;
        }
        else
        {
            cvt = GetCVTypeFromClass(valMT);
        }
        var->SetType(cvt);


        //copy all of the data.
        // Copies must be done based on the exact number of bytes to copy.
        // We don't want to read garbage from other blocks of memory.
        //CV_I8 --> CV_R8, CV_DATETIME, CV_TIMESPAN, & CV_CURRENCY are all of the 8 byte quantities
        //If we don't find one of those ranges, we've found a value class 
        //of which we don't have inherent knowledge, so just slam that into an
        //ObjectRef.
        if (cvt>=CV_BOOLEAN && cvt<=CV_U1 && cvt != CV_CHAR)
        {
            var->SetDataAsInt64(*((UINT8 *)UnboxData));
        }
        else if (cvt==CV_CHAR || cvt>=CV_I2 && cvt<=CV_U2)
        {
            var->SetDataAsInt64(*((UINT16 *)UnboxData));
        }
        else if (cvt>=CV_I4 && cvt<=CV_U4 || cvt==CV_R4)
        {
            var->SetDataAsInt64(*((UINT32 *)UnboxData));
        }
        else if ((cvt>=CV_I8 && cvt<=CV_R8) || (cvt==CV_DATETIME) || (cvt==CV_TIMESPAN) || (cvt==CV_CURRENCY))
        {
            var->SetDataAsInt64(*((INT64 *)UnboxData));
        }
        else if (cvt==CV_EMPTY || cvt==CV_NULL || cvt==CV_MISSING)
        {
            var->SetType(cvt);
        }
        else if (cvt==CV_ENUM)
        {
            var->SetDataAsInt64(*((INT32 *)UnboxData));
            var->SetObjRef(typeHandle.GetManagedClassObject());
            var->SetType(GetEnumFlags(typeHandle));
        }
        else
        {
            // Decimals and other boxed value classes get handled here.
            var->SetObjRef(val);
        }
    }

    GCPROTECT_END();
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND


FCIMPL1(Object*, COMVariant::BoxEnum, VariantData* var)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(var));
        PRECONDITION(var->GetObjRef() != NULL);
    }
    CONTRACTL_END;

    OBJECTREF retO = NULL;
    
    HELPER_METHOD_FRAME_BEGIN_RET_1(retO);

#ifdef _DEBUG
    CVTypes vType = (CVTypes) var->GetType();
#endif

    _ASSERTE(vType == CV_ENUM);

    MethodTable* mt = ((REFLECTCLASSBASEREF) var->GetObjRef())->GetType().GetMethodTable();
    _ASSERTE(mt);

    retO = mt->Box(var->GetData());

    HELPER_METHOD_FRAME_END();
    return OBJECTREFToObject(retO);
}
FCIMPLEND


/*===============================GetTypeFromClass===============================
**Action: Takes an MethodTable * and returns the associated CVType.
**Arguments: MethodTable * -- a pointer to the class for which we want the CVType.
**Returns:  The CVType associated with the MethodTable or CV_OBJECT if this can't be 
**          determined.
**Exceptions: None
==============================================================================*/

CVTypes COMVariant::GetCVTypeFromClass(TypeHandle th)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    if (th.IsNull())
        return CV_EMPTY;

    //We'll start looking from Variant.  Empty and Void are handled below.
    for (int i=CV_EMPTY; i<CV_LAST; i++)
    {
        if (th == GetTypeHandleForCVType((CVTypes)i))
            return (CVTypes)i;
    }

    if (th.IsEnum())
        return CV_ENUM;

    return CV_OBJECT;    
}


int COMVariant::GetEnumFlags(TypeHandle th)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(!th.IsNull());
        PRECONDITION(th.IsEnum());
    }
    CONTRACTL_END;
    
    // <TODO> check this approximation - we may be losing exact type information </TODO>
    ApproxFieldDescIterator fdIterator(th.GetMethodTable(), ApproxFieldDescIterator::INSTANCE_FIELDS);
    FieldDesc* p = fdIterator.Next();
    if (NULL == p)
    {
        _ASSERTE(!"NULL FieldDesc returned");
        return 0;
    }
    
#ifdef _DEBUG
    WORD fldCnt = th.GetMethodTable()->GetNumInstanceFields();
#endif

    _ASSERTE(fldCnt == 1);

    CorElementType cet = p[0].GetFieldType();
    switch (cet)
    {
        case ELEMENT_TYPE_I1:
            return (CV_ENUM | EnumI1);
            
        case ELEMENT_TYPE_U1:
            return (CV_ENUM | EnumU1);
            
        case ELEMENT_TYPE_I2:
            return (CV_ENUM | EnumI2);
            
        case ELEMENT_TYPE_U2:
            return (CV_ENUM | EnumU2);
            
        IN_WIN32(case ELEMENT_TYPE_I:)
        case ELEMENT_TYPE_I4:
            return (CV_ENUM | EnumI4);
            
        IN_WIN32(case ELEMENT_TYPE_U:)
        case ELEMENT_TYPE_U4:
            return (CV_ENUM | EnumU4);
            
        IN_WIN64(case ELEMENT_TYPE_I:)
        case ELEMENT_TYPE_I8:
            return (CV_ENUM | EnumI8);
            
        IN_WIN64(case ELEMENT_TYPE_U:)
        case ELEMENT_TYPE_U8:
            return (CV_ENUM | EnumU8);
            
        default:
            _ASSERTE(!"UNknown Type");
            return 0;
    }
}

#endif // FEATURE_COMINTEROP
