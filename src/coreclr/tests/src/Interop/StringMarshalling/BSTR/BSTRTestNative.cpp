// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <platformdefines.h>

WCHAR strManaged[] = W("Managed\0String\0");
size_t lenstrManaged = sizeof(strManaged) - sizeof(WCHAR);

WCHAR strReturn[] = W("a\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");
WCHAR strerrReturn[] = W("error");

WCHAR strNative[] = W(" Native");
size_t lenstrNative = sizeof(strNative) - sizeof(WCHAR);

//Test Method1
extern "C" BSTR ReturnString()
{
    return TP_SysAllocString(strReturn);
}

extern "C" BSTR ReturnErrorString()
{
    return TP_SysAllocString(strerrReturn);
}

//Test Method2
extern "C" DLL_EXPORT BSTR Marshal_InOut(/*[In,Out]*/BSTR s)
{
    //Check the Input
    size_t len = TP_SysStringByteLen(s);

    if (len != lenstrManaged || memcmp(s,strManaged,lenstrManaged) != 0)
    {
        printf("Error in Function Marshal_InOut(Native Client)\n");
        printf("Error: Actual: %d, Expected: %d\n",(int32_t) len, (int32_t)lenstrManaged);

        return ReturnErrorString();
    }

    //In-Place Change
    memcpy(s, strNative, len);

    //Return
    return ReturnString();
}


extern "C" DLL_EXPORT BSTR Marshal_Out(/*[Out]*/BSTR s)
{
    s = TP_SysAllocString(strNative);
        
    //Return
    return ReturnString();
}


extern "C" DLL_EXPORT BSTR MarshalPointer_InOut(/*[in,out]*/BSTR *s)
{    
    //Check the Input
    size_t len = TP_SysStringByteLen(*s);

    if (len != lenstrManaged || memcmp(*s,strManaged,lenstrManaged)!=0)
    {
        printf("Error in Function MarshalPointer_InOut\n");
        printf("Error: Expected: %d, Actual: %d", (int32_t)lenstrManaged, (int32_t)len);

        return ReturnErrorString();
    }

    //Allocate New
    CoreClrBStrFree(*s);
    *s = TP_SysAllocString(strNative);

    //Return
    return ReturnString();
}

extern "C" DLL_EXPORT BSTR MarshalPointer_Out(/*[out]*/ BSTR *s)
{
    *s = TP_SysAllocString(strNative);
    return ReturnString();
}

typedef BSTR (__stdcall * Test_DelMarshal_InOut)(/*[in]*/ BSTR s);
extern "C" DLL_EXPORT BOOL __cdecl RPinvoke_DelMarshal_InOut(Test_DelMarshal_InOut d, /*[in]*/ BSTR s)
{
    BSTR str = d(s);
    WCHAR ret[] = W("Return\0Return\0");    

    size_t lenstr = TP_SysStringByteLen(str);
    size_t lenret = sizeof(ret) - sizeof(WCHAR);

    if (lenret != lenstr || memcmp(str,ret,lenstr) != 0)
    {
        printf("Error in RPinvoke_DelMarshal_InOut, Returned value didn't match\n");
        return FALSE;
    }

    CoreClrBStrFree(str);
    return TRUE;
}

//
// PInvokeDef.cs explicitly declares that RPinvoke_DelMarshalPointer_Out uses STDCALL
//
typedef BSTR (__cdecl * Test_DelMarshalPointer_Out)(/*[out]*/ BSTR * s);
extern "C" DLL_EXPORT BOOL __stdcall RPinvoke_DelMarshalPointer_Out(Test_DelMarshalPointer_Out d)
{
    BSTR str;
    BSTR ret = d(&str);

    WCHAR changedstr[] = W("Native\0String\0");

    size_t lenstr = TP_SysStringByteLen(str);
    size_t lenchangedstr = sizeof(changedstr) - sizeof(WCHAR);

    if ( lenstr != lenchangedstr || (memcmp(str,changedstr,lenstr)!=0))
    {
        printf("Error in RPinvoke_DelMarshalPointer_Out, Value didn't change\n");
        return FALSE;
    }

    WCHAR expected[] = W("Return\0Return\0");
    size_t lenret = TP_SysStringByteLen(ret);
    size_t lenexpected = sizeof(expected) - sizeof(WCHAR);

    if (lenret != lenexpected || memcmp(ret,expected,lenret)!=0)
    {
        printf("Error in RPinvoke_DelMarshalPointer_Out, Return vaue is different than expected\n");
        return FALSE;
    }

    return TRUE;
}

