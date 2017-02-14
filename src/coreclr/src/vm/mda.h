// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _MDA_
#define _MDA_

#ifndef _DEBUG
#ifdef DACCESS_COMPILE
#undef MDA_SUPPORTED
#endif
#endif

#ifdef MDA_SUPPORTED

#include "sarray.h"
#include "eeconfig.h"
// Factory includes
#include <xmlparser.h>
#include <objbase.h>
#include "unknwn.h"
#include "crst.h"
#include "../xmlparser/_reference.h"
#include "../dlls/mscorrc/resource.h"

#define MdaTypeOf(TYPE) ((TYPE*)0)
#define MdaType(TYPE) (TYPE*)
#define MdaElemDecl(NAME) MdaElemDecl##NAME
#define MdaElemDef(NAME) MdaElemDef##NAME
#define MdaAttrDecl(NAME) MdaAttrDecl##NAME

#define MDA_TARGET_NAMESPACE W("http://schemas.microsoft.com/CLR/2004/10/mda")
#define MDA_SCHEMA_PREFIX W("mda")


class ManagedDebuggingAssistants;
class MdaAssistant;
class MdaInvalidConfigFile;
class MdaXmlElement;
class MdaXmlAttribute;
class MdaXmlMessage;
class MdaXmlIndustry;
class MdaXPath;
class MdaSchema;
class MdaSchemaSchema;
class MdaAssistantSchema;
class MdaAssistantMsgSchema;
class MdaXmlValidationError;
class MdaFramework;
template<typename> class MdaFactory;

#define MDA_BUFFER_SIZE 256
#define MDA_XML_NAME_SIZE 16
#define MDA_XML_VALUE_SIZE 16
#define MDA_XML_ELEMENT_CHILDREN 16
#define MDA_XML_ELEMENT_ATTRIBUTES 16
#define MDA_MAX_FACTORY_PRODUCT 20
#define MDA_MAX_STACK_ELEMENTS 20
#define MDA_MAX_STACK_ATTRIBUTES 20

typedef enum 
{
    MdaSchemaPrimitiveBOOL,
    MdaSchemaPrimitiveSString,
    MdaSchemaPrimitiveINT32,
    MdaSchemaPrimitiveUnknown,
} MdaSchemaPrimitive;

// HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PoolTag
// Hex\Text value

// HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PoolTagOverruns
// 0x0 == Verify Start, 0x1 == VerifyEnd    

#define GFLAG_REG_KEY_PATH W("SYSTEM\\CurrentControlSet\\Control\\Session Manager")
#define GFLAG_REG_KEY_NAME W("GlobalFlag")
#define MDA_REG_KEY_PATH FRAMEWORK_REGISTRY_KEY_W 
#define MDA_REG_KEY_ENABLE W("MdaEnable")
#define MDA_CONFIG_ENV_VARIABLE W("MDA_CONFIG")

extern const bool g_mdaAssistantIsSwitch[];

typedef enum : BYTE
{
#define MDA_DEFINE_ASSISTANT_ENUMERATION
#include "mdaschema.inl"
#undef MDA_DEFINE_ASSISTANT_ENUMERATION
    MdaElemDef(AssistantMax),
    
#define MDA_ELEMENT_DEFINITION_ENUMERATION
#include "mdaschema.inl"
#undef MDA_ELEMENT_DEFINITION_ENUMERATION
    MdaElemDef(Max),
    
#define MDA_ELEMENT_DECLARAION_ENUMERATION
#include "mdaschema.inl"
#undef MDA_ELEMENT_DECLARAION_ENUMERATION
    MdaElemDecl(Max),
    MdaElemComment,    
    MdaElemUndefined,
    MdaElemEnd,
} MdaElemDeclDef;

typedef enum 
{
#define MDA_ATTRIBUTE_DECLARATION_ENUMERATION
#include "mdaschema.inl"
#undef  MDA_ATTRIBUTE_DECLARATION_ENUMERATION
    MdaAttrDecl(Max),
    
    MdaAttrUndefined,
    MdaAttrEnd,
} MdaAttrDeclDef;

typedef const SString CSString;

#pragma warning(push)
#pragma warning(disable:4324)

//
// MdaStack
//
template<typename TYPE>
class MdaStack
{
private:  // MdaStack not for general use. //
    MdaStack() : m_depth(0) { LIMITED_METHOD_CONTRACT; } 
    
public:  // MdaStack not for general use. //
    void Set(MdaStack<TYPE>* pStack) { WRAPPER_NO_CONTRACT; m_stack.Set(pStack->m_stack); m_depth = pStack->m_depth; }
    TYPE Push(TYPE element) { WRAPPER_NO_CONTRACT; *m_stack.Append() = element; m_depth++; return Tos(); }
    TYPE Push() { WRAPPER_NO_CONTRACT; *m_stack.Append(); m_depth++; return Tos(); }
    TYPE Pop() { WRAPPER_NO_CONTRACT; PRECONDITION(GetDepth() > 0); TYPE tos = Tos(); m_stack.Delete(m_stack.End() - 1); m_depth--; return tos; }
    TYPE Tos() { WRAPPER_NO_CONTRACT; return m_stack.End()[-1]; }
    void Clear() { WRAPPER_NO_CONTRACT; while(GetDepth()) Pop(); }
    COUNT_T GetDepth() { WRAPPER_NO_CONTRACT; return m_depth; }
    
private:
    friend class MdaConfigFactory;
    friend class ManagedDebuggingAssistants;
    friend class MdaSchema;
    friend class MdaXPath;
    
private:
    INT32 m_depth;
    InlineSArray<TYPE, 16> m_stack;    
};


//
// MdaHashtable
//
BOOL MdaLockOwner(LPVOID);

template<typename TYPE>
class MdaHashtable
{
private:  // MdaHashtable not for general use. //
    MdaHashtable() { WRAPPER_NO_CONTRACT; LockOwner lockOwner = {NULL, MdaLockOwner}; m_ht.Init(11, &lockOwner); }
    
public:  // MdaHashtable not for general use. //
    TYPE Get(LPCWSTR pKey) { WRAPPER_NO_CONTRACT; StackSString sKey(pKey); return Get(&sKey); }    
    BOOL Get(LPCWSTR pKey, TYPE* pValue) { WRAPPER_NO_CONTRACT; StackSString sKey(pKey); return Get(&sKey, pValue); }    
    BOOL HasKey(LPCWSTR pKey) { TYPE value; return Get(pKey, &value); }
    TYPE Get(CSString* pKey) 
    { 
        WRAPPER_NO_CONTRACT;             
        TYPE value;              
        ASSERT(Get(pKey, &value));               
        return value;
    }
    BOOL Get(CSString* psszKey, TYPE* pValue) 
    {
        WRAPPER_NO_CONTRACT;             
        EEStringData key(psszKey->GetCount(), psszKey->GetUnicode());
        HashDatum value;
        if (m_ht.GetValue(&key, &value))
        {
            *pValue = (TYPE)(UINT_PTR)value;
            return TRUE;
        }
        return FALSE;
    } 
    void EmptyHashTable() { WRAPPER_NO_CONTRACT; m_ht.EmptyHashTable(); }
    void DeleteValue(LPCWSTR szKey) 
    { 
        WRAPPER_NO_CONTRACT; 
        StackSString sszKey(szKey); 
        EEStringData key(sszKey.GetCount(), sszKey.GetUnicode()); 
        m_ht.DeleteValue(&key); 
    }
    DWORD GetCount() { WRAPPER_NO_CONTRACT; return m_ht.GetCount(); }
        
    TYPE Set(LPCWSTR pKey, TYPE value) { WRAPPER_NO_CONTRACT; StackSString sszKey(pKey); return Set(&sszKey, value); }
    TYPE Set(CSString* psszKey, TYPE value) 
    { 
        WRAPPER_NO_CONTRACT; 
        EEStringData key(psszKey->GetCount(), psszKey->GetUnicode()); 
        m_ht.InsertValue(&key, (HashDatum)value);
        return value;
    }
    
private:
    friend class MdaXmlElement;
    friend class MdaSchema;
    friend class ManagedDebuggingAssistants;
    
private:
    EEUnicodeStringHashTable m_ht;
};


// 

// MdaEnvironment
//
class MdaEnvironment
{
public:
    MdaEnvironment();
    ~MdaEnvironment();
    BOOL IsDisabled() { return m_bDisable; }
    LPCWSTR GetConfigFile() { return m_psszConfigFile->GetUnicode(); }
    LPCWSTR GetMdaConfigFile() { return m_psszMdaConfigFile->GetUnicode(); }
    SArray<SString*>& GetActivationMechanisms() { return *m_pGroups; }
    
private:
    LPWSTR m_szMda;
    MdaFactory<StackSString>* m_pStringFactory;
    SString* m_psszMdaConfigFile;
    SString* m_psszConfigFile;
    BOOL m_bDisable;
    SArray<SString*>* m_pGroups;
};


// 
// Mda
//

