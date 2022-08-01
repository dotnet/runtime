// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_utilities.h"
#include "pal_gssapi.h"

#include <minipal/utils.h>

#if HAVE_GSSFW_HEADERS
#include <GSS/GSS.h>
#else
#if HAVE_HEIMDAL_HEADERS
#include <gssapi/gssapi.h>
#include <gssapi/gssapi_krb5.h>
#else
#include <gssapi/gssapi_ext.h>
#include <gssapi/gssapi_krb5.h>
#endif
#endif

#include <assert.h>
#include <string.h>
#include <stdlib.h>

#if defined(GSS_SHIM)
#include <dlfcn.h>
#include "pal_atomic.h"
#endif

c_static_assert(PAL_GSS_C_DELEG_FLAG == GSS_C_DELEG_FLAG);
c_static_assert(PAL_GSS_C_MUTUAL_FLAG == GSS_C_MUTUAL_FLAG);
c_static_assert(PAL_GSS_C_REPLAY_FLAG == GSS_C_REPLAY_FLAG);
c_static_assert(PAL_GSS_C_SEQUENCE_FLAG == GSS_C_SEQUENCE_FLAG);
c_static_assert(PAL_GSS_C_CONF_FLAG == GSS_C_CONF_FLAG);
c_static_assert(PAL_GSS_C_INTEG_FLAG == GSS_C_INTEG_FLAG);
c_static_assert(PAL_GSS_C_ANON_FLAG == GSS_C_ANON_FLAG);
c_static_assert(PAL_GSS_C_PROT_READY_FLAG == GSS_C_PROT_READY_FLAG);
c_static_assert(PAL_GSS_C_TRANS_FLAG == GSS_C_TRANS_FLAG);
c_static_assert(PAL_GSS_C_DCE_STYLE == GSS_C_DCE_STYLE);
c_static_assert(PAL_GSS_C_IDENTIFY_FLAG == GSS_C_IDENTIFY_FLAG);
c_static_assert(PAL_GSS_C_EXTENDED_ERROR_FLAG == GSS_C_EXTENDED_ERROR_FLAG);
c_static_assert(PAL_GSS_C_DELEG_POLICY_FLAG == GSS_C_DELEG_POLICY_FLAG);

c_static_assert(PAL_GSS_COMPLETE == GSS_S_COMPLETE);
c_static_assert(PAL_GSS_CONTINUE_NEEDED == GSS_S_CONTINUE_NEEDED);

#if !HAVE_GSS_SPNEGO_MECHANISM
static char gss_spnego_oid_value[] = "\x2b\x06\x01\x05\x05\x02"; // Binary representation of SPNEGO Oid (RFC 4178)
static gss_OID_desc gss_mech_spnego_OID_desc = {.length = STRING_LENGTH(gss_spnego_oid_value),
                                                .elements = gss_spnego_oid_value};
static char gss_ntlm_oid_value[] =
    "\x2b\x06\x01\x04\x01\x82\x37\x02\x02\x0a"; // Binary representation of NTLM OID
                                                // (https://msdn.microsoft.com/en-us/library/cc236636.aspx)
static gss_OID_desc gss_mech_ntlm_OID_desc = {.length = STRING_LENGTH(gss_ntlm_oid_value),
                                              .elements = gss_ntlm_oid_value};
#endif

#if defined(GSS_SHIM)

#define FOR_ALL_GSS_FUNCTIONS \
    PER_FUNCTION_BLOCK(gss_accept_sec_context) \
    PER_FUNCTION_BLOCK(gss_acquire_cred) \
    PER_FUNCTION_BLOCK(gss_acquire_cred_with_password) \
    PER_FUNCTION_BLOCK(gss_delete_sec_context) \
    PER_FUNCTION_BLOCK(gss_display_name) \
    PER_FUNCTION_BLOCK(gss_display_status) \
    PER_FUNCTION_BLOCK(gss_import_name) \
    PER_FUNCTION_BLOCK(gss_indicate_mechs) \
    PER_FUNCTION_BLOCK(gss_init_sec_context) \
    PER_FUNCTION_BLOCK(gss_inquire_context) \
    PER_FUNCTION_BLOCK(gss_mech_krb5) \
    PER_FUNCTION_BLOCK(gss_oid_equal) \
    PER_FUNCTION_BLOCK(gss_release_buffer) \
    PER_FUNCTION_BLOCK(gss_release_cred) \
    PER_FUNCTION_BLOCK(gss_release_name) \
    PER_FUNCTION_BLOCK(gss_release_oid_set) \
    PER_FUNCTION_BLOCK(gss_unwrap) \
    PER_FUNCTION_BLOCK(gss_wrap) \
    PER_FUNCTION_BLOCK(gss_get_mic) \
    PER_FUNCTION_BLOCK(gss_verify_mic) \
    PER_FUNCTION_BLOCK(GSS_C_NT_USER_NAME) \
    PER_FUNCTION_BLOCK(GSS_C_NT_HOSTBASED_SERVICE)

