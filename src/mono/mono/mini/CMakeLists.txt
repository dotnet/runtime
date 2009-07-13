
# Last synched with Makefile.am at r134597

cmake_policy(SET CMP0010 NEW)

# Helper functions

function(add_cs_target target sources args depends)
  separate_arguments(sources)
  separate_arguments(args)
  separate_arguments(depends)
  add_custom_command(
	OUTPUT ${target}
	COMMAND ${MCS} -out:${target} ${args} ${sources}
	DEPENDS ${sources} ${depends}
	)
  add_custom_target (${target}-target DEPENDS ${target})
endfunction()

function(add_cs_dll target sources args depends)
  add_cs_target(${target} ${sources} "-target:library ${args}" "${depends}")
endfunction()

function(add_cs_exe target sources args depends)
  add_cs_target(${target} ${sources} "-target:exe ${args}" "${depends}")
endfunction()

function(add_il_target target sources args)
  separate_arguments(sources)
  separate_arguments(args)
  add_custom_command(
	OUTPUT ${target}
	COMMAND ${ILASM} -output=${target} ${args} ${sources}
	DEPENDS ${sources}
	)
  add_custom_target (${target}-target DEPENDS ${target})
endfunction()




set(count 100000)
set(mtest for_loop)
set(monodir ${top_builddir})

set(CLASS ${mcs_topdir}/class/lib/net_2_0)

set(RUNTIME MONO_PATH=${CLASS} ${top_builddir}/runtime/mono-wrapper)
set(RUNTIME_AOTCHECK MONO_PATH=${CLASS}:. ${top_builddir}/runtime/mono-wrapper)

set(MCS ${RUNTIME} ${CLASS}/gmcs.exe -unsafe -nowarn:0162)
set(ILASM ${RUNTIME} ${CLASS}/ilasm.exe)

set(x86_sources
	mini-x86.c	
	mini-x86.h	
	exceptions-x86.c
	tramp-x86.c)

set(amd64_sources
	mini-amd64.c	
	mini-amd64.h	
	exceptions-amd64.c
	tramp-amd64.c)

set(ppc_sources
	mini-ppc.c	
	mini-ppc.h	
	exceptions-ppc.c
	tramp-ppc.c)

set(arm_sources
	mini-arm.c	
	mini-arm.h	
	exceptions-arm.c
	tramp-arm.c)

set(mips_sources
	mini-mips.c	
	mini-mips.h	
	exceptions-mips.c
	tramp-mips.c)

set(sparc_sources
	mini-sparc.c	
	mini-sparc.h	
	exceptions-sparc.c
	tramp-sparc.c)

set(s390_sources
	mini-s390.c	
	mini-s390.h	
	exceptions-s390.c
	tramp-s390.c)

set(s390x_sources
	mini-s390x.c	
	mini-s390x.h	
	exceptions-s390x.c
	tramp-s390x.c)

set(ia64_sources
	mini-ia64.c	
	mini-ia64.h	
	exceptions-ia64.c
	tramp-ia64.c)

set(alpha_sources
	mini-alpha.c	
	mini-alpha.h	
	exceptions-alpha.c
	tramp-alpha.c)

set(hppa_sources
	mini-hppa.c	
	mini-hppa.h	
	exceptions-hppa.c
	tramp-hppa.c)

set(darwin_sources
	mini-darwin.c)

set(windows_sources
	mini-windows.c)

set(posix_sources
	mini-posix.c)

set(common_sources
	mini.c		
	ir-emit.h	
	method-to-ir.c	
	decompose.c	
	mini.h		
	version.h	
	optflags-def.h	
	jit-icalls.h 	
	jit-icalls.c 	
	trace.c		
	trace.h		
	patch-info.h	
	mini-ops.h	
	mini-arch.h	
	dominators.c	
	cfold.c		
	regalloc.c	
	regalloc.h	
	helpers.c	
	liveness.c	
	ssa.c		
	abcremoval.c	
	abcremoval.h	
	ssapre.c	
	ssapre.h	
	local-propagation.c
	driver.c	
	debug-mini.c	
	debug-mini.h	
	linear-scan.c	
	aot-compiler.c	
	aot-runtime.c	
	graph.c		
	mini-codegen.c	
	mini-exceptions.c
	mini-trampolines.c  
	declsec.c	
	declsec.h	
	wapihandles.c	
	branch-opts.c	
	mini-generic-sharing.c
	regalloc2.c	
	simd-methods.h	
	tasklets.c	
	tasklets.h	
	simd-intrinsics.c
	unwind.h	
	unwind.c	
	image-writer.h
	image-writer.c
	dwarfwriter.h
	dwarfwriter.c
	mini-gc.h	
	mini-gc.c)