// Use these macros if your callsite cannot run on the debugger helper thread.  This is the fastest version.
#define MDA_GET_ASSISTANT(ASSISTANT) (Mda##ASSISTANT*)ManagedDebuggingAssistants::GetAssistant(MdaElemDef(ASSISTANT))
#define MDA_TRIGGER_ASSISTANT(ASSISTANT, MEMBER) if (Mda##ASSISTANT* pMdaAssistant = MDA_GET_ASSISTANT(ASSISTANT)) pMdaAssistant->MEMBER

// Use these macros if your callsite might run on the debugger helper thread.  This should be avoided for 
// very hot checks.
#define MDA_GET_ASSISTANT_EX(ASSISTANT) (Mda##ASSISTANT*)ManagedDebuggingAssistants::GetAssistantEx(MdaElemDef(ASSISTANT))
#define MDA_TRIGGER_ASSISTANT_EX(ASSISTANT, MEMBER) if (Mda##ASSISTANT* pMdaAssistant = MDA_GET_ASSISTANT_EX(ASSISTANT)) pMdaAssistant->MEMBER

class ManagedDebuggingAssistants
{            
public:
    FORCEINLINE static MdaAssistant* GetAssistant(MdaElemDeclDef id);
    FORCEINLINE static MdaAssistant* GetAssistantEx(MdaElemDeclDef id);
    FORCEINLINE static void          Enable(MdaElemDeclDef assistantDeclDef, MdaAssistant* pMda);

private:  
    static void AllocateManagedDebuggingAssistants();
    ManagedDebuggingAssistants();
    void Initialize();
#ifdef _DEBUG
    void DebugInitialize();
#endif

private:  
    void SetFwLink(MdaElemDeclDef assistant, LPCWSTR szFwLink) { LIMITED_METHOD_CONTRACT; m_szFwLinks[assistant] = szFwLink; }
    LPCWSTR GetFwLink(MdaElemDeclDef assistant) { LIMITED_METHOD_CONTRACT; return m_szFwLinks[assistant]; }
    void ReadAppConfigurationFile(MdaXmlElement* pXmlRoot, SString* pConfigFile, MdaStack<LPCWSTR>* pConfigMdaRoot);
    MdaXmlElement* GetRootElement(MdaXmlElement* pMdaXmlRoot);
    void EnvironmentActivation(MdaEnvironment* pEnvironment);
    void ConfigFileActivation(LPCWSTR szConfigFile, MdaXmlIndustry* pXmlIndustry, MdaHashtable<MdaXmlElement*>* pXmlConfigs);
    void ActivateGroup(LPCWSTR groupName, SArray<MdaElemDeclDef>* pGroup, MdaHashtable<MdaXmlElement*>* pXmlConfigs);
    MdaXmlElement* GetSwitchActivationXml(MdaElemDeclDef mda);

public:
    static BOOL IsDebuggerAttached();
    static BOOL IsManagedDebuggerAttached();
    static BOOL IsUnmanagedDebuggerAttached();

private:
    static void EEStartupActivation();    

private:
    friend HRESULT EEStartup(DWORD fFlags);

private:
    friend class MdaAssistant;
    friend class MdaEnvironment;
    friend class MdaInvalidConfigFile;
    friend class MdaSchema;
    friend class MdaAssistantSchema;
    friend class MdaAssistantMsgSchema;
    friend class MdaSchemaSchema;
    friend class MdaXmlMessage;
    friend class MdaXmlIndustry;
    friend class MdaConfigFactory;
    friend class MdaFramework;
    friend void EEStartupHelper(COINITIEE fFlags);

private:
    BOOL GetConfigBool(MdaAttrDeclDef attrDeclDef, MdaElemDeclDef element = MdaElemUndefined, BOOL bDefault = FALSE);
    BOOL GetConfigBool(MdaAttrDeclDef attrDeclDef, BOOL bDefault) { WRAPPER_NO_CONTRACT; return GetConfigBool(attrDeclDef, MdaElemUndefined, bDefault); }

private:    
    Crst* m_pLock;
    BOOL m_bValidateOutput, m_bIsInitialized;
    LPCWSTR m_szFwLinks[MdaElemDef(AssistantMax)];
    MdaSchema* m_pAssistantSchema;
    MdaSchema* m_pAssistantMsgSchema;
    MdaSchema* m_pSchemaSchema;
    MdaXmlIndustry* m_pMdaXmlIndustry;
    MdaXmlElement* m_pSwitchActivationXml;
};



typedef VPTR(MdaAssistant) PTR_MdaAssistant;

//
// MdaAssistant
//
class MdaAssistant
{     
    friend class ValidateMdaAssistantLayout;
public:
    static MdaXmlElement* OutputThread(Thread* pThread, MdaXmlElement* pXml);
    static MdaXmlElement* OutputParameter(SString parameterName, USHORT sequence, MethodDesc* pMethodDesc, MdaXmlElement* pXml);
    static MdaXmlElement* OutputMethodTable(MethodTable* pMT, MdaXmlElement* pXml);
    static MdaXmlElement* OutputMethodDesc(MethodDesc* pMethodDesc, MdaXmlElement* pXml);
    static MdaXmlElement* OutputFieldDesc(FieldDesc* pFieldDesc, MdaXmlElement* pXml);
    static MdaXmlElement* OutputTypeHandle(TypeHandle typeHandle, MdaXmlElement* pXml);
    static MdaXmlElement* OutputModule(Module* pModule, MdaXmlElement* pXml);
    static MdaXmlElement* OutputCallsite(MethodDesc *pMethodDesc, DWORD dwOffset, MdaXmlElement* pXml);
    static MdaXmlElement* OutputException(OBJECTREF *pExceptionObj, MdaXmlElement* pXml);

public:
    static SString& ToString(SString& sszBuffer, Module* pModule); 
    static SString& ToString(SString& sszBuffer, TypeHandle typeHandle); 
    static SString& ToString(SString& sszBuffer, MethodDesc* pMethodDesc); 
    static SString& ToString(SString& sszBuffer, FieldDesc* pFieldDesc); 
    static void ToString(TypeHandle typeHandle, SString* psszFullname, SString* psszNamespace);
    
public:
    LPCWSTR GetName();
    
private:
    void Initialize(MdaXmlElement* pXmlInput);
    static BOOL IsAssistantActive(MdaXmlElement* pXml); 
    
private:
    bool GetSuppressDialog() { LIMITED_METHOD_CONTRACT; return m_bSuppressDialog; }
    MdaElemDeclDef GetAssistantDeclDef() { LIMITED_METHOD_CONTRACT; return m_assistantDeclDef; }
    MdaElemDeclDef GetAssistantMsgDeclDef() { LIMITED_METHOD_CONTRACT; return m_assistantMsgDeclDef; }
    MdaXmlElement* GetRootElement(MdaXmlElement* pMdaXmlRoot, BOOL bBreak);

private:
    friend class ManagedDebuggingAssistants;
    friend class MdaXmlMessage;
    
private:
    // WARNING: do not modify the field layout without also 
    // modifying the MDA_ASSISTANT_BASE_MEMBERS macro.
    MdaElemDeclDef m_assistantDeclDef;
    MdaElemDeclDef m_assistantMsgDeclDef;
    bool m_bSuppressDialog; 
};

//
// MdaXmlAttribute 
//
class MdaXmlAttribute
{
public:
    LPCWSTR GetName();
    LPCWSTR GetValue() { LIMITED_METHOD_CONTRACT; PRECONDITION(CheckPointer(this)); return m_value.GetUnicode(); } 
    LPCWSTR GetValueAsUnicode() { LIMITED_METHOD_CONTRACT; PRECONDITION(CheckPointer(this)); return GetValueAsCSString()->GetUnicode(); } 
    SString* GetValueAsCSString() { LIMITED_METHOD_CONTRACT; PRECONDITION(CheckPointer(this)); return &m_value; } 
    BOOL GetValueAsBool() { LIMITED_METHOD_CONTRACT; PRECONDITION(CheckPointer(this)); ASSERT(m_type == MdaSchemaPrimitiveBOOL); return m_bool; }
    INT32 GetValueAsInt32() { LIMITED_METHOD_CONTRACT; PRECONDITION(CheckPointer(this)); ASSERT(m_type == MdaSchemaPrimitiveINT32); return m_int; }
    MdaAttrDeclDef GetDeclDef() { LIMITED_METHOD_CONTRACT; PRECONDITION(CheckPointer(this)); return m_declDef; }
    
private:      
    SString* ToXml(SString* xml);

    MdaXmlAttribute* Initialize(LPCWSTR szName, LPCWSTR szValue);
    
