project(System.Security.Cryptography.Native.Apple C)

include(${CMAKE_CURRENT_LIST_DIR}/extra_libs.cmake)

set(NATIVE_LIBS_EXTRA)
append_extra_cryptography_apple_libs(NATIVE_LIBS_EXTRA)

set(NATIVECRYPTO_SOURCES
    pal_digest.c
    pal_ecc.c
    pal_hmac.c
    pal_keyagree.c
    pal_keyderivation.c
    pal_random.c
    pal_rsa.c
    pal_sec.c
    pal_seckey.c
    pal_signverify.c
    pal_ssl.c
    pal_symmetric.c
    pal_x509.c
    pal_x509chain.c
)

if (CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    set(NATIVECRYPTO_SOURCES
        ${NATIVECRYPTO_SOURCES}
        pal_keychain_ios.c
        pal_x509_ios.c
    )
else()
    set(NATIVECRYPTO_SOURCES
        ${NATIVECRYPTO_SOURCES}
        pal_swiftbindings.o
        pal_keychain_macos.c
        pal_seckey_macos.c
        pal_trust_macos.c
        pal_x509_macos.c
    )
endif()

# As of CMake 3.20.2 support for Swift only works with the Ninja and XCode
# generators so we cannot rely on it. Even with the Ninja generator it doesn't
# work in combination with other languages within the same library.
if (NOT SWIFT_COMPILER_TARGET AND CLR_CMAKE_TARGET_OSX)
    set(SWIFT_PLATFORM "macosx")
    set(SWIFT_PLATFORM_SUFFIX "")
    set(SWIFT_DEPLOYMENT_TARGET ${CMAKE_OSX_DEPLOYMENT_TARGET})
    set(SWIFT_COMPILER_TARGET "${CMAKE_OSX_ARCHITECTURES}-apple-${SWIFT_PLATFORM}${SWIFT_DEPLOYMENT_TARGET}${SWIFT_PLATFORM_SUFFIX}")
endif()

add_custom_command(
    OUTPUT pal_swiftbindings.o
    COMMAND xcrun swiftc -emit-object -static -parse-as-library -runtime-compatibility-version none -sdk ${CMAKE_OSX_SYSROOT} -target ${SWIFT_COMPILER_TARGET} ${CMAKE_CURRENT_SOURCE_DIR}/pal_swiftbindings.swift -o pal_swiftbindings.o
    MAIN_DEPENDENCY ${CMAKE_CURRENT_SOURCE_DIR}/pal_swiftbindings.swift
    COMMENT "Compiling Swift file pal_swiftbindings.swift"
)
set_source_files_properties(pal_swiftbindings.o PROPERTIES EXTERNAL_OBJECT true GENERATED true)

if (CLR_CMAKE_TARGET_MACCATALYST)
    add_definitions(-DTARGET_MACCATALYST)
endif()

if (CLR_CMAKE_TARGET_IOS)
    add_definitions(-DTARGET_IOS)
endif()

if (CLR_CMAKE_TARGET_TVOS)
    add_definitions(-DTARGET_TVOS)
endif()

if (GEN_SHARED_LIB)
    add_library(System.Security.Cryptography.Native.Apple
        SHARED
        ${NATIVECRYPTO_SOURCES}
        ${VERSION_FILE_PATH}
    )
endif()

if (NOT GEN_SHARED_LIB AND NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS)
    set(NATIVECRYPTO_SOURCES ${NATIVECRYPTO_SOURCES} entrypoints.c)
endif()

add_library(System.Security.Cryptography.Native.Apple-Static
    STATIC
    ${NATIVECRYPTO_SOURCES}
)

set_target_properties(System.Security.Cryptography.Native.Apple-Static PROPERTIES OUTPUT_NAME System.Security.Cryptography.Native.Apple CLEAN_DIRECT_OUTPUT 1)

if (GEN_SHARED_LIB)
    target_link_libraries(System.Security.Cryptography.Native.Apple
        ${NATIVE_LIBS_EXTRA}
    )

    if (NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS)
        add_custom_command(TARGET System.Security.Cryptography.Native.Apple POST_BUILD
            COMMENT "Verifying System.Security.Cryptography.Native.Apple points against entrypoints.c "
            COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/../verify-entrypoints.sh
                $<TARGET_FILE:System.Security.Cryptography.Native.Apple>
                ${CMAKE_CURRENT_SOURCE_DIR}/entrypoints.c
                ${CMAKE_NM}
            VERBATIM
        )
    endif()
endif()

if (GEN_SHARED_LIB)
    install_with_stripped_symbols (System.Security.Cryptography.Native.Apple PROGRAMS .)
endif()

install (TARGETS System.Security.Cryptography.Native.Apple-Static DESTINATION ${STATIC_LIB_DESTINATION} COMPONENT libs)
