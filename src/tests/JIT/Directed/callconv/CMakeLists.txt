include_directories(${INC_PLATFORM_DIR})

set(SOURCES
   InstanceCallConvTest.cpp
)

add_library (ThisCallNative ${TEST_LIB_TYPE} ${SOURCES})
target_compile_definitions(ThisCallNative PRIVATE INSTANCE_CALLCONV=__thiscall)

add_library (StdCallMemberFunctionNative ${TEST_LIB_TYPE} ${SOURCES})
target_compile_definitions(StdCallMemberFunctionNative PRIVATE INSTANCE_CALLCONV=__stdcall)

add_library (CdeclMemberFunctionNative ${TEST_LIB_TYPE} ${SOURCES})
target_compile_definitions(CdeclMemberFunctionNative PRIVATE INSTANCE_CALLCONV=__cdecl)

add_library (PlatformDefaultMemberFunctionNative ${TEST_LIB_TYPE} ${SOURCES})
target_compile_definitions(PlatformDefaultMemberFunctionNative PRIVATE INSTANCE_CALLCONV=STDMETHODCALLTYPE)

install (TARGETS ThisCallNative DESTINATION bin)
