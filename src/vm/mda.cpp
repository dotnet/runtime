// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"
#include "eeconfig.h"
#include "eeconfigfactory.h"
#include "corhlpr.h"
#include <xmlparser.h>
#include <mscorcfg.h>
#include <holder.h>
#include <dbginterface.h>
#include "wrappers.h"
#include "mda.h"
#include "mdaassistants.h"
#include "sstring.h"
#include "util.hpp"
#include "debugdebugger.h"

#ifdef MDA_SUPPORTED

//
// MdaHashtable
//

BOOL MdaLockOwner(LPVOID) { LIMITED_METHOD_CONTRACT; return TRUE; }

BOOL IsJustMyCode(MethodDesc* pMethodDesc)
{
    CONTRACT(BOOL)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    if (!ManagedDebuggingAssistants::IsManagedDebuggerAttached())
        return TRUE;

    BOOL bIsJMC = FALSE;

    EX_TRY
    {
        if (g_pDebugInterface && g_pDebugInterface->IsJMCMethod(pMethodDesc->GetModule(), pMethodDesc->GetMemberDef()))
            bIsJMC = TRUE;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    RETURN bIsJMC;
}


//
// ManagedDebuggingAssistants
//

const bool g_mdaAssistantIsSwitch[] =
{
#define MDA_ASSISTANT_IS_SWITCH
#include "mdaschema.inl"
#undef MDA_ASSISTANT_IS_SWITCH
    false
};

void ManagedDebuggingAssistants::Initialize()
{
    CONTRACT_VOID
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACT_END;

    EX_TRY
    {
        //
        // Initialize
        //
        m_pSwitchActivationXml = NULL;
        m_pMdaXmlIndustry = new MdaXmlIndustry();

        MdaSchema::Initialize();

        //
        // Create AssistantSchema
        //
        m_pAssistantSchema = new MdaAssistantSchema();

        //
        // Create AssistantMsgSchema
        //
        m_pAssistantMsgSchema = new MdaAssistantMsgSchema();

        //
        // Create SchemaSchema
        //
        m_pSchemaSchema = new MdaSchemaSchema();

        //
        // InvalidConfigFile
        //
        g_mdaStaticHeap.m_mdaInvalidConfigFile.Enable();

#ifdef _DEBUG
        StackSString sszValidateFramework(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MDAValidateFramework));
        if (!sszValidateFramework.IsEmpty() && sszValidateFramework.Equals(W("1")))
            DebugInitialize();
#endif
    }
    EX_CATCH
    {
        // MDA State corrupted, unable to initialize, runtime still OK
    }
    EX_END_CATCH(SwallowAllExceptions);

    RETURN;
}

MdaEnvironment::~MdaEnvironment()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_pStringFactory)
        delete m_pStringFactory;

    if (m_pGroups)
        delete m_pGroups;

    if (m_szMda)
        delete m_szMda;
}

MdaEnvironment::MdaEnvironment()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_bDisable = TRUE;
    m_szMda = CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_MDA);
    m_pStringFactory = NULL;
    m_pGroups = NULL;

    if (ManagedDebuggingAssistants::IsManagedDebuggerAttached())
    {
        if (m_pStringFactory == NULL)
            m_pStringFactory = new MdaFactory<StackSString>();

        if (m_pGroups == NULL)
            m_pGroups = new SArray<SString*>();

        SString* pStr = m_pStringFactory->Create();
        pStr->Set(W("managedDebugger"));
        m_pGroups->Append(pStr);
        m_bDisable = FALSE;
    }

    if (ManagedDebuggingAssistants::IsUnmanagedDebuggerAttached())
    {
        if (m_pStringFactory == NULL)
            m_pStringFactory = new MdaFactory<StackSString>();

        if (m_pGroups == NULL)
            m_pGroups = new SArray<SString*>();

        SString* pStr = m_pStringFactory->Create();
        pStr->Set(W("unmanagedDebugger"));
        m_pGroups->Append(pStr);
        m_bDisable = FALSE;
    }

    if (m_szMda)
    {
        if (m_pStringFactory == NULL)
            m_pStringFactory = new MdaFactory<StackSString>();

        if (m_pGroups == NULL)
            m_pGroups = new SArray<SString*>();

        StackSString sszMda(m_szMda);
        SString::Iterator s = sszMda.Begin();
        SString::Iterator e = s;

        while (true)
        {
            if (!sszMda.Find(e, W(';')))
                e = sszMda.End();
            SString* psszGroup = m_pStringFactory->Create();
            psszGroup->Set(sszMda, s, e);

            if (psszGroup->Equals(W("0")))
            {
                m_pGroups->Clear();
                m_bDisable = TRUE;
            }
            else
            {
                m_pGroups->Append(psszGroup);
                
                m_bDisable = FALSE;
            }

            if (e == sszMda.End())
                break;
            s = ++e;
        }
    }

    if (m_bDisable == FALSE)
    {
        // If we get here, m_pStringFactory should already have been created.
        _ASSERTE(m_pStringFactory != NULL);

        WCHAR szExe[_MAX_PATH];
        if (!WszGetModuleFileName(NULL, szExe, _MAX_PATH))
            return;

        // Construct file name of the config file
        m_psszConfigFile = m_pStringFactory->Create();
        m_psszConfigFile->Set(szExe);
        m_psszConfigFile->Append(W(".config"));

        // Construct file name of mda config file
        m_psszMdaConfigFile = m_pStringFactory->Create();
        m_psszMdaConfigFile->Set(szExe);
        m_psszMdaConfigFile->Append(W(".mda.config"));
    }
}

void ManagedDebuggingAssistants::EEStartupActivation()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    //
    // Read environment variable, then registry settings
    //
    MdaEnvironment env;

    if (env.IsDisabled())
        return;

    AllocateManagedDebuggingAssistants();

    //
    // ConfigFile Activation
    //
    g_mdaStaticHeap.m_pMda->EnvironmentActivation(&env);
}

#ifdef _DEBUG
void ManagedDebuggingAssistants::DebugInitialize()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        SO_INTOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    //
    // Validate MDA output on Debug builds
    //
    m_bValidateOutput = TRUE;

    //
    // XmlValidationError
    //
    g_mdaStaticHeap.m_mdaXmlValidationError.Enable();

    MdaSchema::ValidationResult validationResult;

    //
    // Validate SchemaScheam
    //
    MdaXmlElement* pXmlSchemaSchema = m_pSchemaSchema->ToXml(m_pMdaXmlIndustry);
    if (m_pSchemaSchema->Validate(pXmlSchemaSchema, &validationResult)->ValidationFailed())
    {
        MDA_TRIGGER_ASSISTANT(XmlValidationError, ReportError(&validationResult));
        UNREACHABLE();
    }

    //
    // Validate AssistantSchema
    //
    MdaXmlElement* pXmlAssistantSchema = m_pAssistantSchema->ToXml(m_pMdaXmlIndustry);
    if (m_pSchemaSchema->Validate(pXmlAssistantSchema, &validationResult)->ValidationFailed())
    {
        MDA_TRIGGER_ASSISTANT(XmlValidationError, ReportError(&validationResult));
        ASSERT(!W("You're modifications to MdaAssistantSchema for assistant input don't conform to XSD"));
    }

    //
    // Validate AssistantMsgSchema
    //
    MdaXmlElement* pXmlAssistantMsgSchema = m_pAssistantMsgSchema->ToXml(m_pMdaXmlIndustry);
    if (m_pSchemaSchema->Validate(pXmlAssistantMsgSchema, &validationResult)->ValidationFailed())
    {
        MDA_TRIGGER_ASSISTANT(XmlValidationError, ReportError(&validationResult));
        ASSERT(!W("You're modifications to MdaAssistantSchema for assistant output don't conform to XSD"));
    }
}
#endif

void ManagedDebuggingAssistants::ConfigFileActivation(LPCWSTR szConfigFile, MdaXmlIndustry* pXmlIndustry, MdaHashtable<MdaXmlElement*>* pMdaXmlPairs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Parse
    MdaSchema::ValidationResult validationResult;
    MdaXmlElement* pMdaConfig = MdaConfigFactory::ParseXmlStream(pXmlIndustry, szConfigFile);
    if (!pMdaConfig)
        return;

    // Validate
    if (m_pAssistantSchema->Validate(pMdaConfig, &validationResult)->ValidationFailed())
    {
        MDA_TRIGGER_ASSISTANT(InvalidConfigFile, ReportError(MdaElemDef(MdaConfig)));
        g_mdaStaticHeap.DisableAll();
        return;
    }

    // Activate
    InlineSArray<MdaXmlElement*, MdaElemDef(Max)> xmlMdaConfigs;
    MdaXPath::FindElements(pMdaConfig, W("/mdaConfig/assistants/*"), &xmlMdaConfigs);
    for(COUNT_T i = 0; i < xmlMdaConfigs.GetCount(); i ++)
    {
        MdaXmlElement* pXmlMdaConfig = xmlMdaConfigs[i];
        if (pXmlMdaConfig->GetAttribute(MdaAttrDecl(Enable))->GetValueAsBool())
        {
            pMdaXmlPairs->Set(pXmlMdaConfig->GetName(), xmlMdaConfigs[i]);
        }
        else
        {
            if (pMdaXmlPairs->HasKey(pXmlMdaConfig->GetName()))
                pMdaXmlPairs->DeleteValue(pXmlMdaConfig->GetName());
        }
    }
}

MdaXmlElement* ManagedDebuggingAssistants::GetSwitchActivationXml(MdaElemDeclDef mda)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (g_mdaAssistantIsSwitch[mda])
    {
        MdaXmlElement* pXml = m_pMdaXmlIndustry->CreateElement()->SetDeclDef(mda);
        pXml->AddAttributeBool(MdaAttrDecl(Enable), TRUE);
        return pXml;
    }
    else
    {
        if (!m_pSwitchActivationXml)
        {
            MdaXmlElement* pXmlMdaConfig = m_pMdaXmlIndustry->CreateElement()->SetDeclDef(MdaElemDef(MdaConfig));
            m_pSwitchActivationXml = pXmlMdaConfig->AddChild(MdaElemDecl(Assistants));

            for (COUNT_T i = 0; i < MdaElemDef(AssistantMax); i ++)
                m_pSwitchActivationXml->AddChild((MdaElemDeclDef)i);

            MdaSchema::ValidationResult validationResult;

            // Validating the schema has the side-effect of initializing the default XML attributes
            if (m_pAssistantSchema->Validate(pXmlMdaConfig, &validationResult)->ValidationFailed())
               ASSERT(!W("MDA Assistant must allow <Assistant /> form."));
        }

        return m_pSwitchActivationXml->GetChild(mda);
    }
}

void ManagedDebuggingAssistants::ActivateGroup(LPCWSTR groupName, SArray<MdaElemDeclDef>* pGroupMdaXmlParis, MdaHashtable<MdaXmlElement*>* pActivationMdaXmlPairs)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszGroupName(groupName);
    BOOL bIsManagedDebuggerSet = sszGroupName.EqualsCaseInsensitive(W("managedDebugger"));
    
    SArray<MdaElemDeclDef>& groupMdaXmlParis = *pGroupMdaXmlParis;

    for (COUNT_T i = 0; i < groupMdaXmlParis.GetCount(); i++)
    {
        MdaElemDeclDef mda = groupMdaXmlParis[i];
        MdaXmlElement* pSwitchActivationXml = GetSwitchActivationXml(mda);

        PREFIX_ASSUME(pSwitchActivationXml != NULL);
            
        pSwitchActivationXml->AddAttributeBool(MdaAttrDecl(SuppressDialog), bIsManagedDebuggerSet);
        
        pActivationMdaXmlPairs->Set(MdaSchema::g_arElementNames[mda], pSwitchActivationXml);
    }
}

