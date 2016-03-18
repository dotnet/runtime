// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "WatchCmd.h"

#ifndef IfFailRet
#define IfFailRet(EXPR) do { Status = (EXPR); if(FAILED(Status)) { return (Status); } } while (0)
#endif

_PersistList::~_PersistList()
{
    PersistWatchExpression* pCur = pHeadExpr;
    while(pCur != NULL)
    {
        PersistWatchExpression* toDelete = pCur;
        pCur = pCur->pNext;
        delete toDelete;
    }
}

WatchCmd::WatchCmd() :
pExpressionListHead(NULL)
{ }
WatchCmd::~WatchCmd()
{
    Clear();
    PersistList* pCur = pPersistListHead;
    while(pCur != NULL)
    {
        PersistList* toDelete = pCur;
        pCur = pCur->pNext;
        delete toDelete;
    }
}

// Deletes all current watch expressions from the watch list
// (does not delete persisted watch lists though)
HRESULT WatchCmd::Clear()
{
    WatchExpression* pCurrent = pExpressionListHead;
    while(pCurrent != NULL)
    {
        WatchExpression* toDelete = pCurrent;
        pCurrent = pCurrent->pNext;
        delete toDelete;
    }
    pExpressionListHead = NULL;
    return S_OK;
}

// Adds a new expression to the active watch list
HRESULT WatchCmd::Add(__in_z WCHAR* pExpression)
{
    WatchExpression* pExpr = new WatchExpression;
    if(pExpr == NULL)
        return E_OUTOFMEMORY;
    wcsncpy_s(pExpr->pExpression, MAX_EXPRESSION, pExpression, _TRUNCATE);
    pExpr->pNext = NULL;

    WatchExpression** ppCurrent = &pExpressionListHead;
    while(*ppCurrent != NULL)
        ppCurrent = &((*ppCurrent)->pNext);
    *ppCurrent = pExpr;
    return S_OK;
}

// removes an expression at the given index in the active watch list
HRESULT WatchCmd::Remove(int index)
{
    HRESULT Status = S_FALSE;
    WatchExpression** ppCurrent = &pExpressionListHead;
    for(int i=1; *ppCurrent != NULL; i++)
    {
        if(i == index)
        {
            WatchExpression* toDelete = *ppCurrent;
            *ppCurrent = (*ppCurrent)->pNext;
            delete toDelete;
            Status = S_OK;
            break;
        }
        ppCurrent = &((*ppCurrent)->pNext);

    }
    return Status;
}

// Evaluates and prints a tree version of the active watch list
// The tree will be expanded along the nodes in expansionPath
// Optionally the list is filtered to only show differences from pFilterName (the name of a persisted watch list)
HRESULT WatchCmd::Print(int expansionIndex, __in_z WCHAR* expansionPath, __in_z WCHAR* pFilterName)
{
    HRESULT Status = S_OK;
    INIT_API_EE();
    INIT_API_DAC();
    EnableDMLHolder dmlHolder(TRUE);
    IfFailRet(InitCorDebugInterface());

    PersistList* pFilterList = NULL;
    if(pFilterName != NULL)
    {
        pFilterList = pPersistListHead;
        while(pFilterList != NULL)
        {
            if(_wcscmp(pFilterList->pName, pFilterName)==0)
                break;
            pFilterList = pFilterList->pNext;
        }
    }

    PersistWatchExpression* pHeadFilterExpr = (pFilterList != NULL) ? pFilterList->pHeadExpr : NULL;

    WatchExpression* pExpression = pExpressionListHead;
    int index = 1;
    while(pExpression != NULL)
    {
        ExpressionNode* pResult = NULL;
        if(FAILED(Status = ExpressionNode::CreateExpressionNode(pExpression->pExpression, &pResult)))
        {
            ExtOut("  %d) Error: HRESULT 0x%x while evaluating expression \'%S\'", index, Status, pExpression->pExpression);
        }
        else
        {
            //check for matching absolute expression
            PersistWatchExpression* pCurFilterExpr = pHeadFilterExpr;
            while(pCurFilterExpr != NULL)
            {
                if(_wcscmp(pCurFilterExpr->pExpression, pResult->GetAbsoluteExpression())==0)
                    break;
                pCurFilterExpr = pCurFilterExpr->pNext;
            }

            // check for matching persist evaluation on the matching expression
            BOOL print = TRUE;
            if(pCurFilterExpr != NULL)
            {
                WCHAR pCurPersistResult[MAX_EXPRESSION];
                FormatPersistResult(pCurPersistResult, MAX_EXPRESSION, pResult);
                if(_wcscmp(pCurPersistResult, pCurFilterExpr->pPersistResult)==0)
                {
                    print = FALSE;
                }
            }

            //expand and print
            if(print)
            {
                if(index == expansionIndex)
                    pResult->Expand(expansionPath);
                PrintCallbackData data;
                data.index = index;
                WCHAR pCommand[MAX_EXPRESSION];
                swprintf_s(pCommand, MAX_EXPRESSION, L"!watch -expand %d", index);
                data.pCommand = pCommand;
                pResult->DFSVisit(EvalPrintCallback, (VOID*)&data);
            }
            delete pResult;
        }
        pExpression = pExpression->pNext;
        index++;
    }
    return Status;
}

