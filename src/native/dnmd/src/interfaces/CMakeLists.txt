set(SOURCES
  ./dispenser.cpp
  ./symbinder.cpp
  ./metadataimport.cpp
  ./metadataemit.cpp
  ./hcorenum.cpp
  ./pal.cpp
  ./signatures.cpp
  ./importhelpers.cpp
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
)

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

target_include_directories(dnmd_interfaces_static PUBLIC $<INSTALL_INTERFACE:include>)
target_include_directories(dnmd_interfaces PUBLIC $<INSTALL_INTERFACE:include>)

target_compile_definitions(dnmd_interfaces_static PRIVATE COM_NO_WINDOWS_H)
target_compile_definitions(dnmd_interfaces PRIVATE DNMD_BUILD_SHARED COM_NO_WINDOWS_H)

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

if (WIN32)
  target_link_libraries(dnmd_interfaces_static PUBLIC bcrypt)
  target_link_libraries(dnmd_interfaces PRIVATE bcrypt)
elseif(APPLE)
  find_library(SECURITY_LIBRARY Security REQUIRED)
  target_link_libraries(dnmd_interfaces_static PUBLIC ${SECURITY_LIBRARY})
  target_link_libraries(dnmd_interfaces PRIVATE ${SECURITY_LIBRARY})

  include(CheckIncludeFile)
  check_include_file("CommonCrypto/CommonDigest.h" HAVE_COMMON_DIGEST_H)
  if (NOT HAVE_COMMON_DIGEST_H)
    message(FATAL_ERROR "CommonCrypto/CommonDigest.h not found")
  endif()
elseif(UNIX)
  find_package(OpenSSL REQUIRED COMPONENTS Crypto)
  if (FEATURE_DISTRO_AGNOSTIC_SSL)
    # When we're building against a distro-agnostic SSL, we need to dynamically load OpenSSL
    add_compile_definitions(FEATURE_DISTRO_AGNOSTIC_SSL)
    target_link_libraries(dnmd_interfaces_static PUBLIC ${CMAKE_DL_LIBS})
    target_link_libraries(dnmd_interfaces PRIVATE ${CMAKE_DL_LIBS})
  else()
    target_link_libraries(dnmd_interfaces_static PUBLIC OpenSSL::Crypto)
    target_link_libraries(dnmd_interfaces PRIVATE OpenSSL::Crypto)
  endif()
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
