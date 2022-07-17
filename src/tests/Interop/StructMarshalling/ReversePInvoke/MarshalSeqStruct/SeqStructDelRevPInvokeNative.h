// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <xplatform.h>

const int NumArrElements = 2;
struct InnerSequential
{
    int f1;
    float f2;
    LPCSTR f3;
};

void PrintInnerSequential(InnerSequential* p, const char* name)
{
    printf("\t%s.f1 = %d\n", name, p->f1);
    printf("\t%s.f2 = %f\n", name, p->f2);
    printf("\t%s.f3 = %s\n", name, p->f3);
}

void ChangeInnerSequential(InnerSequential* p)
{
    p->f1 = 77;
    p->f2 = 77.0;

    const char* lpstr = "changed string";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        p->f3 = temp;
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
}

bool IsCorrectInnerSequential(InnerSequential* p)
{
    if(p->f1 != 1)
        return false;
    if(p->f2 != 1.0)
        return false;
    if( strcmp((char*)p->f3,"some string") != 0 )
        return false;
    return true;
}

struct INNER2 // size = 12 bytes
{
    INT f1;
    FLOAT f2;
    LPCSTR f3;
};

void ChangeINNER2(INNER2* p)
{
    p->f1 = 77;
    p->f2 = 77.0;
    const char*  temp = "changed string";
    size_t len = strlen(temp);
    LPCSTR str = (LPCSTR)CoreClrAlloc( sizeof(char)*(len+1) );
    strcpy_s((char*)str,len+1,temp);
    p->f3 = str;
}

void PrintINNER2(INNER2* p, const char* name)
{
    printf("\t%s.f1 = %d\n", name, p->f1);
    printf("\t%s.f2 = %f\n", name, p->f2);
    printf("\t%s.f3 = %s\n", name, p->f3);
}

bool IsCorrectINNER2(INNER2* p)
{
    if(p->f1 != 1)
        return false;
    if(p->f2 != 1.0)
        return false;
    if(memcmp(p->f3, "some string",11*sizeof(char)) != 0 )
        return false;
    return true;
}

struct InnerExplicit
{
#ifdef WINDOWS
    union
    {
        INT f1;
        FLOAT f2;
    };
    CHAR _unused0[4];
    LPCSTR f3;
#else
    union
    {
        INT f1;
        FLOAT f2;
    };
    INT _unused0;
    LPCSTR f3;
#endif
};

void PrintInnerExplicit(InnerExplicit* p, const char* name)
{
    printf("\t%s.f1 = %d\n", name, p->f1);
    printf("\t%s.f2 = %f\n", name, p->f2);
    printf("\t%s.f3 = %s\n", name, p->f3);
}

void ChangeInnerExplicit(InnerExplicit* p)
{
    p->f1 = 77;

    const char*  temp = "changed string";
    size_t len = strlen(temp);
    LPCSTR str = (LPCSTR)CoreClrAlloc( sizeof(char)*(len+1) );
    strcpy_s((char*)str,len+1,temp);
    p->f3 = str;
}

struct InnerArraySequential
{
    InnerSequential arr[NumArrElements];
};

void PrintInnerArraySequential(InnerArraySequential* p, const char* name)
{
    for(int i = 0; i < NumArrElements; i++)
    {
        printf("\t%s.arr[%d].f1 = %d\n", name, i, (p->arr)[i].f1);
        printf("\t%s.arr[%d].f2 = %f\n", name, i, (p->arr)[i].f2);
        printf("\t%s.arr[%d].f2 = %s\n", name, i, (p->arr)[i].f3);
    }
}

void ChangeInnerArraySequential(InnerArraySequential* p)
{
    const char* lpstr = "changed string";
    LPSTR temp;
    for(int i = 0; i < NumArrElements; i++)
    {
        (p->arr)[i].f1 = 77;
        (p->arr)[i].f2 = 77.0;

        size_t size = sizeof(char) * (strlen(lpstr) + 1);
        temp = (LPSTR)CoreClrAlloc( size );
        memset(temp, 0, size);
        if(temp)
        {
            strcpy_s((char*)temp,size,lpstr);
            (p->arr)[i].f3 = temp;
        }
        else
        {
            printf("Memory Allocated Failed!");
        }
    }
}

