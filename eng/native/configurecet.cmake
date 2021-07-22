# Enable EH-continuation table and CET-compatibility for native components for amd64 builds.
# Added some switches using variables instead of add_compile_options to let individual projects override it.
if (MSVC AND CLR_CMAKE_HOST_ARCH_AMD64)
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /guard:ehcont")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} /guard:ehcont")
  set(CMAKE_ASM_MASM_FLAGS "${CMAKE_ASM_MASM_FLAGS} /guard:ehcont")
  add_linker_flag(/guard:ehcont)
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /CETCOMPAT")
endif (MSVC AND CLR_CMAKE_HOST_ARCH_AMD64)
