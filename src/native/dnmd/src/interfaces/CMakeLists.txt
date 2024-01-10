set(SOURCES
  ./dispenser.cpp
  ./symbinder.cpp
  ./metadataimport.cpp
  ./hcorenum.cpp
  ./pal.cpp
  ./signatures.cpp
  ./importhelpers.cpp
)

set(HEADERS
  ../inc/dnmd_interfaces.hpp
  ../inc/internal/span.hpp
  ./metadataimportro.hpp
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
  dncp::dncp
  dnmd::dnmd)

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

if (WIN32)
  target_link_libraries(dnmd_interfaces_static PUBLIC bcrypt)
  target_link_libraries(dnmd_interfaces PRIVATE bcrypt)
elseif(UNIX)
  include(FindOpenSSL)
  target_link_libraries(dnmd_interfaces_static PUBLIC OpenSSL::Crypto)
  target_link_libraries(dnmd_interfaces PRIVATE OpenSSL::Crypto)
elseif(APPLE)
  find_library(SECURITY_LIBRARY Security REQUIRED)
  target_link_libraries(dnmd_interfaces_static PUBLIC ${SECURITY_LIBRARY})
  target_link_libraries(dnmd_interfaces PRIVATE ${SECURITY_LIBRARY})

  include(CheckIncludeFile)
  check_include_file("CommonCrypto/CommonDigest.h" HAVE_COMMON_DIGEST_H)
  if (NOT HAVE_COMMON_DIGEST_H)
    message(FATAL_ERROR "CommonCrypto/CommonDigest.h not found")
  endif()
endif()

set_target_properties(dnmd_interfaces PROPERTIES
  PUBLIC_HEADER ../inc/dnmd_interfaces.hpp
  INTERPROCEDURAL_OPTIMIZATION $<$<NOT:$<CONFIG:DEBUG>>:TRUE>)
  
install(TARGETS dnmd_interfaces dnmd_interfaces_static EXPORT interfaces
  PUBLIC_HEADER DESTINATION include
  ARCHIVE DESTINATION lib
  LIBRARY DESTINATION lib
  RUNTIME DESTINATION bin)

install(EXPORT interfaces NAMESPACE dnmd:: FILE dnmdinterfaces.cmake DESTINATION lib/cmake/dnmd)