// define indirection pointers for all functions, like
// static TYPEOF(gss_accept_sec_context)* gss_accept_sec_context_ptr;
#define PER_FUNCTION_BLOCK(fn) \
static TYPEOF(fn)* fn##_ptr;

FOR_ALL_GSS_FUNCTIONS
#undef PER_FUNCTION_BLOCK

static void* volatile s_gssLib = NULL;

// remap gss function use to use indirection pointers
#define gss_accept_sec_context(...)         gss_accept_sec_context_ptr(__VA_ARGS__)
#define gss_acquire_cred(...)               gss_acquire_cred_ptr(__VA_ARGS__)
#define gss_acquire_cred_with_password(...) gss_acquire_cred_with_password_ptr(__VA_ARGS__)
#define gss_delete_sec_context(...)         gss_delete_sec_context_ptr(__VA_ARGS__)
#define gss_display_name(...)               gss_display_name_ptr(__VA_ARGS__)
#define gss_display_status(...)             gss_display_status_ptr(__VA_ARGS__)
#define gss_import_name(...)                gss_import_name_ptr(__VA_ARGS__)
#define gss_indicate_mechs(...)             gss_indicate_mechs_ptr(__VA_ARGS__)
#define gss_init_sec_context(...)           gss_init_sec_context_ptr(__VA_ARGS__)
#define gss_inquire_context(...)            gss_inquire_context_ptr(__VA_ARGS__)
#define gss_oid_equal(...)                  gss_oid_equal_ptr(__VA_ARGS__)
#define gss_release_buffer(...)             gss_release_buffer_ptr(__VA_ARGS__)
#define gss_release_cred(...)               gss_release_cred_ptr(__VA_ARGS__)
#define gss_release_name(...)               gss_release_name_ptr(__VA_ARGS__)
#define gss_release_oid_set(...)            gss_release_oid_set_ptr(__VA_ARGS__)
#define gss_unwrap(...)                     gss_unwrap_ptr(__VA_ARGS__)
#define gss_wrap(...)                       gss_wrap_ptr(__VA_ARGS__)
#define gss_get_mic(...)                    gss_get_mic_ptr(__VA_ARGS__)
#define gss_verify_mic(...)                 gss_verify_mic_ptr(__VA_ARGS__)

#define GSS_C_NT_USER_NAME                  (*GSS_C_NT_USER_NAME_ptr)
#define GSS_C_NT_HOSTBASED_SERVICE          (*GSS_C_NT_HOSTBASED_SERVICE_ptr)
#define gss_mech_krb5                       (*gss_mech_krb5_ptr)

#define gss_lib_name "libgssapi_krb5.so.2"

