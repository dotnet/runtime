project(armelnonjit)

add_definitions(-DALT_JIT)
add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
remove_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

remove_definitions(-DFEATURE_SIMD)
remove_definitions(-DFEATURE_HW_INTRINSICS)

if(FEATURE_READYTORUN)
  add_definitions(-DFEATURE_READYTORUN_COMPILER)
endif(FEATURE_READYTORUN)

if (CLR_CMAKE_PLATFORM_ARCH_I386)
    remove_definitions(-D_TARGET_X86_)
    add_definitions(-D_TARGET_ARM_)
    add_definitions(-DARM_SOFTFP)
    set(JIT_ARCH_ALTJIT_SOURCES ${JIT_ARM_SOURCES})
    set(JIT_ARCH_LINK_LIBRARIES gcinfo_arm)
elseif(CLR_CMAKE_PLATFORM_ARCH_AMD64)
    remove_definitions(-D_TARGET_AMD64_)
    add_definitions(-D_TARGET_ARM64_)
    set(JIT_ARCH_ALTJIT_SOURCES ${JIT_ARM64_SOURCES})
    set(JIT_ARCH_LINK_LIBRARIES gcinfo_arm64)
else()
    clr_unknown_arch()
endif()

if (NOT WIN32)
    if (CLR_CMAKE_PLATFORM_ARCH_I386)
        remove_definitions(-DUNIX_X86_ABI)
    elseif(CLR_CMAKE_PLATFORM_ARCH_AMD64)
        remove_definitions(-DUNIX_AMD64_ABI)
    else()
        clr_unknown_arch()
    endif()
endif(NOT WIN32)

if(WIN32)
  add_definitions(-DFX_VER_INTERNALNAME_STR=armelnonjit.dll)
endif(WIN32)

set_source_files_properties(${JIT_EXPORTS_FILE} PROPERTIES GENERATED TRUE)

add_library_clr(armelnonjit
   SHARED
   ${SHARED_LIB_SOURCES}
   ${JIT_ARCH_ALTJIT_SOURCES}
)

add_dependencies(armelnonjit jit_exports)

set_property(TARGET armelnonjit APPEND_STRING PROPERTY LINK_FLAGS ${JIT_EXPORTS_LINKER_OPTION})
set_property(TARGET armelnonjit APPEND_STRING PROPERTY LINK_DEPENDS ${JIT_EXPORTS_FILE})

set(RYUJIT_LINK_LIBRARIES
   utilcodestaticnohost
   ${JIT_ARCH_LINK_LIBRARIES}
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

target_link_libraries(armelnonjit
   ${RYUJIT_LINK_LIBRARIES}
)

# add the install targets
install_clr(armelnonjit)