    MdaXmlAttribute* SetSString(LPCUTF8 szValue) { WRAPPER_NO_CONTRACT; m_type = MdaSchemaPrimitiveSString; m_value.SetUTF8(szValue); return this; }
    MdaXmlAttribute* SetSString(LPCWSTR szValue) { WRAPPER_NO_CONTRACT; m_type = MdaSchemaPrimitiveSString; m_value.Set(szValue); return this; }
    MdaXmlAttribute* SetDeclDef(MdaAttrDeclDef declDef) { WRAPPER_NO_CONTRACT; m_declDef = declDef; return this; }
    MdaXmlAttribute* SetNs(LPCWSTR szNs) { WRAPPER_NO_CONTRACT; m_szNs.Set(szNs); return this; } 
    MdaXmlAttribute* SetINT32(INT32 value) { LIMITED_METHOD_CONTRACT; m_type = MdaSchemaPrimitiveINT32; m_int = value; return this; }
    MdaXmlAttribute* SetBOOL(BOOL value) { LIMITED_METHOD_CONTRACT; m_type = MdaSchemaPrimitiveBOOL; m_bool = value; return this; }

private:
    friend class ManagedDebuggingAssistants;
    friend class MdaConfigFactory;
    friend class MdaSchemaSchema;
    friend class MdaXmlElement;
    friend class MdaSchema;
    friend class MdaXmlMessage;
    template<typename PRODUCT> friend class MdaFactory;

private:
    MdaAttrDeclDef m_declDef;
    SString m_szName;
    SString m_szNs;
    MdaSchemaPrimitive m_type;
    SString m_value;
    BOOL m_bool;
    INT32 m_int;
};


//
// MdaXmlElement
//
class MdaXmlElement
{
public: /* inspection */  
    LPCWSTR GetName();
    MdaElemDeclDef GetDeclDef() { LIMITED_METHOD_CONTRACT; return m_elemDeclDef; }
    BOOL IsDefinition() { LIMITED_METHOD_CONTRACT; return m_elemDeclDef < MdaElemDef(Max); }
    BOOL IsDeclaration() { LIMITED_METHOD_CONTRACT; return !IsDefinition(); }
    SArray<MdaXmlElement*>& GetChildren() { LIMITED_METHOD_CONTRACT; return m_children; }
    MdaXmlElement* GetChild(MdaElemDeclDef declDef);
    SArray<MdaXmlAttribute*>& GetAttributes() { LIMITED_METHOD_CONTRACT; return m_attributes; }
    MdaXmlAttribute* GetAttribute(MdaAttrDeclDef attrDeclDef);
    BOOL GetAttributeValueAsBool(MdaAttrDeclDef attrDeclDef, BOOL bDefault);
    BOOL GetAttributeValueAsBool(MdaAttrDeclDef attrDeclDef);
    
public: /* creation */
    MdaXmlElement* SetDeclDef(MdaElemDeclDef elemDeclDef) { LIMITED_METHOD_CONTRACT; m_elemDeclDef = elemDeclDef; return this; }
    MdaXmlElement* SetName(LPCWSTR name, BOOL bAssertDefined = TRUE);
    MdaXmlElement* AddChild(LPCWSTR name, BOOL bAssertDefined = TRUE);
    MdaXmlElement* AddChild(MdaElemDeclDef type);
    void AddChildComment(LPCWSTR szComment) { WRAPPER_NO_CONTRACT; AddChild(MdaElemComment)->m_szName.Set(szComment); }   
    LPCWSTR DebugToString(SString* pBuffer);
    
    template<typename ATTRIBUTE_TYPE>
    MdaXmlAttribute* AddAttributeSz(MdaAttrDeclDef declDef, ATTRIBUTE_TYPE szValue) { return AddAttribute(declDef)->SetSString(szValue); }
    MdaXmlAttribute* AddAttributeInt(MdaAttrDeclDef declDef, INT32 value) { return AddAttribute(declDef)->SetINT32(value); }
    MdaXmlAttribute* AddAttributeBool(MdaAttrDeclDef declDef, BOOL bValue) { return AddAttribute(declDef)->SetBOOL(bValue); }
         
private:
    MdaXmlElement() : m_elemDeclDef(MdaElemUndefined), m_defaultAttrIndex(-1) { WRAPPER_NO_CONTRACT; }
    MdaXmlElement* AddChild(MdaXmlElement* pChild); 

    MdaXmlElement* SetIndustry(MdaXmlIndustry* pXmlIndustry)
        { LIMITED_METHOD_CONTRACT; PRECONDITION(CheckPointer(pXmlIndustry, NULL_OK)); m_pXmlIndustry = pXmlIndustry; return this; }

    MdaXmlAttribute* AddDefaultAttribute(MdaAttrDeclDef attrDeclDef, LPCWSTR szValue); 
    MdaXmlAttribute* AddAttribute(LPCWSTR szName, LPCWSTR szValue);

    SString* ToXml(SString* xml) { WRAPPER_NO_CONTRACT; return ToXml(xml, NULL, 0); }
    SString* ToXml(SString* xml, LPCWSTR ns) { WRAPPER_NO_CONTRACT; return ToXml(xml, ns, 0); }
    SString* ToXml(SString* xml, LPCWSTR ns, INT32 depth);

    MdaXmlAttribute* AddAttribute(MdaAttrDeclDef declDef);
    MdaXmlAttribute* AddAttribute(MdaXmlAttribute* pAttr) { WRAPPER_NO_CONTRACT; *m_attributes.Append() = pAttr; return pAttr; }
    
private:
    friend class MdaSchema;
    friend class ManagedDebuggingAssistants;
    friend class MdaXmlMessage;
    friend class MdaXmlElement;
    template<typename PRODUCT> friend class MdaFactory;
    friend class MdaXmlIndustry;    
    friend class MdaConfigFactory;    
    friend class MdaSchemaSchema;
    friend class MdaXmlValidationError;
    
private:
    MdaXmlIndustry* m_pXmlIndustry;
    MdaElemDeclDef m_elemDeclDef;
    SString m_szName;
    InlineSArray<MdaXmlElement*, MDA_XML_ELEMENT_CHILDREN> m_children;
    COUNT_T m_defaultAttrIndex;
    InlineSArray<MdaXmlAttribute*, MDA_XML_ELEMENT_ATTRIBUTES>  m_attributes;
};


//
// MdaFactory 
//
template<typename PRODUCT>
class MdaFactory
{
public:
    MdaFactory() : m_cProduct(0), m_next(NULL) { LIMITED_METHOD_CONTRACT; }
    ~MdaFactory() { LIMITED_METHOD_CONTRACT; if (m_next) delete m_next; } 
    MdaFactory* GetNext() { if (!m_next) m_next = new MdaFactory<PRODUCT>(); return m_next; }   
    PRODUCT* Create();

private:
    MdaFactory* m_next;
    PRODUCT m_product[MDA_MAX_FACTORY_PRODUCT];
    INT32 m_cProduct;
};


//
// MdaXmlIndustry
//
class MdaXmlIndustry
{
public:
    MdaXmlElement* CreateElement() { WRAPPER_NO_CONTRACT; return m_elements.Create()->SetIndustry(this); }
    MdaXmlAttribute* CreateAttribute() { WRAPPER_NO_CONTRACT; return m_attributes.Create(); }
      
private:
    MdaFactory<MdaXmlElement> m_elements;
    MdaFactory<MdaXmlAttribute> m_attributes;
    
private:
    friend class MdaConfigFactory;
    friend class MdaFramework;
    friend class MdaXmlMessage;
    friend class ManagedDebuggingAssistants;
    friend class MdaXmlElement;
    friend class MdaXmlAttribute;
    friend class MdaSchema;
};


//
// MdaXmlMessage 
//
class MdaXmlMessage
{
public:
    MdaXmlMessage(MdaXmlElement** ppMdaXmlRoot);   
    MdaXmlMessage(MdaAssistant* pAssistant, BOOL bBreak, MdaXmlElement** ppMdaXmlRoot);

public:
    void SendMessage();
    void SendMessage(int resourceID); 
    void SendMessage(LPCWSTR szMessage); 
    void SendMessagef(int resourceID, ...); 
    
private:
    static BOOL IsDebuggerAttached() { WRAPPER_NO_CONTRACT; return ManagedDebuggingAssistants::IsDebuggerAttached(); }
    static BOOL IsManagedDebuggerAttached() { WRAPPER_NO_CONTRACT; return ManagedDebuggingAssistants::IsManagedDebuggerAttached(); }
    static BOOL IsUnmanagedDebuggerAttached() { WRAPPER_NO_CONTRACT; return ManagedDebuggingAssistants::IsUnmanagedDebuggerAttached(); }
    static BOOL ShouldLogToManagedDebugger();
    
private:
    void SendEvent();
    void SendHostEvent();
    void SendDebugEvent();

private:
    friend class ManagedDebuggingAssistants;
    friend class MdaFramework;
    
private:
    BOOL m_bBreak;
    MdaAssistant* m_pMdaAssistant;
    SString m_localizedMessage;
    SString m_englishMessage;
    MdaXmlElement* m_pMdaXmlRoot;
    MdaXmlElement* m_pAssistantXmlRoot;
    MdaXmlIndustry m_mdaXmlIndustry;
};


