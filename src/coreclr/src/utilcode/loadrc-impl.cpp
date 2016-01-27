// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// LoadRC-impl.cpp : Utility to load a localized file (primarily a DLL)

#include "palclr.h"

#if defined(USE_SSTRING)

// This is the normal path:  use SafeString, wrappers, etc...

#include "sstring.h"
#include "safewrap.h"

typedef SString MyString;
typedef SString::CIterator MyStringIterator;
#define EndsWithChar(OBJ, CHAR) (OBJ.EndsWith(CHAR))
#define AppendChar(OBJ, CHAR) (OBJ.Append(CHAR))
#define AppendStr(OBJ, STR) (OBJ.Append(STR))
#define TrimLastChar(OBJ) (OBJ.Truncate(OBJ.End() - 1))
#define GetChars(OBJ) (OBJ.GetUnicode())
#define IsEmptyStr(OBJ) (OBJ.IsEmpty())
#define CharLength(OBJ) (OBJ.GetCount())
#define StrBeginIter(OBJ) (OBJ.Begin())
#define StrEndIter(OBJ) (OBJ.End())
#define FindNext(OBJ, ITER, CHAR) (OBJ.Find(ITER, CHAR))
#define MakeString(DST, OBJ, BEG, END) (DST.Set(OBJ, BEG, END))
#define StrEquals(STR1, STR2) (STR1.Compare(STR2)==0)
#define FindLast(OBJ, ITER, CHAR) (OBJ.FindBack(ITER, CHAR))
void SkipChars(MyString &str, MyStringIterator &i, WCHAR c1, WCHAR c2) { while (str.Skip(i, c1) || str.Skip(i, c2)); }

#elif defined(USE_WSTRING)

// This stuff is used by GacUtil, because it _really_ doesn't want to link with utilcode :-(

#include <string>
#include <algorithm>
typedef std::wstring MyString;
typedef std::wstring::const_iterator MyStringIterator;
#define EndsWithChar(OBJ, CHAR) (*(OBJ.rbegin()) == CHAR)
#define AppendChar(OBJ, CHAR) (OBJ.push_back(CHAR))
#define AppendStr(OBJ, STR) (OBJ += STR)
#define TrimLastChar(OBJ) (OBJ.resize(OBJ.size() - 1))
#define GetChars(OBJ) (OBJ.c_str())
#define IsEmptyStr(OBJ) (OBJ.empty())
#define CharLength(OBJ) (OBJ.size())
#define StrBeginIter(OBJ) (OBJ.begin())
#define StrEndIter(OBJ) (OBJ.end())
#define FindNext(OBJ, ITER, CHAR) (ITER = std::find<std::wstring::const_iterator>(ITER, OBJ.end(), CHAR))
#define MakeString(DST, OBJ, BEG, END) (DST = MyString(BEG, END))
#define StrEquals(STR1, STR2) (STR1 == STR2)
#define ClrGetEnvironmentVariable(var, res) GetEnvVar(L##var, res)
bool FindLast(const MyString &str, MyStringIterator &iter, wchar_t c)
{
    size_t pos = str.find_last_of(c);
    iter = (pos == std::wstring::npos) ? str.end() : (str.begin() + pos);
    return pos != std::wstring::npos;
}
void SkipChars(const MyString &str, MyStringIterator &i, WCHAR c1, WCHAR c2) { while (*i == c1 || *i == c2) i++; }
bool GetEnvVar(_In_z_ wchar_t *var, MyString &res)
{
    wchar_t *buffer;
    size_t size;
    _wdupenv_s(&buffer, &size, var);
    if (!size || !buffer)
        return false;
    res = buffer;
    free(buffer); // Don't forget to free the buffer!
    return true;
}
void ClrGetModuleFileName(HMODULE hModule, MyString& value)
{
    wchar_t driverpath_tmp[_MAX_PATH];
    GetModuleFileNameW(hModule, driverpath_tmp, _MAX_PATH);
    value = driverpath_tmp;
}

#else

#error You must define either USE_SSTRING or USE_WSTRING to use this file

#endif

// This is a helper for loading localized string resource DLL files
HMODULE LoadLocalizedResourceDLLForSDK(_In_z_ LPCWSTR wzResourceDllName, _In_opt_z_ LPCWSTR modulePath, bool trySelf);

// This is a slight variation that can be used for anything else (ildasm.chm, for example)
typedef void* (__cdecl *LocalizedFileHandler)(LPCWSTR);
void* FindLocalizedFile(_In_z_ LPCWSTR wzResourceDllName, LocalizedFileHandler lfh, _In_opt_z_ LPCWSTR modulePath);

