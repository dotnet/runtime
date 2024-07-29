set(CMAKE_INCLUDE_CURRENT_DIR ON)

# other source files
include_directories(../include/tdep)
include_directories(../include)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/../include/tdep)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/../include)

if(TARGET_ARM)
    # Ensure that the remote and local unwind code can reside in the same binary without name clashing
    add_definitions("-Darm_search_unwind_table=UNW_OBJ(arm_search_unwind_table)")
    # We compile code with -std=c99 and the asm keyword is not recognized as it is a gnu extension
    add_definitions(-Dasm=__asm__)
    # The arm sources include ex_tables.h from include/tdep-arm without going through a redirection
    # in include/tdep like it works for similar files on other architectures. So we need to add
    # the include/tdep-arm to include directories
    include_directories(../include/tdep-arm)
elseif(TARGET_AARCH64)
    # We compile code with -std=c99 and the asm keyword is not recognized as it is a gnu extension
    add_definitions(-Dasm=__asm__)
endif()

SET(libunwind_ptrace_la_SOURCES
    mi/init.c
    ptrace/_UPT_elf.c
    ptrace/_UPT_accessors.c ptrace/_UPT_access_fpreg.c
    ptrace/_UPT_access_mem.c ptrace/_UPT_access_reg.c
    ptrace/_UPT_create.c ptrace/_UPT_destroy.c
    ptrace/_UPT_find_proc_info.c ptrace/_UPT_get_dyn_info_list_addr.c
    ptrace/_UPT_put_unwind_info.c ptrace/_UPT_get_proc_name.c
    ptrace/_UPT_reg_offset.c ptrace/_UPT_resume.c
    ptrace/_UPT_get_elf_filename.c
)

SET(libunwind_coredump_la_SOURCES
    coredump/_UCD_accessors.c
    coredump/_UCD_create.c
    coredump/_UCD_destroy.c
    coredump/_UCD_access_mem.c
    coredump/_UCD_elf_map_image.c
    coredump/_UCD_find_proc_info.c
    coredump/_UCD_get_proc_name.c
    coredump/_UCD_get_elf_filename.c

    mi/init.c
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
    mi/Gset_iterate_phdr_function.c
    mi/Gget_elf_filename.c
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

SET(libunwind_la_SOURCES_os_solaris
    os-solaris.c
)

SET(libunwind_la_SOURCES_os_solaris_local
# Nothing
)

if(UNW_CMAKE_TARGET_LINUX)
    SET(libunwind_la_SOURCES_os                 ${libunwind_la_SOURCES_os_linux})
    SET(libunwind_la_SOURCES_os_local           ${libunwind_la_SOURCES_os_linux_local})
    SET(libunwind_la_SOURCES_x86_os             x86/Gos-linux.c)
    SET(libunwind_x86_la_SOURCES_os             x86/getcontext-linux.S)
    SET(libunwind_la_SOURCES_x86_os_local       x86/Los-linux.c)
    SET(libunwind_la_SOURCES_x86_64_os          x86_64/Gos-linux.c)
    SET(libunwind_la_SOURCES_x86_64_os_local    x86_64/Los-linux.c)
    SET(libunwind_la_SOURCES_arm_os             arm/Gos-linux.c)
    SET(libunwind_la_SOURCES_arm_os_local       arm/Los-linux.c)
    list(APPEND libunwind_coredump_la_SOURCES   coredump/_UCD_access_reg_linux.c
                                                coredump/_UCD_get_threadinfo_prstatus.c
                                                coredump/_UCD_get_mapinfo_linux.c)
elseif(UNW_CMAKE_TARGET_FREEBSD)
    SET(libunwind_la_SOURCES_os                 ${libunwind_la_SOURCES_os_freebsd})
    SET(libunwind_la_SOURCES_os_local           ${libunwind_la_SOURCES_os_freebsd_local})
    SET(libunwind_la_SOURCES_x86_os             x86/Gos-freebsd.c)
    SET(libunwind_x86_la_SOURCES_os             x86/getcontext-freebsd.S)
    SET(libunwind_la_SOURCES_x86_os_local       x86/Los-freebsd.c)
    SET(libunwind_la_SOURCES_x86_64_os          x86_64/Gos-freebsd.c)
    SET(libunwind_la_SOURCES_x86_64_os_local    x86_64/Los-freebsd.c)
    SET(libunwind_la_SOURCES_arm_os             arm/Gos-freebsd.c)
    SET(libunwind_la_SOURCES_arm_os_local       arm/Los-freebsd.c)
    list(APPEND libunwind_coredump_la_SOURCES   coredump/_UCD_access_reg_freebsd.c
                                                coredump/_UCD_get_threadinfo_prstatus.c
                                                coredump/_UCD_get_mapinfo_generic.c)
elseif(UNW_CMAKE_HOST_SUNOS)
    SET(libunwind_la_SOURCES_os                 ${libunwind_la_SOURCES_os_solaris})
    SET(libunwind_la_SOURCES_os_local           ${libunwind_la_SOURCES_os_solaris_local})
    SET(libunwind_la_SOURCES_x86_64_os          x86_64/Gos-solaris.c)
    SET(libunwind_la_SOURCES_x86_64_os_local    x86_64/Los-solaris.c)
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
    mi/Lset_iterate_phdr_function.c
    mi/Lget_elf_filename.c
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
    dwarf/Lget_proc_info_in_range.c
)

