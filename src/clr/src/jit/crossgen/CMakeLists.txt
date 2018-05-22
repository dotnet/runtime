include(${CLR_DIR}/crossgen.cmake)

add_library_clr(clrjit_crossgen ${SOURCES} ${JIT_ARCH_SOURCES})
if(FEATURE_MERGE_JIT_AND_ENGINE)
  target_link_libraries(clrjit_crossgen ${RYUJIT_LINK_LIBRARIES})
endif(FEATURE_MERGE_JIT_AND_ENGINE)