LPCWSTR ToLowerFirstChar(LPCWSTR name, MdaFactory<SString>* pSstringFactory)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ASSERT(*name >= 'A' && *name <= 'Z');

    SString* pOutput = pSstringFactory->Create();
    pOutput->Clear();
    pOutput->Append(*name - W('A') + W('a'));
    pOutput->Append(&name[1]);
    return pOutput->GetUnicode();
}

void ManagedDebuggingAssistants::EnvironmentActivation(MdaEnvironment* pEnvironment)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pEnvironment->GetActivationMechanisms().GetCount() == 0)
        return;

    MdaFactory<StackSArray<MdaElemDeclDef> > arrayFactory;
    MdaFactory<SString> sstringFactory;
    MdaHashtable<MdaXmlElement*> mdaXmlPairs;

    // Activate
    SArray<SString*>& aActivationMechanisms = pEnvironment->GetActivationMechanisms();
    SArray<MdaElemDeclDef>* pGroup = NULL;
    StackSArray<SArray<MdaElemDeclDef>* > aGroups;

#define MDA_DEFINE_GROUPS
#include "mdaschema.inl"
#undef MDA_DEFINE_GROUPS

    // Match COMPlus_MDA env var to group
    for (COUNT_T i = 0; i < aActivationMechanisms.GetCount(); i++)
    {
        SString& sszActivationMechanism = *aActivationMechanisms[i];

        if (sszActivationMechanism.EqualsCaseInsensitive(W("ConfigFile")) || sszActivationMechanism.EqualsCaseInsensitive(W("1")))
        {
            ConfigFileActivation(pEnvironment->GetMdaConfigFile(), m_pMdaXmlIndustry, &mdaXmlPairs);
        }
        else
        {
            COUNT_T cGroup = 0;

#define MDA_ACTIVATE_GROUPS
#include "mdaschema.inl"
#undef MDA_ACTIVATE_GROUPS

#define MDA_ACTIVATE_SINGLTON_GROUPS
#include "mdaschema.inl"
#undef MDA_ACTIVATE_SINGLTON_GROUPS

        }
    }

    if (mdaXmlPairs.GetCount() == 0)
        return;

    // Create
    MdaXmlElement* pXmlAssistant = NULL;

#define MDA_ASSISTANT_CREATION
#include "mdaschema.inl"
#undef MDA_ASSISTANT_CREATION
}

typedef enum
{
    MDA_MSGBOX_NONE = 0,
    MDA_MSGBOX_RETRY = 4,
    MDA_MSGBOX_CANCLE = 2,
} MsgBoxResult;

BOOL ManagedDebuggingAssistants::IsUnmanagedDebuggerAttached()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsDebuggerPresent())
        return TRUE;

    return FALSE;
}

BOOL ManagedDebuggingAssistants::IsManagedDebuggerAttached()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#if DEBUGGING_SUPPORTED
    if (CORDebuggerAttached())
        return TRUE;
#endif

    return FALSE;
}

BOOL ManagedDebuggingAssistants::IsDebuggerAttached()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return IsUnmanagedDebuggerAttached() || IsManagedDebuggerAttached();
}

MdaXmlElement* ManagedDebuggingAssistants::GetRootElement(MdaXmlElement* pMdaXmlRoot)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    
    pMdaXmlRoot->SetDeclDef(MdaElemDef(Msg));
    pMdaXmlRoot->AddAttributeSz(MdaAttrDecl(Xmlns), MDA_TARGET_NAMESPACE)->SetNs(W("mda"));
    return pMdaXmlRoot;
}



//
// MdaXmlMessage
//
BOOL IsFormatChar(WCHAR c) { LIMITED_METHOD_CONTRACT; return (c == W('\\') || c == W('!') || c == W('+') || c == W('.') || c == W(':') || c == W('-')); }

// Logic copied from /fx/src/Xml/System/Xml/Core/XmlRawTextWriterGenerator.cxx::WriteAttributeTextBlock
SString& MdaXmlEscape(SString& sszBuffer, const SString& sszXml, BOOL bEscapeComment = FALSE)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    sszBuffer.Clear();

    SString::CIterator itr = sszXml.Begin();
    SString::CIterator end = sszXml.End();

    while (itr != end)
    {
        WCHAR c = *itr;

        switch(c)
        {
            case W('-'):
                if (*(itr+1) == W('-') && bEscapeComment)
                    sszBuffer.Append(W("- "));
                else
                    sszBuffer.Append(W("-"));
                break;
            case W('&'):
                sszBuffer.Append(W("&amp;"));
                break;
            case W('<'):
                sszBuffer.Append(W("&lt;"));
                break;
            case W('>'):
                sszBuffer.Append(W("&gt;"));
                break;
            case W('"'):
                sszBuffer.Append(W("&quote;"));
                break;
            default:
                sszBuffer.Append(c);
        }

        itr++;
    }

    return sszBuffer;
}

SString* WrapString(SString& buffer, SString& sszString, SCOUNT_T cWidth, SCOUNT_T cIndent = 0, SCOUNT_T cPostIndent = 0)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszEscapedString;
    MdaXmlEscape(sszEscapedString, sszString, TRUE);

    StackSString sszIndent;
    for (SCOUNT_T i = 0; i < cIndent; i ++)
        sszIndent.Append(W(" "));

    StackSString sszPostIndent;
    for (SCOUNT_T i = 0; i < cPostIndent; i ++)
        sszPostIndent.Append(W(" "));

    buffer.Append(sszIndent);

    SString::CIterator itr = sszEscapedString.Begin();
    SString::CIterator lineStart = sszEscapedString.Begin();
    SString::CIterator lineEnd = sszEscapedString.Begin();
    SString::CIterator lastFormatChar = sszEscapedString.Begin();
    SString::CIterator end = sszEscapedString.End();

    while (itr != end)
    {
        if (*itr == W(' '))
            lineEnd = itr;

        // Keep track of reasonable breaks in member and file names...
        if (IsFormatChar(*itr) && itr - lineStart < cWidth)
            lastFormatChar = itr;

        if (itr - lineStart >= cWidth || *itr == W('\n'))
        {
            if (*itr == W('\n'))
                lineEnd = itr;

            // If we didn't find a space or wrapping at found space wraps less than 3/5 of the line...
            else if (lineEnd == end || itr - lineEnd > cWidth * 3 / 5)
            {
                // ...then if we found a format char, start the wrap there...
                if (lastFormatChar != end)
                    lineEnd = lastFormatChar + 1;
                // ...else just do a simple wrap...
                else
                    lineEnd = itr;
            }

            SString sszLine(sszEscapedString, lineStart, lineEnd);
            buffer.Append(sszLine);
            buffer.Append(sszPostIndent);
            buffer.Append(W("\n"));
            buffer.Append(sszIndent);

            lineStart = lineEnd;

            // If we wrapped on a space or a return than skip over that character as we already replaced it with a \n.
            if (*lineEnd == W(' ') || *lineEnd == W('\n'))
                lineStart++;

            lineEnd = end;
            lastFormatChar = end;
        }

        itr++;
    }

    SString sszLine(sszEscapedString, lineStart, itr);
    buffer.Append(sszLine);

    return &buffer;
}

LPCWSTR ToUpperFirstChar(SString& buffer, LPCWSTR name)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ASSERT(*name >= 'a' && *name <= 'z');

    buffer.Clear();
    buffer.Append(*name - W('a') + W('A'));
    buffer.Append(&name[1]);
    return buffer.GetUnicode();
}

MdaXmlMessage::MdaXmlMessage(MdaAssistant* pAssistant, BOOL bBreak, MdaXmlElement** ppMdaXmlRoot)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(g_mdaStaticHeap.m_pMda));
    }
    CONTRACTL_END;

    m_pMdaAssistant = pAssistant;
    m_bBreak = (pAssistant->GetSuppressDialog()) ? FALSE : bBreak;
    m_pMdaXmlRoot = g_mdaStaticHeap.m_pMda->GetRootElement(m_mdaXmlIndustry.CreateElement());
    *ppMdaXmlRoot = m_pAssistantXmlRoot = pAssistant->GetRootElement(m_mdaXmlIndustry.CreateElement(), bBreak);
}

MdaXmlMessage::MdaXmlMessage(MdaXmlElement** ppMdaXmlRoot) : m_bBreak(FALSE)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(g_mdaStaticHeap.m_pMda));
    }
    CONTRACTL_END;
    
    *ppMdaXmlRoot = m_pMdaXmlRoot = g_mdaStaticHeap.m_pMda->GetRootElement(m_mdaXmlIndustry.CreateElement());
}

BOOL MdaXmlMessage::ShouldLogToManagedDebugger()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL bUnmanagedDebuggerAttached = FALSE;
    BOOL bManagedDebuggerAttached = FALSE;
    BOOL bManagedDebugLoggingEnabled = FALSE;

    bUnmanagedDebuggerAttached = IsUnmanagedDebuggerAttached();

#if DEBUGGING_SUPPORTED
    bManagedDebuggerAttached = IsManagedDebuggerAttached();
    bManagedDebugLoggingEnabled = (g_pDebugInterface && g_pDebugInterface->IsLoggingEnabled());
#endif

    return (!bUnmanagedDebuggerAttached && bManagedDebuggerAttached && bManagedDebugLoggingEnabled);
}

// Send an event for this MDA.
void MdaXmlMessage::SendEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (IsHostRegisteredForEvent(Event_MDAFired))
    {
        // A host is registered for the MDA fired event so let's start by notifying the
        // debugger is on is attached.
        if (IsManagedDebuggerAttached() || IsUnmanagedDebuggerAttached())
        {
            SendDebugEvent();
        }

        // Now that the debugger has been notified and continued, let's notify the host
        // so it can take any action it deems neccessary based on the MDA that fired.
        SendHostEvent();
    }
    else
    {
        // We aren't hosted or no host registered for the MDA fired event so let's simply
        // send the MDA to the debubber. Note that as opposed to the hosted case, we
        // will force a JIT attach if no debugger is present.
        SendDebugEvent();
    }
}

// Send an event for this MDA.
void MdaXmlMessage::SendHostEvent()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    MDAInfo info;
    SString strStackTrace;

    EX_TRY
    {
        // Retrieve the textual representation of the managed stack trace and add it to
        // the MDA information we give the host.
        GetManagedStackTraceString(TRUE, strStackTrace);
    }
    EX_CATCH
    {
        // We failed to get the stack trace string. This isn't fatal, we will simply not be
        // able to provide this information as part of the notification.
    }
    EX_END_CATCH(SwallowAllExceptions);

    // Set up the information and invoke the host to process the MDA fired event.
    info.lpMDACaption = m_pMdaAssistant->GetName();
    info.lpStackTrace = strStackTrace;
    info.lpMDAMessage = m_localizedMessage;
    ProcessEventForHost(Event_MDAFired, &info);

    // If the host initiated a thread abort, we want to raise it immediatly to
    // prevent any further code inside the VM from running and potentially
    // crashing the process.
    Thread *pThread = GetThread();
    TESTHOOKCALL(AppDomainCanBeUnloaded(pThread->GetDomain()->GetId().m_dwId,FALSE));    
    
    if (pThread && pThread->IsAbortInitiated())
        pThread->HandleThreadAbort(TRUE);
}

