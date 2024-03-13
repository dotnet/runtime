// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
#include "assemblybinder.h"

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

    void PopulateBindRequest(/*inout*/ BinderTracing::AssemblyBindOperation::BindRequest &request)
    {
        AssemblySpec *spec = request.AssemblySpec;
        _ASSERTE(spec != nullptr);

        if (spec->GetName() != nullptr)
            spec->GetDisplayName(ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE | ASM_DISPLAYF_PUBLIC_KEY_TOKEN, request.AssemblyName);

        DomainAssembly *parentAssembly = spec->GetParentAssembly();
        if (parentAssembly != nullptr)
        {
            PEAssembly *pPEAssembly = parentAssembly->GetPEAssembly();
            _ASSERTE(pPEAssembly != nullptr);
            pPEAssembly->GetDisplayName(request.RequestingAssembly);

            AssemblyBinder *binder = pPEAssembly->GetAssemblyBinder();

            binder->GetNameForDiagnostics(request.RequestingAssemblyLoadContext);
        }

        AssemblyBinder::GetNameForDiagnosticsFromSpec(spec, request.AssemblyLoadContext);
    }

    const WCHAR *s_assemblyNotFoundMessage = W("Could not locate assembly");
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
    static thread_local bool t_AssemblyLoadStartInProgress = false;

    AssemblyBindOperation::AssemblyBindOperation(AssemblySpec *assemblySpec, const WCHAR* assemblyPath)
        : m_bindRequest { assemblySpec, SString{ SString::Empty() }, SString{ assemblyPath } }
        , m_populatedBindRequest { false }
        , m_checkedIgnoreBind { false }
        , m_ignoreBind { false }
        , m_resultAssembly { nullptr }
        , m_cached { false }
    {
        _ASSERTE(assemblySpec != nullptr);

        if (!BinderTracing::IsEnabled() || ShouldIgnoreBind())
            return;

        t_AssemblyLoadStartInProgress = true;
        PopulateBindRequest(m_bindRequest);
        m_populatedBindRequest = true;
        FireAssemblyLoadStart(m_bindRequest);
    }

    AssemblyBindOperation::~AssemblyBindOperation()
    {
        if (BinderTracing::IsEnabled() && !ShouldIgnoreBind())
        {
            t_AssemblyLoadStartInProgress = false;

            // Make sure the bind request is populated. Tracing may have been enabled mid-bind.
            if (!m_populatedBindRequest)
                PopulateBindRequest(m_bindRequest);

            FireAssemblyLoadStop(m_bindRequest, m_resultAssembly, m_cached);
        }

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

        // ActivityTracker or EventSource may have triggered the system satellite load, or load of System.Private.CoreLib
        // Don't track such bindings to avoid potential infinite recursion.
        m_ignoreBind = t_AssemblyLoadStartInProgress && (m_bindRequest.AssemblySpec->IsCoreLib() || m_bindRequest.AssemblySpec->IsCoreLibSatellite());
        m_checkedIgnoreBind = true;
        return m_ignoreBind;
    }
}

namespace BinderTracing
{
    // static
    void ResolutionAttemptedOperation::TraceAppDomainAssemblyResolve(AssemblySpec *spec, PEAssembly *resultAssembly, Exception *exception)
    {
        if (!BinderTracing::IsEnabled())
            return;

        Result result;
        StackSString errorMessage;
        StackSString resultAssemblyName;
        StackSString resultAssemblyPath;
        if (exception != nullptr)
        {
            exception->GetMessage(errorMessage);
            result = Result::Exception;
        }
        else if (resultAssembly != nullptr)
        {
            result = Result::Success;
            resultAssemblyPath = resultAssembly->GetPath();
            resultAssembly->GetDisplayName(resultAssemblyName);
        }
        else
        {
            result = Result::AssemblyNotFound;
            errorMessage.Set(s_assemblyNotFoundMessage);
        }

        StackSString assemblyName;
        spec->GetDisplayName(ASM_DISPLAYF_VERSION | ASM_DISPLAYF_CULTURE | ASM_DISPLAYF_PUBLIC_KEY_TOKEN, assemblyName);

        StackSString alcName;
        AssemblyBinder::GetNameForDiagnosticsFromSpec(spec, alcName);

        FireEtwResolutionAttempted(
            GetClrInstanceId(),
            assemblyName,
            static_cast<uint16_t>(Stage::AppDomainAssemblyResolveEvent),
            alcName,
            static_cast<uint16_t>(result),
            resultAssemblyName,
            resultAssemblyPath,
            errorMessage);
    }
}

void BinderTracing::PathProbed(const WCHAR *path, BinderTracing::PathSource source, HRESULT hr)
{
    FireEtwKnownPathProbed(GetClrInstanceId(), path, source, hr);
}
