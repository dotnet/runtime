set(SOURCES
  ./perf.cpp
  ./regnative.cpp
  ./unit_cor.cpp
  ./unit_corsym.cpp
)

add_library(regnative
  SHARED
  ${SOURCES}
)

target_link_libraries(regnative
  dnmd::interfaces
  dncp::dncp)

if(NOT MSVC)
  target_link_libraries(regnative dncp::winhdrs)
endif()

install(TARGETS regnative
  ARCHIVE DESTINATION lib
  LIBRARY DESTINATION lib
  RUNTIME DESTINATION bin)