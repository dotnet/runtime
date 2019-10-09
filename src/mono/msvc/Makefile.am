
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
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)"
	$(mono_msvc_build_bin_dir)/mono-sgen.exe --version

clean-local:
	@echo "Clean Visual Studio build Mono runtime."
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)"

install-exec-local:
	@echo "Install Visual Studio build Mono runtime."
	$(mkinstalldirs) "$(DESTDIR)$(libdir)"
	$(mkinstalldirs) "$(DESTDIR)$(bindir)"
	$(install_sh) $(mono_msvc_build_lib_dir)/*.lib "$(DESTDIR)$(libdir)"
	$(install_sh) $(mono_msvc_build_bin_dir)/*.dll "$(DESTDIR)$(bindir)"
	$(install_sh) $(mono_msvc_build_bin_dir)/*.exe "$(DESTDIR)$(bindir)"
	$(install_sh) $(mono_msvc_build_bin_dir)/*.pdb "$(DESTDIR)$(bindir)"

eglib:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "eglib.vcxproj"

clean-eglib:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "eglib.vcxproj"

libgcmonosgen:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "libgcmonosgen.vcxproj"

clean-libgcmonosgen:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "libgcmonosgen.vcxproj"

libmonoposixhelper:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "monoposixhelper.vcxproj"

clean-libmonoposixhelper:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "monoposixhelper.vcxproj"

libmonoutils:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "libmonoutils.vcxproj"

clean-libmonoutils:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "libmonoutils.vcxproj"

libmonoruntime:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "libmonoruntime.vcxproj"

clean-libmonoruntime:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "libmonoruntime.vcxproj"

mono:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "mono.vcxproj"

clean-mono:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "mono.vcxproj"

monow:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "monow.vcxproj"

clean-monow:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "monow.vcxproj"

pedump:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "pedump.vcxproj"

clean-pedump:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "pedump.vcxproj"

monodis:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "monodis.vcxproj"

clean-monodis:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "monodis.vcxproj"

genmdesc:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "genmdesc.vcxproj"

clean-genmdesc:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "genmdesc.vcxproj"

jay:
	$(top_srcdir)/msvc/run-msbuild.sh "build" "$(host_cpu)" "release" "" "$(top_srcdir)/mcs/jay/jay.vcxproj"

clean-jay:
	$(top_srcdir)/msvc/run-msbuild.sh "clean" "$(host_cpu)" "release" "" "$(top_srcdir)/mcs/jay/jay.vcxproj"

endif
