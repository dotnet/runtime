// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <glib.h>
#include <mono/metadata/components.h>

#ifndef DISABLE_SDB

MONO_API void
mono_debugger_agent_register_transport (DebuggerTransport *trans);

void
mono_debugger_agent_register_transport (DebuggerTransport *trans)
{
	mono_component_debugger ()->register_transport (trans);
}

gboolean
mono_debugger_agent_transport_handshake (void)
{
	return mono_component_debugger ()->mono_debugger_agent_transport_handshake ();
}

void
mono_debugger_agent_init (void)
{
	//not need to do anything anymore
}

void
mono_debugger_agent_parse_options (char *options)
{
	mono_component_debugger ()->mono_debugger_agent_parse_options (options);
}

#endif /* DISABLE_SDB */
