// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ildasmpch.h"

#ifndef FEATURE_CORECLR
#include "debugmacros.h"
#include "corpriv.h"
#include "ceeload.h"
#include "dasmgui.h"
#include "dasmenum.hpp"
#include "dis.h"
#include "resource.h"
#include "gui.h"
#include "formattype.h"
#include "..\tools\metainfo\mdinfo.h"
#include <NdpVersion.h>

struct MemberInfo {
    const char *pszMemberName;
    DWORD dwAttrs;
    ULONG           cComSig;
    PCCOR_SIGNATURE pComSig;
    mdToken token;
};

int __cdecl memberCmp(const void *elem1, const void *elem2 ) {
    MemberInfo* mem1 = (MemberInfo*) elem1;
    MemberInfo* mem2 = (MemberInfo*) elem2;
    return(strcmp(mem1->pszMemberName, mem2->pszMemberName));
}

//
// Global buffer, filled by AddOPCode
//
char *GlobalBuffer = new (nothrow) char[65535];
ULONG GlobalBufferLen = 65535;
ULONG InGlobalBuffer;
//
// Global HINSTANCE
//
extern HINSTANCE   g_hInstance;
extern HINSTANCE   g_hResources;

//
// Main window
//
HWND        g_hwndMain;

//
// Treeview for main window
//
HWND        g_hwndTreeView;

//
// Treeview class full name / partial name switch
//
BOOL        g_fTreeViewFCN = TRUE;

//
// Assembly info window (child of main)
//
HWND        g_hwndAsmInfo;
extern IMetaDataAssemblyImport*    g_pAssemblyImport;
void DumpAssembly(void* GUICookie, BOOL fFullDump);
IMetaDataAssemblyImport* GetAssemblyImport(void* GUICookie);

//
// Global image list
//
HIMAGELIST  g_hImageList;

//
// Menu for main window
//
HMENU       g_hMenu, g_hMetaInfoMenu, g_hFileMenu, g_hViewMenu, g_hFontMenu;

//
// Flags
//
BOOL        g_fFullMemberInfo = FALSE; // Show member type? (method, field, event, prop)
BOOL        g_fSortByName = TRUE; // Sort members in tree?
//
// Module name of loaded DLL/EXE
//
const char *g_pszModule;

//
// Interlocked variable for setting char dimensions once
//
long        g_SetCharDimensions = 0;

unsigned g_uFindReplaceMsg = 0;
HWND     g_hFindText = NULL;
//
// Bitmap handles
//
HBITMAP g_hBitmaps[LAST_IMAGE_INDEX];

//
// Root item for listview
//
HTREEITEM   g_hRoot;

// Global graphics
HBRUSH      g_hWhiteBrush;
HFONT       g_hFixedFont;
HFONT       g_hSmallFont;
HBITMAP     g_hMethodBmp, g_hFieldBmp, g_hClassBmp, g_hStaticMethodBmp, g_hStaticFieldBmp, g_hQuestionBmp;
LOGFONTW    g_strLogFontTree, g_strLogFontDasm;
CHOOSEFONTW g_strChFontTree, g_strChFontDasm;

struct GUI_Info
{
    LOGFONTW*   plfTree;
    LOGFONTW*   plfDasm;
    int         x;
    int         y;
    int         w;
    int         h;
};

GUI_Info guiInfo = {&g_strLogFontTree, &g_strLogFontDasm, CW_USEDEFAULT, CW_USEDEFAULT, 400,600};

// Text info
long        g_Height;
long        g_MaxCharWidth;

// Currently selected treeview item
HTREEITEM   g_CurSelItem;

extern IMAGE_COR20_HEADER * g_CORHeader;
extern BOOL                 g_fDumpTokens;
extern BOOL                 g_fShowBytes;
extern BOOL                 g_fShowSource;
extern BOOL                 g_fTryInCode;
extern BOOL                 g_fQuoteAllNames;
extern BOOL                 g_fCAVerbal;
extern BOOL                 g_fShowProgressBar;
extern BOOL                 g_fDumpHeader;
extern BOOL                 g_fDumpAsmCode;
extern BOOL                 g_fDumpTokens;
extern BOOL                 g_fDumpStats;
extern BOOL                 g_fDumpMetaInfo;
extern BOOL                 g_fDumpClassList;
extern BOOL                 g_fInsertSourceLines;

extern BOOL                 g_fLimitedVisibility;
extern BOOL                 g_fHidePub;
extern BOOL                 g_fHidePriv;
extern BOOL                 g_fHideFam;
extern BOOL                 g_fHideAsm;
extern BOOL                 g_fHideFAA;
extern BOOL                 g_fHideFOA;
extern BOOL                 g_fHidePrivScope;
extern BOOL                 g_fTDC;

extern char                 g_szInputFile[]; // in UTF-8
extern WCHAR                g_wszFullInputFile[]; // in UTF-16
extern ULONG                g_ulMetaInfoFilter;
extern char                 g_szOutputFile[]; // in UTF-8
extern DWORD                g_Mode;
extern FILE*                g_pFile;
extern HINSTANCE            g_hInstance;

extern unsigned             g_uCodePage;
extern unsigned             g_uConsoleCP;
DWORD   DumpResourceToFile(__in __nullterminated WCHAR*   wzFileName); // see DRES.CPP
//
// Functions
//
BOOL            RegisterWindowClasses();
HWND            CreateTreeView(HWND hwndParent);
HTREEITEM       AddOneItem(HTREEITEM hParent, const char *pszText, HTREEITEM hInsAfter, int iImage, HWND hwndTree, BOOL fExpanded);
HWND            GUIDisassemble(mdTypeDef cl, mdToken mbMember, __in __nullterminated char *pszWindowTitle);
HTREEITEM       AddClassToTreeView(HTREEITEM hParent, mdTypeDef cl);
void            AddGlobalFunctions();
void            CreateMenus();
Namespace_t*    FindNamespace(const char *pszNamespace);
void            GUICleanupClassItems();
void            SelectClassByName(__in __nullterminated char *pszFQName);
void            SelectClassByToken(mdToken tk);
void            DumpTreeItem(HTREEITEM hItem, FILE* pFile, __inout __nullterminated WCHAR* szIndent);
HTREEITEM       FindCreateNamespaceRoot(const char *pszNamespace);
FILE*           OpenOutput(__in __nullterminated const char* szFileName);
FILE*           OpenOutput(__in __nullterminated const WCHAR* wzFileName);

#undef SendMessageW
#undef PostMessageW
#undef CreateWindowExW
#undef DefWindowProcW
#undef RegisterClassExW
#undef RegisterClassW
#undef SetWindowTextW
#undef GetWindowTextW
#undef MessageBoxW

char* UtfToAnsi(__in __nullterminated const char* sz) { return UnicodeToAnsi(UtfToUnicode(sz));}

LRESULT CALLBACK DisassemblyWndProc(
    HWND    hwnd,
    UINT    uMsg,
    WPARAM  wParam,
    LPARAM  lParam
);

LRESULT CALLBACK MainWndProc(
    HWND    hwnd,
    UINT    uMsg,
    WPARAM  wParam,
    LPARAM  lParam
);

ClassItem_t *AddClassToGUI(mdTypeDef cl,    UINT        uImageIndex,
        const char *pszNamespace, const char *pszClassName, DWORD cSubItems, HTREEITEM *phRoot);

void AddMethodToGUI(
    mdTypeDef   cl,
    ClassItem_t * pClassItem,
    const char *pszNamespace,
    const char *pszClassName,
    const char *pszMethodName,
    PCCOR_SIGNATURE pComSig,
    unsigned cComSig,
    mdMethodDef mbMethod,
    DWORD       dwAttrs
);

void AddFieldToGUI(
    mdTypeDef   cl,
    ClassItem_t *pClassItem,
    const char  *pszNamespace,
    const char  *pszClassName,
    const char  *pszFieldName,
    const char  *pszSignature,
    mdFieldDef  mbField,
    DWORD       dwAttrs
);

void AddEventToGUI(
    mdTypeDef   cl,
    ClassItem_t *pClassItem,
    const char  *pszNamespace,
    const char  *pszClassName,
    DWORD       dwClassAttrs,
    mdEvent     mbEvent
);

void AddPropToGUI(
    mdTypeDef   cl,
    ClassItem_t *pClassItem,
    const char  *pszNamespace,
    const char  *pszClassName,
    DWORD       dwClassAttrs,
    mdProperty  mbProp
);

DynamicArray<DisasmBox_t> *g_DisasmBox;
DWORD       g_NumDisasmBoxes=0;

DynamicArray<ClassItem_t> *g_ClassItemList;
DWORD       g_NumClassItems=0;

DynamicArray<Namespace_t> *g_NamespaceList;
DWORD       g_NumNamespaces=0;


ClassItem_t *FindClassItem(HTREEITEM hItem);
ClassItem_t *FindClassItem(mdTypeDef cl);
ClassItem_t *FindClassItem(__in_opt __nullterminated char *pszNamespace, __in __nullterminated char *pszName);

// Find disasm box among opened by class and member tokens
DisasmBox_t* FindDisasmBox(mdToken tkClass, mdToken tkMember)
{
    for (DWORD i = 0; i < g_NumDisasmBoxes; i++)
    {
        if (((*g_DisasmBox)[i].tkClass == tkClass)
            &&((*g_DisasmBox)[i].tkMember == tkMember))
            return &(*g_DisasmBox)[i];
    }
    return NULL;
}
// Find disasm box among opened by the container hwnd
DisasmBox_t* FindDisasmBoxByHwnd(HWND hwndContainer)
{
    for (DWORD i = 0; i < g_NumDisasmBoxes; i++)
    {
        if ((*g_DisasmBox)[i].hwndContainer == hwndContainer)
            return &(*g_DisasmBox)[i];
    }
    return NULL;
}
//
// Add a new disassembly box to the global list of them
//
// hwndContainer - parent window
// hwndChild     - listview
//
void AddDisasmBox(HWND hwndContainer, HWND hwndChild, HMENU hMenu, mdToken tkClass, mdToken tkMember)
{
    (*g_DisasmBox)[g_NumDisasmBoxes].hwndContainer = hwndContainer;
    (*g_DisasmBox)[g_NumDisasmBoxes].hwndChild = hwndChild;
    (*g_DisasmBox)[g_NumDisasmBoxes].hMenu = hMenu;
    (*g_DisasmBox)[g_NumDisasmBoxes].tkClass = tkClass;
    (*g_DisasmBox)[g_NumDisasmBoxes].tkMember = tkMember;
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.lStructSize = sizeof(FINDREPLACEW);
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.hwndOwner = hwndContainer;
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.Flags = FR_DOWN|FR_DIALOGTERM;
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.lpstrFindWhat = (LPWSTR)((*g_DisasmBox)[g_NumDisasmBoxes].wzFind);
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.lpstrReplaceWith = NULL;
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.wFindWhatLen = 120;
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.wReplaceWithLen = 0;
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.lCustData = 0;
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.lpfnHook = NULL;
    (*g_DisasmBox)[g_NumDisasmBoxes].strFR.lpTemplateName = NULL;
    g_NumDisasmBoxes++;
}

void UpdateDisasmBox(DisasmBox_t* pBox, HWND hwndContainer, HWND hwndChild, HMENU hMenu)
{
    pBox->hwndContainer = hwndContainer;
    pBox->hwndChild = hwndChild;
    pBox->hMenu = hMenu;
    pBox->strFR.hwndOwner = hwndContainer;
}
//
// Given a container window, find the associated disassembly window
//
HWND FindAssociatedDisassemblyListBox(HWND hwndContainer)
{
    DWORD i;

    for (i = 0; i < g_NumDisasmBoxes; i++)
    {
        if ((*g_DisasmBox)[i].hwndContainer == hwndContainer)
            return (*g_DisasmBox)[i].hwndChild;
    }

    return NULL;
}

//
// Given a disassembly window, find the associated container window
//
HWND FindAssociatedDisassemblyContainer(HWND hwndChild)
{
    DWORD i;

    for (i = 0; i < g_NumDisasmBoxes; i++)
    {
        if ((*g_DisasmBox)[i].hwndChild == hwndChild)
            return (*g_DisasmBox)[i].hwndContainer;
    }

    return NULL;
}


void RemoveDisasmBox(HWND hwndContainer)
{
    DWORD i;

    for (i = 0; i < g_NumDisasmBoxes; i++)
    {
        if ((*g_DisasmBox)[i].hwndContainer == hwndContainer)
        {
            memcpy(&(*g_DisasmBox)[i], &(*g_DisasmBox)[i+1], (g_NumDisasmBoxes-i-1)*sizeof(DisasmBox_t));
            g_NumDisasmBoxes--;
            break;
        }
    }
}


void RemoveItemsFromList()
{
    TreeView_DeleteAllItems(g_hwndTreeView);
}


BOOL RefreshList()
{
    GUICleanupClassItems();
    return GUIAddItemsToList();
}


void GUISetModule(__in __nullterminated const char *pszModule)
{
    g_pszModule = pszModule;
}


TreeItem_t *FindClassMemberByName(ClassItem_t *pClassItem, 
                                  __in __nullterminated char *pszFindName, 
                                  __in __nullterminated char *pszFindSig)
{
    HRESULT hr;
    DWORD i;

    // do in two passes, fields first
    for (i = 0; i < pClassItem->SubItems; i++)
    {
        TreeItem_t *pItem;
        const char *pszMemberName;
        const char *pszMemberSig;
        DWORD   dwAttrs;

        CQuickBytes     qbMemberSig;

        PCCOR_SIGNATURE pComSig;
        ULONG           cComSig;


        pItem = &pClassItem->pMembers[i];
        if (pItem->Discriminator != TREEITEM_TYPE_MEMBER)
            continue;

        if (TypeFromToken(pItem->mbMember) == mdtMethodDef)
        {
            if (FAILED(g_pImport->GetMethodDefProps(pItem->mbMember, &dwAttrs)))
            {
                continue;
            }
            if (FAILED(g_pImport->GetNameOfMethodDef(pItem->mbMember, &pszMemberName)))
            {
                continue;
            }
            if (FAILED(g_pImport->GetSigOfMethodDef(pItem->mbMember, &cComSig, &pComSig)))
            {
                continue;
            }
        }
        else
        {
            if (FAILED(g_pImport->GetFieldDefProps(pItem->mbMember, &dwAttrs)))
            {
                continue;
            }
            if (FAILED(g_pImport->GetNameOfFieldDef(pItem->mbMember, &pszMemberName)))
            {
                continue;
            }
            if (FAILED(g_pImport->GetSigOfFieldDef(pItem->mbMember, &cComSig, &pComSig)))
            {
                continue;
            }
        }
        MAKE_NAME_IF_NONE(pszMemberName,pItem->mbMember);
        qbMemberSig.Shrink(0);
        pszMemberSig = PrettyPrintSig(pComSig, cComSig, "", &qbMemberSig, g_pImport,NULL);

    // @todo: GUI IL is so that NDView can call into DASM with/GUI; NDView uses Reflection API
    // which doesn't let us get a valid signature.
    // If GUI IL only, then ignore signature if it's NULL
        if (IsGuiILOnly()) {
            if (!strcmp(pszMemberName, pszFindName)) {
                if ((pszFindSig != NULL) && strcmp(pszMemberSig, pszFindSig)) continue;
                return pItem;
            }
        } else {
            if (!strcmp(pszMemberName, pszFindName) && !strcmp(pszMemberSig, pszFindSig))
                return pItem;
        }
    }

    return NULL;
}

// Kick of a disassembly window
// Return TRUE if window opened ok, and FALSE if there's an error
BOOL DisassembleMemberByName(__in __nullterminated char *pszClassName, 
                             __in __nullterminated char *pszMemberName, 
                             __in_opt __nullterminated char *pszSig)
{
    char szClassName[MAX_CLASSNAME_LENGTH];
    char szClassNamespace[MAX_CLASSNAME_LENGTH];
    char *pszClassNamespace;
    char *p;

    p = ns::FindSep(pszClassName);
    if (p == NULL)
    {
        strcpy_s(szClassName, MAX_CLASSNAME_LENGTH,pszClassName);
        pszClassNamespace = NULL;
    }
    else
    {
        strncpy_s(szClassNamespace, MAX_CLASSNAME_LENGTH, pszClassName, p - pszClassName);
        szClassNamespace[ p - pszClassName ] = '\0';
        pszClassNamespace = szClassNamespace;

        strcpy_s(szClassName, MAX_CLASSNAME_LENGTH, p+1);
    }

    ClassItem_t *pClassItem = FindClassItem(pszClassNamespace, szClassName);

    if (pClassItem != NULL)
    {
        TreeItem_t *pTreeItem;

        pTreeItem = FindClassMemberByName(pClassItem, pszMemberName, pszSig);

        if (pTreeItem != NULL)
        {
            DWORD   dwAttrs;
            DWORD   dwImplAttrs;

            // What is this member?

            if (TypeFromToken(pTreeItem->mbMember) == mdtMethodDef)
            {
                char* szText;
                HWND fOK=NULL;
                if (FAILED(g_pImport->GetMethodDefProps(pTreeItem->mbMember, &dwAttrs)))
                {
                    goto ErrorHere;
                }
                if (FAILED(g_pImport->GetMethodImplProps(pTreeItem->mbMember, NULL, &dwImplAttrs)))
                {
                    goto ErrorHere;
                }
                
                // Can't be abstract or native
                if (IsMdAbstract(dwAttrs) || IsMiInternalCall(dwImplAttrs))
                    return FALSE;

                szText = new (nothrow) char[4096];
                if(szText)
                {
                    TVITEMA SelItem;

                    // Get the name of this item so that we can title the disassembly window
                    memset(&SelItem, 0, sizeof(SelItem));
                    SelItem.mask = TVIF_TEXT;
                    SelItem.pszText = szText;
                    SelItem.hItem = pTreeItem->hItem;
                    SelItem.cchTextMax = 4095;

                    WCHAR* wzText = (WCHAR*)szText;
                    SendMessageW(g_hwndTreeView, TVM_GETITEMW, 0, (LPARAM) (LPTVITEMW) &SelItem);
                    unsigned L = ((unsigned)wcslen(wzText)+1)*3;
                    char*   szUTFText = new (nothrow) char[L];
                    if(szUTFText)
                    {
                        memset(szUTFText,0,L);
                        WszWideCharToMultiByte(CP_UTF8,0,wzText,-1,szUTFText,L,NULL,NULL);
                        delete[] wzText;
                        szText = szUTFText;
                    }

                    fOK = GUIDisassemble(pClassItem->cl, pTreeItem->mbMember, szText);
                    delete[] szText;
                }
                if (fOK == NULL) {
                    goto ErrorHere;
                }
            }
        } // endif (pTreeItem != NULL)
        else {
            goto ErrorHere;
        }

    } else {
        goto ErrorHere;
    }

    return TRUE;

ErrorHere:
    char pzText[300];
    sprintf_s(pzText, 300,RstrUTF(IDS_CANTVIEW_TX) /*"Can't view %s::%s(%s)"*/, pszClassName, pszMemberName, pszSig);

    WszMessageBox(g_hwndMain, UtfToUnicode(pzText), RstrW(IDS_CANTVIEW_HD) /*"Can't View IL"*/, MB_OK | MB_ICONERROR | GetDasmMBRTLStyle() );


    return FALSE;
}

//HTREEITEM AddInfoItemToClass(HTREEITEM hParent, ClassItem_t *pClassItem, const char *pszText, const char *pszStoredInfoText)
HTREEITEM AddInfoItemToClass(HTREEITEM hParent, ClassItem_t *pClassItem, const char *pszText, mdToken tk)
{
    _ASSERTE(pClassItem->CurMember < pClassItem->SubItems);
    pClassItem->pMembers[pClassItem->CurMember].hItem = AddOneItem(
        pClassItem->hItem, pszText, hParent, RED_ARROW_IMAGE_INDEX, g_hwndTreeView, FALSE
    );
    pClassItem->pMembers[pClassItem->CurMember].Discriminator = TREEITEM_TYPE_INFO;
    //pClassItem->pMembers[pClassItem->CurMember].pszText = (char *) pszStoredInfoText;
    pClassItem->pMembers[pClassItem->CurMember].mbMember = tk;
    pClassItem->CurMember++;

    return pClassItem->pMembers[pClassItem->CurMember-1].hItem;
}

struct ClassDescr
{
    mdToken tk;
    const char* szName;
};
static int __cdecl classDescrCmp(const void *op1, const void *op2)
{
    return  strcmp(((ClassDescr*)op1)->szName,((ClassDescr*)op2)->szName);
}

