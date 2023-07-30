set(SOURCES
  dispenser.cpp
  metadataimport.cpp
  hcorenum.cpp
  pal.cpp
)

set(HEADERS
  ../inc/dnmd_interfaces.hpp
  ./metadataimportro.hpp
  ./hcorenum.hpp
  ./controllingiunknown.hpp
  ./tearoffbase.hpp
  ./pal.hpp
)

if(NOT MSVC)
  # Adds global GUID constants.
  list(APPEND SOURCES ./iids.cpp ./options.cpp)
endif()

add_library(dnmd_interfaces_static
  STATIC
  ${SOURCES}
  ${HEADERS}
)

set_target_properties(dnmd_interfaces_static PROPERTIES EXPORT_NAME interfaces_static)

add_library(dnmd::interfaces_static ALIAS dnmd_interfaces_static)

add_library(dnmd_interfaces
  SHARED
  ${SOURCES}
  ${HEADERS}
)

set_target_properties(dnmd_interfaces PROPERTIES EXPORT_NAME interfaces)
add_library(dnmd::interfaces ALIAS dnmd_interfaces)

target_include_directories(dnmd_interfaces_static PUBLIC $<INSTALL_INTERFACE:include>)
target_include_directories(dnmd_interfaces PUBLIC $<INSTALL_INTERFACE:include>)

target_compile_definitions(dnmd_interfaces PRIVATE DNMD_BUILD_SHARED)

target_link_libraries(dnmd_interfaces_static
  PUBLIC
  dnmd::dnmd
  dncp::dncp)

target_link_libraries(dnmd_interfaces
  PRIVATE
  dncp::dncp
  dnmd::dnmd)

if(NOT MSVC)
  target_link_libraries(dnmd_interfaces_static PUBLIC dncp::winhdrs)
  target_link_libraries(dnmd_interfaces PRIVATE dncp::winhdrs)
endif()

if (NOT WIN32)
  # Use ICU when running on macOS or Linux
  if (APPLE)
    set(ICU_FIND_COMPONENTS core)
    set(ICU_FIND_REQUIRED_core TRUE)
    set(ICU_TARGET_NAME ICU::core)
  elseif(UNIX)
    set(ICU_FIND_COMPONENTS uc)
    set(ICU_FIND_REQUIRED_uc TRUE)
    set(ICU_TARGET_NAME ICU::uc)
  endif()
  include(FindICU)
  target_link_libraries(dnmd_interfaces_static PUBLIC ${ICU_TARGET_NAME})
  target_link_libraries(dnmd_interfaces PRIVATE ${ICU_TARGET_NAME})
endif()

set_target_properties(dnmd_interfaces PROPERTIES
  PUBLIC_HEADER ../inc/dnmd_interfaces.hpp
  INTERPROCEDURAL_OPTIMIZATION $<$<NOT:$<CONFIG:DEBUG>>:TRUE>)
  
install(TARGETS dnmd_interfaces_static EXPORT interfaces_static
  ARCHIVE DESTINATION lib
  LIBRARY DESTINATION lib)

install(EXPORT interfaces_static NAMESPACE dnmd:: FILE dnmdinterfaces_static.cmake DESTINATION lib/cmake/dnmd)

install(TARGETS dnmd_interfaces EXPORT interfaces
  PUBLIC_HEADER DESTINATION include
  ARCHIVE DESTINATION lib
  LIBRARY DESTINATION lib
  RUNTIME DESTINATION bin)

install(EXPORT interfaces NAMESPACE dnmd:: FILE dnmdinterfaces.cmake DESTINATION lib/cmake/dnmd)
  
