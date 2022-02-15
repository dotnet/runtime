include_directories(${CMAKE_CURRENT_LIST_DIR}/libunwind/include/tdep)
include_directories(${CMAKE_CURRENT_LIST_DIR}/libunwind/include)
include_directories(${CMAKE_CURRENT_LIST_DIR}/libunwind/src)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/include/tdep)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/include)

set(libunwind_ptrace_la_SOURCES
    ptrace/_UPT_elf.c
    ptrace/_UPT_accessors.c ptrace/_UPT_access_fpreg.c
    ptrace/_UPT_access_mem.c ptrace/_UPT_access_reg.c
    ptrace/_UPT_create.c ptrace/_UPT_destroy.c
    ptrace/_UPT_find_proc_info.c ptrace/_UPT_get_dyn_info_list_addr.c
    ptrace/_UPT_put_unwind_info.c ptrace/_UPT_get_proc_name.c
    ptrace/_UPT_reg_offset.c ptrace/_UPT_resume.c
)

set(libunwind_coredump_la_SOURCES
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
set(libunwind_la_SOURCES_generic
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

set(libunwind_la_SOURCES_os_linux
    os-linux.c
)

set(libunwind_la_SOURCES_os_linux_local
# Nothing when we don't want to support CXX exceptions
)

set(libunwind_la_SOURCES_os_freebsd
    os-freebsd.c
)

set(libunwind_la_SOURCES_os_freebsd_local
# Nothing
)

set(libunwind_la_SOURCES_os_solaris
    os-solaris.c
)

set(libunwind_la_SOURCES_os_solaris_local
# Nothing
)

if(CLR_CMAKE_TARGET_LINUX)
    set(libunwind_la_SOURCES_os                 ${libunwind_la_SOURCES_os_linux})
    set(libunwind_la_SOURCES_os_local           ${libunwind_la_SOURCES_os_linux_local})
    set(libunwind_la_SOURCES_x86_os             x86/Gos-linux.c)
    set(libunwind_x86_la_SOURCES_os             x86/getcontext-linux.S)
    set(libunwind_la_SOURCES_x86_os_local       x86/Los-linux.c)
    set(libunwind_la_SOURCES_x86_64_os          x86_64/Gos-linux.c)
    set(libunwind_la_SOURCES_x86_64_os_local    x86_64/Los-linux.c)
    set(libunwind_la_SOURCES_arm_os             arm/Gos-linux.c)
    set(libunwind_la_SOURCES_arm_os_local       arm/Los-linux.c)
    list(APPEND libunwind_coredump_la_SOURCES   coredump/_UCD_access_reg_linux.c)
elseif(CLR_CMAKE_TARGET_FREEBSD)
    set(libunwind_la_SOURCES_os                 ${libunwind_la_SOURCES_os_freebsd})
    set(libunwind_la_SOURCES_os_local           ${libunwind_la_SOURCES_os_freebsd_local})
    set(libunwind_la_SOURCES_x86_os             x86/Gos-freebsd.c)
    set(libunwind_x86_la_SOURCES_os             x86/getcontext-freebsd.S)
    set(libunwind_la_SOURCES_x86_os_local       x86/Los-freebsd.c)
    set(libunwind_la_SOURCES_x86_64_os          x86_64/Gos-freebsd.c)
    set(libunwind_la_SOURCES_x86_64_os_local    x86_64/Los-freebsd.c)
    set(libunwind_la_SOURCES_arm_os             arm/Gos-freebsd.c)
    set(libunwind_la_SOURCES_arm_os_local       arm/Los-freebsd.c)
    list(APPEND libunwind_coredump_la_SOURCES   coredump/_UCD_access_reg_freebsd.c)
elseif(CLR_CMAKE_HOST_SUNOS)
    set(libunwind_la_SOURCES_os                 ${libunwind_la_SOURCES_os_solaris})
    set(libunwind_la_SOURCES_os_local           ${libunwind_la_SOURCES_os_solaris_local})
    set(libunwind_la_SOURCES_x86_64_os          x86_64/Gos-solaris.c)
    set(libunwind_la_SOURCES_x86_64_os_local    x86_64/Los-solaris.c)
endif()

# List of arch-independent files needed by both local-only and generic
# libraries:
set(libunwind_la_SOURCES_common
    ${libunwind_la_SOURCES_os}
    mi/init.c mi/flush_cache.c mi/mempool.c mi/strerror.c
)

set(libunwind_la_SOURCES_local_unwind
# Nothing when we don't want to support CXX exceptions
)

# List of arch-independent files needed by local-only library (libunwind):
set(libunwind_la_SOURCES_local_nounwind
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

set(libunwind_la_SOURCES_local
    ${libunwind_la_SOURCES_local_nounwind}
    ${libunwind_la_SOURCES_local_unwind}
)

set(libunwind_dwarf_common_la_SOURCES
    dwarf/global.c
)

set(libunwind_dwarf_local_la_SOURCES
    dwarf/Lexpr.c dwarf/Lfde.c dwarf/Lparser.c dwarf/Lpe.c
    dwarf/Lfind_proc_info-lsb.c
    dwarf/Lfind_unwind_table.c
)

set(libunwind_dwarf_generic_la_SOURCES
    dwarf/Gexpr.c dwarf/Gfde.c dwarf/Gparser.c dwarf/Gpe.c
    dwarf/Gfind_proc_info-lsb.c
    dwarf/Gfind_unwind_table.c
)

set(libunwind_elf32_la_SOURCES
    elf32.c
)

set(libunwind_elf64_la_SOURCES
    elf64.c
)
set(libunwind_elfxx_la_SOURCES
    elfxx.c
)

# The list of files that go into libunwind and libunwind-loongarch64:
set(libunwind_la_SOURCES_loongarch_common
    ${libunwind_la_SOURCES_common}
    loongarch64/is_fpreg.c
    loongarch64/regname.c
)

# The list of files that go into libunwind:
set(libunwind_la_SOURCES_loongarch
    ${libunwind_la_SOURCES_loongarch_common}
    ${libunwind_la_SOURCES_local}
    loongarch64/Lget_proc_info.c  loongarch64/Linit.c  loongarch64/Lis_signal_frame.c
    loongarch64/Lstep.c
    loongarch64/getcontext.S
    loongarch64/Lget_save_loc.c
    loongarch64/Linit_local.c   loongarch64/Lregs.c
    loongarch64/Lcreate_addr_space.c  loongarch64/Lglobal.c  loongarch64/Linit_remote.c  loongarch64/Lresume.c
)

set(libunwind_loongarch_la_SOURCES_loongarch
    ${libunwind_la_SOURCES_loongarch_common}
    ${libunwind_la_SOURCES_generic}
	loongarch64/Gcreate_addr_space.c loongarch64/Gget_proc_info.c loongarch64/Gget_save_loc.c
	loongarch64/Gglobal.c loongarch64/Ginit.c loongarch64/Ginit_local.c loongarch64/Ginit_remote.c
	loongarch64/Gis_signal_frame.c loongarch64/Gregs.c loongarch64/Gresume.c loongarch64/Gstep.c
)

# The list of files that go into libunwind and libunwind-aarch64:
set(libunwind_la_SOURCES_aarch64_common
    ${libunwind_la_SOURCES_common}
    aarch64/is_fpreg.c
    aarch64/regname.c
)

# The list of files that go into libunwind:
set(libunwind_la_SOURCES_aarch64
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

set(libunwind_aarch64_la_SOURCES_aarch64
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
set(libunwind_la_SOURCES_arm_common
    ${libunwind_la_SOURCES_common}
    arm/is_fpreg.c arm/regname.c
)

# The list of files that go into libunwind:
set(libunwind_la_SOURCES_arm
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
set(libunwind_arm_la_SOURCES_arm
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
set(libunwind_la_SOURCES_x86_common
    ${libunwind_la_SOURCES_common}
    x86/is_fpreg.c x86/regname.c
)

# The list of files that go into libunwind:
set(libunwind_la_SOURCES_x86
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
set(libunwind_x86_la_SOURCES_x86
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
set(libunwind_la_SOURCES_x86_64_common
    ${libunwind_la_SOURCES_common}
    x86_64/is_fpreg.c x86_64/regname.c
)

# The list of files that go into libunwind:
set(libunwind_la_SOURCES_x86_64
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
set(libunwind_x86_64_la_SOURCES_x86_64
    ${libunwind_la_SOURCES_x86_64_common}
    ${libunwind_la_SOURCES_x86_64_os}
    ${libunwind_la_SOURCES_generic}
    x86_64/Gapply_reg_state.c x86_64/Greg_states_iterate.c
    x86_64/Gcreate_addr_space.c x86_64/Gget_save_loc.c x86_64/Gglobal.c
    x86_64/Ginit.c x86_64/Ginit_local.c x86_64/Ginit_remote.c
    x86_64/Gget_proc_info.c x86_64/Gregs.c x86_64/Gresume.c
    x86_64/Gstash_frame.c x86_64/Gstep.c x86_64/Gtrace.c
)

# The list of files that go both into libunwind and libunwind-s390x:
set(libunwind_la_SOURCES_s390x_common
    ${libunwind_la_SOURCES_common}
    s390x/is_fpreg.c s390x/regname.c
)

# The list of files that go into libunwind:
set(libunwind_la_SOURCES_s390x
    ${libunwind_la_SOURCES_s390x_common}
    ${libunwind_la_SOURCES_local}
    s390x/setcontext.S s390x/getcontext.S
    s390x/Lapply_reg_state.c s390x/Lreg_states_iterate.c
    s390x/Lcreate_addr_space.c s390x/Lget_save_loc.c s390x/Lglobal.c
    s390x/Linit.c s390x/Linit_local.c s390x/Linit_remote.c
    s390x/Lget_proc_info.c s390x/Lregs.c s390x/Lresume.c
    s390x/Lis_signal_frame.c s390x/Lstep.c
)

# The list of files that go into libunwind-s390x:
set(libunwind_s390x_la_SOURCES_s390x
    ${libunwind_la_SOURCES_s390x_common}
    ${libunwind_la_SOURCES_generic}
    s390x/Gapply_reg_state.c s390x/Greg_states_iterate.c
    s390x/Gcreate_addr_space.c s390x/Gget_save_loc.c s390x/Gglobal.c
    s390x/Ginit.c s390x/Ginit_local.c s390x/Ginit_remote.c
    s390x/Gget_proc_info.c s390x/Gregs.c s390x/Gresume.c
    s390x/Gis_signal_frame.c s390x/Gstep.c
)

if(CLR_CMAKE_HOST_UNIX)
    if(CLR_CMAKE_HOST_ARCH_ARM64)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_aarch64})
        set(libunwind_remote_la_SOURCES             ${libunwind_aarch64_la_SOURCES_aarch64})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     aarch64/siglongjmp.S)
    elseif(CLR_CMAKE_HOST_ARCH_ARM)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_arm})
        set(libunwind_remote_la_SOURCES             ${libunwind_arm_la_SOURCES_arm})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     arm/siglongjmp.S)
    elseif(CLR_CMAKE_HOST_ARCH_ARMV6)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_arm})
        set(libunwind_remote_la_SOURCES             ${libunwind_arm_la_SOURCES_arm})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     arm/siglongjmp.S)
    elseif(CLR_CMAKE_HOST_ARCH_I386)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_x86} ${libunwind_x86_la_SOURCES_os})
        set(libunwind_remote_la_SOURCES             ${libunwind_x86_la_SOURCES_x86})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     x86/longjmp.S x86/siglongjmp.S)
    elseif(CLR_CMAKE_HOST_ARCH_AMD64)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_x86_64})
        set(libunwind_remote_la_SOURCES             ${libunwind_x86_64_la_SOURCES_x86_64})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     x86_64/longjmp.S x86_64/siglongjmp.SA)
    elseif(CLR_CMAKE_HOST_ARCH_S390X)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_s390x})
        set(libunwind_remote_la_SOURCES             ${libunwind_s390x_la_SOURCES_s390x})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
    elseif(CLR_CMAKE_HOST_ARCH_LOONGARCH64)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_loongarch})
        set(libunwind_remote_la_SOURCES             ${libunwind_loongarch_la_SOURCES_loongarch})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     loongarch64/siglongjmp.S)
    endif()

    if(CLR_CMAKE_HOST_OSX)
        set(LIBUNWIND_SOURCES_BASE
          ${libunwind_remote_la_SOURCES}
          ${libunwind_dwarf_common_la_SOURCES}
          ${libunwind_dwarf_generic_la_SOURCES}
        )
    else()
        set(LIBUNWIND_SOURCES_BASE
          ${libunwind_la_SOURCES}
          ${libunwind_remote_la_SOURCES}
          ${libunwind_dwarf_local_la_SOURCES}
          ${libunwind_dwarf_common_la_SOURCES}
          ${libunwind_dwarf_generic_la_SOURCES}
          ${libunwind_elf_la_SOURCES}
        )
    endif(CLR_CMAKE_HOST_OSX)