set(test_sources 		
	basic-calls.cs 	
	basic-long.cs 	
	bench.cs 	
	objects.cs 	
	arrays.cs	
	basic-float.cs	
	basic-math.cs	
	basic.cs	
	exceptions.cs	
	devirtualization.cs
	iltests.il.in	
	test.cs		
	generics.cs	
	generics-variant-types.il
	basic-simd.cs)

if(MONO_DEBUGGER_SUPPORTED)
if(AMD64)
set(mono_debugger_arch_sources mdb-debug-info64.s)
elseif(X86)
set(mono_debugger_arch_sources mdb-debug-info32.s)
endif()
enable_language(ASM-ATT)
set(mono_debugger_sources debug-debugger.c debug-debugger.h ${mono_debugger_arch_sources})

set(ASM-ATT_FLAGS)
else(MONO_DEBUGGER_SUPPORTED)
set(mono_debugger_sources)
endif(MONO_DEBUGGER_SUPPORTED)

set(regtests basic.exe basic-float.exe basic-long.exe basic-calls.exe objects.exe arrays.exe basic-math.exe exceptions.exe iltests.exe devirtualization.exe generics.exe basic-simd.exe)

if(X86)
set(arch_sources ${x86_sources} ${mono_debugger_sources})
set(arch_built cpu-x86.h)
set(arch_define __i386__)
endif()

if(AMD64)
set(arch_sources ${amd64_sources} ${mono_debugger_sources})
set(arch_built cpu-amd64.h)
set(arch_define __x86_64__)
endif()

if(POWERPC)
set(arch_sources ${ppc_sources})
set(arch_built cpu-ppc.h)
set(arch_define __ppc__)
endif()

if(POWERPC64)
set(arch_sources ${ppc_sources})
set(arch_built cpu-ppc64.h)
set(arch_define __ppc64__)
endif()

if(MIPS)
set(arch_sources ${mips_sources})
set(arch_built cpu-mips.h)
set(arch_define __mips__)
endif()

if(ARM)
# pick up arm_dpimacros.h and arm_fpamacros.h
set(ARCH_CFLAGS -I../arch/arm)
set(arch_sources ${arm_sources})
set(arch_built cpu-arm.h)
set(arch_define __arm__)
endif()

if(SPARC)
set(arch_sources ${sparc_sources})
set(arch_built cpu-sparc.h)
set(arch_define __sparc__)
endif()

if(SPARC64)
set(arch_sources ${sparc_sources})
set(arch_built cpu-sparc.h)
set(arch_define __sparc__)
endif()

if(S390)
set(arch_sources ${s390_sources})
set(arch_built cpu-s390.h)
set(arch_define __s390__)
endif()

if(S390x)
set(arch_sources ${s390x_sources})
set(arch_built cpu-s390x.h)
set(arch_define __s390__)
endif()

if(IA64)
set(arch_sources ${ia64_sources})
set(arch_built cpu-ia64.h)
set(arch_define __ia64__)
endif()

if(ALPHA)
set(arch_sources ${alpha_sources} ${mono_debugger_sources})
set(arch_built cpu-alpha.h)
set(arch_define __alpha__)
endif()

if(HPPA)
# Only support 32-bit targets for now
set(arch_sources ${hppa_sources})
set(arch_built cpu-hppa.h)
set(arch_define __hppa__)
endif()

if(PLATFORM_WIN32)
set(os_sources ${windows_sources})
endif()

if(PLATFORM_SIGPOSIX)
set(os_sources ${posix_sources})
endif()

if(PLATFORM_DARWIN)
set(os_sources ${darwin_sources} ${posix_sources})
endif()

