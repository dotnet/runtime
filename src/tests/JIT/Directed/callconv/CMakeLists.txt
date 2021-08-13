include_directories(${INC_PLATFORM_DIR})

set(SOURCES
   InstanceCallConvTest.cpp
)

add_library (ThisCallNative SHARED ${SOURCES})
target_compile_definitions(ThisCallNative PRIVATE INSTANCE_CALLCONV=__thiscall)

add_library (StdCallMemberFunctionNative SHARED ${SOURCES})
target_compile_definitions(StdCallMemberFunctionNative PRIVATE INSTANCE_CALLCONV=__stdcall)

add_library (CdeclMemberFunctionNative SHARED ${SOURCES})
target_compile_definitions(CdeclMemberFunctionNative PRIVATE INSTANCE_CALLCONV=__cdecl)

add_library (PlatformDefaultMemberFunctionNative SHARED ${SOURCES})
target_compile_definitions(PlatformDefaultMemberFunctionNative PRIVATE INSTANCE_CALLCONV=STDMETHODCALLTYPE)

install (TARGETS ThisCallNative DESTINATION bin)
