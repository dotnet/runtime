project(compatjit)

# This compatjit.dll is only built if we are not building JIT32 as compatjit.dll.
# It is the same build as legacyjit.dll, just with a different name, and not
# built as an altjit.

add_definitions(-DLEGACY_BACKEND)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
add_definitions(-DFEATURE_READYTORUN_COMPILER)
remove_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

# No SIMD in legacy back-end.
remove_definitions(-DFEATURE_SIMD)
remove_definitions(-DFEATURE_AVX_SUPPORT)

if(WIN32)
  add_definitions(-DFX_VER_INTERNALNAME_STR=compatjit.dll)
endif(WIN32)

add_library_clr(compatjit
   SHARED
   ${SHARED_LIB_SOURCES}
)

add_dependencies(compatjit jit_exports)

set_property(TARGET compatjit APPEND_STRING PROPERTY LINK_FLAGS ${JIT_EXPORTS_LINKER_OPTION})
set_property(TARGET compatjit APPEND_STRING PROPERTY LINK_DEPENDS ${JIT_EXPORTS_FILE})

set(RYUJIT_LINK_LIBRARIES
   utilcodestaticnohost
   gcinfo
)

if(CLR_CMAKE_PLATFORM_UNIX)
    list(APPEND RYUJIT_LINK_LIBRARIES
       mscorrc_debug
       coreclrpal
       palrt
    )
else()
    list(APPEND RYUJIT_LINK_LIBRARIES
       ${STATIC_MT_CRT_LIB}
       ${STATIC_MT_VCRT_LIB}
       kernel32.lib
       advapi32.lib
       ole32.lib
       oleaut32.lib
       uuid.lib
       user32.lib
       version.lib
       shlwapi.lib
       bcrypt.lib
       crypt32.lib
       RuntimeObject.lib
    )
endif(CLR_CMAKE_PLATFORM_UNIX)

target_link_libraries(compatjit
   ${RYUJIT_LINK_LIBRARIES}
)

# add the install targets
install_clr(compatjit)