static int32_t ensure_gss_shim_initialized()
{
    void* lib = dlopen(gss_lib_name, RTLD_LAZY);
    if (lib == NULL) { fprintf(stderr, "Cannot load library %s \nError: %s\n", gss_lib_name, dlerror()); return -1; }

    // check is someone else has opened and published s_gssLib already
    if (!pal_atomic_cas_ptr(&s_gssLib, lib, NULL))
    {
        dlclose(lib);
    }

    // initialize indirection pointers for all functions, like:
    //   gss_accept_sec_context_ptr = (TYPEOF(gss_accept_sec_context)*)dlsym(s_gssLib, "gss_accept_sec_context");
    //   if (gss_accept_sec_context_ptr == NULL) { fprintf(stderr, "Cannot get symbol %s from %s \nError: %s\n", "gss_accept_sec_context", gss_lib_name, dlerror()); return -1; }
#define PER_FUNCTION_BLOCK(fn) \
    fn##_ptr = (TYPEOF(fn)*)dlsym(s_gssLib, #fn); \
    if (fn##_ptr == NULL) { fprintf(stderr, "Cannot get symbol " #fn " from %s \nError: %s\n", gss_lib_name, dlerror()); return -1; }

    FOR_ALL_GSS_FUNCTIONS
#undef PER_FUNCTION_BLOCK

    return 0;
}

#endif // GSS_SHIM

// transfers ownership of the underlying data from gssBuffer to PAL_GssBuffer
static void NetSecurityNative_MoveBuffer(gss_buffer_t gssBuffer, PAL_GssBuffer* targetBuffer)
{
    assert(gssBuffer != NULL);
    assert(targetBuffer != NULL);

    targetBuffer->length = (uint64_t)(gssBuffer->length);
    targetBuffer->data = (uint8_t*)(gssBuffer->value);
}

static uint32_t AcquireCredSpNego(uint32_t* minorStatus,
                                  GssName* desiredName,
                                  gss_cred_usage_t credUsage,
                                  GssCredId** outputCredHandle)
{
    assert(minorStatus != NULL);
    assert(desiredName != NULL);
    assert(outputCredHandle != NULL);
    assert(*outputCredHandle == NULL);

#if HAVE_GSS_SPNEGO_MECHANISM
    gss_OID_set_desc gss_mech_spnego_OID_set_desc = {.count = 1, .elements = GSS_SPNEGO_MECHANISM};
#else
    gss_OID_set_desc gss_mech_spnego_OID_set_desc = {.count = 1, .elements = &gss_mech_spnego_OID_desc};
#endif
    uint32_t majorStatus = gss_acquire_cred(
        minorStatus, desiredName, 0, &gss_mech_spnego_OID_set_desc, credUsage, outputCredHandle, NULL, NULL);

    return majorStatus;
}

uint32_t
NetSecurityNative_InitiateCredSpNego(uint32_t* minorStatus, GssName* desiredName, GssCredId** outputCredHandle)
{
    return AcquireCredSpNego(minorStatus, desiredName, GSS_C_INITIATE, outputCredHandle);
}

uint32_t NetSecurityNative_DeleteSecContext(uint32_t* minorStatus, GssCtxId** contextHandle)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);

    return gss_delete_sec_context(minorStatus, contextHandle, GSS_C_NO_BUFFER);
}

static uint32_t NetSecurityNative_DisplayStatus(uint32_t* minorStatus,
                                                uint32_t statusValue,
                                                int statusType,
                                                PAL_GssBuffer* outBuffer)
{
    assert(minorStatus != NULL);
    assert(outBuffer != NULL);

    uint32_t messageContext = 0; // Must initialize to 0 before calling gss_display_status.
    GssBuffer gssBuffer = {.length = 0, .value = NULL};
    uint32_t majorStatus =
        gss_display_status(minorStatus, statusValue, statusType, GSS_C_NO_OID, &messageContext, &gssBuffer);

    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    return majorStatus;
}

uint32_t
NetSecurityNative_DisplayMinorStatus(uint32_t* minorStatus, uint32_t statusValue, PAL_GssBuffer* outBuffer)
{
    return NetSecurityNative_DisplayStatus(minorStatus, statusValue, GSS_C_MECH_CODE, outBuffer);
}

uint32_t
NetSecurityNative_DisplayMajorStatus(uint32_t* minorStatus, uint32_t statusValue, PAL_GssBuffer* outBuffer)
{
    return NetSecurityNative_DisplayStatus(minorStatus, statusValue, GSS_C_GSS_CODE, outBuffer);
}

uint32_t
NetSecurityNative_ImportUserName(uint32_t* minorStatus, char* inputName, uint32_t inputNameLen, GssName** outputName)
{
    assert(minorStatus != NULL);
    assert(inputName != NULL);
    assert(outputName != NULL);
    assert(*outputName == NULL);

    GssBuffer inputNameBuffer = {.length = inputNameLen, .value = inputName};
    return gss_import_name(minorStatus, &inputNameBuffer, GSS_C_NT_USER_NAME, outputName);
}

