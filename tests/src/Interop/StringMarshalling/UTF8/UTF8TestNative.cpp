// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>

// helper functions
#ifdef _WIN32	
char* UTF16ToUTF8(wchar_t * pszTextUTF16)
{
    if ((pszTextUTF16 == NULL) || (*pszTextUTF16 == L'\0')) {
        return 0;
    }

    size_t cchUTF16;
    cchUTF16 = wcslen(pszTextUTF16) + 1;
    int cbUTF8 = WideCharToMultiByte(CP_UTF8, 0,
        pszTextUTF16,
        (int)cchUTF16,
        NULL,
        0/* request buffer size*/,
        NULL,
        NULL);
    
    char *pszUTF8 = (char*)CoTaskMemAlloc(sizeof(char) * (cbUTF8 + 1));
    int nc = WideCharToMultiByte(CP_UTF8, // convert to UTF-8
        0,       //default flags 
        pszTextUTF16, //source wide string
        (int)cchUTF16,     // length of wide string
        pszUTF8,      // destination buffer 
        cbUTF8,       // destination buffer size
        NULL,
        NULL);
    
    if (!nc)
    {
        throw;
    }

    pszUTF8[nc] = '\0';
    return pszUTF8;
}

wchar_t* UTF8ToUTF16(const char *utf8)
{
    // Special case of empty input string
    //wszTextUTF16
    wchar_t *wszTextUTF16 = 0;
    if (!utf8 || !(*utf8))
        return wszTextUTF16;
    size_t szUtf8 = strlen(utf8);

    //Get length (in wchar_t's) of resulting UTF-16 string
    int cbUTF16 = ::MultiByteToWideChar(
        CP_UTF8,            // convert from UTF-8
        0,                  // default flags
        utf8,        // source UTF-8 string
        (int)szUtf8,      // length (in chars) of source UTF-8 string
        NULL,               // unused - no conversion done in this step
        0                   // request size of destination buffer, in wchar_t's
    );
    
    wszTextUTF16 = (wchar_t*)(CoTaskMemAlloc((cbUTF16 + 1 )  * sizeof(wchar_t) ));
    // Do the actual conversion from UTF-8 to UTF-16
    int nc = ::MultiByteToWideChar(
        CP_UTF8,            // convert from UTF-8
        0,                  // default flags
        utf8,        // source UTF-8 string
        (int)szUtf8,      // length (in chars) of source UTF-8 string
        wszTextUTF16,          // destination buffer
        cbUTF16);  // size of destination buffer, in wchar_t's

    if (!nc)
    {
        throw;
    }
    //MultiByteToWideChar do not null terminate the string when cbMultiByte is not -1
    wszTextUTF16[nc] = '\0';
    return wszTextUTF16;
}
#endif


LPSTR build_return_string(const char* pReturn)
{
    char *ret = 0;
    if (pReturn == 0 || *pReturn == 0)
        return ret;

    size_t strLength = strlen(pReturn);
    ret = (LPSTR)(CoTaskMemAlloc(sizeof(char)* (strLength + 1)));
    memset(ret, '\0', strLength + 1);
    strncpy_s(ret, strLength + 1, pReturn, strLength);
    return ret;
}

// this is the same set as in managed side , but here 
// string need to be escaped  , still CL applied some local and 
// end up with different byte sequence.

