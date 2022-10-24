set(SOURCES
  main.cpp
)

add_executable(mddump
  ${SOURCES}
)

target_link_libraries(mddump dnmd)

install(TARGETS mddump)