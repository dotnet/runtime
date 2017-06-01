include(${CLR_DIR}/crossgen.cmake)

if(CLR_CMAKE_TARGET_ARCH_ARM)
  add_definitions(-DLEGACY_BACKEND)
endif()

add_library_clr(clrjit_crossgen ${SOURCES} ${JIT_ARCH_SOURCES})
if(FEATURE_MERGE_JIT_AND_ENGINE)
  target_link_libraries(clrjit_crossgen ${RYUJIT_LINK_LIBRARIES})
endif(FEATURE_MERGE_JIT_AND_ENGINE)