// Send a managed debug event for this MDA.
// This will block until the debugger continues us. This means the debugger could to things like run callstacks
// and change debuggee state.
void MdaXmlMessage::SendDebugEvent()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(g_mdaStaticHeap.m_pMda));
    }
    CONTRACTL_END;
    
    // Simple check to avoid getting XML string if we're not going to actually use it.
    if (!IsManagedDebuggerAttached() && !IsUnmanagedDebuggerAttached() && !m_bBreak)
    {
        return;
    }

    EX_TRY
    {
        StackSString sszXml;
        LPCWSTR ns = NULL;

        MdaSchema * pSchema = g_mdaStaticHeap.m_pMda->m_pAssistantSchema;
        ns = pSchema->SetRootAttributes(m_pMdaXmlRoot);
        m_pMdaXmlRoot->ToXml(&sszXml, ns);

        // For managed + interop cases, send a managed debug event.
        // If m_bBreak is true and no unmanaged debugger is attached trigger a jit-attach.
        if (IsManagedDebuggerAttached() || (m_bBreak && !IsUnmanagedDebuggerAttached()))
        {
            // Get MDA name (this is the type)
            StackSString sszMdaName;
            ToUpperFirstChar(sszMdaName, m_pMdaAssistant->GetName());
            // SendMDANotification needs to be called in preemptive GC mode.
            GCX_PREEMP();

            // This will do two things:
            // 1. If a managed debugger is attached, it will send the managed debug event for the MDA.
            // 2. If it's a m_bBreak, we'll try to do a managed jit-attach.
            // This blocks until continued. Since we're not slipping, we don't need the MDA_FLAG_SLIP flag.
            g_pDebugInterface->SendMDANotification(
                GetThread(),
                &sszMdaName,
                &m_localizedMessage,
                &sszXml,
                ((CorDebugMDAFlags) 0 ),
                RunningInteractive() ? m_bBreak : FALSE);        
        }

        if (IsUnmanagedDebuggerAttached() && !IsManagedDebuggerAttached())
        {
            // For native case, sent native debug event for logging.
            WszOutputDebugString(sszXml.GetUnicode());

            if (m_bBreak)
                RetailBreak();
        }
    }
    EX_CATCH
    {
        // No global MDA state modified in TRY
    }
    EX_END_CATCH(SwallowAllExceptions);
}

void MdaXmlMessage::SendMessagef(int resourceID, ...)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszResourcef;
    sszResourcef.LoadResource(CCompRC::DesktopCLR, resourceID );
    ASSERT(!sszResourcef.IsEmpty());

    va_list argItr;
    va_start(argItr, resourceID);
    m_localizedMessage.PVPrintf(sszResourcef, argItr);
    va_end(argItr);

    SendMessage();
}


void MdaXmlMessage::SendMessage(int resourceID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    SendMessagef(resourceID);
}

void MdaXmlMessage::SendMessage(LPCWSTR szMessage)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_localizedMessage.Set(szMessage);

    SendMessage();
}

void MdaXmlMessage::SendMessage()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(g_mdaStaticHeap.m_pMda));
    }
    CONTRACTL_END;

#if _DEBUG
    if (g_mdaStaticHeap.m_pMda->m_bValidateOutput)
    {
        MdaSchema::ValidationResult validationResult;
        if (g_mdaStaticHeap.m_pMda->m_pAssistantMsgSchema->Validate(m_pAssistantXmlRoot, &validationResult)->ValidationFailed())
        {
            MDA_TRIGGER_ASSISTANT(XmlValidationError, ReportError(&validationResult));
            ASSERT(W("Your MDA assistant's output did not match its output schema."));
        }
    }
#endif

    if (!m_localizedMessage.IsEmpty())
    {
        StackSString sszComment(m_localizedMessage);
        StackSString sszWrappedComment(W("\n"));
        WrapString(sszWrappedComment, sszComment, 80, 7);
        sszWrappedComment.Append(W("\n  "));
        m_pMdaXmlRoot->AddChildComment(sszWrappedComment.GetUnicode());
    }

    m_pMdaXmlRoot->AddChild(m_pAssistantXmlRoot);

    // Send applicable debug event (managed, native, interop) for this MDA.
    // If this is a severe probe, it may trigger a jit-attach
    SendEvent();
}


//
// MdaXPath::FindXXX
//

void MdaXPath::Find(SArray<MdaXPathVariable>& args, SString* pWildCard, va_list argItr)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (COUNT_T i = 0; i < GetArgCount(); i ++)
    {
        XPathVarType varType = m_argTypes[i];

        if (varType == XPathVarElemDeclDef)
            args[i].m_u.m_elemDeclDef = va_arg(argItr, MdaElemDeclDef);

        else if (varType == XPathVarAttrDeclDef)
            args[i].m_u.m_attrDeclDef = va_arg(argItr, MdaAttrDeclDef);

        else if (varType == XPathVarAttrBool)
            args[i].m_u.m_bool = va_arg(argItr, BOOL);

        else if (varType == XPathVarAttrINT32)
            args[i].m_u.m_int32 = va_arg(argItr, INT32);

        else if (varType == XPathVarAttrSString)
        {
            SString* pSString = va_arg(argItr, SString*);
            ASSERT(CheckPointer(pSString, NULL_OK));
            if (!pSString)
                pSString = pWildCard;
            args[i].m_u.m_pSstr = pSString;
        }

        else { UNREACHABLE(); }
    }
}

MdaXmlElement* MdaXPath::FindElement(MdaXmlElement* pRoot, ...)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pRoot)
        return NULL;

    va_list argItr;
    va_start(argItr, pRoot);

    SString wildCard;
    InlineSArray<MdaXPathVariable, 20> args;
    Find(args, &wildCard, argItr);

    MdaXPathResult result(&args);
    m_pCompiledQuery->Run(pRoot, &result);

    va_end(argItr);
    return result.GetXmlElement();
}

MdaXmlAttribute* MdaXPath::FindAttribute(MdaXmlElement* pRoot, ...)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pRoot)
        return NULL;

    va_list argItr;
    va_start(argItr, pRoot);

    SString wildCard;
    InlineSArray<MdaXPathVariable, 20> args;
    Find(args, &wildCard, argItr);

    MdaXPathResult result(&args);
    m_pCompiledQuery->Run(pRoot, &result);

    va_end(argItr);
    return result.GetXmlAttribute();
}

SArray<MdaXmlElement*>* MdaXPath::FindElements(MdaXmlElement* pRoot, SArray<MdaXmlElement*>* pResult, ...)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pRoot)
        return NULL;

    va_list argItr;
    va_start(argItr, pResult);

    SString wildCard;
    InlineSArray<MdaXPathVariable, 20> args;
    Find(args, &wildCard, argItr);

    MdaXPathResult result(pResult, &args);
    m_pCompiledQuery->Run(pRoot, &result);

    va_end(argItr);
    return pResult;
}

SArray<MdaXmlAttribute*>* MdaXPath::FindAttributes(MdaXmlElement* pRoot, SArray<MdaXmlAttribute*>* pResult, ...)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pRoot)
        return NULL;

    va_list argItr;
    va_start(argItr, pResult);

    SString wildCard;
    InlineSArray<MdaXPathVariable, 20> args;
    Find(args, &wildCard, argItr);

    MdaXPathResult result(pResult, &args);
    m_pCompiledQuery->Run(pRoot, &result);

    va_end(argItr);
    return pResult;
}


//
// MdaXPath::MdaXPathCompiler -- Lexifier
//

#define ISWHITE(ch) (ch == W(' ') || ch == W('\t') || ch == W('\n'))
#define ISRESERVED(ch) (wcschr(W("./()[]&|=@*?':"), ch) != NULL)
#define ISMDAID(ch) (!ISWHITE(ch) && !ISRESERVED(ch))

MdaXPath::MdaXPathCompiler::MdaXPathTokens MdaXPath::MdaXPathCompiler::LexAToken()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (*m_itr == W('\0'))
        return MdaXPathEnd;

    if (ISWHITE(*m_itr))
    {
        m_itr++;
        return LexAToken();
    }

    if (ISMDAID(*m_itr))
    {
        m_identifier.Clear();

        do
        {
            m_identifier.Append(*m_itr);
            m_itr++;
        }
        while(ISMDAID(*m_itr));

        m_identifier.Append(W("\0"));
        return MdaXPathIdentifier;
    }

    if (*m_itr == W('\''))
    {
        m_identifier.Clear();

        m_itr++;

        while(*m_itr != W('\''))
        {
            m_identifier.Append(*m_itr);
            m_itr++;
        }

        m_identifier.Append(W("\0"));

        m_itr++;
        return MdaXPathQuotedString;
    }

    WCHAR c = *m_itr;
    m_itr++;
    switch(c)
    {
        case W('.'): return MdaXPathDot;
        case W('/'): return MdaXPathSlash;
        case W('('): return MdaXPathOpenParen;
        case W(')'): return MdaXPathCloseParen;
        case W('['): return MdaXPathOpenSqBracket;
        case W(']'): return MdaXPathCloseSqBracket;
        case W('&'): return MdaXPathLogicalAnd;
        case W('|'): return MdaXPathLogicalOr;
        case W('='): return MdaXPathEquals;
        case W('@'): return MdaXPathAtSign;
        case W('*'): return MdaXPathAstrix;
        case W('?'): return MdaXPathQMark;
    }

    UNREACHABLE();
}


//
// MdaXPath::MdaXPathCompiler -- Parser
//

//  XPATH
//      '/' ELEMENT_EXPR end
//      '/' ELEMENT_EXPR XPATH
//      '/' ATTRIBUTE end
MdaXPath::MdaXPathBase* MdaXPath::MdaXPathCompiler::XPATH()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    PRECONDITION(TokenIs(MdaXPathXPATH));

    MdaXPathElement* pElementExpr = NULL;

    NextToken();
    if (TokenIs(MdaXPathELEMENT_EXPR))
        pElementExpr = ELEMENT_EXPR();

    else if (TokenIs(MdaXPathATTRIBUTE))
    {
        MdaXPathAttribute* pAttr = ATTRIBUTE();
        pAttr->MarkAsTarget();
        NextToken();
        ASSERT(TokenIs(MdaXPathEnd));
        return pAttr;
    }

    else { UNREACHABLE(); }


    if (TokenIs(MdaXPathEnd))
        return pElementExpr->MarkAsTarget();

    else if (TokenIs(MdaXPathXPATH))
        return pElementExpr->SetChild(XPATH());

    else { UNREACHABLE(); }
}

//  ATTRIBUTE
//      '@' id
//      '@' '?'
MdaXPath::MdaXPathAttribute* MdaXPath::MdaXPathCompiler::ATTRIBUTE()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    PRECONDITION(TokenIs(MdaXPathATTRIBUTE));

    MdaXPathAttribute* pAttr = NULL;

    NextToken();
    if (TokenIs(MdaXPathQMark))
    {
        pAttr = m_pXPath->m_attrFactory.Create()->SetName(++m_pXPath->m_cArgs);
        *m_pXPath->m_argTypes.Append() = XPathVarAttrDeclDef;
    }

    else if (TokenIs(MdaXPathIdentifier))
    {
        pAttr = m_pXPath->m_attrFactory.Create()->SetName(MdaSchema::GetAttributeType(GetIdentifier()));
    }

    else { UNREACHABLE(); }

    NextToken();
    return pAttr;
}

//  ELEMENT_EXPR
//      ELEMENT '[' FILTER_EXPR ']'
//      ELEMENT
MdaXPath::MdaXPathElement* MdaXPath::MdaXPathCompiler::ELEMENT_EXPR()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    PRECONDITION(TokenIs(MdaXPathELEMENT_EXPR));

    MdaXPathElement* pElement = ELEMENT();

    if (TokenIs(MdaXPathOpenSqBracket))
    {
        NextToken();
        pElement->SetQualifier(FILTER_EXPR());
        ASSERT(TokenIs(MdaXPathCloseSqBracket));

        NextToken();
    }

    return pElement;
}

