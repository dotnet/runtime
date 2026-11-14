pushd %~dp0
call ..\..\..\..\..\..\dotnet.cmd run -- ThunkInput.txt ..\CorInfoImpl_generated.cs ..\..\..\aot\jitinterface\jitinterface_generated.h ..\..\..\..\jit\ICorJitInfo_names_generated.h ..\..\..\..\jit\ICorJitInfo_wrapper_generated.hpp ..\..\..\..\inc\icorjitinfoimpl_generated.h ..\..\..\superpmi\jitinterface\icorjitinfo_forwarders_generated.h ..\..\..\superpmi\managed\shim\OriginalJitInfo_generated.cs ..\..\..\superpmi\jitinterface\icorjitinfo_shim_generated.h
call ..\..\..\..\..\..\dotnet.cmd run -- InstructionSetGenerator InstructionSetDesc.txt ..\..\Internal\Runtime\ReadyToRunInstructionSet.cs ..\..\Internal\Runtime\ReadyToRunInstructionSetHelper.cs ..\CorInfoInstructionSet.cs ..\..\..\..\inc\corinfoinstructionset.h ..\..\..\..\inc\readytoruninstructionset.h ..\CorInfoInstructionSetParser.cs
call ..\..\..\..\..\..\dotnet.cmd run -- NewJITEEVersion ..\..\..\..\inc\jiteeversionguid.h
popd