//
// MdaXPath 
//
class MdaXPath
{
public:
    static SArray<MdaXmlElement*>* FindElements(MdaXmlElement* pRoot, LPCWSTR szQuery, SArray<MdaXmlElement*>* pResult)
        { WRAPPER_NO_CONTRACT; MdaXPath query(szQuery); return query.FindElements(pRoot, pResult); }
    static MdaXmlElement* FindElement(MdaXmlElement* pRoot, LPCWSTR szQuery)
        { WRAPPER_NO_CONTRACT; MdaXPath query(szQuery); return query.FindElement(pRoot); }
    static SArray<MdaXmlAttribute*>* FindAttributes(MdaXmlElement* pRoot, LPCWSTR szQuery, SArray<MdaXmlAttribute*>* pResult)
        { WRAPPER_NO_CONTRACT; MdaXPath query(szQuery); return query.FindAttributes(pRoot, pResult); }
    static MdaXmlAttribute* FindAttribute(MdaXmlElement* pRoot, LPCWSTR szQuery)
        { WRAPPER_NO_CONTRACT; MdaXPath query(szQuery); return query.FindAttribute(pRoot); }

public:
    MdaXPath() : m_cArgs(NOT_VARIABLE), m_pCompiledQuery(NULL) { WRAPPER_NO_CONTRACT; }
    MdaXPath(LPCWSTR xpath) : m_cArgs(NOT_VARIABLE) { WRAPPER_NO_CONTRACT; Initialize(xpath); }
    MdaXPath* Initialize(LPCWSTR xpath) { WRAPPER_NO_CONTRACT; m_xpath.Set(xpath); MdaXPathCompiler(this, &m_pCompiledQuery); return this; }
    MdaXmlElement* FindElement(MdaXmlElement* pRoot, ...); 
    MdaXmlAttribute* FindAttribute(MdaXmlElement* pRoot, ...); 
    SArray<MdaXmlElement*>* FindElements(MdaXmlElement* pRoot, SArray<MdaXmlElement*>* pResult, ...); 
    SArray<MdaXmlAttribute*>* FindAttributes(MdaXmlElement* pRoot, SArray<MdaXmlAttribute*>* pResult, ...);    
    COUNT_T GetArgCount() { LIMITED_METHOD_CONTRACT; return m_cArgs + 1; }
    
private:   
    class MdaXPathBase; 
    class MdaXPathElement;
    class MdaXPathAttribute;    
    class MdaXPathResult;
    class MdaXPathLogicalOp;

    typedef enum
    {
        XPathVarAttrBool = MdaSchemaPrimitiveBOOL,
        XPathVarAttrSString = MdaSchemaPrimitiveSString,
        XPathVarAttrINT32 = MdaSchemaPrimitiveINT32,
        XPathVarElemDeclDef = XPathVarAttrINT32 + 1,
        XPathVarAttrDeclDef = XPathVarAttrINT32 + 2,
    } XPathVarType;
    
    typedef struct 
    {
        union
        {
            MdaElemDeclDef m_elemDeclDef;
            MdaAttrDeclDef m_attrDeclDef;
            BOOL m_bool;
            SString* m_pSstr;
            INT32 m_int32;
        } m_u;
    } MdaXPathVariable;

private:  
    void Find(SArray<MdaXPathVariable>& args, SString* pWildCard, va_list argItr);
    static const COUNT_T NOT_VARIABLE = -1;

private:  
    class MdaXPathResult
    {
    public:
        MdaXPathResult(SArray<MdaXPathVariable>* args) { LIMITED_METHOD_CONTRACT; Initialize(args); }
        MdaXPathResult(SArray<MdaXmlElement*>* pElements, SArray<MdaXPathVariable>* args) { WRAPPER_NO_CONTRACT; Initialize(args); m_pElements = pElements; }
        MdaXPathResult(SArray<MdaXmlAttribute*>* pAttributes, SArray<MdaXPathVariable>* args) { WRAPPER_NO_CONTRACT; Initialize(args); m_pAttributes = pAttributes; }
        void Initialize(SArray<MdaXPathVariable>* args) { LIMITED_METHOD_CONTRACT; m_args = args; m_pElements = NULL; m_pAttributes = NULL; m_pElement = NULL; m_pAttribute = NULL; m_bIsRoot = TRUE; } 
        MdaXmlElement* GetXmlElement() { LIMITED_METHOD_CONTRACT; return m_pElement; }
        MdaXmlAttribute* GetXmlAttribute() { LIMITED_METHOD_CONTRACT; return m_pAttribute; }

        void AddMatch(MdaXmlAttribute* pMatch) 
            { LIMITED_METHOD_CONTRACT; if (m_pAttributes) m_pAttributes->Append((MdaXmlAttribute*)pMatch); else { ASSERT(!m_pAttribute); m_pAttribute = pMatch; } }
        void AddMatch(MdaXmlElement* pMatch) 
            { LIMITED_METHOD_CONTRACT; if (m_pElements) m_pElements->Append((MdaXmlElement*)pMatch); else { ASSERT(!m_pElement); m_pElement = pMatch; } }
        BOOL IsRoot() { LIMITED_METHOD_CONTRACT; if (!m_bIsRoot) return FALSE; m_bIsRoot = FALSE; return TRUE; }
        SArray<MdaXPathVariable>& GetArgs() { LIMITED_METHOD_CONTRACT; return *m_args; }
        
    private:
        BOOL m_bIsRoot;
        SArray<MdaXPathVariable>* m_args;
        SArray<MdaXmlElement*>* m_pElements;
        SArray<MdaXmlAttribute*>* m_pAttributes;
        MdaXmlElement* m_pElement;
        MdaXmlAttribute* m_pAttribute;
    };
    
    class MdaXPathCompiler
    {
    public:
        MdaXPathCompiler(MdaXPath* pXPath, MdaXPathBase** ppCompiledQuery) 
            : m_pXPath(pXPath) { WRAPPER_NO_CONTRACT; m_itr = pXPath->m_xpath.Begin(); NextToken(); *ppCompiledQuery = XPATH(); }

    private:
        typedef enum {
            //
            // TOKENS
            //
            MdaXPathIdentifier          = 0x0001,
            MdaXPathDot                 = 0x0002,
            MdaXPathSlash               = 0x0004,
            MdaXPathAstrix              = 0x0008,
            MdaXPathQuotedString        = 0x0010,
            MdaXPathOpenParen           = 0x0020,
            MdaXPathCloseParen          = 0x0040,
            MdaXPathOpenSqBracket       = 0x0080,
            MdaXPathCloseSqBracket      = 0x0100,
            MdaXPathLogicalAnd          = 0x0200,
            MdaXPathLogicalOr           = 0x0400,
            MdaXPathEquals              = 0x0800,
            MdaXPathAtSign              = 0x1000,         
            MdaXPathQMark               = 0x2000,         
            MdaXPathEnd                 = 0x4000,

            //
            // 1 TOKEN LOOK AHEAD 
            //
            MdaXPathSTART               = MdaXPathSlash,
            MdaXPathXPATH               = MdaXPathSlash,
            MdaXPathATTRIBUTE           = MdaXPathAtSign,
            MdaXPathATTRIBUTE_FILTER    = MdaXPathAtSign,
            MdaXPathELEMENT             = MdaXPathIdentifier | MdaXPathAstrix | MdaXPathQMark,
            MdaXPathELEMENT_EXPR        = MdaXPathELEMENT,
            MdaXPathFILTER              = MdaXPathELEMENT_EXPR | MdaXPathATTRIBUTE_FILTER,
            MdaXPathFILTER_EXPR         = MdaXPathFILTER | MdaXPathOpenParen,
        } MdaXPathTokens;

    //
    // LEXIFIER 
    //
    private:
        MdaXPathTokens LexAToken();
        void NextToken()  { WRAPPER_NO_CONTRACT; m_currentToken = LexAToken(); }
        BOOL TokenIs(MdaXPathTokens token) { LIMITED_METHOD_CONTRACT; return !!(m_currentToken & token); }
        BOOL TokenIs(int token) { LIMITED_METHOD_CONTRACT; return TokenIs((MdaXPathTokens)token); }
        LPCWSTR GetIdentifier() { WRAPPER_NO_CONTRACT; return m_identifier.GetUnicode(); }
        
    //
    // PRODUCTIONS
    //
    private: 
        MdaXPathBase* XPATH();
        //  '/' ATTRIBUTE end
        //  '/' ELEMENT_EXPR XPATH
        //  '/' ELEMENT_EXPR end
        
        MdaXPathAttribute* ATTRIBUTE();
        //  '@' id
        //  '@' '?'
        
        MdaXPathElement* ELEMENT();
        //  id
        //  '*'
        //  '?'
        
        MdaXPathElement* ELEMENT_EXPR();
        //  ELEMENT '[' FILTER_EXPR ']'
        //  ELEMENT 
        
        MdaXPathBase* FILTER_EXPR();
        //  FILTER
        //  '(' FILTER ')'
        //  FILTER '&' FILTER
        //  FILTER '|' FILTER
                
        MdaXPathBase* FILTER();
        //  ELEMENT_EXPR
        //  ATTRIBUTE_FILTER
        //  ELEMENT_EXPR ATTRIBUTE_FILTER
        
        MdaXPathAttribute* ATTRIBUTE_FILTER();
        //  ATTRIBUTE
        //  ATTRIBUTE '=' ''' id '''
        //  ATTRIBUTE '=' '?'
        
