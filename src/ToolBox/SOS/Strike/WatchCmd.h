// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _WATCH_CMD_
#define _WATCH_CMD_

#ifdef FEATURE_PAL
#error This file not designed for use with FEATURE_PAL
#endif

#include "ExpressionNode.h"
#include "windows.h"

// A linked list node for watch expressions
typedef struct _WatchExpression
{
    WCHAR pExpression[MAX_EXPRESSION];
    _WatchExpression* pNext;

} WatchExpression;

// A linked list node that stores both the watch expression and a persisted result
// of the evaluation at some point in the past
typedef struct _PersistWatchExpression
{
    WCHAR pExpression[MAX_EXPRESSION];
    WCHAR pPersistResult[MAX_EXPRESSION];
    _PersistWatchExpression* pNext;

} PersistWatchExpression;

// A named list of persisted watch expressions, each of which has an expression and
// a saved value
typedef struct _PersistList
{
    ~_PersistList();
    WCHAR pName[MAX_EXPRESSION];
    PersistWatchExpression* pHeadExpr;
    _PersistList* pNext;
} PersistList;

// An API for the functionality in the !watch command
class WatchCmd
{
public:
    WatchCmd();
    ~WatchCmd();

    // Deletes all current watch expressions from the watch list
    // (does not delete persisted watch lists though)
    HRESULT Clear();

    // Adds a new expression to the active watch list
    HRESULT Add(__in_z WCHAR* pExpression);

    // removes an expression at the given index in the active watch list
    HRESULT Remove(int index);

    // Evaluates and prints a tree version of the active watch list
    // The tree will be expanded along the nodes in expansionPath
    // Optionally the list is filtered to only show differences from pFilterName (the name of a persisted watch list)
    HRESULT Print(int expansionIndex, __in_z WCHAR* expansionPath, __in_z WCHAR* pFilterName);

    // Deletes an persisted watch list by name
    HRESULT RemoveList(__in_z WCHAR* pListName);

    // Renames a previously saved persisted watch list
    HRESULT RenameList(__in_z WCHAR* pOldName, __in_z WCHAR* pNewName);

    // Saves the active watch list together with the current evaluations as
    // a new persisted watch list
    HRESULT SaveList(__in_z WCHAR* pSaveName);

    // Saves the current watch list to file as a sequence of commands that will
    // recreate the list
    HRESULT SaveListToFile(FILE* pFile);

private:
    WatchExpression* pExpressionListHead;
    PersistList* pPersistListHead;

    // Escapes characters that would be interpretted as DML markup, namely angle brackets
    // that often appear in generic type names
    static VOID DmlEscape(__in_z WCHAR* pInput, int cchInput, __inout_ecount(cchOutput) WCHAR* pEscapedOutput, int cchOutput);

    typedef struct _PrintCallbackData
    {
        int index;
        WCHAR* pCommand;
    } PrintCallbackData;

    // A DFS traversal callback for the expression node tree that prints it
    static VOID EvalPrintCallback(ExpressionNode* pExpressionNode, int depth, VOID* pUserData);

    typedef struct _PersistCallbackData
    {
        PersistWatchExpression** ppNext;
    } PersistCallbackData;

    // A DFS traversal callback for the expression node tree that saves all the values into a new
    // persisted watch list
    static VOID PersistCallback(ExpressionNode* pExpressionNode, int depth, VOID* pUserData);

    // Determines how the value of an expression node is saved as a persisted result. This effectively determines
    // the definition of equality when determining if an expression has changed value
    static VOID FormatPersistResult(__inout_ecount(cchPersistResult) WCHAR* pPersistResult, DWORD cchPersistResult, ExpressionNode* pExpressionNode);
};

#endif
