project(ClrJit)

if(CLR_CMAKE_PLATFORM_ARCH_I386 OR CLR_CMAKE_PLATFORM_ARCH_ARM)
    add_definitions(-DLEGACY_BACKEND)
endif(CLR_CMAKE_PLATFORM_ARCH_I386 OR CLR_CMAKE_PLATFORM_ARCH_ARM)

# Disable the following for UNIX altjit on Windows
if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)

    add_library_clr(${JIT_BASE_NAME}_static
      STATIC
      ${SHARED_LIB_SOURCES}
    )
    add_dependencies(${JIT_BASE_NAME}_static coreclrpal gcinfo)
else()
    add_library_clr(${JIT_BASE_NAME}_static
       ${SOURCES}
    )
# Disable up to here (see above) the following for UNIX altjit on Windows 
# Enable the following for UNIX altjit on Windows
#    add_library_clr(ClrJit
#       SHARED
#       ${SHARED_LIB_SOURCES}
#    )

# Enable the following for UNIX altjit on Windows
#target_link_libraries(ClrJit
#      utilcode
#      gcinfo
#      runtime_library
#    )

# Disable the following for UNIX altjit on Windows
endif(CLR_CMAKE_PLATFORM_UNIX)