//  FILTER_EXPR
//      FILTER
//      '(' FILTER ')'
//      FILTER '&' FILTER
//      FILTER '|' FILTER
MdaXPath::MdaXPathBase* MdaXPath::MdaXPathCompiler::FILTER_EXPR()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    PRECONDITION(TokenIs(MdaXPathFILTER_EXPR));

    // '(' FILTER ')'
    if (TokenIs(MdaXPathOpenParen))
    {
        MdaXPath::MdaXPathBase* pFilter = FILTER();
        ASSERT(TokenIs(MdaXPathCloseParen));

        NextToken();
        return pFilter;
    }

    if (TokenIs(MdaXPathFILTER))
    {
        MdaXPath::MdaXPathBase* pFilter = FILTER();

        // FILTER '&' FILTER
        if (TokenIs(MdaXPathLogicalAnd))
        {
            NextToken();
            return m_pXPath->m_logicalOpFactory.Create()->Initialize(TRUE, pFilter, FILTER());
        }

        // FILTER '|' FILTER
        if (TokenIs(MdaXPathLogicalOr))
        {
            NextToken();
            return m_pXPath->m_logicalOpFactory.Create()->Initialize(FALSE, pFilter, FILTER());
        }

        // FILTER
        return pFilter;
    }

    UNREACHABLE();
}

//  FILTER
//      ELEMENT_EXPR
//      ATTRIBUTE_FILTER
//      ELEMENT_EXPR ATTRIBUTE_FILTER
MdaXPath::MdaXPathBase* MdaXPath::MdaXPathCompiler::FILTER()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    PRECONDITION(TokenIs(MdaXPathFILTER));

    if (TokenIs(MdaXPathELEMENT_EXPR))
    {
        MdaXPathElement* pElementExpr = ELEMENT_EXPR();

        if (TokenIs(MdaXPathATTRIBUTE_FILTER))
            pElementExpr->SetQualifier(ATTRIBUTE_FILTER());

        return pElementExpr;
    }

    if (TokenIs(MdaXPathATTRIBUTE_FILTER))
        return ATTRIBUTE_FILTER();

    UNREACHABLE();
}

//  ELEMENT
//      id
//      '*'
//      '?'
MdaXPath::MdaXPathElement* MdaXPath::MdaXPathCompiler::ELEMENT()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    PRECONDITION(TokenIs(MdaXPathELEMENT));

    MdaXPathElement* pElement = m_pXPath->m_elementFactory.Create();

    if (TokenIs(MdaXPathAstrix))
        pElement->Initialize();

    else if (TokenIs(MdaXPathIdentifier))
        pElement->Initialize(MdaSchema::GetElementType(GetIdentifier()));

    else if (TokenIs(MdaXPathQMark))
    {
        pElement->Initialize(++m_pXPath->m_cArgs);
        *m_pXPath->m_argTypes.Append() = XPathVarElemDeclDef;
    }

    else { UNREACHABLE(); }

    NextToken();
    return pElement;
}

//  ATTRIBUTE_FILTER();
//      ATTRIBUTE
//      ATTRIBUTE '=' ''' id '''
//      ATTRIBUTE '=' '?'
MdaXPath::MdaXPathAttribute* MdaXPath::MdaXPathCompiler::ATTRIBUTE_FILTER()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
    PRECONDITION(TokenIs(MdaXPathATTRIBUTE_FILTER));

    MdaXPathAttribute* pAttr = ATTRIBUTE();

    if (TokenIs(MdaXPathEquals))
    {
        NextToken();

        if (TokenIs(MdaXPathQuotedString))
        {
            NextToken();
            pAttr->SetValue(GetIdentifier());

            NextToken();
            ASSERT(TokenIs(MdaXPathQuotedString));
        }
        else if (TokenIs(MdaXPathQMark))
        {
            pAttr->SetValue(++m_pXPath->m_cArgs);
            *m_pXPath->m_argTypes.Append() = XPathVarAttrSString;
        }
        else { UNREACHABLE(); }
    }

    NextToken();
    return pAttr;
}


//
// MdaXPath::Elements::Run() -- The search engine
//

BOOL MdaXPath::MdaXPathElement::Run(MdaXmlElement* pElement, MdaXPathResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    BOOL bAnyPass = FALSE;
    if (pResult->IsRoot())
    {
        bAnyPass |= RunOnChild(pElement, pResult);
    }
    else
    {
        SArray<MdaXmlElement*>& children = pElement->GetChildren();

        for (UINT32 i = 0; i < children.GetCount(); i ++)
        {
            bAnyPass |= RunOnChild(children[i], pResult);
        }
    }

    return bAnyPass;
}

BOOL MdaXPath::MdaXPathElement::RunOnChild(MdaXmlElement* pElement, MdaXPathResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MdaElemDeclDef name = m_nameArg == NOT_VARIABLE ? m_name : pResult->GetArgs()[m_nameArg].m_u.m_elemDeclDef;

    if (name != MdaElemUndefined && name != pElement->GetDeclDef())
        return FALSE;

    if (m_pQualifier && !m_pQualifier->Run(pElement, pResult))
        return FALSE;

    if (m_pChild && !m_pChild->Run(pElement, pResult))
        return FALSE;

    if (m_bIsTarget)
    {
        ASSERT(!m_pChild);
        pResult->AddMatch(pElement);
    }

    return TRUE;
}

BOOL MdaXPath::MdaXPathAttribute::Run(MdaXmlElement* pElement, MdaXPathResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MdaAttrDeclDef name = m_nameArg == NOT_VARIABLE ? m_name : pResult->GetArgs()[m_nameArg].m_u.m_attrDeclDef;
    SString& value = m_valueArg == NOT_VARIABLE ? m_value : *pResult->GetArgs()[m_valueArg].m_u.m_pSstr;

    MdaXmlAttribute* pAttr = pElement->GetAttribute(name);
    if (!pAttr)
        return FALSE;

    LPCWSTR szAttrValue = pAttr->GetValue();
    if (!value.IsEmpty() && *szAttrValue != W('*') && !value.Equals(szAttrValue))
        return FALSE;

    if (m_bIsTarget)
        pResult->AddMatch(pElement);

    return TRUE;
}

BOOL MdaXPath::MdaXPathLogicalOp::Run(MdaXmlElement* pParent, MdaXPathResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_andOp)
        return m_pLhs->Run(pParent, pResult) && m_pRhs->Run(pParent, pResult);

    return m_pLhs->Run(pParent, pResult) || m_pRhs->Run(pParent, pResult);
}


//
// MdaSchema
//

MdaHashtable<MdaElemDeclDef>* MdaSchema::g_pHtElementType;
MdaHashtable<MdaAttrDeclDef>* MdaSchema::g_pHtAttributeType;
LPCWSTR MdaSchema::g_arElementNames[MdaElemEnd];
LPCWSTR MdaSchema::g_arAttributeNames[MdaAttrEnd];
MdaFactory<SString>* MdaSchema::g_pSstringFactory;
MdaElemDeclDef MdaSchema::MdaSchemaTypeToElemDef[MdaSchema::MdaSchemaTypeEnd];
MdaSchema::MdaSchemaMetaType MdaSchema::MdaSchemaTypeToMetaType[MdaSchema::MdaSchemaTypeEnd];

LPCWSTR MdaSchema::ToLowerFirstChar(LPCWSTR name)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return ::ToLowerFirstChar(name, g_pSstringFactory);
}

void MdaSchema::Initialize()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    g_pHtElementType = new MdaHashtable<MdaElemDeclDef>();
    g_pHtAttributeType = new MdaHashtable<MdaAttrDeclDef>();
    g_pSstringFactory = new MdaFactory<SString>();

    COUNT_T i = 0;
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Sequence);         // MdaSchemaSequenceType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Choice);           // MdaSchemaChoiceType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Group);            // MdaSchemaGroupType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Group);            // MdaSchemaGroupRefType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Schema);           // MdaSchemaRootType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Attribute);        // MdaSchemaAttributeType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Element);          // MdaSchemaElementType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(ComplexType);      // MdaSchemaComplexTypeType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(ComplexType);      // MdaSchemaComplexTypeDefType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Element);          // MdaSchemaElementRefTyp
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Extension);        // MdaSchemaExtensionType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Element);          // MdaSchemaElementRefTypeType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(ComplexContent);   // MdaSchemaComplexContentType
    MdaSchemaTypeToElemDef[i++] = MdaElemDef(Element);          // MdaSchemaElementAnyType

    i = 0;
    MdaSchemaTypeToMetaType[i++] = MdaSchemaMataTypePattern;    // MdaSchemaSequenceType
    MdaSchemaTypeToMetaType[i++] = MdaSchemaMataTypePattern;    // MdaSchemaChoiceType
    MdaSchemaTypeToMetaType[i++] = (MdaSchemaMetaType)(MdaSchemaMataTypePattern | MdaSchemaMataTypeDeclDef); // MdaSchemaGroupType
    MdaSchemaTypeToMetaType[i++] = (MdaSchemaMetaType)(MdaSchemaMataTypePattern | MdaSchemaMataTypeRef); // MdaSchemaGroupRefType
    MdaSchemaTypeToMetaType[i++] = MdaSchemaMataNone;           // MdaSchemaRootType
    MdaSchemaTypeToMetaType[i++] = MdaSchemaMataNone;           // MdaSchemaAttributeType
    MdaSchemaTypeToMetaType[i++] = MdaSchemaMataTypeDeclDef;    // MdaSchemaElementType
    MdaSchemaTypeToMetaType[i++] = (MdaSchemaMetaType)(MdaSchemaMataNone | MdaSchemaMataMayHaveAttributes); // MdaSchemaComplexTypeType
    MdaSchemaTypeToMetaType[i++] = (MdaSchemaMetaType)(MdaSchemaMataTypeDeclDef | MdaSchemaMataMayHaveAttributes); // MdaSchemaComplexTypeDefType
    MdaSchemaTypeToMetaType[i++] = MdaSchemaMataTypeRef;        // MdaSchemaElementRefTyp
    MdaSchemaTypeToMetaType[i++] = (MdaSchemaMetaType)(MdaSchemaMataTypeRef | MdaSchemaMataMayHaveAttributes); // MdaSchemaExtensionType
    MdaSchemaTypeToMetaType[i++] = (MdaSchemaMetaType)(MdaSchemaMataTypeDeclDef | MdaSchemaMataTypeRef); // MdaSchemaElementRefTypeType
    MdaSchemaTypeToMetaType[i++] = MdaSchemaMataNone;           // MdaSchemaComplexContentType
    MdaSchemaTypeToMetaType[i++] = MdaSchemaMataTypeDeclDef;    // MdaSchemaElementAnyType

    i = 0;
#define MDA_MAP_ASSISTANT_DEFINITION_TO_NAME
#include "mdaschema.inl"
#undef MDA_MAP_ASSISTANT_DEFINITION_TO_NAME
    g_arElementNames[i++] = NULL;
#define MDA_MAP_ELEMENT_DEFINITION_TO_NAME
#include "mdaschema.inl"
#undef MDA_MAP_ELEMENT_DEFINITION_TO_NAME
    g_arElementNames[i++] = NULL;
#define MDA_MAP_ELEMENT_DECLARATION_TO_NAME
#include "mdaschema.inl"
#undef MDA_MAP_ELEMENT_DECLARATION_TO_NAME
    g_arElementNames[i++] = NULL; // Max
    g_arElementNames[i++] = W("!--"); // Comment
    g_arElementNames[i++] = NULL; // Undefined

    i = 0;
