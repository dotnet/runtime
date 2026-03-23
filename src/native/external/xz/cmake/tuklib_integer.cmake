# SPDX-License-Identifier: 0BSD

#############################################################################
#
# tuklib_integer.cmake - see tuklib_integer.m4 for description and comments
#
# Author: Lasse Collin
#
#############################################################################

include("${CMAKE_CURRENT_LIST_DIR}/tuklib_common.cmake")
include(TestBigEndian)
include(CheckCSourceCompiles)
include(CheckIncludeFile)
include(CheckSymbolExists)

# An internal helper for tuklib_integer that attempts to detect if
# -mstrict-align or -mno-strict-align is in effect. This sets the
# cache variable TUKLIB_INTEGER_STRICT_ALIGN to ON if OBJDUMP_REGEX
# matches the objdump output of a check program. Otherwise it is set to OFF.
function(tuklib_integer_internal_strict_align OBJDUMP_REGEX)
    if(NOT DEFINED TUKLIB_INTEGER_STRICT_ALIGN)
        # Build a static library because then the function won't be optimized
        # away, and there won't be any unrelated startup code either.
        set(CMAKE_TRY_COMPILE_TARGET_TYPE STATIC_LIBRARY)

        # CMake >= 3.25 wouldn't require us to create a temporary file,
        # but the following method is compatible with 3.20.
        file(WRITE "${CMAKE_BINARY_DIR}/tuklib_integer_strict_align.c" "
                #include <string.h>
                unsigned int check_strict_align(const void *p)
                {
                    unsigned int i;
                    memcpy(&i, p, sizeof(i));
                    return i;
                }
            ")

        # Force -O2 because memcpy() won't be optimized out if optimizations
        # are disabled.
        try_compile(
            TRY_COMPILE_RESULT
            "${CMAKE_BINARY_DIR}"
            "${CMAKE_BINARY_DIR}/tuklib_integer_strict_align.c"
            COMPILE_DEFINITIONS "${CMAKE_REQUIRED_DEFINITIONS}"
            CMAKE_FLAGS "-DCOMPILE_DEFINITIONS=${CMAKE_REQUIRED_FLAGS} -O2"
            COPY_FILE "${CMAKE_BINARY_DIR}/tuklib_integer_strict_align.a"
        )

        if(NOT TRY_COMPILE_RESULT)
            message(FATAL_ERROR
                    "Compilation of the strict align check failed. "
                    "Either the specified compiler flags are broken "
                    "or ${CMAKE_CURRENT_FUNCTION_LIST_FILE} has a bug.")
        endif()

        # Use WORKING_DIRECTORY instead of passing the full path to objdump.
        # This ensures that the pathname won't affect the objdump output,
        # which could result in an unwanted regex match in the next step.
        execute_process(
            COMMAND "${CMAKE_OBJDUMP}" -d "tuklib_integer_strict_align.a"
            WORKING_DIRECTORY "${CMAKE_BINARY_DIR}"
            OUTPUT_VARIABLE OBJDUMP_OUTPUT
            RESULT_VARIABLE OBJDUMP_RESULT
        )

        # FIXME? Should we remove the temporary files here?

        # Look for instructions that load unsigned bytes. If none are found,
        # assume that -mno-strict-align is in effect.
        if(OBJDUMP_RESULT STREQUAL "0" AND
                OBJDUMP_OUTPUT MATCHES "${OBJDUMP_REGEX}")
            set(TUKLIB_INTEGER_STRICT_ALIGN ON CACHE INTERNAL "")
        else()
            set(TUKLIB_INTEGER_STRICT_ALIGN OFF CACHE INTERNAL "")
        endif()
    endif()
endfunction()

function(tuklib_integer TARGET_OR_ALL)
    # Check for endianness. Unlike the Autoconf's AC_C_BIGENDIAN, this doesn't
    # support Apple universal binaries. The CMake module will leave the
    # variable unset so we can catch that situation here instead of continuing
    # as if we were little endian.
    test_big_endian(WORDS_BIGENDIAN)
    if(NOT DEFINED WORDS_BIGENDIAN)
        message(FATAL_ERROR "Cannot determine endianness")
    endif()
    tuklib_add_definition_if("${TARGET_OR_ALL}" WORDS_BIGENDIAN)

    # Look for a byteswapping method.
    check_c_source_compiles("
            int main(void)
            {
                __builtin_bswap16(1);
                __builtin_bswap32(1);
                __builtin_bswap64(1);
                return 0;
            }
        "
        HAVE___BUILTIN_BSWAPXX)
    if(HAVE___BUILTIN_BSWAPXX)
        tuklib_add_definitions("${TARGET_OR_ALL}" HAVE___BUILTIN_BSWAPXX)
    else()
        check_include_file(byteswap.h HAVE_BYTESWAP_H)
        if(HAVE_BYTESWAP_H)
            tuklib_add_definitions("${TARGET_OR_ALL}" HAVE_BYTESWAP_H)
            check_symbol_exists(bswap_16 byteswap.h HAVE_BSWAP_16)
            tuklib_add_definition_if("${TARGET_OR_ALL}" HAVE_BSWAP_16)
            check_symbol_exists(bswap_32 byteswap.h HAVE_BSWAP_32)
            tuklib_add_definition_if("${TARGET_OR_ALL}" HAVE_BSWAP_32)
            check_symbol_exists(bswap_64 byteswap.h HAVE_BSWAP_64)
            tuklib_add_definition_if("${TARGET_OR_ALL}" HAVE_BSWAP_64)
        else()
            check_include_file(sys/endian.h HAVE_SYS_ENDIAN_H)
            if(HAVE_SYS_ENDIAN_H)
                tuklib_add_definitions("${TARGET_OR_ALL}" HAVE_SYS_ENDIAN_H)
            else()
                check_include_file(sys/byteorder.h HAVE_SYS_BYTEORDER_H)
                tuklib_add_definition_if("${TARGET_OR_ALL}"
                                         HAVE_SYS_BYTEORDER_H)
            endif()
        endif()
    endif()

    # Autodetect if unaligned memory access is fast when the cache variable
    # TUKLIB_FAST_UNALIGNED_ACCESS isn't set. The result is stored in
    # FAST_UNALIGNED_GUESS. Assume that unaligned access shouldn't be used.
    # Initialize the variable here so that it's never undefined in the
    # option() command after the if()...endif() block.
    set(FAST_UNALIGNED_GUESS OFF)
    if(NOT DEFINED TUKLIB_FAST_UNALIGNED_ACCESS)
        message(CHECK_START "Check if unaligned memory access should be used")

        # Guess that unaligned access is fast on these archs:
        #   - 32/64-bit x86 / x86-64
        #   - 32/64-bit big endian PowerPC
        #   - 64-bit little endian PowerPC
        #   - 32/64-bit Loongarch (*)
        #   - Some 32-bit ARM
        #   - Some 64-bit ARM64 (AArch64)
        #   - Some 32/64-bit RISC-V
        #
        # (*) See sections 7.4, 8.1, and 8.2:
        #     https://github.com/loongson/la-softdev-convention/blob/v0.2/la-softdev-convention.adoc
        #
        #     That is, desktop and server processors likely support
        #     unaligned access in hardware but embedded processors
        #     might not. GCC defaults to -mno-strict-align and so
        #     do majority of GNU/Linux distributions. As of
        #     GCC 15.2, there is no predefined macro to detect
        #     if -mstrict-align or -mno-strict-align is in effect.
        #     We use heuristics based on compiler output.
        #
        # CMake < 4.1 doesn't provide a standardized/normalized list of arch
        # names. For example, x86-64 may be "x86_64" (Linux),
        # "AMD64" (Windows), or even "EM64T" (64-bit WinXP).
        string(TOLOWER "${CMAKE_SYSTEM_PROCESSOR}" PROCESSOR)

        # CMake 4.1 made CMAKE_<LANG>_COMPILER_ARCHITECTURE_ID useful on many
        # targets. In earlier versions it's still useful with MSVC with which
        # CMAKE_SYSTEM_PROCESSOR can refer to the build machine.
        if(NOT CMAKE_C_COMPILER_ARCHITECTURE_ID STREQUAL "")
            # CMake 4.2.0 docs say that the list typically has only one entry
            # except possibly on macOS. On macOS, most (all?) archs support
            # unaligned access. Just pick the first one from the list.
            list(GET CMAKE_C_COMPILER_ARCHITECTURE_ID 0 PROCESSOR)
            string(TOLOWER "${PROCESSOR}" PROCESSOR)
        endif()

        # There is no ^ in the first regex branch to allow "i" at
        # the beginning so it can match "i386" to "i786", and "x86_64".
        if(PROCESSOR MATCHES "[x34567]86|^x64|^amd64|^em64t")
            set(FAST_UNALIGNED_GUESS ON)

        elseif(PROCESSOR MATCHES "^powerpc|^ppc")
            if(WORDS_BIGENDIAN OR PROCESSOR MATCHES "64")
                set(FAST_UNALIGNED_GUESS ON)
            endif()

        elseif(PROCESSOR MATCHES "^arm|^riscv" AND
                NOT PROCESSOR MATCHES "^arm64")
            # On 32-bit ARM, GCC and Clang # #define __ARM_FEATURE_UNALIGNED
            # if and only if unaligned access is supported.
            #
            # RISC-V C API Specification says that if
            # __riscv_misaligned_fast is defined then
            # unaligned access is known to be fast.
            #
            # MSVC is handled as a special case: We assume that
            # 32-bit ARM supports fast unaligned access.
            # If MSVC gets RISC-V support then this will assume
            # fast unaligned access on RISC-V too.
            check_c_source_compiles("
                    #if !defined(__ARM_FEATURE_UNALIGNED) \
                            && !defined(__riscv_misaligned_fast) \
                            && !defined(_MSC_VER)
                    compile error
                    #endif
                    int main(void) { return 0; }
                "
                TUKLIB_FAST_UNALIGNED_DEFINED_BY_PREPROCESSOR)
            if(TUKLIB_FAST_UNALIGNED_DEFINED_BY_PREPROCESSOR)
                set(FAST_UNALIGNED_GUESS ON)
            endif()

        elseif(PROCESSOR MATCHES "^aarch64|^arm64")
            # On ARM64, Clang defines __ARM_FEATURE_UNALIGNED if and only if
            # unaligned access is supported. However, GCC (at least up to 15.2.0)
            # defines it even when using -mstrict-align, so autodetection with
            # this macro doesn't work with GCC on ARM64. (It does work on
            # 32-bit ARM.) See:
            #
            #     https://gcc.gnu.org/bugzilla/show_bug.cgi?id=111555
            #
            # We need three checks:
            #
            # 1. If __ARM_FEATURE_UNALIGNED is defined and the
            #    compiler isn't GCC, unaligned access is enabled.
            #    If the compiler is MSVC, unaligned access is
            #    enabled even without __ARM_FEATURE_UNALIGNED.
            check_c_source_compiles("
                    #if defined(__ARM_FEATURE_UNALIGNED) \
                            && (!defined(__GNUC__) || defined(__clang__))
                    #elif defined(_MSC_VER)
                    #else
                    compile error
                    #endif
                    int main(void) { return 0; }
                "
                TUKLIB_FAST_UNALIGNED_DEFINED_BY_PREPROCESSOR)
            if(TUKLIB_FAST_UNALIGNED_DEFINED_BY_PREPROCESSOR)
                set(FAST_UNALIGNED_GUESS ON)
            else()
                # 2. If __ARM_FEATURE_UNALIGNED is not defined,
                #    unaligned access is disabled.
                check_c_source_compiles("
                        #ifdef __ARM_FEATURE_UNALIGNED
                        compile error
                        #endif
                        int main(void) { return 0; }
                    "
                    TUKLIB_FAST_UNALIGNED_NOT_DEFINED_BY_PREPROCESSOR)
                if(NOT TUKLIB_FAST_UNALIGNED_NOT_DEFINED_BY_PREPROCESSOR)
                    # 3. Use heuristics to detect if -mstrict-align is
                    #    in effect when building with GCC.
                    tuklib_integer_internal_strict_align("[ \t]ldrb[ \t]")
                    if(NOT TUKLIB_INTEGER_STRICT_ALIGN)
                        set(FAST_UNALIGNED_GUESS ON)
                    endif()
                endif()
            endif()

        elseif(PROCESSOR MATCHES "^loongarch")
            tuklib_integer_internal_strict_align("[ \t]ld\\.bu[ \t]")
            if(NOT TUKLIB_INTEGER_STRICT_ALIGN)
                set(FAST_UNALIGNED_GUESS ON)
            endif()
        endif()

        if(FAST_UNALIGNED_GUESS)
            message(CHECK_PASS "yes")
        else()
            message(CHECK_PASS "no")
        endif()
    endif()

    option(TUKLIB_FAST_UNALIGNED_ACCESS
           "Enable if the system supports *fast* unaligned memory access \
with 16-bit, 32-bit, and 64-bit integers."
           "${FAST_UNALIGNED_GUESS}")
    tuklib_add_definition_if("${TARGET_OR_ALL}" TUKLIB_FAST_UNALIGNED_ACCESS)

    # Unsafe type punning:
    option(TUKLIB_USE_UNSAFE_TYPE_PUNNING
           "This introduces strict aliasing violations and \
may result in broken code. However, this might improve performance \
in some cases, especially with old compilers \
(e.g. GCC 3 and early 4.x on x86, GCC < 6 on ARMv6 and ARMv7)."
           OFF)
    tuklib_add_definition_if("${TARGET_OR_ALL}" TUKLIB_USE_UNSAFE_TYPE_PUNNING)

    # Check for GCC/Clang __builtin_assume_aligned().
    check_c_source_compiles(
        "int main(void) { __builtin_assume_aligned(\"\", 1); return 0; }"
        HAVE___BUILTIN_ASSUME_ALIGNED)
    tuklib_add_definition_if("${TARGET_OR_ALL}" HAVE___BUILTIN_ASSUME_ALIGNED)
endfunction()
