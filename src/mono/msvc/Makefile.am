
VCXPROJS = $(wildcard *.vcxproj)

EXTRA_DIST = \
	     $(VCXPROJS) \
	     README \
	     mono.props \
	     mono.sln \
	     monoposixhelper.def \
	     runmdesc.bat \
	     winsetup.bat

if ENABLE_MSVC
all-local:
	@echo "Building Mono runtime using Visual Studio."
if SUPPORT_BOEHM
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "boehm"
endif
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen"
	$(mono_msvc_build_bin_dir)/mono-sgen.exe --version

clean-local:
	@echo "Clean Visual Studio build Mono runtime."
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)"

install-exec-local:
	@echo "Install Visual Studio build Mono runtime."
	mkdir -p "$(DESTDIR)$(libdir)"
	mkdir -p "$(DESTDIR)$(bindir)"
	$(install_sh) $(mono_msvc_build_lib_dir)/*.lib "$(DESTDIR)$(libdir)"
	$(install_sh) $(mono_msvc_build_bin_dir)/*.dll "$(DESTDIR)$(bindir)"
	$(install_sh) $(mono_msvc_build_bin_dir)/*.exe "$(DESTDIR)$(bindir)"
	$(install_sh) $(mono_msvc_build_bin_dir)/*.pdb "$(DESTDIR)$(bindir)"

eglib:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "eglib.vcxproj"

clean-eglib:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "eglib.vcxproj"

libgcmonosgen:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "libgcmonosgen.vcxproj"

clean-libgcmonosgen:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "libgcmonosgen.vcxproj"

libmonoposixhelper:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "monoposixhelper.vcxproj"

clean-libmonoposixhelper:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "monoposixhelper.vcxproj"

libmonoutils:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "libmonoutils.vcxproj"

clean-libmonoutils:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "libmonoutils.vcxproj"

libmonoruntime:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "libmonoruntime.vcxproj"

clean-libmonoruntime:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "libmonoruntime.vcxproj"

mono:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "mono.vcxproj"

clean-mono:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "mono.vcxproj"

monow:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "monow.vcxproj"

clean-monow:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "monow.vcxproj"

pedump:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "pedump.vcxproj"

clean-pedump:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "pedump.vcxproj"

monodis:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "monodis.vcxproj"

clean-monodis:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "monodis.vcxproj"

genmdesc:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "genmdesc.vcxproj"

clean-genmdesc:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "genmdesc.vcxproj"

mprof-report:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "mprof-report.vcxproj"

clean-mprof-report:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "mprof-report.vcxproj"

jay:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "sgen" "" "$(top_srcdir)/mcs/jay/jay.vcxproj"

clean-jay:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "sgen" "" "$(top_srcdir)/mcs/jay/jay.vcxproj"

endif
