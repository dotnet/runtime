// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _EXPRESSION_NODE_
#define _EXPRESSION_NODE_

#ifdef FEATURE_PAL
#error This file isn't designed to build in PAL
#endif

#include "strike.h"
#include "sos.h"
#include "util.h"

#define MAX_EXPRESSION 500
#define MAX_ERROR 500


// Represents one node in a tree of expressions and sub-expressions
// These nodes are used in the !watch expandable expression tree
// Each node consists of a string based C#-like expression and its
// evaluation within the current context of the debuggee.
//
// These nodes are also intended for eventual use in ClrStack -i expression tree
// but ClrStack -i hasn't yet been refactored to use them
//
// Each node can evaluate to:
// nothing - if an error occurs during expression parsing or the expression
//           names don't match to anything in the debuggee
// a debuggee value - these are values that are backed in memory of the debuggee
//                    (ICorDebugValue objects) or build time constants which are
//                    stored in the assembly metadata.
// a debuggee type - instead of refering to a particular instance of a type (the
//                   value case above), nodes can directly refer to a type definition
//                   represented by an ICorDebugType object
class ExpressionNode
{
public:

    typedef VOID (*ExpressionNodeVisitorCallback)(ExpressionNode* pExpressionNode, int depth, VOID* pUserData);
    
    // Returns the complete expression being evaluated to get the value for this node
    // The returned pointer is a string interior to this object - once you release
    // all references to this object the string is invalid.
    WCHAR* GetAbsoluteExpression();

    // Returns the sub expression that logically indicates how the parent expression
    // was built upon to reach this node. This relative value has no purpose other
    // than an identifier and to convey UI meaning to the user. At present typical values
    // are the name of type, a local, a parameter, a field, an array index, or '<basetype>'
    // for a baseclass casting operation
    // The returned pointer is a string interior to this object - once you release
    // all references to this object the string is invalid.
    WCHAR* GetRelativeExpression();

    // Returns a text representation of the type of value that this node refers to
    // It is possible this node doesn't evaluate to anything and therefore has no
    // type
    // The returned pointer is a string interior to this object - once you release
    // all references to this object the string is invalid.
    WCHAR* GetTypeName();

    // Returns a text representation of the value for this node. It is possible that
    // this node doesn't evaluate to anything and therefore has no value text.
    // The returned pointer is a string interior to this object - once you release
    // all references to this object the string is invalid.
    WCHAR* GetTextValue();

    // If there is any error during the evaluation of this node's expression, it is
    // returned here.
    // The returned pointer is a string interior to this object - once you release
    // all references to this object the string is invalid.
    WCHAR* GetErrorMessage();
    
    // Factory function for creating the expression node at the root of a tree
    static HRESULT CreateExpressionNode(__in_z WCHAR* pExpression, ExpressionNode** ppExpressionNode);

    // Performs recursive expansion within the tree for nodes that are along the path to varToExpand.
    // Expansion involves calulating a set of child expressions from the current expression via
    // field dereferencing, array index dereferencing, or casting to a base type.
    // For example if a tree was rooted with expression 'foo.bar' and varToExpand is '(Baz)foo.bar[9]'
    // then 'foo.bar', 'foo.bar[9]', and '(Baz)foo.bar[9]' nodes would all be expanded.
    HRESULT Expand(__in_z WCHAR* varToExpand);

    // Standard depth first search tree traversal pattern with a callback
    VOID DFSVisit(ExpressionNodeVisitorCallback pFunc, VOID* pUserData, int depth=0);

private:
    // for nodes that evaluate to a type, this is that type
    // for nodes that evaluate to a debuggee value, this is the type of that
    // value or one of its base types. It represents the type the value should
    // displayed and expanded as.
    ToRelease<ICorDebugType> pTypeCast;

    // for nodes that evaluate to a memory backed debuggee value, this is that value
    ToRelease<ICorDebugValue> pValue;

    // if this node gets expanded and it has thread-static or context-static sub-fields,
    // this frame disambiguates which thread and context to use.
    ToRelease<ICorDebugILFrame> pILFrame;

    // TODO: exactly which metadata is this supposed to be? try to get rid of this
    ToRelease<IMetaDataImport> pMD;

    // PERF: this could be a lot more memory efficient
    WCHAR pTextValue[MAX_EXPRESSION];
    WCHAR pErrorMessage[MAX_ERROR];
    WCHAR pAbsoluteExpression[MAX_EXPRESSION];
    WCHAR pRelativeExpression[MAX_EXPRESSION];
    WCHAR pTypeName[MAX_EXPRESSION];

    // if this value represents a build time constant debuggee value, this is a pointer
    // to the value data stored in metadata and its size
    UVCP_CONSTANT pDefaultValue;
    ULONG cchDefaultValue;