// Deletes an persisted watch list by name
HRESULT WatchCmd::RemoveList(__in_z WCHAR* pListName)
{
    PersistList** ppList = &pPersistListHead;
    while(*ppList != NULL)
    {
        if(_wcscmp((*ppList)->pName, pListName) == 0)
        {
            PersistList* toDelete = *ppList;
            *ppList = (*ppList)->pNext;
            delete toDelete;
            return S_OK;
        }
        ppList = &((*ppList)->pNext);
    }
    return S_FALSE;
}

// Renames a previously saved persisted watch list
HRESULT WatchCmd::RenameList(__in_z WCHAR* pOldName, __in_z WCHAR* pNewName)
{
    if(_wcscmp(pOldName, pNewName)==0)
        return S_OK;
    PersistList** ppList = &pPersistListHead;
    while(*ppList != NULL)
    {
        if(_wcscmp((*ppList)->pName, pOldName) == 0)
        {
            PersistList* pListToChangeName = *ppList;
            RemoveList(pNewName);
            wcsncpy_s(pListToChangeName->pName, MAX_EXPRESSION, pNewName, _TRUNCATE);
            return S_OK;
        }
        ppList = &((*ppList)->pNext);
    }
    return S_FALSE;
}

// Saves the active watch list together with the current evaluations as
// a new persisted watch list
HRESULT WatchCmd::SaveList(__in_z WCHAR* pSaveName)
{
    HRESULT Status = S_OK;
    INIT_API_EE();
    INIT_API_DAC();
    IfFailRet(InitCorDebugInterface());

    RemoveList(pSaveName);
    PersistList* pList = new PersistList();
    wcsncpy_s(pList->pName, MAX_EXPRESSION, pSaveName, _TRUNCATE);
    pList->pHeadExpr = NULL;
    PersistCallbackData data;
    data.ppNext = &(pList->pHeadExpr);
    WatchExpression* pExpression = pExpressionListHead;
    while(pExpression != NULL)
    {
        ExpressionNode* pResult = NULL;
        if(SUCCEEDED(Status = ExpressionNode::CreateExpressionNode(pExpression->pExpression, &pResult)))
        {
            pResult->DFSVisit(PersistCallback, (VOID*)&data);
            delete pResult;
        }
        pExpression = pExpression->pNext;
    }

    pList->pNext = pPersistListHead;
    pPersistListHead = pList;
    return Status;
}

// Saves the current watch list to file as a sequence of commands that will
// recreate the list
HRESULT WatchCmd::SaveListToFile(FILE* pFile)
{
    WatchExpression* pExpression = pExpressionListHead;
    while(pExpression != NULL)
    {
        fprintf_s(pFile, "!watch -a %S\n", pExpression->pExpression);
        pExpression = pExpression->pNext;
    }
    return S_OK;
}

