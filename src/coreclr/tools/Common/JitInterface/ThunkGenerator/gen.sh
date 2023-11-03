#!/usr/bin/env bash
cd "$(dirname ${BASH_SOURCE[0]})"
../../../../../../dotnet.sh run -- ThunkInput.txt ../CorInfoImpl_generated.cs ../../../aot/jitinterface/jitinterface_generated.h ../../../../jit/ICorJitInfo_names_generated.h ../../../../jit/ICorJitInfo_wrapper_generated.hpp ../../../../inc/icorjitinfoimpl_generated.h ../../../../tools/superpmi/superpmi-shim-counter/icorjitinfo_generated.cpp ../../../../tools/superpmi/superpmi-shim-simple/icorjitinfo_generated.cpp
../../../../../../dotnet.sh run -- InstructionSetGenerator InstructionSetDesc.txt ../../Internal/Runtime/ReadyToRunInstructionSet.cs ../../Internal/Runtime/ReadyToRunInstructionSetHelper.cs ../CorInfoInstructionSet.cs ../../../../inc/corinfoinstructionset.h ../../../../inc/readytoruninstructionset.h
../../../../../../dotnet.sh run -- NewJITEEVersion ../../../../inc/jiteeversionguid.h