unsigned AddClassesWithEncloser(mdToken tkEncloser, HTREEITEM hParent)
{
    unsigned i, N=0;
    for (i = 0; i < g_NumClasses; i++)
    {
        if(g_cl_enclosing[i] == tkEncloser) N++;
    }

    if(N)
    {
        ClassDescr* rClassDescr = new (nothrow) ClassDescr[N];
        const char  *pszClassName,*pszNamespace;
        for (i = 0, N = 0; i < g_NumClasses; i++)
        {
            if(g_cl_enclosing[i] == tkEncloser)
            {
                rClassDescr[N].tk = g_cl_list[i];
                if (FAILED(g_pImport->GetNameOfTypeDef(g_cl_list[i], &pszClassName, &pszNamespace)))
                {
                    pszClassName = pszNamespace = "Invalid TypeDef record";
                }
                // doesn't throw here, so rClassDescr doesn't leak
                MAKE_NAME_IF_NONE(pszClassName,g_cl_list[i]);
                rClassDescr[N].szName = pszClassName;
                N++;
            }
        }
        if(g_fSortByName) qsort(&rClassDescr[0],N,sizeof(ClassDescr),classDescrCmp);
        for(i = 0; i < N; i++) AddClassToTreeView(hParent,rClassDescr[i].tk);
        delete[] rClassDescr;
    }
    return N;
}

static int __cdecl stringCmp(const void *op1, const void *op2)
{
    return  strcmp(*((char**)op1), *((char**)op2));
    //return(strlen(*((char**)op1)) - strlen(*((char**)op2)));
}

UINT GetDasmMBRTLStyle() {
    UINT RTLMessageBoxStyle = 0;
    WCHAR* pwStr = RstrW(IDS_RTL);
    if( wcscmp(pwStr, L"RTL_True") == 0) {
        RTLMessageBoxStyle  = 0x00080000 |0x00100000; // MB_RIGHT || MB_RTLREADING
    }        
    return RTLMessageBoxStyle;
}

void GUIDumpAssemblyInfo()
{
    memset(GlobalBuffer,0,GlobalBufferLen);
    InGlobalBuffer = 0;
    if(g_pAssemblyImport==NULL) g_pAssemblyImport = GetAssemblyImport((void*)g_hwndAsmInfo);
    if(g_pAssemblyImport)
    {
        if(g_fDumpRTF) DumpRTFPrefix((void *)g_hwndAsmInfo,FALSE);
        DumpAssembly((void *)g_hwndAsmInfo,FALSE);
        if(g_fDumpRTF) DumpRTFPostfix((void *)g_hwndAsmInfo);
    }

    if(g_uCodePage == 0xFFFFFFFF)
        SendMessageW((HWND)g_hwndAsmInfo,WM_SETTEXT,0, (LPARAM)GlobalBuffer);
    else
    {
        UINT32 L = (UINT32)strlen(GlobalBuffer);
        WCHAR* wz = new (nothrow) WCHAR[L+4];
        if(wz)
        {
            memset(wz,0,sizeof(WCHAR)*(L+2));
            int x = WszMultiByteToWideChar(CP_UTF8,0,GlobalBuffer,-1,wz,L+2);
            if(g_fDumpRTF)
            {
                x = (int)SendMessageA((HWND)g_hwndAsmInfo,WM_SETTEXT,0, (LPARAM)UnicodeToAnsi(wz));
            }
            else
            {
                x = (int)WszSendMessage((HWND)g_hwndAsmInfo,WM_SETTEXT,0, (LPARAM)wz);
            }
            delete[] wz;
        }
    }
}

BOOL GUIAddItemsToList()
{
    DWORD i,NumGlobals=0;
    HENUMInternal   hEnumMethod;

    RemoveItemsFromList();
    g_NumClassItems = 0;
    g_NumNamespaces = 0;

    g_hRoot = AddOneItem(
        (HTREEITEM)NULL,
        g_pszModule,
        (HTREEITEM)TVI_ROOT,
        FIELD_IMAGE_INDEX,
        g_hwndTreeView,
        TRUE
    );

    if (SUCCEEDED(g_pImport->EnumGlobalFunctionsInit(&hEnumMethod)))
    {
        NumGlobals = g_pImport->EnumGetCount(&hEnumMethod);
        g_pImport->EnumClose(&hEnumMethod);
    }
    if (SUCCEEDED(g_pImport->EnumGlobalFieldsInit(&hEnumMethod)))
    {
        NumGlobals += g_pImport->EnumGetCount(&hEnumMethod);
        g_pImport->EnumClose(&hEnumMethod);
    }
    (*g_ClassItemList)[0].hItem = g_hRoot;
    (*g_ClassItemList)[0].cl = 0;
    (*g_ClassItemList)[0].SubItems = NumGlobals+1;
    (*g_ClassItemList)[0].CurMember = 0;
    (*g_ClassItemList)[0].pMembers = new (nothrow) TreeItem_t[NumGlobals+1];
    g_NumClassItems++;

    //AddInfoItemToClass((HTREEITEM)TVI_ROOT, &(*g_ClassItemList)[0], " M A N I F E S T", "__MANIFEST__");
    AddInfoItemToClass((HTREEITEM)TVI_ROOT, &(*g_ClassItemList)[0], " M A N I F E S T", 0xFFFFFFFF);

    if (g_NumClasses != 0)
    {
        //create root namespaces
        {
            char**  rszNamespace = new (nothrow) char*[g_NumClasses];
            ULONG               ulNamespaces=0;
            for (i = 0; i < g_NumClasses; i++)
            {
                if (g_cl_enclosing[i] == mdTypeDefNil) // nested classes don't have separate namespaces
                {
                    const char *pszClassName, *pszNameSpace;
                    if (FAILED(g_pImport->GetNameOfTypeDef(
                        g_cl_list[i], 
                        &pszClassName, 
                        &pszNameSpace)))
                    {
                        pszClassName = pszNameSpace = "Invalid TypeDef record";
                    }
                    if ((pszNameSpace != NULL) && (*pszNameSpace != 0))
                    {
                        rszNamespace[ulNamespaces++] = (char*)pszNameSpace;
                    }
                }
            }
            if (ulNamespaces != 0)
            {
                qsort(&rszNamespace[0],ulNamespaces,sizeof(char*),stringCmp);
                for(i = 0; i < ulNamespaces; i++) FindCreateNamespaceRoot(rszNamespace[i]);
            }
            delete[] rszNamespace;
        }
        AddClassesWithEncloser(mdTypeDefNil,NULL);
    }// end if (g_NumClasses)
    AddGlobalFunctions();

    WszSendMessage(g_hwndTreeView, TVM_EXPAND, TVE_EXPAND, (LPARAM)g_hRoot);
    EnableMenuItem(g_hMenu,(UINT)(UINT_PTR)g_hViewMenu, MF_ENABLED);
    EnableMenuItem(g_hFileMenu,IDM_DUMP,MF_ENABLED);
    EnableMenuItem(g_hFileMenu,IDM_DUMP_TREE,MF_ENABLED);
    DrawMenuBar(g_hwndMain);

    {
        WszMultiByteToWideChar(CP_UTF8,0,g_szInputFile,-1,wzUniBuf,2048);
        wcscat_s(wzUniBuf,2048,L" - IL DASM");
        for(int cnt=0; cnt<100; cnt++)
        {
            SendMessageW(g_hwndMain,WM_SETTEXT, 0, (LPARAM)wzUniBuf);
            SendMessageW(g_hwndMain,WM_GETTEXT, 2048, (LPARAM)&wzUniBuf[2048]);
            wzUniBuf[2047]=0;
            if(0 == wcscmp(wzUniBuf,&wzUniBuf[2048])) break;
        }
    }

    if (IsGuiILOnly()) {
        ShowWindow(g_hwndMain, SW_HIDE);
    } else {
        ShowWindow(g_hwndMain, SW_SHOW);
    }
    UpdateWindow(g_hwndMain);
    //GUIDisassemble(0,0,"MANIFEST");
    g_Mode &= ~MODE_GUI;
    DumpManifest(NULL);
    g_Mode |= MODE_GUI;

    GUIDumpAssemblyInfo();

    TreeView_SelectItem(g_hwndTreeView,g_hRoot);
    SetFocus(g_hwndTreeView);
    return TRUE;
}


//
// Find class item by class token
//
ClassItem_t* ClassItemByToken(mdTypeDef cl)
{
    for(ULONG i=0; i < g_NumClassItems; i++)
    {
        if((*g_ClassItemList)[i].cl == cl) return &(*g_ClassItemList)[i];
    }
    return NULL;
}

// Factored out of AddClassToTreeView for its big stack consumption (AddClassToTreeView is
// called recursively via AddClassesWithEncloser).
static void AddClassToTreeView_PrettyPrintClass(mdTypeRef crType, LPCUTF8 pszFormat, __out_ecount(cBufferSize) char *pszBuffer, size_t cBufferSize)
{
    CQuickBytes out;
    sprintf_s(pszBuffer, cBufferSize, pszFormat, PrettyPrintClass(&out, crType, g_pImport));
}

