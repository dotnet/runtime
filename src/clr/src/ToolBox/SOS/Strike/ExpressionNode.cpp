// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ExpressionNode.h"


#ifndef IfFailRet
#define IfFailRet(EXPR) do { Status = (EXPR); if(FAILED(Status)) { return (Status); } } while (0)
#endif

// Returns the complete expression being evaluated to get the value for this node
// The returned pointer is a string interior to this object - once you release
// all references to this object the string is invalid.
WCHAR* ExpressionNode::GetAbsoluteExpression() { return pAbsoluteExpression; }

// Returns the sub expression that logically indicates how the parent expression
// was built upon to reach this node. This relative value has no purpose other
// than an identifier and to convey UI meaning to the user. At present typical values
// are the name of type, a local, a parameter, a field, an array index, or '<basetype>'
// for a baseclass casting operation
// The returned pointer is a string interior to this object - once you release
// all references to this object the string is invalid.
WCHAR* ExpressionNode::GetRelativeExpression() { return pRelativeExpression; }

// Returns a text representation of the type of value that this node refers to
// It is possible this node doesn't evaluate to anything and therefore has no
// type
// The returned pointer is a string interior to this object - once you release
// all references to this object the string is invalid.
WCHAR* ExpressionNode::GetTypeName() { PopulateType(); return pTypeName; }

// Returns a text representation of the value for this node. It is possible that
// this node doesn't evaluate to anything and therefore has no value text.
// The returned pointer is a string interior to this object - once you release
// all references to this object the string is invalid.
WCHAR* ExpressionNode::GetTextValue() { PopulateTextValue(); return pTextValue; }

// If there is any error during the evaluation of this node's expression, it is
// returned here.
// The returned pointer is a string interior to this object - once you release
// all references to this object the string is invalid.
WCHAR* ExpressionNode::GetErrorMessage() { return pErrorMessage; }

// Factory function for creating the expression node at the root of a tree
HRESULT ExpressionNode::CreateExpressionNode(__in_z WCHAR* pExpression, ExpressionNode** ppExpressionNode)
{
    *ppExpressionNode = NULL;
    HRESULT Status = CreateExpressionNodeHelper(pExpression,
        pExpression,
        0,
        NULL,
        NULL,
        NULL,
        0,
        NULL,
        ppExpressionNode);
    if(FAILED(Status) && *ppExpressionNode == NULL)
    {

        WCHAR pErrorMessage[MAX_ERROR];
        _snwprintf_s(pErrorMessage, MAX_ERROR, _TRUNCATE, L"Error 0x%x while parsing expression", Status);
        *ppExpressionNode = new ExpressionNode(pExpression, pErrorMessage);
        Status = S_OK;
        if(*ppExpressionNode == NULL)
            Status = E_OUTOFMEMORY;
    }
    return Status;
}

// Performs recursive expansion within the tree for nodes that are along the path to varToExpand.
// Expansion involves calulating a set of child expressions from the current expression via
// field dereferencing, array index dereferencing, or casting to a base type.
// For example if a tree was rooted with expression 'foo.bar' and varToExpand is '(Baz)foo.bar[9]'
// then 'foo.bar', 'foo.bar[9]', and '(Baz)foo.bar[9]' nodes would all be expanded.
HRESULT ExpressionNode::Expand(__in_z WCHAR* varToExpand)
{
    if(!ShouldExpandVariable(varToExpand))
        return S_FALSE;
    if(pValue == NULL && pTypeCast == NULL)
        return S_OK;

    // if the node evaluates to a type, then the children are static fields of the type
    if(pValue == NULL)
        return ExpandFields(NULL, varToExpand);

    // If the value is a null reference there is nothing to expand
    HRESULT Status = S_OK;
    BOOL isNull = TRUE;
    ToRelease<ICorDebugValue> pInnerValue;
    IfFailRet(DereferenceAndUnboxValue(pValue, &pInnerValue, &isNull));
    if(isNull)
    {
        return S_OK;
    }


    CorElementType corElemType;
    IfFailRet(pValue->GetType(&corElemType));
    if (corElemType == ELEMENT_TYPE_SZARRAY)
    {
        //If its an array, add children representing the indexed elements
        return ExpandSzArray(pInnerValue, varToExpand);
    }
    else if(corElemType == ELEMENT_TYPE_CLASS || corElemType == ELEMENT_TYPE_VALUETYPE)
    {
        // If its a class or struct (not counting string, array, or object) then add children representing
        // the fields.
        return ExpandFields(pInnerValue, varToExpand);
    }
    else
    {
        // nothing else expands
        return S_OK;
    }
}

// Standard depth first search tree traversal pattern with a callback
VOID ExpressionNode::DFSVisit(ExpressionNodeVisitorCallback pFunc, VOID* pUserData, int depth)
{
    pFunc(this, depth, pUserData);
    ExpressionNode* pCurChild = pChild;
    while(pCurChild != NULL)
    {
        pCurChild->DFSVisit(pFunc, pUserData, depth+1);
        pCurChild = pCurChild->pNextSibling;
    }
}


// Creates a new expression with a given debuggee value and frame
ExpressionNode::ExpressionNode(__in_z WCHAR* pExpression, ICorDebugValue* pValue, ICorDebugILFrame* pFrame)
{
    Init(pValue, NULL, pFrame);
    _snwprintf_s(pAbsoluteExpression, MAX_EXPRESSION, _TRUNCATE, L"%s", pExpression);
    _snwprintf_s(pRelativeExpression, MAX_EXPRESSION, _TRUNCATE, L"%s", pExpression);
}

// Creates a new expression that has an error and no value
ExpressionNode::ExpressionNode(__in_z WCHAR* pExpression, __in_z WCHAR* pErrorMessage)
{
    Init(NULL, NULL, NULL);
    _snwprintf_s(pAbsoluteExpression, MAX_EXPRESSION, _TRUNCATE, L"%s", pExpression);
    _snwprintf_s(pRelativeExpression, MAX_EXPRESSION, _TRUNCATE, L"%s", pExpression);
    _snwprintf_s(this->pErrorMessage, MAX_ERROR, _TRUNCATE, L"%s", pErrorMessage);
}

// Creates a new child expression
ExpressionNode::ExpressionNode(__in_z WCHAR* pParentExpression, ChildKind ck, __in_z WCHAR* pRelativeExpression, ICorDebugValue* pValue, ICorDebugType* pType, ICorDebugILFrame* pFrame, UVCP_CONSTANT pDefaultValue, ULONG cchDefaultValue)
{
    Init(pValue, pType, pFrame);
    if(ck == ChildKind_BaseClass)
    {
        _snwprintf_s(pAbsoluteExpression, MAX_EXPRESSION, _TRUNCATE, L"%s", pParentExpression);
    }
    else
    {
        _snwprintf_s(pAbsoluteExpression, MAX_EXPRESSION, _TRUNCATE, ck == ChildKind_Field ? L"%s.%s" : L"%s[%s]", pParentExpression, pRelativeExpression);
    }
    _snwprintf_s(this->pRelativeExpression, MAX_EXPRESSION, _TRUNCATE, ck == ChildKind_Index ? L"[%s]" : L"%s", pRelativeExpression);
    this->pDefaultValue = pDefaultValue;
    this->cchDefaultValue = cchDefaultValue;
}

// Common member initialization for the constructors
VOID ExpressionNode::Init(ICorDebugValue* pValue, ICorDebugType* pTypeCast, ICorDebugILFrame* pFrame)
{
    this->pValue = pValue;
    this->pTypeCast = pTypeCast;
    this->pILFrame = pFrame;
    pChild = NULL;
    pNextSibling = NULL;
    pTextValue[0] = 0;
    pErrorMessage[0] = 0;
    pAbsoluteExpression[0] = 0;
    pRelativeExpression[0] = 0;
    pTypeName[0] = 0;

    pDefaultValue = NULL;
    cchDefaultValue = 0;

    // The ToRelease holders don't automatically AddRef
    if(pILFrame != NULL)
        pILFrame->AddRef();
    if(pTypeCast != NULL)
        pTypeCast->AddRef();
    if(pValue != NULL)
    {   
        pValue->AddRef();
        PopulateMetaDataImport();
    }
}

// Retreves the correct IMetaDataImport for the type represented in this node and stores it
// in pMD.
HRESULT ExpressionNode::PopulateMetaDataImport()
{
    if(pMD != NULL)
        return S_OK;

    HRESULT Status = S_OK;
    ToRelease<ICorDebugType> pType;
    if(pTypeCast != NULL)
    {
        pType = pTypeCast;
        pType->AddRef();
    }
    else
    {
        BOOL isNull;
        ToRelease<ICorDebugValue> pInnerValue;
        IfFailRet(DereferenceAndUnboxValue(pValue, &pInnerValue, &isNull));
        ToRelease<ICorDebugValue2> pValue2;
        IfFailRet(pInnerValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2));

        IfFailRet(pValue2->GetExactType(&pType));

        // for array, pointer, and byref types we can't directly get a class, we must unwrap first
        CorElementType et;
        IfFailRet(pType->GetType(&et));
        while(et == ELEMENT_TYPE_ARRAY || et == ELEMENT_TYPE_SZARRAY || et == ELEMENT_TYPE_BYREF || et == ELEMENT_TYPE_PTR)
        {
            pType->GetFirstTypeParameter(&pType);
            IfFailRet(pType->GetType(&et));
        }
    }
    ToRelease<ICorDebugClass> pClass;
    IfFailRet(pType->GetClass(&pClass));
    ToRelease<ICorDebugModule> pModule;
    IfFailRet(pClass->GetModule(&pModule));
    ToRelease<IUnknown> pMDUnknown;
    IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
    IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));
    return Status;
}