else(CLR_CMAKE_HOST_UNIX)
    if(CLR_CMAKE_TARGET_ARCH_ARM64)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_aarch64})
        set(libunwind_remote_la_SOURCES             ${libunwind_aarch64_la_SOURCES_aarch64})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     aarch64/siglongjmp.S)
    elseif(CLR_CMAKE_TARGET_ARCH_ARM)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_arm})
        set(libunwind_remote_la_SOURCES             ${libunwind_arm_la_SOURCES_arm})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     arm/siglongjmp.S)
    elseif(CLR_CMAKE_TARGET_ARCH_ARMV6)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_arm})
        set(libunwind_remote_la_SOURCES             ${libunwind_arm_la_SOURCES_arm})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     arm/siglongjmp.S)
    elseif(CLR_CMAKE_TARGET_ARCH_I386)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_x86} ${libunwind_x86_la_SOURCES_os})
        set(libunwind_remote_la_SOURCES             ${libunwind_x86_la_SOURCES_x86})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     x86/longjmp.S x86/siglongjmp.S)
    elseif(CLR_CMAKE_TARGET_ARCH_AMD64)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_x86_64})
        set(libunwind_remote_la_SOURCES             ${libunwind_x86_64_la_SOURCES_x86_64})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
        list(APPEND libunwind_setjmp_la_SOURCES     x86_64/longjmp.S x86_64/siglongjmp.SA)
    elseif(CLR_CMAKE_TARGET_ARCH_S390X)
        set(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_s390x})
        set(libunwind_remote_la_SOURCES             ${libunwind_s390x_la_SOURCES_s390x})
        set(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
    endif()

    set_source_files_properties(${CLR_DIR}/pal/src/exception/remote-unwind.cpp PROPERTIES COMPILE_FLAGS /TP INCLUDE_DIRECTORIES ${CLR_DIR}/inc)

    set(LIBUNWIND_SOURCES_BASE
      win/pal-single-threaded.c
      # ${libunwind_la_SOURCES}  Local...
      ${libunwind_remote_la_SOURCES}
      # Commented out above for LOCAL + REMOTE runtime build
      mi/Gget_accessors.c
      # ${libunwind_dwarf_local_la_SOURCES}
      ${libunwind_dwarf_common_la_SOURCES}
      ${libunwind_dwarf_generic_la_SOURCES}
      ${libunwind_elf_la_SOURCES}
    )
endif(CLR_CMAKE_HOST_UNIX)

addprefix(LIBUNWIND_SOURCES "${CMAKE_CURRENT_LIST_DIR}/libunwind/src" "${LIBUNWIND_SOURCES_BASE}")
