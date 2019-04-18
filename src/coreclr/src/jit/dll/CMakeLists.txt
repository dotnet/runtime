project(ClrJit)

set_source_files_properties(${JIT_EXPORTS_FILE} PROPERTIES GENERATED TRUE)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)

    add_library_clr(clrjit_static
      STATIC
      ${SHARED_LIB_SOURCES}
      ${JIT_ARCH_SOURCES}
    )
    add_dependencies(clrjit_static coreclrpal gcinfo)
else()
    add_library_clr(clrjit_static
      ${SHARED_LIB_SOURCES}
      ${JIT_ARCH_SOURCES}
    )
endif(CLR_CMAKE_PLATFORM_UNIX)