// Determines the string representation of pType and stores it in typeName
HRESULT ExpressionNode::CalculateTypeName(ICorDebugType * pType, __inout_ecount(typeNameLen) WCHAR* typeName, DWORD typeNameLen)
{
    HRESULT Status = S_OK;

    CorElementType corElemType;
    IfFailRet(pType->GetType(&corElemType));

    switch (corElemType)
    {
        //List of unsupported CorElementTypes:
        //ELEMENT_TYPE_END            = 0x0,
        //ELEMENT_TYPE_VAR            = 0x13,     // a class type variable VAR <U1>
        //ELEMENT_TYPE_GENERICINST    = 0x15,     // GENERICINST <generic type> <argCnt> <arg1> ... <argn>
        //ELEMENT_TYPE_TYPEDBYREF     = 0x16,     // TYPEDREF  (it takes no args) a typed referece to some other type
        //ELEMENT_TYPE_MVAR           = 0x1e,     // a method type variable MVAR <U1>
        //ELEMENT_TYPE_CMOD_REQD      = 0x1F,     // required C modifier : E_T_CMOD_REQD <mdTypeRef/mdTypeDef>
        //ELEMENT_TYPE_CMOD_OPT       = 0x20,     // optional C modifier : E_T_CMOD_OPT <mdTypeRef/mdTypeDef>
        //ELEMENT_TYPE_INTERNAL       = 0x21,     // INTERNAL <typehandle>
        //ELEMENT_TYPE_MAX            = 0x22,     // first invalid element type
        //ELEMENT_TYPE_MODIFIER       = 0x40,
        //ELEMENT_TYPE_SENTINEL       = 0x01 | ELEMENT_TYPE_MODIFIER, // sentinel for varargs
        //ELEMENT_TYPE_PINNED         = 0x05 | ELEMENT_TYPE_MODIFIER,
    default:
        swprintf_s(typeName, typeNameLen, L"(Unhandled CorElementType: 0x%x)\0", corElemType);
        break;

    case ELEMENT_TYPE_VALUETYPE:
    case ELEMENT_TYPE_CLASS:
        {
            //Defaults in case we fail...
            if(corElemType == ELEMENT_TYPE_VALUETYPE) swprintf_s(typeName, typeNameLen, L"struct\0");
            else swprintf_s(typeName, typeNameLen, L"class\0");

            mdTypeDef typeDef;
            ToRelease<ICorDebugClass> pClass;
            if(SUCCEEDED(pType->GetClass(&pClass)) && SUCCEEDED(pClass->GetToken(&typeDef)))
            {
                ToRelease<ICorDebugModule> pModule;
                IfFailRet(pClass->GetModule(&pModule));

                ToRelease<IUnknown> pMDUnknown;
                ToRelease<IMetaDataImport> pMD;
                IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
                IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));

                if(SUCCEEDED(NameForToken_s(TokenFromRid(typeDef, mdtTypeDef), pMD, g_mdName, mdNameLen, false)))
                    swprintf_s(typeName, typeNameLen, L"%s\0", g_mdName);
            }
            AddGenericArgs(pType, typeName, typeNameLen);
        }
        break;
    case ELEMENT_TYPE_VOID:
        swprintf_s(typeName, typeNameLen, L"void\0");
        break;
    case ELEMENT_TYPE_BOOLEAN:
        swprintf_s(typeName, typeNameLen, L"bool\0");
        break;
    case ELEMENT_TYPE_CHAR:
        swprintf_s(typeName, typeNameLen, L"char\0");
        break;
    case ELEMENT_TYPE_I1:
        swprintf_s(typeName, typeNameLen, L"signed byte\0");
        break;
    case ELEMENT_TYPE_U1:
        swprintf_s(typeName, typeNameLen, L"byte\0");
        break;
    case ELEMENT_TYPE_I2:
        swprintf_s(typeName, typeNameLen, L"short\0");
        break;
    case ELEMENT_TYPE_U2:
        swprintf_s(typeName, typeNameLen, L"unsigned short\0");
        break;    
    case ELEMENT_TYPE_I4:
        swprintf_s(typeName, typeNameLen, L"int\0");
        break;
    case ELEMENT_TYPE_U4:
        swprintf_s(typeName, typeNameLen, L"unsigned int\0");
        break;
    case ELEMENT_TYPE_I8:
        swprintf_s(typeName, typeNameLen, L"long\0");
        break;
    case ELEMENT_TYPE_U8:
        swprintf_s(typeName, typeNameLen, L"unsigned long\0");
        break;
    case ELEMENT_TYPE_R4:
        swprintf_s(typeName, typeNameLen, L"float\0");
        break;
    case ELEMENT_TYPE_R8:
        swprintf_s(typeName, typeNameLen, L"double\0");
        break;
    case ELEMENT_TYPE_OBJECT:
        swprintf_s(typeName, typeNameLen, L"object\0");
        break;
    case ELEMENT_TYPE_STRING:
        swprintf_s(typeName, typeNameLen, L"string\0");
        break;
    case ELEMENT_TYPE_I:
        swprintf_s(typeName, typeNameLen, L"IntPtr\0");
        break;
    case ELEMENT_TYPE_U:
        swprintf_s(typeName, typeNameLen, L"UIntPtr\0");
        break;
    case ELEMENT_TYPE_SZARRAY:
    case ELEMENT_TYPE_ARRAY:
    case ELEMENT_TYPE_BYREF:
    case ELEMENT_TYPE_PTR:
        {
            // get a name for the type we are building from
            ToRelease<ICorDebugType> pFirstParameter;
            if(SUCCEEDED(pType->GetFirstTypeParameter(&pFirstParameter)))
                CalculateTypeName(pFirstParameter, typeName, typeNameLen);
            else
                swprintf_s(typeName, typeNameLen, L"<unknown>\0");

            // append the appropriate [], *, &
            switch(corElemType)
            {
            case ELEMENT_TYPE_SZARRAY: 
                wcsncat_s(typeName, typeNameLen, L"[]", typeNameLen);
                return S_OK;
            case ELEMENT_TYPE_ARRAY:
                {
                    ULONG32 rank = 0;
                    pType->GetRank(&rank);
                    wcsncat_s(typeName, typeNameLen, L"[", typeNameLen);
                    for(ULONG32 i = 0; i < rank - 1; i++)
                    {
                        // todo- could we print out exact boundaries?
                        wcsncat_s(typeName, typeNameLen, L",", typeNameLen);
                    }
                    wcsncat_s(typeName, typeNameLen, L"]", typeNameLen);
                }
                return S_OK;
            case ELEMENT_TYPE_BYREF:   
                wcsncat_s(typeName, typeNameLen, L"&", typeNameLen);
                return S_OK;
            case ELEMENT_TYPE_PTR:     
                wcsncat_s(typeName, typeNameLen, L"*", typeNameLen);
                return S_OK;
            }
        }
        break;
    case ELEMENT_TYPE_FNPTR:
        swprintf_s(typeName, typeNameLen, L"*(...)");
        break;
    case ELEMENT_TYPE_TYPEDBYREF:
        swprintf_s(typeName, typeNameLen, L"typedbyref");
        break;
    }
    return S_OK;
}


// Appends angle brackets and the generic argument list to a type name
HRESULT ExpressionNode::AddGenericArgs(ICorDebugType * pType, __inout_ecount(typeNameLen) WCHAR* typeName, DWORD typeNameLen)
{
    bool isFirst = true;
    ToRelease<ICorDebugTypeEnum> pTypeEnum;
    if(SUCCEEDED(pType->EnumerateTypeParameters(&pTypeEnum)))
    {
        ULONG numTypes = 0;
        ToRelease<ICorDebugType> pCurrentTypeParam;

        while(SUCCEEDED(pTypeEnum->Next(1, &pCurrentTypeParam, &numTypes)))
        {
            if(numTypes == 0) break;

            if(isFirst)
            {
                isFirst = false;
                wcsncat_s(typeName, typeNameLen, L"<", typeNameLen);
            }
            else wcsncat_s(typeName, typeNameLen, L",", typeNameLen);

            WCHAR typeParamName[mdNameLen];
            typeParamName[0] = L'\0';
            CalculateTypeName(pCurrentTypeParam, typeParamName, mdNameLen);
            wcsncat_s(typeName, typeNameLen, typeParamName, typeNameLen);
        }
        if(!isFirst)
            wcsncat_s(typeName, typeNameLen, L">", typeNameLen);
    }

    return S_OK;
}

// Determines the text name for the type of this node and caches it
HRESULT ExpressionNode::PopulateType()
{
    HRESULT Status = S_OK;
    if(pTypeName[0] != 0)
        return S_OK;

    //default value
    swprintf_s(pTypeName, MAX_EXPRESSION, L"<unknown>");

    // if we are displaying this type as a specific sub-type, use that
    if(pTypeCast != NULL)
        return CalculateTypeName(pTypeCast, pTypeName, MAX_EXPRESSION);

    // if there is no value then either we succesfully already determined the type
    // name, or this node has no value or type and thus no type name.
    if(pValue == NULL)
        return S_OK;

    // get the type from the value and then calculate a name based on that
    ToRelease<ICorDebugType> pType;
    ToRelease<ICorDebugValue2> pValue2;
    if(SUCCEEDED(pValue->QueryInterface(IID_ICorDebugValue2, (void**) &pValue2)) && SUCCEEDED(pValue2->GetExactType(&pType)))
        return CalculateTypeName(pType, pTypeName, MAX_EXPRESSION);

    return S_OK;
}

// Node expansion helpers

// Inserts a new child at the end of the linked list of children
// PERF: This has O(N) insert time but these lists should never be large
VOID ExpressionNode::AddChild(ExpressionNode* pNewChild)
{
    if(pChild == NULL)
        pChild = pNewChild;
    else
    {
        ExpressionNode* pCursor = pChild;
        while(pCursor->pNextSibling != NULL)
            pCursor = pCursor->pNextSibling;
        pCursor->pNextSibling = pNewChild;
    }
}

// Helper that determines if the current node is on the path of nodes represented by
// expression varToExpand
BOOL ExpressionNode::ShouldExpandVariable(__in_z WCHAR* varToExpand)
{
    if(pAbsoluteExpression == NULL || varToExpand == NULL) return FALSE;

    // if there is a cast operation, move past it
    WCHAR* pEndCast = _wcschr(varToExpand, L')');
    varToExpand = (pEndCast == NULL) ? varToExpand : pEndCast+1; 

    size_t varToExpandLen = _wcslen(varToExpand);
    size_t currentExpansionLen = _wcslen(pAbsoluteExpression);
    if(currentExpansionLen > varToExpandLen) return FALSE;
    if(currentExpansionLen < varToExpandLen && 
        varToExpand[currentExpansionLen] != L'.' &&
        varToExpand[currentExpansionLen] != L'[')
        return FALSE;
    if(_wcsncmp(pAbsoluteExpression, varToExpand, currentExpansionLen) != 0) return FALSE;

    return TRUE;
}

// Expands this array node by creating child nodes with expressions refering to individual array elements
HRESULT ExpressionNode::ExpandSzArray(ICorDebugValue* pInnerValue, __in_z WCHAR* varToExpand)
{
    HRESULT Status = S_OK;
    ToRelease<ICorDebugArrayValue> pArrayValue;
    IfFailRet(pInnerValue->QueryInterface(IID_ICorDebugArrayValue, (LPVOID*) &pArrayValue));

    ULONG32 nRank;
    IfFailRet(pArrayValue->GetRank(&nRank));
    if (nRank != 1)
    {
        _snwprintf_s(pErrorMessage, MAX_ERROR, _TRUNCATE, L"Multi-dimensional arrays NYI");
        return E_UNEXPECTED;
    }

    ULONG32 cElements;
    IfFailRet(pArrayValue->GetCount(&cElements));

    //TODO: do we really want all the elements? This could be huge!
    for (ULONG32 i=0; i < cElements; i++)
    {
        WCHAR index[20];
        swprintf_s(index, 20, L"%d", i);

        ToRelease<ICorDebugValue> pElementValue;
        IfFailRet(pArrayValue->GetElementAtPosition(i, &pElementValue));
        ExpressionNode* pExpr = new ExpressionNode(pAbsoluteExpression, ChildKind_Index, index, pElementValue, NULL, pILFrame);
        AddChild(pExpr);
        pExpr->Expand(varToExpand);
    }
    return S_OK;
}

