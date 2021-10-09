// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <verrsrc.h>

#define QUOTE_MACRO_HELPER(x)       #x
#define QUOTE_MACRO(x)              QUOTE_MACRO_HELPER(x)

#define VER_PRODUCTNAME_STR         L"Microsoft\256 .NET"


#define VER_INTERNALNAME_STR        QUOTE_MACRO(FX_VER_INTERNALNAME_STR)
#define VER_ORIGINALFILENAME_STR    QUOTE_MACRO(FX_VER_INTERNALNAME_STR)

#define VER_FILEDESCRIPTION_STR     FX_VER_FILEDESCRIPTION_STR

#define VER_COMMENTS_STR            "Flavor=" QUOTE_MACRO(URTBLDENV_FRIENDLY)

#define VER_FILEFLAGSMASK           VS_FFI_FILEFLAGSMASK
#define VER_FILEFLAGS               VER_DEBUG
#define VER_FILEOS                  VOS__WINDOWS32

#define VER_FILETYPE                VFT_UNKNOWN
#define VER_FILESUBTYPE             VFT2_UNKNOWN

#define VER_VERSION_UNICODE_LANG  "040904B0" /* LANG_ENGLISH/SUBLANG_ENGLISH_US, Unicode CP */
#define VER_VERSION_ANSI_LANG     "040904E4" /* LANG_ENGLISH/SUBLANG_ENGLISH_US, Ansi CP */
#define VER_VERSION_TRANSLATION   0x0409, 0x04B0
