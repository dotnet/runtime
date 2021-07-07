// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <glib.h>
#include <mono/metadata/components.h>
#include "debugger-agent-external.h"

#ifndef DISABLE_SDB

static char *sdb_options = NULL;

#define MAX_TRANSPORTS 16
static DebuggerTransport transports [MAX_TRANSPORTS];
static int ntransports = 0;

static void
register_transport (DebuggerTransport *trans)
{
	g_assert (ntransports < MAX_TRANSPORTS);

	memcpy (&transports [ntransports], trans, sizeof (DebuggerTransport));
	ntransports ++;
}

void
mono_debugger_agent_register_transport (DebuggerTransport *trans)
{
	register_transport (trans);
}

gboolean
mono_debugger_agent_transport_handshake (void)
{
	return mono_component_debugger ()->transport_handshake ();
}

void
mono_debugger_agent_init (void)
{
	//not need to do anything anymore
}

void
mono_debugger_agent_parse_options (char *options)
{
	sdb_options = options;
}

DebuggerTransport *
mono_debugger_agent_get_transports (int *ntrans) 
{
	*ntrans = ntransports;
	return transports;
}

char *
mono_debugger_agent_get_sdb_options (void)
{
	return sdb_options;
}
#endif /* DISABLE_SDB */