#define MDA_MAP_ATTRIBUTE_DECLARATION_TO_NAME
#include "mdaschema.inl"
#undef MDA_MAP_ATTRIBUTE_DECLARATION_TO_NAME

#define MDA_MAP_ASSISTANT_NAME_TO_DEFINITION
#include "mdaschema.inl"
#undef MDA_MAP_ASSISTANT_NAME_TO_DEFINITION

#define MDA_MAP_ELEMENT_NAME_TO_DEFINITION
#include "mdaschema.inl"
#undef MDA_MAP_ELEMENT_NAME_TO_DEFINITION

#define MDA_MAP_ELEMENT_NAME_TO_DECLARATION
#include "mdaschema.inl"
#undef MDA_MAP_ELEMENT_NAME_TO_DECLARATION

#define MDA_MAP_ATTRIBUTE_NAME_TO_DECLARATION
#include "mdaschema.inl"
#undef MDA_MAP_ATTRIBUTE_NAME_TO_DECLARATION
}

MdaElemDeclDef MdaSchema::GetElementType(LPCWSTR name, BOOL bAssertDefined)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MdaElemDeclDef type;

    if (!g_pHtElementType->Get(name, &type))
    {
        ASSERT(!bAssertDefined);
        return MdaElemUndefined;
    }

    return type;
}

LPCWSTR MdaSchema::GetElementName(MdaElemDeclDef type)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    PRECONDITION(type >= 0 && type < MdaElemUndefined);
    return g_arElementNames[type];
}

MdaAttrDeclDef MdaSchema::GetAttributeType(LPCWSTR name, BOOL bAssertDefined)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MdaAttrDeclDef type;

    if (!g_pHtAttributeType->Get(name, &type))
    {
        ASSERT(!bAssertDefined);
        return MdaAttrUndefined;
    }

    return type;
}

LPCWSTR MdaSchema::GetAttributeName(MdaAttrDeclDef type)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return g_arAttributeNames[type];
}

// TODO: Validation error reporting needs work
MdaSchema::ValidationResult* MdaSchema::Validate(MdaXmlElement* pRoot, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    pResult->Initialize(this, pRoot);

    MdaSchemaBase* pXsd = *GetDef(pRoot->GetDeclDef());
    ASSERT((CheckPointer(pXsd) || (pRoot->GetDeclDef() > MdaElemDecl(Max))) && W("You likley did not include a MDA_DEFINE_OUTPUT section in your schema!"));

    BOOL bValidationSucceeded = pXsd ? pXsd->Validate(pRoot, pResult) : FALSE;

    if (bValidationSucceeded)
        pResult->ResetResult();
    else
        pResult->SetError();

    ASSERT(pResult->ValidationFailed() == !bValidationSucceeded);
    return pResult;
}

MdaSchema::MdaSchema()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for(COUNT_T i = 0; i < MdaElemEnd; i ++)
        m_definitions[i] = NULL;
}


//
// MdaAssistantSchema
//

MdaAssistantSchema::MdaAssistantSchema()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#define MDA_DEFINE_ASSISTANT_SCHEMA
#include "mdaschema.inl"
#undef MDA_DEFINE_ASSISTANT_SCHEMA

#define MDA_DEFINE_MDA_ASSISTANT_CONFIG_GROUP
#include "mdaschema.inl"
#undef MDA_DEFINE_MDA_ASSISTANT_CONFIG_GROUP
}

LPCWSTR MdaAssistantSchema::SetRootAttributes(MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    //pXml->AddAttribute(W("xmlns:") MDA_SCHEMA_PREFIX, MDA_TARGET_NAMESPACE);
    //pXml->AddAttribute(W("xmlns:xsi"), W("http://www.w3.org/2001/XMLSchema-instance"));
    return MDA_SCHEMA_PREFIX;
}


//
// MdaAssistantMsgSchema
//

MdaAssistantMsgSchema::MdaAssistantMsgSchema()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#define MDA_DEFINE_ASSISTANT_MSG_SCHEMA
#include "mdaschema.inl"
#undef MDA_DEFINE_ASSISTANT_MSG_SCHEMA

#define MDA_DEFINE_MDA_ASSISTANT_MSG_GROUP
#include "mdaschema.inl"
#undef MDA_DEFINE_MDA_ASSISTANT_MSG_GROUP
}

LPCWSTR MdaAssistantMsgSchema::SetRootAttributes(MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    //pXml->AddAttribute(W("xmlns:") MDA_SCHEMA_PREFIX, MDA_TARGET_NAMESPACE);
    //pXml->AddAttribute(W("xmlns:xsi"), W("http://www.w3.org/2001/XMLSchema-instance"));
    return MDA_SCHEMA_PREFIX;
}


//
// MdaSchemaSchema
//

MdaSchemaSchema::MdaSchemaSchema()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#define MDA_DEFINE_SCHEMA_SCHEMA
#include "mdaschema.inl"
#undef MDA_DEFINE_SCHEMA_SCHEMA
}

LPCWSTR MdaSchemaSchema::SetRootAttributes(MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    pXml->AddAttributeSz(MdaAttrDecl(TargetNamespace), MDA_TARGET_NAMESPACE);
    pXml->AddAttributeSz(MdaAttrDecl(Xmlns), W("http://www.w3.org/2001/XMLSchema"))->SetNs(W("xs"));
    pXml->AddAttributeSz(MdaAttrDecl(Xmlns), MDA_TARGET_NAMESPACE);
    return W("xs");
}


//
// MdaSchema::MdaSchemaXXX
//
MdaXmlElement* MdaSchema::MdaSchemaBase::ToXml(MdaXmlIndustry* pMdaXmlIndustry, MdaSchemaBase* pViolation)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return ToXml(pMdaXmlIndustry->CreateElement(), pViolation);
}

MdaXmlElement* MdaSchema::MdaSchemaBase::ToXml(MdaXmlElement* pXmlRoot, MdaSchemaBase* pViolation)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPCWSTR debugName = GetName();

    MdaXmlElement* pXml = pXmlRoot->AddChild(GetSchemaDeclDef());
    SetAttributes(pXml);

//    if (this == pViolation)
//        pXml->AddAttributeSz(MdaAttrDecl(Violated), W("---- THIS XSD ELEMENT VIOLATED -----"));

    if (m_children.GetCount() == 1 &&
        m_children[0]->GetSchemaDeclDef() == MdaElemDef(ComplexType) &&
        m_children[0]->m_children.GetCount() == 0 &&
        (!MayHaveAttr(m_children[0]) ||
         m_children[0]->GetAttributes().GetCount() == 0))
    {
        // Convert <Element><ComplexType/><Element> to <Element/>
        return pXml;
    }

    for(COUNT_T i = 0; i < m_children.GetCount(); i ++)
    {
        debugName = m_children[i]->GetName();
        m_children[i]->ToXml(pXml, pViolation);
    }

    if (MayHaveAttr(this))
    {
        SArray<MdaSchemaAttribute*>& attributes = GetAttributes();
        for(COUNT_T j = 0; j < attributes.GetCount(); j ++)
        {
            debugName = attributes[j]->GetName();
            attributes[j]->ToXml(pXml, pViolation);
        }
    }

    return pXml;
}


void MdaSchema::MdaSchemaBase::AddChild(MdaSchemaBase* pElement)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pElement->GetSchemaDeclDef() == MdaElemDef(Attribute))
        *GetAttributes().Append() = (MdaSchemaAttribute*)pElement;
    else
        *m_children.Append() = pElement;
}

//
// Validation
//

#define CpdXsdIfFailGo(EXPR) do { if (!(EXPR)) { goto Fail; } } while (0)
#define CpdXsdTest(EXPR) do { if (!(EXPR)) { pResult->SetError(this, pElement); goto Fail; } } while (0)
#define MDA_XSD_VERIFY_OK return TRUE;
#define MDA_XSD_VERIFY_FAIL Fail: return FALSE;

BOOL MdaSchema::MdaSchemaElement::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString buffer;
    LPCWSTR debug = pElement->DebugToString(&buffer);

    CpdXsdTest(pElement->GetDeclDef() == GetDeclDef());

    for(COUNT_T i = 0; i < m_children.GetCount(); i++)
        CpdXsdIfFailGo(m_children[i]->Validate(pElement, pResult));

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaSequence::ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString buffer;
    LPCWSTR debug = pElement->DebugToString(&buffer);

    COUNT_T cPeriod = m_children.GetCount();
    COUNT_T cChildren = pElement->GetChildren().GetCount();
    COUNT_T cCurrent = *pCount;
    COUNT_T cCount = cCurrent;
    COUNT_T cMatches = 0;

    if (cPeriod == 0)
        return TRUE;

    while(cCurrent <= cChildren)
    {
        MdaSchemaBase* pXsd = m_children[cMatches % cPeriod];
        if (pXsd->GetSchemaDeclDef() == MdaElemDef(Element))
        {
            if (cCurrent == cChildren)
                break;

            if (!pXsd->Validate(pElement->GetChildren()[cCurrent], pResult))
                break;

            cCurrent++;
        }
        else
        {
            ASSERT(IsPattern(pXsd));
            if (!pXsd->ValidatePattern(pElement, pResult, &cCurrent))
                break;
        }

        cMatches++;

        // One period matched
        if (cMatches % cPeriod == 0)
            cCount = cCurrent;

        // Maximum periods matcheds
        if (cMatches / cPeriod == m_max)
            break;
    }

    // Test if the minumum number periods have been matched
    if (cMatches / cPeriod < m_min)
        return FALSE;

    // Update the position past the matched elements
    *pCount = cCount;

    return TRUE;
}

BOOL MdaSchema::MdaSchemaChoice::ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString buffer;
    LPCWSTR debug = pElement->DebugToString(&buffer);

    BOOL bFound = FALSE;
    COUNT_T cCurrent = *pCount;
    COUNT_T cChildren = pElement->GetChildren().GetCount();

    for(COUNT_T cXsd = 0; cXsd < m_children.GetCount(); cXsd++)
    {
        MdaSchemaBase* pXsd = m_children[cXsd];

        if (IsPattern(pXsd))
        {
            COUNT_T cOldCurrent = cCurrent;
            if (pXsd->ValidatePattern(pElement, pResult, &cCurrent))
            {
                // "Empty matches" only allowed in choice pattern if there are no children to match
                if (cOldCurrent != cCurrent || cChildren == 0)
                {
                    bFound = TRUE;
                    break;
                }
            }
        }
        else
        {
            if (cCurrent == cChildren)
                break;

            if (pXsd->Validate(pElement->GetChildren()[cCurrent], pResult))
            {
                cCurrent++;
                bFound = TRUE;
                break;
            }
        }
    }

    CpdXsdIfFailGo(bFound);

    *pCount = cCurrent;

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

