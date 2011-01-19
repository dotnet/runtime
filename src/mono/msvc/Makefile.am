VCPROJS =					\
	eglib.vcproj				\
	genmdesc.vcproj				\
	libgc.vcproj				\
	libmono.vcproj				\
	mono.vcproj				\
	monoburg.vcproj				\
	monodiet.vcproj				\
	monodis.vcproj				\
	monograph.vcproj			\
	pedump.vcproj				\
	test-invoke.vcproj			\
	test-metadata.vcproj			\
	test_eglib.vcproj			\
	teste.vcproj

VCXPROJS = $(wildcard *.vcxproj)

EXTRA_DIST = \
	     $(VCPROJS) \
	     $(VCXPROJS) \
	     README \
	     create-windef.pl \
	     mono.def \
	     mono.sln \
	     monoposixhelper.def \
	     runmdesc.bat \
	     winsetup.bat

update-def:
	perl create-windef.pl mono.def
