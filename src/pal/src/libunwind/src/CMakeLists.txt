cmake_minimum_required(VERSION 2.8.12.2)

project(unwind)

set(CMAKE_INCLUDE_CURRENT_DIR ON)


include(configure.cmake)

# The HAVE___THREAD set to 1 causes creation of thread local variable with tls_model("initial-exec")
# which is incompatible with usage of the unwind code in a shared library.
add_definitions(-DHAVE___THREAD=0)
add_definitions(-DHAVE_CONFIG_H=1)

add_definitions(-DPACKAGE_STRING="")
add_definitions(-DPACKAGE_BUGREPORT="")

add_definitions(-D_GNU_SOURCE)
# Ensure that the remote and local unwind code can reside in the same binary without name clashing
add_definitions("-Ddwarf_search_unwind_table_int=UNW_OBJ(dwarf_search_unwind_table_int)")

# Disable warning due to incorrect format specifier in debugging printf via the Debug macro
add_compile_options(-Wno-format)

if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wno-header-guard)
elseif()
  add_compile_options(-Wno-unused-value)
endif()

if(CLR_CMAKE_PLATFORM_ARCH_ARM)
    # Ensure that the remote and local unwind code can reside in the same binary without name clashing
    add_definitions("-Darm_search_unwind_table=UNW_OBJ(arm_search_unwind_table)")
    # Disable warning in asm: use of SP or PC in the list is deprecated
    add_compile_options(-Wno-inline-asm)
    if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        # Disable warning due to labs function called on unsigned argument
        add_compile_options(-Wno-absolute-value)
    endif()
    # Disable warning for a bug in the libunwind source src/arm/Gtrace.c:529, but not in code that we exercise
    add_compile_options(-Wno-implicit-function-declaration)
    # Disable warning due to an unused function prel31_read
    add_compile_options(-Wno-unused-function)
    # We compile code with -std=c99 and the asm keyword is not recognized as it is a gnu extension
    add_definitions(-Dasm=__asm__)
    # The arm sources include ex_tables.h from include/tdep-arm without going through a redirection
    # in include/tdep like it works for similar files on other architectures. So we need to add
    # the include/tdep-arm to include directories
    include_directories(../include/tdep-arm)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        # Disable warning due to labs function called on unsigned argument
        add_compile_options(-Wno-absolute-value)
    endif()
    # We compile code with -std=c99 and the asm keyword is not recognized as it is a gnu extension
    add_definitions(-Dasm=__asm__)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
    # Disable warning for a bug in the libunwind source src/x86/Gos-linux.c, but not in code that we exercise
    add_compile_options(-Wno-incompatible-pointer-types)
endif()

SET(libunwind_ptrace_la_SOURCES
    ptrace/_UPT_elf.c
    ptrace/_UPT_accessors.c ptrace/_UPT_access_fpreg.c
    ptrace/_UPT_access_mem.c ptrace/_UPT_access_reg.c
    ptrace/_UPT_create.c ptrace/_UPT_destroy.c
    ptrace/_UPT_find_proc_info.c ptrace/_UPT_get_dyn_info_list_addr.c
    ptrace/_UPT_put_unwind_info.c ptrace/_UPT_get_proc_name.c
    ptrace/_UPT_reg_offset.c ptrace/_UPT_resume.c
)

SET(libunwind_coredump_la_SOURCES
    coredump/_UCD_accessors.c
    coredump/_UCD_create.c
    coredump/_UCD_destroy.c
    coredump/_UCD_access_mem.c
    coredump/_UCD_elf_map_image.c
    coredump/_UCD_find_proc_info.c
    coredump/_UCD_get_proc_name.c

    coredump/_UPT_elf.c
    coredump/_UPT_access_fpreg.c
    coredump/_UPT_get_dyn_info_list_addr.c
    coredump/_UPT_put_unwind_info.c
    coredump/_UPT_resume.c
)

# List of arch-independent files needed by generic library (libunwind-$ARCH):
SET(libunwind_la_SOURCES_generic
    mi/Gdyn-extract.c mi/Gdyn-remote.c mi/Gfind_dynamic_proc_info.c
    # The Gget_accessors.c implements the same function as Lget_accessors.c, so
    # the source is excluded here to prevent name clash
    #mi/Gget_accessors.c
    mi/Gget_proc_info_by_ip.c mi/Gget_proc_name.c
    mi/Gput_dynamic_unwind_info.c mi/Gdestroy_addr_space.c
    mi/Gget_reg.c mi/Gset_reg.c
    mi/Gget_fpreg.c mi/Gset_fpreg.c
    mi/Gset_caching_policy.c
    mi/Gset_cache_size.c
)