//
// Add a class and its members
//
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HTREEITEM AddClassToTreeView(HTREEITEM hParent, mdTypeDef cl)
{
    HRESULT         hr;
    ClassItem_t     *pClassItem;
    HTREEITEM       hClassRoot;
    HTREEITEM       hNamespaceRoot = hParent;
    HTREEITEM       hPrimaryInfo;
    HTREEITEM       hLast;
    mdToken         *pMemberList = NULL;
    HENUMInternal   hEnumMethod;        // enumerator for method defs
    HENUMInternal   hEnumField;         // enumerator for fields
    HENUMInternal   hEnumEvent;         // enumerator for events
    HENUMInternal   hEnumProp;          // enumerator for properties
    DWORD           NumMembers;
    const char      *pszClassName; // name associated with this CL
    const char      *pszNamespace;
    DWORD           dwClassAttrs;
    mdTypeRef       crExtends;
    mdInterfaceImpl ii;
    DWORD           NumInterfaces;
    DWORD           i;
    char            *szPrimaryInfo; // public class foo extends bar
//  char            *szFullClassName;
    DWORD           SubItems;
    const char      *pszSuperName = NULL;
    HENUMInternal   hEnumII;            // enumerator for interface impl
    mdCustomAttribute *rCA;
    ULONG           ulCAs;
    UINT            uImageIndex = CLASS_IMAGE_INDEX;
    BOOL            bIsEnum = FALSE;
    BOOL            bIsValueType = FALSE;
    BOOL            bExtendsSysObject=FALSE;

    if (FAILED(g_pImport->GetNameOfTypeDef(
        cl, 
        &pszClassName, 
        &pszNamespace)))
    {
        return FALSE;
    }
    MAKE_NAME_IF_NONE(pszClassName,cl);
    g_pImport->GetTypeDefProps(
        cl,
        &dwClassAttrs,
        &crExtends
    );
    if(g_fLimitedVisibility)
    {
        if(g_fHidePub && (IsTdPublic(dwClassAttrs)||IsTdNestedPublic(dwClassAttrs))) return NULL;
        if(g_fHidePriv && (IsTdNotPublic(dwClassAttrs)||IsTdNestedPrivate(dwClassAttrs))) return NULL;
        if(g_fHideFam && IsTdNestedFamily(dwClassAttrs)) return NULL;
        if(g_fHideAsm && IsTdNestedAssembly(dwClassAttrs)) return NULL;
        if(g_fHideFOA && IsTdNestedFamORAssem(dwClassAttrs)) return NULL;
        if(g_fHideFAA && IsTdNestedFamANDAssem(dwClassAttrs)) return NULL;
    }
    hr = g_pImport->EnumInit(
        mdtInterfaceImpl,
        cl,
        &hEnumII);
    if (FAILED(hr))
        return FALSE;

    NumInterfaces = g_pImport->EnumGetCount(&hEnumII);
    hr = g_pImport->EnumInit(mdtMethodDef, cl, &hEnumMethod);
    if (FAILED(hr))
    {
        printf("Unable to enum methods\n");
        return FALSE;
    }
    NumMembers = g_pImport->EnumGetCount(&hEnumMethod);

    hr = g_pImport->EnumInit(mdtFieldDef, cl, &hEnumField);
    if (FAILED(hr))
    {
        g_pImport->EnumClose(&hEnumMethod);
        printf("Unable to enum fields\n");
        return FALSE;
    }
    NumMembers += g_pImport->EnumGetCount(&hEnumField);

    hr = g_pImport->EnumInit(mdtEvent, cl, &hEnumEvent);
    if (FAILED(hr))
    {
        g_pImport->EnumClose(&hEnumMethod);
        g_pImport->EnumClose(&hEnumField);
        printf("Unable to enum events\n");
        return FALSE;
    }
    NumMembers += g_pImport->EnumGetCount(&hEnumEvent);

    hr = g_pImport->EnumInit(mdtProperty, cl, &hEnumProp);
    if (FAILED(hr))
    {
        g_pImport->EnumClose(&hEnumMethod);
        g_pImport->EnumClose(&hEnumField);
        g_pImport->EnumClose(&hEnumEvent);
        printf("Unable to enum properties\n");
        return FALSE;
    }
    NumMembers += g_pImport->EnumGetCount(&hEnumProp);
    if (NumMembers > 0)
    {
        pMemberList = new (nothrow) mdToken[NumMembers];
        if (pMemberList == NULL)
        {
            // close enum before return
            g_pImport->EnumClose(&hEnumMethod);
            g_pImport->EnumClose(&hEnumField);
            g_pImport->EnumClose(&hEnumEvent);
            g_pImport->EnumClose(&hEnumProp);
            return FALSE;
        }

        for (i = 0; g_pImport->EnumNext(&hEnumField, &pMemberList[i]); i++);
        for (; g_pImport->EnumNext(&hEnumMethod, &pMemberList[i]); i++);
        for (; g_pImport->EnumNext(&hEnumEvent, &pMemberList[i]); i++);
        for (; g_pImport->EnumNext(&hEnumProp, &pMemberList[i]); i++);
        _ASSERTE(i == NumMembers);

    }
    else
    {
        pMemberList = NULL;
    }

    // Add class root to treeview
    SubItems = NumMembers + NumInterfaces + 3;
    if (!IsNilToken(crExtends))
    {
        LPCSTR szClassName="";
        LPCSTR szNameSpace="";
        SubItems++;
        if(TypeFromToken(crExtends)==mdtTypeRef)
        {
            if (FAILED(g_pImport->GetNameOfTypeRef(crExtends, &szNameSpace, &szClassName)))
            {
                return FALSE;
            }
            if(!(strcmp(szNameSpace,"System") || strcmp(szClassName, "Object")))
            {
                SubItems--;
                bExtendsSysObject = TRUE;
            }
        }
        else if (TypeFromToken(crExtends) == mdtTypeDef)
        {
            if (FAILED(g_pImport->GetNameOfTypeDef(crExtends, &szClassName, &szNameSpace)))
            {
                return FALSE;
            }
        }

        bIsEnum = (!strcmp(szNameSpace,"System"))&&(!strcmp(szClassName,"Enum"));

        bIsValueType = (!strcmp(szNameSpace,"System"))&&(!strcmp(szClassName,"ValueType"))
            && (strcmp(pszNamespace,"System") || strcmp(pszClassName,"Enum"));
    }
    {
        HCORENUM        hEnum = NULL;
        rCA = new (nothrow) mdCustomAttribute[4096];
        g_pPubImport->EnumCustomAttributes(&hEnum, cl, 0, rCA, 4096, &ulCAs);
        SubItems += ulCAs;
        g_pPubImport->CloseEnum( hEnum);
    }
    for (i = 0; i < g_NumClasses; i++)
    {
        if(g_cl_enclosing[i] == cl) SubItems++;
    }

    if(IsTdInterface(dwClassAttrs)) uImageIndex = CLASSINT_IMAGE_INDEX;
    if(bIsValueType) uImageIndex = CLASSVAL_IMAGE_INDEX;
    if(bIsEnum)      uImageIndex = CLASSENUM_IMAGE_INDEX;
    char *szptr1;
    if((*pszNamespace != 0) && g_fTreeViewFCN)
        sprintf_s(szString,SZSTRING_SIZE,"%s.",pszNamespace);
    else
        szString[0] = 0;
    strcat_s(szString,SZSTRING_SIZE,pszClassName);
    szptr1 = &szString[strlen(szString)];
    // Count the type parameters -- could be too many for GUI
    DWORD           NumTyPars;
    mdGenericParam  tkTyPar;
    HCORENUM        hEnumTyPar = NULL;
    unsigned jj;

    for(jj=0; 
        SUCCEEDED(g_pPubImport->EnumGenericParams(&hEnumTyPar, cl, &tkTyPar, 1, &NumTyPars))
        && (NumTyPars != 0); jj++);

    if (jj > 0)
    {
        if(jj > 16)
            szptr1 += sprintf_s(szptr1,SZSTRING_REMAINING_SIZE(szptr1),"%s%d type parameters%s",LTN(),jj,GTN());
        else
            DumpGenericPars(szString,cl);

        uImageIndex = CLASS_GEN_IMAGE_INDEX;
        if(IsTdInterface(dwClassAttrs)) uImageIndex = CLASSINT_GEN_IMAGE_INDEX;
        if(bIsValueType) uImageIndex = CLASSVAL_GEN_IMAGE_INDEX;
        if(bIsEnum)      uImageIndex = CLASSENUM_GEN_IMAGE_INDEX;
    }

    pClassItem = AddClassToGUI(cl, uImageIndex, pszNamespace, szString, SubItems, &hNamespaceRoot);
    if (pClassItem == NULL)
        return FALSE;

    hClassRoot = pClassItem->hItem;

    const size_t BUFFER_SIZE = 8192;
    szPrimaryInfo = new (nothrow) char[BUFFER_SIZE];
    strcpy_s(szPrimaryInfo, BUFFER_SIZE,".class ");

    if (IsTdInterface(dwClassAttrs))        strcat_s(szPrimaryInfo, BUFFER_SIZE, "interface ");
    //else if (IsTdUnmanagedValueType(dwClassAttrs)) strcat(szPrimaryInfo, "not_in_gc_heap value ");
    else if (bIsValueType)                  strcat_s(szPrimaryInfo, BUFFER_SIZE, "value ");
    else if (bIsEnum)                       strcat_s(szPrimaryInfo, BUFFER_SIZE, "enum ");

    if (IsTdPublic(dwClassAttrs))           strcat_s(szPrimaryInfo, BUFFER_SIZE, "public ");
    if (IsTdNotPublic(dwClassAttrs))        strcat_s(szPrimaryInfo, BUFFER_SIZE, "private ");
    if (IsTdNestedPublic(dwClassAttrs))     strcat_s(szPrimaryInfo, BUFFER_SIZE, "nested public ");
    if (IsTdNestedPrivate(dwClassAttrs))    strcat_s(szPrimaryInfo, BUFFER_SIZE, "nested private ");
    if (IsTdNestedFamily(dwClassAttrs))     strcat_s(szPrimaryInfo, BUFFER_SIZE, "nested family ");
    if (IsTdNestedAssembly(dwClassAttrs))   strcat_s(szPrimaryInfo, BUFFER_SIZE, "nested assembly ");
    if (IsTdNestedFamANDAssem(dwClassAttrs))   strcat_s(szPrimaryInfo, BUFFER_SIZE, "nested famandassem ");
    if (IsTdNestedFamORAssem(dwClassAttrs))    strcat_s(szPrimaryInfo, BUFFER_SIZE, "nested famorassem ");
    if (IsTdAbstract(dwClassAttrs))         strcat_s(szPrimaryInfo, BUFFER_SIZE, "abstract ");
    if (IsTdAutoLayout(dwClassAttrs))       strcat_s(szPrimaryInfo, BUFFER_SIZE, "auto ");
    if (IsTdSequentialLayout(dwClassAttrs)) strcat_s(szPrimaryInfo, BUFFER_SIZE, "sequential ");
    if (IsTdExplicitLayout(dwClassAttrs))   strcat_s(szPrimaryInfo, BUFFER_SIZE, "explicit ");
    if (IsTdAnsiClass(dwClassAttrs))        strcat_s(szPrimaryInfo, BUFFER_SIZE, "ansi ");
    if (IsTdUnicodeClass(dwClassAttrs))     strcat_s(szPrimaryInfo, BUFFER_SIZE, "unicode ");
    if (IsTdAutoClass(dwClassAttrs))        strcat_s(szPrimaryInfo, BUFFER_SIZE, "autochar ");
    if (IsTdImport(dwClassAttrs))           strcat_s(szPrimaryInfo, BUFFER_SIZE, "import ");
    if (IsTdWindowsRuntime(dwClassAttrs))   strcat_s(szPrimaryInfo, BUFFER_SIZE, "windowsruntime ");
    if (IsTdSerializable(dwClassAttrs))     strcat_s(szPrimaryInfo, BUFFER_SIZE, "serializable ");
//    if (IsTdEnum(dwClassAttrs))             strcat(szPrimaryInfo, "enum ");
    if (IsTdSealed(dwClassAttrs))           strcat_s(szPrimaryInfo, BUFFER_SIZE, "sealed ");
    if (IsTdBeforeFieldInit(dwClassAttrs))  strcat_s(szPrimaryInfo, BUFFER_SIZE, "beforefieldinit ");
    if (IsTdSpecialName(dwClassAttrs))      strcat_s(szPrimaryInfo, BUFFER_SIZE, "specialname ");
    if (IsTdRTSpecialName(dwClassAttrs))    strcat_s(szPrimaryInfo, BUFFER_SIZE, "rtspecialname ");

    if(g_fDumpTokens) sprintf_s(&szPrimaryInfo[strlen(szPrimaryInfo)], BUFFER_SIZE - strlen(szPrimaryInfo)," /*%08X*/",cl);
    hPrimaryInfo = AddInfoItemToClass(hClassRoot, pClassItem, szPrimaryInfo, NULL);
    hLast = hPrimaryInfo;
    // Now add nodes for extends, implements
    if (!IsNilToken(crExtends))
    {
        if (!bExtendsSysObject)
        {
            AddClassToTreeView_PrettyPrintClass(crExtends, " extends %s ", szPrimaryInfo, BUFFER_SIZE);
            hLast = AddInfoItemToClass(hLast, pClassItem, szPrimaryInfo, crExtends);
        }
    }

    if (NumInterfaces > 0)
    {
        for (i=0; g_pImport->EnumNext(&hEnumII, &ii); i++)
        {
            mdTypeRef crInterface;
            
            if (FAILED(g_pImport->GetTypeOfInterfaceImpl(ii, &crInterface)))
            {
                printf("Unable to get information about interface implementation\n");
                return FALSE;
            }
            {
                AddClassToTreeView_PrettyPrintClass(crInterface, " implements %s ", szPrimaryInfo, BUFFER_SIZE);
                hLast = AddInfoItemToClass(hLast, pClassItem, szPrimaryInfo, crInterface);
            }
        }

        // The assertion will fire if the enumerator is bad
        _ASSERTE(NumInterfaces == i);

        // close the enumerator
        g_pImport->EnumClose(&hEnumII);
    }
    delete[] szPrimaryInfo;

    BOOL fDumpRTF = g_fDumpRTF;
    g_fDumpRTF = FALSE;
    // add info entries for custom attributes
    for(i = 0; i < ulCAs; i++)
    {
        char* pc;
        memset(GlobalBuffer,0,GlobalBufferLen);
        InGlobalBuffer = 0;
        DumpCustomAttribute(rCA[i],(void *)g_hwndTreeView,false);
        if(pc = strchr(GlobalBuffer,'\r')) strcpy_s(pc,6," ..."); // until the first <CR> only
        //hLast = AddInfoItemToClass(hLast, pClassItem, GlobalBuffer, "#####"); // this "name" is guaranteed to be unique!
        hLast = AddInfoItemToClass(hLast, pClassItem, GlobalBuffer, rCA[i]);
    }
    delete[] rCA;

    // Re-fetch the current class item ptr, dynamic array may have shifted
    pClassItem = ClassItemByToken(cl);

    // Add nested classes
    AddClassesWithEncloser(cl,pClassItem->hItem);
    pClassItem = ClassItemByToken(cl);

    MemberInfo* members = NULL;
    if (NumMembers != 0)
    {
        members = new (nothrow) MemberInfo[NumMembers];
        if (members == NULL)
        {
            if (pMemberList != NULL) delete[] pMemberList;
            return FALSE;
        }
    }
    // do in four passes, fields first
    MemberInfo* curMem = members;
    for (i = 0; i < NumMembers; i++)
    {
        if (TypeFromToken(pMemberList[i]) == mdtFieldDef)
        {
            curMem->token = pMemberList[i];
            if (FAILED(g_pImport->GetFieldDefProps(pMemberList[i], &curMem->dwAttrs)))
            {
                printf("Invalid FieldDef %08X record\n", pMemberList[i]);
                delete []members;
                delete []pMemberList;
                return FALSE;
            }
            if (FAILED(g_pImport->GetNameOfFieldDef(pMemberList[i], &curMem->pszMemberName)))
            {
                printf("Invalid FieldDef %08X record\n", pMemberList[i]);
                delete []members;
                delete []pMemberList;
                return FALSE;
            }
            MAKE_NAME_IF_NONE(curMem->pszMemberName,pMemberList[i]);
            if (FAILED(g_pImport->GetSigOfFieldDef(pMemberList[i], &curMem->cComSig, &curMem->pComSig)))
            {
                printf("Invalid FieldDef %08X record\n", pMemberList[i]);
                delete []members;
                delete []pMemberList;
                return FALSE;
            }
            curMem++;
        }
        else break;
    }

    MemberInfo* endMem = curMem;
    if (g_fSortByName) qsort(members, endMem - members, sizeof MemberInfo, memberCmp);

    for (curMem = members; curMem < endMem;curMem++)
    {
        if (g_fLimitedVisibility)
        {
            if(g_fHidePub && IsFdPublic(curMem->dwAttrs)) continue;
            if(g_fHidePriv && IsFdPrivate(curMem->dwAttrs)) continue;
            if(g_fHideFam && IsFdFamily(curMem->dwAttrs)) continue;
            if(g_fHideAsm && IsFdAssembly(curMem->dwAttrs)) continue;
            if(g_fHideFOA && IsFdFamORAssem(curMem->dwAttrs)) continue;
            if(g_fHideFAA && IsFdFamANDAssem(curMem->dwAttrs)) continue;
            if(g_fHidePrivScope && IsFdPrivateScope(curMem->dwAttrs)) continue;
        }
        AddFieldToGUI(cl, pClassItem, pszNamespace, pszClassName, curMem->pszMemberName, NULL, curMem->token, curMem->dwAttrs);
    }

    // methods second
    curMem = members;
    for (; i < NumMembers; i++)
    {
        if (TypeFromToken(pMemberList[i]) == mdtMethodDef)
        {
            curMem->token = pMemberList[i];
            if (FAILED(g_pImport->GetMethodDefProps(pMemberList[i], &curMem->dwAttrs)))
            {
                printf("Invalid MethodDef %08X record\n", pMemberList[i]);
                delete []members;
                delete []pMemberList;
                return FALSE;
            }
            
            if (FAILED(g_pImport->GetNameOfMethodDef(pMemberList[i], &curMem->pszMemberName)))
            {
                printf("Invalid MethodDef %08X record\n", pMemberList[i]);
                delete []members;
                delete []pMemberList;
                return FALSE;
            }
            MAKE_NAME_IF_NONE(curMem->pszMemberName,pMemberList[i]);
            if (FAILED(g_pImport->GetSigOfMethodDef(pMemberList[i], &curMem->cComSig, &curMem->pComSig)))
            {
                printf("Invalid MethodDef %08X record\n", pMemberList[i]);
                delete []members;
                delete []pMemberList;
                return FALSE;
            }
            curMem++;
        }
        else break;
    }
    
    endMem = curMem;
    if (g_fSortByName) qsort(members, endMem - members, sizeof MemberInfo, memberCmp);

    for (curMem = members; curMem < endMem;curMem++)
    {
        if (g_fLimitedVisibility)
        {
            if(g_fHidePub && IsMdPublic(curMem->dwAttrs)) continue;
            if(g_fHidePriv && IsMdPrivate(curMem->dwAttrs)) continue;
            if(g_fHideFam && IsMdFamily(curMem->dwAttrs)) continue;
            if(g_fHideAsm && IsMdAssem(curMem->dwAttrs)) continue;
            if(g_fHideAsm && g_fHideFam && IsMdFamORAssem(curMem->dwAttrs)) continue;
            if(g_fHideFAA && IsMdFamANDAssem(curMem->dwAttrs)) continue;
            if(g_fHidePrivScope && IsMdPrivateScope(curMem->dwAttrs)) continue;
        }
        AddMethodToGUI(cl, pClassItem, pszNamespace, pszClassName, curMem->pszMemberName, curMem->pComSig, curMem->cComSig, curMem->token, curMem->dwAttrs);
    }
    // events third
    curMem = members;
    for (; i < NumMembers; i++)
    {
        if (TypeFromToken(pMemberList[i]) == mdtEvent)
        {
            curMem->token = pMemberList[i];
            if (FAILED(g_pImport->GetEventProps(
                curMem->token, 
                &curMem->pszMemberName, 
                &curMem->dwAttrs, 
                (mdToken *)&curMem->pComSig)))
            {
                curMem->pszMemberName = "Invalid Event record";
                curMem->dwAttrs = 0;
                curMem->pComSig = (PCCOR_SIGNATURE)mdTypeDefNil;
            }
            MAKE_NAME_IF_NONE(curMem->pszMemberName,pMemberList[i]);
            curMem++;
        }
        else break;
    }

    endMem = curMem;
    if (g_fSortByName) qsort(members, endMem - members, sizeof MemberInfo, memberCmp);
    curMem = members;
    while (curMem < endMem)
    {
        if (g_fLimitedVisibility)
        {
            HENUMInternal   hAssoc;
            unsigned nAssoc;
            if (FAILED(g_pImport->EnumAssociateInit(curMem->token,&hAssoc)))
            {
                continue;
            }
            if (nAssoc = hAssoc.m_ulCount)
            {
                NewArrayHolder<ASSOCIATE_RECORD> rAssoc = new (nothrow) ASSOCIATE_RECORD[nAssoc];
                if (FAILED(g_pImport->GetAllAssociates(&hAssoc,rAssoc,nAssoc)))
                {
                    continue;
                }
                
                for (unsigned i=0; i < nAssoc;i++)
                {
                    if (TypeFromToken(rAssoc[i].m_memberdef) == mdtMethodDef)
                    {
                        DWORD dwAttrs;
                        if (FAILED(g_pImport->GetMethodDefProps(rAssoc[i].m_memberdef, &dwAttrs)))
                        {
                            continue;
                        }
                        if(g_fHidePub && IsMdPublic(dwAttrs)) continue;
                        if(g_fHidePriv && IsMdPrivate(dwAttrs)) continue;
                        if(g_fHideFam && IsMdFamily(dwAttrs)) continue;
                        if(g_fHideAsm && IsMdAssem(dwAttrs)) continue;
                        if(g_fHideFOA && IsMdFamORAssem(dwAttrs)) continue;
                        if(g_fHideFAA && IsMdFamANDAssem(dwAttrs)) continue;
                        if(g_fHidePrivScope && IsMdPrivateScope(dwAttrs)) continue;
                    }
                    AddEventToGUI(cl, pClassItem, pszNamespace, pszClassName, dwClassAttrs, curMem->token);
                    break;
                }
            }
            g_pImport->EnumClose(&hAssoc);
        }
        else AddEventToGUI(cl, pClassItem, pszNamespace, pszClassName, dwClassAttrs, curMem->token);
        curMem++;
    }
    // properties fourth
    curMem = members;
    for (; i < NumMembers; i++)
    {
        if (TypeFromToken(pMemberList[i]) == mdtProperty)
        {
            curMem->token = pMemberList[i];
            if (FAILED(g_pImport->GetPropertyProps(
                curMem->token, 
                &curMem->pszMemberName, 
                &curMem->dwAttrs, 
                &curMem->pComSig, 
                &curMem->cComSig)))
            {
                curMem->pszMemberName = "Invalid Property record";
                curMem->dwAttrs = 0;
                curMem->pComSig = NULL;
                curMem->cComSig = 0;
            }
            MAKE_NAME_IF_NONE(curMem->pszMemberName,pMemberList[i]);
            curMem++;
        }
    }

    endMem = curMem;
    if(g_fSortByName) qsort(members, endMem - members, sizeof MemberInfo, memberCmp);
    curMem = members;
    while(curMem < endMem)
    {
        if (g_fLimitedVisibility)
        {
            HENUMInternal   hAssoc;
            unsigned nAssoc;
            if (FAILED(g_pImport->EnumAssociateInit(curMem->token,&hAssoc)))
            {
                continue;
            }
            if (nAssoc = hAssoc.m_ulCount)
            {
                NewArrayHolder<ASSOCIATE_RECORD> rAssoc = new (nothrow) ASSOCIATE_RECORD[nAssoc];
                if (FAILED(g_pImport->GetAllAssociates(&hAssoc,rAssoc,nAssoc)))
                {
                    continue;
                }
                
                for (unsigned i=0; i < nAssoc;i++)
                {
                    if (TypeFromToken(rAssoc[i].m_memberdef) == mdtMethodDef)
                    {
                        DWORD dwAttrs;
                        if (FAILED(g_pImport->GetMethodDefProps(rAssoc[i].m_memberdef, &dwAttrs)))
                        {
                            continue;
                        }
                        if(g_fHidePub && IsMdPublic(dwAttrs)) continue;
                        if(g_fHidePriv && IsMdPrivate(dwAttrs)) continue;
                        if(g_fHideFam && IsMdFamily(dwAttrs)) continue;
                        if(g_fHideAsm && IsMdAssem(dwAttrs)) continue;
                        if(g_fHideFOA && IsMdFamORAssem(dwAttrs)) continue;
                        if(g_fHideFAA && IsMdFamANDAssem(dwAttrs)) continue;
                        if(g_fHidePrivScope && IsMdPrivateScope(dwAttrs)) continue;
                    }
                    AddPropToGUI(cl, pClassItem, pszNamespace, pszClassName, dwClassAttrs, curMem->token);
                    break;
                }
            }
            g_pImport->EnumClose(&hAssoc);
        }
        else AddPropToGUI(cl, pClassItem, pszNamespace, pszClassName, dwClassAttrs, curMem->token);
        curMem++;
    }
    g_fDumpRTF = fDumpRTF;
    if(pMemberList) delete[] pMemberList;
    if(members) delete[] members;
#ifdef _PREFAST_
#pragma warning(pop)
#endif
    return hClassRoot;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

void CreateMenus()
{
    HMENU   hMenuPopup;

    g_hMenu = CreateMenu();

    hMenuPopup = CreateMenu();
    WszAppendMenu(hMenuPopup, MF_STRING, IDM_OPEN, RstrW(IDS_OPEN));
    WszAppendMenu(hMenuPopup, MF_STRING|MF_GRAYED, IDM_DUMP, RstrW(IDS_DUMP));
    WszAppendMenu(hMenuPopup, MF_STRING|MF_GRAYED, IDM_DUMP_TREE, RstrW(IDS_DUMPTREE));
    WszAppendMenu(hMenuPopup, MF_STRING, IDM_EXIT, RstrW(IDS_EXIT));
    WszAppendMenu(g_hMenu, MF_POPUP, (UINT)(UINT_PTR)hMenuPopup, RstrW(IDS_FILE));
    g_hFileMenu = hMenuPopup;

    hMenuPopup = CreateMenu();
    g_hFontMenu = CreateMenu();
    WszAppendMenu(hMenuPopup,MF_POPUP,(UINT)(UINT_PTR)g_hFontMenu,RstrW(IDS_FONTS));
    WszAppendMenu(g_hFontMenu,MF_STRING,IDM_FONT_TREE,RstrW(IDS_FONT_TREE));
    WszAppendMenu(g_hFontMenu,MF_STRING,IDM_FONT_DASM,RstrW(IDS_FONT_DASM));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fSortByName ? MF_CHECKED : MF_UNCHECKED), IDM_SORT_BY_NAME, RstrW(IDS_SORT_BY_NAME));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fTreeViewFCN ? MF_CHECKED : MF_UNCHECKED), IDM_TREEVIEWFCN, RstrW(IDS_TREEVIEWFCN));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fCAVerbal ? MF_CHECKED : MF_UNCHECKED), IDM_CAVERBAL, RstrW(IDS_CAVERBAL));
    //WszAppendMenu(hMenuPopup, MF_STRING|(g_fDumpRTF ? MF_CHECKED : MF_UNCHECKED), IDM_DUMPRTF, RstrW(IDS_DUMPRTF));
    // MF_SEPARATOR ==> last 2 params ignored
    WszAppendMenu(hMenuPopup, MF_SEPARATOR,0,NULL);
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fHidePub ? MF_CHECKED : MF_UNCHECKED), IDM_SHOW_PUB, RstrW(IDS_SHOW_PUB));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fHidePriv ? MF_CHECKED : MF_UNCHECKED), IDM_SHOW_PRIV, RstrW(IDS_SHOW_PRIV));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fHideFam ? MF_CHECKED : MF_UNCHECKED), IDM_SHOW_FAM, RstrW(IDS_SHOW_FAM));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fHideAsm ? MF_CHECKED : MF_UNCHECKED), IDM_SHOW_ASM, RstrW(IDS_SHOW_ASM));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fHideFAA ? MF_CHECKED : MF_UNCHECKED), IDM_SHOW_FAA, RstrW(IDS_SHOW_FAA));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fHideFOA ? MF_CHECKED : MF_UNCHECKED), IDM_SHOW_FOA, RstrW(IDS_SHOW_FOA));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fHidePrivScope ? MF_CHECKED : MF_UNCHECKED), IDM_SHOW_PSCOPE, RstrW(IDS_SHOW_PSCOPE));
    WszAppendMenu(hMenuPopup, MF_SEPARATOR,0,NULL);
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fFullMemberInfo ? MF_CHECKED : MF_UNCHECKED), IDM_FULL_INFO, RstrW(IDS_FULL_INFO));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fShowBytes ? MF_CHECKED : MF_UNCHECKED), IDM_BYTES, RstrW(IDS_BYTES));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fDumpTokens ? MF_CHECKED : MF_UNCHECKED), IDM_TOKENS, RstrW(IDS_TOKENS));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fShowSource ? MF_CHECKED : MF_UNCHECKED), IDM_SOURCELINES, RstrW(IDS_SOURCELINES));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fQuoteAllNames ? MF_CHECKED : MF_UNCHECKED), IDM_QUOTEALLNAMES, RstrW(IDS_QUOTEALLNAMES));
    WszAppendMenu(hMenuPopup, MF_STRING|(g_fTryInCode ? MF_CHECKED : MF_UNCHECKED), IDM_EXPANDTRY, RstrW(IDS_EXPANDTRY));
    if(g_fTDC)
    {

        WszAppendMenu(hMenuPopup, MF_STRING, IDM_SHOW_HEADER, RstrW(IDS_SHOW_HEADER));
        WszAppendMenu(hMenuPopup, MF_STRING, IDM_SHOW_STAT, RstrW(IDS_SHOW_STAT));
        g_hMetaInfoMenu = CreateMenu();
        //MENUINFO mi;
        //GetMenuInfo(g_hMetaInfoMenu,&mi);
        //mi.dwStyle |= MNS_MODELESS;
        //SetMenuInfo(g_hMetaInfoMenu,&mi);
        WszAppendMenu(hMenuPopup, MF_POPUP, (UINT)(UINT_PTR)g_hMetaInfoMenu, RstrW(IDS_METAINFO));
        
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING|(g_ulMetaInfoFilter & MDInfo::dumpMoreHex ? MF_CHECKED : MF_UNCHECKED),IDM_MI_HEX,RstrW(IDS_MI_HEX));
        WszAppendMenu(g_hMetaInfoMenu,MF_SEPARATOR,0,NULL);
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING|(g_ulMetaInfoFilter & MDInfo::dumpCSV ? MF_CHECKED : MF_UNCHECKED),IDM_MI_CSV,RstrW(IDS_MI_CSV));
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING|(g_ulMetaInfoFilter & MDInfo::dumpHeader ?   MF_CHECKED : MF_UNCHECKED),IDM_MI_HEADER,RstrW(IDS_MI_HEADER));
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING|(g_ulMetaInfoFilter & MDInfo::dumpSchema ? MF_CHECKED : MF_UNCHECKED),IDM_MI_SCHEMA,RstrW(IDS_MI_SCHEMA));
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING|(g_ulMetaInfoFilter & MDInfo::dumpRaw ? MF_CHECKED : MF_UNCHECKED),IDM_MI_RAW,RstrW(IDS_MI_RAW));
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING|(g_ulMetaInfoFilter & MDInfo::dumpRawHeaps ? MF_CHECKED : MF_UNCHECKED),IDM_MI_HEAPS,RstrW(IDS_MI_HEAPS));
        WszAppendMenu(g_hMetaInfoMenu,MF_SEPARATOR,0,NULL);
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING|(g_ulMetaInfoFilter & MDInfo::dumpUnsat ? MF_CHECKED : MF_UNCHECKED),IDM_MI_UNREX,RstrW(IDS_MI_UNREX));
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING|(g_ulMetaInfoFilter & MDInfo::dumpValidate ? MF_CHECKED : MF_UNCHECKED),IDM_MI_VALIDATE,RstrW(IDS_MI_VALIDATE));
        WszAppendMenu(g_hMetaInfoMenu,MF_STRING,IDM_SHOW_METAINFO,RstrW(IDS_SHOW_METAINFO));
    }
    WszAppendMenu(g_hMenu, MF_POPUP|MF_GRAYED, (UINT)(UINT_PTR)hMenuPopup, RstrW(IDS_VIEW));
    g_hViewMenu = hMenuPopup;
    hMenuPopup = CreateMenu();
    WszAppendMenu(hMenuPopup, MF_STRING, IDM_HELP,RstrW(IDS_HELP));
    WszAppendMenu(hMenuPopup, MF_STRING, IDM_ABOUT,RstrW(IDS_ABOUT));
    WszAppendMenu(g_hMenu, MF_POPUP, (UINT)(UINT_PTR)hMenuPopup, RstrW(IDS_HELP));
}

