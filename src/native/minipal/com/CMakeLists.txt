add_library(minipal_comhdrs INTERFACE)
target_include_directories(minipal_comhdrs INTERFACE ${CMAKE_CURRENT_SOURCE_DIR}/winhdrs)
target_compile_definitions(minipal_comhdrs INTERFACE MINIPAL_COM_TYPEDEFS MINIPAL_COM_WINHDRS)

if (CLR_CMAKE_HOST_WIN32 OR HOST_WIN32)
    add_library(minipal_com STATIC windows.c)
else()
    add_library(minipal_com STATIC
        guid.c
        memory.c
        strings.c
        interfaces.c)

    target_link_libraries(minipal_com PUBLIC minipal_comhdrs minipal)
endif()

target_include_directories(minipal_com PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
