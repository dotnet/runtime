# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

if(WIN32)
    add_definitions(-DWIN32)
    add_definitions(-D_WIN32=1)
    if(IS_64BIT_BUILD)
        add_definitions(-D_WIN64=1)
    endif()
    add_compile_options($<$<CONFIG:Debug>:-DDEBUG>)
    add_compile_options($<$<CONFIG:Debug>:/Od>)
    add_compile_options(/DEBUG)
    add_compile_options(/GS)
    add_compile_options(/W1)
    add_compile_options(/Zc:inline)
    add_compile_options(/fp:precise)
    add_compile_options(/EHsc)

    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /INCREMENTAL:NO")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /PDBCOMPRESS")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:1572864")

    # Debug build specific flags
    set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "/NOVCFEATURE")

    # Release build specific flags
    set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_STATIC_LINKER_FLAGS_RELEASE "${CMAKE_STATIC_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /DEBUG /OPT:REF /OPT:ICF")

    # RelWithDebInfo specific flags
    set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO} /DEBUG /OPT:REF /OPT:ICF")
    set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO} /DEBUG /OPT:REF /OPT:ICF")
endif()
