#!/usr/bin/env bash
cd "$(dirname ${BASH_SOURCE[0]})"
../../../../../../dotnet.sh run -- ThunkInput.txt ../CorInfoBase.cs ../../../aot/jitinterface/jitinterface.h ../../../../jit/ICorJitInfo_API_names.h ../../../../jit/ICorJitInfo_API_wrapper.hpp ../../../../inc/icorjitinfoimpl_generated.h ../../../../ToolBox/superpmi/superpmi-shim-counter/icorjitinfo.cpp ../../../../ToolBox/superpmi/superpmi-shim-simple/icorjitinfo.cpp
../../../../../../dotnet.sh run -- InstructionSetGenerator InstructionSetDesc.txt ../../Internal/Runtime/ReadyToRunInstructionSet.cs ../../Internal/Runtime/ReadyToRunInstructionSetHelper.cs ../CorInfoInstructionSet.cs ../../../../inc/corinfoinstructionset.h ../../../../inc/readytoruninstructionset.h
