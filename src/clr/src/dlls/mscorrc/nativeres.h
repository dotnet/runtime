// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#ifndef __NATIVE_BINDER_RESOURCE_H_
#define __NATIVE_BINDER_RESOURCE_H_

#ifdef FEATURE_FUSION

#define ID_FUSLOG_NGEN_BIND_IL_PROVIDED                   10204
#define ID_FUSLOG_NGEN_BIND_LOADFROM_NOT_ALLOWED          10174
#define ID_FUSLOG_NGEN_BIND_SUCCESS                       10171
#define ID_FUSLOG_NGEN_BIND_FAIL                          10172
#define ID_FUSLOG_NGEN_BIND_NO_MATCH                      10179
#define ID_FUSLOG_NGEN_BIND_NGEN_REJECT_CONFIG_MASK       10178
#define ID_FUSLOG_NGEN_BIND_NI_DEPEND_START               10206
#define ID_FUSLOG_NGEN_BIND_IL_DEPEND_START               10207
#define ID_FUSLOG_NGEN_BIND_VALIDATE_DEPENDENCIES         10197
#define ID_FUSLOG_NGEN_BIND_VALIDATE_DEPENDENCIES_SUCCESS 10200
#define ID_FUSLOG_NGEN_BIND_REJECT_IL_NOT_FOUND           10183
#define ID_FUSLOG_NGEN_BIND_MISSING_FOUND                 10209

// Ids 10400 - 10500 reserved for new native binder messages
// If more ids are needed, reserve them in fusres.h
#define ID_FUSLOG_NGEN_BIND_AUXCORRUPTION_GENERAL         10400
#define ID_FUSLOG_NGEN_BIND_AUX_REOPTIMIZED               10401
#define ID_FUSLOG_NGEN_BIND_REJECT_SIG                    10402
#define ID_FUSLOG_NGEN_BIND_REJECT_TP                     10403
#define ID_FUSLOG_NGEN_BIND_REJECT_TIMESTAMP_SIGFALLBACK  10404
#define ID_FUSLOG_NGEN_BIND_REJECT_TIMESTAMP_TPFALLBACK   10405
#define ID_FUSLOG_NGEN_BIND_CHANGED_BINDING_POLICY        10406
#define ID_FUSLOG_NGEN_BIND_REJECT_OPTOUT                 10407

#endif // FEATURE_FUSION

#endif // __NATIVE_BINDER_RESOURCE_H_
