// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __MONO_DEBUGGER_AGENT_H__
#define __MONO_DEBUGGER_AGENT_H__

#include "mini.h"

MONO_API void
mono_debugger_agent_init (void);

MONO_API void
mono_debugger_agent_parse_options (char *options);

MONO_API MONO_RT_EXTERNAL_ONLY gboolean
mono_debugger_agent_transport_handshake (void);

#endif