include(${CLR_DIR}/crossgen.cmake)

if(CLR_CMAKE_TARGET_ARCH_I386 OR CLR_CMAKE_TARGET_ARCH_ARM)
  add_definitions(-DLEGACY_BACKEND)
endif()

add_library_clr(${JIT_BASE_NAME}_crossgen ${SOURCES})
