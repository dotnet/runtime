set(SOURCES
  main.cpp
)

if(NOT MSVC)
  # Define for DNCP
  add_compile_definitions(
      DNCP_TYPEDEFS
      DNCP_WINHDRS
  )
endif()

add_executable(mddump
  ${SOURCES}
)

target_link_libraries(mddump
  dnmd
  dncp)

if(NOT MSVC)
  target_link_libraries(mddump dncp_winhdrs)
endif()

install(TARGETS mddump)