uint32_t NetSecurityNative_ImportPrincipalName(uint32_t* minorStatus,
                                               char* inputName,
                                               uint32_t inputNameLen,
                                               GssName** outputName)
{
    assert(minorStatus != NULL);
    assert(inputName != NULL);
    assert(outputName != NULL);
    assert(*outputName == NULL);

    // Principal name will usually be in the form SERVICE/HOST. But SPNEGO protocol prefers
    // GSS_C_NT_HOSTBASED_SERVICE format. That format uses '@' separator instead of '/' between
    // service name and host name. So convert input string into that format.
    char* ptrSlash = memchr(inputName, '/', inputNameLen);
    char* inputNameCopy = NULL;
    if (ptrSlash != NULL)
    {
        inputNameCopy = (char*) malloc(inputNameLen);
        if (inputNameCopy != NULL)
        {
            memcpy(inputNameCopy, inputName, inputNameLen);
            inputNameCopy[ptrSlash - inputName] = '@';
            inputName = inputNameCopy;
        }
        else
        {
          *minorStatus = 0;
          return GSS_S_BAD_NAME;
        }
    }

    GssBuffer inputNameBuffer = {.length = inputNameLen, .value = inputName};
    uint32_t result = gss_import_name(minorStatus, &inputNameBuffer, GSS_C_NT_HOSTBASED_SERVICE, outputName);

    if (inputNameCopy != NULL)
    {
        free(inputNameCopy);
    }

    return result;
}

uint32_t NetSecurityNative_InitSecContext(uint32_t* minorStatus,
                                          GssCredId* claimantCredHandle,
                                          GssCtxId** contextHandle,
                                          uint32_t packageType,
                                          GssName* targetName,
                                          uint32_t reqFlags,
                                          uint8_t* inputBytes,
                                          uint32_t inputLength,
                                          PAL_GssBuffer* outBuffer,
                                          uint32_t* retFlags,
                                          int32_t* isNtlmUsed)
{
    return NetSecurityNative_InitSecContextEx(minorStatus,
                                              claimantCredHandle,
                                              contextHandle,
                                              packageType,
                                              NULL,
                                              0,
                                              targetName,
                                              reqFlags,
                                              inputBytes,
                                              inputLength,
                                              outBuffer,
                                              retFlags,
                                              isNtlmUsed);
}

uint32_t NetSecurityNative_InitSecContextEx(uint32_t* minorStatus,
                                            GssCredId* claimantCredHandle,
                                            GssCtxId** contextHandle,
                                            uint32_t packageType,
                                            void* cbt,
                                            int32_t cbtSize,
                                            GssName* targetName,
                                            uint32_t reqFlags,
                                            uint8_t* inputBytes,
                                            uint32_t inputLength,
                                            PAL_GssBuffer* outBuffer,
                                            uint32_t* retFlags,
                                            int32_t* isNtlmUsed)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(packageType == PAL_GSS_NEGOTIATE || packageType == PAL_GSS_NTLM || packageType == PAL_GSS_KERBEROS);
    assert(targetName != NULL);
    assert(inputBytes != NULL || inputLength == 0);
    assert(outBuffer != NULL);
    assert(retFlags != NULL);
    assert(isNtlmUsed != NULL);
    assert(cbt != NULL || cbtSize == 0);

// Note: claimantCredHandle can be null
// Note: *contextHandle is null only in the first call and non-null in the subsequent calls

#if HAVE_GSS_SPNEGO_MECHANISM
    gss_OID krbMech = GSS_KRB5_MECHANISM;
    gss_OID desiredMech;
    if (packageType == PAL_GSS_NTLM)
    {
        desiredMech = GSS_NTLM_MECHANISM;
    }
    else if (packageType == PAL_GSS_KERBEROS)
    {
        desiredMech = GSS_KRB5_MECHANISM;
    }
    else
    {
        desiredMech = GSS_SPNEGO_MECHANISM;
    }
#else
    gss_OID krbMech = (gss_OID)(unsigned long)gss_mech_krb5;
    gss_OID desiredMech;
    if (packageType == PAL_GSS_NTLM)
    {
        desiredMech = &gss_mech_ntlm_OID_desc;
    }
    else if (packageType == PAL_GSS_KERBEROS)
    {
        desiredMech = gss_mech_krb5;
    }
    else
    {
        desiredMech = &gss_mech_spnego_OID_desc;
    }