SET(libunwind_dwarf_generic_la_SOURCES
    dwarf/Gexpr.c dwarf/Gfde.c dwarf/Gparser.c dwarf/Gpe.c
    dwarf/Gfind_proc_info-lsb.c
    dwarf/Gfind_unwind_table.c
    dwarf/Gget_proc_info_in_range.c
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

# The list of files that go both into libunwind and libunwind-s390x:
SET(libunwind_la_SOURCES_s390x_common
    ${libunwind_la_SOURCES_common}
    s390x/is_fpreg.c s390x/regname.c
)

# The list of files that go into libunwind:
SET(libunwind_la_SOURCES_s390x
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
SET(libunwind_s390x_la_SOURCES_s390x
    ${libunwind_la_SOURCES_s390x_common}
    ${libunwind_la_SOURCES_generic}
    s390x/Gapply_reg_state.c s390x/Greg_states_iterate.c
    s390x/Gcreate_addr_space.c s390x/Gget_save_loc.c s390x/Gglobal.c
    s390x/Ginit.c s390x/Ginit_local.c s390x/Ginit_remote.c
    s390x/Gget_proc_info.c s390x/Gregs.c s390x/Gresume.c
    s390x/Gis_signal_frame.c s390x/Gstep.c
)

# The list of files that go into libunwind and libunwind-loongarch64:
SET(libunwind_la_SOURCES_loongarch_common
    ${libunwind_la_SOURCES_common}
    loongarch64/is_fpreg.c
    loongarch64/regname.c
)

# The list of files that go into libunwind:
SET(libunwind_la_SOURCES_loongarch64
    ${libunwind_la_SOURCES_loongarch_common}
    ${libunwind_la_SOURCES_local}
    loongarch64/Lget_proc_info.c  loongarch64/Linit.c  loongarch64/Lis_signal_frame.c
    loongarch64/Lstep.c
    loongarch64/getcontext.S
    loongarch64/Lget_save_loc.c
    loongarch64/Linit_local.c   loongarch64/Lregs.c
    loongarch64/Lcreate_addr_space.c  loongarch64/Lglobal.c  loongarch64/Linit_remote.c  loongarch64/Lresume.c
)

SET(libunwind_loongarch64_la_SOURCES_loongarch
    ${libunwind_la_SOURCES_loongarch_common}
    ${libunwind_la_SOURCES_generic}
	loongarch64/Gcreate_addr_space.c loongarch64/Gget_proc_info.c loongarch64/Gget_save_loc.c
	loongarch64/Gglobal.c loongarch64/Ginit.c loongarch64/Ginit_local.c loongarch64/Ginit_remote.c
	loongarch64/Gis_signal_frame.c loongarch64/Gregs.c loongarch64/Gresume.c loongarch64/Gstep.c
)

if(TARGET_AARCH64)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_aarch64})
    SET(libunwind_remote_la_SOURCES             ${libunwind_aarch64_la_SOURCES_aarch64})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
    list(APPEND libunwind_setjmp_la_SOURCES     aarch64/siglongjmp.S)
elseif(TARGET_ARM)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_arm})
    SET(libunwind_remote_la_SOURCES             ${libunwind_arm_la_SOURCES_arm})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf32_la_SOURCES})
    list(APPEND libunwind_setjmp_la_SOURCES     arm/siglongjmp.S)
elseif(TARGET_AMD64)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_x86_64})
    SET(libunwind_remote_la_SOURCES             ${libunwind_x86_64_la_SOURCES_x86_64})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
    list(APPEND libunwind_setjmp_la_SOURCES     x86_64/longjmp.S x86_64/siglongjmp.SA)
elseif(TARGET_S390X)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_s390x})
    SET(libunwind_remote_la_SOURCES             ${libunwind_s390x_la_SOURCES_s390x})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
elseif(TARGET_LOONGARCH64)
    SET(libunwind_la_SOURCES                    ${libunwind_la_SOURCES_loongarch64})
    SET(libunwind_remote_la_SOURCES             ${libunwind_loongarch64_la_SOURCES_loongarch})
    SET(libunwind_elf_la_SOURCES                ${libunwind_elf64_la_SOURCES})
endif()

add_library(libunwind
  OBJECT
  remote/win/missing-functions.c
  # ${libunwind_la_SOURCES}  Local...
  ${libunwind_remote_la_SOURCES}
    # Commented out above for LOCAL + REMOTE runtime build
    mi/Gget_accessors.c
  # ${libunwind_dwarf_local_la_SOURCES}
  ${libunwind_dwarf_common_la_SOURCES}
  ${libunwind_dwarf_generic_la_SOURCES}
  ${libunwind_elf_la_SOURCES}
)