// Expands this struct/class node by creating child nodes with expressions refering to individual field values
// and one node for the basetype value
HRESULT ExpressionNode::ExpandFields(ICorDebugValue* pInnerValue, __in_z WCHAR* varToExpand)
{
    HRESULT Status = S_OK;

    mdTypeDef currentTypeDef;
    ToRelease<ICorDebugClass> pClass;
    ToRelease<ICorDebugType> pType;
    ToRelease<ICorDebugModule> pModule;
    if(pTypeCast == NULL)
    {
        ToRelease<ICorDebugValue2> pValue2;
        IfFailRet(pInnerValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2));
        IfFailRet(pValue2->GetExactType(&pType));
    }
    else
    {
        pType = pTypeCast;
        pType->AddRef();
    }
    IfFailRet(pType->GetClass(&pClass));
    IfFailRet(pClass->GetModule(&pModule));
    IfFailRet(pClass->GetToken(&currentTypeDef));

    ToRelease<IUnknown> pMDUnknown;
    ToRelease<IMetaDataImport> pMD;
    IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
    IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));

    // If the current type has a base type that isn't object, enum, or ValueType then add a node for the base type
    WCHAR baseTypeName[mdNameLen] = L"\0";
    ToRelease<ICorDebugType> pBaseType;
    ExpressionNode* pBaseTypeNode = NULL;
    if(SUCCEEDED(pType->GetBase(&pBaseType)) && pBaseType != NULL && SUCCEEDED(CalculateTypeName(pBaseType, baseTypeName, mdNameLen)))
    {
        if(_wcsncmp(baseTypeName, L"System.Enum", 11) == 0)
            return S_OK;
        else if(_wcsncmp(baseTypeName, L"System.Object", 13) != 0 && _wcsncmp(baseTypeName, L"System.ValueType", 16) != 0)
        {
            pBaseTypeNode = new ExpressionNode(pAbsoluteExpression, ChildKind_BaseClass, L"<baseclass>", pInnerValue, pBaseType, pILFrame);
            AddChild(pBaseTypeNode);
        }
    }

    // add nodes for all the fields in this object
    ULONG numFields = 0;
    HCORENUM fEnum = NULL;
    mdFieldDef fieldDef;
    BOOL fieldExpanded = FALSE;
    while(SUCCEEDED(pMD->EnumFields(&fEnum, currentTypeDef, &fieldDef, 1, &numFields)) && numFields != 0)
    {
        mdTypeDef         classDef = 0;
        ULONG             nameLen = 0;
        DWORD             fieldAttr = 0;
        WCHAR             mdName[mdNameLen];
        WCHAR             typeName[mdNameLen];
        CorElementType    fieldDefaultValueEt;
        UVCP_CONSTANT     pDefaultValue;
        ULONG             cchDefaultValue;
        if(SUCCEEDED(pMD->GetFieldProps(fieldDef, &classDef, mdName, mdNameLen, &nameLen, &fieldAttr, NULL, NULL, (DWORD*)&fieldDefaultValueEt, &pDefaultValue, &cchDefaultValue)))
        {
            ToRelease<ICorDebugType> pFieldType;
            ToRelease<ICorDebugValue> pFieldVal;

            // static fields (of any kind - AppDomain, thread, context, RVA)
            if (fieldAttr & fdStatic)
            {
                pType->GetStaticFieldValue(fieldDef, pILFrame, &pFieldVal);
            } 
            // non-static fields on an object instance
            else if(pInnerValue != NULL)
            {
                ToRelease<ICorDebugObjectValue> pObjValue;
                if (SUCCEEDED(pInnerValue->QueryInterface(IID_ICorDebugObjectValue, (LPVOID*) &pObjValue)))
                    pObjValue->GetFieldValue(pClass, fieldDef, &pFieldVal);
            }
            // skip over non-static fields on static types
            else
            {
                continue; 
            }

            // we didn't get a value yet and there is default value available
            // need to calculate the type because there won't be a ICorDebugValue to derive it from
            if(pFieldVal == NULL && pDefaultValue != NULL)
            {
                FindTypeFromElementType(fieldDefaultValueEt, &pFieldType);
            }

            ExpressionNode* pNewChildNode = new ExpressionNode(pAbsoluteExpression, ChildKind_Field, mdName, pFieldVal, pFieldType, pILFrame, pDefaultValue, cchDefaultValue);
            AddChild(pNewChildNode);
            if(pNewChildNode->Expand(varToExpand) != S_FALSE)
                fieldExpanded = TRUE;
        }
    }
    pMD->CloseEnum(fEnum);

    // Only recurse to expand the base type if all of these hold:
    // 1) base type exists
    // 2) no field was expanded
    // 3) the non-casting portion of the varToExpand doesn't match the current expression
    //    OR the cast exists and doesn't match

    if(pBaseTypeNode == NULL) return Status;
    if(fieldExpanded) return Status;

    WCHAR* pEndCast = _wcschr(varToExpand, L')');
    WCHAR* pNonCast = (pEndCast == NULL) ? varToExpand : pEndCast+1;
    if(_wcscmp(pNonCast, pAbsoluteExpression) != 0)
    {
        pBaseTypeNode->Expand(varToExpand);
        return Status;
    }

    if(varToExpand[0] == L'(' && pEndCast != NULL)
    {
        int cchCastTypeName = ((int)(pEndCast-1)-(int)varToExpand)/2;
        PopulateType();
        if(_wcslen(pTypeName) != (cchCastTypeName) ||
            _wcsncmp(varToExpand+1, pTypeName, cchCastTypeName) != 0)
        {
            pBaseTypeNode->Expand(varToExpand);
            return Status;
        }
    }

    return Status;
}


// Value Population functions

//Helper for unwrapping values
HRESULT ExpressionNode::DereferenceAndUnboxValue(ICorDebugValue * pInputValue, ICorDebugValue** ppOutputValue, BOOL * pIsNull)
{
    HRESULT Status = S_OK;
    *ppOutputValue = NULL;
    if(pIsNull != NULL) *pIsNull = FALSE;

    ToRelease<ICorDebugReferenceValue> pReferenceValue;
    Status = pInputValue->QueryInterface(IID_ICorDebugReferenceValue, (LPVOID*) &pReferenceValue);
    if (SUCCEEDED(Status))
    {
        BOOL isNull = FALSE;
        IfFailRet(pReferenceValue->IsNull(&isNull));
        if(!isNull)
        {
            ToRelease<ICorDebugValue> pDereferencedValue;
            IfFailRet(pReferenceValue->Dereference(&pDereferencedValue));
            return DereferenceAndUnboxValue(pDereferencedValue, ppOutputValue);
        }
        else
        {
            if(pIsNull != NULL) *pIsNull = TRUE;
            *ppOutputValue = pInputValue;
            (*ppOutputValue)->AddRef();
            return S_OK;
        }
    }

    ToRelease<ICorDebugBoxValue> pBoxedValue;
    Status = pInputValue->QueryInterface(IID_ICorDebugBoxValue, (LPVOID*) &pBoxedValue);
    if (SUCCEEDED(Status))
    {
        ToRelease<ICorDebugObjectValue> pUnboxedValue;
        IfFailRet(pBoxedValue->GetObject(&pUnboxedValue));
        return DereferenceAndUnboxValue(pUnboxedValue, ppOutputValue);
    }
    *ppOutputValue = pInputValue;
    (*ppOutputValue)->AddRef();
    return S_OK;
}

// Returns TRUE if the value derives from System.Enum
BOOL ExpressionNode::IsEnum(ICorDebugValue * pInputValue)
{
    ToRelease<ICorDebugValue> pValue;
    if(FAILED(DereferenceAndUnboxValue(pInputValue, &pValue, NULL))) return FALSE;

    WCHAR baseTypeName[mdNameLen];
    ToRelease<ICorDebugValue2> pValue2;
    ToRelease<ICorDebugType> pType;
    ToRelease<ICorDebugType> pBaseType;

    if(FAILED(pValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2))) return FALSE;
    if(FAILED(pValue2->GetExactType(&pType))) return FALSE;
    if(FAILED(pType->GetBase(&pBaseType)) || pBaseType == NULL) return FALSE;
    if(FAILED(CalculateTypeName(pBaseType, baseTypeName, mdNameLen))) return  FALSE;

    return (_wcsncmp(baseTypeName, L"System.Enum", 11) == 0);
}

