project(ClrJit)

# Disable the following for UNIX altjit on Windows
if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-Wno-trigraphs)
    add_compile_options(-fPIC)
    add_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

    add_library(ClrJit
      STATIC
      ${SHARED_LIB_SOURCES}
    )
    add_dependencies(ClrJit coreclrpal gcinfo)
else()
    add_library(ClrJit
       ${SOURCES}
    )
# Disable up to here (see above) the following for UNIX altjit on Windows 
# Enable the following for UNIX altjit on Windows
#    add_library(ClrJit
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
