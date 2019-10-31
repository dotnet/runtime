// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// bindertracing.cpp
//


//
// Implements helpers for binder tracing
//
// ============================================================

#include "common.h"
#include "bindertracing.h"

#include "activitytracker.h"

#ifdef FEATURE_EVENT_TRACE
#include "eventtracebase.h"
#endif // FEATURE_EVENT_TRACE

using namespace BINDER_SPACE;

namespace
{
    void FireAssemblyLoadStart(const BinderTracing::AssemblyBindOperation::BindRequest &request)
    {
#ifdef FEATURE_EVENT_TRACE
        if (!EventEnabledAssemblyLoadStart())
            return;

        GUID activityId = GUID_NULL;
        GUID relatedActivityId = GUID_NULL;
        ActivityTracker::Start(&activityId, &relatedActivityId);

        FireEtwAssemblyLoadStart(
            GetClrInstanceId(),
            request.AssemblyName,
            request.AssemblyPath,
            request.RequestingAssembly,
            request.AssemblyLoadContext,
            request.RequestingAssemblyLoadContext,
            &activityId,
            &relatedActivityId);
#endif // FEATURE_EVENT_TRACE
    }

    void FireAssemblyLoadStop(const BinderTracing::AssemblyBindOperation::BindRequest &request, PEAssembly *resultAssembly, bool cached)
    {
#ifdef FEATURE_EVENT_TRACE
        if (!EventEnabledAssemblyLoadStop())
            return;

        GUID activityId = GUID_NULL;
        ActivityTracker::Stop(&activityId);

        SString resultName;
        SString resultPath;
        bool success = resultAssembly != nullptr;
        if (success)
        {
            resultPath = resultAssembly->GetPath();
            resultAssembly->GetDisplayName(resultName);
        }

        FireEtwAssemblyLoadStop(
            GetClrInstanceId(),
            request.AssemblyName,
            request.AssemblyPath,
            request.RequestingAssembly,
            request.AssemblyLoadContext,
            request.RequestingAssemblyLoadContext,
            success,
            resultName,
            resultPath,
            cached,
            &activityId);
#endif // FEATURE_EVENT_TRACE
    }

    void GetAssemblyLoadContextNameFromManagedALC(INT_PTR managedALC, /* out */ SString &alcName)
    {
#ifdef CROSSGEN_COMPILE
        alcName.Set(W("Custom"));
#else // CROSSGEN_COMPILE
        OBJECTREF *alc = reinterpret_cast<OBJECTREF *>(managedALC);

        GCX_COOP();
        struct _gc {
            STRINGREF alcName;
        } gc;
        ZeroMemory(&gc, sizeof(gc));

        GCPROTECT_BEGIN(gc);

        PREPARE_VIRTUAL_CALLSITE(METHOD__OBJECT__TO_STRING, *alc);
        DECLARE_ARGHOLDER_ARRAY(args, 1);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(*alc);
        CALL_MANAGED_METHOD_RETREF(gc.alcName, STRINGREF, args);
        gc.alcName->GetSString(alcName);

        GCPROTECT_END();
#endif // CROSSGEN_COMPILE
    }

    void GetAssemblyLoadContextNameFromBinderID(UINT_PTR binderID, AppDomain *domain, /*out*/ SString &alcName)
    {
        ICLRPrivBinder *binder = reinterpret_cast<ICLRPrivBinder *>(binderID);
#ifdef FEATURE_COMINTEROP
        if (AreSameBinderInstance(binder, domain->GetTPABinderContext()) || AreSameBinderInstance(binder, domain->GetWinRtBinder()))
#else
        if (AreSameBinderInstance(binder, domain->GetTPABinderContext()))
#endif // FEATURE_COMINTEROP
        {
            alcName.Set(W("Default"));
        }
        else
        {
#ifdef CROSSGEN_COMPILE
            GetAssemblyLoadContextNameFromManagedALC(0, alcName);
#else // CROSSGEN_COMPILE
            CLRPrivBinderAssemblyLoadContext *alcBinder = static_cast<CLRPrivBinderAssemblyLoadContext *>(binder);

            GetAssemblyLoadContextNameFromManagedALC(alcBinder->GetManagedAssemblyLoadContext(), alcName);
#endif // CROSSGEN_COMPILE
        }
    }

