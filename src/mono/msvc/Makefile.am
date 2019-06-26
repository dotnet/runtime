
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
endif
