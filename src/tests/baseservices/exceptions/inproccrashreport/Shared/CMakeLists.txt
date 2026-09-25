project(InProcCrashReportNative)

# Mobile builds use their functional-test adapters instead of native discovery.
if(CLR_CMAKE_TARGET_WIN32 OR
   CLR_CMAKE_TARGET_ANDROID OR
   (CLR_CMAKE_TARGET_APPLE_MOBILE AND NOT CRASH_REPORT_APPLE_APP) OR
   CLR_CMAKE_TARGET_ARCH_WASM)
    return()
endif()

# Native discovery runs independently of the managed project's runtime exclusions.
if(CLR_CMAKE_TEST_RUNTIME_FLAVOR STREQUAL "mono" OR CLR_CMAKE_TEST_BUILD_MODE STREQUAL "nativeaot")
    return()
endif()

set(CRASH_REPORT_SOURCE_DIR "${CLR_REPO_ROOT_DIR}/src/coreclr/debug/crashreport")

# build.sh installs native tests to <artifacts>/tests/coreclr/<os>.<arch>.<config>.
# Reuse the matching product build, including when -bindir relocates the artifacts.
# A different product configuration/location can be selected with -cmakeargs
# -DCRASH_REPORT_PAL_LIBRARY=<installed libcoreclrpal.a>.
if(NOT DEFINED CRASH_REPORT_PAL_LIBRARY)
    get_filename_component(CRASH_REPORT_PRODUCT_CONFIG "${CMAKE_INSTALL_PREFIX}" NAME)
    get_filename_component(CRASH_REPORT_ARTIFACTS_DIR "${CMAKE_INSTALL_PREFIX}/../../.." ABSOLUTE)
    set(CRASH_REPORT_PAL_LIBRARY "${CRASH_REPORT_ARTIFACTS_DIR}/bin/coreclr/${CRASH_REPORT_PRODUCT_CONFIG}/lib/libcoreclrpal.a")
endif()

if(NOT EXISTS "${CRASH_REPORT_PAL_LIBRARY}")
    message(FATAL_ERROR "Missing ${CRASH_REPORT_PAL_LIBRARY}. Build CoreCLR first, or set CRASH_REPORT_PAL_LIBRARY to the matching installed libcoreclrpal.a.")
endif()

# coreclrpal already contains the production reporter, writers, lifecycle and watchdog.
# CI builds this library in the product job, then transfers it as a native-test artifact.
# The test-running job copies the linked library; it does not need to relink the PAL.
add_library(InProcCrashReportNative SHARED inproccrashreport_test_driver.cpp)

target_include_directories(InProcCrashReportNative PRIVATE
    ${CRASH_REPORT_SOURCE_DIR}
    ${CLR_REPO_ROOT_DIR}/src/coreclr/pal/inc)

# configurecompiler.cmake supplies the HOST_*/TARGET_* architecture and OS definitions.
target_compile_definitions(InProcCrashReportNative PRIVATE PLATFORM_UNIX)

target_link_libraries(InProcCrashReportNative PRIVATE
    "${CRASH_REPORT_PAL_LIBRARY}"
    ${LINK_LIBRARIES_ADDITIONAL}
    m)

# The installed archive does not carry coreclrpal's CMake usage requirements.
# Keep these dependencies in sync with src/coreclr/pal/src/CMakeLists.txt.
# Pass CLR_CMAKE_USE_SYSTEM_LIBUNWIND when the product was built with it.

if(CLR_CMAKE_TARGET_OSX)
    foreach(framework IN ITEMS CoreFoundation CoreServices Security System)
        find_library(CRASH_REPORT_${framework} NAMES ${framework} REQUIRED)
        target_link_libraries(InProcCrashReportNative PRIVATE "${CRASH_REPORT_${framework}}")
    endforeach()
elseif(CLR_CMAKE_TARGET_LINUX)
    target_link_libraries(InProcCrashReportNative PRIVATE gcc_s pthread rt dl)
    if(CLR_CMAKE_TARGET_LINUX_MUSL AND (CLR_CMAKE_TARGET_ARCH_I386 OR CLR_CMAKE_TARGET_ARCH_POWERPC64))
        target_link_libraries(InProcCrashReportNative PRIVATE ucontext)
    endif()
elseif(CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)
    target_link_libraries(InProcCrashReportNative PRIVATE pthread rt)
elseif(CLR_CMAKE_TARGET_OPENBSD)
    target_link_libraries(InProcCrashReportNative PRIVATE pthread)
elseif(CLR_CMAKE_TARGET_HAIKU)
    target_link_libraries(InProcCrashReportNative PRIVATE bsd)
endif()

if(CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_OPENBSD OR CLR_CMAKE_TARGET_HAIKU OR
   (CLR_CMAKE_TARGET_LINUX AND CLR_CMAKE_USE_SYSTEM_LIBUNWIND))
    find_unwind_libs(CRASH_REPORT_UNWIND_LIBS)
    target_link_libraries(InProcCrashReportNative PRIVATE ${CRASH_REPORT_UNWIND_LIBS})
endif()

if(CLR_CMAKE_TARGET_NETBSD)
    find_library(CRASH_REPORT_KVM NAMES kvm REQUIRED)
    target_link_libraries(InProcCrashReportNative PRIVATE "${CRASH_REPORT_KVM}")
    if(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
        find_library(CRASH_REPORT_UNWIND NAMES unwind REQUIRED)
        target_link_libraries(InProcCrashReportNative PRIVATE "${CRASH_REPORT_UNWIND}")
    endif()
endif()

# Keep this PAL private rather than interposing with the hosting CoreCLR's PAL.
set(CRASH_REPORT_EXPORTS_FILE "${CMAKE_CURRENT_BINARY_DIR}/InProcCrashReportNative.exports")
if(CLR_CMAKE_TARGET_APPLE)
    file(GENERATE OUTPUT "${CRASH_REPORT_EXPORTS_FILE}" CONTENT "_InProcCrashReportTest_*\n")
else()
    file(GENERATE OUTPUT "${CRASH_REPORT_EXPORTS_FILE}" CONTENT "{ global: InProcCrashReportTest_*; local: *; };\n")
    target_link_options(InProcCrashReportNative PRIVATE "LINKER:-z,defs")
endif()
set_exports_linker_option("${CRASH_REPORT_EXPORTS_FILE}")
target_link_options(InProcCrashReportNative PRIVATE "${EXPORTS_LINKER_OPTION}")
set_property(TARGET InProcCrashReportNative APPEND PROPERTY LINK_DEPENDS "${CRASH_REPORT_EXPORTS_FILE}")

install(TARGETS InProcCrashReportNative DESTINATION bin)