BOOL LoadImages()
{
    int i;

    g_hImageList = ImageList_Create(BITMAP_WIDTH, BITMAP_HEIGHT, ILC_COLOR8, LAST_IMAGE_INDEX, 1);
    if (g_hImageList == NULL)
        return FALSE;

    _ASSERTE(g_hResources != NULL);
    for (i = 0; i < LAST_IMAGE_INDEX; i++)
    {
        g_hBitmaps[i] = (HBITMAP) WszLoadImage(
            g_hResources,
            MAKEINTRESOURCE(i + IDB_CLASS),
            IMAGE_BITMAP,
            15,
            15,
            LR_LOADTRANSPARENT  //LR_DEFAULTCOLOR
        );
        if (g_hBitmaps[i] == NULL)
            return FALSE;
        int index = ImageList_Add(g_hImageList, g_hBitmaps[i], NULL);
        if (index != i)
            return FALSE;
    }

    return TRUE;
}
// Local functions for font persistence:
char* FontSaveFileName()
{
    static char szFileName[MAX_PATH];
    static BOOL bInit = TRUE;
    if(bInit)
    {
        (void)GetWindowsDirectoryA(szFileName,MAX_PATH);
        if(szFileName[strlen(szFileName)-1]!='\\') strcat_s(szFileName,MAX_PATH,"\\");
        strcat_s(szFileName,MAX_PATH,"ildasmfnt.bin");
        bInit = FALSE;
    }
    return szFileName;
}
BOOL LoadGUIFonts(GUI_Info* pguiInfo)
{
    FILE*   pF = NULL;
    BOOL ret = FALSE;
    int dummy;
    if(fopen_s(&pF,FontSaveFileName(),"rb")==0)
    {
        ret = (fread(pguiInfo->plfDasm,sizeof(LOGFONTW),1,pF) && fread(pguiInfo->plfTree,sizeof(LOGFONTW),1,pF));
        if(fread(&dummy,sizeof(int),1,pF)) pguiInfo->x = dummy;
        if(fread(&dummy,sizeof(int),1,pF)) pguiInfo->y = dummy;
        if(fread(&dummy,sizeof(int),1,pF)) pguiInfo->w = dummy;
        if(fread(&dummy,sizeof(int),1,pF)) pguiInfo->h = dummy;
        if(fread(&dummy,sizeof(int),1,pF)) g_fTreeViewFCN    = (dummy != 0);
        if(fread(&dummy,sizeof(int),1,pF)) g_fSortByName     = (dummy != 0);
        if(fread(&dummy,sizeof(int),1,pF)) g_fFullMemberInfo = (dummy != 0);

        fclose(pF);
    }
    return ret;
}
BOOL SaveGUIFonts(GUI_Info* pguiInfo)
{
    FILE*   pF=NULL;
    BOOL ret = FALSE;
    int dummyFCN = (g_fTreeViewFCN ? 1:0);
    int dummySBN = (g_fSortByName  ? 1:0);
    int dummyFMI = (g_fFullMemberInfo ? 1:0);
    if(fopen_s(&pF,FontSaveFileName(),"wb")==0)
    {
        ret = (fwrite(pguiInfo->plfDasm,sizeof(LOGFONTW),1,pF) 
               && fwrite(pguiInfo->plfTree,sizeof(LOGFONTW),1,pF)
               && fwrite(&(pguiInfo->x),sizeof(int),1,pF)
               && fwrite(&(pguiInfo->y),sizeof(int),1,pF)
               && fwrite(&(pguiInfo->w),sizeof(int),1,pF)
               && fwrite(&(pguiInfo->h),sizeof(int),1,pF)
               && fwrite(&dummyFCN,sizeof(int),1,pF)
               && fwrite(&dummySBN,sizeof(int),1,pF)
               && fwrite(&dummyFMI,sizeof(int),1,pF)
               );
        fclose(pF);
    }
    return ret;
}
// Init various GUI variables, get handles
// if InitGUI returns FALSE, ildasm exits
#define DEFAULT_FONTS
BOOL InitGUI()
{
    INITCOMMONCONTROLSEX    InitInfo;
#ifdef DEFAULT_FONTS
    LOGFONTW strDefaultLogFontDasm = {-14,0,0,0,FW_REGULAR,0,0,0,ANSI_CHARSET,
        OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,ANTIALIASED_QUALITY,FIXED_PITCH | FF_MODERN,L"Fixedsys"};
    LOGFONTW strDefaultLogFontTree = {-11,0,0,0,FW_REGULAR,0,0,0,ANSI_CHARSET,
        OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,ANTIALIASED_QUALITY,VARIABLE_PITCH | FF_SWISS,L"Tahoma"};
    LOGFONTA strDefaultLogFontDasmA = {-14,0,0,0,FW_REGULAR,0,0,0,ANSI_CHARSET,
        OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,ANTIALIASED_QUALITY,FIXED_PITCH | FF_MODERN,"Fixedsys"};
    LOGFONTA strDefaultLogFontTreeA = {-11,0,0,0,FW_REGULAR,0,0,0,ANSI_CHARSET,
        OUT_DEFAULT_PRECIS,CLIP_DEFAULT_PRECIS,ANTIALIASED_QUALITY,VARIABLE_PITCH | FF_SWISS,"Tahoma"};
#endif
    g_DisasmBox = new DynamicArray<DisasmBox_t>;
    g_ClassItemList = new DynamicArray<ClassItem_t>;
    g_NamespaceList = new DynamicArray<Namespace_t>;
    WszLoadLibrary(L"riched20.dll");

    InitInfo.dwSize = sizeof(InitInfo);
    InitInfo.dwICC = ICC_LISTVIEW_CLASSES;

    if (InitCommonControlsEx(&InitInfo) == FALSE)
        return FALSE;

    g_hInstance = WszGetModuleHandle(NULL);
    g_hResources = LoadLocalizedResourceDLLForSDK(L"ildasmrc.dll");

    //--------- get logical fonts
#ifdef DEFAULT_FONTS
    if(!LoadGUIFonts(&guiInfo))
    {
        memcpy(&g_strLogFontDasm,&strDefaultLogFontDasm,sizeof(LOGFONTW));
        memcpy(&g_strLogFontTree,&strDefaultLogFontTree,sizeof(LOGFONTW));
    }
    if(g_fDumpRTF) { g_strLogFontDasm.lfWeight = FW_REGULAR; g_strLogFontDasm.lfItalic = FALSE; }
    // -------- create font for disassembly window
    g_hFixedFont = CreateFontIndirectW(&g_strLogFontDasm);
    // -------- create font for tree view
    g_hSmallFont = CreateFontIndirectW(&g_strLogFontTree);
#else
    if(LoadGUIFonts(&guiInfo))
    {
        if(g_fDumpRTF) { g_strLogFontDasm.lfWeight = FW_REGULAR; g_strLogFontDasm.lfItalic = FALSE; }
        // -------- create font for disassembly window
        g_hFixedFont = CreateFontIndirect(&g_strLogFontDasm);
        // -------- create font for tree view
        g_hSmallFont = CreateFontIndirect(&g_strLogFontTree);
    }
    else
    {
        g_hFixedFont = (HFONT)GetStockObject(SYSTEM_FIXED_FONT);
        g_hSmallFont = (HFONT)GetStockObject(DEFAULT_GUI_FONT);
    }
#endif
    if (g_hFixedFont == NULL) return FALSE;
    if (g_hSmallFont == NULL) return FALSE;

    memset(&g_strChFontDasm,0,sizeof(CHOOSEFONT));
    g_strChFontDasm.lStructSize = sizeof(CHOOSEFONT);
    g_strChFontDasm.lpLogFont = &g_strLogFontDasm;
    g_strChFontDasm.Flags = CF_INITTOLOGFONTSTRUCT | CF_SCREENFONTS |CF_SHOWHELP;
    if(!g_fDumpRTF) g_strChFontDasm.Flags |= CF_EFFECTS; // no color change option for RTF output!
    g_strChFontDasm.rgbColors = GetSysColor(COLOR_INFOTEXT);

    memset(&g_strChFontTree,0,sizeof(CHOOSEFONTW));
    g_strChFontTree.lStructSize = sizeof(CHOOSEFONTW);
    g_strChFontTree.lpLogFont = &g_strLogFontTree;
    g_strChFontTree.Flags = CF_INITTOLOGFONTSTRUCT | CF_SCREENFONTS |CF_SHOWHELP /*| CF_EFFECTS*/;
    g_strChFontTree.rgbColors = GetSysColor(COLOR_WINDOWTEXT);

    g_hWhiteBrush = (HBRUSH) GetStockObject(WHITE_BRUSH);
    if (g_hWhiteBrush == NULL)
        return FALSE;

    if (LoadImages() == FALSE)
        return FALSE;

    if (RegisterWindowClasses() == FALSE)
        return FALSE;
#undef RegisterWindowMessageW
    g_uFindReplaceMsg = RegisterWindowMessageW(FINDMSGSTRING);

    CreateMenus();

    return TRUE;
}

void DestroyGUI()
{
    SDELETE(g_DisasmBox);
    SDELETE(g_ClassItemList);
    SDELETE(g_NamespaceList);
}
//
// Set the font of a particular window to the global fixed size font
//
void SetWindowFontFixed(HWND hwnd)
{
    WszSendMessage(
        hwnd,
        WM_SETFONT,
        (LPARAM) g_hFixedFont,
        FALSE
    );
}


//
// Set the char dimensions variables
//
void SetCharDimensions(HWND hwnd)
{
    if (InterlockedIncrement(&g_SetCharDimensions) == 1)
    {
        HDC         hdc;
        TEXTMETRIC  tm;

        hdc = GetDC(hwnd);

        GetTextMetrics(hdc, &tm);

        g_MaxCharWidth  = tm.tmAveCharWidth;
        g_Height        = tm.tmHeight;

        ReleaseDC(hwnd, hdc);
    }
    else
    {
        // Already set
        InterlockedDecrement(&g_SetCharDimensions);
    }
}


//
// Given a member handle and a class item, find the TreeItem for that member
//
TreeItem_t *FindMemberInClass(ClassItem_t *pClassItem, HTREEITEM hMember)
{
    DWORD i;

    for (i = 0; i < pClassItem->SubItems; i++)
    {
        if (pClassItem->pMembers[i].hItem == hMember)
            return &pClassItem->pMembers[i];
    }

    return NULL;
}


//
// Register the window classes
//
BOOL RegisterWindowClasses()
{
    _ASSERTE(g_hResources != NULL);
    WNDCLASSW   wndClass;

    wndClass.style          = CS_DBLCLKS|CS_HREDRAW|CS_VREDRAW;
    wndClass.lpfnWndProc    = DisassemblyWndProc;
    wndClass.cbClsExtra     = 0;
    wndClass.cbWndExtra     = 0;
    wndClass.hInstance      = g_hInstance;
    wndClass.hIcon          = WszLoadIcon(g_hResources,MAKEINTRESOURCE(IDI_ICON2));
    wndClass.hCursor        = NULL;
    wndClass.hbrBackground  = g_hWhiteBrush;
    wndClass.lpszMenuName   = NULL;
    wndClass.lpszClassName  = DISASSEMBLY_CLASS_NAMEW;
    if (WszRegisterClass((WNDCLASSW*)(&wndClass)) == 0)
        return FALSE;

    wndClass.style          = CS_DBLCLKS|CS_HREDRAW|CS_VREDRAW;
    wndClass.lpfnWndProc    = MainWndProc;
    wndClass.cbClsExtra     = 0;
    wndClass.cbWndExtra     = 0;
    wndClass.hInstance      = g_hInstance;
    wndClass.hIcon          = WszLoadIcon(g_hResources,MAKEINTRESOURCE(IDI_ICON2));
    wndClass.hCursor        = NULL;
    wndClass.hbrBackground  = g_hWhiteBrush;
    wndClass.lpszMenuName   = NULL;

    wndClass.lpszClassName  = MAIN_WINDOW_CLASSW;
    if (WszRegisterClass((WNDCLASSW*)(&wndClass)) == 0)
        return FALSE;
    return TRUE;
}

//
// Dump one item to global buffer
//
void GUIDumpItemToDisassemblyEditBox(void*pvDLB, mdToken cl, mdToken mbMember)
{
    const char *    pszClassName;
    const char *    pszNamespace;
    mdTypeRef       crExtends;
    DWORD           dwClassAttrs;

    if ((cl != mdTokenNil)&&TypeFromToken(mbMember))
    {
        if (FAILED(g_pImport->GetNameOfTypeDef(cl, &pszClassName, &pszNamespace)))
        {
            pszClassName = pszNamespace = "Invalid TypeDef record";
        }
        MAKE_NAME_IF_NONE(pszClassName,cl);
    }
    else
    {
        pszClassName = (TypeFromToken(mbMember) == mdtMethodDef) ? "Global Functions" : "Global Fields";
    }
    memset(GlobalBuffer,0,GlobalBufferLen);
    InGlobalBuffer = 0;

    if (TypeFromToken(mbMember) && cl && (cl != mdTypeDefNil))
    {
        if (FAILED(g_pImport->GetTypeDefProps(cl, &dwClassAttrs, &crExtends)))
        {
            dwClassAttrs = 0;
            crExtends = mdTypeDefNil;
        }
    }
    g_Mode |= MODE_GUI;
    //_ASSERTE(0);
    mdToken tkVarOwner = g_tkVarOwner;
    g_tkVarOwner = cl;
    if(g_fDumpRTF) DumpRTFPrefix(pvDLB,FALSE);
    switch (TypeFromToken(mbMember))
    {
        case 0:
            switch(cl)
            {
                case 0:
                    DumpManifest(pvDLB);
                    DumpTypedefs(pvDLB);
                    break;
                case IDM_SHOW_HEADER:
                    DumpHeader(g_CORHeader,pvDLB);
                    DumpHeaderDetails(g_CORHeader,pvDLB);
                    break;
                case IDM_SHOW_METAINFO:
                    DumpMetaInfo(g_wszFullInputFile,NULL,pvDLB);
                    break;
                case IDM_SHOW_STAT:
                    DumpStatistics(g_CORHeader,pvDLB);
                    break;
            }
            break;

        case mdtTypeDef:
            DumpClass(mbMember,VAL32(g_CORHeader->EntryPointToken), pvDLB, 1); //1 = title+size+pack+custom attributes
            break;
        case mdtFieldDef:
            {
                ULONG ul1,ul2;
                GetClassLayout(cl,&ul1,&ul2);
                DumpField(mbMember,pszClassName, pvDLB, TRUE);
            }
            break;
        case mdtMethodDef:
            DumpMethod(mbMember,pszClassName,VAL32(g_CORHeader->EntryPointToken), pvDLB, TRUE);
            break;
        case mdtEvent:
            DumpEvent(mbMember,pszClassName, dwClassAttrs, pvDLB, TRUE);
            break;
        case mdtProperty:
            DumpProp(mbMember,pszClassName, dwClassAttrs, pvDLB, TRUE);
            break;
    }
    if(g_fDumpRTF) DumpRTFPostfix(pvDLB);
    g_tkVarOwner = tkVarOwner;
    if(g_uCodePage==0xFFFFFFFF)
    {
        SendMessageW((HWND)pvDLB,WM_SETTEXT,0, (LPARAM)GlobalBuffer);
    }
    else
    {
        UINT32 L = (UINT32)strlen(GlobalBuffer);
        WCHAR* wz = new (nothrow) WCHAR[L+4];
        if(wz)
        {
            memset(wz,0,sizeof(WCHAR)*(L+2));
            int x = WszMultiByteToWideChar(CP_UTF8,0,GlobalBuffer,-1,wz,L+2);
            if(g_fDumpRTF)
            {
                x = (int)SendMessageA((HWND)pvDLB,WM_SETTEXT,0, (LPARAM)UnicodeToAnsi(wz));
            }
            else
            {
                SETTEXTEX ste;
                ste.flags = ST_DEFAULT;
                ste.codepage = 1200;
                x = (int)WszSendMessage((HWND)pvDLB,EM_SETTEXTEX,(WPARAM)&ste, (LPARAM)wz);
            }
            delete[] wz;
        }
    }
}

//
// Disassemble the given method in a new window
//

HWND GUIDisassemble(mdTypeDef cl, mdToken mbMember, __in __nullterminated char *pszNiceMemberName)
{
    HWND            hwndDisassemblyMain;
    HWND            hwndDisassemblyListBox;
    const char *    pszClassName;
    const char *    pszNamespace;
    char*           szTemp=NULL;
    RECT            rcl;
    static char szsz[4096];
    bool fUpdate = false;
    bool fMetaInfo = (TypeFromToken(mbMember)==0)&&(cl==IDM_SHOW_METAINFO);
    BOOL fDumpRTF = g_fDumpRTF;
    if(fMetaInfo) g_fDumpRTF = FALSE;

    //before we even try, check if this member's disasm box is already opened
    DisasmBox_t* pDisasmBox = FindDisasmBox(cl, mbMember);
    if(pDisasmBox)
    {
        if(fMetaInfo || (0 == strcmp(pszNiceMemberName,"UpdateThisDisassemblyBox")))
        {
            fUpdate = true;
        }
        else
        {
            PostMessageA(pDisasmBox->hwndContainer,WM_ACTIVATE,WA_CLICKACTIVE,0);
            PostMessageA(pDisasmBox->hwndContainer,WM_SETFOCUS,0,0);
            return pDisasmBox->hwndContainer;
        }
    }
    if(fUpdate)
    {
        SendMessageW(pDisasmBox->hwndContainer,WM_GETTEXT, 0, (LPARAM)szsz);
        strcpy_s(szsz,4096,UnicodeToUtf((WCHAR*)szsz));

        szTemp = szsz;
        PostMessageA(pDisasmBox->hwndContainer,WM_CLOSE,1,0);
    }
    else
    {
        // Prepend class name to nicely formatted member name
        if (mbMember != 0)
        {
            if (cl != mdTokenNil)
            {
                if (FAILED(g_pImport->GetNameOfTypeDef(
                    cl, 
                    &pszClassName, 
                    &pszNamespace)))
                {
                    pszClassName = pszNamespace = "Invalid TypeDef record";
                }
                MAKE_NAME_IF_NONE(pszClassName,cl);
                if(*pszNamespace != 0)
                    sprintf_s(szsz,4096,"%s.",pszNamespace);
                else 
                    szsz[0] = 0;
                strcat_s(szsz,4096,pszClassName);
                pszClassName = (const char*)&szsz[0];
            }
            else
            {
                pszClassName = (TypeFromToken(mbMember) == mdtMethodDef) ? "Global Functions" : "Global Fields";
            }
            szTemp = new (nothrow) char[strlen(pszClassName)+strlen(pszNiceMemberName)+4];
            if(szTemp) sprintf_s(szTemp, strlen(pszClassName)+strlen(pszNiceMemberName)+4,"%s::%s", pszClassName, pszNiceMemberName);
            _ASSERTE(TypeFromToken(mbMember) & (mdtMethodDef|mdtEvent|mdtProperty|mdtTypeDef|mdtFieldDef));
        }
        if(!szTemp) szTemp = pszNiceMemberName;
    }
    _ASSERTE(szTemp);
    
    HMENU hMenu =  CreateMenu();
    WszAppendMenu(hMenu, MF_STRING, IDM_FIND, RstrW(IDS_FIND));
    WszAppendMenu(hMenu, MF_STRING, IDM_FINDNEXT, RstrW(IDS_FINDNEXT));
    
    hwndDisassemblyMain = WszCreateWindowEx(
        WS_EX_CLIENTEDGE,
        DISASSEMBLY_CLASS_NAMEW,
        UtfToUnicode(szTemp),
        WS_OVERLAPPEDWINDOW | WS_SIZEBOX,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        640,
        400,
        NULL,
        hMenu, // menu
        g_hInstance, // hinst
        NULL
    );
    SendMessageW(hwndDisassemblyMain,WM_SETTEXT, 0, (LPARAM)(UtfToUnicode(szTemp)));

    if((!fUpdate) && szTemp &&(szTemp != pszNiceMemberName)) delete[] szTemp;
    if (hwndDisassemblyMain == NULL)
    {
        g_fDumpRTF = fDumpRTF;
        return NULL;
    }
    GetClientRect(hwndDisassemblyMain, &rcl);

    hwndDisassemblyListBox = WszCreateWindowEx(
        0,
        (!g_fDumpRTF) ? L"RichEdit20W" : L"RichEdit20A",
        RstrW(IDS_TEXTTOOLARGEFORGUI),
        WS_CHILD | WS_VSCROLL | WS_HSCROLL | WS_VISIBLE
        | ES_MULTILINE | ES_READONLY | ES_AUTOHSCROLL | ES_AUTOVSCROLL | ES_NOHIDESEL,
        rcl.left,
        rcl.top,
        rcl.right - rcl.left,
        rcl.bottom - rcl.top,
        hwndDisassemblyMain,
        (HMENU) ID_LISTBOX,
        g_hInstance, // hinst
        NULL
    );
    
    DWORD e = GetLastError();
    _ASSERTE(hwndDisassemblyListBox);
    if (hwndDisassemblyListBox == NULL)
    {
        DestroyWindow(hwndDisassemblyMain);
        return NULL;
    }

    if(fUpdate)
    {
        UpdateDisasmBox(pDisasmBox, hwndDisassemblyMain, hwndDisassemblyListBox, hMenu);
    }
    else
        AddDisasmBox(hwndDisassemblyMain, hwndDisassemblyListBox, hMenu, cl, mbMember);

    SendMessage(hwndDisassemblyListBox,EM_SETTEXTMODE,TM_RICHTEXT|TM_MULTICODEPAGE,0);
    SendMessage(hwndDisassemblyListBox,EM_SETBKGNDCOLOR,0,GetSysColor(COLOR_INFOBK));
    SetWindowFontFixed(hwndDisassemblyListBox);
    SetCharDimensions(hwndDisassemblyListBox);

    GUIDumpItemToDisassemblyEditBox((void *)hwndDisassemblyListBox,cl,mbMember);

    ShowWindow(hwndDisassemblyMain, SW_SHOWNORMAL);
    UpdateWindow(hwndDisassemblyMain);
    
    g_fDumpRTF = fDumpRTF;
    return hwndDisassemblyMain;
}

