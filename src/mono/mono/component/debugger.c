// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>

#include "mono/mini/mini-runtime.h"

#include <mono/component/debugger.h>
#include "debugger-agent.h"
#include "debugger-engine.h"

static bool
debugger_avaliable (void);

static MonoComponentDebugger fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &debugger_avaliable }
};

static bool
debugger_avaliable (void)
{
	return true;
}


MonoComponentDebugger *
mono_component_debugger_init (void)
{
	debugger_agent_add_function_pointers (&fn_table);
#ifdef TARGET_WASM	
	mini_wasm_debugger_add_function_pointers (&fn_table);
#endif	
	return &fn_table;
}