bool IsCorrectInnerArraySequential(InnerArraySequential* p)
{
    for(int i = 0; i < NumArrElements; i++)
    {
        if( (p->arr)[i].f1 != 1 )
            return false;
        if( (p->arr)[i].f2 != 1.0 )
            return false;
    }
    return true;
}

#ifdef WINDOWS
#ifdef _WIN64
union OUTER3 // size = 32 bytes
{
    struct InnerSequential arr[2];
    struct
    {
        CHAR _unused0[24];
        LPCSTR f4;
    };
};
#else
struct OUTER3 // size = 28 bytes
{
    struct InnerSequential arr[2];
    LPCSTR f4;
};
#endif
#else
struct OUTER3 // size = 28 bytes
{
    struct InnerSequential arr[2];
    LPCSTR f4;
};
#endif

void PrintOUTER3(OUTER3* p, const char* name)
{
    for(int i = 0; i < NumArrElements; i++)
    {
        printf("\t%s.arr[%d].f1 = %d\n", name, i, (p->arr)[i].f1);
        printf("\t%s.arr[%d].f2 = %f\n", name, i, (p->arr)[i].f2);
        printf("\t%s.arr[%d].f3 = %s\n", name, i, (p->arr)[i].f3);
    }
    printf("\t%s.f4 = %s\n",name,p->f4);
}

void ChangeOUTER3(OUTER3* p)
{
    const char*  temp = "changed string";
    size_t len = strlen(temp);
    LPCSTR str = NULL;
    for(int i = 0; i < NumArrElements; i++)
    {
        (p->arr)[i].f1 = 77;
        (p->arr)[i].f2 = 77.0;

        str = (LPCSTR)CoreClrAlloc( sizeof(char)*(len+1) );
        strcpy_s((char*)str,len+1,temp);
        (p->arr)[i].f3 = str;
    }

    str = (LPCSTR)CoreClrAlloc( sizeof(char)*(len+1) );
    strcpy_s((char*)str,len+1,temp);
    p->f4 = str;
}

bool IsCorrectOUTER3(OUTER3* p)
{
    for(int i = 0; i < NumArrElements; i++)
    {
        if( (p->arr)[i].f1 != 1 )
            return false;
        if( (p->arr)[i].f2 != 1.0 )
            return false;
        if( memcmp((p->arr)[i].f3, "some string",11*sizeof(char)) != 0 )
            return false;
    }
    if(memcmp(p->f4,"some string",11*sizeof(char)) != 0)
    {
        return false;
    }
    return true;
}

struct CharSetAnsiSequential
{
    LPCSTR f1;
    char f2;
};

void PrintCharSetAnsiSequential(CharSetAnsiSequential* p, const char* name)
{
    printf("\t%s.f1 = %s\n", name, p->f1);
    printf("\t%s.f2 = %c\n", name, p->f2);
}