SET(libunwind_la_SOURCES_os_linux
    os-linux.c
)

SET(libunwind_la_SOURCES_os_linux_local
# Nothing when we don't want to support CXX exceptions
)

SET(libunwind_la_SOURCES_os_freebsd
    os-freebsd.c
)

SET(libunwind_la_SOURCES_os_freebsd_local
# Nothing
)

if(CLR_CMAKE_PLATFORM_LINUX)
    SET(libunwind_la_SOURCES_os                 ${libunwind_la_SOURCES_os_linux})
    SET(libunwind_la_SOURCES_os_local           ${libunwind_la_SOURCES_os_linux_local})
    SET(libunwind_la_SOURCES_x86_os             x86/Gos-linux.c)
    SET(libunwind_x86_la_SOURCES_os             x86/getcontext-linux.S)
    SET(libunwind_la_SOURCES_x86_os_local       x86/Los-linux.c)
    SET(libunwind_la_SOURCES_x86_64_os          x86_64/Gos-linux.c)
    SET(libunwind_la_SOURCES_x86_64_os_local    x86_64/Los-linux.c)
    SET(libunwind_la_SOURCES_arm_os             arm/Gos-linux.c)
    SET(libunwind_la_SOURCES_arm_os_local       arm/Los-linux.c)
    list(APPEND libunwind_coredump_la_SOURCES   coredump/_UCD_access_reg_linux.c)
elseif(CLR_CMAKE_PLATFORM_FREEBSD)
    SET(libunwind_la_SOURCES_os                 ${libunwind_la_SOURCES_os_freebsd})
    SET(libunwind_la_SOURCES_os_local           ${libunwind_la_SOURCES_os_freebsd_local})
    SET(libunwind_la_SOURCES_x86_os             x86/Gos-freebsd.c)
    SET(libunwind_x86_la_SOURCES_os             x86/getcontext-freebsd.S)
    SET(libunwind_la_SOURCES_x86_os_local       x86/Los-freebsd.c)
    SET(libunwind_la_SOURCES_x86_64_os          x86_64/Gos-freebsd.c)
    SET(libunwind_la_SOURCES_x86_64_os_local    x86_64/Los-freebsd.c)
    SET(libunwind_la_SOURCES_arm_os             arm/Gos-freebsd.c)
    SET(libunwind_la_SOURCES_arm_os_local       arm/Los-freebsd.c)
    list(APPEND libunwind_coredump_la_SOURCES   coredump/_UCD_access_reg_freebsd.c)
endif()

# List of arch-independent files needed by both local-only and generic
# libraries:
SET(libunwind_la_SOURCES_common
    ${libunwind_la_SOURCES_os}
    mi/init.c mi/flush_cache.c mi/mempool.c mi/strerror.c
)

SET(libunwind_la_SOURCES_local_unwind
# Nothing when we don't want to support CXX exceptions
)

# List of arch-independent files needed by local-only library (libunwind):
SET(libunwind_la_SOURCES_local_nounwind
    ${libunwind_la_SOURCES_os_local}
    mi/backtrace.c
    mi/dyn-cancel.c mi/dyn-info-list.c mi/dyn-register.c
    mi/Ldyn-extract.c mi/Lfind_dynamic_proc_info.c
    mi/Lget_accessors.c
    mi/Lget_proc_info_by_ip.c mi/Lget_proc_name.c
    mi/Lput_dynamic_unwind_info.c mi/Ldestroy_addr_space.c
    mi/Lget_reg.c   mi/Lset_reg.c
    mi/Lget_fpreg.c mi/Lset_fpreg.c
    mi/Lset_caching_policy.c
    mi/Lset_cache_size.c
)

SET(libunwind_la_SOURCES_local
    ${libunwind_la_SOURCES_local_nounwind}
    ${libunwind_la_SOURCES_local_unwind}
)

SET(libunwind_dwarf_common_la_SOURCES
    dwarf/global.c
)

SET(libunwind_dwarf_local_la_SOURCES
    dwarf/Lexpr.c dwarf/Lfde.c dwarf/Lparser.c dwarf/Lpe.c
    dwarf/Lfind_proc_info-lsb.c
    dwarf/Lfind_unwind_table.c
)

