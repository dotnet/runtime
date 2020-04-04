pushd %~dp0
call ..\..\..\..\..\..\..\dotnet.cmd run -- ThunkInput.txt ..\CorInfoBase.cs ..\..\..\crossgen2\jitinterface\jitinterface.h
call ..\..\..\..\..\..\..\dotnet.cmd run -- InstructionSetGenerator InstructionSetDesc.txt ..\..\Internal\Runtime\ReadyToRunInstructionSet.cs ..\..\Internal\Runtime\ReadyToRunInstructionSetHelper.cs ..\CorInfoInstructionSet.cs ..\..\..\..\inc\corinfoinstructionset.h ..\..\..\..\inc\readytoruninstructionset.h
popd