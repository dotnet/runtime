CSCRIPT = $(SYSTEMROOT)/system32/cscript.exe
CSC=csc
CORCOMPARE=../../../mcs/tools/corcompare

OBJECTS=	\
	deploy/ado-net.html			\
	deploy/anoncvs.html			\
	deploy/asp-net.html			\
	deploy/books.html			\
	deploy/contributing.html		\
	deploy/class-status.html		\
	deploy/class-status-corlib.html		\
	deploy/class-status-System.html		\
	deploy/class-status-System.Xml.html	\
	deploy/class-status-System.Drawing.html	\
	deploy/class-status-System.Data.html	\
	deploy/class-status-System.Web.html	\
	deploy/class-library.html		\
	deploy/classlib-doc.html		\
	deploy/contact.html			\
	deploy/c-sharp.html			\
	deploy/ccvs.html			\
	deploy/documentation.html		\
	deploy/download.html			\
	deploy/faq.html				\
	deploy/gcc-frontend.html		\
	deploy/index.html			\
	deploy/ideas.html			\
	deploy/java.html			\
	deploy/jit-debug.html			\
	deploy/jit-debug-sample.html		\
	deploy/jit-debug-sample2.html		\
	deploy/languages.html			\
	deploy/mailing-lists.html		\
	deploy/mono-contribution-howto.html	\
	deploy/monodoc-xml.html			\
	deploy/papers.html			\
	deploy/passport.html			\
	deploy/plans.html			\
	deploy/porting.html			\
	deploy/ppc.html				\
	deploy/rationale.html			\
	deploy/resources.html			\
	deploy/roadmap.html			\
	deploy/runtime.html			\
	deploy/status.html			\
	deploy/testing.html			\
	deploy/tools.html			\
	deploy/winforms.html

# deploy/class-status-System.html		\

NON_HTML_SOURCES=	\
	../mono-build.sh	\
	../mono-build-w32.sh


all: $(OBJECTS) index.rss
	perl process.pl commands template.html.in deploy

index.rss: mono-rss.exe ../index
	./mono-rss.exe ../index deploy/index.rss

mono-rss.exe: mono-rss.cs rss.cs 
	$(CSC) mono-rss.cs rss.cs

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
src/%.html.in: src/%.xml $(CORCOMPARE)/transform.js $(CORCOMPARE)/cormissing.xsl
	$(CSCRIPT) /nologo $(CORCOMPARE)/transform.js $< $(CORCOMPARE)/cormissing.xsl > $@



.PRECIOUS: src/%.xml
src/%.xml: ../../../mcs/class/lib/%.dll $(CORCOMPARE)/CorCompare.exe
	$(CORCOMPARE)/CorCompare.exe -x $@ $<

src/corlib.xml: ../../../mcs/class/lib/corlib_cmp.dll $(CORCOMPARE)/CorCompare.exe
	$(CORCOMPARE)/CorCompare.exe -f corlib -ms mscorlib -x $@ $<


../class-status: ../class-status.in
	cp -f $< $@