#define this pThis
BOOL MdaSchema::Validate(MdaSchemaAttribute* pThis, MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszValue;
    MdaXmlAttribute* pAttr = (MdaXmlAttribute*)pElement->GetAttribute(pThis->m_declDef);

    if (!pAttr && !pThis->m_szDefault.IsEmpty())
    {
        pAttr = pElement->AddDefaultAttribute(pThis->m_declDef, pThis->m_szDefault.GetUnicode());
    }

    if (!pAttr)
    {
        CpdXsdTest(!pThis->m_bRequired);
        return TRUE;
    }

#ifdef _DEBUG
    // Only necessary for validation of assistant output
    if (pAttr->m_type != MdaSchemaPrimitiveUnknown)
    {
        CpdXsdTest(pAttr->m_type == pThis->m_type);
        return TRUE;
    }
#endif

    LPCWSTR szValue = pAttr->GetValue();
    sszValue.Set(szValue);

    if (pThis->m_type == MdaSchemaPrimitiveSString)
    {
        /* accept all strings? */
    }
    else if (pThis->m_type == MdaSchemaPrimitiveINT32)
    {
        CpdXsdTest(!sszValue.IsEmpty() && sszValue.GetCount() != 0);

        for (COUNT_T i = 0; i < sszValue.GetCount(); i ++)
        {
            if (i == 0 && *szValue == W('-') && sszValue.GetCount() > 1)
                continue;

            CpdXsdTest(IS_DIGIT(szValue[i]));
        }

        pAttr->SetINT32(_wtoi(szValue));
    }
    else if (pThis->m_type == MdaSchemaPrimitiveBOOL)
    {
        CpdXsdTest(!sszValue.IsEmpty() && sszValue.GetCount() != 0);

        if (sszValue.Equals(W("true")))
            pAttr->SetBOOL(true);
        else if (sszValue.Equals(W("false")))
            pAttr->SetBOOL(false);
        else
            CpdXsdTest(FALSE);
    }

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}
#undef this

BOOL MdaSchema::MdaSchemaBase::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    COUNT_T count = 0;

    CpdXsdTest(ValidatePattern(pElement, pResult, &count));

    CpdXsdTest(count == pElement->GetChildren().GetCount());

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaRoot::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for(COUNT_T i = 0; i < m_children.GetCount(); i++)
        CpdXsdIfFailGo(m_children[i]->Validate(pElement, pResult));

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaComplexType::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for(COUNT_T i = 0; i < m_children.GetCount(); i++)
        CpdXsdIfFailGo(m_children[i]->Validate(pElement, pResult));

    for(COUNT_T i = 0; i < m_attributes.GetCount(); i++)
        CpdXsdIfFailGo(m_attributes[i]->Validate(pElement, pResult));

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaComplexTypeDef::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for(COUNT_T i = 0; i < m_children.GetCount(); i++)
        CpdXsdIfFailGo(m_children[i]->Validate(pElement, pResult));

    for(COUNT_T i = 0; i < m_attributes.GetCount(); i++)
        CpdXsdIfFailGo(m_attributes[i]->Validate(pElement, pResult));

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaComplexContent::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for(COUNT_T i = 0; i < m_children.GetCount(); i++)
        CpdXsdIfFailGo(m_children[i]->Validate(pElement, pResult));

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaGroup::ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for(COUNT_T i = 0; i < m_children.GetCount(); i++)
    {
        ASSERT(IsPattern(m_children[i]));
        CpdXsdIfFailGo(m_children[i]->ValidatePattern(pElement, pResult, pCount));
    }

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaGroupRef::ValidatePattern(MdaXmlElement* pElement, ValidationResult* pResult, COUNT_T* pCount)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MdaSchemaBase* pReference = GetRef();
    LPCWSTR debug = GetRefName();
    ASSERT(IsPattern(this));
    return pReference->ValidatePattern(pElement, pResult, pCount);
}

BOOL MdaSchema::MdaSchemaExtension::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ASSERT(GetRef()->GetSchemaType() == MdaSchemaComplexTypeDefType);
    MdaSchemaComplexTypeDef* pReference = (MdaSchemaComplexTypeDef*)GetRef();

    MdaSchemaSequence sequence;
    sequence.Initialize(1, 1);

    MdaSchemaBase* pXsd = pReference;
    while(true)
    {
        if (MayHaveAttr(pXsd))
        {
            for(COUNT_T i = 0; i < pXsd->GetAttributes().GetCount(); i++)
                CpdXsdIfFailGo(pXsd->GetAttributes()[i]->Validate(pElement, pResult));
        }

        if (pXsd->GetSchemaType() == MdaSchemaExtensionType)
        {
            pXsd = ((MdaSchemaComplexTypeDef*)pXsd)->GetRef();
            continue;
        }

        if (pXsd->m_children.GetCount() == 0)
            break;

        pXsd = pXsd->m_children[0];

        if (IsPattern(pXsd))
        {
            sequence.AddChild(pXsd);
            break;
        }
    }

    if (m_children.GetCount() == 1)
    {
        ASSERT(IsPattern(m_children[0]));
        sequence.AddChild(m_children[0]);
    }

    CpdXsdIfFailGo(sequence.Validate(pElement, pResult));

    for(COUNT_T i = 0; i < m_attributes.GetCount(); i++)
        CpdXsdIfFailGo(m_attributes[i]->Validate(pElement, pResult));

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaElementRefType::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CpdXsdIfFailGo(GetRef()->Validate(pElement, pResult));

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaElementAny::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString buffer;
    LPCWSTR debug = pElement->DebugToString(&buffer);

    CpdXsdTest(pElement->GetDeclDef() == GetDeclDef());

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}

BOOL MdaSchema::MdaSchemaElementRef::Validate(MdaXmlElement* pElement, ValidationResult* pResult)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPCWSTR debug = GetRefName();
    CpdXsdIfFailGo(GetRef()->Validate(pElement, pResult));

    MDA_XSD_VERIFY_OK;
    MDA_XSD_VERIFY_FAIL;
}


//
// MdaSchema::XXX::SetAttributes()
//

void MdaSchema::MdaSchemaSequence::SetAttributes(MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SmallStackSString ssBound;

    ssBound.Printf(W("%d"), m_min);
    pXml->AddAttributeSz(MdaAttrDecl(MinOccurs), ssBound.GetUnicode());

    if (m_max == -1)
    {
        pXml->AddAttributeSz(MdaAttrDecl(MaxOccurs), W("unbounded"));
    }
    else
    {
        ssBound.Printf(W("%d"), m_max);
        pXml->AddAttributeSz(MdaAttrDecl(MaxOccurs), ssBound.GetUnicode());
    }
}

void MdaSchema::MdaSchemaAttribute::SetAttributes(MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    pXml->AddAttributeSz(MdaAttrDecl(Name), GetAttributeName(m_declDef));

    LPCWSTR szType = NULL;
    if (m_type == MdaSchemaPrimitiveBOOL)
        szType = W("xs:boolean");
    else if (m_type == MdaSchemaPrimitiveINT32)
        szType = W("xs:int");
    else if (m_type == MdaSchemaPrimitiveSString)
        szType = W("xs:string");
    else { UNREACHABLE(); }

    pXml->AddAttributeSz(MdaAttrDecl(Type), szType);
    pXml->AddAttributeSz(MdaAttrDecl(Use), m_bRequired ? W("required") : W("optional"));

    if (!m_szDefault.IsEmpty())
        pXml->AddAttributeSz(MdaAttrDecl(Default), m_szDefault);
}

void MdaSchema::MdaSchemaDeclDefRef::SetAttributes(MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LPCWSTR szDeclDef = NULL;
    LPCWSTR szRef = NULL;

    if (IsDeclDef(this))
        szDeclDef = GetDeclDefName();

    if (IsRef(this))
        szRef = GetRefName();

    switch (GetSchemaType())
    {
        case MdaSchemaGroupRefType:
        case MdaSchemaElementRefTyp:
            pXml->AddAttributeSz(MdaAttrDecl(Ref), szRef);
            break;

        case MdaSchemaExtensionType:
            pXml->AddAttributeSz(MdaAttrDecl(Base), szRef);
            break;

        case MdaSchemaElementRefTypeType:
            pXml->AddAttributeSz(MdaAttrDecl(Name), szDeclDef);
            pXml->AddAttributeSz(MdaAttrDecl(Type), szRef);
            break;

        case MdaSchemaElementAnyType:
            pXml->AddAttributeSz(MdaAttrDecl(Name), szDeclDef);
            pXml->AddAttributeSz(MdaAttrDecl(Type), W("xs:anyType"));
            break;

        case MdaSchemaGroupType:
        case MdaSchemaElementType:
        case MdaSchemaComplexTypeDefType:
            pXml->AddAttributeSz(MdaAttrDecl(Name), szDeclDef);
            break;

        default:
            UNREACHABLE();
    }
}

//
// MdaAssistant
//
void MdaAssistant::Initialize(MdaXmlElement* pXmlInput) 
{ 
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (pXmlInput->GetAttribute(MdaAttrDecl(SuppressDialog)))
        m_bSuppressDialog = !!pXmlInput->GetAttributeValueAsBool(MdaAttrDecl(SuppressDialog));
}

LPCWSTR MdaAssistant::GetName()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return MdaSchema::GetElementName(m_assistantDeclDef);
}

MdaXmlElement* MdaAssistant::GetRootElement(MdaXmlElement* pRoot, BOOL bBreak)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MdaXmlElement* pXmlAssistant = pRoot->AddChild(GetAssistantMsgDeclDef());

    if (bBreak)
        pXmlAssistant->AddAttributeSz(MdaAttrDecl(Break), W("true"));

    return pXmlAssistant;
}

BOOL MdaAssistant::IsAssistantActive(MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return TRUE;
}

MdaXmlElement* MdaAssistant::OutputThread(Thread* pThread, MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(pThread);
    pXml->AddAttributeInt(MdaAttrDecl(OsId), pThread->GetOSThreadId());
    pXml->AddAttributeInt(MdaAttrDecl(ManagedId), pThread->GetThreadId());

    return pXml;
}

MdaXmlElement* MdaAssistant::OutputMethodTable(MethodTable* pMT, MdaXmlElement* pXml)
{
    CONTRACT (MdaXmlElement*)
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(CheckPointer(pMT));
        PRECONDITION(CheckPointer(pXml));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    static WCHAR szTemplateMsg[] = {W("Failed to QI for interface %s because it does not have a COM proxy stub registered.")};

    DefineFullyQualifiedNameForClassWOnStack();
    pXml->AddAttributeSz(MdaAttrDecl(Name), GetFullyQualifiedNameForClassW(pMT));

    RETURN pXml;
}

void MdaAssistant::ToString(TypeHandle typeHandle, SString* psszFullname, SString* psszNamespace)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString ssz;;

    psszFullname->Clear();

    LPCSTR szDeclTypeName, szNamespace;
    InlineSArray<mdTypeDef, 32> nesting;

    mdTypeDef tkTypeDef = typeHandle.GetCl();
    Module* pModule = typeHandle.GetModule();
    IMDInternalImport* pImport = pModule->GetMDImport();

    // Get tkTypeDef tokens for declaring type and its nested classes
    nesting.Append(tkTypeDef);
    while (S_OK == pImport->GetNestedClassProps(tkTypeDef, &tkTypeDef))
        nesting.Append(tkTypeDef);

    // Append the namespace
    COUNT_T i = nesting.GetCount() - 1;
    if (FAILED(pImport->GetNameOfTypeDef(nesting[i], &szDeclTypeName, &szNamespace)))
    {
        szNamespace = NULL;
        szDeclTypeName = NULL;
    }
    if (szNamespace && *szNamespace != W('\0'))
    {
        if (psszNamespace)
            psszNamespace->SetUTF8(szNamespace);

        psszFullname->SetUTF8(szNamespace);
        psszFullname->Append(W("."));
    }

    // Append the nested classes
    for(; i > 0; i --)
    {
        IfFailThrow(pImport->GetNameOfTypeDef(nesting[i], &szDeclTypeName, &szNamespace));
        ssz.SetUTF8(szDeclTypeName);
        psszFullname->Append(ssz);
        psszFullname->Append(W("+"));
    }

    // Append the declaring type name
    IfFailThrow(pImport->GetNameOfTypeDef(nesting[i], &szDeclTypeName, &szNamespace));
    ssz.SetUTF8(szDeclTypeName);
    psszFullname->Append(ssz);
}

SString& MdaAssistant::ToString(SString& sszBuffer, Module* pModule)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    sszBuffer.AppendUTF8(pModule->GetSimpleName());
    return sszBuffer;
}

