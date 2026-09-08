project(nativeaot-createdump)

# Enabled variant: full ELF core dump writer (Linux only).
# macOS support requires Mach-O dump format and Mach APIs — deferred to future work.
if(CLR_CMAKE_TARGET_LINUX AND (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64))

    set(NATIVEAOT_CREATEDUMP_SOURCES
        nativeaot_createdump_main.c
        process_reader.c
        elf_dump_writer.c
    )

    add_library(nativeaot-createdump-enabled STATIC ${NATIVEAOT_CREATEDUMP_SOURCES})
    target_include_directories(nativeaot-createdump-enabled PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

    install_static_library(nativeaot-createdump-enabled aotsdk nativeaot)

endif()

# Disabled variant: no-op stubs (all platforms).
add_library(nativeaot-createdump-disabled STATIC createdump_disabled.c)

install_static_library(nativeaot-createdump-disabled aotsdk nativeaot)
