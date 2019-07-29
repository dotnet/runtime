add_definitions(-DNO_COR)

set(STATICMD_SOURCES
  apis.cpp
)

convert_to_absolute_path(STATICMD_SOURCES ${STATICMD_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)


add_definitions(-DFEATURE_METADATA_EMIT_ALL)
add_definitions(-DFEATURE_METADATA_EMIT)
add_definitions(-DFEATURE_METADATA_INTERNAL_APIS)

add_library_clr(mdstaticapi ${STATICMD_SOURCES})