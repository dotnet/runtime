project(System.Net.Security.Native C)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_compile_options(-Wno-incompatible-pointer-types-discards-qualifiers)

add_definitions(-DPIC=1)

if (HAVE_GSSFW_HEADERS)
   find_library(LIBGSS NAMES GSS)
   if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
      message(FATAL_ERROR "Cannot find GSS.Framework and System.Net.Security.Native cannot build without it. Try installing GSS.Framework (or the appropriate package for your platform)")
   endif()
elseif(HAVE_HEIMDAL_HEADERS)
   find_library(LIBGSS NAMES gssapi)
   if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
      message(FATAL_ERROR "Cannot find libgssapi and System.Net.Security.Native cannot build without it. Try installing heimdal (or the appropriate package for your platform)")
   endif()
elseif(HeimdalGssApi)
     message(FATAL_ERROR "HeimdalGssApi option was set but gssapi headers could not be found and System.Net.Security.Native cannot build without the headers. Try installing heimdal (or the appropriate package for your platform)")
else()
   find_library(LIBGSS NAMES gssapi_krb5)
   if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
      message(FATAL_ERROR "Cannot find libgssapi_krb5 and System.Net.Security.Native cannot build without it. Try installing libkrb5-dev (or the appropriate package for your platform)")
   endif()
endif()

set(NATIVEGSS_SOURCES
    pal_gssapi.c
)

add_library(System.Net.Security.Native
    SHARED
    ${NATIVEGSS_SOURCES}
    ${VERSION_FILE_PATH}
)

add_library(System.Net.Security.Native-Static
    STATIC
    ${NATIVEGSS_SOURCES}
    ${VERSION_FILE_PATH}
)

# Disable the "lib" prefix and override default name
set_target_properties(System.Net.Security.Native-Static PROPERTIES PREFIX "")
set_target_properties(System.Net.Security.Native-Static PROPERTIES OUTPUT_NAME System.Net.Security.Native CLEAN_DIRECT_OUTPUT 1)

target_link_libraries(System.Net.Security.Native
    ${LIBGSS}
)

install_library_and_symbols (System.Net.Security.Native)
install (TARGETS System.Net.Security.Native-Static DESTINATION .)