    void GetAssemblyLoadContextNameFromBindContext(ICLRPrivBinder *bindContext, AppDomain *domain, /*out*/ SString &alcName)
    {
        _ASSERTE(bindContext != nullptr);

        UINT_PTR binderID = 0;
        HRESULT hr = bindContext->GetBinderID(&binderID);
        _ASSERTE(SUCCEEDED(hr));
        if (SUCCEEDED(hr))
            GetAssemblyLoadContextNameFromBinderID(binderID, domain, alcName);
    }

    void GetAssemblyLoadContextNameFromSpec(AssemblySpec *spec, /*out*/ SString &alcName)
    {
        _ASSERTE(spec != nullptr);

        AppDomain *domain = spec->GetAppDomain();
        ICLRPrivBinder* bindContext = spec->GetBindingContext();
        if (bindContext == nullptr)
            bindContext = spec->GetBindingContextFromParentAssembly(domain);

        GetAssemblyLoadContextNameFromBindContext(bindContext, domain, alcName);
    }

    void PopulateBindRequest(/*inout*/ BinderTracing::AssemblyBindOperation::BindRequest &request)
    {
        AssemblySpec *spec = request.AssemblySpec;
        _ASSERTE(spec != nullptr);

        if (request.AssemblyPath.IsEmpty())
            request.AssemblyPath = spec->GetCodeBase();

        if (spec->GetName() != nullptr)
            spec->GetDisplayName(ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE | ASM_DISPLAYF_PUBLIC_KEY_TOKEN, request.AssemblyName);

        DomainAssembly *parentAssembly = spec->GetParentAssembly();
        if (parentAssembly != nullptr)
        {
            PEAssembly *peAssembly = parentAssembly->GetFile();
            _ASSERTE(peAssembly != nullptr);
            peAssembly->GetDisplayName(request.RequestingAssembly);

            AppDomain *domain = parentAssembly->GetAppDomain();
            ICLRPrivBinder *bindContext = peAssembly->GetBindingContext();
            if (bindContext == nullptr)
                bindContext = domain->GetTPABinderContext(); // System.Private.CoreLib returns null

            GetAssemblyLoadContextNameFromBindContext(bindContext, domain, request.RequestingAssemblyLoadContext);
        }

        GetAssemblyLoadContextNameFromSpec(spec, request.AssemblyLoadContext);
    }
}

bool BinderTracing::IsEnabled()
{
#ifdef FEATURE_EVENT_TRACE
    // Just check for the AssemblyLoadStart event being enabled.
    return EventEnabledAssemblyLoadStart();
#endif // FEATURE_EVENT_TRACE
    return false;
}

namespace BinderTracing
{
    AssemblyBindOperation::AssemblyBindOperation(AssemblySpec *assemblySpec, const WCHAR *assemblyPath)
        : m_bindRequest { assemblySpec, nullptr, assemblyPath }
        , m_populatedBindRequest { false }
        , m_checkedIgnoreBind { false }
        , m_ignoreBind { false }
        , m_resultAssembly { nullptr }
        , m_cached { false }
    {
        _ASSERTE(assemblySpec != nullptr);

        if (!BinderTracing::IsEnabled() || ShouldIgnoreBind())
            return;

        PopulateBindRequest(m_bindRequest);
        m_populatedBindRequest = true;
        FireAssemblyLoadStart(m_bindRequest);
    }

    AssemblyBindOperation::~AssemblyBindOperation()
    {
        if (!BinderTracing::IsEnabled() || ShouldIgnoreBind())
            return;

        // Make sure the bind request is populated. Tracing may have been enabled mid-bind.
        if (!m_populatedBindRequest)
            PopulateBindRequest(m_bindRequest);

        FireAssemblyLoadStop(m_bindRequest, m_resultAssembly, m_cached);

        if (m_resultAssembly != nullptr)
            m_resultAssembly->Release();
    }

    void AssemblyBindOperation::SetResult(PEAssembly *assembly, bool cached)
    {
        _ASSERTE(m_resultAssembly == nullptr);
        m_resultAssembly = assembly;
        if (m_resultAssembly != nullptr)
            m_resultAssembly->AddRef();

        m_cached = cached;
    }