void ChangeCharSetAnsiSequential(CharSetAnsiSequential* p)
{
    const char* strSource = "change string";
    size_t size = strlen(strSource) + 1;
    LPSTR temp = (LPSTR)CoreClrAlloc(size);
    if(temp != NULL)
    {
        strcpy_s((char*)temp,size,strSource);
        p->f1 = temp;
        p->f2 = 'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
}

bool IsCorrectCharSetAnsiSequential(CharSetAnsiSequential* p)
{
    if(strcmp((char*)p->f1, (char*)"some string") != 0 )
        return false;
    if(p->f2 != 'c')
        return false;
    return true;
}


struct CharSetUnicodeSequential
{
    LPCWSTR f1;
    WCHAR f2;
};

void PrintCharSetUnicodeSequential(CharSetUnicodeSequential* p, const char* name)
{
#ifdef _WIN32
	wprintf(L"\t%S.first = %s\n", name, p->f1);
	wprintf(L"\t%S.last = %c\n", name, p->f2);
#else
	wprintf(L"\t%s.first = %s\n", name, p->f1);
	wprintf(L"\t%s.last = %c\n", name, p->f2);
#endif
}

void ChangeCharSetUnicodeSequential(CharSetUnicodeSequential* p)
{
    LPCWSTR strSource = W("change string");
    size_t len = TP_slen(strSource);
    LPCWSTR temp = (LPCWSTR)CoreClrAlloc(sizeof(WCHAR)*(len+1));
    if(temp != NULL)
    {
        TP_scpy_s((WCHAR*)temp, (len+1), strSource);
        p->f1 = temp;
        p->f2 = L'n';
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
}

bool IsCorrectCharSetUnicodeSequential(CharSetUnicodeSequential* p)
{
    LPCWSTR expected= W("some string");
    LPCWSTR actual = p->f1;
    if(0 != TP_wcmp_s(actual, expected))
    {
        return false;
    }
    if(p->f2 != L'c')
    {
        return false;
    }
    return true;
}

struct NumberSequential // size = 64 bytes
{
    LONG64 i64;
    ULONG64 ui64;
    DOUBLE d;
    INT i32;
    UINT ui32;
    SHORT s1;
    WORD us1;
    SHORT i16;
    WORD ui16;
    FLOAT sgl;
    BYTE b;
    CHAR sb;
};

void PrintNumberSequential(NumberSequential* str, const char* name)
{
    printf("\t%s.i32 = %d\n", name, str->i32);
    printf("\t%s.ui32 = %d\n", name, str->ui32);
    printf("\t%s.s1 = %d\n", name, str->s1);
    printf("\t%s.us1 = %u\n", name, str->us1);
    printf("\t%s.b = %u\n", name, str->b);
    printf("\t%s.sb = %d\n", name, str->sb);
    printf("\t%s.i16 = %d\n", name, str->i16);
    printf("\t%s.ui16 = %u\n", name, str->ui16);
    printf("\t%s.i64 = %lld\n", name, str->i64);
    printf("\t%s.ui64 = %llu\n", name, str->ui64);
    printf("\t%s.sgl = %f\n", name, str->sgl);
    printf("\t%s.d = %f\n",name, str->d);
}

void ChangeNumberSequential(NumberSequential* p)
{
    p->i32 = 0;
    p->ui32 = 32;
    p->s1 = 0;
    p->us1 = 16;
    p->b = 0;
    p->sb = 8;
    p->i16 = 0;
    p->ui16 = 16;
    p->i64 = 0;
    p->ui64 = 64;
    p->sgl = 64.0;
    p->d = 6.4;
}

bool IsCorrectNumberSequential(NumberSequential* p)
{
    if(p->i32 != INT_MIN || p->ui32 != 0xffffffff || p->s1 != -0x8000 || p->us1 != 0xffff || p->b != 0 ||
        p->sb != 0x7f ||p->i16 != -0x8000 || p->ui16 != 0xffff || p->i64 != -1234567890 ||
        p->ui64 != 1234567890 || (p->sgl) != 32.0 || p->d != 3.2)
    {
        return false;
    }
    return true;
}

struct S3 // size = 1032 bytes
{
    BOOL flag;
    LPCSTR str;
    INT vals[256];
};

void PrintS3(S3* str, const char* name)
{
    printf("\t%s.flag = %d\n", name, str->flag);
    printf("\t%s.str = %s\n", name, str->str);
    for(int i = 0; i<256 ;i++)
    {
        printf("\t%s.vals[%d] = %d\n",name,i,str->vals[i]);
    }
}

void ChangeS3(S3* p)
{
    p->flag = false;

    const char* strSource = "change string";
    size_t len = strlen(strSource);
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*(len+1));
    if(temp != NULL)
    {
        memset((LPVOID)temp,0,len+1);
        strncpy_s((char*)temp,len+1,strSource,len);
        p->str = temp;
    }
    for(int i = 1;i<257;i++)
    {
        p->vals[i-1] = i;
    }
}

bool IsCorrectS3(S3* p)
{
    int iflag = 0;
    if(!p->flag || strcmp((char*)p->str,"some string") != 0)
        return false;
    for (int i = 0; i < 256; i++)
    {
        if (p->vals[i] != i)
        {
            printf("\tThe Index of %i is not expected",i);
            iflag++;
        }
    }
    if (iflag != 0)
    {
        return false;
    }
    return true;
}

struct S4 // size = 8 bytes
{
    INT age;
    LPCSTR name;
};

enum Enum1
{
    e1 = 1,
    e2 = 3
};

struct S5 // size = 8 bytes
{
    struct S4 s4;
    Enum1 ef;
};

void PrintS5(S5* str, const char* name)
{
    printf("\t%s.s4.age = %d", name, str->s4.age);
    printf("\t%s.s4.name = %s", name, str->s4.name);
    printf("\t%s.ef = %d", name, str->ef);
}

void ChangeS5(S5* str)
{
    Enum1 eInstance = e2;
    const char* strSource = "change string";
    size_t len = strlen(strSource);
    LPCSTR temp = (LPCSTR)CoreClrAlloc(sizeof(char)*(len+1));
    if(temp != NULL)
    {
        memset((LPVOID)temp,0,len+1);
        strncpy_s((char*)temp,len+1,strSource,len);
        str->s4.name = temp;
    }
    str->s4.age = 64;
    str->ef = eInstance;
}

bool IsCorrectS5(S5* str)
{
    Enum1 eInstance = e1;
    if(str->s4.age != 32 || strcmp((char*)str->s4.name,"some string") != 0)
        return false;
    if(str->ef != eInstance)
    {
        return false;
    }
    return true;
}

struct StringStructSequentialAnsi // size = 8 bytes
{
    LPCSTR first;
    LPCSTR last;
};

void PrintStringStructSequentialAnsi(StringStructSequentialAnsi* str, const char* name)
{
    printf("\t%s.first = %s\n", name, str->first);
    printf("\t%s.last = %s\n", name, str->last);
}

bool IsCorrectStringStructSequentialAnsi(StringStructSequentialAnsi* str)
{
    char strOne[512];
    char strTwo[512];
    for(int i = 0;i<512;i++)
    {
        strOne[i] = 'a';
        strTwo[i] = 'b';
    }

    if(memcmp(str->first,strOne,512)!= 0)
        return false;

    if(memcmp(str->last,strTwo,512)!= 0)
        return false;

    return true;
}

void ChangeStringStructSequentialAnsi(StringStructSequentialAnsi* str)
{
    char* newFirst = (char*)CoreClrAlloc(sizeof(char)*513);
    char* newLast = (char*)CoreClrAlloc(sizeof(char)*513);
    for (int i = 0; i < 512; ++i)
    {
        newFirst[i] = 'b';
        newLast[i] = 'a';
    }
    newFirst[512] = '\0';
    newLast[512] = '\0';

    str->first = newFirst;
    str->last = newLast;
}

struct StringStructSequentialUnicode // size = 8 bytes
{
    LPCWSTR first;
    LPCWSTR last;
};

void PrintStringStructSequentialUnicode(StringStructSequentialUnicode* str, const char* name)
{
#ifdef _WIN32
	wprintf(L"\t%S.first = %s\n", name, str->first);
	wprintf(L"\t%S.last = %s\n", name, str->last);
#else
	wprintf(L"\t%s.first = %s\n", name, str->first);
	wprintf(L"\t%s.last = %s\n", name, str->last);
#endif
}

bool IsCorrectStringStructSequentialUnicode(StringStructSequentialUnicode* str)
{
    WCHAR strOne[256+1];
    WCHAR strTwo[256+1];

    for(int i = 0;i<256;++i)
    {
        strOne[i] = L'a';
        strTwo[i] = L'b';
    }
    strOne[256] = L'\0';
    strTwo[256] = L'\0';

    if(memcmp(str->first,strOne,256*sizeof(WCHAR)) != 0)
        return false;
    if(memcmp(str->last,strTwo,256*sizeof(WCHAR)) != 0)
        return false;
    return true;
}

void ChangeStringStructSequentialUnicode(StringStructSequentialUnicode* str)
{
    WCHAR* newFirst = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    WCHAR* newLast = (WCHAR*)CoreClrAlloc(sizeof(WCHAR)*257);
    for (int i = 0; i < 256; ++i)
    {
        newFirst[i] = L'b';
        newLast[i] = L'a';
    }
    newFirst[256] = L'\0';
    newLast[256] = L'\0';
    str->first = (const WCHAR*)newFirst;
    str->last = (const WCHAR*)newLast;
}

struct S8 // size = 32 bytes
{
    LPCSTR name;
    BOOL gender;
    HRESULT i32;
    HRESULT ui32;
    WORD jobNum;
    CHAR mySByte;
};

void PrintS8(S8* str, const char* name)
{
    printf("\t%s.name = %s\n",name, str->name);
    printf("\t%s.gender = %d\n", name, str->gender);
    printf("\t%s.jobNum = %d\n",name, str->jobNum);
    printf("\t%s.i32 = %d\n", name, (int)(str->i32));
    printf("\t%s.ui32 = %u\n", name, (unsigned int)(str->ui32));
    printf("\t%s.mySByte = %c\n", name, str->mySByte);
}

bool IsCorrectS8(S8* str)
{
    if(memcmp( str->name,"hello", strlen("hello")*sizeof(char)+1 )!= 0)
        return false;
    if(!str->gender)
        return false;
    if(str->jobNum != 10)
        return false;
    if(str->i32!= 128 || str->ui32 != 128)
        return false;
    if(str->mySByte != 32)
        return false;
    return true;
}

void ChangeS8(S8* str)
{
    const char* lpstr = "world";
    size_t size = sizeof(char) * (strlen(lpstr) + 1);
    LPSTR temp = (LPSTR)CoreClrAlloc( size );
    memset(temp, 0, size);
    if(temp)
    {
        strcpy_s((char*)temp,size,lpstr);
        str->name = temp;
    }
    else
    {
        printf("Memory Allocated Failed!");
    }
    str->gender = false;
    str->jobNum = 1;
    str->i32 = 256;
    str->ui32 = 256;
    str->mySByte = 64;
}

#pragma pack (8)
struct IntegerStructSequential // size = 4 bytes
{
    INT i;
};

void PrintIntegerStructSequential(IntegerStructSequential* str, const char* name)
{
    printf("\t%s.i = %d\n", name, str->i);
}

bool IsCorrectIntegerStructSequential(IntegerStructSequential* str)
{
    if(str->i != 32)
        return false;
    return true;
}

struct S9;
typedef void (*TestDelegate1)(struct S9 myStruct);

struct S9 // size = 8 bytes
{
    HRESULT i32;
    TestDelegate1 myDelegate1;
};

struct OuterIntegerStructSequential // size = 8 bytes
{
    INT i;
    struct IntegerStructSequential s_int;
};

struct IncludeOuterIntegerStructSequential // size = 8 bytes
{
    struct OuterIntegerStructSequential s;
};

void PrintIncludeOuterIntegerStructSequential(IncludeOuterIntegerStructSequential* str, const char* name)
{
    printf("\t%s.s.s_int.i = %d\n", name, str->s.s_int.i);
    printf("\t%s.s.i = %d\n", name, str->s.i);
}

bool IsCorrectIncludeOuterIntegerStructSequential(IncludeOuterIntegerStructSequential* str)
{
    if(str->s.s_int.i != 32)
        return false;
    if(str->s.i != 32)
        return false;
    return true;
}

void ChangeIncludeOuterIntegerStructSequential(IncludeOuterIntegerStructSequential* str)
{
    str->s.s_int.i = 64;
    str->s.i = 64;
}

#ifndef WINDOWS
typedef int* LPINT;
#endif

struct S11 // size = 8 bytes
{
    LPINT i32;
    INT i;
};

union U // size = 8 bytes
{
    INT i32;
    UINT ui32;
    LPVOID iPtr;
    LPVOID uiPtr;
    SHORT s;
    WORD us;
    BYTE b;
    CHAR sb;
    LONG64 l;
    ULONG64 ul;
    FLOAT f;
    DOUBLE d;
};

void PrintU(U* str, const char* name)
{
    printf("\t%s.i32 = %d\n", name, str->i32);
    printf("\t%s.ui32 = %u\n", name, str->ui32);
    printf("\t%s.iPtr = %zu\n", name, (size_t)(str->iPtr));
    printf("\t%s.uiPtr = %zu\n", name, (size_t)(str->uiPtr));
    printf("\t%s.s = %d\n", name, str->s);
    printf("\t%s.us = %u\n", name, str->us);
    printf("\t%s.b = %u\n", name, str->b);
    printf("\t%s.sb = %d\n", name, str->sb);
    printf("\t%s.l = %lld\n", name, str->l);
    printf("\t%s.ul = %llu\n", name, str->ul);
    printf("\t%s.f = %f\n", name, str->f);
    printf("\t%s.d = %f\n", name, str->d);
}

void ChangeU(U* p)
{
    p->i32 = 2147483647;
    p->ui32 = 0;
    p->iPtr = (LPVOID)(-64);
    p->uiPtr = (LPVOID)(64);
    p->s = 32767;
    p->us = 0;
    p->b = 255;
    p->sb = -128;
    p->l = -1234567890;
    p->ul = 0;
    p->f = 64.0;
    p->d = 6.4;
}

bool IsCorrectU(U* p)
{
    if(p->d != 3.2)
    {
        return false;
    }
    return true;
}

struct ByteStructPack2Explicit // size = 2 bytes
{
    BYTE b1;
    BYTE b2;
};

void PrintByteStructPack2Explicit(ByteStructPack2Explicit* str, const char* name)
{
    printf("\t%s.b1 = %d", name, str->b1);
    printf("\t%s.b2 = %d", name, str->b2);
}

void ChangeByteStructPack2Explicit(ByteStructPack2Explicit* p)
{
    p->b1 = 64;
    p->b2 = 64;
}

bool IsCorrectByteStructPack2Explicit(ByteStructPack2Explicit* p)
{
    if(p->b1 != 32 || p->b2 != 32)
        return false;
    return true;
}

struct ShortStructPack4Explicit // size = 4 bytes
{
    SHORT s1;
    SHORT s2;
};

void PrintShortStructPack4Explicit(ShortStructPack4Explicit* str, const char* name)
{
    printf("\t%s.s1 = %d", name, str->s1);
    printf("\t%s.s2 = %d", name, str->s2);
}

void ChangeShortStructPack4Explicit(ShortStructPack4Explicit* p)
{
    p->s1 = 64;
    p->s2 = 64;
}

bool IsCorrectShortStructPack4Explicit(ShortStructPack4Explicit* p)
{
    if(p->s1 != 32 || p->s2 != 32)
        return false;
    return true;
}

struct IntStructPack8Explicit // size = 8 bytes
{
    INT i1;
    INT i2;
};

void PrintIntStructPack8Explicit(IntStructPack8Explicit* str, const char* name)
{
    printf("\t%s.i1 = %d", name, str->i1);
    printf("\t%s.i2 = %d", name, str->i2);
}

void ChangeIntStructPack8Explicit(IntStructPack8Explicit* p)
{
    p->i1 = 64;
    p->i2 = 64;
}

bool IsCorrectIntStructPack8Explicit(IntStructPack8Explicit* p)
{
    if(p->i1 != 32 || p->i2 != 32)
        return false;
    return true;
}

struct LongStructPack16Explicit // size = 16 bytes
{
    LONG64 l1;
    LONG64 l2;
};

void PrintLongStructPack16Explicit(LongStructPack16Explicit* str, const char* name)
{
    printf("\t%s.l1 = %lld", name, str->l1);
    printf("\t%s.l2 = %lld", name, str->l2);
}

void ChangeLongStructPack16Explicit(LongStructPack16Explicit* p)
{
    p->l1 = 64;
    p->l2 = 64;
}

bool IsCorrectLongStructPack16Explicit(LongStructPack16Explicit* p)
{
    if(p->l1 != 32 || p->l2 != 32)
        return false;
    return true;
}

struct ByteStruct3Byte
{
    BYTE b1;
    BYTE b2;
    BYTE b3;
};
void PrintByteStruct3Byte(ByteStruct3Byte* str, char const * name)
{
	printf("\t%s.b1 = %d", name, str->b1);
	printf("\t%s.b2 = %d", name, str->b2);
	printf("\t%s.b3 = %d", name, str->b3);
}

void ChangeByteStruct3Byte(ByteStruct3Byte* p)
{
	p->b1 = 1;
	p->b2 = 42;
	p->b2 = 90;
}
bool IsCorrectByteStruct3Byte(ByteStruct3Byte* p)
{
    if(p->b1 != 7 || p->b2 != 12 || p->b3 != 18)
        return false;
    return true;
}