#endif

    GssBuffer inputToken = {.length = inputLength, .value = inputBytes};
    GssBuffer gssBuffer = {.length = 0, .value = NULL};
    gss_OID_desc* outmech;

    struct gss_channel_bindings_struct gssCbt;
    if (cbt != NULL)
    {
        memset(&gssCbt, 0, sizeof(struct gss_channel_bindings_struct));
        gssCbt.application_data.length = (size_t)cbtSize;
        gssCbt.application_data.value = cbt;
    }

    uint32_t majorStatus = gss_init_sec_context(minorStatus,
                                                claimantCredHandle,
                                                contextHandle,
                                                targetName,
                                                desiredMech,
                                                reqFlags,
                                                0,
                                                (cbt != NULL) ? &gssCbt : GSS_C_NO_CHANNEL_BINDINGS,
                                                &inputToken,
                                                &outmech,
                                                &gssBuffer,
                                                retFlags,
                                                NULL);

    *isNtlmUsed = (packageType == PAL_GSS_NTLM || majorStatus != GSS_S_COMPLETE || gss_oid_equal(outmech, krbMech) == 0) ? 1 : 0;

    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    return majorStatus;
}

uint32_t NetSecurityNative_AcceptSecContext(uint32_t* minorStatus,
                                            GssCredId* acceptorCredHandle,
                                            GssCtxId** contextHandle,
                                            uint8_t* inputBytes,
                                            uint32_t inputLength,
                                            PAL_GssBuffer* outBuffer,
                                            uint32_t* retFlags,
                                            int32_t* isNtlmUsed)
{
    assert(minorStatus != NULL);
    assert(acceptorCredHandle != NULL);
    assert(contextHandle != NULL);
    assert(inputBytes != NULL || inputLength == 0);
    assert(outBuffer != NULL);
    assert(isNtlmUsed != NULL);
    // Note: *contextHandle is null only in the first call and non-null in the subsequent calls

    GssBuffer inputToken = {.length = inputLength, .value = inputBytes};
    GssBuffer gssBuffer = {.length = 0, .value = NULL};

    gss_OID mechType = GSS_C_NO_OID;
    uint32_t majorStatus = gss_accept_sec_context(minorStatus,
                                                  contextHandle,
                                                  acceptorCredHandle,
                                                  &inputToken,
                                                  GSS_C_NO_CHANNEL_BINDINGS,
                                                  NULL,
                                                  &mechType,
                                                  &gssBuffer,
                                                  retFlags,
                                                  NULL,
                                                  NULL);

#if HAVE_GSS_SPNEGO_MECHANISM
    gss_OID ntlmMech = GSS_NTLM_MECHANISM;
#else
    gss_OID ntlmMech = &gss_mech_ntlm_OID_desc;
#endif

    *isNtlmUsed = (gss_oid_equal(mechType, ntlmMech) != 0) ? 1 : 0;

    // The gss_ntlmssp provider doesn't support impersonation or delegation but fails to set the GSS_C_IDENTIFY_FLAG
    // flag. So, we'll set it here to keep the behavior consistent with Windows platform.
    if (*isNtlmUsed == 1)
    {
        *retFlags |= GSS_C_IDENTIFY_FLAG;
    }

    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    return majorStatus;
}

uint32_t NetSecurityNative_GetUser(uint32_t* minorStatus,
                                   GssCtxId* contextHandle,
                                   PAL_GssBuffer* outBuffer)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(outBuffer != NULL);

    gss_name_t srcName = GSS_C_NO_NAME;

    uint32_t majorStatus = gss_inquire_context(minorStatus,
                                               contextHandle,
                                               &srcName,
                                               NULL,
                                               NULL,
                                               NULL,
                                               NULL,
                                               NULL,
                                               NULL);

    if (majorStatus == GSS_S_COMPLETE)
    {
        GssBuffer gssBuffer = {.length = 0, .value = NULL};
        majorStatus = gss_display_name(minorStatus, srcName, &gssBuffer, NULL);
        if (majorStatus == GSS_S_COMPLETE)
        {
            NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
        }
    }

    if (srcName != NULL)
    {
        majorStatus = gss_release_name(minorStatus, &srcName);
    }

    return majorStatus;
}

