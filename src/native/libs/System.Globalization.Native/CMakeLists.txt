# Required for StaticICULinking
cmake_minimum_required(VERSION 3.10..3.20)

project(System.Globalization.Native C)

if(CLR_CMAKE_TARGET_UNIX OR CLR_CMAKE_TARGET_WASI)
    if (ANDROID_FORCE_ICU_DATA_DIR)
        add_definitions(-DANDROID_FORCE_ICU_DATA_DIR)
    endif()

    add_compile_options(-Wno-switch-enum)
    add_compile_options(-Wno-covered-switch-default)

    # Workaround for warnings produced by ICU headers
    add_compile_options(-Wno-reserved-id-macro)
    add_compile_options(-Wno-documentation)
    add_compile_options(-Wno-documentation-unknown-command)
    add_compile_options(-Wno-reserved-identifier)

    # Workaround for https://unicode-org.atlassian.net/browse/ICU-20601
    add_compile_options(-Wno-extra-semi-stmt)
    add_compile_options(-Wno-unknown-warning-option)

    # The mobile configurations in particular bring their own ICU, so skip
    if (NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND NOT DEFINED CMAKE_ICU_DIR)
        if (CLR_CMAKE_TARGET_OSX)
            execute_process(COMMAND  brew --prefix OUTPUT_VARIABLE brew_prefix OUTPUT_STRIP_TRAILING_WHITESPACE)
            set(ICU_HOMEBREW_INC_PATH "${brew_prefix}/opt/icu4c/include")
        endif()

        find_path(UCURR_H "unicode/ucurr.h" PATHS ${ICU_HOMEBREW_INC_PATH})
        if(UCURR_H STREQUAL UCURR_H-NOTFOUND)
            message(FATAL_ERROR "Cannot find ucurr.h, try installing libicu-dev (or the appropriate package for your platform)")
            return()
        endif()

        if(CLR_CMAKE_TARGET_OSX)
            add_definitions(-DOSX_ICU_LIBRARY_PATH="/usr/lib/libicucore.dylib")
            add_definitions(-DU_DISABLE_RENAMING)
        else()
            find_library(ICUUC icuuc)
            if(ICUUC STREQUAL ICUUC-NOTFOUND)
                message(FATAL_ERROR "Cannot find libicuuc, try installing libicu-dev (or the appropriate package for your platform)")
                return()
            endif()

            find_library(ICUI18N icui18n)
            if(ICUI18N STREQUAL ICUI18N-NOTFOUND)
                message(FATAL_ERROR "Cannot find libicui18n, try installing libicu-dev (or the appropriate package for your platform)")
                return()
            endif()
        endif()

        include_directories(${UCURR_H})
    endif()
endif()

include(configure.cmake)

set(NATIVEGLOBALIZATION_SOURCES
    pal_idna.c
    pal_common.c
)
if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS)
    set(NATIVEGLOBALIZATION_SOURCES
        ${NATIVEGLOBALIZATION_SOURCES}
        pal_calendarData.c
        pal_casing.c
        pal_collation.c
        pal_locale.c
        pal_localeNumberData.c
        pal_localeStringData.c
        pal_normalization.c
    )
endif()

if(CLR_CMAKE_TARGET_WIN32)
add_definitions(-DTARGET_WINDOWS)
add_definitions(-DPALEXPORT=EXTERN_C)
endif()

if (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    add_definitions(-DAPPLE_HYBRID_GLOBALIZATION)
    set(NATIVEGLOBALIZATION_SOURCES
        ${NATIVEGLOBALIZATION_SOURCES}
        pal_placeholders.c
    )
endif()

if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS AND DEFINED CMAKE_ICU_DIR)
    include_directories(${CMAKE_ICU_DIR}/include)
    link_libraries(${CMAKE_ICU_DIR}/lib/libicuuc.a ${CMAKE_ICU_DIR}/lib/libicui18n.a ${CMAKE_ICU_DIR}/lib/libicudata.a)
    link_libraries(stdc++)
endif()

if ((CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS) AND NOT DEFINED CMAKE_ICU_DIR)
    link_libraries(icucore)
endif()

if (CMAKE_USE_PTHREADS)
    add_compile_options(-pthread)
    add_linker_flag(-pthread)
endif()

if (LOCAL_BUILD)
    set(CMAKE_POSITION_INDEPENDENT_CODE ON)
    set(NATIVEGLOBALIZATION_SOURCES ${NATIVEGLOBALIZATION_SOURCES} pal_icushim_static.c)
    add_definitions(-DLOCAL_BUILD)
    add_definitions(-DSTATIC_ICU)
    add_definitions(-DPALEXPORT=EXTERN_C)
    add_definitions(-DTARGET_UNIX)
    # For minipal files
    include_directories(../../)
    include_directories(${CMAKE_CURRENT_BINARY_DIR})