const int NSTRINGS = 6;
#ifdef _WIN32	
wchar_t  *utf8Strings[] = { L"Managed",
L"S\x00EEne kl\x00E2wen durh die wolken sint geslagen" ,
L"\x0915\x093E\x091A\x0902 \x0936\x0915\x094D\x0928\x094B\x092E\x094D\x092F\x0924\x094D\x0924\x0941\x092E\x094D \x0964 \x0928\x094B\x092A\x0939\x093F\x0928\x0938\x094D\x0924\x093F \x092E\x093E\x092E\x094D",
L"\x6211\x80FD\x541E\x4E0B\x73BB\x7483\x800C\x4E0D\x4F24\x8EAB\x4F53",
L"\x10E6\x10DB\x10D4\x10E0\x10D7\x10E1\x10D8 \x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4,\x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4, \x10DC\x10E3\x10D7\x10E3 \x10D9\x10D5\x10DA\x10D0 \x10D3\x10D0\x10DB\x10EE\x10E1\x10DC\x10D0\x10E1 \x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4,\x10E1\x10DD\x10E4\x10DA\x10D8\x10E1\x10D0 \x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4, \x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4,\x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4,\x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4,\x10E8\x10E0\x10DD\x10DB\x10D0\x10E1\x10D0, \x10EA\x10D4\x10EA\x10EE\x10DA\x10E1, \x10EC\x10E7\x10D0\x10DA\x10E1\x10D0 \x10D3\x10D0 \x10DB\x10D8\x10EC\x10D0\x10E1\x10D0, \x10F0\x10D0\x10D4\x10E0\x10D7\x10D0 \x10D7\x10D0\x10DC\x10D0 \x10DB\x10E0\x10DD\x10DB\x10D0\x10E1\x10D0; \x10DB\x10DD\x10DB\x10EA\x10DC\x10D4\x10E1 \x10E4\x10E0\x10D7\x10D4\x10DC\x10D8 \x10D3\x10D0 \x10D0\x10E6\x10D5\x10E4\x10E0\x10D8\x10DC\x10D3\x10D4, \x10DB\x10D8\x10D5\x10F0\x10EE\x10D5\x10D3\x10D4 \x10DB\x10D0\x10E1 \x10E9\x10D4\x10DB\x10E1\x10D0 \x10DC\x10D3\x10DD\x10DB\x10D0\x10E1\x10D0, \x10D3\x10E6\x10D8\x10E1\x10D8\x10D7 \x10D3\x10D0 \x10E6\x10D0\x10DB\x10D8\x10D7 \x10D5\x10F0\x10EE\x10D4\x10D3\x10D5\x10D8\x10D3\x10D4 \x10DB\x10D6\x10D8\x10E1\x10D0 \x10D4\x10DA\x10D5\x10D0\x10D7\x10D0 \x10D9\x10E0\x10D7\x10DD\x10DB\x10D0\x10D0\x10E1\x10D0\x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4,\x10E8\x10D4\x10DB\x10D5\x10D4\x10D3\x10E0\x10D4,",
L"\x03A4\x03B7 \x03B3\x03BB\x03CE\x03C3\x03C3\x03B1 \x03BC\x03BF\x03C5 \x03AD\x03B4\x03C9\x03C3\x03B1\x03BD \x03B5\x03BB\x03BB\x03B7\x03BD\x03B9\x03BA\x03AE",
L"\0"
};

#else
//test strings
const char  *utf8Strings[] = { "Managed",
"Sîne klâwen durh die wolken sint geslagen",
"काचं शक्नोम्यत्तुम् । नोपहिनस्ति माम्",
"我能吞下玻璃而不伤身体",
"ღმერთსი შემვედრე,შემვედრე, ნუთუ კვლა დამხსნას შემვედრე,სოფლისა შემვედრე, შემვედრე,შემვედრე,შემვედრე,შრომასა, ცეცხლს, წყალსა და მიწასა, ჰაერთა თანა მრომასა; მომცნეს ფრთენი და აღვფრინდე, მივჰხვდე მას ჩემსა ნდომასა, დღისით და ღამით ვჰხედვიდე მზისა ელვათა კრთომაასაშემვედრე,შემვედრე,",
"Τη γλώσσα μου έδωσαν ελληνική",
"\0"
};
#endif

// Modify the string builder in place, managed side validates.
extern "C" DLL_EXPORT void __cdecl StringBuilderParameterInOut(/*[In,Out] StringBuilder*/ char *s, int index)
{
    // if string.empty 
    if (s == 0 || *s == 0)
        return;

#ifdef _WIN32
    char *pszTextutf8 = UTF16ToUTF8(utf8Strings[index]);
#else
    char *pszTextutf8 = (char*)utf8Strings[index];
#endif

    // do byte by byte validation of in string
    size_t szLen = strlen(s);
    for (size_t i = 0; i < szLen; i++) 
    {
        if (s[i] != pszTextutf8[i])
        {
            printf("[in] managed string do not match native string\n");
            throw;
        }
    }  

    // modify the string inplace 
    size_t outLen = strlen(pszTextutf8);
    for (size_t i = 0; i < outLen; i++) {
        s[i] = pszTextutf8[i];
    }
    s[outLen] = '\0';
#ifdef _WIN32
    CoTaskMemFree(pszTextutf8);
#endif
}

//out string builder
extern "C" DLL_EXPORT void __cdecl  StringBuilderParameterOut(/*[In,Out] StringBuilder*/ char *s, int index)
{

#ifdef _WIN32
    char *pszTextutf8 = UTF16ToUTF8(utf8Strings[index]);
#else 
    char *pszTextutf8 = (char*)utf8Strings[index];
#endif
    // modify the string inplace 
    size_t outLen = strlen(pszTextutf8);
    for (size_t i = 0; i < outLen; i++) {
        s[i] = pszTextutf8[i];
    }
    s[outLen] = '\0';
#ifdef _WIN32
    CoTaskMemFree(pszTextutf8);
#endif
}

