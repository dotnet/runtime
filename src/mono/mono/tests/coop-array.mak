# This file is not used in automated tests, but is useful for unautomated testing.
# The AOT testing here does not actually work.
#
# Confirmed reachable with reflection:
# ves_icall_System_Array_ClearInternal
# ves_icall_System_Array_CreateInstanceImpl
# ves_icall_System_Array_GetGenericValueImpl
# ves_icall_System_Array_GetLowerBound
# ves_icall_System_Array_GetRank
# ves_icall_System_Array_GetValue
# ves_icall_System_Array_GetValueImpl
# ves_icall_System_Array_SetGenericValueImpl
#
# Confirmed reachable without reflection:
# ves_icall_System_Array_ClearInternal
# ves_icall_System_Array_GetLowerBound
# ves_icall_System_Array_GetValue
# ves_icall_System_Array_GetValueImpl
# ves_icall_System_Array_CreateInstanceImpl
#
# The implication is that, the functions reachable without reflection
# are more performance sensitive than functions reachably "only" with reflection,
# unless in fact they are also reachable without reflection.
#
# However that does not really mean these functions are performance sensitive,
# as they are not necessarily used much.

all: array-coop-bigvt.exe.dylib array-coop-smallvt.exe.dylib array-coop-int.exe.dylib

run: all
	MONO_LOG_LEVEL=debug MONO_LOG_MASK=icallarray $(MONO) array-coop-bigvt.exe
	MONO_LOG_LEVEL=debug MONO_LOG_MASK=icallarray $(MONO) array-coop-smallvt.exe
	MONO_LOG_LEVEL=debug MONO_LOG_MASK=icallarray $(MONO) array-coop-int.exe

clean:
	rm -f array-coop-bigvt.exe.dylib array-coop-bigvt.exe array-coop-int.exe.dylib array-coop-int.exe array-coop-smallvt.exe.dylib array-coop-smallvt.exe 
	rm -f array-coop-bigvt.exe array-coop-smallvt.exe array-coop-int.exe
	rm -f array-coop-bigvt.cs array-coop-smallvt.cs array-coop-int.cs

array-coop-bigvt.cs array-coop-smallvt.cs array-coop-int.cs:
	MONO_LOG_LEVEL= MONO_LOG_MASK= make $@

array-coop-bigvt.exe.dylib: array-coop-bigvt.exe
	MONO_LOG_LEVEL= MONO_LOG_MASK= $(MONO) --aot=full array-coop-bigvt.exe

array-coop-smallvt.exe.dylib: array-coop-smallvt.exe
	MONO_LOG_LEVEL= MONO_LOG_MASK= $(MONO) --aot=full array-coop-smallvt.exe

array-coop-int.exe.dylib: array-coop-int.exe
	MONO_LOG_LEVEL= MONO_LOG_MASK= $(MONO) --aot=full array-coop-int.exe

array-coop-bigvt.exe: array-coop-bigvt.cs
	MONO_LOG_LEVEL= MONO_LOG_MASK= MONO_PATH= csc array-coop-bigvt.cs

array-coop-smallvt.exe: array-coop-smallvt.cs
	MONO_LOG_LEVEL= MONO_LOG_MASK= MONO_PATH= csc array-coop-smallvt.cs

array-coop-int.exe: array-coop-int.cs
	MONO_LOG_LEVEL= MONO_LOG_MASK= MONO_PATH= csc array-coop-int.cs