    // Pointer in a linked list of sibling nodes that all share the same parent
    ExpressionNode* pNextSibling;
    // Pointer to the first child node of this node, other children can be found
    // by following the child's sibling list.
    ExpressionNode* pChild;

    typedef VOID (*VariableEnumCallback)(ICorDebugValue* pValue, WCHAR* pName, WCHAR* pErrorMessage, VOID* pUserData);
    typedef VOID (*FrameEnumCallback)(ICorDebugFrame* pFrame, VOID* pUserData);
    
    // Indicates how a child node was derived from its parent
    enum ChildKind
    {
        ChildKind_Field,
        ChildKind_Index,
        ChildKind_BaseClass
    };

    // Creates a new expression with a given debuggee value and frame
    ExpressionNode(__in_z WCHAR* pExpression, ICorDebugValue* pValue, ICorDebugILFrame* pFrame);

    // Creates a new expression that has an error and no value
    ExpressionNode(__in_z WCHAR* pExpression, __in_z WCHAR* pErrorMessage);

    // Creates a new child expression
    ExpressionNode(__in_z WCHAR* pParentExpression, ChildKind ck, __in_z WCHAR* pRelativeExpression, ICorDebugValue* pValue, ICorDebugType* pType, ICorDebugILFrame* pFrame, UVCP_CONSTANT pDefaultValue = NULL, ULONG cchDefaultValue = 0);

    // Common member initialization for the constructors
    VOID Init(ICorDebugValue* pValue, ICorDebugType* pTypeCast, ICorDebugILFrame* pFrame);

    // Retreves the correct IMetaDataImport for the type represented in this node and stores it
    // in pMD.
    HRESULT PopulateMetaDataImport();

    // Determines the string representation of pType and stores it in typeName
    static HRESULT CalculateTypeName(ICorDebugType * pType, __inout_ecount(typeNameLen) WCHAR* typeName, DWORD typeNameLen);
    

    // Appends angle brackets and the generic argument list to a type name
    static HRESULT AddGenericArgs(ICorDebugType * pType, __inout_ecount(typeNameLen) WCHAR* typeName, DWORD typeNameLen);

    // Determines the text name for the type of this node and caches it
    HRESULT PopulateType();

    // Node expansion helpers

    // Inserts a new child at the end of the linked list of children
    // PERF: This has O(N) insert time but these lists should never be large
    VOID AddChild(ExpressionNode* pNewChild);

    // Helper that determines if the current node is on the path of nodes represented by
    // expression varToExpand
    BOOL ShouldExpandVariable(__in_z WCHAR* varToExpand);

    // Expands this array node by creating child nodes with expressions refering to individual array elements
    HRESULT ExpandSzArray(ICorDebugValue* pInnerValue, __in_z WCHAR* varToExpand);

    // Expands this struct/class node by creating child nodes with expressions refering to individual field values
    // and one node for the basetype value
    HRESULT ExpandFields(ICorDebugValue* pInnerValue, __in_z WCHAR* varToExpand);
    
    // Value Population functions

    //Helper for unwrapping values
    static HRESULT DereferenceAndUnboxValue(ICorDebugValue * pInputValue, ICorDebugValue** ppOutputValue, BOOL * pIsNull = NULL);

    // Returns TRUE if the value derives from System.Enum
    static BOOL IsEnum(ICorDebugValue * pInputValue);

    // Calculates the value text for nodes that have enum values
    HRESULT PopulateEnumValue(ICorDebugValue* pEnumValue, BYTE* enumValue);

    // Helper that fetches the text value of a string ICorDebugValue
    HRESULT GetDebuggeeStringValue(ICorDebugValue* pInputValue, __inout_ecount(cchBuffer) WCHAR* wszBuffer, DWORD cchBuffer);

    // Helper that fetches the text value of a string build-time literal
    HRESULT GetConstantStringValue(__inout_ecount(cchBuffer) WCHAR* wszBuffer, DWORD cchBuffer);

    // Helper that caches the textual value for nodes that evaluate to array objects
    HRESULT PopulateSzArrayValue(ICorDebugValue* pInputValue);

    // Helper that caches the textual value for nodes of any type
    HRESULT PopulateTextValueHelper();

    // Caches the textual value of this node
    HRESULT PopulateTextValue();


    // Expression parsing and search

    // In/Out parameters for the EvaluateExpressionFrameScanCallback
    typedef struct _EvaluateExpressionFrameScanData
    {
        WCHAR* pIdentifier;
        ToRelease<ICorDebugValue> pFoundValue;
        ToRelease<ICorDebugILFrame> pFoundFrame;
        ToRelease<ICorDebugILFrame> pFirstFrame;
        WCHAR* pErrorMessage;
        DWORD cchErrorMessage;
    } EvaluateExpressionFrameScanData;

    //Callback that searches a frame to determine if it contains a local variable or parameter of a given name
    static VOID EvaluateExpressionFrameScanCallback(ICorDebugFrame* pFrame, VOID* pUserData);

