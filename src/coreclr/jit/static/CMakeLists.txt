project(ClrJit)

set_source_files_properties(${JIT_EXPORTS_FILE} PROPERTIES GENERATED TRUE)

add_library_clr(clrjit_obj
    OBJECT
    ${JIT_SOURCES}
    ${JIT_ARCH_SOURCES}
)

if(CLR_CMAKE_HOST_UNIX)
    add_dependencies(clrjit_obj coreclrpal gcinfo)
endif(CLR_CMAKE_HOST_UNIX)

target_include_directories(clrjit_obj PRIVATE ${JIT_SOURCE_DIR})
target_precompile_headers(clrjit_obj PRIVATE [["jitpch.h"]])

add_library(clrjit_static INTERFACE)
target_sources(clrjit_static INTERFACE $<TARGET_OBJECTS:clrjit_obj>)
