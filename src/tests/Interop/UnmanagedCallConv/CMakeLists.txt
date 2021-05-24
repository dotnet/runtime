project(UnmanagedCallConvNative)
Include("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES UnmanagedCallConvNative.cpp )

# add the executable
add_library(${PROJECT_NAME} SHARED ${SOURCES})
target_link_libraries(${PROJECT_NAME} ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS ${PROJECT_NAME} DESTINATION bin)
