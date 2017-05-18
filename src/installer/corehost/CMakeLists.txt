cmake_minimum_required (VERSION 2.6)

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name Linux")
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name Darwin")
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    add_definitions(-D_BSD_SOURCE) # required for getline
    message("System name FreeBSD")
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name OpenBSD")
endif(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)

if(CMAKE_SYSTEM_NAME STREQUAL NetBSD)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name NetBSD")
endif(CMAKE_SYSTEM_NAME STREQUAL NetBSD)

if(CMAKE_SYSTEM_NAME STREQUAL SunOS)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    message("System name SunOS")
endif(CMAKE_SYSTEM_NAME STREQUAL SunOS)

if (NOT WIN32)
    if (CMAKE_SYSTEM_NAME STREQUAL Darwin)
        # Ensure that dsymutil and strip are present
        find_program(DSYMUTIL dsymutil)
        if (DSYMUTIL STREQUAL "DSYMUTIL-NOTFOUND")
            message(FATAL_ERROR "dsymutil not found")
        endif()

        find_program(STRIP strip)
        if (STRIP STREQUAL "STRIP-NOTFOUND")
            message(FATAL_ERROR "strip not found")
        endif()
    else (CMAKE_SYSTEM_NAME STREQUAL Darwin)
        # Ensure that objcopy is present
        if(DEFINED ENV{CROSSCOMPILE})
            if(CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l OR CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
                find_program(OBJCOPY ${TOOLCHAIN}-objcopy)
            else()
                message(FATAL_ERROR "Only AMD64, X86, ARM64 and ARM are supported")
            endif()
        else()
            find_program(OBJCOPY objcopy)
        endif()
        if (OBJCOPY STREQUAL "OBJCOPY-NOTFOUND" AND NOT CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
            message(FATAL_ERROR "objcopy not found")
        endif()
    endif (CMAKE_SYSTEM_NAME STREQUAL Darwin)
endif ()

function(strip_symbols targetName outputFilename)
    if(CLR_CMAKE_PLATFORM_UNIX)
        if(STRIP_SYMBOLS)

            # On the older version of cmake (2.8.12) used on Ubuntu 14.04 the TARGET_FILE
            # generator expression doesn't work correctly returning the wrong path and on
            # the newer cmake versions the LOCATION property isn't supported anymore.
            if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
                set(strip_source_file $<TARGET_FILE:${targetName}>)
            else()
                get_property(strip_source_file TARGET ${targetName} PROPERTY LOCATION)
            endif()

            if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
                set(strip_destination_file ${strip_source_file}.dwarf)

                add_custom_command(
                    TARGET ${targetName}
                    POST_BUILD
                    VERBATIM 
                    COMMAND ${DSYMUTIL} --flat --minimize ${strip_source_file}
                    COMMAND ${STRIP} -u -r ${strip_source_file}
                    COMMENT Stripping symbols from ${strip_source_file} into file ${strip_destination_file}
                )
            else(CMAKE_SYSTEM_NAME STREQUAL Darwin)
                set(strip_destination_file ${strip_source_file}.dbg)

                add_custom_command(
                    TARGET ${targetName}
                    POST_BUILD
                    VERBATIM 
                    COMMAND ${OBJCOPY} --only-keep-debug ${strip_source_file} ${strip_destination_file}
                    COMMAND ${OBJCOPY} --strip-unneeded ${strip_source_file}
                    COMMAND ${OBJCOPY} --add-gnu-debuglink=${strip_destination_file} ${strip_source_file}
                    COMMENT Stripping symbols from ${strip_source_file} into file ${strip_destination_file}
                )
            endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

            set(${outputFilename} ${strip_destination_file} PARENT_SCOPE)
        endif(STRIP_SYMBOLS)
    endif(CLR_CMAKE_PLATFORM_UNIX)
endfunction()

function(install_library_and_symbols targetName)
    strip_symbols(${targetName} strip_destination_file)

    # On the older version of cmake (2.8.12) used on Ubuntu 14.04 the TARGET_FILE
    # generator expression doesn't work correctly returning the wrong path and on
    # the newer cmake versions the LOCATION property isn't supported anymore.
    if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
        set(install_source_file $<TARGET_FILE:${targetName}>)
    else()
        get_property(install_source_file TARGET ${targetName} PROPERTY LOCATION)
    endif()

    install(PROGRAMS ${install_source_file} DESTINATION .)
    if(WIN32)
        install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION PDB)
    else()
        install(FILES ${strip_destination_file} DESTINATION .)
    endif()
endfunction()

add_subdirectory(cli)
