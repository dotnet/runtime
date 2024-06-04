project(ClrJit)

set_source_files_properties(${JIT_EXPORTS_FILE} PROPERTIES GENERATED TRUE)

add_library_clr(clrjit_static
    OBJECT
    ${JIT_SOURCES}
    ${JIT_ARCH_SOURCES}
)

if(CLR_CMAKE_HOST_UNIX)
    add_dependencies(clrjit_static coreclrpal gcinfo)
endif(CLR_CMAKE_HOST_UNIX)

target_include_directories(clrjit_static PRIVATE ${JIT_SOURCE_DIR})
target_precompile_headers(clrjit_static PRIVATE [["jitpch.h"]])
