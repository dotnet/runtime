// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once
#include "pal_compiler.h"

#if HAVE_GSSFW_HEADERS || HAVE_HEIMDAL_HEADERS
typedef struct gss_name_t_desc_struct GssName;
typedef struct gss_ctx_id_t_desc_struct GssCtxId;
typedef struct gss_cred_id_t_desc_struct GssCredId;
typedef struct gss_buffer_desc_struct GssBuffer;
#else
typedef struct gss_name_struct GssName;
typedef struct gss_ctx_id_struct GssCtxId;
typedef struct gss_cred_id_struct GssCredId;
typedef struct gss_buffer_desc_struct GssBuffer;
#endif

typedef enum
{
    PAL_GSS_COMPLETE = 0,
    PAL_GSS_CONTINUE_NEEDED = 1
} PAL_GssStatus;

typedef enum
{
    PAL_GSS_C_DELEG_FLAG = 0x1,
    PAL_GSS_C_MUTUAL_FLAG = 0x2,
    PAL_GSS_C_REPLAY_FLAG = 0x4,
    PAL_GSS_C_SEQUENCE_FLAG = 0x8,
    PAL_GSS_C_CONF_FLAG = 0x10,
    PAL_GSS_C_INTEG_FLAG = 0x20,
    PAL_GSS_C_ANON_FLAG = 0x40,
    PAL_GSS_C_PROT_READY_FLAG = 0x80,
    PAL_GSS_C_TRANS_FLAG = 0x100,
    PAL_GSS_C_DCE_STYLE = 0x1000,
    PAL_GSS_C_IDENTIFY_FLAG = 0x2000,
    PAL_GSS_C_EXTENDED_ERROR_FLAG = 0x4000,
    PAL_GSS_C_DELEG_POLICY_FLAG = 0x8000
} PAL_GssFlags;

/*
Issue: #7342
Disable padded warning which occurs in case of 32-bit builds
*/
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wpadded"
typedef struct
{
    uint64_t length;
    uint8_t* data;
} PAL_GssBuffer;
#pragma clang diagnostic pop

/*
Shims the gss_release_buffer method.
*/
DLLEXPORT void NetSecurityNative_ReleaseGssBuffer(void* buffer, uint64_t length);

/*
Shims the gss_display_status method for minor status (status_type = GSS_C_MECH_CODE).
*/
DLLEXPORT uint32_t
NetSecurityNative_DisplayMinorStatus(uint32_t* minorStatus, uint32_t statusValue, PAL_GssBuffer* outBuffer);

/*
Shims the gss_display_status method for major status (status_type = GSS_C_GSS_CODE).
*/
DLLEXPORT uint32_t
NetSecurityNative_DisplayMajorStatus(uint32_t* minorStatus, uint32_t statusValue, PAL_GssBuffer* outBuffer);

/*
Shims the gss_import_name method with nametype = GSS_C_NT_USER_NAME.
*/
DLLEXPORT uint32_t
NetSecurityNative_ImportUserName(uint32_t* minorStatus, char* inputName, uint32_t inputNameLen, GssName** outputName);

/*
Shims the gss_import_name method with nametype = GSS_C_NT_USER_NAME.
*/
DLLEXPORT uint32_t NetSecurityNative_ImportTargetName(uint32_t* minorStatus,
                                                         char* inputName,
                                                         uint32_t inputNameLen,
                                                         GssName** outputName);

/*
Shims the gss_release_name method.
*/
DLLEXPORT uint32_t NetSecurityNative_ReleaseName(uint32_t* minorStatus, GssName** inputName);

/*
Shims the gss_acquire_cred method with SPNEGO oids with  GSS_C_INITIATE.
*/
DLLEXPORT uint32_t
NetSecurityNative_InitiateCredSpNego(uint32_t* minorStatus, GssName* desiredName, GssCredId** outputCredHandle);

/*
Shims the gss_release_cred method.
*/
DLLEXPORT uint32_t NetSecurityNative_ReleaseCred(uint32_t* minorStatus, GssCredId** credHandle);

/*
Shims the gss_init_sec_context method with SPNEGO oids.
*/
DLLEXPORT uint32_t NetSecurityNative_InitSecContext(uint32_t* minorStatus,
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
                                                    int32_t* isNtlmUsed);

/*
Shims the gss_accept_sec_context method.
*/
DLLEXPORT uint32_t NetSecurityNative_AcceptSecContext(uint32_t* minorStatus,
                                                      GssCtxId** contextHandle,
                                                      uint8_t* inputBytes,
                                                      uint32_t inputLength,
                                                      PAL_GssBuffer* outBuffer,
                                                      uint32_t* retFlags);

/*

Shims the gss_delete_sec_context method.
*/
DLLEXPORT uint32_t NetSecurityNative_DeleteSecContext(uint32_t* minorStatus, GssCtxId** contextHandle);

/*
Shims the gss_wrap method.
*/
DLLEXPORT uint32_t NetSecurityNative_Wrap(uint32_t* minorStatus,
                                          GssCtxId* contextHandle,
                                          int32_t isEncrypt,
                                          uint8_t* inputBytes,
                                          int32_t offset,
                                          int32_t count,
                                          PAL_GssBuffer* outBuffer);

/*
Shims the gss_unwrap method.
*/
DLLEXPORT uint32_t NetSecurityNative_Unwrap(uint32_t* minorStatus,
                                            GssCtxId* contextHandle,
                                            uint8_t* inputBytes,
                                            int32_t offset,
                                            int32_t count,
                                            PAL_GssBuffer* outBuffer);

/*
Shims the gss_acquire_cred_with_password method with GSS_C_INITIATE.
*/
DLLEXPORT uint32_t NetSecurityNative_InitiateCredWithPassword(uint32_t* minorStatus,
                                                              int32_t isNtlm,
                                                              GssName* desiredName,
                                                              char* password,
                                                              uint32_t passwdLen,
                                                              GssCredId** outputCredHandle);

/*
Shims the gss_indicate_mechs method to detect if NTLM mech is installed.
*/
DLLEXPORT uint32_t NetSecurityNative_IsNtlmInstalled(void);

/*
Shims gss_inquire_context and gss_display_name to get the remote user principal name.
*/
DLLEXPORT uint32_t NetSecurityNative_GetUser(uint32_t* minorStatus,
                                             GssCtxId* contextHandle,
                                             PAL_GssBuffer* outBuffer);