SString& MdaAssistant::ToString(SString& sszBuffer, TypeHandle typeHandle)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszScratch;
    ToString(sszBuffer, typeHandle.GetModule()).GetUnicode();
    sszBuffer.Append(W("!"));
    ToString(typeHandle, &sszScratch, NULL);
    sszBuffer.Append(sszScratch);
    return sszBuffer;
}

SString& MdaAssistant::ToString(SString& sszBuffer, MethodDesc* pMethodDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ToString(sszBuffer, pMethodDesc->GetMethodTable()).GetUnicode();
    sszBuffer.Append(W("::"));
    StackSString ssz;
    ssz.SetUTF8(pMethodDesc->GetName());
    sszBuffer.Append(ssz);
    return sszBuffer;
}

SString& MdaAssistant::ToString(SString& sszBuffer, FieldDesc* pFieldDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ToString(sszBuffer, pFieldDesc->GetEnclosingMethodTable()).GetUnicode();
    sszBuffer.Append(W("::"));
    StackSString ssz;
    ssz.SetUTF8(pFieldDesc->GetName());
    sszBuffer.Append(ssz);
    return sszBuffer;
}

MdaXmlElement* MdaAssistant::OutputParameter(SString parameterName, USHORT sequence, MethodDesc* pMethodDesc, MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    TypeHandle declType(pMethodDesc->GetMethodTable());
    Module* pDeclModule = declType.GetModule();

    pXml->AddAttributeSz(MdaAttrDecl(Name), parameterName);
    pXml->AddAttributeInt(MdaAttrDecl(Index), sequence);

    OutputMethodDesc(pMethodDesc, pXml->AddChild(MdaElemDecl(DeclaringMethod)));

    return pXml;
}

MdaXmlElement* MdaAssistant::OutputMethodDesc(MethodDesc* pMethodDesc, MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    TypeHandle declType(pMethodDesc->GetMethodTable());
    Module* pDeclModule = declType.GetModule();

    StackSString sszMethod;

    pXml->AddAttributeSz(MdaAttrDecl(Name), ToString(sszMethod, pMethodDesc).GetUnicode());

    return pXml;
}

MdaXmlElement* MdaAssistant::OutputFieldDesc(FieldDesc* pFieldDesc, MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszField;

    pXml->AddAttributeSz(MdaAttrDecl(Name), ToString(sszField, pFieldDesc).GetUnicode());

    return pXml;
}

MdaXmlElement* MdaAssistant::OutputTypeHandle(TypeHandle typeHandle, MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszTypeName;

    // Set Attribute
    pXml->AddAttributeSz(MdaAttrDecl(Name), ToString(sszTypeName, typeHandle.GetMethodTable()).GetUnicode());

    return pXml;
}

MdaXmlElement* MdaAssistant::OutputModule(Module* pModule, MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    pXml->AddAttributeSz(MdaAttrDecl(Name), pModule->GetSimpleName());

    return pXml;
}

MdaXmlElement* MdaAssistant::OutputCallsite(MethodDesc *pMethodDesc, DWORD dwOffset, MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszMethod;
    pXml->AddAttributeSz(MdaAttrDecl(Name), ToString(sszMethod, pMethodDesc).GetUnicode());

    StackSString sszOffset;
    sszOffset.Printf(W("0x%04X"), dwOffset);
    pXml->AddAttributeSz(MdaAttrDecl(Offset), sszOffset.GetUnicode());

    return pXml;
}

MdaXmlElement* MdaAssistant::OutputException(OBJECTREF *pExceptionObj, MdaXmlElement* pXml)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    OutputTypeHandle((*pExceptionObj)->GetTypeHandle(), pXml->AddChild(MdaElemDecl(Type)));

    StackSString message;
    GetExceptionMessage(*pExceptionObj, message);

    pXml->AddAttributeSz(MdaAttrDecl(Message), message);

    return pXml;
}

//
// MdaQuery::CompiledQueries
//
BOOL MdaQuery::CompiledQueries::Test(MethodDesc* pMethodDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (COUNT_T i = 0; i < m_queries.GetCount(); i ++)
    {
        if (m_queries[i]->Test(pMethodDesc))
            return TRUE;
    }

    return FALSE;
}

BOOL MdaQuery::CompiledQueries::Test(FieldDesc* pFieldDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (COUNT_T i = 0; i < m_queries.GetCount(); i ++)
    {
        if (m_queries[i]->Test(pFieldDesc))
            return TRUE;
    }

    return FALSE;
}

BOOL MdaQuery::CompiledQueries::Test(MethodTable* pMethodTable)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (COUNT_T i = 0; i < m_queries.GetCount(); i ++)
    {
        if (m_queries[i]->Test(pMethodTable))
            return TRUE;
    }

    return FALSE;
}

MdaQuery::CompiledQuery* MdaQuery::CompiledQueries::AddQuery()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    CompiledQuery* pQuery = m_factory.Create();
    m_queries.Append(pQuery);
    return pQuery;
}


//
// MdaQuery::CompiledQuery
//
void MdaQuery::Compile(MdaXmlElement* pXmlFilters, CompiledQueries* pCompiledQueries)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SArray<MdaXmlElement*>& children = pXmlFilters->GetChildren();
    BOOL bJmc = pXmlFilters->GetAttribute(MdaAttrDecl(JustMyCode))->GetValueAsBool();

    for (COUNT_T i = 0; i < children.GetCount(); i ++)
    {
        MdaXmlElement* pXmlFilter = children[i];
        SString* psszName = pXmlFilter->GetAttribute(MdaAttrDecl(Name))->GetValueAsCSString();
        MdaXmlAttribute* pJmcOptAttr = pXmlFilter->GetAttribute(MdaAttrDecl(JustMyCode));
        if (pJmcOptAttr)
            bJmc = pJmcOptAttr->GetValueAsBool();
        Compiler compiler;
        CompiledQuery* pQuery = pCompiledQueries->AddQuery();
        compiler.Compile(psszName, pQuery);
        if (bJmc)
            pQuery->SetJustMyCode();
    }
}

MdaQuery::CompiledQuery::CompiledQuery()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_bJustMyCode = FALSE;
    m_bAnyMember = FALSE;
    m_bAnyType = FALSE;
    m_sszFullname.Clear();
    m_sszMember.Clear();
}

BOOL StartsWith(SString* psszString, SString* psszSubstring)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszString(*psszString);
    if (psszString->GetCount() < psszSubstring->GetCount())
        return FALSE;
    sszString.Truncate(sszString.Begin() + psszSubstring->GetCount());
    return sszString.Equals(*psszSubstring);
}

BOOL MdaQuery::CompiledQuery::Test(MethodDesc* pMethodDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszName(SString::Utf8, pMethodDesc->GetName());

    if (pMethodDesc->IsLCGMethod() || pMethodDesc->IsILStub())
        return FALSE;

    if (!Test(&sszName, pMethodDesc->GetMethodTable()))
        return FALSE;

    if (!m_bJustMyCode)
        return TRUE;

    if (IsJustMyCode(pMethodDesc))
        return TRUE;

    return FALSE;
}

BOOL MdaQuery::CompiledQuery::Test(FieldDesc* pFieldDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString sszName(SString::Utf8, pFieldDesc->GetName());
    if (!Test(&sszName, pFieldDesc->GetApproxEnclosingMethodTable()))
        return FALSE;

    if (!m_bJustMyCode)
        return TRUE;

    return TRUE;
}

BOOL MdaQuery::CompiledQuery::Test(SString* psszName, MethodTable* pMethodTable)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!m_sszMember.IsEmpty())
    {
        if (!m_sszMember.Equals(*psszName))
            return FALSE;

        if (m_sszMember.GetCount() == m_sszFullname.GetCount())
            return TRUE;
    }
    else if (!m_bAnyMember)
        return FALSE;

    return Test(pMethodTable);
}

BOOL MdaQuery::CompiledQuery::Test(MethodTable* pMethodTable)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!pMethodTable)
        return FALSE;

    if (m_sszFullname.IsEmpty())
        return TRUE;

    StackSString sszNamespace, sszFullName;
    MdaAssistant::ToString(pMethodTable, &sszFullName, &sszNamespace);

    if (m_bAnyType && StartsWith(&m_sszFullname, &sszNamespace))
        return TRUE;

    if (m_bAnyMember && StartsWith(&m_sszFullname, &sszFullName))
        return TRUE;

    return m_sszFullname.Equals(sszFullName);
}

void MdaQuery::CompiledQuery::SetName(LPCWSTR name)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!m_sszFullname.IsEmpty())
    {
        m_sszFullname.Append(W("."));
        m_sszMember.Clear();
    }
    else
    {
        m_sszMember.Set(name);
    }

    m_sszFullname.Append(name);

}

void MdaQuery::CompiledQuery::SetNestedTypeName(LPCWSTR name)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_sszMember.Clear();

    if (!m_sszFullname.IsEmpty())
        m_sszFullname.Append(W("+"));

    m_sszFullname.Append(name);
}

void MdaQuery::CompiledQuery::SetAnyMember()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_bAnyMember = TRUE;
    m_sszMember.Clear();
}

void MdaQuery::CompiledQuery::SetAnyType()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_bAnyType = TRUE;
    m_sszMember.Clear();

    if (m_sszFullname.IsEmpty())
        m_bAnyMember = TRUE;
}


//
// MdaQuery::CompiledQuery
//

MdaQuery::Compiler::Token MdaQuery::Compiler::LexAToken()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (*m_itr == W('\0'))
        return MdaFilterEnd;

    if (ISWHITE(*m_itr))
    {
        m_itr++;
        return LexAToken();
    }

    if (ISMDAID(*m_itr))
    {
        m_identifier.Clear();

        do
        {
            m_identifier.Append(*m_itr);
            m_itr++;
        }
        while(ISMDAID(*m_itr));

        m_identifier.Append(W("\0"));
        return MdaFilterIdentifier;
    }

    WCHAR c = *m_itr;
    m_itr++;
    switch(c)
    {
        case W('.'): return MdaFilterDot;
        case W(':'): return MdaFilterColon;
        case W('*'): return MdaFilterAstrix;
        case W('+'): return MdaFilterPlus;
    }

    return MdaFilterEnd;
}

//
// MdaXPath::MdaXPathCompiler -- Parser
//
BOOL MdaQuery::Compiler::Compile(SString* sszQuery, CompiledQuery* pAst)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_itr = sszQuery->Begin();

    NextToken();
    BOOL bResult = NAME(pAst);

    return bResult;
}

//  NAME
//      '*'
//      id
//      id '.' NAME
//      id '+' NESTNAME
//      id ':' ':' NESTNAME
BOOL MdaQuery::Compiler::NAME(CompiledQuery* pAst)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (TokenIs(MdaFilterIdentifier))
    {
        pAst->SetName(GetIdentifier());

        NextToken();
        if (TokenIs(MdaFilterDot))
        {
            NextToken();
            return NAME(pAst);
        }
        else if (TokenIs(MdaFilterPlus))
        {
            NextToken();
            return NESTNAME(pAst);
        }
        else if (TokenIs(MdaFilterColon))
        {
            NextToken();
            if (!TokenIs(MdaFilterColon))
                return FALSE;

            NextToken();
            return MEMBERNAME(pAst);
        }
    }
    else if (TokenIs(MdaFilterAstrix))
    {
        pAst->SetAnyType();
        NextToken();
    }
    else return FALSE;

    return TRUE;
}

