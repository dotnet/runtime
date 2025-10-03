
project (ReversePInvokeNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    ExpStructAsParamNative.cpp
)
# add the executable
add_library (ReversePInvokeNative ${TEST_LIB_TYPE} ${SOURCES})
target_link_libraries(ReversePInvokeNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
# add the install targets
install (TARGETS ReversePInvokeNative DESTINATION bin)
