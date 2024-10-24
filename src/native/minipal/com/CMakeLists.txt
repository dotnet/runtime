add_library(minipal_comhdrs INTERFACE)
target_include_directories(minipal_comhdrs INTERFACE ${CMAKE_CURRENT_SOURCE_DIR}/winhdrs)
target_compile_definitions(minipal_comhdrs INTERFACE MINIPAL_COM_TYPEDEFS MINIPAL_COM_WINHDRS)

add_library(minipal_com INTERFACE)

if (CLR_CMAKE_HOST_WIN32 OR HOST_WIN32)
else()
    target_link_libraries(minipal_com INTERFACE minipal_comhdrs minipal)
endif()

if (CMAKE_C_BYTE_ORDER EQUAL "BIG_ENDIAN")
    target_compile_definitions(minipal_com INTERFACE MINIPAL_COM_BIGENDIAN)
endif()

target_include_directories(minipal_com INTERFACE ${CMAKE_CURRENT_SOURCE_DIR})
