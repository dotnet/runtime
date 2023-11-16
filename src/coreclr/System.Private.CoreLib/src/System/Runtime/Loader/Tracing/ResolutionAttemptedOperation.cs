// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Loader.Tracing
{
    // An object of this class manages firing events for all the stages during a binder resolving
    // attempt operation.  It has minimal cost if tracing for this event is disabled.
    //
    // This class should be declared in the stack.  As information is determined by each stage
    // (e.g. an AssemblySpec is initialized), the appropriate Set*() method should be called.  All
    // pointers held by an object of this class must either be a nullptr, or point to a valid
    // object during the time it is in scope.
    //
    // As the resolution progresses to different stages, the GoToStage() method should be called.
    // Calling it will fire an event for the previous stage with whatever context the class had
    // at that point; it is assumed that if GoToStage() is called, the previous stage failed
    // (the HRESULT is read by the dtor to assess success).
    //
    // It holds a reference to a HRESULT (that must be live during the lifetime of this object),
    // which is used to determine the success or failure of a stage either at the moment this
    // class is destructed (e.g. last stage), or when moving from one stage to another.  (This
    // is especially useful if the HRESULT is captured from an exception handler.)
    internal ref partial struct ResolutionAttemptedOperation
    {
        // This must match the ResolutionAttemptedStage value map in ClrEtwAll.man
        public enum Stage : ushort
        {
            FindInLoadContext = 0,
            AssemblyLoadContextLoad = 1,
            ApplicationAssemblies = 2,
            DefaultAssemblyLoadContextFallback = 3,
            ResolveSatelliteAssembly = 4,
            AssemblyLoadContextResolvingEvent = 5,
            AppDomainAssemblyResolveEvent = 6,
            NotYetStarted = 0xffff, // Used as flag to not fire event; not present in value map
        };

        // This must match the ResolutionAttemptedResult value map in ClrEtwAll.man
        private enum Result : ushort
        {
            Success = 0,
            AssemblyNotFound = 1,
            IncompatibleVersion = 2,
            MismatchedAssemblyName = 3,
            Failure = 4,
            Exception = 5,
        };

        private ref readonly int _hr;
        private Stage _stage;
        private bool _tracingEnabled;
        private BinderAssemblyName? _assemblyNameObject;
        private BinderAssembly? _foundAssembly;
        private string _assemblyName = string.Empty;
        private string _assemblyLoadContextName = string.Empty;
        private string? _exceptionMessage;

        public ResolutionAttemptedOperation(BinderAssemblyName? assemblyName, AssemblyLoadContext binder, ref int hResult)
        {
            _hr = ref hResult;
            _stage = Stage.NotYetStarted;
            _tracingEnabled = AssemblyLoadContext.IsTracingEnabled();
            _assemblyNameObject = assemblyName;

            if (!_tracingEnabled)
                return;

            // When binding the main assembly (by code base instead of name), the assembly name will be null. In this special case, we just
            // leave the assembly name empty.
            if (_assemblyNameObject != null)
                _assemblyName = _assemblyNameObject.GetDisplayName(AssemblyNameIncludeFlags.INCLUDE_VERSION | AssemblyNameIncludeFlags.INCLUDE_PUBLIC_KEY_TOKEN);

            _assemblyLoadContextName = binder.ToString();
        }

        public void TraceBindResult(in BindResult bindResult, bool mvidMismatch = false)
        {
            if (_tracingEnabled)
                return;

            string? errorMsg = null;

            // Use the error message that would be reported in the file load exception
            if (mvidMismatch)
            {
                errorMsg = SR.Format(SR.Host_AssemblyResolver_AssemblyAlreadyLoadedInContext, _assemblyName, SR.Host_AssemblyResolver_AssemblyAlreadyLoadedInContext);
            }

            BindResult.AttemptResult? inContextAttempt = bindResult.GetAttemptResult(isInContext: true);
            BindResult.AttemptResult? appAssembliesAttempt = bindResult.GetAttemptResult(isInContext: false);

            if (inContextAttempt is { } inContextAttemptValue)
            {
                // If there the attempt HR represents a success, but the tracked HR represents a failure (e.g. from further validation), report the failed HR
                bool isLastAttempt = appAssembliesAttempt == null;
                TraceStage(Stage.FindInLoadContext,
                    isLastAttempt && (_hr < 0) && (inContextAttemptValue.HResult >= 0) ? _hr : inContextAttemptValue.HResult,
                    inContextAttemptValue.Assembly,
                    mvidMismatch && isLastAttempt ? errorMsg : null);
            }

            if (appAssembliesAttempt is { }  appAssembliesAttemptValue)
            {
                TraceStage(Stage.ApplicationAssemblies,
                    (_hr < 0) && (appAssembliesAttemptValue.HResult >= 0) ? _hr : appAssembliesAttemptValue.HResult,
                    appAssembliesAttemptValue.Assembly,
                    mvidMismatch ? errorMsg : null);
            }
        }

        public void SetFoundAssembly(BinderAssembly assembly) => _foundAssembly = assembly;

        public void GoToStage(Stage stage)
        {
            Debug.Assert(stage != _stage);
            Debug.Assert(stage != Stage.NotYetStarted);

            if (!_tracingEnabled)
                return;

            // Going to a different stage should only happen if the current
            // stage failed (or if the binding process wasn't yet started).
            // Firing the event at this point not only helps timing each binding
            // stage, but avoids keeping track of which stages were reached to
            // resolve the assembly.
            TraceStage(_stage, _hr, _foundAssembly);
            _stage = stage;
            _exceptionMessage = string.Empty;
        }

        public void SetException(Exception ex)
        {
            if (!_tracingEnabled)
                return;

            _exceptionMessage = ex.Message;
        }

        // FEATURE_EVENT_TRACE
        public void Dispose()
        {
            if (!_tracingEnabled)
                return;

            TraceStage(_stage, _hr, _foundAssembly);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "PEImage_GetPath", StringMarshalling = StringMarshalling.Utf16)]
        private static unsafe partial char* PEImage_GetPath(IntPtr pPEImage);

        private unsafe void TraceStage(Stage stage, int hResult, BinderAssembly? resultAssembly, string? customError = null)
        {
            if (!_tracingEnabled || stage == Stage.NotYetStarted)
                return;

            string resultAssemblyName = string.Empty;
            string resultAssemblyPath = string.Empty;

            if (resultAssembly != null)
            {
                resultAssemblyName = resultAssembly.AssemblyName.GetDisplayName(AssemblyNameIncludeFlags.INCLUDE_VERSION | AssemblyNameIncludeFlags.INCLUDE_PUBLIC_KEY_TOKEN);
                resultAssemblyPath = new string(PEImage_GetPath(resultAssembly.PEImage));
            }

            Result result;
            string errorMsg;
            if (customError != null)
            {
                errorMsg = customError;
                result = Result.Failure;
            }
            else if (!string.IsNullOrEmpty(_exceptionMessage))
            {
                errorMsg = _exceptionMessage;
                result = Result.Exception;
            }
            else
            {
                switch (hResult)
                {
                    case HResults.S_FALSE:
                    case HResults.COR_E_FILENOTFOUND:
                        result = Result.AssemblyNotFound;
                        errorMsg = "Could not locate assembly";
                        break;

                    case AssemblyBinderCommon.FUSION_E_APP_DOMAIN_LOCKED:
                        result = Result.IncompatibleVersion;
                        errorMsg = "Requested version";

                        if (_assemblyNameObject != null)
                        {
                            AssemblyVersion version = _assemblyNameObject.Version;
                            errorMsg += $" {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                        }

                        if (resultAssembly != null)
                        {
                            AssemblyVersion version = resultAssembly.AssemblyName.Version;
                            errorMsg += $" is incompatible with found version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                        }

                        break;

                    case HResults.FUSION_E_REF_DEF_MISMATCH:
                        result = Result.MismatchedAssemblyName;
                        errorMsg = $"Requested assembly name ':{_assemblyName}' does not match found assembly name";

                        if (resultAssembly != null)
                        {
                            errorMsg += $" '{resultAssemblyName}'";
                        }

                        break;

                    case >= 0: // SUCCEEDED(hr)
                        result = Result.Success;
                        Debug.Assert(resultAssembly != null);
                        errorMsg = string.Empty;
                        break;

                    default:
                        result = Result.Failure;
                        errorMsg = $"Resolution failed with HRESULT ({_hr:x8})";
                        break;
                }
            }

            NativeRuntimeEventSource.Log.ResolutionAttempted(
                _assemblyName,
                (ushort)stage,
                _assemblyLoadContextName,
                (ushort)result,
                resultAssemblyName,
                resultAssemblyPath,
                errorMsg);
        }
    }

    // This must match the BindingPathSource value map in ClrEtwAll.man
    internal enum PathSource : ushort
    {
        ApplicationAssemblies,
        Unused,
        AppPaths,
        PlatformResourceRoots,
        SatelliteSubdirectory,
        Bundle
    };
}