    private:
        MdaXPath* m_pXPath;
        SString::CIterator m_itr;
        StackSString m_identifier;
        MdaXPathTokens m_currentToken;
    };

    class MdaXPathBase 
    {
    public:
        virtual BOOL Run(MdaXmlElement* pElement, MdaXPathResult* pResult) = 0;
        virtual BOOL IsXPathAttribute() { LIMITED_METHOD_CONTRACT; return FALSE; }

    private:
    };
    
    class MdaXPathElement : public MdaXPathBase 
    {
    public:
        virtual BOOL Run(MdaXmlElement* pElement, MdaXPathResult* pResult);
        BOOL RunOnChild(MdaXmlElement* pElement, MdaXPathResult* pResult);
        
    public:
        MdaXPathElement() : m_name(MdaElemUndefined), m_nameArg(NOT_VARIABLE), m_bIsTarget(FALSE), m_pChild(NULL), m_pQualifier(NULL) { LIMITED_METHOD_CONTRACT; }
        MdaXPathBase* MarkAsTarget() { LIMITED_METHOD_CONTRACT; m_bIsTarget = TRUE; return this; };
        MdaXPathElement* SetChild(MdaXPathBase* pChild) { LIMITED_METHOD_CONTRACT; m_pChild = pChild; return this; }
        MdaXPathElement* SetQualifier(MdaXPathBase* pQualifier) { LIMITED_METHOD_CONTRACT; m_pQualifier = pQualifier; return this; }
        MdaXPathElement* Initialize() { LIMITED_METHOD_CONTRACT; return this; }
        MdaXPathElement* Initialize(MdaElemDeclDef identifier) { LIMITED_METHOD_CONTRACT; m_name = identifier; return this; }
        MdaXPathElement* Initialize(COUNT_T identifier) { LIMITED_METHOD_CONTRACT; m_nameArg = identifier; return this; }
        
    private:
        MdaElemDeclDef m_name;
        COUNT_T m_nameArg;
        BOOL m_bIsTarget;       
        MdaXPathBase* m_pChild;
        MdaXPathBase* m_pQualifier;
    };
    
    class MdaXPathAttribute : public MdaXPathBase 
    {
    public:
        MdaXPathAttribute() : m_name(MdaAttrUndefined), m_nameArg(NOT_VARIABLE), m_valueArg(NOT_VARIABLE) { WRAPPER_NO_CONTRACT; }
        virtual BOOL Run(MdaXmlElement* pElement, MdaXPathResult* pResult);
        virtual BOOL IsXPathAttribute() { LIMITED_METHOD_CONTRACT; return TRUE; }

    public:
        MdaXPathBase* MarkAsTarget() { LIMITED_METHOD_CONTRACT; m_bIsTarget = TRUE; return this; };
        MdaXPathAttribute* SetName(MdaAttrDeclDef name) { WRAPPER_NO_CONTRACT; m_name = name; return this; }
        MdaXPathAttribute* SetValue(LPCWSTR value) { WRAPPER_NO_CONTRACT; m_value.Set(value); return this; }
        MdaXPathAttribute* SetName(COUNT_T name) { WRAPPER_NO_CONTRACT; m_nameArg = name; return this; }
        MdaXPathAttribute* SetValue(COUNT_T value) { WRAPPER_NO_CONTRACT; m_valueArg = value; return this; }
        
    private:
        BOOL m_bIsTarget;       
        MdaAttrDeclDef m_name;
        COUNT_T m_nameArg;
        SString m_value;
        COUNT_T m_valueArg;
    };
    
    class MdaXPathLogicalOp : public MdaXPathBase 
    {
    public:
        virtual BOOL Run(MdaXmlElement* pElement, MdaXPathResult* pResult);
        
    public:
        MdaXPathLogicalOp* Initialize(BOOL andOp, MdaXPathBase* pLhs, MdaXPathBase* pRhs)
            { LIMITED_METHOD_CONTRACT; m_andOp = andOp; m_pLhs = pLhs; m_pRhs = pRhs; return this; }
               
    private:
        BOOL m_andOp;
        MdaXPathBase* m_pLhs;
        MdaXPathBase* m_pRhs;
    };     
    
private:
    COUNT_T m_cArgs;
    InlineSArray<XPathVarType, 20> m_argTypes;
    StackSString m_xpath;
    MdaXPathBase* m_pCompiledQuery;
    MdaFactory<MdaXPathElement> m_elementFactory;
    MdaFactory<MdaXPathAttribute> m_attrFactory;
    MdaFactory<MdaXPathLogicalOp> m_logicalOpFactory;  
};


//
// MdaSchema 
//
class MdaSchema
{
private:
    static void Initialize();

public:
//     SPTR_DECL(RangeSection, m_RangeTree);
//     SPTR_IMPL(RangeSection, ExecutionManager, m_RangeTree);

    static MdaElemDeclDef GetElementType(LPCWSTR name, BOOL bAssertDefined = TRUE);
    static LPCWSTR GetElementName(MdaElemDeclDef type);
    static MdaAttrDeclDef GetAttributeType(LPCWSTR name, BOOL bAssertDefined = TRUE);
    static LPCWSTR GetAttributeName(MdaAttrDeclDef type);

public:
    static LPCWSTR g_arElementNames[MdaElemEnd];

private:
    static LPCWSTR g_arAttributeNames[MdaAttrEnd];
    static MdaFactory<SString>* g_pSstringFactory;
    static MdaHashtable<MdaElemDeclDef>* g_pHtElementType;
    static MdaHashtable<MdaAttrDeclDef>* g_pHtAttributeType;
    static LPCWSTR ToLowerFirstChar(LPCWSTR name);

private:
    class MdaSchemaBase;
    class MdaSchemaAttribute;
    class MdaSchemaSequence;
    class MdaSchemaChoice;
    class MdaSchemaComplexType;
    class MdaSchemaElement;
    class MdaSchemaGroup;
    class MdaSchemaGroupRef;
    class MdaSchemaExtension;
    class MdaSchemaDeclDefRef;

private:
    class ValidationResult
    {
    public:
        ValidationResult() { LIMITED_METHOD_CONTRACT; ResetResult(); }
        void ResetResult() { LIMITED_METHOD_CONTRACT; m_bValid = TRUE; m_pViolatedElement = NULL; m_pViolatingElement = NULL; m_pXmlRoot = NULL; m_pSchema = NULL; }
        BOOL ValidationFailed() { LIMITED_METHOD_CONTRACT; return !m_bValid; }
        void Initialize(MdaSchema* pSchema, MdaXmlElement* pRoot) { LIMITED_METHOD_CONTRACT; m_pXmlRoot = pRoot; m_pSchema = pSchema; }
        void SetError() { LIMITED_METHOD_CONTRACT; m_bValid = FALSE; }
        void SetError(MdaSchemaBase* pViolatedElement, MdaXmlElement* pViolatingElement)
            { LIMITED_METHOD_CONTRACT; m_bValid = FALSE; m_pViolatedElement = pViolatedElement; m_pViolatingElement = pViolatingElement; }

    private:
        friend class MdaXmlValidationError;
        
    private:
        BOOL m_bValid;
        MdaXmlElement* m_pXmlRoot;
        MdaSchema* m_pSchema;        
        MdaSchemaBase* m_pViolatedElement;
        MdaXmlElement* m_pViolatingElement;
    };

private:
    static BOOL MayHaveAttr(MdaSchemaBase* pBase) { LIMITED_METHOD_CONTRACT; return MdaSchemaTypeToMetaType[pBase->GetSchemaType()] & MdaSchemaMataMayHaveAttributes; }
    static BOOL IsPattern(MdaSchemaBase* pBase) { LIMITED_METHOD_CONTRACT; return MdaSchemaTypeToMetaType[pBase->GetSchemaType()] & MdaSchemaMataTypePattern; }
    static BOOL IsRef(MdaSchemaBase* pBase) { LIMITED_METHOD_CONTRACT; return MdaSchemaTypeToMetaType[pBase->GetSchemaType()] & MdaSchemaMataTypeRef; }
    static BOOL IsDeclDef(MdaSchemaBase* pBase) { LIMITED_METHOD_CONTRACT; return MdaSchemaTypeToMetaType[pBase->GetSchemaType()] & MdaSchemaMataTypeDeclDef; }
    static BOOL IsDeclDefRef(MdaSchemaBase* pBase) { WRAPPER_NO_CONTRACT; return IsDeclDef(pBase) || IsRef(pBase); }
    static MdaSchemaDeclDefRef* AsDeclDefRef(MdaSchemaBase* pBase) { WRAPPER_NO_CONTRACT; if (!IsDeclDefRef(pBase)) return NULL; return (MdaSchemaDeclDefRef*)pBase; }
    static MdaSchemaDeclDefRef* ToDeclDefRef(MdaSchemaBase* pBase) { WRAPPER_NO_CONTRACT; ASSERT(IsDeclDefRef(pBase)); return (MdaSchemaDeclDefRef*)pBase; }
    static MdaSchemaDeclDefRef* ToDeclDef(MdaSchemaBase* pBase) { WRAPPER_NO_CONTRACT; ASSERT(IsDeclDef(pBase)); return (MdaSchemaDeclDefRef*)pBase; }
    static MdaSchemaDeclDefRef* ToRef(MdaSchemaBase* pBase) { WRAPPER_NO_CONTRACT; ASSERT(IsRef(pBase)); return (MdaSchemaDeclDefRef*)pBase; }

public:
    typedef enum {
        MdaSchemaSequenceType,
        MdaSchemaChoiceType,
        MdaSchemaGroupType,
        MdaSchemaGroupRefType,
        MdaSchemaRootType,
        MdaSchemaAttributeType,
        MdaSchemaElementType,
        MdaSchemaComplexTypeType,
        MdaSchemaComplexTypeDefType,
        MdaSchemaElementRefTyp,
        MdaSchemaExtensionType,
        MdaSchemaElementRefTypeType,
        MdaSchemaComplexContentType,
        MdaSchemaElementAnyType,
        MdaSchemaTypeEnd,
    } MdaSchemaType;