// Calculates the value text for nodes that have enum values
HRESULT ExpressionNode::PopulateEnumValue(ICorDebugValue* pEnumValue, BYTE* enumValue)
{
    HRESULT Status = S_OK;

    mdTypeDef currentTypeDef;
    ToRelease<ICorDebugClass> pClass;
    ToRelease<ICorDebugValue2> pValue2;
    ToRelease<ICorDebugType> pType;
    ToRelease<ICorDebugModule> pModule;
    IfFailRet(pEnumValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2));
    IfFailRet(pValue2->GetExactType(&pType));
    IfFailRet(pType->GetClass(&pClass));
    IfFailRet(pClass->GetModule(&pModule));
    IfFailRet(pClass->GetToken(&currentTypeDef));

    ToRelease<IUnknown> pMDUnknown;
    ToRelease<IMetaDataImport> pMD;
    IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
    IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));


    //First, we need to figure out the underlying enum type so that we can correctly type cast the raw values of each enum constant
    //We get that from the non-static field of the enum variable (I think the field is called __value or something similar)
    ULONG numFields = 0;
    HCORENUM fEnum = NULL;
    mdFieldDef fieldDef;
    CorElementType enumUnderlyingType = ELEMENT_TYPE_END;
    while(SUCCEEDED(pMD->EnumFields(&fEnum, currentTypeDef, &fieldDef, 1, &numFields)) && numFields != 0)
    {
        DWORD             fieldAttr = 0;
        PCCOR_SIGNATURE   pSignatureBlob = NULL;
        ULONG             sigBlobLength = 0;
        if(SUCCEEDED(pMD->GetFieldProps(fieldDef, NULL, NULL, 0, NULL, &fieldAttr, &pSignatureBlob, &sigBlobLength, NULL, NULL, NULL)))
        {
            if((fieldAttr & fdStatic) == 0)
            {
                CorSigUncompressCallingConv(pSignatureBlob);
                enumUnderlyingType = CorSigUncompressElementType(pSignatureBlob);
                break;
            }
        }
    }
    pMD->CloseEnum(fEnum);


    //Now that we know the underlying enum type, let's decode the enum variable into OR-ed, human readable enum contants
    fEnum = NULL;
    bool isFirst = true;
    ULONG64 remainingValue = *((ULONG64*)enumValue);
    WCHAR* pTextValueCursor = pTextValue;
    DWORD cchTextValueCursor = MAX_EXPRESSION;
    while(SUCCEEDED(pMD->EnumFields(&fEnum, currentTypeDef, &fieldDef, 1, &numFields)) && numFields != 0)
    {
        ULONG             nameLen = 0;
        DWORD             fieldAttr = 0;
        WCHAR             mdName[mdNameLen];
        WCHAR             typeName[mdNameLen];
        UVCP_CONSTANT     pRawValue = NULL;
        ULONG             rawValueLength = 0;
        if(SUCCEEDED(pMD->GetFieldProps(fieldDef, NULL, mdName, mdNameLen, &nameLen, &fieldAttr, NULL, NULL, NULL, &pRawValue, &rawValueLength)))
        {
            DWORD enumValueRequiredAttributes = fdPublic | fdStatic | fdLiteral | fdHasDefault;
            if((fieldAttr & enumValueRequiredAttributes) != enumValueRequiredAttributes)
                continue;

            ULONG64 currentConstValue = 0;
            switch (enumUnderlyingType)
            {
            case ELEMENT_TYPE_CHAR:
            case ELEMENT_TYPE_I1:
                currentConstValue = (ULONG64)(*((CHAR*)pRawValue));
                break;
            case ELEMENT_TYPE_U1:
                currentConstValue = (ULONG64)(*((BYTE*)pRawValue));
                break;
            case ELEMENT_TYPE_I2:
                currentConstValue = (ULONG64)(*((SHORT*)pRawValue));
                break;
            case ELEMENT_TYPE_U2:
                currentConstValue = (ULONG64)(*((USHORT*)pRawValue));
                break;
            case ELEMENT_TYPE_I4:
                currentConstValue = (ULONG64)(*((INT32*)pRawValue));
                break;
            case ELEMENT_TYPE_U4:
                currentConstValue = (ULONG64)(*((UINT32*)pRawValue));
                break;
            case ELEMENT_TYPE_I8:
                currentConstValue = (ULONG64)(*((LONG*)pRawValue));
                break;
            case ELEMENT_TYPE_U8:
                currentConstValue = (ULONG64)(*((ULONG*)pRawValue));
                break;
            case ELEMENT_TYPE_I:
                currentConstValue = (ULONG64)(*((int*)pRawValue));
                break;
            case ELEMENT_TYPE_U:
            case ELEMENT_TYPE_R4:
            case ELEMENT_TYPE_R8:
                // Technically U and the floating-point ones are options in the CLI, but not in the CLS or C#, so these are NYI
            default:
                currentConstValue = 0;
            }

            if((currentConstValue == remainingValue) || ((currentConstValue != 0) && ((currentConstValue & remainingValue) == currentConstValue)))
            {
                remainingValue &= ~currentConstValue;
                DWORD charsCopied = 0;
                if(isFirst)
                {
                    charsCopied = _snwprintf_s(pTextValueCursor, cchTextValueCursor, _TRUNCATE, L"= %s", mdName);
                    isFirst = false;
                }
                else 
                    charsCopied = _snwprintf_s(pTextValueCursor, cchTextValueCursor, _TRUNCATE, L" | %s", mdName);

                // if an error or truncation occurred, stop copying
                if(charsCopied == -1)
                {
                    cchTextValueCursor = 0;
                    pTextValueCursor = NULL;
                }
                else
                {
                    // charsCopied is the number of characters copied, not counting the terminating null
                    // this advances the cursor to point right at the terminating null so that future copies
                    // will concatenate the string
                    pTextValueCursor += charsCopied;
                    cchTextValueCursor -= charsCopied;
                }
            }
        }
    }
    pMD->CloseEnum(fEnum);

    return Status;
}

// Helper that caches the textual value for nodes that evaluate to a string object
HRESULT ExpressionNode::GetDebuggeeStringValue(ICorDebugValue* pInputValue, __inout_ecount(cchBuffer) WCHAR* wszBuffer, DWORD cchBuffer)
{
    HRESULT Status;

    ToRelease<ICorDebugStringValue> pStringValue;
    IfFailRet(pInputValue->QueryInterface(IID_ICorDebugStringValue, (LPVOID*) &pStringValue));

    ULONG32 cchValueReturned;
    IfFailRet(pStringValue->GetString(cchBuffer, &cchValueReturned, wszBuffer));

    return S_OK;
}

// Retrieves the string value for a constant
HRESULT ExpressionNode::GetConstantStringValue(__inout_ecount(cchBuffer) WCHAR* wszBuffer, DWORD cchBuffer)
{
    // The string encoded in metadata isn't null-terminated
    // so we need to copy it to a null terminated buffer
    DWORD copyLen = cchDefaultValue;
    if(copyLen > cchBuffer-1)
        copyLen = cchDefaultValue;

    wcsncpy_s(wszBuffer, cchBuffer, (WCHAR*)pDefaultValue, copyLen);
    return S_OK;
}

// Helper that caches the textual value for nodes that evaluate to array objects
HRESULT ExpressionNode::PopulateSzArrayValue(ICorDebugValue* pInputValue)
{
    HRESULT Status = S_OK;

    ToRelease<ICorDebugArrayValue> pArrayValue;
    IfFailRet(pInputValue->QueryInterface(IID_ICorDebugArrayValue, (LPVOID*) &pArrayValue));

    ULONG32 nRank;
    IfFailRet(pArrayValue->GetRank(&nRank));
    if (nRank != 1)
    {
        _snwprintf_s(pErrorMessage, MAX_EXPRESSION, _TRUNCATE, L"Multi-dimensional arrays NYI");
        return E_UNEXPECTED;
    }

    ULONG32 cElements;
    IfFailRet(pArrayValue->GetCount(&cElements));

    if (cElements == 0)
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"(empty)");
    else if (cElements == 1) 
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"(1 element)");
    else  
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"(%d elements)", cElements);

    return S_OK;
}