SET(libunwind_dwarf_generic_la_SOURCES
    dwarf/Gexpr.c dwarf/Gfde.c dwarf/Gparser.c dwarf/Gpe.c
    dwarf/Gfind_proc_info-lsb.c
    dwarf/Gfind_unwind_table.c
)

SET(libunwind_elf32_la_SOURCES
    elf32.c
)

SET(libunwind_elf64_la_SOURCES
    elf64.c
)
SET(libunwind_elfxx_la_SOURCES
    elfxx.c
)

# The list of files that go into libunwind and libunwind-aarch64:
SET(libunwind_la_SOURCES_aarch64_common
    ${libunwind_la_SOURCES_common}
    aarch64/is_fpreg.c
    aarch64/regname.c
)

# The list of files that go into libunwind:
SET(libunwind_la_SOURCES_aarch64
    ${libunwind_la_SOURCES_aarch64_common}
    ${libunwind_la_SOURCES_local}
    aarch64/Lapply_reg_state.c aarch64/Lreg_states_iterate.c
    aarch64/Lcreate_addr_space.c aarch64/Lget_proc_info.c
    aarch64/Lget_save_loc.c aarch64/Lglobal.c aarch64/Linit.c
    aarch64/Linit_local.c aarch64/Linit_remote.c
    aarch64/Lis_signal_frame.c aarch64/Lregs.c aarch64/Lresume.c
    aarch64/Lstash_frame.c aarch64/Lstep.c aarch64/Ltrace.c
    aarch64/getcontext.S
)

SET(libunwind_aarch64_la_SOURCES_aarch64
    ${libunwind_la_SOURCES_aarch64_common}
    ${libunwind_la_SOURCES_generic}
    aarch64/Gapply_reg_state.c aarch64/Greg_states_iterate.c
    aarch64/Gcreate_addr_space.c aarch64/Gget_proc_info.c
    aarch64/Gget_save_loc.c aarch64/Gglobal.c aarch64/Ginit.c
    aarch64/Ginit_local.c aarch64/Ginit_remote.c
    aarch64/Gis_signal_frame.c aarch64/Gregs.c aarch64/Gresume.c
    aarch64/Gstash_frame.c aarch64/Gstep.c aarch64/Gtrace.c
)

# The list of files that go into libunwind and libunwind-arm:
SET(libunwind_la_SOURCES_arm_common
    ${libunwind_la_SOURCES_common}
    arm/is_fpreg.c arm/regname.c
)

# The list of files that go into libunwind:
SET(libunwind_la_SOURCES_arm
    ${libunwind_la_SOURCES_arm_common}
    ${libunwind_la_SOURCES_arm_os_local}
    ${libunwind_la_SOURCES_local}
    arm/getcontext.S
    arm/Lapply_reg_state.c arm/Lreg_states_iterate.c
    arm/Lcreate_addr_space.c arm/Lget_proc_info.c arm/Lget_save_loc.c
    arm/Lglobal.c arm/Linit.c arm/Linit_local.c arm/Linit_remote.c
    arm/Lregs.c arm/Lresume.c arm/Lstep.c
    arm/Lex_tables.c arm/Lstash_frame.c arm/Ltrace.c
)

# The list of files that go into libunwind-arm:
SET(libunwind_arm_la_SOURCES_arm
    ${libunwind_la_SOURCES_arm_common}
    ${libunwind_la_SOURCES_arm_os}
    ${libunwind_la_SOURCES_generic}
    arm/Gapply_reg_state.c arm/Greg_states_iterate.c
    arm/Gcreate_addr_space.c arm/Gget_proc_info.c arm/Gget_save_loc.c
    arm/Gglobal.c arm/Ginit.c arm/Ginit_local.c arm/Ginit_remote.c
    arm/Gregs.c arm/Gresume.c arm/Gstep.c
    arm/Gex_tables.c arm/Gstash_frame.c arm/Gtrace.c
)

# The list of files that go both into libunwind and libunwind-x86:
SET(libunwind_la_SOURCES_x86_common
    ${libunwind_la_SOURCES_common}
    x86/is_fpreg.c x86/regname.c
)