    typedef enum {
        MdaSchemaMataNone               = 0x0,
        MdaSchemaMataTypePattern        = 0x1,
        MdaSchemaMataTypeDeclDef        = 0x2,
        MdaSchemaMataTypeRef            = 0x4,        
        MdaSchemaMataMayHaveAttributes  = 0x8,        
    } MdaSchemaMetaType;
    
private:
    static MdaElemDeclDef MdaSchemaTypeToElemDef[];
    static MdaSchemaMetaType MdaSchemaTypeToMetaType[];

    class MdaSchemaBase
    {
    public:
        virtual MdaSchemaType GetSchemaType() = 0;
        virtual MdaElemDeclDef GetSchemaDeclDef() { LIMITED_METHOD_CONTRACT; return MdaSchemaTypeToElemDef[GetSchemaType()]; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
        virtual BOOL ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount) { UNREACHABLE(); }
        virtual void SetAttributes(MdaXmlElement* pXml) { LIMITED_METHOD_CONTRACT; }
        
    public:
        void Verify(MdaSchemaType schemaType, MdaElemDeclDef declDef) 
        { 
            LIMITED_METHOD_CONTRACT; 
            // Look for missmatch element in your schema ELEMENT(Foo) ... ELEMENT_END(bar)
            ASSERT(schemaType == GetSchemaType() && 
                W("Mismatch element in your schema ELEMENT(foo) ... TYPE_END(foo) -- attach debugger and look for MdaAssistantSchema on stack"));
            ASSERT(ToDeclDef(this)->GetDeclDef() == declDef && 
                W("Mismatch declaration in your schema ELEMENT(Foo) ... ELEMENT_END(bar) -- attach debugger and look for MdaAssistantSchema on stack")); 
        }
        void Verify(MdaSchemaType schemaType, MdaSchemaBase** ppRef) 
        { 
            LIMITED_METHOD_CONTRACT; 
            // Look for missmatch element in your schema ELEMENT(Foo) ... ELEMENT_END(bar)
            ASSERT(schemaType == GetSchemaType() && 
                W("Mismatch element in your schema ELEMENT(foo) ... TYPE_END(foo) -- attach debugger and look for MdaAssistantSchema on stack"));
            ASSERT(ToRef(this)->m_ppRef == ppRef && 
                W("Mismatch declaration in your schema ELEMENT(foo) ... ELEMENT_END(bar) -- attach debugger and look for MdaAssistantSchema on stack")); 
        }
        void Verify(MdaSchemaType schemaType) 
        { 
            LIMITED_METHOD_CONTRACT; 
            // Look for missmatch element in your schema ELEMENT(Foo) ... ELEMENT_END(bar)
            ASSERT(schemaType == GetSchemaType() && 
                W("Mismatch element in your schema ELEMENT(foo) ... TYPE_END(foo) -- attach debugger and look for MdaAssistantSchema on stack")); 
        }
        
    public:
        MdaXmlElement* ToXml(MdaXmlIndustry* pMdaXmlIndustry, MdaSchemaBase* pViolation = NULL);
        MdaXmlElement* ToXml(MdaXmlElement* pXmlRoot) { WRAPPER_NO_CONTRACT; return ToXml(pXmlRoot, NULL); }
        MdaXmlElement* ToXml(MdaXmlElement* pXmlRoot, MdaSchemaBase* pViolation);
        void AddChild(MdaSchemaBase* pElement);
        LPCWSTR GetName() { WRAPPER_NO_CONTRACT; return GetElementName(GetSchemaDeclDef()); }
        friend class MdaSchemaExtension;
        
    protected:
        InlineSArray<MdaSchemaBase*, MDA_XML_ELEMENT_CHILDREN> m_children;
        virtual InlineSArray<MdaSchemaAttribute*, MDA_XML_ELEMENT_CHILDREN>& GetAttributes() { LIMITED_METHOD_CONTRACT; UNREACHABLE(); }
    };

    // <xs:schema>
    class MdaSchemaRoot : public MdaSchemaBase
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaRootType; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
    };

    // <xs:attribute name="enable" value="xs:boolean" required="true" default="true">
    static BOOL MdaSchema::Validate(MdaSchemaAttribute* pThis, MdaXmlElement* pElement, ValidationResult* pResult);
    class MdaSchemaAttribute : public MdaSchemaBase
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaAttributeType; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult) { WRAPPER_NO_CONTRACT; return MdaSchema::Validate(this, pElement, pResult); }
        
    public:
        void SetAttributes(MdaXmlElement* pXml);
        MdaSchemaAttribute* Initialize(MdaAttrDeclDef name, MdaSchemaPrimitive type, BOOL bRequired, LPCWSTR szDefault) 
            { WRAPPER_NO_CONTRACT; m_declDef = name; m_type = type; m_bRequired = bRequired; m_szDefault = szDefault; return this; }
        
    private:
        friend MdaSchema;
        
    private:       
        BOOL m_bRequired; 
        SString m_szDefault;
        MdaAttrDeclDef m_declDef;
        MdaSchemaPrimitive m_type;
    };

    // <xs:sequence minOccures="0" maxOccures="unbounded">
    class MdaSchemaSequence : public MdaSchemaBase
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaSequenceType; }
        virtual BOOL ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount);
        virtual void SetAttributes(MdaXmlElement* pXml);
        
    public:
        MdaSchemaSequence* Initialize(COUNT_T min, COUNT_T max) { WRAPPER_NO_CONTRACT; m_min = min; m_max = max; return this; }
        
    private:
        BOOL m_VsHack;
        COUNT_T m_min;
        COUNT_T m_max;
    };

    // <xs:choice>
    class MdaSchemaChoice : public MdaSchemaBase
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaChoiceType; }
        virtual BOOL ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount);
    };
    
    // <xs:complexContent>
    class MdaSchemaComplexContent : public MdaSchemaBase
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaComplexContentType; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
    };

    // <xs:complexType>
    class MdaSchemaComplexType : public MdaSchemaBase
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaComplexTypeType; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
        virtual InlineSArray<MdaSchemaAttribute*, MDA_XML_ELEMENT_CHILDREN>& GetAttributes() { LIMITED_METHOD_CONTRACT; return m_attributes; }

    private:
        friend class MdaSchemaExtension;
        InlineSArray<MdaSchemaAttribute*, MDA_XML_ELEMENT_CHILDREN> m_attributes;
    };

    class MdaSchemaDeclDefRef : public MdaSchemaBase
    {
    public:
        virtual void SetAttributes(MdaXmlElement* pXml);

    public:
        MdaSchemaDeclDefRef() : m_declDef(MdaElemUndefined), m_ppRef(NULL) { LIMITED_METHOD_CONTRACT; }
        LPCWSTR GetDeclDefName() { WRAPPER_NO_CONTRACT; ASSERT(IsDeclDef(this)); return GetElementName(m_declDef); }
        LPCWSTR GetRefName() { LIMITED_METHOD_CONTRACT; return GetRef()->GetDeclDefName(); }
        MdaElemDeclDef GetDeclDef() { LIMITED_METHOD_CONTRACT; ASSERT(IsDeclDef(this)); return m_declDef; }
        MdaSchemaDeclDefRef* GetRef() { LIMITED_METHOD_CONTRACT; ASSERT(IsRef(this)); return ToDeclDef(*m_ppRef); }
        BOOL IsDefinition() { LIMITED_METHOD_CONTRACT; ASSERT(IsDeclDef(this)); return m_declDef < MdaElemDef(Max); }

    public:
        MdaSchemaDeclDefRef* InitRef(MdaSchemaBase** ppRef) { WRAPPER_NO_CONTRACT; ASSERT(IsRef(this)); m_ppRef = ppRef; return this; }
        MdaSchemaDeclDefRef* InitDeclDef(MdaElemDeclDef declDef) { WRAPPER_NO_CONTRACT; ASSERT(IsDeclDef(this)); m_declDef = declDef; return this; }

    private:
        friend class MdaSchemaBase;
        MdaSchemaBase** m_ppRef;
        MdaElemDeclDef m_declDef;
    };

    // <xs:group name="myGroup">
    class MdaSchemaGroup : public MdaSchemaDeclDefRef
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaGroupType; }
        virtual BOOL ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount);
    };

    // <xs:element name="myGroup">
    class MdaSchemaElement : public MdaSchemaDeclDefRef
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaElementType; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
    };

    // <xs:complexType name="myElementType">
    class MdaSchemaComplexTypeDef : public MdaSchemaDeclDefRef
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaComplexTypeDefType; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
        virtual InlineSArray<MdaSchemaAttribute*, MDA_XML_ELEMENT_CHILDREN>& GetAttributes() { LIMITED_METHOD_CONTRACT; return m_attributes; }

    private:
        friend class MdaSchemaExtension;
        InlineSArray<MdaSchemaAttribute*, MDA_XML_ELEMENT_CHILDREN> m_attributes;
    };

    // <xs:group ref="myGroup">
    class MdaSchemaGroupRef : public MdaSchemaDeclDefRef
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaGroupRefType; }
        virtual BOOL ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount);
    };

    // <xs:extension base="myElementType">
    class MdaSchemaExtension : public MdaSchemaDeclDefRef
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaExtensionType; }
        virtual BOOL Validate(MdaXmlElement* pXml, ValidationResult* pResult);
        virtual InlineSArray<MdaSchemaAttribute*, MDA_XML_ELEMENT_CHILDREN>& GetAttributes() { LIMITED_METHOD_CONTRACT; return m_attributes; }
        
    public:
        MdaSchemaExtension() { LIMITED_METHOD_CONTRACT; }

    private:
        InlineSArray<MdaSchemaAttribute*, MDA_XML_ELEMENT_CHILDREN> m_attributes;
    };

    // <xs:element ref="myElement">
    class MdaSchemaElementRef : public MdaSchemaDeclDefRef
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaElementRefTyp; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
    };

    // <xs:element name="myElementAsMyType" type="myType">
    class MdaSchemaElementRefType : public MdaSchemaDeclDefRef
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaElementRefTypeType; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
    };

    // <xs:element name="myElementAsMyType" type="xs:anyType">
    class MdaSchemaElementAny : public MdaSchemaDeclDefRef
    {
    public:
        virtual MdaSchemaType GetSchemaType() { LIMITED_METHOD_CONTRACT; return MdaSchemaElementAnyType; }
        virtual BOOL Validate(MdaXmlElement* pElement, ValidationResult* pResult);
    };