// Helper that caches the textual value for nodes of any type
HRESULT ExpressionNode::PopulateTextValueHelper()
{
    HRESULT Status = S_OK;

    BOOL isNull = TRUE;
    ToRelease<ICorDebugValue> pInnerValue;
    CorElementType corElemType;
    ULONG32 cbSize = 0;
    if(pValue != NULL)
    {
        IfFailRet(DereferenceAndUnboxValue(pValue, &pInnerValue, &isNull));

        if(isNull)
        {
            _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= null");
            return S_OK;
        }
        IfFailRet(pInnerValue->GetSize(&cbSize));
        IfFailRet(pInnerValue->GetType(&corElemType));
    }
    else if(pDefaultValue != NULL)
    {
        if(pTypeCast == NULL)
        {
            // this shouldn't happen, but just print nothing if it does
            return S_OK;
        }
        // This works around an irritating issue in ICorDebug. For default values
        // we have to construct the ICorDebugType ourselves, however ICorDebug
        // doesn't allow type construction using the correct element types. The
        // caller must past CLASS or VALUETYPE even when a more specific short
        // form element type is applicable. That means that later, here, we get
        // back the wrong answer. To work around this we format the type as a
        // string, and check it against all the known types. That allows us determine
        // everything except VALUETYPE/CLASS. Thankfully that distinction is the
        // one piece of data ICorDebugType will tell us if needed.
        if(FAILED(GetCanonicalElementTypeForTypeName(GetTypeName(), &corElemType)))
        {
            pTypeCast->GetType(&corElemType);
        }

        switch(corElemType)
        {
        case ELEMENT_TYPE_BOOLEAN:
        case ELEMENT_TYPE_I1:
        case ELEMENT_TYPE_U1:
            cbSize = 1;
            break;

        case ELEMENT_TYPE_CHAR:
        case ELEMENT_TYPE_I2:
        case ELEMENT_TYPE_U2:
            cbSize = 2;
            break;

        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
        case ELEMENT_TYPE_I4:
        case ELEMENT_TYPE_U4:
        case ELEMENT_TYPE_R4:
            cbSize = 4;
            break;

        case ELEMENT_TYPE_I8:
        case ELEMENT_TYPE_U8:
        case ELEMENT_TYPE_R8:
            cbSize = 8;
            break;
        }
    }

    if (corElemType == ELEMENT_TYPE_STRING)
    {
        WCHAR buffer[MAX_EXPRESSION];
        buffer[0] = L'\0';
        if(pInnerValue != NULL)
            GetDebuggeeStringValue(pInnerValue, buffer, MAX_EXPRESSION);
        else
            GetConstantStringValue(buffer, MAX_EXPRESSION);
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= \"%s\"", buffer);
    }
    else if (corElemType == ELEMENT_TYPE_SZARRAY)
    {
        return PopulateSzArrayValue(pInnerValue);
    }


    ArrayHolder<BYTE> rgbValue = new BYTE[cbSize];
    memset(rgbValue.GetPtr(), 0, cbSize * sizeof(BYTE));
    if(pInnerValue != NULL)
    {
        ToRelease<ICorDebugGenericValue> pGenericValue;
        IfFailRet(pInnerValue->QueryInterface(IID_ICorDebugGenericValue, (LPVOID*) &pGenericValue));
        IfFailRet(pGenericValue->GetValue((LPVOID) &(rgbValue[0])));
    }
    else
    {
        memcpy((LPVOID) &(rgbValue[0]), pDefaultValue, cbSize);
    }

    //TODO: this should really be calculated from the type
    if(pInnerValue != NULL && IsEnum(pInnerValue))
    {
        Status = PopulateEnumValue(pInnerValue, rgbValue);
        return Status;
    }

    switch (corElemType)
    {
    default:
        _snwprintf_s(pErrorMessage, MAX_ERROR, _TRUNCATE, L"Unhandled CorElementType: 0x%x", corElemType);
        Status = E_FAIL;
        break;

    case ELEMENT_TYPE_PTR:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"<pointer>");
        break;

    case ELEMENT_TYPE_FNPTR:
        {
            CORDB_ADDRESS addr = 0;
            ToRelease<ICorDebugReferenceValue> pReferenceValue = NULL;
            if(SUCCEEDED(pInnerValue->QueryInterface(IID_ICorDebugReferenceValue, (LPVOID*) &pReferenceValue)))
                pReferenceValue->GetValue(&addr);
            _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"<function pointer 0x%x>", addr);
        }
        break;

    case ELEMENT_TYPE_VALUETYPE:
    case ELEMENT_TYPE_CLASS:
    case ELEMENT_TYPE_OBJECT:
        ULONG64 pointer;
        if(pInnerValue != NULL && SUCCEEDED(pInnerValue->GetAddress(&pointer)))
            _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"@ 0x%p", (void *) pointer);
        break;

    case ELEMENT_TYPE_BOOLEAN:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %s", rgbValue[0] == 0 ? L"false" : L"true");
        break;

    case ELEMENT_TYPE_CHAR:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= '%C'", *(WCHAR *) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_I1:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %d", *(char*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_U1:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %d", *(unsigned char*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_I2:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %hd", *(short*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_U2:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %hu", *(unsigned short*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_I:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %d", *(int*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_U:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %u", *(unsigned int*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_I4:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %d", *(int*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_U4:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %u", *(unsigned int*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_I8:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %I64d", *(__int64*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_U8:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %I64u", *(unsigned __int64*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_R4:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"= %f", (double) *(float*) &(rgbValue[0]));
        break;

    case ELEMENT_TYPE_R8:
        _snwprintf_s(pTextValue, MAX_EXPRESSION, _TRUNCATE, L"%f", *(double*) &(rgbValue[0]));
        break;

        // TODO: The following corElementTypes are not yet implemented here.  Array
        // might be interesting to add, though the others may be of rather limited use:
        // ELEMENT_TYPE_ARRAY          = 0x14,     // MDARRAY <type> <rank> <bcount> <bound1> ... <lbcount> <lb1> ...
        // 
        // ELEMENT_TYPE_GENERICINST    = 0x15,     // GENERICINST <generic type> <argCnt> <arg1> ... <argn>
    }

    return Status;
}

// Caches the textual value of this node
HRESULT ExpressionNode::PopulateTextValue()
{
    if(pErrorMessage[0] != 0)
        return E_UNEXPECTED;
    if(pValue == NULL && pDefaultValue == NULL)
        return S_OK;
    HRESULT Status = PopulateTextValueHelper();
    if(FAILED(Status) && pErrorMessage[0] == 0)
    {
        _snwprintf_s(pErrorMessage, MAX_ERROR, _TRUNCATE, L"Error in PopulateTextValueHelper: 0x%x", Status);
    }
    return Status;
}


// Expression parsing and search

//Callback that searches a frame to determine if it contains a local variable or parameter of a given name
VOID ExpressionNode::EvaluateExpressionFrameScanCallback(ICorDebugFrame* pFrame, VOID* pUserData)
{
    EvaluateExpressionFrameScanData* pData = (EvaluateExpressionFrameScanData*)pUserData;

    // we already found what we were looking for, just continue
    if(pData->pFoundValue != NULL)
        return;

    // if any of these fail we just continue on
    // querying for ILFrame will frequently fail because many frames aren't IL
    ToRelease<ICorDebugILFrame> pILFrame;
    HRESULT Status = pFrame->QueryInterface(IID_ICorDebugILFrame, (LPVOID*) &pILFrame);
    if (FAILED(Status))
    {
        return;
    }
    // we need to save off the first frame we find regardless of whether we find the
    // local or not. We might need this frame later for static field lookup.
    if(pData->pFirstFrame == NULL)
    {
        pData->pFirstFrame = pILFrame;
        pData->pFirstFrame->AddRef();
    }
    // not all IL frames map to an assembly (ex. LCG)
    ToRelease<ICorDebugFunction> pFunction;
    Status = pFrame->GetFunction(&pFunction);
    if (FAILED(Status))
    {
        return;
    }
    // from here down shouldn't generally fail, but just in case
    mdMethodDef methodDef;
    Status = pFunction->GetToken(&methodDef);
    if (FAILED(Status))
    {
        return;
    }
    ToRelease<ICorDebugModule> pModule;
    Status = pFunction->GetModule(&pModule);
    if (FAILED(Status))
    {
        return;
    }
    ToRelease<IUnknown> pMDUnknown;
    Status = pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown);
    if (FAILED(Status))
    {
        return;
    }
    ToRelease<IMetaDataImport> pMD;
    Status = pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD);
    if (FAILED(Status))
    {
        return;
    }

    pData->pFoundFrame = pILFrame;
    pData->pFoundFrame->AddRef();
    // Enumerate all the parameters
    EnumerateParameters(pMD, methodDef, pILFrame, EvaluateExpressionVariableScanCallback, pUserData);
    // Enumerate all the locals
    EnumerateLocals(pMD, methodDef, pILFrame, EvaluateExpressionVariableScanCallback, pUserData);

    // if we didn't find it in this frame then clear the frame back out
    if(pData->pFoundValue == NULL)
    {
        pData->pFoundFrame = NULL;
    }

    return;
}

//Callback checks to see if a given local/parameter has name pName
VOID ExpressionNode::EvaluateExpressionVariableScanCallback(ICorDebugValue* pValue, __in_z WCHAR* pName, __out_z WCHAR* pErrorMessage, VOID* pUserData)
{
    EvaluateExpressionFrameScanData* pData = (EvaluateExpressionFrameScanData*)pUserData;
    if(_wcscmp(pName, pData->pIdentifier) == 0)
    {
        // found it
        pData->pFoundValue = pValue;
        pValue->AddRef();
    }
    return;
}

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
//  Valid combinations of state comming into this method:
//      The expression up to charactersParsed isn't recognized yet:
//           pParsedValue = pParsedType = pParsedDefaultValue = NULL
//           cchParsedDefaultValue = 0
//      The expression up to charactersParsed is a recognized type:
//           pParsedType = <parsed type>
//           pParsedValue = pParsedDefaultValue = NULL
//           cchParsedDefaultValue = 0
//      The expression up to charactersParsed is a recognized value in the debuggee:
//           pParsedValue = <parsed value>
//           pParsedType = pParsedDefaultValue = NULL
//           cchParsedDefaultValue = 0
//      The expression up to charactersParsed is a recognized default value stored in metadata:
//           pParsedValue = NULL
//           pParsedType = <type calculated from metadata>
//           pParsedDefaultValue = <value from metadata>
//           cchParsedDefaultValue = <size of metadata value>
//
//
// REFACTORING NOTE: This method is very similar (but not identical) to the expansion logic
//                   in ExpressionNode. The primary difference is that the nodes expand all
//                   fields/indices whereas this function only expands along a precise route.
//                   If the ExpressionNode code where enhanced to support expanding precisely
//                   large portions of this function could be disposed of. As soon as the function
//                   matched the initial name it could create an ExpressionNode and then use the
//                   ExpressionNode expansion functions to drill down to the actual node required.
//                   Also need to make sure the nodes manage lifetime ok when a parent is destroyed
//                   but a child node is still referenced.
HRESULT ExpressionNode::CreateExpressionNodeHelper(__in_z WCHAR* pExpression,
                                                          __in_z WCHAR* pExpressionParseRemainder,
                                                          DWORD charactersParsed,
                                                          ICorDebugValue* pParsedValue,
                                                          ICorDebugType* pParsedType,
                                                          UVCP_CONSTANT pParsedDefaultValue,
                                                          ULONG cchParsedDefaultValue,
                                                          ICorDebugILFrame* pFrame,
                                                          ExpressionNode** ppExpressionNode)
{
    HRESULT Status = S_OK;
    WCHAR* pExpressionCursor = pExpressionParseRemainder;
    DWORD currentCharsParsed = charactersParsed;
    WCHAR pIdentifier[mdNameLen];
    pIdentifier[0] = 0;
    BOOL isArray = FALSE;
    WCHAR pResultBuffer[MAX_EXPRESSION];

    // Get the next name from the expression string
    if(FAILED(Status = ParseNextIdentifier(&pExpressionCursor, pIdentifier, mdNameLen, pResultBuffer, MAX_EXPRESSION, &currentCharsParsed, &isArray)))
    {
        *ppExpressionNode = new ExpressionNode(pExpression, pResultBuffer);
        if(*ppExpressionNode == NULL)
            return E_OUTOFMEMORY;
        else
            return S_OK;
    }

    // we've gone as far as we need, nothing left to parse
    if(Status == S_FALSE)
    {
        ToRelease<ICorDebugValue> pValue;
        *ppExpressionNode = new ExpressionNode(pExpression, ChildKind_BaseClass, pExpression, pParsedValue, pParsedType, pFrame, pParsedDefaultValue, cchParsedDefaultValue);
        if(*ppExpressionNode == NULL)
            return E_OUTOFMEMORY;
        else
            return S_OK;
    }
    // if we are just starting and have no context then we need to search locals/parameters/type names
    else if(pParsedValue == NULL && pParsedType == NULL)
    {
        // the first identifier must be a name, not an indexing expression
        if(isArray)
        {
            *ppExpressionNode = new ExpressionNode(pExpression, L"Expression must begin with a local variable, parameter, or fully qualified type name");
            return S_OK;
        }

        // scan for root on stack
        EvaluateExpressionFrameScanData data;
        data.pIdentifier = pIdentifier;
        data.pFoundValue = NULL;
        data.pFoundFrame = NULL;
        data.pFirstFrame = NULL;
        data.pErrorMessage = pResultBuffer;
        data.cchErrorMessage = MAX_EXPRESSION;
        EnumerateFrames(EvaluateExpressionFrameScanCallback, (VOID*) &data);

        if(data.pFoundValue != NULL)
        {
            // found the root, now recurse along the expression
            return CreateExpressionNodeHelper(pExpression, pExpressionCursor, currentCharsParsed, data.pFoundValue, NULL, NULL, 0, data.pFoundFrame, ppExpressionNode);
        }

        // didn't find it - search the type table for a matching name
        WCHAR pName[MAX_EXPRESSION];
        while(true)
        {
            wcsncpy_s(pName, MAX_EXPRESSION, pExpression, currentCharsParsed);
            ToRelease<ICorDebugType> pType;
            if(SUCCEEDED(FindTypeByName(pName, &pType)))
                return CreateExpressionNodeHelper(pExpression, pExpressionCursor, currentCharsParsed, NULL, pType, NULL, 0, data.pFirstFrame, ppExpressionNode);

            if(FAILED(Status = ParseNextIdentifier(&pExpressionCursor, pIdentifier, mdNameLen, pResultBuffer, MAX_EXPRESSION, &currentCharsParsed, &isArray)))
            {
                *ppExpressionNode = new ExpressionNode(pExpression, pResultBuffer);
                return S_OK;
            }
            else if(Status == S_FALSE)
            {
                break;
            }
        }

        WCHAR errorMessage[MAX_ERROR];
        swprintf_s(errorMessage, MAX_ERROR, L"No expression prefix could not be matched to an existing type, parameter, or local");
        *ppExpressionNode = new ExpressionNode(pExpression, errorMessage);
        return S_OK;
    }

    // we've got some context from an earlier portion of the search, now just need to continue
    // by dereferencing and indexing until we reach the end of the expression

    // Figure out the type, module, and metadata from our context information
    ToRelease<ICorDebugType> pType;
    BOOL isNull = TRUE;
    ToRelease<ICorDebugValue> pInnerValue = NULL;
    if(pParsedValue != NULL)
    {
        IfFailRet(DereferenceAndUnboxValue(pParsedValue, &pInnerValue, &isNull));

        if(isNull)
        {
            WCHAR parsedExpression[MAX_EXPRESSION];
            wcsncpy_s(parsedExpression, MAX_EXPRESSION, pExpression, charactersParsed);
            WCHAR errorMessage[MAX_ERROR];
            swprintf_s(errorMessage, MAX_ERROR, L"Dereferencing \'%s\' throws NullReferenceException", parsedExpression);
            *ppExpressionNode = new ExpressionNode(pExpression, errorMessage);
            return S_OK;
        }

        ToRelease<ICorDebugValue2> pValue2;
        IfFailRet(pInnerValue->QueryInterface(IID_ICorDebugValue2, (LPVOID *) &pValue2));
        IfFailRet(pValue2->GetExactType(&pType));
        CorElementType et;
        IfFailRet(pType->GetType(&et));
        while(et == ELEMENT_TYPE_ARRAY || et == ELEMENT_TYPE_SZARRAY || et == ELEMENT_TYPE_BYREF || et == ELEMENT_TYPE_PTR)
        {
            pType->GetFirstTypeParameter(&pType);
            IfFailRet(pType->GetType(&et));
        }
    }
    else
    {
        pType = pParsedType;
        pType->AddRef();
    }
    ToRelease<ICorDebugClass> pClass;
    IfFailRet(pType->GetClass(&pClass));
    ToRelease<ICorDebugModule> pModule;
    IfFailRet(pClass->GetModule(&pModule));
    mdTypeDef currentTypeDef;
    IfFailRet(pClass->GetToken(&currentTypeDef));

    ToRelease<IUnknown> pMDUnknown;
    ToRelease<IMetaDataImport> pMD;
    IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
    IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));


    // if we are searching along and this is an array index dereference
    if(isArray)
    {
        ToRelease<ICorDebugArrayValue> pArrayValue;
        if(pInnerValue == NULL || FAILED(Status = pInnerValue->QueryInterface(IID_ICorDebugArrayValue, (LPVOID*) &pArrayValue)))
        {
            WCHAR errorMessage[MAX_ERROR];
            swprintf_s(errorMessage, MAX_ERROR, L"Index notation only supported for instances of an array type");
            *ppExpressionNode = new ExpressionNode(pExpression, errorMessage);
            return S_OK;
        }

        ULONG32 nRank;
        IfFailRet(pArrayValue->GetRank(&nRank));
        if (nRank != 1)
        {
            WCHAR errorMessage[MAX_ERROR];
            swprintf_s(errorMessage, MAX_ERROR, L"Multi-dimensional arrays NYI");
            *ppExpressionNode = new ExpressionNode(pExpression, errorMessage);
            return S_OK;
        }

        int index = -1;
        if(swscanf_s(pIdentifier, L"%d", &index) != 1)
        {
            WCHAR errorMessage[MAX_ERROR];
            swprintf_s(errorMessage, MAX_ERROR, L"Failed to parse expression, missing or invalid index expression at character %d", charactersParsed+1);
            *ppExpressionNode = new ExpressionNode(pExpression, errorMessage);
            return S_OK;
        }

        ULONG32 cElements;
        IfFailRet(pArrayValue->GetCount(&cElements));
        if(index < 0 || (ULONG32)index >= cElements)
        {
            WCHAR errorMessage[MAX_ERROR];
            swprintf_s(errorMessage, MAX_ERROR, L"Index is out of range for this array");
            *ppExpressionNode = new ExpressionNode(pExpression, errorMessage);
            return S_OK;
        }

        ToRelease<ICorDebugValue> pElementValue;
        IfFailRet(pArrayValue->GetElementAtPosition(index, &pElementValue));
        return CreateExpressionNodeHelper(pExpression, pExpressionCursor, currentCharsParsed, pElementValue, NULL, NULL, 0, pFrame, ppExpressionNode);
    }
    // if we are searching along and this is field dereference
    else
    {
        ToRelease<ICorDebugType> pBaseType = pType;
        pBaseType->AddRef();

        while(pBaseType != NULL)
        {
            // get the current base type class/token/MD
            ToRelease<ICorDebugClass> pBaseClass;
            IfFailRet(pBaseType->GetClass(&pBaseClass));
            ToRelease<ICorDebugModule> pBaseTypeModule;
            IfFailRet(pBaseClass->GetModule(&pBaseTypeModule));
            mdTypeDef baseTypeDef;
            IfFailRet(pBaseClass->GetToken(&baseTypeDef));
            ToRelease<IUnknown> pBaseTypeMDUnknown;
            ToRelease<IMetaDataImport> pBaseTypeMD;
            IfFailRet(pBaseTypeModule->GetMetaDataInterface(IID_IMetaDataImport, &pBaseTypeMDUnknown));
            IfFailRet(pBaseTypeMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pBaseTypeMD));


            // iterate through all fields at this level of the class hierarchy
            ULONG numFields = 0;
            HCORENUM fEnum = NULL;
            mdFieldDef fieldDef;
            while(SUCCEEDED(pMD->EnumFields(&fEnum, baseTypeDef, &fieldDef, 1, &numFields)) && numFields != 0)
            {
                ULONG             nameLen = 0;
                DWORD             fieldAttr = 0;
                WCHAR             mdName[mdNameLen];
                WCHAR             typeName[mdNameLen];
                CorElementType    fieldDefaultValueEt;
                UVCP_CONSTANT     pDefaultValue;
                ULONG             cchDefaultValue;
                if(SUCCEEDED(pBaseTypeMD->GetFieldProps(fieldDef, NULL, mdName, mdNameLen, &nameLen, &fieldAttr, NULL, NULL, (DWORD*)&fieldDefaultValueEt, &pDefaultValue, &cchDefaultValue)) &&
                    _wcscmp(mdName, pIdentifier) == 0)
                {
                    ToRelease<ICorDebugType> pFieldValType = NULL;
                    ToRelease<ICorDebugValue> pFieldVal;
                    if (fieldAttr & fdStatic)
                        pBaseType->GetStaticFieldValue(fieldDef, pFrame, &pFieldVal);
                    else if(pInnerValue != NULL)
                    {
                        ToRelease<ICorDebugObjectValue> pObjValue;
                        if (SUCCEEDED(pInnerValue->QueryInterface(IID_ICorDebugObjectValue, (LPVOID*) &pObjValue)))
                            pObjValue->GetFieldValue(pBaseClass, fieldDef, &pFieldVal);
                    }

                    // we didn't get a value yet and there is default value available
                    // need to calculate the type because there won't be a ICorDebugValue to derive it from
                    if(pFieldVal == NULL && pDefaultValue != NULL)
                    {
                        FindTypeFromElementType(fieldDefaultValueEt, &pFieldValType);
                    }
                    else
                    {
                        // if we aren't using default value, make sure it is cleared out
                        pDefaultValue = NULL;
                        cchDefaultValue = 0;
                    }

                    // if we still don't have a value, check if we are trying to get an instance field from a static type
                    if(pInnerValue == NULL && pFieldVal == NULL && pDefaultValue == NULL)
                    {
                        WCHAR pObjectTypeName[MAX_EXPRESSION];
                        CalculateTypeName(pBaseType, pObjectTypeName, MAX_EXPRESSION);
                        WCHAR errorMessage[MAX_ERROR];
                        swprintf_s(errorMessage, MAX_ERROR, L"Can not evaluate instance field \'%s\' from static type \'%s\'", pIdentifier, pObjectTypeName);
                        *ppExpressionNode = new ExpressionNode(pExpression, errorMessage);
                        return S_OK;
                    }
                    return CreateExpressionNodeHelper(pExpression, pExpressionCursor, currentCharsParsed, pFieldVal, pFieldValType, pDefaultValue, cchDefaultValue, pFrame, ppExpressionNode);
                }
            }

            //advance to next base type
            ICorDebugType* pTemp = NULL;
            pBaseType->GetBase(&pTemp);
            pBaseType = pTemp;
        }

        WCHAR pObjectTypeName[MAX_EXPRESSION];
        CalculateTypeName(pType, pObjectTypeName, MAX_EXPRESSION);
        WCHAR errorMessage[MAX_ERROR];
        swprintf_s(errorMessage, MAX_ERROR, L"Field \'%s\' does not exist in type \'%s\'", pIdentifier, pObjectTypeName);
        *ppExpressionNode = new ExpressionNode(pExpression, errorMessage);
        return S_OK;
    }

    return Status;
}