#### we don't always use the perl impl because it's an additional
#### build dependency for the poor windows users
#### ${arch_define} is the preprocessor symbol that enables all the opcodes
#### for the specific platform in mini-ops.h
###if CROSS_COMPILING
###GENMDESC_PRG=perl ${srcdir)/genmdesc.pl ${arch_define} ${srcdir)
###else !CROSS_COMPILING
set(GENMDESC_PRG ${CMAKE_CURRENT_BINARY_DIR}/genmdesc)
###endif !CROSS_COMPILING

function(add_genmdesc_target target source define)
  add_custom_command(
	OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${target}
	COMMAND ${GENMDESC_PRG} ${CMAKE_CURRENT_BINARY_DIR}/${target} ${define} ${source}
	DEPENDS genmdesc ${source}
	)
endfunction()

foreach(arch x86 amd64 sparc ia64 alpha hppa mips)
  add_genmdesc_target(cpu-${arch}.h cpu-${arch}.md ${arch}_desc)
endforeach()

add_genmdesc_target(cpu-ppc.h cpu-ppc.md ppcg4)
add_genmdesc_target(cpu-ppc64.h cpu-ppc64.md ppc64_cpu_desc)
add_genmdesc_target(cpu-arm.h cpu-arm.md arm_cpu_desc)
add_genmdesc_target(cpu-s390.h cpu-s390.md s390_cpu_desc)
add_genmdesc_target(cpu-s390x.h cpu-s390x.md s390x_cpu_desc)

include_directories(../..)
include_directories(${GLIB2_INCLUDE_DIRS})
include_directories(${LIBGC_INCLUDE_DIRS})
# FIXME:
link_directories(../../libgc/.libs)
set(CMAKE_C_FLAGS "${CFLAGS} ${LIBGC_CFLAGS} ${CPPFLAGS}")

# genmdesc
add_executable(genmdesc genmdesc.c helpers.c)
target_link_libraries(genmdesc monoutils-static monoruntime-static ${GLIB2_LIBRARIES})

# libmono

set(libmono_la_SOURCES ${common_sources} ${llvm_sources} ${arch_sources} ${os_sources})

