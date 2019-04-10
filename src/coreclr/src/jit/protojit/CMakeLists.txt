project(protojit)

add_definitions(-DALT_JIT)
add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
remove_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

if(FEATURE_READYTORUN)
  add_definitions(-DFEATURE_READYTORUN_COMPILER)
endif(FEATURE_READYTORUN)

if(WIN32)
  add_definitions(-DFX_VER_INTERNALNAME_STR=protojit.dll)
endif(WIN32)

set_source_files_properties(${JIT_EXPORTS_FILE} PROPERTIES GENERATED TRUE)

add_library_clr(protojit
   SHARED
   ${SHARED_LIB_SOURCES}
   ${JIT_ARCH_SOURCES}
)

add_dependencies(protojit jit_exports)

set_property(TARGET protojit APPEND_STRING PROPERTY LINK_FLAGS ${JIT_EXPORTS_LINKER_OPTION})
set_property(TARGET protojit APPEND_STRING PROPERTY LINK_DEPENDS ${JIT_EXPORTS_FILE})

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

target_link_libraries(protojit
   ${RYUJIT_LINK_LIBRARIES}
)

# add the install targets
install_clr(protojit)