// Splits apart a C#-like expression and determines the first identifier in the string and updates expression to point
// at the remaining unparsed portion
HRESULT ExpressionNode::ParseNextIdentifier(__in_z WCHAR** expression, __inout_ecount(cchIdentifierName) WCHAR* identifierName, DWORD cchIdentifierName, __inout_ecount(cchErrorMessage) WCHAR* errorMessage, DWORD cchErrorMessage, DWORD* charactersParsed, BOOL* isArrayIndex)
{

    // This algorithm is best understood as a two stage process. The first stage splits
    // the expression into two chunks an identifier and a remaining expression. The second stage
    // normalizes the identifier. The splitting algorithm doesn't care if identifiers are well-formed
    // at all, we do some error checking in the 2nd stage though. For the splitting stage, an identifier is
    // any first character, followed by as many characters as possible that aren't a '.' or a '['.
    // In the 2nd stage any '.' character is removed from the front (the only place it could be)
    // and enclosing braces are removed. An error is recorded if the identifier ends 0 length or the
    // opening bracket isn't matched. Here is an example showing how we would parse an expression
    // which is deliberately not very well formed. Each line is the result of calling this function once... it
    // takes many calls to break the entire expression down.
    //
    // expression              1st stage identifier   2nd stage identifier
    // foo.bar[18..f[1.][2][[ 
    // .bar[18..f[1.][2][[     foo                    foo
    // [18..f[1.][2][[         .bar                   bar
    // ..f[1.][2][[            [18                    error no ]
    // .f[1.][2][[             .                      error 0-length 
    // [1.][2][[               .f                     f
    // .][2][[                 [1                     error no ]
    // [2][[                   .]                     ]  (we don't error check legal CLI identifier name characters)
    // [[                      [2]                    2
    // [                       [                      error no ]
    //                         [                      error no ]

    // not an error, just the end of the expression
    if(*expression == NULL || **expression == 0)
        return S_FALSE;

    WCHAR* expressionStart = *expression;
    DWORD currentCharsParsed = *charactersParsed;
    DWORD identifierLen = (DWORD) _wcscspn(expressionStart, L".[");
    // if the first character was a . or [ skip over it. Note that we don't
    // do this always in case the first WCHAR was part of a surrogate pair
    if(identifierLen == 0)
    {
        identifierLen = (DWORD) _wcscspn(expressionStart+1, L".[") + 1;
    }

    *expression += identifierLen;
    *charactersParsed += identifierLen;

    // done with the first stage splitting, on to 2nd stage

    // a . should be followed by field name
    if(*expressionStart == L'.')
    {
        if(identifierLen == 1) // 0-length after .
        {
            swprintf_s(errorMessage, cchErrorMessage, L"Failed to parse expression, missing name after character %d", currentCharsParsed+1);
            return E_FAIL;
        }
        if(identifierLen-1 >= cchIdentifierName)
        {
            swprintf_s(errorMessage, cchErrorMessage, L"Failed to parse expression, name at character %d is too long", currentCharsParsed+2);
            return E_FAIL;
        }
        *isArrayIndex = FALSE;
        wcsncpy_s(identifierName, cchIdentifierName, expressionStart+1, identifierLen-1);
        return S_OK;
    }
    // an open bracket should be followed by a decimal value and then a closing bracket
    else if(*expressionStart == L'[')
    {
        if(*(expressionStart+identifierLen-1) != L']')
        {
            swprintf_s(errorMessage, cchErrorMessage, L"Failed to parse expression, missing or invalid index expression at character %d", currentCharsParsed+1);
            return E_FAIL;
        }
        if(identifierLen <= 2) // 0-length between []
        {
            swprintf_s(errorMessage, cchErrorMessage, L"Failed to parse expression, missing index after character %d", currentCharsParsed+1);
            return E_FAIL;
        }
        if(identifierLen-2 >= cchIdentifierName)
        {
            swprintf_s(errorMessage, cchErrorMessage, L"Failed to parse expression, index at character %d is too large", currentCharsParsed+2);
            return E_FAIL;
        }
        *isArrayIndex = TRUE;
        wcsncpy_s(identifierName, cchIdentifierName, expressionStart+1, identifierLen-2);
        return S_OK;
    }
    else // no '.' or '[', this is an initial name
    {
        if(identifierLen == 0) // 0-length
        {
            swprintf_s(errorMessage, cchErrorMessage, L"Failed to parse expression, missing name after character %d", currentCharsParsed+1);
            return E_FAIL;
        }
        if(identifierLen >= cchIdentifierName)
        {
            swprintf_s(errorMessage, cchErrorMessage, L"Failed to parse expression, name at character %d is too long", currentCharsParsed+1);
            return E_FAIL;
        }
        *isArrayIndex = FALSE;
        wcsncpy_s(identifierName, cchIdentifierName, expressionStart, identifierLen);
        return S_OK;
    }
}