//  NESTNAME
//      id '+' NESTNAME
//      id ':' ':' NESTNAME
BOOL MdaQuery::Compiler::NESTNAME(CompiledQuery* pAst)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (!TokenIs(MdaFilterIdentifier))
        return FALSE;

    pAst->SetNestedTypeName(GetIdentifier());

    NextToken();

    if (TokenIs(MdaFilterPlus))
    {
        NextToken();
        return NESTNAME(pAst);
    }
    else if (TokenIs(MdaFilterColon))
    {
        NextToken();
        if (!TokenIs(MdaFilterColon))
            return FALSE;

        NextToken();
        return MEMBERNAME(pAst);
    }
    else return FALSE;
}

//  MEMBERNAME
//      '*'
//      id
BOOL MdaQuery::Compiler::MEMBERNAME(CompiledQuery* pAst)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (TokenIs(MdaFilterIdentifier))
        pAst->SetMemberName(GetIdentifier());

    else if (TokenIs(MdaFilterAstrix))
        pAst->SetAnyMember();

    else return FALSE;

    NextToken();
    return TRUE;
}


//
// MdaXmlElement
//
MdaXmlElement* MdaXmlElement::GetChild(MdaElemDeclDef declDef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for(COUNT_T i = 0; i < m_children.GetCount(); i ++)
    {
        if (m_children[i]->GetDeclDef() == declDef)
            return m_children[i];
    }

    return NULL;
}

SString* MdaXmlElement::ToXml(SString* pXml, LPCWSTR ns, INT32 depth)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(depth < 60); // Trap for recursion
    }
    CONTRACTL_END;

    // Indent
    for (INT32 i = 0; i < depth; i ++)
        pXml->Append(W("  "));

    pXml->Append(W("<"));
    if (ns && IsDefinition()) { pXml->Append(ns); pXml->Append(W(":")); }
    pXml->Append(GetName());

    if (m_attributes.GetCount() != 0)
    {
        for (COUNT_T i = 0; i < m_defaultAttrIndex && i < m_attributes.GetCount(); i ++)
        {
            pXml->Append(W(" "));
            m_attributes[i]->ToXml(pXml);
        }
    }

    if (m_children.GetCount() == 0)
    {
        if (GetDeclDef() == MdaElemComment)
        {
            pXml->Append(W(" "));
            pXml->Append(m_szName.GetUnicode());
            pXml->Append(W(" -->\n"));
        }
        else
            pXml->Append(W("/>\n"));
    }
    else
    {
        pXml->Append(W(">"));

        SArray<MdaXmlElement*>::Iterator itr = m_children.Begin();
        SArray<MdaXmlElement*>::Iterator end = m_children.End();

        pXml->Append(W("\n"));
        while (itr != end)
        {
            (*itr)->ToXml(pXml, ns, depth + 1);
            itr++;
        }

        // Indent
        for (INT32 i = 0; i < depth; i ++)
            pXml->Append(W("  "));

        pXml->Append(W("</"));
        if (ns && IsDefinition()) { pXml->Append(ns); pXml->Append(W(":")); }
        pXml->Append(GetName());
        pXml->Append(W(">\n"));
    }


    return pXml;
}

LPCWSTR MdaXmlElement::DebugToString(SString* pBuffer)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    pBuffer->Append(W("<"));
    pBuffer->Append(GetName());

    for(COUNT_T i = 0; i < GetAttributes().GetCount(); i++)
    {
        pBuffer->Append(W(" "));
        GetAttributes()[i]->ToXml(pBuffer);
    }

    pBuffer->Append(W("/>"));
    return pBuffer->GetUnicode();
}

MdaXmlElement* MdaXmlElement::SetName(LPCWSTR name, BOOL bAssertDefined)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SetDeclDef(MdaSchema::GetElementType(name, bAssertDefined));

    if (GetDeclDef() == MdaElemUndefined)
        m_szName.Set(name);

    return this;
}

MdaXmlAttribute* MdaXmlElement::AddAttribute(MdaAttrDeclDef declDef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return AddAttribute(m_pXmlIndustry->CreateAttribute()->SetDeclDef(declDef));
}

MdaXmlAttribute* MdaXmlElement::AddAttribute(LPCWSTR szName, LPCWSTR szValue)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return AddAttribute(m_pXmlIndustry->CreateAttribute()->Initialize(szName, szValue));
}

MdaXmlAttribute* MdaXmlElement::AddDefaultAttribute(MdaAttrDeclDef attrDeclDef, LPCWSTR szValue)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_defaultAttrIndex == -1)
        m_defaultAttrIndex = m_attributes.GetCount();
    MdaXmlAttribute* pAttr = AddAttribute(attrDeclDef)->SetSString(szValue);
    pAttr->m_type = MdaSchemaPrimitiveUnknown;
    return pAttr;
}

MdaXmlElement* MdaXmlElement::AddChild(MdaXmlElement* pChild)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    //PRECONDITION(m_elemDeclDef != MdaElemUndefined);
    PRECONDITION(CheckPointer(pChild));
    PRECONDITION(CheckPointer(pChild->m_pXmlIndustry));

    *m_children.Append() = pChild;
    return pChild;
}

MdaXmlElement* MdaXmlElement::AddChild(LPCWSTR name, BOOL bAssertDefined)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return AddChild(m_pXmlIndustry->CreateElement())->SetName(name, bAssertDefined);
}

MdaXmlElement* MdaXmlElement::AddChild(MdaElemDeclDef type)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return AddChild(m_pXmlIndustry->CreateElement()->SetDeclDef(type));
}

LPCWSTR MdaXmlElement::GetName()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (GetDeclDef() == MdaElemUndefined)
        return m_szName.GetUnicode();

    return MdaSchema::GetElementName(m_elemDeclDef);
}

MdaXmlAttribute* MdaXmlElement::GetAttribute(MdaAttrDeclDef attrDeclDef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;


    for(UINT32 i = 0; i < m_attributes.GetCount(); i++)
    {
        if (attrDeclDef == m_attributes[i]->GetDeclDef())
            return m_attributes[i];
    }

    return NULL;
}

BOOL MdaXmlElement::GetAttributeValueAsBool(MdaAttrDeclDef attrDeclDef, BOOL bDefault)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MdaXmlAttribute* pAttr = GetAttribute(attrDeclDef);

    if (!pAttr)
        return bDefault;

    return pAttr->GetValueAsBool();
}

BOOL MdaXmlElement::GetAttributeValueAsBool(MdaAttrDeclDef attrDeclDef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MdaXmlAttribute* pAttr = GetAttribute(attrDeclDef);
    PREFIX_ASSUME(pAttr != NULL);
    ASSERT(pAttr);
    return pAttr->GetValueAsBool();
}

//
// MdaXmlAttribute
//

SString* MdaXmlAttribute::ToXml(SString* xml)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    SString sszBuffer;

    xml->Append(GetName());
    if (!m_szNs.IsEmpty())
    {
        xml->Append(W(":"));
        xml->Append(m_szNs.GetUnicode());
    }

    xml->Append(W("=\""));
    if (m_type == MdaSchemaPrimitiveSString)
        xml->Append(MdaXmlEscape(sszBuffer, m_value));
    else if (m_type == MdaSchemaPrimitiveBOOL)
        xml->Append(m_bool ? W("true") : W("false"));
    else if (m_type == MdaSchemaPrimitiveINT32)
    {
        StackSString sszOutput;
        sszOutput.Printf(W("%d"), m_int);
        xml->Append(sszOutput);
    }
    xml->Append(W("\""));
    return xml;
}

LPCWSTR MdaXmlAttribute::GetName()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (m_declDef != MdaAttrUndefined)
        return MdaSchema::GetAttributeName(m_declDef);

    return m_szName.GetUnicode();
}

MdaXmlAttribute* MdaXmlAttribute::Initialize(LPCWSTR szName, LPCWSTR szValue)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_type = MdaSchemaPrimitiveUnknown;
    m_value.Set(szValue);

    SetDeclDef(MdaSchema::GetAttributeType(szName, FALSE));
    if (m_declDef == MdaAttrUndefined)
        m_szName.Set(szName);

    return this;
}


//
// MdaConfigFactory
//
STDAPI GetXMLObjectEx(IXMLParser **ppv);

MdaXmlElement* MdaConfigFactory::ParseXmlStream(MdaXmlIndustry* pXmlIndustry, LPCWSTR pszFileName)
{
    CONTRACT(MdaXmlElement*)
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACT_END;

    HRESULT hr = S_OK;
    MdaXmlElement* pRoot = NULL;

    EX_TRY
    {
    {
        if (!pszFileName)
            goto Exit;

        NonVMComHolder<IXMLParser> pIXMLParser(NULL);
        NonVMComHolder<IStream> pFile(NULL);

        hr = CreateConfigStream(pszFileName, &pFile);
        if(FAILED(hr)) goto Exit;

        hr = GetXMLObjectEx(&pIXMLParser);
        if(FAILED(hr)) goto Exit;

        hr = pIXMLParser->SetInput(pFile); // filestream's RefCount=2
        if ( ! SUCCEEDED(hr))
            goto Exit;

        pRoot = pXmlIndustry->CreateElement()->SetDeclDef(MdaElemDef(Dummy));
        MdaConfigFactory mdaConfigFactory(pRoot);

        hr = pIXMLParser->SetFactory(&mdaConfigFactory); // factory's RefCount=2
        if (!SUCCEEDED(hr))
            goto Exit;

        hr = pIXMLParser->Run(-1);

        if (pRoot->GetChildren().GetCount() == 1)
            pRoot = pRoot->GetChildren()[0];
        else
            pRoot = NULL;
    }
    Exit: ;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

        if (hr == (HRESULT)XML_E_MISSINGROOT)
            hr = S_OK;
        else if (Assembly::FileNotFound(hr))
            hr = S_FALSE;

    RETURN pRoot;
}

HRESULT STDMETHODCALLTYPE MdaConfigFactory::CreateNode(
    IXMLNodeSource* pSource,
    PVOID pNodeParent,
    USHORT cNumRecs,
    XML_NODE_INFO** apNodeInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_pMdaXmlElement = NULL;

    for(INT32 i = 0; i < cNumRecs; i++)
    {
        DWORD dwType = apNodeInfo[i]->dwType;

        if(dwType == XML_ELEMENT || dwType == XML_ATTRIBUTE)
        {
            StackSString sszName((WCHAR*)apNodeInfo[i]->pwcText, apNodeInfo[i]->ulLen);

            if (dwType == XML_ELEMENT)
            {
                m_pMdaXmlElement = m_stack.Tos()->AddChild(sszName, FALSE);
            }
            else if (dwType == XML_ATTRIBUTE)
            {
                i++;
                InlineSString<MDA_BUFFER_SIZE> szValue((WCHAR*)apNodeInfo[i]->pwcText, apNodeInfo[i]->ulLen);

                if (m_pMdaXmlElement)
                    m_pMdaXmlElement->AddAttribute(sszName.GetUnicode(), szValue);
            }
        }
    }

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MdaConfigFactory::BeginChildren(
    IXMLNodeSource* pSource,
    XML_NODE_INFO* pNodeInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    m_stack.Push(m_pMdaXmlElement);

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MdaConfigFactory::EndChildren(
    IXMLNodeSource* pSource,
    BOOL fEmptyNode,
    XML_NODE_INFO* pNodeInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;


    if (fEmptyNode)
        return S_OK;

    m_stack.Pop();

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MdaConfigFactory::NotifyEvent(
    IXMLNodeSource* pSource,
    XML_NODEFACTORY_EVENT iEvt)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MdaConfigFactory::Error(
    IXMLNodeSource* pSource,
    HRESULT hrErrorCode,
    USHORT cNumRecs,
    XML_NODE_INFO** apNodeInfo)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return E_FAIL;
}

#endif