/*
    // <xs:simpleType name="mySimpleType>
    class MdaSimpleTypeDef : public MdaSchemaDeclDefRef
    {
    }

    // <xs:restriction base="xs:string>
    class MdaRestriction : public MdaSchemaDeclDefRef
    {
    }

    // <xs:enumeration value="blue">
    class MdaEnumeration : public MdaSchemaBase
    {
    }
*/
private:
    MdaSchema();
    virtual LPCWSTR SetRootAttributes(MdaXmlElement* pXml) = 0;
    ValidationResult* Validate(MdaXmlElement* pRoot, ValidationResult* pResult);
    MdaXmlElement* ToXml(MdaXmlElement* pXmlRoot) { WRAPPER_NO_CONTRACT; return m_tos->ToXml(pXmlRoot); }
    MdaXmlElement* ToXml(MdaXmlIndustry* pMdaXmlIndustry) { WRAPPER_NO_CONTRACT; return m_tos->ToXml(pMdaXmlIndustry); }
    MdaXmlElement* ToXml(MdaXmlIndustry* pMdaXmlIndustry, MdaSchemaBase* pXsdViolation) { WRAPPER_NO_CONTRACT; return m_tos->ToXml(pMdaXmlIndustry, pXsdViolation); }
        
private: // Assistant Definitions
    void DefineAssistant(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; m_currentAssistant = name; }  
    void DefineAssistantEnd(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; m_currentAssistant = MdaElemUndefined; }      
    void DefineAssistantInput(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; ASSERT(m_currentAssistant == name); AddExtendElement(name, MdaElemDef(Assistant)); }  
    void DefineAssistantInputEnd(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; ASSERT(m_currentAssistant == name); AddExtendElementEnd(name, MdaElemDef(Assistant)); }      
    void DefineAssistantOutput(MdaElemDeclDef name, MdaElemDeclDef msgName) { WRAPPER_NO_CONTRACT; ASSERT(m_currentAssistant == name); AddExtendElement(msgName, MdaElemDef(AssistantMsgType)); }  
    void DefineAssistantOutputEnd(MdaElemDeclDef name, MdaElemDeclDef msgName) { WRAPPER_NO_CONTRACT; ASSERT(m_currentAssistant == name); AddExtendElementEnd(msgName, MdaElemDef(AssistantMsgType)); }      
    
private: // <xs:*>    
    void DefineSchema() { WRAPPER_NO_CONTRACT; m_tos = m_schemaRootFactory.Create(); }    
    void DefineSchemaEnd() { CONTRACTL {NOTHROW; GC_NOTRIGGER; SO_TOLERANT; MODE_ANY; PRECONDITION(m_stack.GetDepth() == 0); } CONTRACTL_END; }
    void AddElement(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; Push(CreateDeclDef(name, &m_elementFactory)); Push(m_complexTypeFactory.Create()); }
    void AddElementRefType(MdaElemDeclDef name, MdaElemDeclDef type) { WRAPPER_NO_CONTRACT; AddTerminal(CreateDeclDef(name, &m_elementRefTypeFactory)->InitRef(GetDef(type))); }
    void AddElementAny(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; AddTerminal(CreateDeclDef(name, &m_elementAnyFactory)); }
    void AddExtendElement(MdaElemDeclDef name, MdaElemDeclDef type) { WRAPPER_NO_CONTRACT; AddElement(name); AddExtension(type); }
    void AddComplexType(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; Push(CreateDeclDef(name, &m_complexTypeDefFactory)); }   
    void AddExtendType(MdaElemDeclDef name, MdaElemDeclDef type) { WRAPPER_NO_CONTRACT; AddComplexType(name); AddExtension(type); }
    void AddExtension(MdaElemDeclDef type) { WRAPPER_NO_CONTRACT; Push(m_complexContentFactory.Create()); Push(m_extensionFactory.Create()->InitRef(GetDef(type))); }
    void RefElement(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; AddTerminal(m_elementRefFactory.Create()->InitRef(GetDef(name))); }
    void RefGroup(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; AddTerminal(m_groupRefFactory.Create()->InitRef(GetDef(name))); }
    void AddChoice() { WRAPPER_NO_CONTRACT; Push(m_choiceFactory.Create()); }
    void AddGroup(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; Push(CreateDeclDef(name, &m_groupFactory)); }
    void AddSequence(COUNT_T minOccures, COUNT_T maxOccures) { WRAPPER_NO_CONTRACT; Push(m_sequenceFactory.Create()->Initialize(minOccures, maxOccures)); }
    void AddAttribute(MdaAttrDeclDef name, MdaSchemaPrimitive type, BOOL bRequired, LPCWSTR szDefault) 
        { WRAPPER_NO_CONTRACT; AddTerminal(m_attrFactory.Create()->Initialize(name, type, bRequired, szDefault)); }
    
private: // </xs:*>   
    void AddElementEnd(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; Pop()->Verify(MdaSchemaComplexTypeType); Pop()->Verify(MdaSchemaElementType, name); }    
    void AddExtendElementEnd(MdaElemDeclDef name, MdaElemDeclDef type) { WRAPPER_NO_CONTRACT; AddExtensionEnd(type); AddElementEnd(name); }        
    void AddComplexTypeEnd(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; Pop()->Verify(MdaSchemaComplexTypeDefType, name); }
    void AddExtendTypeEnd(MdaElemDeclDef name, MdaElemDeclDef type) { WRAPPER_NO_CONTRACT; AddExtensionEnd(type); AddComplexTypeEnd(name); }   
    void AddExtensionEnd(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; Pop()->Verify(MdaSchemaExtensionType, GetDef(name)); Pop()->Verify(MdaSchemaComplexContentType); }        
    void AddGroupEnd(MdaElemDeclDef name) { WRAPPER_NO_CONTRACT; Pop()->Verify(MdaSchemaGroupType, name); }
    void AddChoiceEnd() { WRAPPER_NO_CONTRACT; Pop()->Verify(MdaSchemaChoiceType); }
    void AddSequenceEnd() { WRAPPER_NO_CONTRACT; Pop()->Verify(MdaSchemaSequenceType); }    
        
private:
    MdaSchemaBase* Pop() { WRAPPER_NO_CONTRACT; ASSERT(m_stack.GetDepth() > 0); MdaSchemaBase* popped = m_tos; m_tos = m_stack.Pop(); return popped; } 
    void AddTerminal(MdaSchemaBase* pSchemaBase) { WRAPPER_NO_CONTRACT; m_tos->AddChild(pSchemaBase); }
    
    template<typename TYPE>
    TYPE* Push(TYPE* pChild)  { WRAPPER_NO_CONTRACT; AddTerminal(pChild); m_stack.Push(m_tos); m_tos = pChild; return pChild; }

    template<typename TYPE>
    TYPE* CreateDeclDef(MdaElemDeclDef name, MdaFactory<TYPE>* m_pFactory)
    {
        WRAPPER_NO_CONTRACT;

        TYPE* pDeclDef = m_pFactory->Create();
        pDeclDef->InitDeclDef(name);

        if (pDeclDef->IsDefinition())
        {
            ASSERT(m_stack.GetDepth() == 0);
            *GetDef(name) = pDeclDef;
        }

        return pDeclDef;
    }
    
    MdaSchemaBase** GetDef(MdaElemDeclDef type) { WRAPPER_NO_CONTRACT; return &m_definitions[type]; }

