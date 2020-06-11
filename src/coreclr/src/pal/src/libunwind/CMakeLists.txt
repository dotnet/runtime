# define variables for the configure_file below

set(PKG_MAJOR "1")
set(PKG_MINOR "5")
set(PKG_EXTRA "-rc2")
if(CLR_CMAKE_HOST_UNIX)
    if (CLR_CMAKE_HOST_ARCH_AMD64)
      set(arch x86_64)
    elseif(CLR_CMAKE_HOST_ARCH_ARM64)
      set(arch aarch64)
    elseif(CLR_CMAKE_PLATFROM_ARCH_ARM)
      set(arch arm)
    elseif(CLR_CMAKE_HOST_ARCH_I386)
        set(arch x86)
    endif ()
endif(CLR_CMAKE_HOST_UNIX)

if(CLR_CMAKE_HOST_WIN32)
    if (CLR_CMAKE_TARGET_ARCH_AMD64)
      set(TARGET_AMD64 1)
      set(arch x86_64)
      add_definitions(-D__x86_64__)
      add_definitions(-D__amd64__)
    elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
      set(TARGET_AARCH64 1)
      set(arch aarch64)
      add_definitions(-D__aarch64__)
    elseif(CLR_CMAKE_TARGET_ARCH_ARM)
      set(TARGET_ARM 1)
      set(arch arm)
      add_definitions(-D__arm__)
    else ()
      message(FATAL_ERROR "Unrecognize value in environment variable TARGET")
    endif ()

    set(PACKAGE_STRING "libunwind")
    set(PACKAGE_BUGREPORT "")

    include(CheckCSourceCompiles)
    include(CheckIncludeFiles)

    # Our posix abstraction layer will provide these headers
    set(HAVE_ELF_H 1)
    set(HAVE_ENDIAN_H 1)

    # MSVC compiler is currently missing C11 stdalign.h header
    # Fake it until support is added
    check_include_files(stdalign.h HAVE_STDALIGN_H)
    if (NOT HAVE_STDALIGN_H)
        configure_file(include/win/fakestdalign.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/stdalign.h COPYONLY)
    endif (NOT HAVE_STDALIGN_H)

    # MSVC compiler is currently missing C11 stdatomic.h header
    # Fake it until support is added
    check_include_files(stdatomic.h HAVE_STDATOMIC_H)
    if (NOT HAVE_STDATOMIC_H)
        configure_file(include/win/fakestdatomic.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/stdatomic.h COPYONLY)
    endif (NOT HAVE_STDATOMIC_H)

    # MSVC compiler is currently missing C11 _Thread_local
    check_c_source_compiles("void main() { _Thread_local int a; }"  HAVE_THREAD_LOCAL)
    if (NOT HAVE_THREAD_LOCAL)
        add_definitions(-D_Thread_local=)
    endif (NOT HAVE_THREAD_LOCAL)

    configure_file(include/config.h.cmake.in ${CMAKE_CURRENT_BINARY_DIR}/include/config.h)
    add_definitions(-DHAVE_CONFIG_H=1)

    # Windows builds will only support remote unwind
    add_definitions(-DUNW_REMOTE_ONLY)

    # Disable security warnings
    add_definitions(-D_CRT_SECURE_NO_WARNINGS)

    if(CLR_CMAKE_TARGET_LINUX)
      add_definitions(-D__linux__)
    endif ()

    # The HAVE___THREAD set to 1 causes creation of thread local variable with tls_model("initial-exec")
    # which is incompatible with usage of the unwind code in a shared library.
    add_definitions(-DHAVE___THREAD=0)

    add_definitions(-D_GNU_SOURCE)

    # Assume we are using default MSVC compiler
    add_compile_options(/std:c++latest)
    add_compile_options(/TC) # compile all files as C
    add_compile_options(/permissive-)

    # include paths
    include_directories(include/tdep)
    include_directories(include)
    include_directories(${CMAKE_CURRENT_BINARY_DIR}/include/tdep)
    include_directories(${CMAKE_CURRENT_BINARY_DIR}/include)

    if(CLR_CMAKE_TARGET_ARCH_ARM)
        # The arm sources include ex_tables.h from include/tdep-arm without going through a redirection
        # in include/tdep like it works for similar files on other architectures. So we need to add
        # the include/tdep-arm to include directories
        include_directories(include/tdep-arm)
    endif()

    # files for cross os compilation
    include_directories(include/win)

    # Warnings in release builds
    add_compile_options(-wd4068) # ignore unknown pragma warnings (gcc pragmas)
    add_compile_options(-wd4146) # minus operator applied to unsigned
    add_compile_options(-wd4244) # possible loss of data
    add_compile_options(-wd4267) # possible loss of data
    add_compile_options(-wd4334) # 32-bit shift implicitly converted to 64 bits

    # Disable warning due to incorrect format specifier in debugging printf via the Debug macro
    add_compile_options(-wd4311) # pointer truncation from 'unw_word_t *' to 'long'
    add_compile_options(-wd4475) # 'fprintf' : length modifier 'L' cannot be used
    add_compile_options(-wd4477) # fprintf argument type

endif (CLR_CMAKE_HOST_WIN32)


configure_file(include/libunwind-common.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind-common.h)
configure_file(include/libunwind.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind.h)
configure_file(include/tdep/libunwind_i.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/tdep/libunwind_i.h)

add_subdirectory(src)
