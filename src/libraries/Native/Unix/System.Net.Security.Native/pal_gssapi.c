// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_utilities.h"
#include "pal_gssapi.h"

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
static gss_OID_desc gss_mech_spnego_OID_desc = {.length = ARRAY_SIZE(gss_spnego_oid_value) - 1,
                                                .elements = gss_spnego_oid_value};
static char gss_ntlm_oid_value[] =
    "\x2b\x06\x01\x04\x01\x82\x37\x02\x02\x0a"; // Binary representation of NTLM OID
                                                // (https://msdn.microsoft.com/en-us/library/cc236636.aspx)
static gss_OID_desc gss_mech_ntlm_OID_desc = {.length = ARRAY_SIZE(gss_ntlm_oid_value) - 1,
                                              .elements = gss_ntlm_oid_value};
#endif

// transfers ownership of the underlying data from gssBuffer to PAL_GssBuffer
static void NetSecurityNative_MoveBuffer(gss_buffer_t gssBuffer, PAL_GssBuffer* targetBuffer)
{
    assert(gssBuffer != NULL);
    assert(targetBuffer != NULL);

    targetBuffer->length = (uint64_t)(gssBuffer->length);
    targetBuffer->data = (uint8_t*)(gssBuffer->value);
}

static uint32_t NetSecurityNative_AcquireCredSpNego(uint32_t* minorStatus,
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

    // call gss_set_cred_option with GSS_KRB5_CRED_NO_CI_FLAGS_X to support Kerberos Sign Only option from *nix client against a windows server
#if HAVE_GSS_KRB5_CRED_NO_CI_FLAGS_X
    if (majorStatus == GSS_S_COMPLETE)
    {
        GssBuffer emptyBuffer = GSS_C_EMPTY_BUFFER;
        majorStatus = gss_set_cred_option(minorStatus, outputCredHandle, GSS_KRB5_CRED_NO_CI_FLAGS_X, &emptyBuffer);
    }
#endif

    return majorStatus;
}

uint32_t
NetSecurityNative_InitiateCredSpNego(uint32_t* minorStatus, GssName* desiredName, GssCredId** outputCredHandle)
{
    return NetSecurityNative_AcquireCredSpNego(minorStatus, desiredName, GSS_C_INITIATE, outputCredHandle);
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

uint32_t NetSecurityNative_ImportTargetName(uint32_t* minorStatus,
                                            char* inputName,
                                            uint32_t inputNameLen,
                                            GssName** outputName)
{
    assert(minorStatus != NULL);
    assert(inputName != NULL);
    assert(outputName != NULL);
    assert(*outputName == NULL);

    GssBuffer inputNameBuffer = {.length = inputNameLen, .value = inputName};
    return gss_import_name(minorStatus, &inputNameBuffer, GSS_C_NT_HOSTBASED_SERVICE, outputName);
}

uint32_t NetSecurityNative_InitSecContext(uint32_t* minorStatus,
                                          GssCredId* claimantCredHandle,
                                          GssCtxId** contextHandle,
                                          uint32_t isNtlm,
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
    assert(isNtlm == 0 || isNtlm == 1);
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
    if (isNtlm)
    {
        desiredMech = GSS_NTLM_MECHANISM;
    }
    else
    {
        desiredMech = GSS_SPNEGO_MECHANISM;
    }
#else
    gss_OID krbMech = (gss_OID)(unsigned long)gss_mech_krb5;
    gss_OID_desc gss_mech_OID_desc;
    if (isNtlm)
    {
        gss_mech_OID_desc = gss_mech_ntlm_OID_desc;
    }
    else
    {
        gss_mech_OID_desc = gss_mech_spnego_OID_desc;
    }

    gss_OID desiredMech = &gss_mech_OID_desc;
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

    *isNtlmUsed = (isNtlm || majorStatus != GSS_S_COMPLETE || gss_oid_equal(outmech, krbMech) == 0) ? 1 : 0;

    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    return majorStatus;
}

uint32_t NetSecurityNative_AcceptSecContext(uint32_t* minorStatus,
                                            GssCtxId** contextHandle,
                                            uint8_t* inputBytes,
                                            uint32_t inputLength,
                                            PAL_GssBuffer* outBuffer,
                                            uint32_t* retFlags)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(inputBytes != NULL || inputLength == 0);
    assert(outBuffer != NULL);
    // Note: *contextHandle is null only in the first call and non-null in the subsequent calls

    GssBuffer inputToken = {.length = inputLength, .value = inputBytes};
    GssBuffer gssBuffer = {.length = 0, .value = NULL};

    uint32_t majorStatus = gss_accept_sec_context(minorStatus,
                                                  contextHandle,
                                                  GSS_C_NO_CREDENTIAL,
                                                  &inputToken,
                                                  GSS_C_NO_CHANNEL_BINDINGS,
                                                  NULL,
                                                  NULL,
                                                  &gssBuffer,
                                                  retFlags,
                                                  NULL,
                                                  NULL);

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
                                int32_t isEncrypt,
                                uint8_t* inputBytes,
                                int32_t offset,
                                int32_t count,
                                PAL_GssBuffer* outBuffer)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(isEncrypt == 1 || isEncrypt == 0);
    assert(inputBytes != NULL);
    assert(offset >= 0);
    assert(count >= 0);
    assert(outBuffer != NULL);
    // count refers to the length of the input message. That is, number of bytes of inputBytes
    // starting at offset that need to be wrapped.

    int confState;
    GssBuffer inputMessageBuffer = {.length = (size_t)count, .value = inputBytes + offset};
    GssBuffer gssBuffer;
    uint32_t majorStatus =
        gss_wrap(minorStatus, contextHandle, isEncrypt, GSS_C_QOP_DEFAULT, &inputMessageBuffer, &confState, &gssBuffer);

    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    return majorStatus;
}

