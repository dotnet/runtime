// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "dynamicarray.h"

#define BITMAP_WIDTH    15
#define BITMAP_HEIGHT   15

#define DISASSEMBLY_CLASS_NAME  "disassembly"
#define MAIN_WINDOW_CLASS       "dasm"
#define MAIN_WINDOW_CAPTION     "IL DASM"

#define DISASSEMBLY_CLASS_NAMEW  L"disassembly"
#define MAIN_WINDOW_CLASSW       L"dasm"
#define MAIN_WINDOW_CAPTIONW     L"IL DASM\0"

#define PHDC    (pDIS->hDC)
#define PRC     (pDIS->rcItem)

#define PADDING         28

#define ID_TREEVIEW     1
#define ID_LISTBOX      2

typedef struct
{
    const char *pszNamespace;
    HTREEITEM   hRoot;
} Namespace_t;


//
// Menu info
//
enum
{
    IDM_PROGRESS,
    IDM_OPEN,
    IDM_DUMP,
    IDM_DUMP_TREE,
    IDM_EXIT,
    IDM_SORT_BY_NAME,
    IDM_SHOW_PUB,
    IDM_SHOW_PRIV,
    IDM_SHOW_FAM,
    IDM_SHOW_ASM,
    IDM_SHOW_FAA,
    IDM_SHOW_FOA,
    IDM_SHOW_PSCOPE,
    IDM_FULL_INFO,
    IDM_BYTES,
    IDM_TOKENS,
    IDM_SOURCELINES,
    IDM_EXPANDTRY,
    IDM_QUOTEALLNAMES,
    IDM_SHOW_HEADER,
    IDM_SHOW_STAT,
    IDM_SHOW_METAINFO,
    IDM_MI_DEBUG,
    IDM_MI_HEADER,
    IDM_MI_HEX,
    IDM_MI_CSV,
    IDM_MI_UNREX,
    IDM_MI_SCHEMA,
    IDM_MI_RAW,
    IDM_MI_HEAPS,
    IDM_MI_VALIDATE,
    IDM_HELP,
    IDM_ABOUT,
    IDM_FONT_TREE,
    IDM_FONT_DASM,
    IDM_FIND,
    IDM_FINDNEXT,
    IDM_TREEVIEWFCN,
    IDM_CAVERBAL,
    IDM_DUMPRTF
};


//
// Bitmaps - keep in same order as in dasm.rc file
//
enum
{
    CLASS_IMAGE_INDEX,
    EVENT_IMAGE_INDEX,
    METHOD_IMAGE_INDEX,
    NAMESPACE_IMAGE_INDEX,
    FIELD_IMAGE_INDEX,
    PROP_IMAGE_INDEX,
    STATIC_METHOD_IMAGE_INDEX,
    STATIC_FIELD_IMAGE_INDEX,
    RED_ARROW_IMAGE_INDEX,
    CLASSENUM_IMAGE_INDEX,
    CLASSINT_IMAGE_INDEX,
    CLASSVAL_IMAGE_INDEX,
    CLASS_GEN_IMAGE_INDEX,
    METHOD_GEN_IMAGE_INDEX,
    STATIC_METHOD_GEN_IMAGE_INDEX,
    CLASSENUM_GEN_IMAGE_INDEX,
    CLASSINT_GEN_IMAGE_INDEX,
    CLASSVAL_GEN_IMAGE_INDEX,
    LAST_IMAGE_INDEX
};

#define TREEITEM_TYPE_MEMBER    1
#define TREEITEM_TYPE_INFO      2

// Member items and info items (under classes)
typedef struct
{
    HTREEITEM       hItem;
    union
    {
        mdToken			mbMember;
        char *          pszText; // if an info item (extends or implements some class)
    };
    BYTE            Discriminator;
} TreeItem_t;

// Class items (under the root)
typedef struct
{
    HTREEITEM   hItem;
    mdTypeDef   cl;
    TreeItem_t *pMembers;       // List of subitems
    DWORD       SubItems;       // Number of subitems
    DWORD       CurMember;      // Used when building member list
} ClassItem_t;

typedef struct
{
    HWND        hwndContainer;
    HWND        hwndChild;
    HMENU       hMenu;
    mdToken		tkClass;
    mdToken		tkMember;
    WCHAR        wzFind[120];
    FINDREPLACEW strFR;
} DisasmBox_t;



// For accessing metadata
extern IMDInternalImport*	g_pImport;
extern PELoader *           g_pPELoader;
extern IMetaDataImport2*     g_pPubImport;

//extern DynamicArray<mdToken>	g_cl_list;
extern mdToken *				g_cl_list;
//extern DynamicArray<mdToken>    g_cl_enclosing;
extern mdToken *				g_cl_enclosing;
extern mdTypeDef				g_cl_module;
extern DWORD					g_NumClasses;
