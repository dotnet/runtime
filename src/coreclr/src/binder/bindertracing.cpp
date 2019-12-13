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
#include "bindresult.hpp"

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
        if (managedALC == GetAppDomain()->GetTPABinderContext()->GetManagedAssemblyLoadContext())
        {
            alcName.Set(W("Default"));
            return;
        }

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

    void PopulateAttemptInfo(/*inout*/ BinderTracing::ResolutionAttemptedOperation::AttemptInfo &info)
    {
        _ASSERTE(info.AssemblyNameObject != nullptr);

        info.AssemblyNameObject->GetDisplayName(info.AssemblyName, AssemblyName::INCLUDE_VERSION | AssemblyName::INCLUDE_PUBLIC_KEY_TOKEN);

        if (info.ManagedAssemblyLoadContext != 0)
        {
            GetAssemblyLoadContextNameFromManagedALC(info.ManagedAssemblyLoadContext, info.AssemblyLoadContext);
        }
        else
        {
            GetAssemblyLoadContextNameFromBinderID(info.BinderID, GetAppDomain(), info.AssemblyLoadContext);
        }
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
    ResolutionAttemptedOperation::ResolutionAttemptedOperation(AssemblyName *assemblyName, UINT_PTR binderID, const HRESULT& hr)
        : ResolutionAttemptedOperation(assemblyName, binderID, 0, hr)
    { }

    ResolutionAttemptedOperation::ResolutionAttemptedOperation(AssemblyName *assemblyName, INT_PTR managedALC, const HRESULT& hr)
        : ResolutionAttemptedOperation(assemblyName, 0, managedALC, hr)
    { }

    ResolutionAttemptedOperation::ResolutionAttemptedOperation(AssemblyName *assemblyName, UINT_PTR binderID, INT_PTR managedALC, const HRESULT& hr)
        : m_hr { hr }
        , m_stage { Stage::NotYetStarted }
        , m_attemptInfo { assemblyName, binderID, managedALC }
        , m_pFoundAssembly { nullptr }
    {
        if (!BinderTracing::IsEnabled())
            return;

        _ASSERTE(assemblyName != nullptr);
        PopulateAttemptInfo(m_attemptInfo);
        m_populatedAttemptInfo = true;
    }

    // This function simply traces out the two stages represented by the bind result.
    // It does not change the stage/assembly of the ResolutionAttemptedOperation class instance.
    void ResolutionAttemptedOperation::TraceBindResult(const BindResult &bindResult)
    {
        if (!BinderTracing::IsEnabled())
            return;

        const BindResult::AttemptResult *attempt = bindResult.GetAttempt(true /*foundInContext*/);
        if (attempt != nullptr)
            TraceStage(Stage::FindInLoadContext, attempt->HResult, attempt->Assembly);

        attempt = bindResult.GetAttempt(false /*foundInContext*/);
        if (attempt != nullptr)
            TraceStage(Stage::PlatformAssemblies, attempt->HResult, attempt->Assembly);
    }

    void ResolutionAttemptedOperation::TraceStage(Stage stage, HRESULT hr, BINDER_SPACE::Assembly *resultAssembly)
    {
        if (!BinderTracing::IsEnabled() || stage == Stage::NotYetStarted)
            return;

        if (!m_populatedAttemptInfo)
            PopulateAttemptInfo(m_attemptInfo);

        PathString resultAssemblyName;
        StackSString resultAssemblyPath;
        if (resultAssembly != nullptr)
        {
            resultAssembly->GetAssemblyName()->GetDisplayName(resultAssemblyName, AssemblyName::INCLUDE_VERSION | AssemblyName::INCLUDE_PUBLIC_KEY_TOKEN);
            resultAssemblyPath = resultAssembly->GetPEImage()->GetPath();
        }

        Result result;
        StackSString errorMsg;
        switch (hr)
        {
            case S_FALSE:
            case HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND):
                static_assert(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == COR_E_FILENOTFOUND,
                                "COR_E_FILENOTFOUND has sane value");

                result = Result::AssemblyNotFound;
                errorMsg.Set(W("Could not locate assembly"));
                break;

            case FUSION_E_APP_DOMAIN_LOCKED:
                result = Result::IncompatibleVersion;

                {
                    const auto &reqVersion = m_attemptInfo.AssemblyNameObject->GetVersion();
                    errorMsg.Printf(W("Requested version %d.%d.%d.%d is incompatible with found version"),
                        reqVersion->GetMajor(),
                        reqVersion->GetMinor(),
                        reqVersion->GetBuild(),
                        reqVersion->GetRevision());
                    if (resultAssembly != nullptr)
                    {
                        const auto &foundVersion = resultAssembly->GetAssemblyName()->GetVersion();
                        errorMsg.AppendPrintf(W(" %d.%d.%d.%d"),
                            foundVersion->GetMajor(),
                            foundVersion->GetMinor(),
                            foundVersion->GetBuild(),
                            foundVersion->GetRevision());
                    }
                }
                break;

            case FUSION_E_REF_DEF_MISMATCH:
                result = Result::MismatchedAssemblyName;
                errorMsg.Printf(W("Requested assembly name '%s' does not match found assembly name"), m_attemptInfo.AssemblyName.GetUnicode());
                if (resultAssembly != nullptr)
                    errorMsg.AppendPrintf(W(" '%s'"), resultAssemblyName.GetUnicode());

                break;

            default:
                if (SUCCEEDED(hr))
                {
                    result = Result::Success;
                    _ASSERTE(resultAssembly != nullptr);
                    // Leave errorMsg empty in this case.
                }
                else
                {
                    result = Result::Failure;
                    errorMsg.Printf(W("Resolution failed with HRESULT (%08x)"), m_hr);
                }
        }

        FireEtwResolutionAttempted(
            GetClrInstanceId(),
            m_attemptInfo.AssemblyName,
            static_cast<uint16_t>(stage),
            m_attemptInfo.AssemblyLoadContext,
            static_cast<uint16_t>(result),
            resultAssemblyName,
            resultAssemblyPath,
            errorMsg);
    }
}

void BinderTracing::PathProbed(const WCHAR *path, BinderTracing::PathSource source, HRESULT hr)
{
    FireEtwKnownPathProbed(GetClrInstanceId(), path, source, hr);
}