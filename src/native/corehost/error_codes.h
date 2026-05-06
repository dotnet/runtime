// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __ERROR_CODES_H__
#define __ERROR_CODES_H__

// These error and exit codes are document in the host-error-codes.md
enum StatusCode
{
    // Success
    Success                             = 0,            // Operation was successful
    Success_HostAlreadyInitialized      = 0x00000001,   // Initialization was successful, but another host context is already initialized
    Success_DifferentRuntimeProperties  = 0x00000002,   // Initialization was successful, but another host context is already initialized and the requested context specified runtime properties which are not the same

    // Failure
    InvalidArgFailure                   = 0x80008081,   // One or more arguments are invalid
    CoreHostLibLoadFailure              = 0x80008082,   // Failed to load a hosting component
    CoreHostLibMissingFailure           = 0x80008083,   // One of the hosting components is missing
    CoreHostEntryPointFailure           = 0x80008084,   // One of the hosting components is missing a required entry point
    CurrentHostFindFailure              = 0x80008085,   // Failed to get the path of the current hosting component and determine the .NET installation location
    // unused                           = 0x80008086,
    CoreClrResolveFailure               = 0x80008087,   // The `coreclr` library could not be found
    CoreClrBindFailure                  = 0x80008088,   // Failed to load the `coreclr` library or finding one of the required entry points
    CoreClrInitFailure                  = 0x80008089,   // Call to `coreclr_initialize` failed
    CoreClrExeFailure                   = 0x8000808a,   // Call to `coreclr_execute_assembly` failed
    ResolverInitFailure                 = 0x8000808b,   // Initialization of the `hostpolicy` dependency resolver failed
    ResolverResolveFailure              = 0x8000808c,   // Resolution of dependencies in `hostpolicy` failed
    // unused                           = 0x8000808d,
    LibHostInitFailure                  = 0x8000808e,   // Initialization of the `hostpolicy` library failed
    // unused                           = 0x8000808f,
    // unused                           = 0x80008090,
    // unused                           = 0x80008091,
    LibHostInvalidArgs                  = 0x80008092,   // Arguments to `hostpolicy` are invalid
    InvalidConfigFile                   = 0x80008093,   // The `.runtimeconfig.json` file is invalid
    AppArgNotRunnable                   = 0x80008094,   // [internal usage only]
    AppHostExeNotBoundFailure           = 0x80008095,   // `apphost` failed to determine which application to run
    FrameworkMissingFailure             = 0x80008096,   // Failed to find a compatible framework version
    HostApiFailed                       = 0x80008097,   // Host command failed
    HostApiBufferTooSmall               = 0x80008098,   // Buffer provided to a host API is too small to fit the requested value
    // unused                           = 0x80008099,
    AppPathFindFailure                  = 0x8000809a,   // Application path imprinted in `apphost` doesn't exist
    SdkResolveFailure                   = 0x8000809b,   // Failed to find the requested SDK
    FrameworkCompatFailure              = 0x8000809c,   // Application has multiple references to the same framework which are not compatible
    FrameworkCompatRetry                = 0x8000809d,   // [internal usage only]
    // unused                           = 0x8000809e,
    BundleExtractionFailure             = 0x8000809f,   // Error extracting single-file bundle
    BundleExtractionIOError             = 0x800080a0,   // Error reading or writing files during single-file bundle extraction
    LibHostDuplicateProperty            = 0x800080a1,   // The application's `.runtimeconfig.json` contains a runtime property which is produced by the hosting layer
    HostApiUnsupportedVersion           = 0x800080a2,   // Feature which requires certain version of the hosting layer was used on a version which doesn't support it
    HostInvalidState                    = 0x800080a3,   // Current state is incompatible with the requested operation
    HostPropertyNotFound                = 0x800080a4,   // Property requested by `hostfxr_get_runtime_property_value` doesn't exist
    HostIncompatibleConfig              = 0x800080a5,   // Host configuration is incompatible with existing host context
    HostApiUnsupportedScenario          = 0x800080a6,   // Hosting API does not support the requested scenario
    HostFeatureDisabled                 = 0x800080a7,   // Support for a requested feature is disabled
};

#define STATUS_CODE_SUCCEEDED(status_code) ((static_cast<int>(static_cast<StatusCode>(status_code))) >= 0)

#endif // __ERROR_CODES_H__
