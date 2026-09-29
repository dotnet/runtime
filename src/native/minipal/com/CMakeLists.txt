add_library(minipal_comhdrs INTERFACE)
target_include_directories(minipal_comhdrs INTERFACE ${CMAKE_CURRENT_SOURCE_DIR}/winhdrs)
target_compile_definitions(minipal_comhdrs INTERFACE MINIPAL_COM_TYPEDEFS MINIPAL_COM_WINHDRS)

add_library(minipal_com INTERFACE)

if (NOT CLR_CMAKE_HOST_WIN32 AND NOT HOST_WIN32)
    target_link_libraries(minipal_com INTERFACE minipal_comhdrs minipal)
endif()

if (CMAKE_C_BYTE_ORDER STREQUAL "BIG_ENDIAN")
    target_compile_definitions(minipal_com INTERFACE MINIPAL_COM_BIGENDIAN)
endif()

target_include_directories(minipal_com INTERFACE ${CMAKE_CURRENT_SOURCE_DIR})
