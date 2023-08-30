set(SOURCES
  main.cpp
)

add_executable(mddump
  ${SOURCES}
)

target_link_libraries(mddump
  dnmd::pdb
  dncp::dncp)

if(NOT MSVC)
  target_link_libraries(mddump dncp::winhdrs)
endif()

install(TARGETS mddump)