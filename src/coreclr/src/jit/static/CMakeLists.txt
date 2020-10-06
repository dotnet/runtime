project(ClrJit)

set_source_files_properties(${JIT_EXPORTS_FILE} PROPERTIES GENERATED TRUE)

add_library_clr(clrjit_obj
    OBJECT
    ${JIT_CORE_SOURCES}
    ${JIT_ARCH_SOURCES}
)

if(CLR_CMAKE_HOST_UNIX)
    add_dependencies(clrjit_obj coreclrpal gcinfo)
endif(CLR_CMAKE_HOST_UNIX)

target_precompile_header(TARGET clrjit_obj HEADER jitpch.h ADDITIONAL_INCLUDE_DIRECTORIES ${JIT_SOURCE_DIR})

add_library(clrjit_static INTERFACE)
target_sources(clrjit_static INTERFACE $<TARGET_OBJECTS:clrjit_obj>)
