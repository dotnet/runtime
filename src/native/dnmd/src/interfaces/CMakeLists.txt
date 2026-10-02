set(SOURCES
  ./dispenser.cpp
  ./symbinder.cpp
  ./metadataimport.cpp
  ./metadataemit.cpp
  ./hcorenum.cpp
  ./pal.cpp
  ./signatures.cpp
  ./importhelpers.cpp
  ./internal/metadataimport.cpp
)

set(HEADERS
  ../inc/dnmd_interfaces.hpp
  ../inc/internal/span.hpp
  ./metadataimportro.hpp
  ./metadataemit.hpp
  ./hcorenum.hpp
  ./controllingiunknown.hpp
  ./tearoffbase.hpp
  ./pal.hpp
  ./dnmdowner.hpp
  ./signatures.hpp
  ./importhelpers.hpp
  ./internal/metadataimport.hpp
)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(NOT MSVC)
  # Adds global GUID constants.
  list(APPEND SOURCES ./iids.cpp ./options.cpp)
endif()

if (WIN32)
  # Disable "secure CRT" warnings when targeting Windows
  # as the "secure CRT" is not cross-platform.
  add_compile_definitions(_CRT_SECURE_NO_WARNINGS)
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

target_include_directories(dnmd_interfaces_static
  PRIVATE "${CLR_SRC_NATIVE_DIR}/../coreclr/minipal"
  PUBLIC $<INSTALL_INTERFACE:include>)
target_include_directories(dnmd_interfaces
  PRIVATE "${CLR_SRC_NATIVE_DIR}/../coreclr/minipal"
  PUBLIC $<INSTALL_INTERFACE:include>)

target_compile_definitions(dnmd_interfaces_static PRIVATE COM_NO_WINDOWS_H)
target_compile_definitions(dnmd_interfaces PRIVATE DNMD_BUILD_SHARED COM_NO_WINDOWS_H)

if (DNMD_ENABLE_LOADED_MODULES_CACHE)
  target_compile_definitions(dnmd_interfaces_static PUBLIC DNMD_ENABLE_LOADED_MODULES_CACHE)
  target_compile_definitions(dnmd_interfaces PUBLIC DNMD_ENABLE_LOADED_MODULES_CACHE)
endif()

if(CMAKE_PROJECT_NAME STREQUAL "CoreCLR" AND CLR_CMAKE_TARGET_UNIX)
  target_compile_definitions(dnmd_interfaces_static PRIVATE DNMD_USE_CORECLR_GUIDS)
endif()

target_link_libraries(dnmd_interfaces_static
  PUBLIC
  minipal
  minipal_com
  dnmd::dnmd)

target_link_libraries(dnmd_interfaces
  PRIVATE
  minipal
  minipal_com
  dnmd::dnmd)

if(NOT MSVC)
  target_link_libraries(dnmd_interfaces_static PUBLIC minipal_comhdrs)
  target_link_libraries(dnmd_interfaces PRIVATE minipal_comhdrs)
endif()

set_target_properties(dnmd_interfaces PROPERTIES
  PUBLIC_HEADER ${CMAKE_CURRENT_SOURCE_DIR}/../inc/dnmd_interfaces.hpp)

if (DNMD_INSTALL)
  install(TARGETS dnmd_interfaces EXPORT interfaces
    PUBLIC_HEADER DESTINATION include
    ARCHIVE DESTINATION lib
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION bin)

  install(EXPORT interfaces NAMESPACE dnmd:: FILE dnmdinterfaces.cmake DESTINATION lib/cmake/dnmd)
endif()