// Iterate through all parameters in the ILFrame calling the callback function for each of them
HRESULT ExpressionNode::EnumerateParameters(IMetaDataImport * pMD,
                                                   mdMethodDef methodDef,
                                                   ICorDebugILFrame * pILFrame,
                                                   VariableEnumCallback pCallback,
                                                   VOID* pUserData)
{
    HRESULT Status = S_OK;

    ULONG cParams = 0;
    ToRelease<ICorDebugValueEnum> pParamEnum;
    IfFailRet(pILFrame->EnumerateArguments(&pParamEnum));
    IfFailRet(pParamEnum->GetCount(&cParams));
    DWORD methAttr = 0;
    IfFailRet(pMD->GetMethodProps(methodDef, NULL, NULL, 0, NULL, &methAttr, NULL, NULL, NULL, NULL));
    for (ULONG i=0; i < cParams; i++)
    {
        ULONG paramNameLen = 0;
        mdParamDef paramDef;
        WCHAR paramName[mdNameLen] = L"\0";

        if(i == 0 && (methAttr & mdStatic) == 0)
            swprintf_s(paramName, mdNameLen, L"this\0");
        else 
        {
            int idx = ((methAttr & mdStatic) == 0)? i : (i + 1);
            if(SUCCEEDED(pMD->GetParamForMethodIndex(methodDef, idx, &paramDef)))
                pMD->GetParamProps(paramDef, NULL, NULL, paramName, mdNameLen, &paramNameLen, NULL, NULL, NULL, NULL);
        }
        if(_wcslen(paramName) == 0)
            swprintf_s(paramName, mdNameLen, L"param_%d\0", i);

        ToRelease<ICorDebugValue> pValue;
        ULONG cArgsFetched;
        WCHAR pErrorMessage[MAX_ERROR] = L"\0";
        HRESULT hr = pParamEnum->Next(1, &pValue, &cArgsFetched);
        if (FAILED(hr))
        {
            swprintf_s(pErrorMessage, MAX_ERROR, L"  + (Error 0x%x retrieving parameter '%S')\n", hr, paramName);
        }
        if (hr == S_FALSE)
        {
            break;
        }
        pCallback(pValue, paramName, pErrorMessage, pUserData);
    }

    return Status;
}

// Enumerate all locals in the given ILFrame, calling the callback method for each of them
HRESULT ExpressionNode::EnumerateLocals(IMetaDataImport * pMD,
                                               mdMethodDef methodDef,
                                               ICorDebugILFrame * pILFrame,
                                               VariableEnumCallback pCallback,
                                               VOID* pUserData)
{
    HRESULT Status = S_OK;
    ULONG cLocals = 0;
    ToRelease<ICorDebugFunction> pFunction;
    ToRelease<ICorDebugModule> pModule;
    if(SUCCEEDED(pILFrame->GetFunction(&pFunction)))
    {
        IfFailRet(pFunction->GetModule(&pModule));
    }
    ToRelease<ICorDebugValueEnum> pLocalsEnum;
    IfFailRet(pILFrame->EnumerateLocalVariables(&pLocalsEnum));
    IfFailRet(pLocalsEnum->GetCount(&cLocals));
    if (cLocals > 0)
    {
        SymbolReader symReader;
        bool symbolsAvailable = false;
        if(pModule != NULL && SUCCEEDED(symReader.LoadSymbols(pMD, pModule)))
            symbolsAvailable = true;

        for (ULONG i=0; i < cLocals; i++)
        {
            ULONG paramNameLen = 0;
            WCHAR paramName[mdNameLen] = L"\0";
            WCHAR pErrorMessage[MAX_ERROR] = L"\0";
            ToRelease<ICorDebugValue> pValue;
            HRESULT hr = S_OK;
            if(symbolsAvailable)
                hr = symReader.GetNamedLocalVariable(pILFrame, i, paramName, mdNameLen, &pValue);
            else
            {
                ULONG cArgsFetched;
                hr = pLocalsEnum->Next(1, &pValue, &cArgsFetched);
            }
            if(_wcslen(paramName) == 0)
                swprintf_s(paramName, mdNameLen, L"local_%d\0", i);

            if (FAILED(hr))
            {
                swprintf_s(pErrorMessage, MAX_ERROR, L"  + (Error 0x%x retrieving local variable '%S')\n", hr, paramName);
            }
            else if (hr == S_FALSE)
            {
                break;
            }
            pCallback(pValue, paramName, pErrorMessage, pUserData);
        }
    }

    return Status;
}

// Iterates over all frames on the current thread's stack, calling the callback function for each of them
HRESULT ExpressionNode::EnumerateFrames(FrameEnumCallback pCallback, VOID* pUserData)
{
    HRESULT Status = S_OK;
    ToRelease<ICorDebugThread> pThread;
    ToRelease<ICorDebugThread3> pThread3;
    ToRelease<ICorDebugStackWalk> pStackWalk;
    ULONG ulThreadID = 0;
    g_ExtSystem->GetCurrentThreadSystemId(&ulThreadID);

    IfFailRet(g_pCorDebugProcess->GetThread(ulThreadID, &pThread));
    IfFailRet(pThread->QueryInterface(IID_ICorDebugThread3, (LPVOID *) &pThread3));
    IfFailRet(pThread3->CreateStackWalk(&pStackWalk));

    InternalFrameManager internalFrameManager;
    IfFailRet(internalFrameManager.Init(pThread3));

    int currentFrame = -1;

    for (Status = S_OK; ; Status = pStackWalk->Next())
    {
        currentFrame++;

        if (Status == CORDBG_S_AT_END_OF_STACK)
        {
            break;
        }
        IfFailRet(Status);

        if (IsInterrupt())
        {
            ExtOut("<interrupted>\n");
            break;
        }

        CROSS_PLATFORM_CONTEXT context;
        ULONG32 cbContextActual;
        if ((Status=pStackWalk->GetContext(
            DT_CONTEXT_FULL, 
            sizeof(context),
            &cbContextActual,
            (BYTE *)&context))!=S_OK)
        {
            ExtOut("GetFrameContext failed: %lx\n",Status);
            break;
        }

        ToRelease<ICorDebugFrame> pFrame;
        IfFailRet(pStackWalk->GetFrame(&pFrame));
        if (Status == S_FALSE)
        {
            Status = S_OK;
            continue;
        }

        pCallback(pFrame, pUserData);
    }

    return Status;
}

// Determines the corresponding ICorDebugType for a given primitive type
HRESULT ExpressionNode::FindTypeFromElementType(CorElementType et, ICorDebugType** ppType)
{
    HRESULT Status;
    switch (et)
    {
    default:
        Status = E_FAIL;
        break;

    case ELEMENT_TYPE_BOOLEAN:
        Status = FindTypeByName(L"System.Boolean", ppType);
        break;

    case ELEMENT_TYPE_CHAR:
        Status = FindTypeByName(L"System.Char", ppType);
        break;

    case ELEMENT_TYPE_I1:
        Status = FindTypeByName(L"System.SByte", ppType);
        break;

    case ELEMENT_TYPE_U1:
        Status = FindTypeByName(L"System.Byte", ppType);
        break;

    case ELEMENT_TYPE_I2:
        Status = FindTypeByName(L"System.Short", ppType);
        break;

    case ELEMENT_TYPE_U2:
        Status = FindTypeByName(L"System.UShort", ppType);
        break;

    case ELEMENT_TYPE_I:
        Status = FindTypeByName(L"System.Int32", ppType);
        break;

    case ELEMENT_TYPE_U:
        Status = FindTypeByName(L"System.UInt32", ppType);
        break;

    case ELEMENT_TYPE_I4:
        Status = FindTypeByName(L"System.Int32", ppType);
        break;

    case ELEMENT_TYPE_U4:
        Status = FindTypeByName(L"System.UInt32", ppType);
        break;

    case ELEMENT_TYPE_I8:
        Status = FindTypeByName(L"System.Int64", ppType);
        break;

    case ELEMENT_TYPE_U8:
        Status = FindTypeByName(L"System.UInt64", ppType);
        break;

    case ELEMENT_TYPE_R4:
        Status = FindTypeByName(L"System.Single", ppType);
        break;

    case ELEMENT_TYPE_R8:
        Status = FindTypeByName(L"System.Double", ppType);
        break;

    case ELEMENT_TYPE_OBJECT:
        Status = FindTypeByName(L"System.Object", ppType);
        break;

    case ELEMENT_TYPE_STRING:
        Status = FindTypeByName(L"System.String", ppType);
        break;
    }
    return Status;
}