    //Callback checks to see if a given local/parameter has name pName
    static VOID EvaluateExpressionVariableScanCallback(ICorDebugValue* pValue, __in_z WCHAR* pName, __out_z WCHAR* pErrorMessage, VOID* pUserData);

    //Factory method that recursively parses pExpression and create an ExpressionNode
    //  pExpression -          the entire expression being parsed
    //  pExpressionRemainder - the portion of the expression that remains to be parsed in this
    //                         recursive invocation
    //  charactersParsed     - the number of characters that have been parsed from pExpression
    //                         so far (todo: this is basically the difference between remainder and
    //                         full expression, do we need it?)
    //  pParsedValue         - A debuggee value that should be used as the context for interpreting
    //                         pExpressionRemainder
    //  pParsedType          - A debuggee type that should be used as the context for interpreting
    //                         pExpressionRemainder. 
    //  pParsedDefaultValue  - A fixed value from metadata that should be used as context for
    //                         interpretting pExpressionRemainder
    //  cchParsedDefaultValue- Size of pParsedDefaultValue
    //  pFrame               - A debuggee IL frame that disambiguates the thread and context needed
    //                         to evaluate a thread-static or context-static value
    //  ppExpressionNode     - OUT - the resulting expression node
    //
    //
    static HRESULT CreateExpressionNodeHelper(__in_z WCHAR* pExpression,
                                              __in_z WCHAR* pExpressionParseRemainder,
                                              DWORD charactersParsed,
                                              ICorDebugValue* pParsedValue,
                                              ICorDebugType* pParsedType,
                                              UVCP_CONSTANT pParsedDefaultValue,
                                              ULONG cchParsedDefaultValue,
                                              ICorDebugILFrame* pFrame,
                                              ExpressionNode** ppExpressionNode);

    // Splits apart a C#-like expression and determines the first identifier in the string and updates expression to point
    // at the remaining unparsed portion
    static HRESULT ParseNextIdentifier(__in_z WCHAR** expression,
                                       __inout_ecount(cchIdentifierName) WCHAR* identifierName,
                                       DWORD cchIdentifierName,
                                       __inout_ecount(cchErrorMessage) WCHAR* errorMessage,
                                       DWORD cchErrorMessage,
                                       DWORD* charactersParsed,
                                       BOOL* isArrayIndex);


    // Iterate through all parameters in the ILFrame calling the callback function for each of them
    static HRESULT EnumerateParameters(IMetaDataImport * pMD,
                                       mdMethodDef methodDef,
                                       ICorDebugILFrame * pILFrame,
                                       VariableEnumCallback pCallback,
                                       VOID* pUserData);

    // Enumerate all locals in the given ILFrame, calling the callback method for each of them
    static HRESULT EnumerateLocals(IMetaDataImport * pMD,
                                   mdMethodDef methodDef,
                                   ICorDebugILFrame * pILFrame,
                                   VariableEnumCallback pCallback,
                                   VOID* pUserData);

    // Iterates over all frames on the current thread's stack, calling the callback function for each of them
    static HRESULT EnumerateFrames(FrameEnumCallback pCallback, VOID* pUserData);

    // Determines the corresponding ICorDebugType for a given primitive type
    static HRESULT FindTypeFromElementType(CorElementType et, ICorDebugType** ppType);

    // Gets the appropriate element type encoding for well-known fully qualified type names
    // This doesn't work for arbitrary types, just types that have CorElementType short forms.
    static HRESULT GetCanonicalElementTypeForTypeName(__in_z WCHAR* pTypeName, CorElementType *et);

    // Searches the debuggee for any ICorDebugType that matches the given fully qualified name
    // This will search across all AppDomains and Assemblies
    static HRESULT FindTypeByName(__in_z WCHAR* pTypeName, ICorDebugType** ppType);

    // Searches the debuggee for any ICorDebugType that matches the given fully qualified name
    // This will search across all Assemblies in the given AppDomain
    static HRESULT FindTypeByName(ICorDebugAppDomain* pAppDomain, __in_z WCHAR* pTypeName, ICorDebugType** ppType);

    // Searches the assembly for any ICorDebugType that matches the given fully qualified name
    static HRESULT FindTypeByName(ICorDebugAssembly* pAssembly, __in_z WCHAR* pTypeName, ICorDebugType** ppType);

    // Searches a given module for any ICorDebugType that matches the given fully qualified type name
    static HRESULT FindTypeByName(ICorDebugModule* pModule, __in_z WCHAR* pTypeName, ICorDebugType** ppType);

    // Checks whether the given token is or refers to type System.ValueType or System.Enum
    static HRESULT IsTokenValueTypeOrEnum(mdToken token, IMetaDataImport* pMetadata, BOOL* pResult);
};

#endif
