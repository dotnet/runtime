set(SOURCES
  main.cpp
)

add_executable(mdmerge
  ${SOURCES}
)

target_link_libraries(mdmerge
  dnmd::dnmd
  minipal_com)

if(NOT MSVC)
  target_link_libraries(mdmerge minipal_comhdrs)
endif()

if (DNMD_INSTALL)
  install(TARGETS mdmerge)
endif()