uint32_t NetSecurityNative_ReleaseCred(uint32_t* minorStatus, GssCredId** credHandle)
{
    assert(minorStatus != NULL);
    assert(credHandle != NULL);

    return gss_release_cred(minorStatus, credHandle);
}

void NetSecurityNative_ReleaseGssBuffer(void* buffer, uint64_t length)
{
    assert(buffer != NULL);

    uint32_t minorStatus;
    GssBuffer gssBuffer = {.length = (size_t)(length), .value = buffer};
    gss_release_buffer(&minorStatus, &gssBuffer);
}

uint32_t NetSecurityNative_ReleaseName(uint32_t* minorStatus, GssName** inputName)
{
    assert(minorStatus != NULL);
    assert(inputName != NULL);

    return gss_release_name(minorStatus, inputName);
}

uint32_t NetSecurityNative_Wrap(uint32_t* minorStatus,
                                GssCtxId* contextHandle,
                                int32_t* isEncrypt,
                                uint8_t* inputBytes,
                                int32_t count,
                                PAL_GssBuffer* outBuffer)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(isEncrypt != NULL);
    assert(*isEncrypt == 1 || *isEncrypt == 0);
    assert(inputBytes != NULL);
    assert(count >= 0);
    assert(outBuffer != NULL);
    // count refers to the length of the input message. That is, number of bytes of inputBytes
    // that need to be wrapped.

    int confState;
    GssBuffer inputMessageBuffer = {.length = (size_t)count, .value = inputBytes};
    GssBuffer gssBuffer;
    uint32_t majorStatus =
        gss_wrap(minorStatus, contextHandle, *isEncrypt, GSS_C_QOP_DEFAULT, &inputMessageBuffer, &confState, &gssBuffer);

    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    *isEncrypt = confState;
    return majorStatus;
}

uint32_t NetSecurityNative_Unwrap(uint32_t* minorStatus,
                                  GssCtxId* contextHandle,
                                  int32_t* isEncrypt,
                                  uint8_t* inputBytes,
                                  int32_t count,
                                  PAL_GssBuffer* outBuffer)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(isEncrypt != NULL);
    assert(inputBytes != NULL);
    assert(count >= 0);
    assert(outBuffer != NULL);

    // count refers to the length of the input message. That is, the number of bytes of inputBytes
    // starting at offset that need to be wrapped.
    int confState;
    GssBuffer inputMessageBuffer = {.length = (size_t)count, .value = inputBytes};
    GssBuffer gssBuffer = {.length = 0, .value = NULL};
    uint32_t majorStatus = gss_unwrap(minorStatus, contextHandle, &inputMessageBuffer, &gssBuffer, &confState, NULL);
    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    *isEncrypt = confState;
    return majorStatus;
}

uint32_t NetSecurityNative_GetMic(uint32_t* minorStatus,
                                  GssCtxId* contextHandle,
                                  uint8_t* inputBytes,
                                  int32_t inputLength,
                                  PAL_GssBuffer* outBuffer)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(inputBytes != NULL);
    assert(inputLength >= 0);
    assert(outBuffer != NULL);

    GssBuffer inputMessageBuffer = {.length = (size_t)inputLength, .value = inputBytes};
    GssBuffer gssBuffer;
    uint32_t majorStatus =
        gss_get_mic(minorStatus, contextHandle, GSS_C_QOP_DEFAULT, &inputMessageBuffer, &gssBuffer);

    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    return majorStatus;
}

uint32_t NetSecurityNative_VerifyMic(uint32_t* minorStatus,
                                     GssCtxId* contextHandle,
                                     uint8_t* inputBytes,
                                     int32_t inputLength,
                                     uint8_t* tokenBytes,
                                     int32_t tokenLength)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(inputBytes != NULL);
    assert(inputLength >= 0);
    assert(tokenBytes != NULL);
    assert(tokenLength >= 0);

    GssBuffer inputMessageBuffer = {.length = (size_t)inputLength, .value = inputBytes};
    GssBuffer tokenBuffer = {.length = (size_t)tokenLength, .value = tokenBytes};
    GssBuffer gssBuffer;
    uint32_t majorStatus =
        gss_verify_mic(minorStatus, contextHandle, &inputMessageBuffer, &tokenBuffer, NULL);

    return majorStatus;
}