// Gets the appropriate element type encoding for well-known fully qualified type names
// This doesn't work for arbitrary types, just types that have CorElementType short forms.
HRESULT ExpressionNode::GetCanonicalElementTypeForTypeName(__in_z WCHAR* pTypeName, CorElementType *et)
{
    //Sadly ICorDebug deliberately prevents creating ICorDebugType instances
    //that use canonical short form element types... seems like an issue to me.

    if(_wcscmp(pTypeName, L"System.String")==0)
    {
        *et = ELEMENT_TYPE_STRING;
    }
    else if(_wcscmp(pTypeName, L"System.Object")==0)
    {
        *et = ELEMENT_TYPE_OBJECT;
    }
    else if(_wcscmp(pTypeName, L"System.Void")==0)
    {
        *et = ELEMENT_TYPE_VOID;
    }
    else if(_wcscmp(pTypeName, L"System.Boolean")==0)
    {
        *et = ELEMENT_TYPE_BOOLEAN;
    }
    else if(_wcscmp(pTypeName, L"System.Char")==0)
    {
        *et = ELEMENT_TYPE_CHAR;
    }
    else if(_wcscmp(pTypeName, L"System.Byte")==0)
    {
        *et = ELEMENT_TYPE_U1;
    }
    else if(_wcscmp(pTypeName, L"System.Sbyte")==0)
    {
        *et = ELEMENT_TYPE_I1;
    }
    else if(_wcscmp(pTypeName, L"System.Int16")==0)
    {
        *et = ELEMENT_TYPE_I2;
    }
    else if(_wcscmp(pTypeName, L"System.UInt16")==0)
    {
        *et = ELEMENT_TYPE_U2;
    }
    else if(_wcscmp(pTypeName, L"System.UInt32")==0)
    {
        *et = ELEMENT_TYPE_U4;
    }
    else if(_wcscmp(pTypeName, L"System.Int32")==0)
    {
        *et = ELEMENT_TYPE_I4;
    }
    else if(_wcscmp(pTypeName, L"System.UInt64")==0)
    {
        *et = ELEMENT_TYPE_U8;
    }
    else if(_wcscmp(pTypeName, L"System.Int64")==0)
    {
        *et = ELEMENT_TYPE_I8;
    }
    else if(_wcscmp(pTypeName, L"System.Single")==0)
    {
        *et = ELEMENT_TYPE_R4;
    }
    else if(_wcscmp(pTypeName, L"System.Double")==0)
    {
        *et = ELEMENT_TYPE_R8;
    }
    else if(_wcscmp(pTypeName, L"System.IntPtr")==0)
    {
        *et = ELEMENT_TYPE_U;
    }
    else if(_wcscmp(pTypeName, L"System.UIntPtr")==0)
    {
        *et = ELEMENT_TYPE_I;
    }
    else if(_wcscmp(pTypeName, L"System.TypedReference")==0)
    {
        *et = ELEMENT_TYPE_TYPEDBYREF;
    }
    else 
    {
        return E_FAIL; // can't tell from a name whether it should be valuetype or class
    }
    return S_OK;
}

// Searches the debuggee for any ICorDebugType that matches the given fully qualified name
// This will search across all AppDomains and Assemblies
HRESULT ExpressionNode::FindTypeByName(__in_z  WCHAR* pTypeName, ICorDebugType** ppType)
{
    HRESULT Status = S_OK;
    ToRelease<ICorDebugAppDomainEnum> pAppDomainEnum;
    IfFailRet(g_pCorDebugProcess->EnumerateAppDomains(&pAppDomainEnum));
    DWORD count;
    IfFailRet(pAppDomainEnum->GetCount(&count));
    for(DWORD i = 0; i < count; i++)
    {
        ToRelease<ICorDebugAppDomain> pAppDomain;
        DWORD countFetched = 0;
        IfFailRet(pAppDomainEnum->Next(1, &pAppDomain, &countFetched));
        Status = FindTypeByName(pAppDomain, pTypeName, ppType);
        if(SUCCEEDED(Status))
            break;
    }

    return Status;
}

// Searches the debuggee for any ICorDebugType that matches the given fully qualified name
// This will search across all Assemblies in the given AppDomain
HRESULT ExpressionNode::FindTypeByName(ICorDebugAppDomain* pAppDomain, __in_z WCHAR* pTypeName, ICorDebugType** ppType)
{
    HRESULT Status = S_OK;
    ToRelease<ICorDebugAssemblyEnum> pAssemblyEnum;
    IfFailRet(pAppDomain->EnumerateAssemblies(&pAssemblyEnum));
    DWORD count;
    IfFailRet(pAssemblyEnum->GetCount(&count));
    for(DWORD i = 0; i < count; i++)
    {
        ToRelease<ICorDebugAssembly> pAssembly;
        DWORD countFetched = 0;
        IfFailRet(pAssemblyEnum->Next(1, &pAssembly, &countFetched));
        Status = FindTypeByName(pAssembly, pTypeName, ppType);
        if(SUCCEEDED(Status))
            break;
    }

    return Status;
}

// Searches the assembly for any ICorDebugType that matches the given fully qualified name
HRESULT ExpressionNode::FindTypeByName(ICorDebugAssembly* pAssembly, __in_z WCHAR* pTypeName, ICorDebugType** ppType)
{
    HRESULT Status = S_OK;
    ToRelease<ICorDebugModuleEnum> pModuleEnum;
    IfFailRet(pAssembly->EnumerateModules(&pModuleEnum));
    DWORD count;
    IfFailRet(pModuleEnum->GetCount(&count));
    for(DWORD i = 0; i < count; i++)
    {
        ToRelease<ICorDebugModule> pModule;
        DWORD countFetched = 0;
        IfFailRet(pModuleEnum->Next(1, &pModule, &countFetched));
        Status = FindTypeByName(pModule, pTypeName, ppType);
        if(SUCCEEDED(Status))
            break;
    }

    return Status;
}

// Searches a given module for any ICorDebugType that matches the given fully qualified type name
HRESULT ExpressionNode::FindTypeByName(ICorDebugModule* pModule, __in_z WCHAR* pTypeName, ICorDebugType** ppType)
{
    HRESULT Status = S_OK;
    ToRelease<IUnknown> pMDUnknown;
    ToRelease<IMetaDataImport> pMD;
    IfFailRet(pModule->GetMetaDataInterface(IID_IMetaDataImport, &pMDUnknown));
    IfFailRet(pMDUnknown->QueryInterface(IID_IMetaDataImport, (LPVOID*) &pMD));

    // If the name contains a generic argument list, extract the type name from
    // before the list
    WCHAR rootName[mdNameLen];
    WCHAR* pRootName = NULL;
    int typeNameLen = (int) _wcslen(pTypeName);
    int genericParamListStart = (int) _wcscspn(pTypeName, L"<");
    if(genericParamListStart != typeNameLen)
    {
        if(pTypeName[typeNameLen-1] != L'>' || genericParamListStart > mdNameLen)
        {
            return E_FAIL; // mal-formed type name
        }
        else
        {
            wcsncpy_s(rootName, mdNameLen, pTypeName, genericParamListStart);
            pRootName = rootName;
        }
    }
    else
    {
        pRootName = pTypeName;
    }

    // Convert from name to token to ICorDebugClass
    mdTypeDef typeDef;
    IfFailRet(pMD->FindTypeDefByName(pRootName, NULL, &typeDef));
    DWORD flags;
    ULONG nameLen;
    mdToken tkExtends;
    IfFailRet(pMD->GetTypeDefProps(typeDef, NULL, 0, &nameLen, &flags, &tkExtends));
    BOOL isValueType;
    IfFailRet(IsTokenValueTypeOrEnum(tkExtends, pMD, &isValueType));
    CorElementType et = isValueType ? ELEMENT_TYPE_VALUETYPE : ELEMENT_TYPE_CLASS;
    ToRelease<ICorDebugClass> pClass;
    IfFailRet(pModule->GetClassFromToken(typeDef, &pClass));
    ToRelease<ICorDebugClass2> pClass2;
    IfFailRet(pClass->QueryInterface(__uuidof(ICorDebugClass2), (void**)&pClass2));

    // Convert from class to type - if generic then recursively resolve the generic
    // parameter list
    ArrayHolder<ToRelease<ICorDebugType>> typeParams = NULL;
    int countTypeParams = 0;
    if(genericParamListStart != typeNameLen)
    {
        ToRelease<ICorDebugAssembly> pAssembly;
        IfFailRet(pModule->GetAssembly(&pAssembly));
        ToRelease<ICorDebugAppDomain> pDomain;
        IfFailRet(pAssembly->GetAppDomain(&pDomain));

        countTypeParams = 1;
        for(int i = genericParamListStart+1; i < typeNameLen; i++)
        {
            if(pTypeName[i] == L',') countTypeParams++;
        }
        typeParams = new ToRelease<ICorDebugType>[countTypeParams];

        WCHAR* pCurName = pTypeName + genericParamListStart+1;
        for(int i = 0; i < countTypeParams; i++)
        {
            WCHAR typeParamName[mdNameLen];
            WCHAR* pNextComma = _wcschr(pCurName, L',');
            int len = (pNextComma != NULL) ? (int)(pNextComma - pCurName) : (int)_wcslen(pCurName)-1;
            if(len > mdNameLen)
                return E_FAIL;
            wcsncpy_s(typeParamName, mdNameLen, pCurName, len);
            FindTypeByName(pDomain, typeParamName, &(typeParams[i]));
            pCurName = pNextComma+1;
        }
    }
    IfFailRet(pClass2->GetParameterizedType(et, countTypeParams, &(typeParams[0]), ppType));

    return Status;
}

// Checks whether the given token is or refers to type System.ValueType or System.Enum
HRESULT ExpressionNode::IsTokenValueTypeOrEnum(mdToken token, IMetaDataImport* pMetadata, BOOL* pResult)
{
    // This isn't a 100% correct check because we aren't verifying the module portion of the
    // type identity. Arbitrary assemblies could define a type named System.ValueType or System.Enum.
    // If that happens this code will get the answer wrong... we just assume that happens so rarely
    // that it isn't worth doing all the overhead of assembly resolution to deal with

    HRESULT Status = S_OK;
    CorTokenType type = (CorTokenType)(token & 0xFF000000);

    // only need enough space to hold either System.ValueType or System.Enum
    //System.ValueType -> 16 characters
    //System.Enum -> 11 characters
    WCHAR nameBuffer[17];
    nameBuffer[0] = L'\0';

    if(type == mdtTypeRef)
    {
        ULONG chTypeDef;
        pMetadata->GetTypeRefProps(token, NULL, NULL, 0, &chTypeDef);
        if(chTypeDef > _countof(nameBuffer))
        {
            *pResult = FALSE;
            return Status;
        }
        IfFailRet(pMetadata->GetTypeRefProps(token, NULL, nameBuffer, _countof(nameBuffer), &chTypeDef));
    }
    else if(type == mdtTypeDef)
    {
        ULONG chTypeDef;
        pMetadata->GetTypeDefProps(token, NULL, 0, &chTypeDef, NULL, NULL);
        if(chTypeDef > _countof(nameBuffer))
        {
            *pResult = FALSE;
            return Status;
        }
        IfFailRet(pMetadata->GetTypeDefProps(token, nameBuffer, _countof(nameBuffer), &chTypeDef, NULL, NULL));
    }

    if(_wcscmp(nameBuffer, L"System.ValueType") == 0 ||
        _wcscmp(nameBuffer, L"System.Enum") == 0)
    {
        *pResult = TRUE;
    }
    else
    {
        *pResult = FALSE;
    }
    return Status;
}
