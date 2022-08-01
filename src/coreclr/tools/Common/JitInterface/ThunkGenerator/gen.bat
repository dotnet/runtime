pushd %~dp0
call ..\..\..\..\..\..\dotnet.cmd run -- ThunkInput.txt ..\CorInfoBase.cs ..\..\..\aot\jitinterface\jitinterface.h ..\..\..\..\jit\ICorJitInfo_API_names.h ..\..\..\..\jit\ICorJitInfo_API_wrapper.hpp ..\..\..\..\inc\icorjitinfoimpl_generated.h ..\..\..\..\tools\superpmi\superpmi-shim-counter\icorjitinfo.cpp ..\..\..\..\tools\superpmi\superpmi-shim-simple\icorjitinfo.cpp
call ..\..\..\..\..\..\dotnet.cmd run -- InstructionSetGenerator InstructionSetDesc.txt ..\..\Internal\Runtime\ReadyToRunInstructionSet.cs ..\..\Internal\Runtime\ReadyToRunInstructionSetHelper.cs ..\CorInfoInstructionSet.cs ..\..\..\..\inc\corinfoinstructionset.h ..\..\..\..\inc\readytoruninstructionset.h
popd