//
// Callback by the disassembler to add another entry to the disassembly window
//
void GUIAddOpcode(__inout_opt __nullterminated const char *pszString, __in_opt void *GUICookie)
{
    if(pszString)
    {
        ULONG L = (g_uCodePage == 0xFFFFFFFF) ? (ULONG)(wcslen((WCHAR*)pszString)*sizeof(WCHAR)) : (ULONG)strlen(pszString);
        if(InGlobalBuffer+L >= GlobalBufferLen-4)
        {
            ULONG LL = ((L >> 12)+1)<<12;
            char *pch = new (nothrow) char[GlobalBufferLen + LL];
            if(pch)
            {
                memcpy(pch,GlobalBuffer,InGlobalBuffer+1);
                delete[] GlobalBuffer;
                GlobalBuffer = pch;
                GlobalBufferLen += LL;
            }
        }
        if(g_uCodePage == 0xFFFFFFFF)
        {
            if(g_fDumpRTF)
            {
                swprintf_s((WCHAR*)&GlobalBuffer[InGlobalBuffer], (GlobalBufferLen-InGlobalBuffer)/sizeof(WCHAR), L"%s\\line\r\n",(WCHAR*)pszString);
                InGlobalBuffer += L+14;
            }
            else
            {
                swprintf_s((WCHAR*)&GlobalBuffer[InGlobalBuffer], (GlobalBufferLen-InGlobalBuffer)/sizeof(WCHAR), L"%s\r\n",(WCHAR*)pszString);
                InGlobalBuffer += L+4;
            }
        }
        else
        {
            if(g_fDumpRTF)
            {
                sprintf_s(&GlobalBuffer[InGlobalBuffer],GlobalBufferLen-InGlobalBuffer,"%s\\line\r\n",pszString);
                InGlobalBuffer += L+7;
            }
            else
            {
                sprintf_s(&GlobalBuffer[InGlobalBuffer],GlobalBufferLen-InGlobalBuffer,"%s\r\n",pszString);
                InGlobalBuffer += L+2;
            }
        }
    }
    else
    {
        delete[] GlobalBuffer;
        GlobalBufferLen = 0;
        InGlobalBuffer = 0;
    }
}

//
// Someone has double clicked on an item
//
// It could be a method (diassemble it), or a field (ignore it), or an "extends" or "implements"
// component, in which case we select that component if available.
//
HWND DoubleClickSelectedMember(HTREEITEM hItem)
{
    HTREEITEM hClass;
    ClassItem_t *pClassItem;

    static HCURSOR hWaitCursor = NULL;

    if (hWaitCursor == NULL)
        hWaitCursor = WszLoadCursor(NULL,IDC_WAIT);

    //
    // It could be any item, but assume it's a member item or class info and find its parent
    //
    hClass = TreeView_GetParent(g_hwndTreeView, hItem);
    if (hClass == NULL)
        return NULL;

    //
    // Find the class item given the HTREEITEM
    // (will return NULL if hClass is not really a class item)
    //
    pClassItem = FindClassItem(hClass);
    if (pClassItem != NULL)
    {
        // Which subitem was it?
        TreeItem_t *pItem = FindMemberInClass(pClassItem, hItem);

        if (pItem == NULL)
            return NULL;

        if (pItem->Discriminator == TREEITEM_TYPE_MEMBER)
        {
            TVITEMA      SelItem;
            char*        szText;
            // Must be a method, event or property
            switch (TypeFromToken(pItem->mbMember))
            {
                case mdtMethodDef:
                case mdtEvent:
                case mdtProperty:
                case mdtFieldDef:
                    break;
                default:
                    return NULL;
            }


            // Get the name of this item so that we can title the disassembly window
            szText = new (nothrow) char[8192];
            if(szText)
            {
                memset(&SelItem, 0, sizeof(SelItem));
                SelItem.mask = TVIF_TEXT;
                SelItem.pszText = szText;
                SelItem.hItem = pItem->hItem;
                SelItem.cchTextMax = 8192;

                WCHAR* wzText = (WCHAR*)szText;
                SelItem.cchTextMax /= sizeof(WCHAR);
                SendMessageW(g_hwndTreeView, TVM_GETITEMW, 0, (LPARAM) (LPTVITEMW) &SelItem);
                unsigned L = ((unsigned)wcslen(wzText)+1)*3;
                char*   szUTFText = new (nothrow) char[L];
                if(szUTFText)
                {
                    memset(szUTFText,0,L);
                    WszWideCharToMultiByte(CP_UTF8,0,wzText,-1,szUTFText,L,NULL,NULL);
                    delete[] wzText;
                    szText = szUTFText;
                }
            }
            HCURSOR hWasCursor = SetCursor(hWaitCursor);

            HWND hRet = GUIDisassemble(pClassItem->cl, pItem->mbMember, szText? szText : "");
            if(szText) delete[] szText;

            SetCursor(hWasCursor);

            return hRet;
        }
        else if (pItem->Discriminator == TREEITEM_TYPE_INFO)
        {
            if(pItem->mbMember)
            {
                if(pItem->mbMember != 0xFFFFFFFF)
                {
                    // We've clicked on an "extends X" or "implements Y", so select that class
                    SelectClassByToken(pItem->mbMember);
                }
                else
                {
                    HCURSOR hWasCursor = SetCursor(hWaitCursor);

                    HWND hRet = GUIDisassemble(0, 0, "MANIFEST");

                    SetCursor(hWasCursor);
                    return hRet;
                }
            }
            else
            {
                TVITEMA      SelItem;
                char*       szText = new (nothrow) char[8192];
                if(szText)
                {
                    // Get the name of this item so that we can title the disassembly window
                    memset(&SelItem, 0, sizeof(SelItem));
                    SelItem.mask = TVIF_TEXT;
                    SelItem.pszText = szText;
                    SelItem.hItem = pItem->hItem;
                    SelItem.cchTextMax = 8192;

                    WCHAR* wzText = (WCHAR*)szText;
                    SelItem.cchTextMax /= sizeof(WCHAR);
                    SendMessageW(g_hwndTreeView, TVM_GETITEMW, 0, (LPARAM) (LPTVITEMW) &SelItem);
                    unsigned L = ((unsigned)wcslen(wzText)+1)*3;
                    char*   szUTFText = new (nothrow) char[L];
                    memset(szUTFText,0,L);
                    WszWideCharToMultiByte(CP_UTF8,0,wzText,-1,szUTFText,L,NULL,NULL);
                    delete[] wzText;
                    szText = szUTFText;
                }
                HCURSOR hWasCursor = SetCursor(hWaitCursor);

                HWND hRet = GUIDisassemble(pClassItem->cl, pClassItem->cl, szText ? szText : "");
                if(szText) delete[] szText;

                SetCursor(hWasCursor);

                return hRet;
            }
        }
    }
    return NULL;
}


void SelectClassByName(__in __nullterminated char *pszFQName)
{
    ClassItem_t *pDestItem;

    // Find namespace
    char *p = ns::FindSep(pszFQName);
    if (p == NULL)
    {
        pDestItem = FindClassItem(NULL, pszFQName);
    }
    else
    {
        char szBuffer[MAX_CLASSNAME_LENGTH];
        strncpy_s(szBuffer, MAX_CLASSNAME_LENGTH,pszFQName, p - pszFQName);
        szBuffer[ p - pszFQName ] = '\0';
        pDestItem = FindClassItem(szBuffer, p+1);
    }

    if (pDestItem != NULL)
    {
        SendMessageA(g_hwndTreeView, TVM_SELECTITEM, TVGN_CARET, (LPARAM) (LPTVITEM) pDestItem->hItem);
    }
}

void SelectClassByToken(mdToken tk)
{
    if(TypeFromToken(tk)==mdtTypeDef)
    {
        ClassItem_t *pDestItem;
        if(pDestItem = FindClassItem(tk))
        {
            SendMessageA(g_hwndTreeView, TVM_SELECTITEM, TVGN_CARET, (LPARAM) (LPTVITEM) pDestItem->hItem);
        }
    }
}

//
// Text search in rich text edit
//
void FindTextInListbox(HWND hwnd, FINDREPLACEW* lpfr)
{
    HWND hwndLB = FindAssociatedDisassemblyListBox(hwnd);
    if(hwndLB)
    {
        FINDTEXTW strFind;
        DWORD bgn,end;
        SendMessage(hwndLB,EM_GETSEL,(WPARAM)&bgn,(LPARAM)&end);
        if(lpfr->Flags & FR_DOWN)
        {
            strFind.chrg.cpMin=end;
            strFind.chrg.cpMax=-1;
        }
        else
        {
            strFind.chrg.cpMin=bgn;
            strFind.chrg.cpMax=0;
        }
        strFind.lpstrText=lpfr->lpstrFindWhat;
        int pos = (int)SendMessage(hwndLB,EM_FINDTEXTW,(WPARAM)(lpfr->Flags),(LPARAM)&strFind);
        if(pos >= 0)
        {
            //char sz[32];
            //sprintf(sz,"%d:%d",strFind.chrg.cpMin,strFind.chrg.cpMax);
            //MessageBox(hwnd,sz,"Find",MB_OK);
            SendMessage(hwndLB,EM_SETSEL,(WPARAM)pos,(LPARAM)(pos+wcslen(lpfr->lpstrFindWhat)));
        }
    }
}

//
// Disassembly window(s) WndProc
//
LRESULT CALLBACK DisassemblyWndProc(
    HWND    hwnd,
    UINT    uMsg,
    WPARAM  wParam,
    LPARAM  lParam
)
{
    static HBRUSH hBrush=NULL;
    COLORREF    crBackGr;

    static COLORREF crBackGrOld=NULL;

    if (crBackGrOld == NULL)
        crBackGrOld = GetSysColor(COLOR_INFOBK);

    if(uMsg== g_uFindReplaceMsg)
    {
        FINDREPLACEW* lpfr = (FINDREPLACEW*)lParam;
        if(!(lpfr->Flags & FR_DIALOGTERM))
        {
            FindTextInListbox(hwnd,lpfr);
        }
    }
    else
    switch (uMsg)
    {
        case WM_CREATE:
            hBrush = CreateSolidBrush(RGB(255,255,255));
            break;

        //===== Sent by Static (label) and Read-Only Edit field =====
        case WM_CTLCOLORSTATIC:
        case WM_CTLCOLOREDIT:
            if(hBrush) DeleteObject(hBrush);
            crBackGr = GetSysColor(COLOR_INFOBK);
            hBrush = CreateSolidBrush(crBackGr);
            SetBkColor((HDC) wParam, crBackGr);
            if(crBackGr != crBackGrOld)
            {
                g_strChFontDasm.rgbColors = GetSysColor(COLOR_INFOTEXT);
                crBackGrOld = crBackGr;
            }
            SetTextColor((HDC) wParam, g_strChFontDasm.rgbColors);
            return (LRESULT) hBrush;

        //====== Sent by active Edit field ============
        //case WM_CTLCOLOREDIT:
        //    if(hBrush) DeleteObject(hBrush);
        //    hBrush = CreateSolidBrush(RGB(255,255,255));
        //    SetBkColor((HDC) wParam, RGB(255,255,255));
        //    return (LRESULT) hBrush;

        // Ownerdraw stuff
        case WM_MEASUREITEM:
        {
            ((MEASUREITEMSTRUCT *) (lParam))->itemHeight = g_Height;
            break;
        }

        // Ownerdraw stuff
        case WM_DRAWITEM:
        {
            DRAWITEMSTRUCT *pDIS;
            WCHAR           wzBuf[1024];
            int             ItemID;

            pDIS =  (DRAWITEMSTRUCT *) lParam;
            ItemID = (int) pDIS->itemID;

            if (ItemID < 0)
            {
                switch (pDIS->CtlType)
                {
                    case ODT_LISTBOX:
                    {
                        if ((pDIS->itemAction) & (ODA_FOCUS))
                            DrawFocusRect (PHDC, &PRC);
                        break;
                    }

                    case ODT_COMBOBOX:
                    {
                        if ((pDIS->itemAction) & ODS_FOCUS)
                            DrawFocusRect(PHDC, &PRC);
                        break;
                    }
                }

                return TRUE;
            }

            switch (pDIS->CtlType)
            {
                case ODT_LISTBOX:
                    WszSendMessage(pDIS->hwndItem, LB_GETTEXT, pDIS->itemID, (LPARAM)wzBuf);
                    break;

                case ODT_COMBOBOX:
                    WszSendMessage(pDIS->hwndItem, CB_GETLBTEXT, pDIS->itemID, (LPARAM)wzBuf);
                    break;
            }

            int crBack,crText;
            HBRUSH hbrBack;

            if ((pDIS->itemState) & (ODS_SELECTED))
            {
                crBack = GetSysColor(COLOR_HIGHLIGHT);
                crText = GetSysColor(COLOR_HIGHLIGHTTEXT);
            }
            else
            {
                crBack = GetSysColor(COLOR_WINDOW);
                crText = GetSysColor(COLOR_WINDOWTEXT);
            }

            hbrBack = CreateSolidBrush(crBack);
            FillRect(PHDC, &PRC, hbrBack);
            DeleteObject(hbrBack);

            // 0x00bbggrr
            SetBkColor(PHDC, crBack);

            // Instruction counter
            if (wcslen(wzBuf) >= PADDING && isdigit(*wzBuf))
            {
                SetTextColor(PHDC, 0x00FF0000);
                TextOutW(PHDC, PRC.left, PRC.top, wzBuf, 5);

                SetTextColor(PHDC, 0x00005500);
                TextOutW(PHDC, PRC.left + (5*g_MaxCharWidth), PRC.top, &wzBuf[5], PADDING-5);

                SetTextColor(PHDC, crText);
                TextOutW(PHDC, PRC.left + (PADDING*g_MaxCharWidth), PRC.top, &wzBuf[PADDING], (UINT32)wcslen(&wzBuf[PADDING]));
            }
            else
                TextOutW(PHDC, PRC.left, PRC.top, wzBuf, (UINT32)wcslen(wzBuf));

            if ((pDIS->itemState) & (ODS_FOCUS))
                DrawFocusRect(PHDC, &PRC);

            break;
        }


        case WM_COMMAND:
        {
            if(HIWORD(wParam) > 1) break; // we are interested in commands from menu only
            switch (LOWORD(wParam))
            {
                case IDM_FIND:
                {
                    HWND hwndLB = FindAssociatedDisassemblyListBox(hwnd);
                    if(hwndLB)
                    {
                        FINDREPLACEW *pFR = &(FindDisasmBoxByHwnd(hwnd)->strFR);
                        if(pFR && (pFR->Flags&FR_DIALOGTERM))  // i.e., if the box isn't up
                        {
                            DWORD bgn,end;
                            WszSendMessage(hwndLB,EM_GETSEL,(WPARAM)&bgn,(LPARAM)&end);
                            if(end > bgn)
                            {
                                if(end - bgn > 119)
                                    SendMessage(hwndLB,EM_SETSEL,(WPARAM)bgn,(LPARAM)(bgn+119));
                                SendMessage(hwndLB,EM_GETSELTEXT,0,(LPARAM)(pFR->lpstrFindWhat));
                            }
                            pFR->Flags &= ~FR_DIALOGTERM;
                            g_hFindText = FindTextW(pFR);
                        }
                    }
                }
                break;
                case IDM_FINDNEXT:
                {
                    FindTextInListbox(hwnd,&(FindDisasmBoxByHwnd(hwnd)->strFR));
                }
                break;
            }
            break;
        }

        case WM_SETFOCUS:
            SetFocus(FindAssociatedDisassemblyListBox(hwnd));
            break;

        case WM_SIZE:
        {
            DWORD   cxClient = LOWORD(lParam);
            DWORD   cyClient = HIWORD(lParam);
            HWND    hListView;

            // We have to size the listview also

            // Will be NULL if we are ourselves a listview
            hListView = FindAssociatedDisassemblyListBox(hwnd);

            if (hListView != NULL)
            {
                // Resize listview window
                MoveWindow(
                    hListView,
                    0,
                    0,
                    cxClient,
                    cyClient,
                    TRUE // repaint
                );
            }

            break;
        }

        case WM_CLOSE:
            if(hBrush) DeleteObject(hBrush);
            if(LOWORD(wParam)==0) RemoveDisasmBox(hwnd);
            DestroyWindow(hwnd);        // Generates the WM_DESTROY message

        // Shutdown everything if we're just viewing GUI IL and close all our boxes
            if (IsGuiILOnly() && (g_NumDisasmBoxes == 0)) {
                    PostQuitMessage(0);
            }

            break;

        default :
            return WszDefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    return 0;
}

BOOL CALLBACK AboutBoxProc(HWND hwndDlg,  // handle to dialog box
                              UINT uMsg,     // message
                              WPARAM wParam, // first message parameter
                              LPARAM lParam) // second message parameter
{
    switch(uMsg)
    {
        case WM_INITDIALOG:
            {
                WCHAR str[1024];
                WszSendDlgItemMessage(hwndDlg,IDC_ABOUT_LINE1,WM_SETTEXT,0,
                    (LPARAM)RstrW(IDS_ILDASM_TITLE));
                swprintf_s(str,1024,RstrW(IDS_VERSION), VER_FILEVERSION_STR_L); str[1023]=0;
                WszSendDlgItemMessage(hwndDlg,IDC_ABOUT_LINE2,WM_SETTEXT,0,(LPARAM)str);
                WszSendDlgItemMessage(hwndDlg,IDC_ABOUT_LINE3,WM_SETTEXT,0,
                    (LPARAM)RstrW(IDS_LEGALCOPYRIGHT));
            }
            return TRUE;
        case WM_COMMAND:
            switch (LOWORD(wParam))
            {
                case ID_ABOUT_OK:
                    EndDialog(hwndDlg,0);
                    return TRUE;
            }
            break;

    }
    return FALSE;
}

BOOL CALLBACK DumpOptionsProc(HWND hwndDlg,  // handle to dialog box
                              UINT uMsg,     // message
                              WPARAM wParam, // first message parameter
                              LPARAM lParam) // second message parameter
{
    static BOOL fAsmChecked;
    static BOOL fMetaChecked;
    static ULONG uCodePage = 0;

    if (uCodePage == 0)
        uCodePage = g_uCodePage;

    switch(uMsg)
    {
        case WM_INITDIALOG:
            WszSendDlgItemMessage(hwndDlg,IDC_RADIO1,BM_SETCHECK,(uCodePage==g_uConsoleCP ? BST_CHECKED : BST_UNCHECKED),0);
            WszSendDlgItemMessage(hwndDlg,IDC_RADIO2,BM_SETCHECK,(uCodePage==CP_UTF8 ? BST_CHECKED : BST_UNCHECKED),0);
            WszSendDlgItemMessage(hwndDlg,IDC_RADIO3,BM_SETCHECK,(uCodePage==0xFFFFFFFF ? BST_CHECKED : BST_UNCHECKED),0);


            WszSendDlgItemMessage(hwndDlg,IDC_CHECK18,BM_SETCHECK,(g_fShowProgressBar ? BST_CHECKED : BST_UNCHECKED),0);
            WszSendDlgItemMessage(hwndDlg,IDC_CHECK1, BM_SETCHECK,(g_fDumpHeader ? BST_CHECKED : BST_UNCHECKED),0);
            if(g_fTDC)
            {
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK2, BM_SETCHECK,(g_fDumpStats  ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK19, BM_SETCHECK,(g_fDumpClassList  ? BST_CHECKED : BST_UNCHECKED),0);
            }
            else
            {
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK2),  SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK19),  SW_HIDE);
            }
            WszSendDlgItemMessage(hwndDlg,IDC_CHECK3, BM_SETCHECK,(g_fDumpAsmCode ? BST_CHECKED : BST_UNCHECKED),0);
            WszSendDlgItemMessage(hwndDlg,IDC_CHECK4, BM_SETCHECK,(g_fDumpTokens ? BST_CHECKED : BST_UNCHECKED),0);
            WszSendDlgItemMessage(hwndDlg,IDC_CHECK5, BM_SETCHECK,(g_fShowBytes ? BST_CHECKED : BST_UNCHECKED),0);
            WszSendDlgItemMessage(hwndDlg,IDC_CHECK6, BM_SETCHECK,(g_fShowSource ? BST_CHECKED : BST_UNCHECKED),0);
            WszSendDlgItemMessage(hwndDlg,IDC_CHECK20, BM_SETCHECK,(g_fInsertSourceLines ? BST_CHECKED : BST_UNCHECKED),0);
            WszSendDlgItemMessage(hwndDlg,IDC_CHECK7, BM_SETCHECK,(g_fTryInCode ? BST_CHECKED : BST_UNCHECKED),0);
            if(!(fAsmChecked = g_fDumpAsmCode))
            {
                EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK4), FALSE);
                EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK5), FALSE);
                EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK6), FALSE);
                EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK7), FALSE);
            }
            if(g_fTDC)
            {
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK8, BM_SETCHECK,(g_fDumpMetaInfo ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK10,BM_SETCHECK,(g_ulMetaInfoFilter & MDInfo::dumpHeader ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK11,BM_SETCHECK,(g_ulMetaInfoFilter & MDInfo::dumpMoreHex ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK12,BM_SETCHECK,(g_ulMetaInfoFilter & MDInfo::dumpCSV ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK13,BM_SETCHECK,(g_ulMetaInfoFilter & MDInfo::dumpUnsat ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK16,BM_SETCHECK,(g_ulMetaInfoFilter & MDInfo::dumpValidate ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK14,BM_SETCHECK,(g_ulMetaInfoFilter & MDInfo::dumpSchema ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK15,BM_SETCHECK,(g_ulMetaInfoFilter & MDInfo::dumpRaw ? BST_CHECKED : BST_UNCHECKED),0);
                WszSendDlgItemMessage(hwndDlg,IDC_CHECK17,BM_SETCHECK,(g_ulMetaInfoFilter & MDInfo::dumpRawHeaps ? BST_CHECKED : BST_UNCHECKED),0);
                if(!(fMetaChecked = g_fDumpMetaInfo))
                {
                    //EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK9), FALSE);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK10), FALSE);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK11), FALSE);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK12), FALSE);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK13), FALSE);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK14), FALSE);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK15), FALSE);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK16), FALSE);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK17), FALSE);
                }
            }
            else
            {
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK8),  SW_HIDE);
                //ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK9),  SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK10), SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK11), SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK12), SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK13), SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK14), SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK15), SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK16), SW_HIDE);
                ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK17), SW_HIDE);
            }
            ShowWindow(GetDlgItem(hwndDlg,IDC_CHECK9),  SW_HIDE);
            return TRUE;

        case WM_COMMAND:
            switch (LOWORD(wParam))
            {
                case IDC_CHECK3:
                    fAsmChecked = !fAsmChecked;
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK4), fAsmChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK5), fAsmChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK6), fAsmChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK7), fAsmChecked);
                    return TRUE;

                case IDC_CHECK8:
                    fMetaChecked = !fMetaChecked;
                    //EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK9), fMetaChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK10), fMetaChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK11), fMetaChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK12), fMetaChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK13), fMetaChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK14), fMetaChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK15), fMetaChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK16), fMetaChecked);
                    EnableWindow(GetDlgItem(hwndDlg,IDC_CHECK17), fMetaChecked);
                    return TRUE;

                case IDOK:
                    if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_RADIO1, BM_GETCHECK,0,0)) g_uCodePage = g_uConsoleCP;
                    else if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_RADIO2, BM_GETCHECK,0,0)) g_uCodePage = CP_UTF8;
                    else if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_RADIO3, BM_GETCHECK,0,0)) g_uCodePage = 0xFFFFFFFF;
                    uCodePage = g_uCodePage;

                    g_fShowProgressBar = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK18, BM_GETCHECK,0,0));
                    g_fDumpHeader = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK1, BM_GETCHECK,0,0));
                    if(g_fTDC)
                    {
                        g_fDumpStats = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK2, BM_GETCHECK,0,0));
                        g_fDumpClassList = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK19, BM_GETCHECK,0,0));
                    }
                    g_fDumpAsmCode = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK3, BM_GETCHECK,0,0));
                    g_fDumpTokens = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK4, BM_GETCHECK,0,0));
                    g_fShowBytes = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK5, BM_GETCHECK,0,0));
                    g_fShowSource = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK6, BM_GETCHECK,0,0));
                    g_fInsertSourceLines = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK20, BM_GETCHECK,0,0));
                    g_fTryInCode = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK7, BM_GETCHECK,0,0));
                    if(g_fTDC)
                    {
                        g_fDumpMetaInfo = (BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK8, BM_GETCHECK,0,0));
                        g_ulMetaInfoFilter = 0;
                        if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK10, BM_GETCHECK,0,0)) g_ulMetaInfoFilter |= MDInfo::dumpHeader;
                        if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK11, BM_GETCHECK,0,0)) g_ulMetaInfoFilter |= MDInfo::dumpMoreHex;
                        if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK12, BM_GETCHECK,0,0)) g_ulMetaInfoFilter |= MDInfo::dumpCSV;
                        if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK13, BM_GETCHECK,0,0)) g_ulMetaInfoFilter |= MDInfo::dumpUnsat;
                        if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK16, BM_GETCHECK,0,0)) g_ulMetaInfoFilter |= MDInfo::dumpValidate;
                        if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK14, BM_GETCHECK,0,0)) g_ulMetaInfoFilter |= MDInfo::dumpSchema;
                        if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK15, BM_GETCHECK,0,0)) g_ulMetaInfoFilter |= MDInfo::dumpRaw;
                        if(BST_CHECKED==WszSendDlgItemMessage(hwndDlg,IDC_CHECK17, BM_GETCHECK,0,0)) g_ulMetaInfoFilter |= MDInfo::dumpRawHeaps;
                    }
                    EndDialog(hwndDlg,1);
                    return TRUE;

                case IDCANCEL:
                    EndDialog(hwndDlg,0);
                    return TRUE;
            }
            break;

    }
    return FALSE;
}

