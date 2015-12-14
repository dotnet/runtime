cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
    add_definitions(-DFEATURE_ENABLE_HARDWARE_EXCEPTIONS)
endif(CLR_CMAKE_PLATFORM_UNIX)

# Test DLL1

set(DEF_SOURCES1 dlltest1.src)
convert_to_absolute_path(DEF_SOURCES1 ${DEF_SOURCES1})
set(EXPORTS_FILE1 ${CMAKE_CURRENT_BINARY_DIR}/dlltest1.exports)
generate_exports_file(${DEF_SOURCES1} ${EXPORTS_FILE1})

if(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    set(EXPORTS_LINKER_OPTION1 -Wl,--version-script=${EXPORTS_FILE1})
endif(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(EXPORTS_LINKER_OPTION1 -Wl,-exported_symbols_list,${EXPORTS_FILE1})
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

set(DLL1SOURCES dlltest1.cpp)
add_library(paltest_pal_sxs_test1_dll1 SHARED ${DLL1SOURCES})
add_custom_target(dlltest1_exports DEPENDS ${EXPORTS_FILE1})
set_property(TARGET paltest_pal_sxs_test1_dll1 APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION1})
set_property(TARGET paltest_pal_sxs_test1_dll1 APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE1})

add_dependencies(paltest_pal_sxs_test1_dll1
    dlltest1_exports
    coreclrpal
)

target_link_libraries(paltest_pal_sxs_test1_dll1
    pthread
    m
    coreclrpal
)

# Test DLL2

set(DEF_SOURCES2 dlltest2.src)
convert_to_absolute_path(DEF_SOURCES2 ${DEF_SOURCES2})
set(EXPORTS_FILE2 ${CMAKE_CURRENT_BINARY_DIR}/dlltest2.exports)
generate_exports_file(${DEF_SOURCES2} ${EXPORTS_FILE2})

if(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    set(EXPORTS_LINKER_OPTION2 -Wl,--version-script=${EXPORTS_FILE2})
endif(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(EXPORTS_LINKER_OPTION2 -Wl,-exported_symbols_list,${EXPORTS_FILE2})
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

set(DLL2SOURCES dlltest2.cpp)
add_library(paltest_pal_sxs_test1_dll2 SHARED ${DLL2SOURCES})
add_custom_target(dlltest2_exports DEPENDS ${EXPORTS_FILE2})
set_property(TARGET paltest_pal_sxs_test1_dll2 APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION2})
set_property(TARGET paltest_pal_sxs_test1_dll2 APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE2})

add_dependencies(paltest_pal_sxs_test1_dll2
   dlltest2_exports
   coreclrpal
)

target_link_libraries(paltest_pal_sxs_test1_dll2
   pthread
   m
   coreclrpal
)

# Main program

set(TESTSOURCES exceptionsxs.cpp)

add_executable(paltest_pal_sxs_test1 ${TESTSOURCES})

add_dependencies(paltest_pal_sxs_test1
   paltest_pal_sxs_test1_dll1
   paltest_pal_sxs_test1_dll2
)

target_link_libraries(paltest_pal_sxs_test1
   paltest_pal_sxs_test1_dll1
   paltest_pal_sxs_test1_dll2
)