private:
    friend class ManagedDebuggingAssistants;
    friend class MdaFramework;
    friend class MdaXmlElement;
    friend class MdaXmlAttribute;
    friend class MdaAssistant;
    friend class MdaAssistantSchema;
    friend class MdaAssistantMsgSchema;
    friend class MdaSchemaSchema;
    friend class MdaXPath;
    friend class MdaXmlMessage;
    friend class MdaXmlValidationError;
       
private:
    MdaFactory<MdaSchemaRoot> m_schemaRootFactory;
    MdaFactory<MdaSchemaAttribute> m_attrFactory;
    MdaFactory<MdaSchemaSequence> m_sequenceFactory;
    MdaFactory<MdaSchemaChoice> m_choiceFactory;
    MdaFactory<MdaSchemaGroup> m_groupFactory;
    MdaFactory<MdaSchemaGroupRef> m_groupRefFactory;
    MdaFactory<MdaSchemaComplexTypeDef> m_complexTypeDefFactory;
    MdaFactory<MdaSchemaComplexType> m_complexTypeFactory;
    MdaFactory<MdaSchemaComplexContent> m_complexContentFactory;
    MdaFactory<MdaSchemaElement> m_elementFactory;
    MdaFactory<MdaSchemaElementRef> m_elementRefFactory;
    MdaFactory<MdaSchemaElementRefType> m_elementRefTypeFactory;
    MdaFactory<MdaSchemaExtension> m_extensionFactory;
    MdaFactory<MdaSchemaElementAny> m_elementAnyFactory;
    
private:
    MdaSchemaBase* m_definitions[MdaElemEnd];
    MdaElemDeclDef m_currentAssistant;
    MdaSchemaBase* m_tos;  
    MdaStack<MdaSchemaBase*> m_stack;
};


//
// MdaAssistantMsgSchema 
//
class MdaAssistantSchema : public MdaSchema
{
private:
    MdaAssistantSchema();
    LPCWSTR SetRootAttributes(MdaXmlElement* pXml);
        
private:
    friend class ManagedDebuggingAssistants;
    friend class MdaXmlElement;
    friend class MdaAssistant;    
};


//
// MdaAssistantMsgSchema 
//
class MdaAssistantMsgSchema : public MdaSchema
{
private:
    MdaAssistantMsgSchema();
    LPCWSTR SetRootAttributes(MdaXmlElement* pXml);
        
private:
    friend class ManagedDebuggingAssistants;
    friend class MdaXmlElement;
    friend class MdaAssistant;    
};


//
// MdaSchemaSchema 
//
class MdaSchemaSchema : public MdaSchema
{
private:
    MdaSchemaSchema();
    LPCWSTR SetRootAttributes(MdaXmlElement* pXml);
    
private:
    friend class ManagedDebuggingAssistants;
    friend class MdaXmlElement;
    friend class MdaAssistant;    
};


//
// MdaQuery
//

BOOL IsJustMyCode(MethodDesc* pMethodDesc);

class MdaQuery
{
private:
    class CompiledQuery
    {
    public:
        CompiledQuery();

    public:
        BOOL Test(MethodDesc* pMethodDesc);
        BOOL Test(FieldDesc* pFieldDesc);
        BOOL Test(MethodTable* pMethodTable);
        
    public:
        void SetName(LPCWSTR name);
        void SetNestedTypeName(LPCWSTR name);
        void SetMemberName(LPCWSTR name) { WRAPPER_NO_CONTRACT; m_sszMember.Set(name); }
        void SetAnyMember();
        void SetAnyType();
        void SetJustMyCode() { LIMITED_METHOD_CONTRACT; m_bJustMyCode = TRUE; }
       
    private:
        BOOL Test(SString* psszName, MethodTable* pMethodTable);
        
    private:
        friend class MdaQuery;
        
    private:
        BOOL m_bAnyMember;
        BOOL m_bAnyType;
        BOOL m_bJustMyCode;
        StackSString m_sszFullname;
        StackSString m_sszMember;
    };

public:
    class CompiledQueries
    {
    public:
        CompiledQueries() { LIMITED_METHOD_CONTRACT; }

    public:
        BOOL Test(MethodDesc* pMethodDesc);
        BOOL Test(FieldDesc* pFieldDesc);
        BOOL Test(MethodTable* pMethodTable);

    private:
        friend class MdaQuery;
        
    private:
        CompiledQuery* AddQuery();
        
    private:
        InlineSArray<CompiledQuery*, 10> m_queries;
        MdaFactory<CompiledQuery> m_factory;
    };
    
public:
    static void Compile(MdaXmlElement* pXmlFilter, CompiledQueries* pCompiledQueries);
    
private:
    friend class ManagedDebuggingAssistants;
    
private:
    class Compiler
    {
    private:
        friend class CompiledQuery;
        friend class MdaQuery;
        
    private:
        BOOL Compile(SString* sszQuery, CompiledQuery* pCompiledQuery);

        typedef enum 
        {
            //
            // TOKENS
            //
            MdaFilterIdentifier          = 0x0001,
            MdaFilterDot                 = 0x0002,
            MdaFilterPlus                = 0x0004,
            MdaFilterAstrix              = 0x0008,
            MdaFilterColon               = 0x0010,
            MdaFilterEnd                 = 0x4000,
        } 
        Token;

    //
    // LEXIFIER 
    //
    private:
        Token LexAToken();
        void NextToken()  { WRAPPER_NO_CONTRACT; m_currentToken = LexAToken(); }
        BOOL TokenIs(Token token) { LIMITED_METHOD_CONTRACT; return !!(m_currentToken & token); }
        BOOL TokenIs(int token) { LIMITED_METHOD_CONTRACT; return TokenIs((Token)token); }
        LPCWSTR GetIdentifier() { WRAPPER_NO_CONTRACT; return m_identifier.GetUnicode(); }
        
    //
    // PRODUCTIONS
    //
    private: 
        
        BOOL NAME(CompiledQuery* pAst);
        // '*'
        // id
        // id '.' NAME
        // id '+' NESTNAME 
        // id ':' ':' MEMBERNAME
        
        BOOL NESTNAME(CompiledQuery* pAst);
        // id '+' NESTNAME  
        // id ':' ':' MEMBERNAME
        
        BOOL MEMBERNAME(CompiledQuery* pAst);
        // '*'
        // id
        
    private:
        SString::CIterator m_itr;
        StackSString m_identifier;
        Token m_currentToken;
    };    
};


//
// MdaConfigFactory 
//
class MdaConfigFactory : public IXMLNodeFactory 
{
private:
    friend class ManagedDebuggingAssistants;
    
private:
    static MdaXmlElement* ParseXmlStream(MdaXmlIndustry* pXmlIndustry, LPCWSTR szXmlStream);

private:
    MdaConfigFactory(MdaXmlElement* pXmlRoot, BOOL bDeveloperSettings = FALSE) { WRAPPER_NO_CONTRACT; m_bParse = !bDeveloperSettings; m_pMdaXmlElement = NULL; m_stack.Push(pXmlRoot); }

public:
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) { WRAPPER_NO_CONTRACT; return S_OK; }
    ULONG STDMETHODCALLTYPE AddRef() { WRAPPER_NO_CONTRACT; return 0; }
    ULONG STDMETHODCALLTYPE Release() { WRAPPER_NO_CONTRACT; return 0; }
    
public:
    HRESULT STDMETHODCALLTYPE NotifyEvent( 
        IXMLNodeSource* pSource,
        XML_NODEFACTORY_EVENT iEvt);

    HRESULT STDMETHODCALLTYPE BeginChildren( 
        IXMLNodeSource* pSource,
        XML_NODE_INFO* pNodeInfo);

    HRESULT STDMETHODCALLTYPE EndChildren( 
        IXMLNodeSource* pSource,
        BOOL fEmptyNode,
        XML_NODE_INFO* pNodeInfo);
    
    HRESULT STDMETHODCALLTYPE Error( 
        IXMLNodeSource* pSource,
        HRESULT hrErrorCode,
        USHORT cNumRecs,
        XML_NODE_INFO** apNodeInfo);
    
    HRESULT STDMETHODCALLTYPE CreateNode( 
        IXMLNodeSource* pSource,
        PVOID pNodeParent,
        USHORT cNumRecs,
        XML_NODE_INFO** apNodeInfo);
     
private:
    BOOL m_bParse;
    MdaXmlElement* m_pMdaXmlElement;
    MdaStack<MdaXmlElement*> m_stack;
};

#pragma warning(pop)

#include "mda.inl"

#endif
#endif