//
// PInvokeDef.cs explicitly declares that ReverseP_MarshalStrB_InOut uses STDCALL
//
typedef BSTR (__stdcall * Test_Del_MarshalStrB_InOut)(/*[in,out]*/ BSTR s);
extern "C" DLL_EXPORT  BOOL __stdcall ReverseP_MarshalStrB_InOut(Test_Del_MarshalStrB_InOut d, /*[in]*/ BSTR s)
{
    BSTR ret = d((BSTR)s);
    WCHAR expected[] = W("Return");
    size_t lenret = TP_SysStringByteLen(ret);
    size_t lenexpected = sizeof(expected) - sizeof(WCHAR);

    if (lenret != lenexpected || memcmp(ret,expected,lenret) != 0)
    {
        printf("Error in ReverseP_MarshalStrB_InOut, Return vaue is different than expected\n");
        return FALSE;
    }

    WCHAR expectedchange[] = W("m");
    size_t lenstr = TP_SysStringByteLen(s);
    size_t lenexpectedchange = sizeof(expectedchange) - sizeof(WCHAR);
    
    if (lenstr != lenexpectedchange || memcmp(s,expectedchange,lenstr) != 0)
    {
        printf("Error in ReverseP_MarshalStrB_InOut, Value didn't get change\n");
        return FALSE;
    }
    return TRUE;
}

typedef struct Person Person;
struct Person{
    int age;
    int _padding;
    BSTR name;
};

extern "C" DLL_EXPORT BOOL Marshal_Struct_In(Person person)
{
    if (person.age != 12)
    {
        printf("Error in Marshal_Struct_In, The value for age field is incorrect\n");
        return FALSE;
    }

    size_t len = TP_SysStringByteLen(person.name);
    if (len != lenstrManaged || memcmp(person.name, strManaged, lenstrManaged) != 0)
    {
        printf("Error in Marshal_Struct_In, The value for name field is incorrect\n");
        return FALSE;
    }

    return TRUE;
}

extern "C" DLL_EXPORT BOOL MarshalPointer_Struct_InOut(Person* person)
{
    if (person->age != 12)
    {
        printf("Error in MarshalPointer_Struct_InOut, The value for age field is incorrect\n");
        return FALSE;
    }

    size_t len = TP_SysStringByteLen(person->name);
    if (len != lenstrManaged || memcmp(person->name, strManaged, lenstrManaged) != 0)
    {
        printf("Error in MarshalPointer_Struct_InOut, The value for name field is incorrect\n");
        return FALSE;
    }

    person->age = 21;
    person->name = TP_SysAllocString(strNative);
    return TRUE;
}

typedef BOOL (* Test_DelMarshal_Struct_In)(Person person);
extern "C" DLL_EXPORT BOOL RPInvoke_DelMarshal_Struct_In(Test_DelMarshal_Struct_In d)
{
    Person * pPerson = (Person *)CoreClrAlloc(sizeof(Person));
    pPerson->age = 21;
    pPerson->name =  TP_SysAllocString(strNative);
    
    if (!d(*pPerson))
    {
        printf("Error in RPInvoke_DelMarshal_Struct_In, Managed delegate return false\n");
        return FALSE;
    }

    return TRUE;
}

typedef BOOL (* Test_DelMarshalPointer_Struct_InOut)(Person * person);
extern "C" DLL_EXPORT BOOL RPInvoke_DelMarshalStructPointer_InOut(Test_DelMarshalPointer_Struct_InOut d)
{
    Person * pPerson = (Person *)CoreClrAlloc(sizeof(Person));
    pPerson->age = 21;
    pPerson->name =  TP_SysAllocString(strNative);

    if (!d(pPerson))
    {
       printf("Error in RPInvoke_DelMarshalStructPointer_InOut,The delegate return false\n");
       return FALSE;
    }

    size_t len = TP_SysStringByteLen(pPerson->name);
    if (len != lenstrManaged || memcmp(pPerson->name, strManaged, lenstrManaged) != 0)
    {
        printf("Error in RPInvoke_DelMarshalStructPointer_InOut,The value for name field for pPerson is incorrect\n");
        return FALSE;
    }

    return TRUE;
}

