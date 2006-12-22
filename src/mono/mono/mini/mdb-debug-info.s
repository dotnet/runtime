.section	.mdb_debug_info, "aw", @progbits
.global		MONO_DEBUGGER__debugger_info
.global		MONO_DEBUGGER__debugger_info_ptr
MONO_DEBUGGER__debugger_info_ptr:
		.long	MONO_DEBUGGER__debugger_info
