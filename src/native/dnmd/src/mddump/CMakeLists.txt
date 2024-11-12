set(SOURCES
  main.cpp
)

add_executable(mddump
  ${SOURCES}
)

target_link_libraries(mddump
  dnmd::pdb
  minipal_com)

if(NOT MSVC)
  target_link_libraries(mddump minipal_comhdrs)
endif()

if (DNMD_INSTALL)
  install(TARGETS mddump)
endif()