    bool AssemblyBindOperation::ShouldIgnoreBind()
    {
        if (m_checkedIgnoreBind)
            return m_ignoreBind;

        // ActivityTracker or EventSource may have triggered the system satellite load.
        // Don't track system satellite binding to avoid potential infinite recursion.
        m_ignoreBind = m_bindRequest.AssemblySpec->IsMscorlibSatellite();
        m_checkedIgnoreBind = true;
        return m_ignoreBind;
    }
}

namespace BinderTracing
{
#ifdef FEATURE_EVENT_TRACE
    void ResolutionAttemptedOperation::FireEventForStage(Stage stage)
    {
        if (stage == Stage::NotYetStarted)
        {
            return;
        }

        StackSString errorMsg, resultAssemblyName, resultAssemblyPath;
        uint16_t result;

        assert(m_pAssemblyName != nullptr);

        // If a resolution stage is reached (by calling GoToStage()), FireEventForStage() will
        // be called twice: once it starts, and once it finishes.
        if (m_lastStage != stage)
        {
            result = static_cast<uint16_t>(Result::Started);
            // Leave errorMsg empty in this case.
        }
        else
        {
            switch (m_hr)
            {
                case HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND):
                    static_assert(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == COR_E_FILENOTFOUND,
                                  "COR_E_FILENOTFOUND has sane value");

                    result = static_cast<uint16_t>(Result::AssemblyNotFound);
                    // TODO: How do we signal the paths we tried looking this assembly in?
                    errorMsg.Set(W("Could not locate assembly"));
                    break;

                case FUSION_E_APP_DOMAIN_LOCKED:
                    result = static_cast<uint16_t>(Result::IncompatbileVersion);

                    {
                        const auto &reqVersion = m_pAssemblyName->GetVersion();
                        const auto &foundVersion = m_pFoundAssembly->GetAssemblyName()->GetVersion();
                        errorMsg.Printf(W("Assembly found, but requested version %d.%d.%d.%d is incompatible with found version %d.%d.%d.%d"),
                                        reqVersion->GetMajor(), reqVersion->GetMinor(),
                                        reqVersion->GetBuild(), reqVersion->GetRevision(),
                                        foundVersion->GetMajor(), foundVersion->GetMinor(),
                                        foundVersion->GetBuild(), foundVersion->GetRevision());
                    }
                    break;

                case FUSION_E_REF_DEF_MISMATCH:
                    result = static_cast<uint16_t>(Result::MismatchedAssemblyName);
                    errorMsg.Printf(W("Name mismatch: found %s instead"),
                                    m_pFoundAssembly->GetAssemblyName()->GetSimpleName().GetUnicode());
                    break;

                default:
                    if (SUCCEEDED(m_hr))
                    {
                        result = static_cast<uint16_t>(Result::Success);
                        // Leave errorMsg empty in this case.
                    }
                    else
                    {
                        result = static_cast<uint16_t>(Result::Unknown);
                        errorMsg.Printf(W("Resolution failed with unknown HRESULT (%08x)"), m_hr);
                    }
            }

            if (m_pFoundAssembly != nullptr)
            {
                resultAssemblyName = m_pFoundAssembly->GetAssemblyName()->GetSimpleName();
                resultAssemblyPath = m_pFoundAssembly->GetPEImage()->GetPath();
            }
            else
            {
                assert(!SUCCEEDED(m_hr));
            }
        }

        StackSString assemblyLoadContext;
        if (m_pManagedALC != 0)
        {
            GetAssemblyLoadContextNameFromManagedALC(m_pManagedALC, assemblyLoadContext);
        }
        else
        {
            assemblyLoadContext.Set(W("Default"));
        }

        FireEtwResolutionAttempted(
            GetClrInstanceId(),
            m_pAssemblyName->GetSimpleName(),
            static_cast<uint16_t>(stage),
            assemblyLoadContext,
            result,
            resultAssemblyName,
            resultAssemblyPath,
            errorMsg);
    }
#endif // FEATURE_EVENT_TRACE
}

void BinderTracing::PathProbed(const WCHAR *path, BinderTracing::PathSource source, HRESULT hr)
{
    FireEtwKnownPathProbed(GetClrInstanceId(), path, source, hr);
}