static HWND help_hw;
void * __cdecl HelpFileLoader(_In_z_ LPCWSTR lpHelpFileName)
{
    return HtmlHelpW(help_hw, lpHelpFileName, HH_DISPLAY_TOPIC, NULL);
}

//
// Main window WndProc
//
#define CHECK_UNCHECK(x)  { x=!x; CheckMenuItem(g_hMenu, LOWORD(wParam), (x ? MF_CHECKED : MF_UNCHECKED)); }

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
LRESULT CALLBACK MainWndProc(
    HWND    hwnd,
    UINT    uMsg,
    WPARAM  wParam,
    LPARAM  lParam
)
{
    HWND hwndDasm;
    static HCURSOR hWaitCursor = NULL;

    if (hWaitCursor == NULL)
        hWaitCursor = WszLoadCursor(NULL,IDC_WAIT);

    switch (uMsg)
    {
        case WM_DROPFILES:
        {
            WCHAR   wzFileName[MAX_FILENAME_LENGTH];
            DragQueryFileW((HDROP)wParam,0,wzFileName,MAX_FILENAME_LENGTH-1);
            memset(g_szInputFile,0,MAX_FILENAME_LENGTH);
            WszWideCharToMultiByte(CP_UTF8,0,wzFileName,-1,g_szInputFile,MAX_FILENAME_LENGTH-1,NULL,NULL);
            GetInputFileFullPath();
            {
                HCURSOR hWasCursor = SetCursor(hWaitCursor);
                GUICleanupClassItems();
                TreeView_DeleteAllItems(g_hwndTreeView);
                Cleanup();
                GUISetModule(g_szInputFile);
                DumpFile();
                SetCursor(hWasCursor);
            }
            DragFinish((HDROP)wParam);
        }
        break;

        case WM_COMMAND:
        {
            if(HIWORD(wParam) > 1) break; // we are interested in commands from menu only
            switch (LOWORD(wParam))
            {
                case IDM_OPEN:
                {
                    WCHAR wzInputFile[MAX_FILENAME_LENGTH];
                    memset(wzInputFile,0,sizeof(wzInputFile));
                    if(strlen(g_szInputFile))
                    {
                        WszMultiByteToWideChar(CP_UTF8,0,g_szInputFile,-1,wzInputFile,MAX_FILENAME_LENGTH-1);
                    }
                    {
                        OPENFILENAMEW ofn;
                        WCHAR* wzFilter = RstrW(IDS_FILTER_IN); //L"PE file (*.exe,*.dll,*.mod,*.mdl,*.winmd)\0*.exe;*.dll;*.mod;*.mdl;*.winmd\0Any type (*.*)\0*.*\0\0";
                        const WCHAR* wzDefltExt = L"exe";
                        for(WCHAR* pwc = wzFilter; pwc = wcschr(pwc,'\t'); pwc++) *pwc = 0;
                        memset(&ofn,0,sizeof(OPENFILENAMEW));
                        ofn.lStructSize = sizeof(OPENFILENAMEW);
                        ofn.hwndOwner = hwnd;
                        ofn.lpstrFilter = wzFilter;
                        ofn.nFilterIndex = 0;
                        ofn.lpstrFile = wzInputFile;
                        ofn.nMaxFile = MAX_FILENAME_LENGTH-1;
                        ofn.Flags = OFN_FILEMUSTEXIST;
                        ofn.lpstrDefExt = wzDefltExt;
                        if(GetOpenFileName(&ofn))
                        {
                            HCURSOR hWasCursor = SetCursor(hWaitCursor);
                            GUICleanupClassItems();
                            TreeView_DeleteAllItems(g_hwndTreeView);
                            Cleanup();
                            memset(g_szInputFile,0,MAX_FILENAME_LENGTH);
                            WszWideCharToMultiByte(CP_UTF8,0,wzInputFile,-1,g_szInputFile,MAX_FILENAME_LENGTH-1,NULL,NULL);
                            GetInputFileFullPath();
                            GUISetModule(g_szInputFile);
                            DumpFile();
                            SetCursor(hWasCursor);
                        }
                    }
                    break;
                }
                case IDM_ABOUT:
                {
                    _ASSERTE(g_hResources != NULL);
                    WszDialogBoxParam(g_hResources,MAKEINTRESOURCE(IDD_ABOUT),hwnd,(DLGPROC)AboutBoxProc,0L);
                    break;
                }
                case IDM_DUMP:
                //case IDM_DUMP_TREE:
                if(g_pImport)
                {
                    unsigned    uWasCodePage = g_uCodePage;
                    WCHAR wzOutputFile[MAX_FILENAME_LENGTH];
                    memset(wzOutputFile,0,sizeof(wzOutputFile));
                    if(strlen(g_szOutputFile))
                    {
                        WszMultiByteToWideChar(CP_UTF8,0,g_szOutputFile,-1,wzOutputFile,MAX_FILENAME_LENGTH-1);
                    }
                    {
                        OPENFILENAMEW ofn;
                        WCHAR* wzFilter = RstrW(IDS_FILTER_OUT);//L"IL file (*.il)\0*.il\0Text file (*.txt) \0*.txt\0Any type (*.*)\0*.*\0\0";
                        const WCHAR* wzDefltExt = L"il";
                        for(WCHAR* pwc = wzFilter; pwc = wcschr(pwc,'\t'); pwc++) *pwc = 0;
                        memset(&ofn,0,sizeof(OPENFILENAMEW));
                        ofn.lStructSize = sizeof(OPENFILENAMEW);
                        ofn.hwndOwner = hwnd;
                        ofn.lpstrFilter = wzFilter;
                        ofn.nFilterIndex = 0;
                        ofn.lpstrFile = wzOutputFile;
                        ofn.nMaxFile = MAX_FILENAME_LENGTH-1;
                        ofn.Flags = OFN_OVERWRITEPROMPT;
                        ofn.lpstrDefExt = wzDefltExt;
                        _ASSERTE(g_hResources != NULL);
                        if(WszDialogBoxParam(g_hResources,MAKEINTRESOURCE(IDD_DIALOG1),hwnd,(DLGPROC)DumpOptionsProc,0L) &&
                            GetSaveFileName(&ofn))
                        {
                            HCURSOR hWasCursor = SetCursor(hWaitCursor);
                            g_Mode &= ~MODE_GUI;
                            memset(g_szOutputFile,0,MAX_FILENAME_LENGTH);
                            WszWideCharToMultiByte(CP_UTF8,0,wzOutputFile,-1,g_szOutputFile,MAX_FILENAME_LENGTH-1,NULL,NULL);
                            g_pFile = OpenOutput(wzOutputFile);
                            if(g_pFile)
                            {
                                DumpFile(); // closes g_pFile upon completion
                                SetCursor(hWasCursor);
                            }
                            else
                            {
                                SetCursor(hWasCursor);
                                WszMessageBox(hwnd,wzOutputFile,RstrW(IDS_CANNOTOPENFILE),MB_OK|MB_ICONERROR | GetDasmMBRTLStyle());
                            }
                            g_szOutputFile[0] = 0;
                            g_Mode |= MODE_GUI;
                            //g_fShowSource = FALSE; // flag could have been changed for dump
                        }
                    }
                    g_uCodePage = uWasCodePage; // g_uCodePage is changed in DumpOptionsProc
                }
                break;

                case IDM_DUMP_TREE:
                if(g_pImport)
                {
                   // Dump the tree view(fully expanded, with current sorting) to a text file
                    OPENFILENAMEW ofn;
                    WCHAR* wzFilter = RstrW(IDS_FILTER_OUT2); //L"Text file (*.txt) \0*.txt\0Any type (*.*)\0*.*\0\0";
                    const WCHAR* wzDefltExt = L"txt";
                    WCHAR    szIndent[MAX_FILENAME_LENGTH];
                    FILE*   pFile;
                    WCHAR wzOutputFile[MAX_FILENAME_LENGTH];
                    for(WCHAR* pwc = wzFilter; pwc = wcschr(pwc,'\t'); pwc++) *pwc = 0;
                    memset(wzOutputFile,0,sizeof(wzOutputFile));
                    memset(&ofn,0,sizeof(OPENFILENAMEW));
                    ofn.lStructSize = sizeof(OPENFILENAMEW);
                    ofn.hwndOwner = hwnd;
                    ofn.lpstrFilter = wzFilter;
                    ofn.nFilterIndex = 0;
                    ofn.lpstrFile = wzOutputFile;
                    ofn.nMaxFile = MAX_FILENAME_LENGTH-1;
                    ofn.Flags = OFN_OVERWRITEPROMPT;
                    ofn.lpstrDefExt = wzDefltExt;
                    if(GetSaveFileName(&ofn))
                    {
                        HCURSOR hWasCursor = SetCursor(hWaitCursor);
                        pFile = g_pFile;
                        g_pFile = OpenOutput(wzOutputFile);
                        szIndent[0] = 0;
                        if(g_pFile)
                        {
                            g_Mode &= ~MODE_GUI;
                            DumpTreeItem(g_hRoot,g_pFile,szIndent);
                            g_Mode |= MODE_GUI;
                            fclose(g_pFile);
                            SetCursor(hWasCursor);
                        }
                        else
                        {
                            SetCursor(hWasCursor);
                            WszMessageBox(hwnd,wzOutputFile,RstrW(IDS_CANNOTOPENFILE),MB_OK|MB_ICONERROR | GetDasmMBRTLStyle());
                        }
                        g_pFile = pFile;
                    }
                }
                break;

                case IDM_EXIT:
                {
                    WszSendMessage(GetActiveWindow(),WM_CLOSE,0,0);
                }
                break;

                case IDM_FONT_TREE:
                {
                    g_strChFontTree.hwndOwner = g_hwndMain;
                    if(ChooseFont(&g_strChFontTree))
                    {
                        DeleteObject((HGDIOBJ)g_hSmallFont);
                        g_hSmallFont = CreateFontIndirect(&g_strLogFontTree);
                        WszSendMessage(g_hwndTreeView,WM_SETFONT,(LPARAM) g_hSmallFont,TRUE);
                        if(g_hwndAsmInfo)
                            WszSendMessage(g_hwndAsmInfo,WM_SETFONT,(LPARAM) g_hSmallFont,TRUE);
                        SaveGUIFonts(&guiInfo);
                    }
                    break;
                }

                case IDM_FONT_DASM:
                {
                    g_strChFontDasm.hwndOwner = g_hwndMain;
                    if(ChooseFont(&g_strChFontDasm))
                    {
                        if(g_fDumpRTF) { g_strLogFontDasm.lfWeight = FW_REGULAR; g_strLogFontDasm.lfItalic = FALSE; }
                        DeleteObject((HGDIOBJ)g_hFixedFont);
                        g_hFixedFont = CreateFontIndirect(&g_strLogFontDasm);

                        for (DWORD i = 0; i < g_NumDisasmBoxes; i++)
                        {
                            WszSendMessage((*g_DisasmBox)[i].hwndChild,WM_SETFONT,(LPARAM)g_hFixedFont,TRUE);
                            if(g_fDumpRTF)
                                GUIDumpItemToDisassemblyEditBox((void*)(*g_DisasmBox)[i].hwndChild,
                                                            (*g_DisasmBox)[i].tkClass,(*g_DisasmBox)[i].tkMember);
                        }
                        SaveGUIFonts(&guiInfo);
                    }
                    break;
                }

                case IDM_CAVERBAL:
                {
                    CHECK_UNCHECK(g_fCAVerbal);
                    for (DWORD i = 0; i < g_NumDisasmBoxes; i++)
                    {
                        GUIDumpItemToDisassemblyEditBox((void*)(*g_DisasmBox)[i].hwndChild,
                                                        (*g_DisasmBox)[i].tkClass,
                                                        (*g_DisasmBox)[i].tkMember);
                    }
                    break;
                }

                case IDM_DUMPRTF:
                {
                    CHECK_UNCHECK(g_fDumpRTF);
                    //GUIDumpAssemblyInfo();
                    for (DWORD i = 0; i < g_NumDisasmBoxes; i++)
                    {
                        mdToken tkClass = (*g_DisasmBox)[i].tkClass;
                        mdToken tkMember = (*g_DisasmBox)[i].tkMember;
                        if((TypeFromToken(tkMember)==0)&&(tkClass==IDM_SHOW_METAINFO))
                            continue;
                        GUIDisassemble(tkClass,tkMember,"UpdateThisDisassemblyBox");
                    }
                    break;
                }

                case IDM_SORT_BY_NAME:
                {
                    CHECK_UNCHECK(g_fSortByName);
                    if(g_pImport)
                    {
                        if(!RefreshList())  goto CloseAndDestroy;
                    }
                    break;
                }
            
                case IDM_TREEVIEWFCN:
                {
                    CHECK_UNCHECK(g_fTreeViewFCN);
                    if(g_pImport)
                    {
                        if(!RefreshList())  goto CloseAndDestroy;
                    }
                    break;
                }

                case IDM_SHOW_PUB:
                {
                    CHECK_UNCHECK(g_fHidePub);
UpdateVisibilityOptions:
                    g_fLimitedVisibility = g_fHidePub ||
                                           g_fHidePriv ||
                                           g_fHideFam ||
                                           g_fHideFAA ||
                                           g_fHideFOA ||
                                           g_fHidePrivScope ||
                                           g_fHideAsm;
                    if(g_pImport)
                    {
                        if(!RefreshList())  DestroyWindow(hwnd);
                    }
                    break;
                }
                case IDM_SHOW_PRIV:
                {
                    CHECK_UNCHECK(g_fHidePriv);
                    goto UpdateVisibilityOptions;
                }
                case IDM_SHOW_FAM:
                {
                    CHECK_UNCHECK(g_fHideFam);
                    goto UpdateVisibilityOptions;
                }
                case IDM_SHOW_ASM:
                {
                    CHECK_UNCHECK(g_fHideAsm);
                    goto UpdateVisibilityOptions;
                }
                case IDM_SHOW_FAA:
                {
                    CHECK_UNCHECK(g_fHideFAA);
                    goto UpdateVisibilityOptions;
                }
                case IDM_SHOW_FOA:
                {
                    CHECK_UNCHECK(g_fHideFOA);
                    goto UpdateVisibilityOptions;
                }
                case IDM_SHOW_PSCOPE:
                {
                    CHECK_UNCHECK(g_fHidePrivScope);
                    goto UpdateVisibilityOptions;
                }
                case IDM_FULL_INFO:
                {
                    CHECK_UNCHECK(g_fFullMemberInfo);
                    if(g_pImport)
                    {
                        if(!RefreshList())  DestroyWindow(hwnd);
                    }
                    break;
                }
                case IDM_BYTES:
                {
                    CHECK_UNCHECK(g_fShowBytes);
                    break;
                }
                case IDM_TOKENS:
                {
                    CHECK_UNCHECK(g_fDumpTokens);
                    break;
                }
                case IDM_SOURCELINES:
                {
                    CHECK_UNCHECK(g_fShowSource);
                    break;
                }
                case IDM_EXPANDTRY:
                {
                    CHECK_UNCHECK(g_fTryInCode);
                    break;
                }
                case IDM_QUOTEALLNAMES:
                {
                    CHECK_UNCHECK(g_fQuoteAllNames);
                    break;
                }
                case IDM_SHOW_HEADER:
                {
                    GUIDisassemble(IDM_SHOW_HEADER,0,"Headers");
                    break;
                }
                case IDM_SHOW_STAT:
                {
                    GUIDisassemble(IDM_SHOW_STAT,0,"Statistics");
                    break;
                }
                case IDM_HELP:
                {
                    help_hw = hwnd;
                    FindLocalizedFile(L"ildasm.chm", &HelpFileLoader);
                    break;
                }
                case IDM_SHOW_METAINFO:
                {
                    if(g_pImport)
                        GUIDisassemble(IDM_SHOW_METAINFO,0,"MetaInfo");
                    break;
                }
                case IDM_MI_HEADER:
                {
                    WORD iSelection = LOWORD(wParam);
                    if(g_ulMetaInfoFilter & MDInfo::dumpHeader) g_ulMetaInfoFilter &= ~MDInfo::dumpHeader;
                    else g_ulMetaInfoFilter |= MDInfo::dumpHeader;
                    CheckMenuItem(g_hMetaInfoMenu, iSelection, (g_ulMetaInfoFilter & MDInfo::dumpHeader ? MF_CHECKED : MF_UNCHECKED));
                    if(g_ulMetaInfoFilter & MDInfo::dumpHeader)
                    {
                        // HeaderOnly specified,
                        // Suppress Counts,Sizes, Header,Schema and Header,Schema,Rows
                        g_ulMetaInfoFilter &= ~MDInfo::dumpCSV;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_CSV, MF_UNCHECKED);
                        g_ulMetaInfoFilter &= ~MDInfo::dumpSchema;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_SCHEMA, MF_UNCHECKED);
                        g_ulMetaInfoFilter &= ~MDInfo::dumpRaw;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_RAW, MF_UNCHECKED);
                    }
                    return 1; //break;
                }
                case IDM_MI_HEX:
                {
                    WORD iSelection = LOWORD(wParam);
                    if(g_ulMetaInfoFilter & MDInfo::dumpMoreHex) g_ulMetaInfoFilter &= ~MDInfo::dumpMoreHex;
                    else g_ulMetaInfoFilter |= MDInfo::dumpMoreHex;
                    CheckMenuItem(g_hMetaInfoMenu, iSelection, (g_ulMetaInfoFilter & MDInfo::dumpMoreHex ? MF_CHECKED : MF_UNCHECKED));
                    return 1; //break;
                }
                case IDM_MI_CSV:
                {
                    WORD iSelection = LOWORD(wParam);
                    if(g_ulMetaInfoFilter & MDInfo::dumpCSV) g_ulMetaInfoFilter &= ~MDInfo::dumpCSV;
                    else g_ulMetaInfoFilter |= MDInfo::dumpCSV;
                    CheckMenuItem(g_hMetaInfoMenu, iSelection, (g_ulMetaInfoFilter & MDInfo::dumpCSV ? MF_CHECKED : MF_UNCHECKED));
                    if(g_ulMetaInfoFilter & MDInfo::dumpCSV)
                    {
                        // Counts,Sizes specified,
                        // Suppress HeaderOnly, Header,Schema and Header,Schema,Rows
                        g_ulMetaInfoFilter &= ~MDInfo::dumpHeader;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_HEADER, MF_UNCHECKED);
                        g_ulMetaInfoFilter &= ~MDInfo::dumpSchema;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_SCHEMA, MF_UNCHECKED);
                        g_ulMetaInfoFilter &= ~MDInfo::dumpRaw;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_RAW, MF_UNCHECKED);
                    }
                    return 1; //break;
                }
                case IDM_MI_UNREX:
                {
                    WORD iSelection = LOWORD(wParam);
                    if(g_ulMetaInfoFilter & MDInfo::dumpUnsat) g_ulMetaInfoFilter &= ~MDInfo::dumpUnsat;
                    else g_ulMetaInfoFilter |= MDInfo::dumpUnsat;
                    CheckMenuItem(g_hMetaInfoMenu, iSelection, (g_ulMetaInfoFilter & MDInfo::dumpUnsat ? MF_CHECKED : MF_UNCHECKED));
                    return 1; //break;
                }
                case IDM_MI_SCHEMA:
                {
                    WORD iSelection = LOWORD(wParam);
                    if(g_ulMetaInfoFilter & MDInfo::dumpSchema) g_ulMetaInfoFilter &= ~MDInfo::dumpSchema;
                    else g_ulMetaInfoFilter |= MDInfo::dumpSchema;
                    CheckMenuItem(g_hMetaInfoMenu, iSelection, (g_ulMetaInfoFilter & MDInfo::dumpSchema ? MF_CHECKED : MF_UNCHECKED));
                    if(g_ulMetaInfoFilter & MDInfo::dumpSchema)
                    {
                        // Header,Schema specified,
                        // suppress Counts,Sizes, HeaderOnly and Header,Schema,Rows
                        g_ulMetaInfoFilter &= ~MDInfo::dumpCSV;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_CSV, MF_UNCHECKED);
                        g_ulMetaInfoFilter &= ~MDInfo::dumpHeader;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_HEADER, MF_UNCHECKED);
                        g_ulMetaInfoFilter &= ~MDInfo::dumpRaw;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_RAW, MF_UNCHECKED);
                    }
                    return 1; //break;
                }
                case IDM_MI_RAW:
                {
                    WORD iSelection = LOWORD(wParam);
                    if(g_ulMetaInfoFilter & MDInfo::dumpRaw) g_ulMetaInfoFilter &= ~MDInfo::dumpRaw;
                    else g_ulMetaInfoFilter |= MDInfo::dumpRaw;
                    CheckMenuItem(g_hMetaInfoMenu, iSelection, (g_ulMetaInfoFilter & MDInfo::dumpRaw ? MF_CHECKED : MF_UNCHECKED));
                    if(g_ulMetaInfoFilter & MDInfo::dumpRaw)
                    {
                        // Header,Schema,Rows specified,
                        // suppress Counts,Sizes, HeaderOnly and Header,Schema
                        g_ulMetaInfoFilter &= ~MDInfo::dumpCSV;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_CSV, MF_UNCHECKED);
                        g_ulMetaInfoFilter &= ~MDInfo::dumpHeader;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_HEADER, MF_UNCHECKED);
                        g_ulMetaInfoFilter &= ~MDInfo::dumpSchema;
                        CheckMenuItem(g_hMetaInfoMenu, IDM_MI_SCHEMA, MF_UNCHECKED);
                    }
                    return 1; //break;
                }
                case IDM_MI_HEAPS:
                {
                    WORD iSelection = LOWORD(wParam);
                    if(g_ulMetaInfoFilter & MDInfo::dumpRawHeaps) g_ulMetaInfoFilter &= ~MDInfo::dumpRawHeaps;
                    else g_ulMetaInfoFilter |= MDInfo::dumpRawHeaps;
                    CheckMenuItem(g_hMetaInfoMenu, iSelection, (g_ulMetaInfoFilter & MDInfo::dumpRawHeaps ? MF_CHECKED : MF_UNCHECKED));
                    return 1; //break;
                }
                case IDM_MI_VALIDATE:
                {
                    WORD iSelection = LOWORD(wParam);
                    if(g_ulMetaInfoFilter & MDInfo::dumpValidate) g_ulMetaInfoFilter &= ~MDInfo::dumpValidate;
                    else g_ulMetaInfoFilter |= MDInfo::dumpValidate;
                    CheckMenuItem(g_hMetaInfoMenu, iSelection, (g_ulMetaInfoFilter & MDInfo::dumpValidate ? MF_CHECKED : MF_UNCHECKED));
                    return 1; //break;
                }
                
            }

            break;
        }

        case WM_SETFOCUS:
            SetFocus(g_hwndTreeView);
            break;

        case WM_SIZE:
        {
            DWORD cxClient = LOWORD(lParam);
            DWORD cyClient = HIWORD(lParam);
            DWORD dy;

            dy = cyClient >> 3;
            if(dy < 50) dy = 50;
            if(cyClient < dy+4) cyClient = dy+4;

            // Resize listview window
            MoveWindow(
                g_hwndTreeView,
                0,
                0,
                cxClient,
                cyClient-dy-2,
                TRUE // repaint
            );
            // Resize AsmInfo window
            MoveWindow(
                g_hwndAsmInfo,
                0,
                cyClient-dy-1,
                cxClient,
                dy,
                TRUE // repaint
            );

            break;
        }

        case WM_NOTIFY:
        {
            if (wParam == ID_TREEVIEW)
            {
                NMHDR *  pNMHDR = (NMHDR*) lParam;
                switch (pNMHDR->code)
                {
                    case TVN_KEYDOWN:
                    {
                        NMTVKEYDOWN *pKeyDown = (NMTVKEYDOWN *) pNMHDR;

                        if (pKeyDown->wVKey == '\r')
                        {
                            if(DoubleClickSelectedMember(g_CurSelItem) == NULL)
                                TreeView_Expand(g_hwndTreeView,g_CurSelItem,TVE_TOGGLE);
                        }
                        break;
                    }

                    case NM_DBLCLK:
                    {
                        hwndDasm = DoubleClickSelectedMember(g_CurSelItem);
                        if(hwndDasm)
                        {
                            PostMessageA(hwndDasm,WM_ACTIVATE,WA_CLICKACTIVE,0);
                            PostMessageA(hwndDasm,WM_SETFOCUS,0,0);
                        }
                        break;
                    }

                    case TVN_SELCHANGEDW:
                    case TVN_SELCHANGEDA:
                    {
                        NMTREEVIEW *pTV = (NMTREEVIEW *) pNMHDR;
                        /*
                        TVITEM      SelItem;
                        char        szText[256];

                        memset(&SelItem, 0, sizeof(SelItem));
                        SelItem.mask = TVIF_TEXT;
                        SelItem.pszText = szText;
                        SelItem.cchTextMax = sizeof(szText)-1;
                        SelItem.hItem = pTV->itemNew.hItem;

                        g_CurSelItem = SelItem.hItem;
                        SendMessageA(g_hwndTreeView, TVM_GETITEM, 0, (LPARAM)&SelItem);
                        */
                        g_CurSelItem = pTV->itemNew.hItem;
                        break;
                    }
                }
            }

            break;
        }

        case WM_CLOSE:
    CloseAndDestroy:
            // HTML help window is closed automatically
            {
                RECT r;
                ShowWindow(hwnd,SW_RESTORE);
                GetWindowRect(hwnd,(LPRECT)&r);
                guiInfo.x = r.left;
                guiInfo.y = r.top;
                guiInfo.w = r.right - r.left;
                guiInfo.h = r.bottom - r.top;
                SaveGUIFonts(&guiInfo);
            }
            DestroyWindow(hwnd);        // Generates the WM_DESTROY message
            break;

        case WM_DESTROY :
            PostQuitMessage(0);         // Puts a WM_QUIT in the queue
            break;

        default :
            return DefWindowProcW(hwnd, uMsg, wParam, lParam);
    }

    return 0;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif


