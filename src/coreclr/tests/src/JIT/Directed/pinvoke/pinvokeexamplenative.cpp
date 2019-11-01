// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file fakes a few Windows APIs to enable the tail.il test on all platforms.

#include <cstring>
#include <string>
#include <vector>

#if defined(_MSC_VER)
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C" __attribute__((visibility("default")))

#ifdef BIT64
#define __int64     long
#else // BIT64
#define __int64     long long
#endif // BIT64

#define __int32     int
#define __int16     short int
#define __int8      char        // assumes char is signed

#endif 

#include <cstddef>

typedef std::vector<std::string> MENU;
typedef MENU * HMENU;

EXPORT_API
HMENU
CreatePopupMenu()
{
    return new MENU();
}

EXPORT_API
unsigned __int32
DestroyMenu(
    HMENU hMenu
    )
{
    delete hMenu;

    return 1;
}

EXPORT_API
unsigned __int32
AppendMenuA(
    HMENU hMenu,
    unsigned __int32 uFlags,
    unsigned __int32 uID,
    const char * item
    )
{
    if (uFlags != 0)
    {
        throw "AppendMenu: only MF_STRING (0) supported for uFlags";
    }

    hMenu->push_back(std::string(item));

    return 1;
}

EXPORT_API
__int32
GetMenuStringA(
    HMENU hMenu,
    unsigned __int32 uIDItem,
    char * lpString,
    __int32 cchMax,
    unsigned __int32 flags
    )
{
    if (flags != 0x400)
    {
        throw "GetMenuStringA: only MF_BYPOSITION (0x400) supported for flags";
    }

    if (cchMax < 0)
    {
        throw "GetMenuStringA: invalid argument (cchMax)";
    }

    if (uIDItem >= hMenu->size())
    { 
        return 0;
    }

    const std::string & str = (*hMenu)[uIDItem];

    __int32 cch = (__int32)str.size();

    if ((cchMax == 0) || (lpString == nullptr))
    {
        return cch;
    }

    if (cch >= cchMax)
    {
        cch = cchMax - 1;
    }
   
    memcpy(lpString, str.c_str(), cch);
    lpString[cch] = '\0';

    return cch;
}

EXPORT_API
int GetConstantInternal()
{
    return 27;
}