static uint32_t AcquireCredWithPassword(uint32_t* minorStatus,
                                        int32_t packageType,
                                        GssName* desiredName,
                                        char* password,
                                        uint32_t passwdLen,
                                        gss_cred_usage_t credUsage,
                                        GssCredId** outputCredHandle)
{
    assert(minorStatus != NULL);
    assert(packageType == PAL_GSS_NEGOTIATE || packageType == PAL_GSS_NTLM || packageType == PAL_GSS_KERBEROS);
    assert(desiredName != NULL);
    assert(password != NULL);
    assert(outputCredHandle != NULL);
    assert(*outputCredHandle == NULL);

#if HAVE_GSS_SPNEGO_MECHANISM
    (void)packageType; // unused
    // Specifying GSS_SPNEGO_MECHANISM as a desiredMech on OSX fails.
    gss_OID_set desiredMechSet = GSS_C_NO_OID_SET;
#else
    gss_OID_desc gss_mech_OID_desc;
    gss_OID desiredMech;
    if (packageType == PAL_GSS_NTLM)
    {
        desiredMech = &gss_mech_ntlm_OID_desc;
    }
    else if (packageType == PAL_GSS_KERBEROS)
    {
        desiredMech = gss_mech_krb5;
    }
    else
    {
        desiredMech = &gss_mech_spnego_OID_desc;
    }

    gss_OID_set_desc gss_mech_OID_set_desc = {.count = 1, .elements = desiredMech};
    gss_OID_set desiredMechSet = &gss_mech_OID_set_desc;
#endif

    GssBuffer passwordBuffer = {.length = passwdLen, .value = password};
    uint32_t majorStatus = gss_acquire_cred_with_password(
        minorStatus, desiredName, &passwordBuffer, 0, desiredMechSet, credUsage, outputCredHandle, NULL, NULL);

    return majorStatus;
}

uint32_t NetSecurityNative_AcquireAcceptorCred(uint32_t* minorStatus,
                                               GssCredId** outputCredHandle)
{
    return gss_acquire_cred(minorStatus,
                            GSS_C_NO_NAME,
                            GSS_C_INDEFINITE,
                            GSS_C_NO_OID_SET,
                            GSS_C_ACCEPT,
                            outputCredHandle,
                            NULL,
                            NULL);
}

uint32_t NetSecurityNative_InitiateCredWithPassword(uint32_t* minorStatus,
                                                    int32_t packageType,
                                                    GssName* desiredName,
                                                    char* password,
                                                    uint32_t passwdLen,
                                                    GssCredId** outputCredHandle)
{
    return AcquireCredWithPassword(
        minorStatus, packageType, desiredName, password, passwdLen, GSS_C_INITIATE, outputCredHandle);
}

uint32_t NetSecurityNative_IsNtlmInstalled()
{
#if HAVE_GSS_SPNEGO_MECHANISM
    gss_OID ntlmOid = GSS_NTLM_MECHANISM;
#else
    gss_OID ntlmOid = &gss_mech_ntlm_OID_desc;
#endif

    uint32_t majorStatus;
    uint32_t minorStatus;
    gss_OID_set mechSet;
    gss_OID_desc oid;
    uint32_t foundNtlm = 0;

    majorStatus = gss_indicate_mechs(&minorStatus, &mechSet);
    if (majorStatus == GSS_S_COMPLETE)
    {
        for (size_t i = 0; i < mechSet->count; i++)
        {
            oid = mechSet->elements[i];
            if ((oid.length == ntlmOid->length) && (memcmp(oid.elements, ntlmOid->elements, oid.length) == 0))
            {
                foundNtlm = 1;
                break;
            }
        }

        gss_release_oid_set(&minorStatus, &mechSet);
    }

    return foundNtlm;
}

int32_t NetSecurityNative_EnsureGssInitialized()
{
#if defined(GSS_SHIM)
    return ensure_gss_shim_initialized();
#else
    return 0;
#endif
}