# The list of files that go into libunwind:
SET(libunwind_la_SOURCES_x86
    ${libunwind_la_SOURCES_x86_common}
    ${libunwind_la_SOURCES_x86_os_local}
    ${libunwind_la_SOURCES_local}
    x86/Lapply_reg_state.c x86/Lreg_states_iterate.c
    x86/Lcreate_addr_space.c x86/Lget_save_loc.c x86/Lglobal.c
    x86/Linit.c x86/Linit_local.c x86/Linit_remote.c
    x86/Lget_proc_info.c x86/Lregs.c
    x86/Lresume.c x86/Lstep.c
)

# The list of files that go into libunwind-x86:
SET(libunwind_x86_la_SOURCES_x86
    ${libunwind_la_SOURCES_x86_common}
    ${libunwind_la_SOURCES_x86_os}
    ${libunwind_la_SOURCES_generic}
    x86/Gapply_reg_state.c x86/Greg_states_iterate.c
    x86/Gcreate_addr_space.c x86/Gget_save_loc.c x86/Gglobal.c
    x86/Ginit.c x86/Ginit_local.c x86/Ginit_remote.c
    x86/Gget_proc_info.c x86/Gregs.c
    x86/Gresume.c x86/Gstep.c
)

# The list of files that go both into libunwind and libunwind-x86_64:
SET(libunwind_la_SOURCES_x86_64_common
    ${libunwind_la_SOURCES_common}
    x86_64/is_fpreg.c x86_64/regname.c
)

# The list of files that go into libunwind:
SET(libunwind_la_SOURCES_x86_64
    ${libunwind_la_SOURCES_x86_64_common}
    ${libunwind_la_SOURCES_x86_64_os_local}
    ${libunwind_la_SOURCES_local}
    x86_64/setcontext.S
    x86_64/Lapply_reg_state.c x86_64/Lreg_states_iterate.c
    x86_64/Lcreate_addr_space.c x86_64/Lget_save_loc.c x86_64/Lglobal.c
    x86_64/Linit.c x86_64/Linit_local.c x86_64/Linit_remote.c
    x86_64/Lget_proc_info.c x86_64/Lregs.c x86_64/Lresume.c
    x86_64/Lstash_frame.c x86_64/Lstep.c x86_64/Ltrace.c x86_64/getcontext.S
)

# The list of files that go into libunwind-x86_64:
SET(libunwind_x86_64_la_SOURCES_x86_64
    ${libunwind_la_SOURCES_x86_64_common}
    ${libunwind_la_SOURCES_x86_64_os}
    ${libunwind_la_SOURCES_generic}
    x86_64/Gapply_reg_state.c x86_64/Greg_states_iterate.c
    x86_64/Gcreate_addr_space.c x86_64/Gget_save_loc.c x86_64/Gglobal.c
    x86_64/Ginit.c x86_64/Ginit_local.c x86_64/Ginit_remote.c
    x86_64/Gget_proc_info.c x86_64/Gregs.c x86_64/Gresume.c
    x86_64/Gstash_frame.c x86_64/Gstep.c x86_64/Gtrace.c
)

if(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_aarch64})
    SET(libunwind_remote_la_SOURCES             ${libunwind_aarch64_la_SOURCES_aarch64})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
    list(APPEND libunwind_setjmp_la_SOURCES     aarch64/siglongjmp.S)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_arm})
    SET(libunwind_remote_la_SOURCES             ${libunwind_arm_la_SOURCES_arm})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
    list(APPEND libunwind_setjmp_la_SOURCES     arm/siglongjmp.S)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_x86} ${libunwind_x86_la_SOURCES_os})
    SET(libunwind_remote_la_SOURCES             ${libunwind_x86_la_SOURCES_x86})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
    list(APPEND libunwind_setjmp_la_SOURCES     x86/longjmp.S x86/siglongjmp.S)
elseif(CLR_CMAKE_PLATFORM_ARCH_AMD64)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_x86_64})
    SET(libunwind_remote_la_SOURCES             ${libunwind_x86_64_la_SOURCES_x86_64})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
    list(APPEND libunwind_setjmp_la_SOURCES     x86_64/longjmp.S x86_64/siglongjmp.SA)
endif()

add_library(libunwind
  OBJECT
  ${libunwind_la_SOURCES}
  ${libunwind_remote_la_SOURCES}
  ${libunwind_dwarf_local_la_SOURCES}
  ${libunwind_dwarf_common_la_SOURCES}
  ${libunwind_dwarf_generic_la_SOURCES}
  ${libunwind_elf_la_SOURCES}
)

