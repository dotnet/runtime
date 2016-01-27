// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// MDA_DEFINE_ASSISTANT_ENUMERATION
//
#ifdef MDA_DEFINE_ASSISTANT_ENUMERATION
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) MdaElemDef(NAME),
#endif

//
// MDA_ASSISTANT_NAME
//
#ifdef MDA_ASSISTANT_NAME
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) L#NAME,
#endif

//
// MDA_ASSISTANT_ABBR
//
#ifdef MDA_ASSISTANT_ABBR
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) ABBR,
#endif

//
// MDA_ELEMENT_DEFINITION_STRING
//
#ifdef MDA_ASSISTANT_STRING
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) L#NAME
#endif

//
// MDA_ASSISTANT_HEAP_RAW
//
#ifdef MDA_ASSISTANT_HEAP_RAW
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) Mda##NAME m_mda##NAME;   
#endif

//
// MDA_ASSISTANT_STATIC_INIT
//
#ifdef MDA_ASSISTANT_STATIC_INIT
#define MDA_DEFINE_ASSISTANT(NAME, ABBR)                                        \
    {                                                                           \
        MdaElemDef(NAME),       /* m_assistantDeclDef */                        \
        MdaElemDef(NAME##Msg),  /* m_assistantMsgDeclDef */                     \
        0                       /* m_bSuppressDialog */                         \
    },                                                                          
#endif 

#ifdef MDA_VALIDATE_MEMBER_LAYOUT
// See MDA_ASSISTANT_BASE_MEMBERS for details on why we're asserting that these fields have matching offsets.
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) \
    static_assert_no_msg(offsetof(MdaAssistant, m_assistantDeclDef)     == offsetof(Mda##NAME, m_assistantDeclDef));    \
    static_assert_no_msg(offsetof(MdaAssistant, m_assistantMsgDeclDef)  == offsetof(Mda##NAME, m_assistantMsgDeclDef)); \
    static_assert_no_msg(offsetof(MdaAssistant, m_bSuppressDialog)      == offsetof(Mda##NAME, m_bSuppressDialog));     
#endif

//
// MDA_ASSISTANT_IS_SWITCH
//
#ifdef MDA_ASSISTANT_IS_SWITCH
#define MDA_DEFINE_INPUT_AS_SWITCH(ASSISTANT) true,                            
#define MDA_DEFINE_INPUT(ASSISTANT) false,                                     
#endif

//
// MDA_DEFINE_GROUPS
//
#ifdef MDA_DEFINE_GROUPS
#define MDA_GROUP_DEFINITION(NAME)                                                      \
    pGroup = arrayFactory.Create();                                                     \
    aGroups.Append(pGroup);    
#define MDA_GROUP_MEMBER(NAME) pGroup->Append(MdaElemDef(NAME));
#endif

//
// MDA_ACTIVATE_GROUPS
//
#ifdef MDA_ACTIVATE_GROUPS
#define MDA_GROUP_DEFINITION(NAME)                                                      \
    if (sszActivationMechanism.EqualsCaseInsensitive(L#NAME))                           \
        ActivateGroup(L#NAME, aGroups[cGroup], &mdaXmlPairs);                           \
    cGroup++;
#endif

//
// MDA_ACTIVATE_SINGLTON_GROUPS
//
#ifdef MDA_ACTIVATE_SINGLTON_GROUPS
#define MDA_DEFINE_ASSISTANT(NAME, ABBR)                                        \
    if (sszActivationMechanism.EqualsCaseInsensitive(L#NAME))                           \
        mdaXmlPairs.Set(ToLowerFirstChar(L#NAME, &sstringFactory),                      \
            GetSwitchActivationXml(MdaElemDef(NAME)));
#endif

 
//
// MDA_ELEMENT_DEFINITION_ENUMERATION
//
#ifdef MDA_ELEMENT_DEFINITION_ENUMERATION
#define MDA_XSD_DEFINE_ELEMENT(NAME) MdaElemDef(NAME),
#define MDA_XSD_TYPEDEF_ELEMENT(NAME, TYPE) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_EXTEND_ELEMENT(NAME, TYPE)  MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_EXTEND_TYPE(NAME, TYPE)  MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_GROUP(NAME) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_TYPE(NAME) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) MDA_XSD_DEFINE_ELEMENT(NAME##Msg)
MDA_XSD_DEFINE_ELEMENT(AssistantConfigGroup)
MDA_XSD_DEFINE_ELEMENT(AssistantMsgGroup)
#endif

//
// MDA_ELEMENT_DECLARAION_ENUMERATION
//
#ifdef MDA_ELEMENT_DECLARAION_ENUMERATION
#define MDA_XSD_ELEMENT(NAME) MdaElemDecl(NAME),
#define MDA_XSD_ELEMENT_REFTYPE(NAME, TYPE) MDA_XSD_ELEMENT(NAME)
#define MDA_XSD_ELEMENT_EXTEND_TYPE(NAME, TYPE) MDA_XSD_ELEMENT(NAME)
#define MDA_XSD_ELEMENT_ANY(NAME) MDA_XSD_ELEMENT(NAME)
#endif

//
// MDA_ATTRIBUTE_DECLARATION_ENUMERATION
//
#ifdef MDA_ATTRIBUTE_DECLARATION_ENUMERATION
#define MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE) MdaAttrDecl(NAME),
#define MDA_XSD_ATTRIBUTE_REQ(NAME, TYPE) MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE)
#define MDA_XSD_ATTRIBUTE_DEFAULT(NAME, TYPE, DEFAULT) MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE)
#endif

//
// MDA_MAP_ASSISTANT_DEFINITION_TO_NAME
//
#ifdef MDA_MAP_ASSISTANT_DEFINITION_TO_NAME
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) g_arElementNames[i++] = ToLowerFirstChar(L#NAME);
#endif

//
// MDA_MAP_ELEMENT_DEFINITION_TO_NAME
//
#ifdef MDA_MAP_ELEMENT_DEFINITION_TO_NAME
#define MDA_XSD_DEFINE_ELEMENT(NAME) g_arElementNames[i++] = ToLowerFirstChar(L#NAME);
#define MDA_XSD_TYPEDEF_ELEMENT(NAME, TYPE) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_EXTEND_ELEMENT(NAME, TYPE)  MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_EXTEND_TYPE(NAME, TYPE)  MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_GROUP(NAME) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_TYPE(NAME) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) MDA_XSD_DEFINE_ELEMENT(NAME##Msg)
MDA_XSD_DEFINE_ELEMENT(AssistantConfigGroup)
MDA_XSD_DEFINE_ELEMENT(AssistantMsgGroup)
#endif

//
// MDA_MAP_ELEMENT_DECLARATION_TO_NAME
//
#ifdef MDA_MAP_ELEMENT_DECLARATION_TO_NAME
#define MDA_XSD_ELEMENT(NAME) g_arElementNames[i++] = ToLowerFirstChar(L#NAME);
#define MDA_XSD_ELEMENT_REFTYPE(NAME, TYPE) MDA_XSD_ELEMENT(NAME)
#define MDA_XSD_ELEMENT_EXTEND_TYPE(NAME, TYPE) MDA_XSD_ELEMENT(NAME)
#define MDA_XSD_ELEMENT_ANY(NAME) MDA_XSD_ELEMENT(NAME)
#endif

//
// MDA_MAP_ATTRIBUTE_DECLARATION_TO_NAME
//
#ifdef MDA_MAP_ATTRIBUTE_DECLARATION_TO_NAME
#define MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE) g_arAttributeNames[i++] = ToLowerFirstChar(L#NAME);
#define MDA_XSD_ATTRIBUTE_REQ(NAME, TYPE) MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE)
#define MDA_XSD_ATTRIBUTE_DEFAULT(NAME, TYPE, DEFAULT) MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE)
#endif

//
// MDA_MAP_ELEMENT_NAME_TO_DEFINITION
//
#ifdef MDA_MAP_ASSISTANT_NAME_TO_DEFINITION
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) g_pHtElementType->Set(g_arElementNames[MdaElemDef(NAME)], MdaElemDef(NAME));
#endif

//
// MDA_MAP_ELEMENT_NAME_TO_DEFINITION
//
#ifdef MDA_MAP_ELEMENT_NAME_TO_DEFINITION
#define MDA_XSD_DEFINE_ELEMENT(NAME) g_pHtElementType->Set(g_arElementNames[MdaElemDef(NAME)], MdaElemDef(NAME));
#define MDA_XSD_TYPEDEF_ELEMENT(NAME, TYPE) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_EXTEND_ELEMENT(NAME, TYPE)  MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_EXTEND_TYPE(NAME, TYPE)  MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_GROUP(NAME) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_XSD_DEFINE_TYPE(NAME) MDA_XSD_DEFINE_ELEMENT(NAME)
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) MDA_XSD_DEFINE_ELEMENT(NAME##Msg)
MDA_XSD_DEFINE_ELEMENT(AssistantConfigGroup)
MDA_XSD_DEFINE_ELEMENT(AssistantMsgGroup)
#endif

//
// MDA_MAP_ELEMENT_NAME_TO_DECLARATION
//
#ifdef MDA_MAP_ELEMENT_NAME_TO_DECLARATION
#define MDA_XSD_ELEMENT(NAME) g_pHtElementType->Set(g_arElementNames[MdaElemDecl(NAME)], MdaElemDecl(NAME));
#define MDA_XSD_ELEMENT_REFTYPE(NAME, TYPE) MDA_XSD_ELEMENT(NAME)
#define MDA_XSD_ELEMENT_EXTEND_TYPE(NAME, TYPE) MDA_XSD_ELEMENT(NAME)
#define MDA_XSD_ELEMENT_ANY(NAME) MDA_XSD_ELEMENT(NAME)
#endif

//
// MDA_MAP_ATTRIBUTE_NAME_TO_DECLARATION
//
#ifdef MDA_MAP_ATTRIBUTE_NAME_TO_DECLARATION
#define MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE) g_pHtAttributeType->Set(g_arAttributeNames[MdaAttrDecl(NAME)], MdaAttrDecl(NAME));
#define MDA_XSD_ATTRIBUTE_REQ(NAME, TYPE) MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE)
#define MDA_XSD_ATTRIBUTE_DEFAULT(NAME, TYPE, DEFAULT) MDA_XSD_ATTRIBUTE_OPT(NAME, TYPE)
#endif

//
// MDA_ASSISTANT_CREATION
//
#ifdef MDA_ASSISTANT_CREATION
#define MDA_DEFINE_ASSISTANT(ASSISTANT, ABBR)                                                   \
    if (mdaXmlPairs.Get(MdaSchema::g_arElementNames[MdaElemDef(ASSISTANT)], &pXmlAssistant))    \
    {                                                                                           \
        if (MdaAssistant::IsAssistantActive(pXmlAssistant))                                     \
        {                                                                                       \
            Mda##ASSISTANT* pAssistant = &g_mdaStaticHeap.m_mda##ASSISTANT;                     \
            pAssistant->AsMdaAssistant()->Initialize(pXmlAssistant);                            \
            pAssistant->Initialize(pXmlAssistant);                                              \
            g_mdaStaticHeap.m_assistants[MdaElemDef(ASSISTANT)] = pAssistant->AsMdaAssistant(); \
        }                                                                                       \
    }
#endif


//
// MDA_DEFINE_SCHEMA_SCHEMA
//
#ifdef MDA_DEFINE_SCHEMA_SCHEMA
#define MDA_DEFINE_SCHEMA
#define MDA_XSD_ASSISTANT_SCHEMA() if (FALSE) {
#define MDA_XSD_ASSISTANT_SCHEMA_END() }
#define MDA_XSD_SCHEMA_SCHEMA() DefineSchema();
#define MDA_XSD_SCHEMA_SCHEMA_END() DefineSchemaEnd();
#endif

//
// MDA_DEFINE_ASSISTANT_SCHEMA
//
#ifdef MDA_DEFINE_ASSISTANT_SCHEMA
#define MDA_DEFINE_SCHEMA
#define MDA_XSD_SCHEMA_SCHEMA() if (FALSE) {
#define MDA_XSD_SCHEMA_SCHEMA_END() }
#define MDA_XSD_OUTPUT_ONLY() if (FALSE) {
#define MDA_XSD_OUTPUT_ONLY_END() }
#define MDA_DEFINE_INPUT(ASSISTANT) DefineAssistantInput(MdaElemDef(ASSISTANT));
#define MDA_DEFINE_INPUT_END(ASSISTANT) DefineAssistantInputEnd(MdaElemDef(ASSISTANT));
#define MDA_DEFINE_OUTPUT(ASSISTANT) if (FALSE) {
#define MDA_DEFINE_OUTPUT_END(ASSISTANT) }
#define MDA_XSD_ASSISTANT_SCHEMA() DefineSchema();
#endif

//
// MDA_DEFINE_MDA_ASSISTANT_CONFIG_GROUP
//
#ifdef MDA_DEFINE_MDA_ASSISTANT_CONFIG_GROUP
#define MDA_XSD_ASSISTANT_SCHEMA() AddGroup(MdaElemDef(AssistantConfigGroup)); AddSequence(0, 1);
#define MDA_XSD_ASSISTANT_SCHEMA_END()  AddSequenceEnd(); AddGroupEnd(MdaElemDef(AssistantConfigGroup));
#define MDA_DEFINE_INPUT(ASSISTANT) AddSequence(0, 1); RefElement(MdaElemDef(ASSISTANT)); AddSequenceEnd();                              
#endif

//
// MDA_DEFINE_ASSISTANT_MSG_SCHEMA
//
#ifdef MDA_DEFINE_ASSISTANT_MSG_SCHEMA
#define MDA_DEFINE_SCHEMA
#define MDA_XSD_SCHEMA_SCHEMA() if (FALSE) {
#define MDA_XSD_SCHEMA_SCHEMA_END() }
#define MDA_XSD_INPUT_ONLY() if (FALSE) {
#define MDA_XSD_INPUT_ONLY_END() }
#define MDA_DEFINE_INPUT(ASSISTANT) if (FALSE) {
#define MDA_DEFINE_INPUT_END(ASSISTANT) }
#define MDA_DEFINE_OUTPUT(ASSISTANT) DefineAssistantOutput(MdaElemDef(ASSISTANT), MdaElemDef(ASSISTANT##Msg));
#define MDA_DEFINE_OUTPUT_END(ASSISTANT) DefineAssistantOutputEnd(MdaElemDef(ASSISTANT), MdaElemDef(ASSISTANT##Msg));
#define MDA_XSD_ASSISTANT_SCHEMA() DefineSchema();
#endif

//
// MDA_DEFINE_MDA_ASSISTANT_MSG_GROUP
//
#ifdef MDA_DEFINE_MDA_ASSISTANT_MSG_GROUP
#define MDA_XSD_ASSISTANT_SCHEMA() AddGroup(MdaElemDef(AssistantMsgGroup)); AddSequence(0, 1);
#define MDA_XSD_ASSISTANT_SCHEMA_END()  AddSequenceEnd(); AddGroupEnd(MdaElemDef(AssistantMsgGroup)); DefineSchemaEnd();
#define MDA_DEFINE_OUTPUT(ASSISTANT) RefElement(MdaElemDef(ASSISTANT##Msg));
#endif

//
// MDA_DEFINE_SCHEMA
//
#ifdef MDA_DEFINE_SCHEMA

// Assistants
#define MDA_DEFINE_ASSISTANT(NAME, ABBR) DefineAssistant(MdaElemDef(NAME));
#define MDA_DEFINE_ASSISTANT_END(NAME) DefineAssistantEnd(MdaElemDef(NAME));

// Attributes
#define MDA_XSD_ATTRIBUTE_OPT(NAME,TYPE) AddAttribute(MdaAttrDecl(NAME), MdaSchemaPrimitive##TYPE, FALSE, NULL);
#define MDA_XSD_ATTRIBUTE__OPT(NAME,TYPE) AddAttribute(MdaAttrDecl(NAME), MdaSchemaPrimitive##TYPE, FALSE, NULL);
#define MDA_XSD_ATTRIBUTE_REQ(NAME,TYPE) AddAttribute(MdaAttrDecl(NAME), MdaSchemaPrimitive##TYPE, TRUE, NULL);
#define MDA_XSD_ATTRIBUTE__REQ(NAME,TYPE) AddAttribute(MdaAttrDecl(NAME), MdaSchemaPrimitive##TYPE, TRUE, NULL);
#define MDA_XSD_ATTRIBUTE_DEFAULT(NAME,TYPE,DEFAULT) AddAttribute(MdaAttrDecl(NAME), MdaSchemaPrimitive##TYPE, FALSE, DEFAULT);
#define MDA_XSD_ATTRIBUTE__DEFAULT(NAME,TYPE,DEFAULT) AddAttribute(MdaAttrDecl(NAME), MdaSchemaPrimitive##TYPE, FALSE, DEFAULT);

// Definitions
#define MDA_XSD_DEFINE_ELEMENT(NAME)  AddElement(MdaElemDef(NAME));
#define MDA_XSD_DEFINE_ELEMENT_END(NAME) AddElementEnd(MdaElemDef(NAME));
#define MDA_XSD_DEFINE_TYPE(NAME) AddComplexType(MdaElemDef(NAME));
#define MDA_XSD_DEFINE_TYPE_END(NAME) AddComplexTypeEnd(MdaElemDef(NAME));
#define MDA_XSD_DEFINE_EXTEND_TYPE(NAME, TYPE) AddExtendType(MdaElemDef(NAME), MdaElemDef(TYPE));
#define MDA_XSD_DEFINE_EXTEND_TYPE_END(NAME, TYPE) AddExtendTypeEnd(MdaElemDef(NAME), MdaElemDef(TYPE));
#define MDA_XSD_DEFINE_EXTEND_ELEMENT(NAME, TYPE) AddExtendElement(MdaElemDef(NAME), MdaElemDef(TYPE));
#define MDA_XSD_DEFINE_EXTEND_ELEMENT_END(NAME, TYPE) AddExtendElementEnd(MdaElemDef(NAME), MdaElemDef(TYPE));
#define MDA_XSD_TYPEDEF_ELEMENT(NAME, TYPE) AddElementRefType(MdaElemDef(NAME), MdaElemDef(TYPE));

// Declarations
#define MDA_XSD_ELEMENT(NAME) AddElement(MdaElemDecl(NAME));
#define MDA_XSD__ELEMENT(NAME) AddElement(MdaElemDecl(NAME));
#define MDA_XSD_ELEMENT_END(NAME) AddElementEnd(MdaElemDecl(NAME));
#define MDA_XSD_ELEMENT_ANY(NAME) AddElementAny(MdaElemDecl(NAME));
#define MDA_XSD_ELEMENT__ANY(NAME) AddElementAny(MdaElemDecl(NAME));
#define MDA_XSD_ELEMENT_REF(NAME) RefElement(MdaElemDef(NAME));
#define MDA_XSD_ELEMENT_REFTYPE(NAME, TYPE) AddElementRefType(MdaElemDecl(NAME), MdaElemDef(TYPE));
#define MDA_XSD_ELEMENT__REFTYPE(NAME, TYPE) AddElementRefType(MdaElemDecl(NAME), MdaElemDef(TYPE));
#define MDA_XSD_ELEMENT_EXTEND_TYPE(NAME, TYPE) AddExtendElement(MdaElemDecl(NAME), MdaElemDef(TYPE));
#define MDA_XSD_ELEMENT_EXTEND__TYPE(NAME, TYPE) AddExtendElement(MdaElemDecl(NAME), MdaElemDef(TYPE));
#define MDA_XSD_ELEMENT_EXTEND_TYPE_END(NAME, TYPE) AddExtendElementEnd(MdaElemDecl(NAME), MdaElemDef(TYPE));

// Patterns
#define MDA_XSD_CHOICE() AddChoice();
#define MDA_XSD_CHOICE_END() AddChoiceEnd();
#define MDA_XSD_GROUP(NAME) AddGroup(MdaElemDef(NAME));
#define MDA_XSD_GROUP_END(NAME) AddGroupEnd(MdaElemDef(NAME));
#define MDA_XSD_GROUP_REF(NAME) RefGroup(MdaElemDef(NAME));
#define MDA_XSD_ONCE() AddSequence(1, 1);
#define MDA_XSD_ONCE_END() AddSequenceEnd();
#define MDA_XSD_OPTIONAL() AddSequence(0, 1);
#define MDA_XSD_OPTIONAL_END() AddSequenceEnd();
#define MDA_XSD_PERIODIC() AddSequence(0, -1);
#define MDA_XSD_PERIODIC_END() AddSequenceEnd();
#endif

#ifndef MDA_DEFINE_INPUT_AS_SWITCH
#ifdef MDA_DEFINE_INPUT
#define MDA_DEFINE_INPUT_AS_SWITCH(ASSISTANT) MDA_DEFINE_INPUT(ASSISTANT) MDA_DEFINE_INPUT_END(ASSISTANT) 
#endif
#endif

#include "mdamacroscrubber.inl"

#include "mdagroups.inl"

//
// Standard Element Definitions
//
MDA_XSD_ASSISTANT_SCHEMA()


#include "mdaassistantschemas.inl"

    //
    // MDA Output Framework Defintions
    //
    MDA_XSD_OUTPUT_ONLY()
        
        // MdaAssistantMsgGroup
        // MDA_XSD_GROUP(AssistantMsgGroup)
        // MDA_XSD_GROUP_END(AssistantMsgGroup)

        // Output Root
        MDA_XSD_DEFINE_TYPE(Msg)
            MDA_XSD_GROUP_REF(AssistantMsgGroup)
        MDA_XSD_DEFINE_TYPE_END(Msg)   
        
        // Output Root
        MDA_XSD_DEFINE_TYPE(AssistantMsgType)
            //MDA_XSD_ATTRIBUTE_REQ(Documentation, SString)
        MDA_XSD_DEFINE_TYPE_END(AssistantMsgType)   
        
    MDA_XSD_OUTPUT_ONLY_END()



    //
    // MDA Input Framework Defintions
    //
    MDA_XSD_INPUT_ONLY()
        
        // MdaAssistantConfigGroup
        // MDA_XSD_GROUP(AssistantConfigGroup)
        // MDA_XSD_GROUP_END(AssistantConfigGroup)

        // MdaConfigType
        MDA_XSD_DEFINE_TYPE(MdaConfigType) 
            MDA_XSD_ONCE()
                MDA_XSD_OPTIONAL()
                    MDA_XSD_ELEMENT(Assistants)
                        MDA_XSD_GROUP_REF(AssistantConfigGroup)
                    MDA_XSD_ELEMENT_END(Assistants)
                MDA_XSD_OPTIONAL_END()  
            MDA_XSD_ONCE_END()
        MDA_XSD_DEFINE_TYPE_END(MdaConfigType)  

        // AppConfig 
        MDA_XSD_DEFINE_EXTEND_ELEMENT(MdaAppConfig, MdaConfigType)    
        MDA_XSD_DEFINE_EXTEND_ELEMENT_END(MdaAppConfig, MdaConfigType)    

        // MdaConfig 
        MDA_XSD_DEFINE_EXTEND_ELEMENT(MdaConfig, MdaConfigType)    
        MDA_XSD_DEFINE_EXTEND_ELEMENT_END(MdaConfig, MdaConfigType)    
        
        // MdaGroupConfig
        MDA_XSD_DEFINE_ELEMENT(MdaGroupConfig)   
            MDA_XSD_PERIODIC()
                MDA_XSD_ELEMENT(Group)
                    MDA_XSD_ONCE()
                        MDA_XSD_PERIODIC()
                            MDA_XSD_ELEMENT(GroupReference)
                                MDA_XSD_ATTRIBUTE__REQ(Name, SString)
                            MDA_XSD_ELEMENT_END(GroupReference)                            
                        MDA_XSD_PERIODIC_END()                       
                        MDA_XSD_OPTIONAL()
                            MDA_XSD_GROUP_REF(AssistantConfigGroup)
                        MDA_XSD_OPTIONAL_END()                          
                    MDA_XSD_ONCE_END()
                    MDA_XSD_ATTRIBUTE__REQ(Name, SString)
                MDA_XSD_ELEMENT_END(Group)
            MDA_XSD_PERIODIC_END()
        MDA_XSD_DEFINE_ELEMENT_END(MdaGroupConfig)

        // Mda Assistant
        MDA_XSD_DEFINE_TYPE(Assistant)
            MDA_XSD_ATTRIBUTE_DEFAULT(Enable, BOOL, W("true"))
        MDA_XSD_DEFINE_TYPE_END(Assistant)

        // Dummy
        MDA_XSD_DEFINE_ELEMENT(Dummy) 
            MDA_XSD_ATTRIBUTE_OPT(SuppressDialog, BOOL)
        MDA_XSD_DEFINE_ELEMENT_END(Dummy) 
        
    MDA_XSD_INPUT_ONLY_END()
    

MDA_XSD_ASSISTANT_SCHEMA_END()


//
// Schema Infrastructure 
//
MDA_XSD_SCHEMA_SCHEMA()

    // Schema Schema Definition
    MDA_XSD_DEFINE_ELEMENT(Schema)
        MDA_XSD_PERIODIC()
            MDA_XSD_CHOICE()
                MDA_XSD_ELEMENT_REF(ComplexType)               
                MDA_XSD_ELEMENT_REF(Group)               
                MDA_XSD_ELEMENT_REF(Element)        
            MDA_XSD_CHOICE_END()
        MDA_XSD_PERIODIC_END()
        MDA_XSD_ATTRIBUTE_OPT(TargetNamespace, SString)
        MDA_XSD_ATTRIBUTE_OPT(Xmlns, SString)
    MDA_XSD_DEFINE_ELEMENT_END(Schema)

    // Element
    MDA_XSD_DEFINE_ELEMENT(Element)
        MDA_XSD_OPTIONAL()                                            
            MDA_XSD_ELEMENT_REF(ComplexType)
        MDA_XSD_OPTIONAL_END()
        
        MDA_XSD_ATTRIBUTE__OPT(Name, SString)
        MDA_XSD_ATTRIBUTE__OPT(Ref, SString)
        MDA_XSD_ATTRIBUTE__OPT(Type, SString)
    MDA_XSD_DEFINE_ELEMENT_END(Element)
                
    // ComplexType
    MDA_XSD_DEFINE_ELEMENT(ComplexType)                  
        MDA_XSD_OPTIONAL()   
            MDA_XSD_CHOICE()
                MDA_XSD_GROUP_REF(ElementContent)
                MDA_XSD_ELEMENT_REF(ComplexContent)
            MDA_XSD_CHOICE_END()
        MDA_XSD_OPTIONAL_END()      
        
        MDA_XSD_ATTRIBUTE__OPT(Name, SString)
    MDA_XSD_DEFINE_ELEMENT_END(ComplexType)
    
    // ComplexContent
    MDA_XSD_DEFINE_ELEMENT(ComplexContent)
        MDA_XSD_ONCE()
            MDA_XSD_ELEMENT_REF(Extension)
        MDA_XSD_ONCE_END()
    MDA_XSD_DEFINE_ELEMENT_END(ComplexContent)

    // Extension
    MDA_XSD_DEFINE_ELEMENT(Extension)                                
        MDA_XSD_GROUP_REF(ElementContent) 
        
        MDA_XSD_ATTRIBUTE_REQ(Base, SString)
    MDA_XSD_DEFINE_ELEMENT_END(Extension)

    // ElementContent
    MDA_XSD_GROUP(ElementContent)
        MDA_XSD_OPTIONAL()   
            MDA_XSD_GROUP_REF(PatternRoot)
            
            MDA_XSD_PERIODIC()
                MDA_XSD_ELEMENT_REF(Attribute)               
            MDA_XSD_PERIODIC_END()                        
        MDA_XSD_OPTIONAL_END()              
    MDA_XSD_GROUP_END(ElementContent)

    // PatternRoot
    MDA_XSD_GROUP(PatternRoot)
        MDA_XSD_OPTIONAL()
            MDA_XSD_CHOICE()
                MDA_XSD_ELEMENT_REF(Choice)
                MDA_XSD_ELEMENT_REF(Sequence)
                MDA_XSD_ELEMENT_REF(Group)
            MDA_XSD_CHOICE_END()
        MDA_XSD_OPTIONAL_END()
    MDA_XSD_GROUP_END(PatternRoot)

    // PeriodicPattern
    MDA_XSD_GROUP(PeriodicPattern)
        MDA_XSD_PERIODIC()
            MDA_XSD_CHOICE()
                MDA_XSD_ELEMENT_REF(Element)
                MDA_XSD_ELEMENT_REF(Choice)
                MDA_XSD_ELEMENT_REF(Sequence)
                MDA_XSD_ELEMENT_REF(Group)
            MDA_XSD_CHOICE_END()
        MDA_XSD_PERIODIC_END()
    MDA_XSD_GROUP_END(PeriodicPattern)

    // Sequence
    MDA_XSD_DEFINE_ELEMENT(Sequence)                        
        MDA_XSD_GROUP_REF(PeriodicPattern)
        
        MDA_XSD_ATTRIBUTE_OPT(MinOccurs, SString)
        MDA_XSD_ATTRIBUTE_OPT(MaxOccurs, SString)
    MDA_XSD_DEFINE_ELEMENT_END(Sequence)

    // Choice
    MDA_XSD_DEFINE_ELEMENT(Choice)
        MDA_XSD_GROUP_REF(PeriodicPattern)                                    
    MDA_XSD_DEFINE_ELEMENT_END(Choice)
    
    // Group
    MDA_XSD_DEFINE_ELEMENT(Group)
        MDA_XSD_GROUP_REF(PatternRoot)    
        
        MDA_XSD_ATTRIBUTE__OPT(Name, SString)
        MDA_XSD_ATTRIBUTE_OPT(Ref, SString)
    MDA_XSD_DEFINE_ELEMENT_END(Group)

    // Attribute
    MDA_XSD_DEFINE_ELEMENT(Attribute)
        MDA_XSD_ATTRIBUTE__REQ(Name, SString)
        MDA_XSD_ATTRIBUTE_REQ(Type, SString)
        MDA_XSD_ATTRIBUTE_OPT(Use, SString)
        MDA_XSD_ATTRIBUTE_OPT(Default, SString)
    MDA_XSD_DEFINE_ELEMENT_END(Attribute)                
    
MDA_XSD_SCHEMA_SCHEMA_END()       

#include "mdamacroscrubber.inl"