elseif (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS OR CLR_CMAKE_TARGET_BROWSER OR CLR_CMAKE_TARGET_WASI)
    add_definitions(-DSTATIC_ICU)
    set(NATIVEGLOBALIZATION_SOURCES ${NATIVEGLOBALIZATION_SOURCES} pal_icushim_static.c)
else()
    set(NATIVEGLOBALIZATION_SOURCES ${NATIVEGLOBALIZATION_SOURCES} pal_icushim.c)
endif()

if (CLR_CMAKE_TARGET_APPLE)
    set(NATIVEGLOBALIZATION_SOURCES_OBJC
        pal_locale.m
        pal_collation.m
        pal_casing.m
        pal_calendarData.m
        pal_normalization.m
        pal_timeZoneInfo.m)
    set_source_files_properties(${NATIVEGLOBALIZATION_SOURCES_OBJC} PROPERTIES COMPILE_FLAGS "-fobjc-arc ${CLR_CMAKE_COMMON_OBJC_FLAGS}")
    set(NATIVEGLOBALIZATION_SOURCES ${NATIVEGLOBALIZATION_SOURCES} ${NATIVEGLOBALIZATION_SOURCES_OBJC})
endif()

# time zone names are filtered out of icu data for the browser and associated functionality is disabled
if (NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS) 
    set(NATIVEGLOBALIZATION_SOURCES ${NATIVEGLOBALIZATION_SOURCES} pal_timeZoneInfo.c)
endif()

if (MSVC)
    set_source_files_properties(${NATIVEGLOBALIZATION_SOURCES} PROPERTIES LANGUAGE CXX)
endif()

include_directories("../Common")

if (GEN_SHARED_LIB)
    if (CLR_CMAKE_TARGET_APPLE)
        include(CMakeFindFrameworks)
        find_library(FOUNDATION Foundation REQUIRED)
    endif()

    add_library(System.Globalization.Native
        SHARED
        ${NATIVEGLOBALIZATION_SOURCES}
        ${VERSION_FILE_PATH}
    )

    target_link_libraries(System.Globalization.Native
        PRIVATE
        ${CMAKE_DL_LIBS}
        ${FOUNDATION}
    )

    install_with_stripped_symbols (System.Globalization.Native PROGRAMS .)
endif()

add_library(System.Globalization.Native-Static
    STATIC
    ${NATIVEGLOBALIZATION_SOURCES}
    entrypoints.c
)

if(CLR_CMAKE_TARGET_UNIX OR CLR_CMAKE_TARGET_WASI)
    set_target_properties(System.Globalization.Native-Static PROPERTIES OUTPUT_NAME System.Globalization.Native  CLEAN_DIRECT_OUTPUT 1)
endif()

install (TARGETS System.Globalization.Native-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)

if(NOT CLR_CMAKE_TARGET_APPLE AND NOT CLR_CMAKE_TARGET_ANDROID AND NOT CLR_CMAKE_TARGET_LINUX_MUSL AND NOT CLR_CMAKE_TARGET_HAIKU)
    if (GEN_SHARED_LIB)
        add_custom_command(TARGET System.Globalization.Native POST_BUILD
            COMMENT "Verifying System.Globalization.Native.so dependencies"
            COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-so.sh
                $<TARGET_FILE:System.Globalization.Native>
                "Verification failed. System.Globalization.Native.so has undefined dependencies. These are likely ICU APIs that need to be added to icushim.h."
            VERBATIM
        )
    endif()
endif()

if(CLR_CMAKE_TARGET_WIN32)
    if(STATIC_LIBS_ONLY)
        add_library(System.Globalization.Native.Aot
            STATIC
            ${NATIVEGLOBALIZATION_SOURCES}
        )
        set_target_properties(System.Globalization.Native.Aot PROPERTIES CLR_CONTROL_FLOW_GUARD OFF)
        set_target_properties(System.Globalization.Native.Aot PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)

        add_library(System.Globalization.Native.Aot.GuardCF
            STATIC
            ${NATIVEGLOBALIZATION_SOURCES}
        )
        set_target_properties(System.Globalization.Native.Aot.GuardCF PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)

        install_static_library(System.Globalization.Native.Aot aotsdk nativeaot)
        install_static_library(System.Globalization.Native.Aot.GuardCF aotsdk nativeaot)
    endif()
endif()
