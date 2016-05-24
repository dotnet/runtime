project(ryujit)
add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
add_definitions(-DFEATURE_READYTORUN_COMPILER)
remove_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

if(CLR_CMAKE_TARGET_ARCH_I386 OR CLR_CMAKE_TARGET_ARCH_ARM)
  add_definitions(-DLEGACY_BACKEND)
endif()

add_library_clr(${JIT_BASE_NAME}
   SHARED
   ${SHARED_LIB_SOURCES}
)

add_dependencies(${JIT_BASE_NAME} jit_exports)

set_property(TARGET ${JIT_BASE_NAME} APPEND_STRING PROPERTY LINK_FLAGS ${JIT_EXPORTS_LINKER_OPTION})
set_property(TARGET ${JIT_BASE_NAME} APPEND_STRING PROPERTY LINK_DEPENDS ${JIT_EXPORTS_FILE})

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

target_link_libraries(${JIT_BASE_NAME}
   ${RYUJIT_LINK_LIBRARIES}
)

# add the install targets
install_clr(${JIT_BASE_NAME})
