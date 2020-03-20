#!/usr/bin/env bash
cd "$(dirname ${BASH_SOURCE[0]})"
../../../../../../../dotnet.sh run -- ThunkInput.txt ../CorInfoBase.cs ../../../crossgen2/jitinterface/jitinterface.h
../../../../../../../dotnet.sh run -- InstructionSetGenerator InstructionSetDesc.txt ../../Internal/Runtime/ReadyToRunInstructionSet.cs ../CorInfoInstructionSet.cs ../../../../inc/corinfoinstructionset.h ../../../../inc/readytoruninstructionset.h