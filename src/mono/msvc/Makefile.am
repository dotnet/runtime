
VCXPROJS = $(wildcard *.vcxproj)

EXTRA_DIST = \
	     $(VCXPROJS) \
	     README \
	     create-windef.pl \
	     mono.def \
	     mono.props \
	     mono.sln \
	     monoposixhelper.def \
	     runmdesc.bat \
	     winsetup.bat

update-def:
	perl create-windef.pl mono.def ../mono/mini/.libs/libmonoboehm-2.0.so mono-2.0.dll
	perl create-windef.pl monosgen.def ../mono/mini/.libs/libmonosgen-2.0.so monosgen-2.0.dll