// Escapes characters that would be interpretted as DML markup, namely angle brackets
// that often appear in generic type names
VOID WatchCmd::DmlEscape(__in_ecount(cchInput) WCHAR* pInput, int cchInput, __in_ecount(cchOutput) WCHAR* pEscapedOutput, int cchOutput)
{
    pEscapedOutput[0] = L'\0';
    for(int i = 0; i < cchInput; i++)
    {
        if(pInput[i] == L'<') 
        {
            if(0 != wcscat_s(pEscapedOutput, cchOutput, L"&lt;")) return;
            pEscapedOutput += 4;
            cchOutput -= 4;
        }
        else if(pInput[i] == L'>')
        {
            if(0 != wcscat_s(pEscapedOutput, cchOutput, L"&gt;")) return;
            pEscapedOutput += 4;
            cchOutput -= 4;
        }
        else if(cchOutput > 1)
        {
            pEscapedOutput[0] = pInput[i];
            pEscapedOutput[1] = '\0';
            pEscapedOutput++;
            cchOutput--;
        }
        if(pInput[i] == L'\0' || cchOutput == 1) break;
    }
}

// A DFS traversal callback for the expression node tree that prints it
VOID WatchCmd::EvalPrintCallback(ExpressionNode* pExpressionNode, int depth, VOID* pUserData)
{
    PrintCallbackData* pData = (PrintCallbackData*)pUserData;
    for(int i = 0; i < depth; i++) ExtOut("    ");
    if(depth == 0)
        ExtOut("  %d) ", pData->index);
    else
        ExtOut(" |- ");
    if(pExpressionNode->GetErrorMessage()[0] != 0)
    {
        ExtOut("%S (%S)\n", pExpressionNode->GetRelativeExpression(), pExpressionNode->GetErrorMessage());
    }
    else
    {
        // names can have '<' and '>' in them, need to escape
        WCHAR pEscapedTypeName[MAX_EXPRESSION];
        DmlEscape(pExpressionNode->GetTypeName(), (int)_wcslen(pExpressionNode->GetTypeName()), pEscapedTypeName, MAX_EXPRESSION);
        WCHAR pRelativeExpression[MAX_EXPRESSION];
        DmlEscape(pExpressionNode->GetRelativeExpression(), (int)_wcslen(pExpressionNode->GetRelativeExpression()), pRelativeExpression, MAX_EXPRESSION);
        DMLOut("%S <exec cmd=\"%S (%S)%S\">%S</exec> %S\n", pEscapedTypeName, pData->pCommand, pEscapedTypeName, pExpressionNode->GetAbsoluteExpression(), pRelativeExpression, pExpressionNode->GetTextValue());
    }
}

// A DFS traversal callback for the expression node tree that saves all the values into a new
// persisted watch list
VOID WatchCmd::PersistCallback(ExpressionNode* pExpressionNode, int depth, VOID* pUserData)
{
    PersistCallbackData* pData = (PersistCallbackData*)pUserData;
    if(depth != 0)
        return;

    PersistWatchExpression* pPersistExpr = new PersistWatchExpression();
    wcsncpy_s(pPersistExpr->pExpression, MAX_EXPRESSION, pExpressionNode->GetAbsoluteExpression(), _TRUNCATE);
    FormatPersistResult(pPersistExpr->pPersistResult, MAX_EXPRESSION, pExpressionNode);
    pPersistExpr->pNext = NULL;
    *(pData->ppNext) = pPersistExpr;
    pData->ppNext = &(pPersistExpr->pNext);
}

// Determines how the value of an expression node is saved as a persisted result. This effectively determines
// the definition of equality when determining if an expression has changed value
VOID WatchCmd::FormatPersistResult(__inout_ecount(cchPersistResult)  WCHAR* pPersistResult, DWORD cchPersistResult, ExpressionNode* pExpressionNode)
{
    if(pExpressionNode->GetErrorMessage()[0] != 0)
    {
        _snwprintf_s(pPersistResult, MAX_EXPRESSION, _TRUNCATE, L"%s (%s)\n", pExpressionNode->GetRelativeExpression(), pExpressionNode->GetErrorMessage());
    }
    else
    {
        _snwprintf_s(pPersistResult, MAX_EXPRESSION, _TRUNCATE, L"%s %s %s\n", pExpressionNode->GetTypeName(), pExpressionNode->GetRelativeExpression(), pExpressionNode->GetTextValue());
    }
}
