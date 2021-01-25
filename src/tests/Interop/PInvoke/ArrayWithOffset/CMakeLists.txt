project (ArrayWithOffsetNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    ArrayWithOffsetNative.cpp
)
if(CLR_CMAKE_HOST_WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(CLR_CMAKE_HOST_WIN32)
# add the executable
add_library (ArrayWithOffsetNative SHARED ${SOURCES})
target_link_libraries(ArrayWithOffsetNative ${LINK_LIBRARIES_ADDITIONAL})
# add the install targets
install (TARGETS ArrayWithOffsetNative DESTINATION bin)
