add_subdirectory(src)

# define variables for the configure_file below

if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
    set(arch x86_64)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    set(arch aarch64)
elseif(CLR_CMAKE_PLATFROM_ARCH_ARM)
    set(arch arm)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
    set(arch x86)
endif()

set(PKG_MAJOR "1")
set(PKG_MINOR "3")
set(PKG_EXTRA "-rc1")

configure_file(include/libunwind-common.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind-common.h)
configure_file(include/libunwind.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind.h)
configure_file(include/tdep/libunwind_i.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/tdep/libunwind_i.h)