# FIXME: cmake doesn't seem to recognize the ${arch_built} dependency
add_library(libmono-static STATIC ${libmono_la_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/${arch_built})
target_link_libraries(libmono-static monoruntime-static monoutils-static monogc-static wapi-static ${GLIB2_LIBRARIES} ${LIBS})
set_target_properties(libmono-static PROPERTIES OUTPUT_NAME "mono-static")

# Since cmake has no support for convenience libraries, we have to link the
# final libmono.a ourselves, similarly to how libtool does it
add_custom_target(libmono.a
  COMMAND rm -rf .libs/tmp libmono.a
  COMMAND mkdir -p .libs/tmp/{1,2,3,4,5}
  COMMAND cd .libs/tmp/1 && ar x ../../../../metadata/libmonoruntime-static.a
  COMMAND cd .libs/tmp/2 && ar x ../../../../utils/libmonoutils-static.a
  COMMAND cd .libs/tmp/3 && ar x ../../../../io-layer/libwapi-static.a
  COMMAND cd .libs/tmp/3 && ar x ../../../../mini/libmono-static.a
  COMMAND cd .libs/tmp/4 && ar x ../../../../../libgc/libmonogc-static.a
  COMMAND ar r libmono.a `find .libs/tmp/ -name '*.o'`
)

# FIXME: cmake has no support for convenience libraries, so we would end up 
# creating a lot of shared libraries linking to each other
#add_library(libmono SHARED ${libmono_la_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/${arch_built})
#set_target_properties(libmono PROPERTIES OUTPUT_NAME "mono")
#target_link_libraries(libmono monoruntime monoutils monogc wapi ${GLIB2_LIBRARIES} ${LIBS})

# version.h

# Its a pain to try to pass a complex shell script to add_custom_command (), so
# write it to disk instead
# " needs to be escaped as \"
# \ needs to be escaped as \\
set(top_srcdir ../../)
file(WRITE create-version.h.sh "
	if test -d ${top_srcdir}/.git/svn; then svn_info=\"echo \\\"`git log --no-color --first-parent --pretty=format:%b|grep -m1 git-svn-id|sed -e 's,git-svn-id: \\(.*\\)@\\(.*\\) .*,URL: \\1 Revision: \\2,'`\"; fi;
	if test -d ${srcdir}/.svn; then svn_info='svn info'; fi;
	if test -n \"$svn_info\"; then
		(cd ${top_srcdir};
			LANG=C; export LANG;
			branch=`$svn_info | grep URL | sed -e 's,.*source/\\(.*\\)/mono.*,/\\1/mono,'`;
			version=`$svn_info | grep Revision | sed 's/.*: //'`;
			echo \"#define FULL_VERSION \\\"$branch r$version\\\"\";
		);
	else
		echo \"#define FULL_VERSION \\\"tarball\\\"\";
	fi > version.h
")

# FIXME: dependencies ?
add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/version.h
  COMMAND chmod a+x ./create-version.h.sh
  COMMAND ./create-version.h.sh
  VERBATIM
)

# buildver.h

# We build this after libmono was built so it contains the date when the final
# link was done
add_custom_command(
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/buildver.h
  PRE_LINK
  COMMAND sh -c "X='\"'; echo \"const char *build_date = $X`date`$X;\" > ${CMAKE_CURRENT_BINARY_DIR}/buildver.h"
  DEPENDS libmono-static
  VERBATIM
)
set_source_files_properties(main.c PROPERTIES OBJECT_DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/buildver.h)
set_source_files_properties(main.c PROPERTIES OBJECT_DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/buildver.h)

# mono

add_executable(mono main.c ${CMAKE_CURRENT_BINARY_DIR}/buildver.h)
add_dependencies(mono libmono-static)
target_link_libraries(mono libmono-static ${GLIB2_LIBRARIES})

install(
  TARGETS mono
  RUNTIME DESTINATION bin)

# FIXME: Can't specify a dependency on libmono.a, and we don't want to build
# libmono.a until needed
install(
  FILES libmono.a DESTINATION lib)

# Test file targets

foreach(test ${test_sources})
  if (${test} MATCHES ".*\\.cs")
	string(REPLACE ".cs" ".exe" exe_name ${test})
	add_cs_exe(${exe_name} ${test} "-r:TestDriver.dll -r:generics-variant-types.dll -r:Mono.Simd.dll" "TestDriver.dll generics-variant-types.dll")
  endif()
endforeach()

set(il_source "iltests.il")
set(exe_name "iltests.exe")
add_custom_command(
  OUTPUT ${exe_name}
  COMMAND ${ILASM} -output=${exe_name} ${il_source}
  DEPENDS ${il_source}
  )
add_custom_target(${exe_name}-exe DEPENDS ${exe_name})

add_custom_command(
  OUTPUT "iltests.il"
  COMMAND echo // DO NOT EDIT: This file has been generated from iltests.il.in > iltests.il
  COMMAND cpp -Darch=${arch_define} < iltests.il.in | sed "s/^#.*//" >> iltests.il
  DEPENDS iltests.il.in
  VERBATIM
)

add_cs_dll("TestDriver.dll" "TestDriver.cs" "-r:System.dll -r:System.dll" "")

add_il_target("generics-variant-types.dll" "generics-variant-types.il" "-dll")
  
# Testing targets

add_custom_target(rcheck
  COMMAND ${RUNTIME} --regression ${regtests}
  DEPENDS mono ${regtests}
)

# FIXME: make runs rcheck, but then says: 'No rule to make target `rcheck', needed by `mono/mini/CMakeFiles/check'
#add_custom_target(check
#  DEPENDS rcheck)
add_custom_target(check
  COMMAND ${RUNTIME} --regression ${regtests}
  DEPENDS mono ${regtests}
)

add_custom_target(testi
  COMMAND ${RUNTIME} -v -v --ncompile 1 --compile Test:${mtest} test.exe
  DEPENDS mono test.exe
)

# ensure the tests are actually correct
add_custom_target(checktests
  COMMAND for i in ${regtests}\; do ${RUNTIME} $$i\; done
  DEPENDS ${regtests}
)

add_custom_target(aotcheck
  COMMAND rm -f *.exe.so
  COMMAND ${RUNTIME} --aot ${regtests} || exit 1
  COMMAND for i in ${regtests}\; do ${RUNTIME_AOTCHECK} --regression $$i || exit 1\; done
  COMMAND rm -f *.exe.so
  DEPENDS mono ${regtests}
)

# This currently only works on amd64/arm
add_custom_target(fullaotcheck
  COMMAND	rm -rf fullaot-tmp
  COMMAND	mkdir fullaot-tmp
  COMMAND	cp ${CLASS}/mscorlib.dll ${CLASS}/Mono.Simd.dll ${regtests} generics-variant-types.dll TestDriver.dll fullaot-tmp/
  COMMAND	cp ${regtests} fullaot-tmp/
  COMMAND	MONO_PATH=fullaot-tmp ${top_builddir}/runtime/mono-wrapper --aot=full fullaot-tmp/* || exit 1
  COMMAND   for i in ${regtests}\; do echo $$i\; MONO_PATH=fullaot-tmp ${top_builddir}/runtime/mono-wrapper --full-aot fullaot-tmp/$$i --exclude '!FULLAOT' || exit 1\; done
  DEPENDS mono ${regtests}
)

add_custom_target(bench
  COMMAND time env ${RUNTIME} --ncompile ${count} --compile Test:${mtest} test.exe
  DEPENDS mono test.exe
)

add_custom_target(stat1
  COMMAND ${RUNTIME} --verbose --statfile stats.pl --regression bench.exe
  COMMAND perl viewstat.pl stats.pl
  DEPENDS mono bench.exe
)

add_custom_target(stat2
  COMMAND ${RUNTIME} --verbose --statfile stats.pl --regression basic.exe
  COMMAND perl viewstat.pl -e stats.pl
  DEPENDS mono basic.exe
)

add_custom_target(stat3
  COMMAND ${RUNTIME} --statfile stats.pl --ncompile 1000 --compile Tests:test_0_many_nested_loops bench.exe 
  COMMAND perl viewstat.pl stats.pl
  DEPENDS mono bench.exe
)

#### This is needed for automake dependency generation
###if INCLUDED_LIBGC
###libgc_libs=${monodir)/libgc/libmonogc.la
###libgc_static_libs=${monodir)/libgc/libmonogc-static.la
###else
###libgc_libs=${LIBGC_LIBS)
###libgc_static_libs=${LIBGC_STATIC_LIBS)
###endif

###AM_CFLAGS = \
###	-I${top_srcdir} 	\
###	${LIBGC_CFLAGS)		\
###	${GLIB_CFLAGS)
###	${PLATFORM_CFLAGS} ${ARCH_CFLAGS)

###AM_CXXFLAGS = ${LLVM_CXXFLAGS} ${GLIB_CFLAGS)

###if PLATFORM_WIN32
###export HOST_CC
#### The mingw math.h has "extern inline" functions that dont appear in libs, so
#### optimisation is required to actually inline them
###PLATFORM_CFLAGS = -O
###endif

#### hack for automake to have the same source file in a library and a bin
###genmdesc_CFLAGS = ${AM_CFLAGS)

###if NO_VERSION_SCRIPT
###monoldflags=${export_ldflags)
###monobinldflags=${export_ldflags)
###else
###monoldflags=-Wl,-version-script=${srcdir)/ldscript ${export_ldflags)
###monobinldflags=-Wl,-version-script=${srcdir)/ldscript.mono ${export_ldflags)
###endif

###if PLATFORM_WIN32
###libmono_la_LDFLAGS=-no-undefined -avoid-version -Wl,--kill-at ${monoldflags)
###else
###libmono_la_LDFLAGS=${monoldflags)
###endif

###if JIT_SUPPORTED

###if PLATFORM_WIN32
###bin_PROGRAMS = mono monow
###else
###bin_PROGRAMS = mono
###endif

###noinst_PROGRAMS = genmdesc

###lib_LTLIBRARIES = libmono.la
###noinst_LTLIBRARIES = libmono-static.la
###endif

###if DTRACE_G_REQUIRED
###LIBMONO_DTRACE_OBJECT = .libs/mono-dtrace.${OBJEXT)
###if STATIC_MONO
###MONO_DTRACE_OBJECT = mono-dtrace.${OBJEXT)
###else
###MONO_DTRACE_OBJECT = 
###endif
###else
###MONO_DTRACE_OBJECT = 
###LIBMONO_DTRACE_OBJECT = 
###endif

###if STATIC_MONO
#### Link libmono into mono statically
#### This leads to higher performance, especially with TLS
###MONO_LIB=libmono-static.la
###else 
###MONO_LIB=libmono.la
###endif

###mono_LDADD = \
###	${MONO_LIB)			\
###	${GLIB_LIBS)		\
###	${LLVM_LIBS)		\
###	-lm	\
###	${MONO_DTRACE_OBJECT)

###mono_LDFLAGS = \
###	${static_flags} -export-dynamic ${monobinldflags)

###if DTRACE_G_REQUIRED

###mono-dtrace.${OBJEXT): ${top_srcdir)/data/mono.d mini.lo ${monodir)/mono/metadata/libmonoruntime-static.la
###	DTRACE="${DTRACE)" DTRACEFLAGS="${DTRACEFLAGS)" AR="${AR)" ${SHELL} ${top_srcdir)/data/dtrace-prelink.sh \
###	$@ ${top_srcdir)/data/mono.d ${monodir)/mono/metadata/libmonoruntime-static.la mini.lo

###.libs/mono-dtrace.${OBJEXT): ${top_srcdir)/data/mono.d mini.lo ${monodir)/mono/metadata/libmonoruntime.la
###	DTRACE="${DTRACE)" DTRACEFLAGS="${DTRACEFLAGS)" AR="${AR)" ${SHELL} ${top_srcdir)/data/dtrace-prelink.sh \
###	--pic $@ ${top_srcdir)/data/mono.d ${monodir)/mono/metadata/libmonoruntime.la mini.lo

###endif

#### Create monow.exe, linked for the 'windows' subsystem
###if PLATFORM_WIN32
###monow_LDADD = ${mono_LDADD)
###monow_LDFLAGS = ${mono_LDFLAGS} -mwindows
###monow_SOURCES = ${mono_SOURCES)
###endif

#### Don't link this against libmonoruntime to speed up rebuilds
###genmdesc_LDADD = \
###	${monodir)/mono/utils/libmonoutils.la ${monodir)/mono/metadata/opcodes.lo -lm	\
###	${GLIB_LIBS)

###if ENABLE_LLVM
#### Disabled for now to fix the windows build
####llvm_sources = \
####	mini-llvm.c		\
####	mini-llvm-cpp.cpp
###endif

###libmono_static_la_LIBADD = ${static_libs} ${MONO_DTRACE_OBJECT)

###libmonoincludedir = ${includedir)/mono-${API_VER)/mono/jit

###libmonoinclude_HEADERS = jit.h

###libmono_la_LIBADD = \
###	${libs} ${LIBMONO_DTRACE_OBJECT)

###clean-local:
###	rm -f mono a.out gmon.out *.o buildver.h test.exe

###pkgconfigdir = ${libdir)/pkgconfig

###if JIT_SUPPORTED
###BUILT_SOURCES = version.h ${arch_built)
###else
###BUILT_SOURCES = version.h
###endif

###CLEANFILES= ${BUILT_SOURCES} *.exe *.dll
###EXTRA_DIST = TestDriver.cs ldscript ldscript.mono \
###	genmdesc.pl	\
###	${test_sources} \
###	${x86_sources} cpu-x86.md \
###	${amd64_sources} cpu-amd64.md \
###	${ppc_sources} cpu-ppc.md cpu-ppc64.md \
###	${arm_sources} cpu-arm.md \
###	${mips_sources} cpu-mips.md \
###	${sparc_sources} cpu-sparc.md \
###	${s390_sources} cpu-s390.md \
###	${s390x_sources} cpu-s390x.md \
###	${ia64_sources} cpu-ia64.md \
###	${alpha_sources} cpu-alpha.md \
###	${hppa_sources} cpu-hppa.md 	\
###	${windows_sources)		\
###	${darwin_sources)		\
###	${posix_sources)
