.text
.globl		_MONO_DEBUGGER__debugger_info
.globl		_MONO_DEBUGGER__notification_function	
_MONO_DEBUGGER__notification_function:
		int3
		ret
.section	.mdb_debug_info, "aw"
.globl		_MONO_DEBUGGER__debugger_info_ptr
.globl		_MONO_DEBUGGER__using_debugger
_MONO_DEBUGGER__debugger_info_ptr:
		.long	_MONO_DEBUGGER__debugger_info
_MONO_DEBUGGER__using_debugger:
		.long	0
		.long	0
