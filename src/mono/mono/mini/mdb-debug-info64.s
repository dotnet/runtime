.text
.global		MONO_DEBUGGER__debugger_info
.section	.mdb_debug_info, "aw", @progbits
.global		MONO_DEBUGGER__debugger_info_ptr
MONO_DEBUGGER__debugger_info_ptr:
		.quad	MONO_DEBUGGER__debugger_info
