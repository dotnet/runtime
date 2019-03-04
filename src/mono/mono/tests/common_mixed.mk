CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

with_mono_path = MONO_PATH=$(CLASS)

MONO_EXE = $(top_builddir)/runtime/mono-wrapper
RUNTIME = $(with_mono_path) $(MONO_EXE)
TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(MONO_EXE) --aot-path=$(mcs_topdir)/class/lib/build

MCS_NO_UNSAFE = $(TOOLS_RUNTIME) $(CSC) -debug:portable \
	-noconfig -nologo \
	-nowarn:0162 -nowarn:0168 -nowarn:0219 -nowarn:0414 -nowarn:0618 \
	-nowarn:0169 -nowarn:1690 -nowarn:0649 -nowarn:0612 -nowarn:3021 \
	-nowarn:0197 -langversion:latest $(PROFILE_MCS_FLAGS)
MCS_NO_LIB = $(MCS_NO_UNSAFE) -unsafe

MCS = $(MCS_NO_LIB)

ILASM = $(TOOLS_RUNTIME) $(mcs_topdir)/class/lib/build/ilasm.exe


CLEANFILES=*.dll *.exe *.pdb

clean-local:
	$(RM) -rf $(AOTDIR) $(AOT_TMPDIR)