// Helper functions to combine paths
static MyString MakePath(const MyString &root, const MyString &file)
{
    MyString res = root;
    if (!EndsWithChar(res, W('\\')))
        AppendChar(res, W('\\'));
    AppendStr(res, file);
    return res;
}
static MyString MakePath(const MyString &root, const MyString &dir, const MyString &file)
{
    return MakePath(MakePath(root, dir), file);
}

// Helper to deal with occasional training back-slashes
static bool FileExists(const MyString &file)
{
    if (!EndsWithChar(file, W('\\')))
        return GetFileAttributesW(GetChars(file)) != INVALID_FILE_ATTRIBUTES;
    else
    {
        MyString tmp(file);
        TrimLastChar(tmp);
        return GetFileAttributesW(GetChars(tmp)) != INVALID_FILE_ATTRIBUTES;
    }
}

// Little helper function to get the codepage integer ID from the LocaleInfo
static UINT GetCodePage(LANGID LanguageID, DWORD locale)
{    
    wchar_t CodePageInt[12];
    GetLocaleInfo(MAKELCID(LanguageID, SORT_DEFAULT), LOCALE_IDEFAULTCODEPAGE, CodePageInt, _countof(CodePageInt));
    return _wtoi(CodePageInt);
}

// LCID helper macro
#define ENGLISH_LCID MAKELCID(MAKELANGID( LANG_ENGLISH, SUBLANG_ENGLISH_US ), SORT_DEFAULT)

// FindLocaleDirectory:  Search the provided path for one of the expected code page subdirectories
// Returns empty string on failure, or the full path to the c:\my\directory\1033\myrcfile.dll
static MyString FindLocaleDirectory(const MyString &path, const MyString &dllName)
{
    // We'll be checking for 3 different locales:  The user's default locale
    // The user's primary language locale, and english (in that order)
    const LCID lcidUser = MAKELCID(GetUserDefaultUILanguage(), SORT_DEFAULT);
    LCID rglcid[3] = {lcidUser, MAKELCID(MAKELANGID(PRIMARYLANGID(lcidUser), SUBLANG_DEFAULT), SORTIDFROMLCID(lcidUser)), ENGLISH_LCID};

    for (int i = 0; i < _countof(rglcid); i++)
    {
        LCID lcid = rglcid[i];
        // Turn the LCID into a string
        wchar_t wzNumBuf[12];
        _itow_s(lcid, wzNumBuf, _countof(wzNumBuf), 10);
        MyString localePath = MakePath(path, wzNumBuf, dllName);

        // Check to see if the file exists
        if (FileExists(localePath))
        {
            // make sure the console can support a codepage for this language.
            UINT ConsoleCP = GetConsoleOutputCP();
            
            // Dev10 #843375: For a GUI application, GetConsoleOutputCP returns 0
            // If that's the case, we don't care about capabilities of the console, 
            // since we're not outputting to the console, anyway...
            if ( ConsoleCP != 0 && lcid != ENGLISH_LCID )
            {
                LANGID LanguageID = MAKELANGID( lcid, SUBLANGID(lcid) );
                // we know the console cannot support arabic or hebrew (right to left scripts?)
                if( PRIMARYLANGID(LanguageID) == LANG_ARABIC || PRIMARYLANGID(LanguageID) == LANG_HEBREW )
                    continue;

                UINT LangOEMCodepage = GetCodePage(LanguageID, LOCALE_IDEFAULTCODEPAGE);
                UINT LangANSICodepage = GetCodePage(LanguageID, LOCALE_IDEFAULTANSICODEPAGE);

                // We can only support it if the console's code page is UTF8, OEM, or ANSI
                if( ConsoleCP != CP_UTF8 && ConsoleCP != LangOEMCodepage && ConsoleCP != LangANSICodepage )
                    continue;
            }

            return localePath;
        }
    }
    return W("");
}

