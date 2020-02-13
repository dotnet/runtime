project (VariantNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES VariantNative.cpp)

# add the executable
add_library (VariantNative SHARED ${SOURCES})

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(CLR_CMAKE_HOST_WIN32)

target_link_libraries(VariantNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS VariantNative DESTINATION bin)
