project(COREPAL)

include(../clrfeatures.cmake)

include_directories(${COREPAL_SOURCE_DIR}/inc)
include_directories(${COREPAL_SOURCE_DIR}/src)
include_directories(${COREPAL_SOURCE_DIR}/../inc)

if (NOT CLR_CMAKE_TARGET_BROWSER)
    add_compile_options(-fexceptions)
else()
    add_compile_options(-fwasm-exceptions)
    add_link_options(-fwasm-exceptions -sEXIT_RUNTIME=1)
endif()

add_subdirectory(src)
add_subdirectory(tests)