uint32_t NetSecurityNative_Unwrap(uint32_t* minorStatus,
                                  GssCtxId* contextHandle,
                                  uint8_t* inputBytes,
                                  int32_t offset,
                                  int32_t count,
                                  PAL_GssBuffer* outBuffer)
{
    assert(minorStatus != NULL);
    assert(contextHandle != NULL);
    assert(inputBytes != NULL);
    assert(offset >= 0);
    assert(count >= 0);
    assert(outBuffer != NULL);

    // count refers to the length of the input message. That is, the number of bytes of inputBytes
    // starting at offset that need to be wrapped.
    GssBuffer inputMessageBuffer = {.length = (size_t)count, .value = inputBytes + offset};
    GssBuffer gssBuffer = {.length = 0, .value = NULL};
    uint32_t majorStatus = gss_unwrap(minorStatus, contextHandle, &inputMessageBuffer, &gssBuffer, NULL, NULL);
    NetSecurityNative_MoveBuffer(&gssBuffer, outBuffer);
    return majorStatus;
}

static uint32_t NetSecurityNative_AcquireCredWithPassword(uint32_t* minorStatus,
                                                          int32_t isNtlm,
                                                          GssName* desiredName,
                                                          char* password,
                                                          uint32_t passwdLen,
                                                          gss_cred_usage_t credUsage,
                                                          GssCredId** outputCredHandle)
{
    assert(minorStatus != NULL);
    assert(isNtlm == 1 || isNtlm == 0);
    assert(desiredName != NULL);
    assert(password != NULL);
    assert(outputCredHandle != NULL);
    assert(*outputCredHandle == NULL);

#if HAVE_GSS_SPNEGO_MECHANISM
    (void)isNtlm; // unused
    // Specifying GSS_SPNEGO_MECHANISM as a desiredMech on OSX fails.
    gss_OID_set desiredMech = GSS_C_NO_OID_SET;
#else
    gss_OID_desc gss_mech_OID_desc;
    if (isNtlm)
    {
        gss_mech_OID_desc = gss_mech_ntlm_OID_desc;
    }
    else
    {
        gss_mech_OID_desc = gss_mech_spnego_OID_desc;
    }

    gss_OID_set_desc gss_mech_OID_set_desc = {.count = 1, .elements = &gss_mech_OID_desc};
    gss_OID_set desiredMech = &gss_mech_OID_set_desc;
#endif

    GssBuffer passwordBuffer = {.length = passwdLen, .value = password};
    uint32_t majorStatus = gss_acquire_cred_with_password(
        minorStatus, desiredName, &passwordBuffer, 0, desiredMech, credUsage, outputCredHandle, NULL, NULL);

    // call gss_set_cred_option with GSS_KRB5_CRED_NO_CI_FLAGS_X to support Kerberos Sign Only option from *nix client against a windows server
#if HAVE_GSS_KRB5_CRED_NO_CI_FLAGS_X
    if (majorStatus == GSS_S_COMPLETE)
    {
        GssBuffer emptyBuffer = GSS_C_EMPTY_BUFFER;
        majorStatus = gss_set_cred_option(minorStatus, outputCredHandle, GSS_KRB5_CRED_NO_CI_FLAGS_X, &emptyBuffer);
    }
#endif

    return majorStatus;
}

uint32_t NetSecurityNative_InitiateCredWithPassword(uint32_t* minorStatus,
                                                    int32_t isNtlm,
                                                    GssName* desiredName,
                                                    char* password,
                                                    uint32_t passwdLen,
                                                    GssCredId** outputCredHandle)
{
    return NetSecurityNative_AcquireCredWithPassword(
        minorStatus, isNtlm, desiredName, password, passwdLen, GSS_C_INITIATE, outputCredHandle);
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
