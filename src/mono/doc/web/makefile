# Until we are ready:
#	../class-library	\

OBJECTS=	\
	deploy/anoncvs.html		\
	deploy/contributing.html	\
	deploy/class-status.html		\
	deploy/class-status-corlib.html		\
	deploy/class-status-System.html		\
	deploy/class-status-System.Xml.html		\
	deploy/class-status-System.Data.html		\
	deploy/class-library.html	\
	deploy/classlib-doc.html		\
	deploy/contact.html		\
	deploy/c-sharp.html		\
	deploy/ccvs.html			\
	deploy/documentation.html	\
	deploy/download.html		\
	deploy/faq.html			\
	deploy/gcc-frontend.html		\
	deploy/index.html		\
	deploy/ideas.html		\
	deploy/mailing-lists.html	\
	deploy/passport.html		\
	deploy/rationale.html		\
	deploy/resources.html		\
	deploy/roadmap.html		\
	deploy/runtime.html		\
	deploy/status.html		\
	deploy/testing.html		\
	deploy/porting.html		\
	deploy/tools.html

NON_HTML_SOURCES=	\
	../mono-build.sh	\
	../mono-build-w32.sh


all: $(OBJECTS)
	perl process.pl commands template.html.in deploy

clean:
	rm -f $(OBJECTS) $(patsubst deploy/%.html,src/%.src, $(OBJECTS)) src/*.xml src/*.html.in ../class-status-* ../class-status

push:
	cp -f $(NON_HTML_SOURCES) deploy
	echo scp -P 2200 -r deploy/* www@www.ximian.com:/web/cvsmodules/mono
	(cd deploy; rsync --verbose --recursive --checksum --update --rsh='ssh -p 2200' . www@www.ximian.com:/web/cvsmodules/mono )

push2: 
	scp -r deploy/* primates:public_html/xxx

$(OBJECTS): $(patsubst deploy/%.html,src/%.src, $(OBJECTS))

#.PRECIOUS: $(patsubst deploy/%.html,src/%.src, $(OBJECTS))



.PRECIOUS: src/%.src
src/%.src: ../% commands makefile template.html.in
	perl htmlify $< > $@



.PRECIOUS: ../class-status-%
../class-status-%: src/%.html.in ../class-status.in
	cat ../class-status.in $< > $@



.PRECIOUS: src/%.html.in
src/%.html.in: src/%.xml ../../../mcs/tools/corcompare/transform.js ../../../mcs/tools/corcompare/cormissing.xsl
	cscript /nologo ../../../mcs/tools/corcompare/transform.js $< ../../../mcs/tools/corcompare/cormissing.xsl > $@



.PRECIOUS: src/%.xml
src/%.xml: ../../../mcs/class/lib/%.dll
	../../../mcs/tools/corcompare/CorCompare.exe -x $@ $<

src/corlib.xml: ../../../mcs/class/lib/corlib_cmp.dll
	../../../mcs/tools/corcompare/CorCompare.exe -f corlib -ms mscorlib -x $@ $<


../class-status: ../class-status.in
	cp -f $< $@
