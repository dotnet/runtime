set(SOURCES
  main.cpp
)

add_executable(mdmerge
  ${SOURCES}
)

target_link_libraries(mdmerge
  dnmd::dnmd
  dncp::dncp)

if(NOT MSVC)
  target_link_libraries(mdmerge dncp::winhdrs)
endif()

install(TARGETS mdmerge)