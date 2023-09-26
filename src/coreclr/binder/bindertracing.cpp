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
    ResolutionAttemptedOperation::ResolutionAttemptedOperation(AssemblyName *assemblyName, AssemblyBinder* binder, INT_PTR managedALC, const HRESULT& hr)
        : m_hr { hr }
        , m_stage { Stage::NotYetStarted }
        , m_tracingEnabled { BinderTracing::IsEnabled() }
        , m_assemblyNameObject { assemblyName }
        , m_pFoundAssembly { nullptr }
    {
        _ASSERTE(binder != nullptr || managedALC != 0);

        if (!m_tracingEnabled)
            return;

        // When binding the main assembly (by code base instead of name), the assembly name will be null. In this special case, we just
        // leave the assembly name empty.
        if (m_assemblyNameObject != nullptr)
            m_assemblyNameObject->GetDisplayName(m_assemblyName, AssemblyName::INCLUDE_VERSION | AssemblyName::INCLUDE_PUBLIC_KEY_TOKEN);

        if (managedALC != 0)
        {
            AssemblyBinder::GetNameForDiagnosticsFromManagedALC(managedALC, m_assemblyLoadContextName);
        }
        else
        {
            binder->GetNameForDiagnostics(m_assemblyLoadContextName);
        }
    }

    // This function simply traces out the two stages represented by the bind result.
    // It does not change the stage/assembly of the ResolutionAttemptedOperation class instance.
    void ResolutionAttemptedOperation::TraceBindResult(const BindResult &bindResult, bool mvidMismatch)
    {
        if (!m_tracingEnabled)
            return;

        // Use the error message that would be reported in the file load exception
        StackSString errorMsg;
        if (mvidMismatch)
        {
            StackSString format;
            format.LoadResource(CCompRC::Error, IDS_EE_FILELOAD_ERROR_GENERIC);
            StackSString details;
            details.LoadResource(CCompRC::Error, IDS_HOST_ASSEMBLY_RESOLVER_ASSEMBLY_ALREADY_LOADED_IN_CONTEXT);
            errorMsg.FormatMessage(FORMAT_MESSAGE_FROM_STRING, format.GetUnicode(), 0, 0, m_assemblyName, details);
        }

        const BindResult::AttemptResult *inContextAttempt = bindResult.GetAttempt(true /*foundInContext*/);
        const BindResult::AttemptResult *appAssembliesAttempt = bindResult.GetAttempt(false /*foundInContext*/);

        if (inContextAttempt != nullptr)
        {
            // If there the attempt HR represents a success, but the tracked HR represents a failure (e.g. from further validation), report the failed HR
            bool isLastAttempt = appAssembliesAttempt == nullptr;
            TraceStage(Stage::FindInLoadContext,
                isLastAttempt && FAILED(m_hr) && SUCCEEDED(inContextAttempt->HResult) ? m_hr : inContextAttempt->HResult,
                inContextAttempt->Assembly,
                mvidMismatch && isLastAttempt ? errorMsg.GetUnicode() : nullptr);
        }

        if (appAssembliesAttempt != nullptr)
            TraceStage(Stage::ApplicationAssemblies, FAILED(m_hr) && SUCCEEDED(appAssembliesAttempt->HResult) ? m_hr : appAssembliesAttempt->HResult, appAssembliesAttempt->Assembly, mvidMismatch ? errorMsg.GetUnicode() : nullptr);
    }

    void ResolutionAttemptedOperation::TraceStage(Stage stage, HRESULT hr, BINDER_SPACE::Assembly *resultAssembly, const WCHAR *customError)
    {
        if (!m_tracingEnabled || stage == Stage::NotYetStarted)
            return;

        PathString resultAssemblyName;
        StackSString resultAssemblyPath;
        if (resultAssembly != nullptr)
        {
            resultAssembly->GetAssemblyName()->GetDisplayName(resultAssemblyName, AssemblyName::INCLUDE_VERSION | AssemblyName::INCLUDE_PUBLIC_KEY_TOKEN);
            resultAssemblyPath = resultAssembly->GetPEImage()->GetPath();
        }

        Result result;
        StackSString errorMsg;
        if (customError != nullptr)
        {
            errorMsg.Set(customError);
            result = Result::Failure;
        }
        else if (!m_exceptionMessage.IsEmpty())
        {
            errorMsg = m_exceptionMessage;
            result = Result::Exception;
        }
        else
        {
            switch (hr)
            {
                case S_FALSE:
                case HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND):
                    static_assert(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == COR_E_FILENOTFOUND,
                                    "COR_E_FILENOTFOUND has sane value");

                    result = Result::AssemblyNotFound;
                    errorMsg.Set(s_assemblyNotFoundMessage);
                    break;

                case FUSION_E_APP_DOMAIN_LOCKED:
                    result = Result::IncompatibleVersion;

                    {
                        SString errorMsgUtf8(SString::Utf8, "Requested version");
                        if (m_assemblyNameObject != nullptr)
                        {
                            const auto &reqVersion = m_assemblyNameObject->GetVersion();
                            errorMsgUtf8.AppendPrintf(" %d.%d.%d.%d",
                                reqVersion->GetMajor(),
                                reqVersion->GetMinor(),
                                reqVersion->GetBuild(),
                                reqVersion->GetRevision());
                        }

                        errorMsgUtf8.AppendUTF8(" is incompatible with found version");
                        if (resultAssembly != nullptr)
                        {
                            const auto &foundVersion = resultAssembly->GetAssemblyName()->GetVersion();
                            errorMsgUtf8.AppendPrintf(" %d.%d.%d.%d",
                                foundVersion->GetMajor(),
                                foundVersion->GetMinor(),
                                foundVersion->GetBuild(),
                                foundVersion->GetRevision());
                        }
                        errorMsg.Set(errorMsgUtf8.GetUnicode());
                    }
                    break;

                case FUSION_E_REF_DEF_MISMATCH:
                    result = Result::MismatchedAssemblyName;
                    errorMsg.Append(W("Requested assembly name '"));
                    errorMsg.Append(m_assemblyName.GetUnicode());
                    errorMsg.Append(W("' does not match found assembly name"));
                    if (resultAssembly != nullptr)
                    {
                        errorMsg.Append(W(" '"));
                        errorMsg.Append(resultAssemblyName.GetUnicode());
                        errorMsg.Append(W("'"));
                    }

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
                        errorMsg.Printf("Resolution failed with HRESULT (%08x)", m_hr);
                    }
            }
        }

        FireEtwResolutionAttempted(
            GetClrInstanceId(),
            m_assemblyName,
            static_cast<uint16_t>(stage),
            m_assemblyLoadContextName,
            static_cast<uint16_t>(result),
            resultAssemblyName,
            resultAssemblyPath,
            errorMsg);
    }

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
