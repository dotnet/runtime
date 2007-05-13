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

EXTRA_DIST = create-windef.pl mono.def README mono.sln $(VCPROJS)

update-def:
	perl create-windef.pl mono.def