//
// Create the treeview in the main window
//
HWND CreateTreeView(HWND hwndParent)
{
    HWND        hwndTree;
    RECT        rcl;
    DWORD       tvs =
                        TVS_HASLINES
                        |TVS_HASBUTTONS
                        |TVS_LINESATROOT
                        |TVS_SHOWSELALWAYS
                        // |TVS_TRACKSELECT
                        // |TVS_SINGLEEXPAND
                        |TVS_DISABLEDRAGDROP
                        ;
    unsigned    cy,dy;

    GetClientRect(hwndParent, &rcl);
    cy = rcl.bottom - rcl.top;
    dy = cy >> 3;
    hwndTree = WszCreateWindowEx(
        0,
        WC_TREEVIEWW,
        NULL,
        WS_VISIBLE|WS_CHILD|WS_BORDER|tvs,
        0,
        0,
        rcl.right - rcl.left,
        cy-dy-2,    //rcl.bottom - rcl.top,
        hwndParent,
        (HMENU) ID_TREEVIEW,
        g_hInstance,
        NULL
    );
    g_hwndAsmInfo = NULL;
    if (hwndTree == NULL)
        return NULL;

    WszSendMessage(hwndTree,WM_SETFONT,(LPARAM) g_hSmallFont,FALSE);

    TreeView_SetBkColor(hwndTree,-1);
    TreeView_SetImageList(hwndTree, g_hImageList, TVSIL_NORMAL);

    g_hwndAsmInfo = WszCreateWindowEx(
        0, //WS_EX_TOOLWINDOW,
        g_fDumpRTF ? L"RichEdit20A" : L"EDIT",
        NULL,
        WS_CHILD | WS_VSCROLL | WS_HSCROLL | WS_VISIBLE | WS_BORDER //| WS_CAPTION | WS_OVERLAPPEDWINDOW
        | ES_MULTILINE | ES_READONLY | ES_AUTOHSCROLL | ES_AUTOVSCROLL | ES_NOHIDESEL,
        0,
        cy-dy-1,
        rcl.right - rcl.left,
        dy,
        hwndParent,
        (HMENU) ID_LISTBOX,
        g_hInstance, // hinst
        NULL
    );
    if(g_hwndAsmInfo)
    {
        WszSendMessage(g_hwndAsmInfo,WM_SETFONT,(LPARAM) g_hSmallFont,FALSE);
    }

    return hwndTree;
}


//
// Add one item to a treeview
//
HTREEITEM AddOneItem(HTREEITEM hParent, const char *pszText, HTREEITEM hInsAfter, int iImage, HWND hwndTree, BOOL fExpanded)
{
    HTREEITEM       hItem;
    WCHAR* wz = UtfToUnicode(pszText);
    ULONG  lLen = (ULONG)wcslen(wz);
    TVINSERTSTRUCTW tvIns;
    memset(&tvIns, 0, sizeof(tvIns));

    tvIns.item.mask            = TVIF_TEXT|TVIF_IMAGE|TVIF_SELECTEDIMAGE|TVIF_PARAM;
    tvIns.item.pszText         = wz;
    tvIns.item.cchTextMax      = lLen;
    tvIns.item.iImage          = iImage;
    tvIns.item.iSelectedImage  = iImage;

    tvIns.hInsertAfter  = hInsAfter;
    tvIns.hParent       = hParent;

    hItem = (HTREEITEM)WszSendMessage(hwndTree, TVM_INSERTITEMW, 0, (LPARAM)(&tvIns));

    return hItem;
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22018) // Suppress PREFast warning about Integer overflow/underflow
#endif
//ulen cannot be greater than GlobalBufferLen by the definition of ulen. Therefore it's safe to disable this warning here.
void AddMethodToGUI(
    mdTypeDef   cl,
    ClassItem_t * pClassItem,
    const char *pszNamespace,
    const char *pszClassName,
    const char *pszMethodName,
    PCCOR_SIGNATURE pComSig,
    unsigned    cComSig,
    mdMethodDef mbMethod,
    DWORD       dwAttrs
)
{
    HTREEITEM       hParent;
    char* szName;
    ULONG   ulLen,ulImageIndex;
    BOOL    wasDumpRTF;

    memset(GlobalBuffer,0,GlobalBufferLen);
    sprintf_s(GlobalBuffer, GlobalBufferLen,g_fFullMemberInfo ? "method %s : ": "%s : ",pszMethodName);
    InGlobalBuffer = (UINT32)strlen(GlobalBuffer);
    ulLen = InGlobalBuffer;
    wasDumpRTF = g_fDumpRTF;
    g_fDumpRTF = FALSE;
    mdToken tkVarOwner = g_tkVarOwner;
    g_tkVarOwner = cl;
    DumpMethod(mbMethod, pszClassName,VAL32(g_CORHeader->EntryPointToken),(void *)g_hwndTreeView,FALSE);
    g_tkVarOwner = tkVarOwner;
    g_fDumpRTF = wasDumpRTF;
    GlobalBuffer[InGlobalBuffer-2] = 0; // get rid of \r\n

    szName = &GlobalBuffer[ulLen];

    if(strstr(szName,"instance ") == szName) strcpy_s(szName,GlobalBufferLen-ulLen,szName+9);

    szName = GlobalBuffer;

    hParent = pClassItem->hItem;

    _ASSERTE(pClassItem->CurMember < pClassItem->SubItems);

    if((strchr(szName, '<'))&&(strchr(szName, '>')))
    {
        ulImageIndex = IsMdStatic(dwAttrs) ? STATIC_METHOD_GEN_IMAGE_INDEX : METHOD_GEN_IMAGE_INDEX;
    }
    else
    {
        ulImageIndex = IsMdStatic(dwAttrs) ? STATIC_METHOD_IMAGE_INDEX : METHOD_IMAGE_INDEX;
    }

    pClassItem->pMembers[pClassItem->CurMember].hItem = AddOneItem(
        hParent, szName, TVI_LAST, ulImageIndex, g_hwndTreeView, FALSE
    );
    pClassItem->pMembers[pClassItem->CurMember].Discriminator = TREEITEM_TYPE_MEMBER;
    pClassItem->pMembers[pClassItem->CurMember].mbMember = mbMethod;
    pClassItem->CurMember++;
}

#ifdef _PREFAST_
#pragma warning(pop)
#endif
BOOL NamespaceMatch(const char *pszNamespace, __in __nullterminated char *pszString)
{
    if (strncmp(pszNamespace, pszString, strlen(pszNamespace)) == 0)
    {
        if (pszString[ strlen(pszNamespace) ] == NAMESPACE_SEPARATOR_CHAR)
            return TRUE;
    }

    return FALSE;
}

