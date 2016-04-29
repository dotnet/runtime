@REM -------------------------------------------------------------------------
@REM 
@REM  This script provides x86 Ryujit test environment settings
@REM
@REM -------------------------------------------------------------------------

set COMPLUS_AltJit=*
set COMPLUS_AltJitName=protojit.dll
set COMPLUS_NoGuiOnAssert=1

@REM By default, do not set COMPLUS_AltJitAssertOnNYI=1. This allows us to compile
@REM as much as possible with RyuJIT, and not just stop at the first NYI. It means
@REM we will fall back to x86 legacy JIT for many functions.
@REM set COMPLUS_AltJitAssertOnNYI=1