// Attempt to load the resource file from the locale, first.
// If that fails, then just try any subdirectory of of the path provided
static void *LoadLocalFile(const MyString &path, const MyString &dllName, LocalizedFileHandler lfh)
{
    if (IsEmptyStr(path) || IsEmptyStr(dllName))
        return NULL;

    MyString pathTemp = path;

    // Languages are checked in the following order.  
    //    1)  The UI language:  this is returned by GetUserDefaultUILanguage.
    //    2)  As step 1, but with SUBLANG_DEFAULT
    //    3)  English
    //    4)  Any language that can be found!

    MyString localePath = FindLocaleDirectory(pathTemp, dllName);

    if (IsEmptyStr(localePath))
    {
        // None of the default choices exists, so now look for the first version of the dll in the given path.
        // We don't bother to see if the console supports the dll's language.
	    MyString wildCard = MakePath(pathTemp, W("*.*"));
	    WIN32_FIND_DATAW    wfdw;
        HANDLE hDirs = FindFirstFileW(GetChars(wildCard), &wfdw);
        if (hDirs == INVALID_HANDLE_VALUE)
            return NULL;
        do 
        {
            // We are only interested in directories, since at this level, that should
            // be the only thing in this directory, i.e, LCID sub dirs
            if (wfdw.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
            {
                MyString file(wfdw.cFileName);

                if (StrEquals(file, W(".")))
                    continue;

                if (StrEquals(file, W("..")))
                    continue;

                // Does this dir have the resource dll?
                MyString fullPath = MakePath(pathTemp, file, dllName);

                if (GetFileAttributesW(GetChars(fullPath)) != INVALID_FILE_ATTRIBUTES)
                {
                    localePath = fullPath; // Got it - bail out of here
                    break;
                }
            }
        } while (FindNextFileW(hDirs, &wfdw));

        FindClose(hDirs);


        if (IsEmptyStr(localePath))
        {
            //
            // With CoreCLR we have the resource dll directly in the bin directory so check there now.
            //
            
            // Does this dir have the resource dll?
            MyString fullPath = MakePath(path, dllName);

            if (GetFileAttributesW(GetChars(fullPath)) != INVALID_FILE_ATTRIBUTES)
            {
                localePath = fullPath; // Got it - bail out of here
            }
        }
    }

    // Attempt to load the library
    // Beware!  A dll loaded with LOAD_LIBRARY_AS_DATAFILE won't
    // let you use LoadIcon and things like that (only general calls like FindResource and LoadResource).
    return IsEmptyStr(localePath) ? NULL : lfh(GetChars(localePath));
}

// Try to load the resource DLL from [each directory in %PATH%]/<lcid>/
static void *LoadSearchPath(const MyString &resourceDllName, LocalizedFileHandler lfh)
{
    void *hmod = NULL;

    // Get the PATH variable into a C++ string
    MyString envPath;

    if (ClrGetEnvironmentVariable("PATH", envPath))
        return hmod;

    MyStringIterator  endOfChunk, startOfChunk = StrBeginIter(envPath);
    MyString tryPath;
    for (SkipChars(envPath, startOfChunk, W(' '), W(';')); 
        hmod == NULL && startOfChunk != StrEndIter(envPath);
        SkipChars(envPath, startOfChunk, W(' '), W(';')))
    {
        // copy this chunk of the path into our trypath
        endOfChunk = startOfChunk;
        FindNext(envPath, endOfChunk, W(';'));
        MakeString(tryPath, envPath, startOfChunk, endOfChunk);

        // Don't try invalid locations
        if (IsEmptyStr(tryPath) || CharLength(tryPath) >= _MAX_PATH)
            continue;

        // Try to load the dll
        hmod = LoadLocalFile(tryPath, resourceDllName, lfh);
        startOfChunk = endOfChunk;
    }
    return hmod;
}

void * __cdecl LibraryLoader(_In_z_ LPCWSTR lpFileName)
{
    return (void *)(LoadLibraryExW(lpFileName, NULL, LOAD_LIBRARY_AS_DATAFILE));
}

void *FindLocalizedFile(_In_z_ LPCWSTR wzResourceDllName, LocalizedFileHandler lfh, _In_opt_z_ LPCWSTR modulePathW)
{
    // find path of the modulePath
    MyString driverPath;
    MyString modulePath;
    ClrGetModuleFileName(GetModuleHandleW(modulePathW), modulePath);

    // Rip off the application name.
    MyStringIterator trailingSlashLocation = StrEndIter(modulePath);
    if (FindLast(modulePath, trailingSlashLocation, W('\\')))
        MakeString(driverPath, modulePath, StrBeginIter(modulePath), trailingSlashLocation);
    else
        // If it's not a full path, look in the current directory
        driverPath = W(".");

    // return the first of the local directory's copy or the resource DLL on %PATH%
    void *hmod = LoadLocalFile(driverPath, wzResourceDllName, lfh);
    if (hmod == NULL)
        hmod = LoadSearchPath(wzResourceDllName, lfh);
    return hmod;
}

// load the satellite dll which contains string resources
HMODULE LoadLocalizedResourceDLLForSDK(_In_z_ LPCWSTR wzResourceDllName, _In_opt_z_ LPCWSTR modulePath, bool trySelf)
{
    HMODULE hmod = (HMODULE)FindLocalizedFile(wzResourceDllName, &LibraryLoader, modulePath);
    if (hmod == NULL && trySelf)
        hmod = GetModuleHandleW(NULL);
    return hmod;
}