void AddFieldToGUI(
    mdTypeDef   cl,
    ClassItem_t *pClassItem,
    const char  *pszNamespace,
    const char  *pszClassName,
    const char  *pszFieldName,
    const char  *pszSignature,
    mdFieldDef  mbField,
    DWORD       dwAttrs
)
{
    DWORD   Dimensions;
    ULONG   ul1,ul2;
    BOOL    wasDumpRTF;
    HTREEITEM hParent = pClassItem->hItem;

    Dimensions = 0;

    memset(GlobalBuffer,0,GlobalBufferLen);
    sprintf_s(GlobalBuffer,GlobalBufferLen,g_fFullMemberInfo ? "field %s : " : "%s : ",pszFieldName);
    InGlobalBuffer = (UINT32)strlen(GlobalBuffer);
    GetClassLayout(cl,&ul1,&ul2);
    wasDumpRTF = g_fDumpRTF;
    g_fDumpRTF = FALSE;
    DumpField(mbField, pszClassName,(void *)g_hwndTreeView,FALSE);
    g_fDumpRTF = wasDumpRTF;
    GlobalBuffer[InGlobalBuffer-2] = 0; // get rid of \r\n
    char* pch = strchr(GlobalBuffer,'\r');
    if(pch) strcpy_s(pch,5," ...");
    _ASSERTE(pClassItem->CurMember < pClassItem->SubItems);

    pClassItem->pMembers[pClassItem->CurMember].mbMember = mbField;
    pClassItem->pMembers[pClassItem->CurMember].Discriminator = TREEITEM_TYPE_MEMBER;
    pClassItem->pMembers[pClassItem->CurMember++].hItem = AddOneItem(
        hParent,
        GlobalBuffer, //szType,
        TVI_LAST,
        (dwAttrs & mdStatic) ? STATIC_FIELD_IMAGE_INDEX : FIELD_IMAGE_INDEX,
        g_hwndTreeView,
        FALSE
    );
}

void AddEventToGUI(
    mdTypeDef   cl,
    ClassItem_t *pClassItem,
    const char  *pszNamespace,
    const char  *pszClassName,
    DWORD       dwClassAttrs,
    mdEvent     mbEvent
)
{
    DWORD   Dimensions;
    BOOL    wasDumpRTF;
    HTREEITEM hParent = pClassItem->hItem;

    Dimensions = 0;

    memset(GlobalBuffer,0,GlobalBufferLen);
    if(g_fFullMemberInfo) strcpy_s(GlobalBuffer,GlobalBufferLen,"event ");
    InGlobalBuffer = (UINT32)strlen(GlobalBuffer);
    wasDumpRTF = g_fDumpRTF;
    g_fDumpRTF = FALSE;
    DumpEvent(mbEvent, pszClassName, dwClassAttrs, (void *)g_hwndTreeView, FALSE); //FALSE=don't dump the body
    g_fDumpRTF = wasDumpRTF;
    GlobalBuffer[InGlobalBuffer-2] = 0; // get rid of \r\n

    _ASSERTE(pClassItem->CurMember < pClassItem->SubItems);

    pClassItem->pMembers[pClassItem->CurMember].mbMember = mbEvent;
    pClassItem->pMembers[pClassItem->CurMember].Discriminator = TREEITEM_TYPE_MEMBER;
    pClassItem->pMembers[pClassItem->CurMember++].hItem = AddOneItem(
        hParent,
        GlobalBuffer, //szType,
        TVI_LAST,
        EVENT_IMAGE_INDEX,
        g_hwndTreeView,
        FALSE
    );
}

void AddPropToGUI(
    mdTypeDef   cl,
    ClassItem_t *pClassItem,
    const char  *pszNamespace,
    const char  *pszClassName,
    DWORD       dwClassAttrs,
    mdProperty  mbProp
)
{
    DWORD   Dimensions;
    BOOL    wasDumpRTF;
    HTREEITEM hParent = pClassItem->hItem;

    Dimensions = 0;

    memset(GlobalBuffer,0,GlobalBufferLen);
    if(g_fFullMemberInfo) strcpy_s(GlobalBuffer,GlobalBufferLen,"prop ");
    InGlobalBuffer = (UINT32)strlen(GlobalBuffer);
    wasDumpRTF = g_fDumpRTF;
    g_fDumpRTF = FALSE;
    DumpProp(mbProp, pszClassName, dwClassAttrs, (void *)g_hwndTreeView, FALSE); //FALSE=don't dump the body
    g_fDumpRTF = wasDumpRTF;
    GlobalBuffer[InGlobalBuffer-2] = 0; // get rid of \r\n

    _ASSERTE(pClassItem->CurMember < pClassItem->SubItems);

    pClassItem->pMembers[pClassItem->CurMember].mbMember = mbProp;
    pClassItem->pMembers[pClassItem->CurMember].Discriminator = TREEITEM_TYPE_MEMBER;
    pClassItem->pMembers[pClassItem->CurMember++].hItem = AddOneItem(
        hParent,
        GlobalBuffer, //szType,
        TVI_LAST,
        PROP_IMAGE_INDEX,
        g_hwndTreeView,
        FALSE
    );
}



HTREEITEM FindCreateNamespaceRoot(const char *pszNamespace)
{
    DWORD       i;
    HTREEITEM   hRoot;
    DWORD l = 0,ll;

    if (!pszNamespace || !*pszNamespace)
        return g_hRoot; // not in a namespace, use tree root

    hRoot = g_hRoot;
    for (i = 0; i < g_NumNamespaces; i++)
    {
        if (!strcmp(pszNamespace, (*g_NamespaceList)[i].pszNamespace))
            return (*g_NamespaceList)[i].hRoot;
    }
    for (i = 0; i < g_NumNamespaces; i++)
    {
        if(strstr(pszNamespace,(*g_NamespaceList)[i].pszNamespace) == pszNamespace)
        {
            ll = (DWORD)strlen((*g_NamespaceList)[i].pszNamespace);
            if((ll > l)&&(pszNamespace[ll] == '.'))
            {
                hRoot = (*g_NamespaceList)[i].hRoot;
                l = ll;
            }
        }
    }

    hRoot = AddOneItem(hRoot, pszNamespace, TVI_LAST, NAMESPACE_IMAGE_INDEX, g_hwndTreeView, TRUE);
    (*g_NamespaceList)[g_NumNamespaces].pszNamespace = pszNamespace;
    (*g_NamespaceList)[g_NumNamespaces].hRoot = hRoot;
    g_NumNamespaces++;

    return hRoot;
}


Namespace_t *FindNamespace(const char *pszNamespace)
{
    DWORD i;

    for (i = 0; i < g_NumNamespaces; i++)
    {
        if (!strcmp(pszNamespace, (*g_NamespaceList)[i].pszNamespace))
            return &(*g_NamespaceList)[i];
    }

    return NULL;
}


void GUICleanupClassItems()
{
    DWORD i;
    WCHAR* sz=L"\0\0";

    for (i = 0; i < g_NumClassItems; i++)
    {
        if((*g_ClassItemList)[i].pMembers)
        {
            delete[] (*g_ClassItemList)[i].pMembers;
            (*g_ClassItemList)[i].pMembers = NULL;
        }
    }
    for (i = 0; i < g_NumDisasmBoxes; i++)
    {
        PostMessageA((*g_DisasmBox)[i].hwndContainer,WM_CLOSE,0,0);
    }
    WszSendMessage(g_hwndAsmInfo,WM_SETTEXT,0,(LPARAM)sz);
    EnableMenuItem(g_hMenu,(UINT)(UINT_PTR)g_hViewMenu, MF_GRAYED);
    EnableMenuItem(g_hFileMenu,IDM_DUMP,MF_GRAYED);
    EnableMenuItem(g_hFileMenu,IDM_DUMP_TREE,MF_GRAYED);
}

//
// Add a new class tree node
//
ClassItem_t *AddClassToGUI(
    mdTypeDef   cl,
    UINT        uImageIndex,
    const char  *pszNamespace,
    const char  *pszClassName,
    DWORD       cSubItems,
    HTREEITEM   *phRoot  // Returns the namespace root (NOT the class root)
)
{
    HTREEITEM   hRoot;

    if(*phRoot)
        hRoot = *phRoot;
    else
    {
        hRoot = FindCreateNamespaceRoot(pszNamespace);
        _ASSERTE(hRoot != NULL);

        *phRoot = hRoot;
    }

    (*g_ClassItemList)[g_NumClassItems].hItem = AddOneItem(hRoot, pszClassName, TVI_LAST, uImageIndex, g_hwndTreeView, FALSE);
    (*g_ClassItemList)[g_NumClassItems].cl = cl;
    (*g_ClassItemList)[g_NumClassItems].SubItems = cSubItems;
    (*g_ClassItemList)[g_NumClassItems].CurMember = 0;

    (*g_ClassItemList)[g_NumClassItems].pMembers = new (nothrow) TreeItem_t[cSubItems];

    g_NumClassItems++;

    return &(*g_ClassItemList)[g_NumClassItems-1];
}


void AddGlobalFunctions()
{
    HRESULT         hr = S_OK;
    HENUMInternal   hEnumMethod;
    mdToken         FuncToken;
    DWORD           i;
    HTREEITEM       hNamespaceRoot = NULL;
    DWORD           NumGlobals;

    ClassItem_t* pClassItem =  &(*g_ClassItemList)[0];

    if (SUCCEEDED(g_pImport->EnumGlobalFieldsInit(&hEnumMethod)))
    {
        NumGlobals = g_pImport->EnumGetCount(&hEnumMethod);
        MemberInfo* fields = new (nothrow) MemberInfo[NumGlobals];
        MemberInfo* curField = fields;

        for (i = 0; g_pImport->EnumNext(&hEnumMethod, &FuncToken); i++)
        {
            curField->token = FuncToken;
            if (FAILED(g_pImport->GetFieldDefProps(FuncToken, &curField->dwAttrs)) || 
                FAILED(g_pImport->GetNameOfFieldDef(FuncToken, &curField->pszMemberName)))
            {
                curField->pszMemberName = "Invalid FieldDef record";
            }
            MAKE_NAME_IF_NONE(curField->pszMemberName,FuncToken);
            //curField->pComSig = g_pImport->GetSigOfFieldDef(FuncToken, &curMethod->cComSig);
            curField++;
        }
        g_pImport->EnumClose(&hEnumMethod);

        _ASSERTE(curField - fields == (int) NumGlobals);

        if(g_fSortByName) qsort(fields, NumGlobals, sizeof MemberInfo, memberCmp);

        for(curField = fields; curField < &fields[NumGlobals];curField++)
        {
            if(g_fLimitedVisibility)
            {
                if(g_fHidePub && IsFdPublic(curField->dwAttrs)) continue;
                if(g_fHidePriv && IsFdPrivate(curField->dwAttrs)) continue;
                if(g_fHideFam && IsFdFamily(curField->dwAttrs)) continue;
                if(g_fHideAsm && IsFdAssembly(curField->dwAttrs)) continue;
                if(g_fHideFOA && IsFdFamORAssem(curField->dwAttrs)) continue;
                if(g_fHideFAA && IsFdFamANDAssem(curField->dwAttrs)) continue;
                if(g_fHidePrivScope && IsFdPrivateScope(curField->dwAttrs)) continue;
            }
            AddFieldToGUI(NULL, pClassItem, NULL, "Global Fields", curField->pszMemberName, NULL, curField->token, curField->dwAttrs);
        }
        delete[] fields;
    }
    if (FAILED(g_pImport->EnumGlobalFunctionsInit(&hEnumMethod)))
        return;

    NumGlobals = g_pImport->EnumGetCount(&hEnumMethod);
    MemberInfo* methods = new (nothrow) MemberInfo[NumGlobals];
    MemberInfo* curMethod = methods;

    for (i = 0; g_pImport->EnumNext(&hEnumMethod, &FuncToken); i++)
    {
        curMethod->token = FuncToken;
        if (FAILED(g_pImport->GetMethodDefProps(FuncToken, &curMethod->dwAttrs)) || 
            FAILED(g_pImport->GetNameOfMethodDef(FuncToken, &curMethod->pszMemberName)))
        {
            curMethod->pszMemberName = "Invalid MethodDef record";
        }
        MAKE_NAME_IF_NONE(curMethod->pszMemberName,FuncToken);
        if (FAILED(g_pImport->GetSigOfMethodDef(FuncToken, &curMethod->cComSig, &curMethod->pComSig)))
        {
            curMethod->pszMemberName = "Invalid MethodDef record";
            curMethod->cComSig = 0;
            curMethod->pComSig = NULL;
        }
        curMethod++;
    }
    g_pImport->EnumClose(&hEnumMethod);

    _ASSERTE(curMethod - methods == (int) NumGlobals);

    if(g_fSortByName) qsort(methods, NumGlobals, sizeof MemberInfo, memberCmp);

    for(curMethod = methods; curMethod < &methods[NumGlobals];curMethod++)
    {
        if(g_fLimitedVisibility)
        {
            if(g_fHidePub && IsMdPublic(curMethod->dwAttrs)) continue;
            if(g_fHidePriv && IsMdPrivate(curMethod->dwAttrs)) continue;
            if(g_fHideFam && IsMdFamily(curMethod->dwAttrs)) continue;
            if(g_fHideAsm && IsMdAssem(curMethod->dwAttrs)) continue;
            if(g_fHideFOA && IsMdFamORAssem(curMethod->dwAttrs)) continue;
            if(g_fHideFAA && IsMdFamANDAssem(curMethod->dwAttrs)) continue;
            if(g_fHidePrivScope && IsMdPrivateScope(curMethod->dwAttrs)) continue;
        }
        AddMethodToGUI(NULL, pClassItem, NULL, "Global Functions", curMethod->pszMemberName, curMethod->pComSig, curMethod->cComSig, curMethod->token, curMethod->dwAttrs);
    }
    delete[] methods;
    return;
}


BOOL CreateMainWindow()
{
    DWORD dwStyle, dwStyleEx;

// If only showing GUI's IL window, than we don't want to see the main window
// However, main window still manages our data, so we have to still create it. :(
// But we can "pretend" it's not there by hiding it (no WS_VISIBLE, and add WS_EX_TOOLWINDOW)
    if (IsGuiILOnly()) {
        dwStyle = WS_OVERLAPPEDWINDOW | WS_CAPTION | WS_POPUP | WS_SIZEBOX;
        dwStyleEx = WS_EX_TOOLWINDOW;
    } else {
        dwStyle = WS_OVERLAPPEDWINDOW | WS_VISIBLE | WS_CAPTION | WS_POPUP | WS_SIZEBOX;
        dwStyleEx = WS_EX_CLIENTEDGE;
    }
    g_hwndMain = WszCreateWindowEx(dwStyleEx,
        MAIN_WINDOW_CLASSW,
        L"IL DASM ", //MAIN_WINDOW_CAPTIONW,
        dwStyle,
        guiInfo.x,
        guiInfo.y,
        guiInfo.w,
        guiInfo.h,
        NULL,
        g_hMenu, // menu
        g_hInstance, // hinst
        NULL
    );
    if (g_hwndMain == NULL)
        return FALSE;
    DragAcceptFiles(g_hwndMain,TRUE);
    SendMessageA(g_hwndMain,WM_SETTEXT, 0, (LPARAM)"IL DASM   ");
    return TRUE;
}


//
// Given a CL token, find the classitem for it
//
ClassItem_t *FindClassItem(mdTypeDef cl)
{
    DWORD i;

    for (i = 0; i < g_NumClassItems; i++)
    {
        if ((*g_ClassItemList)[i].cl == cl)
            return &(*g_ClassItemList)[i];
    }

    return NULL;
}


//
// Given a class name, find the classitem for it (may fail)
//
ClassItem_t *FindClassItem(__in_opt __nullterminated char *pszFindNamespace, __in __nullterminated char *pszFindName)
{
    DWORD i;

    for (i = 0; i < g_NumClassItems; i++)
    {
        const char *pszClassName;
        const char *pszNamespace;

        if((*g_ClassItemList)[i].cl)
        {

            if (FAILED(g_pImport->GetNameOfTypeDef(
                (*g_ClassItemList)[i].cl, 
                &pszClassName, 
                &pszNamespace)))
            {
                pszClassName = pszNamespace = "Invalid TypeDef record";
            }
            MAKE_NAME_IF_NONE(pszClassName,(*g_ClassItemList)[i].cl);

            if (!strcmp(pszFindName, pszClassName))
            {
                if ((((pszFindNamespace == NULL)||(*pszFindNamespace == 0))
                    &&((pszNamespace == NULL)||(*pszNamespace == 0)))
                    ||(!strcmp(pszFindNamespace, pszNamespace)))
                    return &(*g_ClassItemList)[i];
            }
        }
    }
    //MessageBox(NULL,pszFindName,"Class Not Found",MB_OK);
    return NULL;
}


ClassItem_t *FindClassItem(HTREEITEM hItem)
{
    DWORD i;

    for (i = 0; i < g_NumClassItems; i++)
    {
        if ((*g_ClassItemList)[i].hItem == hItem)
            return &(*g_ClassItemList)[i];
    }

    return NULL;
}


//
// Init GUI components
//
BOOL CreateGUI()
{

    if (InitGUI() == FALSE)
        return FALSE;

    // Register the window class for the main window.
    if (CreateMainWindow() == FALSE)
        return FALSE;

    g_hwndTreeView = CreateTreeView(g_hwndMain);
    if (g_hwndTreeView == NULL)
        return FALSE;

    return 0;
}


//
// This is the main loop which the disassembler sits in when in GUI mode
//
void GUIMainLoop()
{
    MSG msg;
    HACCEL  hAccel = NULL;

    _ASSERTE(g_hResources != NULL);
    hAccel = WszLoadAccelerators(g_hResources,L"FileAccel");
    // Accelerator tables are released when the app exits
    while (WszGetMessage(&msg, (HWND) NULL, 0, 0))
    {
        // Dispatch message to appropriate window
        if((g_hFindText == NULL)|| !WszIsDialogMessage(g_hFindText,&msg))
        {
            if(hAccel && WszTranslateAccelerator(g_hwndMain,hAccel,&msg));
            else
            {
                TranslateMessage(&msg);
                WszDispatchMessage(&msg);
            }
        }
    }
    GUICleanupClassItems();
}
// Dump one tree item to a text file (calls itself recursively)
void DumpTreeItem(HTREEITEM hSelf, FILE* pFile, __inout __nullterminated WCHAR* szIndent)
{
    HTREEITEM   hNext;
    TVITEMEXW    tvi;
    static WCHAR       szText[2048];
    WCHAR* wzString = (WCHAR*)GlobalBuffer;
    tvi.mask = TVIF_HANDLE | TVIF_IMAGE | TVIF_TEXT;
    tvi.hItem = hSelf;
    tvi.pszText = szText;
    tvi.cchTextMax = 2047;
    if(WszSendMessage(g_hwndTreeView,TVM_GETITEMW,0,(LPARAM)(&tvi)))
    {
        WCHAR* szType = NULL;
        if(hSelf == g_hRoot) szType = L"MOD";
        else
        {
            switch(tvi.iImage)
            {
                case CLASS_IMAGE_INDEX:         szType = L"CLS"; break;
                case EVENT_IMAGE_INDEX:         szType = L"EVT"; break;
                case FIELD_IMAGE_INDEX:         szType = L"FLD"; break;
                case NAMESPACE_IMAGE_INDEX:     szType = L"NSP"; break;
                case METHOD_IMAGE_INDEX:        szType = L"MET"; break;
                case PROP_IMAGE_INDEX:          szType = L"PTY"; break;
                case STATIC_FIELD_IMAGE_INDEX:  szType = L"STF"; break;
                case STATIC_METHOD_IMAGE_INDEX: szType = L"STM"; break;
                case CLASSENUM_IMAGE_INDEX:     szType = L"ENU"; break;
                case CLASSINT_IMAGE_INDEX:      szType = L"INT"; break;
                case CLASSVAL_IMAGE_INDEX:      szType = L"VCL"; break;
            }
        }
        if(szType) swprintf_s(wzString,4096,L"%s___[%s] %s",szIndent,szType,szText);
        else       swprintf_s(wzString,4096,L"%s     %s",szIndent,szText);
    }
    else swprintf_s(wzString,4096,L"%sGetItemW failed",szIndent);
    printLineW(pFile,wzString);
    *wzString = 0;
    if(hNext = TreeView_GetChild(g_hwndTreeView,hSelf))
    {
        wcscat_s(szIndent,MAX_FILENAME_LENGTH,L"   |");
        
        do {
            DumpTreeItem(hNext,pFile,szIndent);
        } while(hNext = TreeView_GetNextSibling(g_hwndTreeView,hNext));
        
        szIndent[wcslen(szIndent)-4] = 0;
        printLineW(pFile,szIndent);
    }
}
#endif