// return utf8 stringbuilder
extern "C" DLL_EXPORT char* __cdecl  StringBuilderParameterReturn(int index) {

#ifdef _WIN32
    char *pszTextutf8 = UTF16ToUTF8(utf8Strings[index]);
#else
    char *pszTextutf8 = (char*)utf8Strings[index];
#endif
    size_t strLength = strlen(pszTextutf8);
    LPSTR ret = (LPSTR)(CoTaskMemAlloc(sizeof(char)* (strLength + 1)));
    memcpy(ret, pszTextutf8, strLength);
    ret[strLength] = '\0';

#ifdef _WIN32
    CoTaskMemFree(pszTextutf8);
#endif

    return  ret;
}

extern "C" DLL_EXPORT LPSTR __cdecl StringParameterOut(/*[Out]*/ char *s, int index)
{
    // return a copy
    return build_return_string(s);
}

// string 
extern "C" DLL_EXPORT LPSTR __cdecl StringParameterInOut(/*[In,Out]*/ char *s, int index)
{
    // return a copy
    return build_return_string(s);
}

// Utf8 field
typedef struct FieldWithUtf8
{
    char *pFirst;
    int index;
}FieldWithUtf8;

//utf8 struct field
extern "C" DLL_EXPORT void _cdecl TestStructWithUtf8Field(struct FieldWithUtf8 fieldStruct)
{
    char *pszManagedutf8 = fieldStruct.pFirst;
    int stringIndex = fieldStruct.index;
    char *pszNative = 0;
    size_t outLen = 0;

    if (pszManagedutf8 == 0 || *pszManagedutf8 == 0)
        return;

#ifdef _WIN32
    pszNative = UTF16ToUTF8(utf8Strings[stringIndex]);
#else 
    pszNative = (char*)utf8Strings[stringIndex];
#endif
    outLen = strlen(pszNative);
    // do byte by byte comparision
    for (size_t i = 0; i < outLen; i++) 
    {
        if (pszNative[i] != pszManagedutf8[i]) 
        {
            printf("Native and managed string do not match.\n");
            throw;
        }
    }
#ifdef _WIN32
    CoTaskMemFree(pszNative);
#endif
}

// test c# out keyword
extern "C" DLL_EXPORT void __cdecl StringParameterRefOut(/*out*/ char **s, int index)
{
#ifdef _WIN32
    char *pszTextutf8 = UTF16ToUTF8(utf8Strings[index]);
#else
    char *pszTextutf8 = (char*)utf8Strings[index];
#endif      
    size_t strLength = strlen(pszTextutf8);
     *s = (LPSTR)(CoTaskMemAlloc(sizeof(char)* (strLength + 1)));
    memcpy(*s, pszTextutf8, strLength);
    (*s)[strLength] = '\0';
#ifdef _WIN32
    CoTaskMemFree(pszTextutf8);
#endif
}

//c# ref
extern "C" DLL_EXPORT void __cdecl StringParameterRef(/*ref*/ char **s, int index)
{
#ifdef _WIN32
    char *pszTextutf8 = UTF16ToUTF8(utf8Strings[index]);
#else
    char *pszTextutf8 = (char*)utf8Strings[index];
#endif
    size_t strLength = strlen(pszTextutf8);
    // do byte by byte validation of in string
    size_t szLen = strlen(*s);
    for (size_t i = 0; i < szLen; i++)
    {
        if ((*s)[i] != pszTextutf8[i])
        {
            printf("[in] managed string do not match native string\n");
            throw;
        }
    }

    // overwrite the orginal 
    *s = (LPSTR)(CoTaskMemAlloc(sizeof(char)* (strLength + 1)));
    memcpy(*s, pszTextutf8, strLength);
    (*s)[strLength] = '\0';
#ifdef _WIN32
    CoTaskMemFree(pszTextutf8);
#endif
}

extern "C" DLL_EXPORT void __cdecl StringParameterLPStr(/*out*/ char **s)
{
    const char *managed = "ManagedString";
    size_t strLength = strlen(managed);
    *s = (LPSTR)(CoTaskMemAlloc(sizeof(char)* (strLength + 1)));
    memcpy(*s, managed, strLength);
    (*s)[strLength] = '\0';
}

// delegate test
typedef void (__cdecl * Callback)(char *text, int index);
extern "C" DLL_EXPORT void _cdecl Utf8DelegateAsParameter(Callback managedCallback)
{
    for (int i = 0; i < NSTRINGS; ++i) 
    {
        char *pszNative = 0;
#ifdef _WIN32
        pszNative = UTF16ToUTF8(utf8Strings[i]);
#else 
        pszNative = (char*)utf8Strings[i];
#endif
        managedCallback(pszNative, i);
    }
}