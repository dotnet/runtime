ifeq ($(findstring CYGWIN,$(shell uname)),)
CSC=mcs
RUN_MONO=mono
else
CSC=csc /nologo
endif

CORCOMPARE=../../../mcs/tools/corcompare

OBJECTS=									\
	deploy/ado-net.html							\
	deploy/anoncvs.html							\
	deploy/asp-net.html							\
	deploy/blogs.html							\
	deploy/books.html							\
	deploy/bugs.html							\
	deploy/contributing.html						\
	deploy/contact.html							\
	deploy/c-sharp.html							\
	deploy/ccvs.html							\
	deploy/documentation.html						\
	deploy/download.html							\
	deploy/crypto.html							\
	deploy/embedded-api.html						\
	deploy/faq.html								\
	deploy/firebird.html							\
	deploy/gcc-frontend.html						\
	deploy/gtk-sharp.html							\
	deploy/hackers.html							\
	deploy/ideas.html							\
	deploy/ibmdb2.html							\
	deploy/index.html							\
	deploy/oldnews.html							\
	deploy/java.html							\
	deploy/jit-debug.html							\
	deploy/languages.html							\
	deploy/mailing-lists.html						\
	deploy/mbas.html							\
	deploy/mono-beginning.html						\
	deploy/mono-contribution-howto.html					\
	deploy/mono-todo.html							\
	deploy/monodoc-xml.html							\
	deploy/mysql.html							\
	deploy/odbc.html							\
	deploy/oledb.html							\
	deploy/oracle.html							\
	deploy/other.html							\
	deploy/papers.html							\
	deploy/passport.html							\
	deploy/plans.html							\
	deploy/porting.html							\
	deploy/postgresql.html							\
	deploy/ppc.html								\
	deploy/provider-factory.html						\
	deploy/rationale.html							\
	deploy/resources.html							\
	deploy/roadmap.html							\
	deploy/runtime.html							\
	deploy/screenshots.html							\
	deploy/sqlclient.html							\
	deploy/sqlite.html							\
	deploy/status.html							\
	deploy/sybase.html							\
	deploy/tdsclient.html							\
	deploy/team.html							\
	deploy/testing.html							\
	deploy/tools.html							\
	deploy/winforms.html							\
	deploy/class-library.html						\
	deploy/classlib-doc.html

#2,Configuration.Install,class-status-System.Configuration.Install.html,class-status-System.Configuration.Install.src,cm/cormissing.css,cm/cormissing.js
#2,Runtime.Remoting,class-status-System.Runtime.Remoting.html,class-status-System.Runtime.Remoting.src,cm/cormissing.css,cm/cormissing.js

#	deploy/class-status-System.Configuration.Install.html	\
#	deploy/class-status-System.Runtime.Remoting.html	\

NON_HTML_SOURCES=		\
	../mono-build.sh	\
	../mono-build-w32.sh

all: $(OBJECTS) deploy/index.rss ../team
	perl process.pl commands template.html.in deploy
	-mkdir deploy/team
	cp team/*png deploy/team

tutorial: mono-beginning.html.stamp mono-beginning.pdf 
	cp -a mono-beginning mono-beginning.xml mono-beginning.pdf deploy

mono-beginning.html.stamp: mono-beginning.xml
	db2html mono-beginning.xml
	touch mono-beginning.html.stamp

mono-beginning.pdf: mono-beginning.xml
	db2pdf mono-beginning.xml

mono-beginning.xml: ../mono-beginning.xml
	cp ../mono-beginning.xml .

deploy/index.rss: mono-rss.exe ../index
	$(RUN_MONO) ./mono-rss.exe ../index deploy/index.rss

mono-rss.exe: mono-rss.cs rss.cs 
	$(CSC) mono-rss.cs rss.cs

../team: render-team-page.exe team.xml
	$(RUN_MONO) ./render-team-page.exe team.xml "../team"

render-team-page.exe: render-team-page.cs
	$(CSC) render-team-page.cs

clean:
	rm -f $(OBJECTS) $(patsubst deploy/%.html,src/%.src, $(OBJECTS)) src/*.xml src/*.html.in ../class-status-* ../class-status

push:
	cp -f $(NON_HTML_SOURCES) deploy
	#echo scp -P 2200 -r deploy/* www@www.go-mono.com:/web/cvsmodules/mono
	#(cd deploy; rsync --verbose --recursive --checksum --update --rsh='ssh' . www@www.go-mono.com:/web/cvsmodules/mono )
	(cd deploy; rsync --verbose --recursive --checksum --update --rsh='ssh' . mono-web@www.go-mono.com:/web/cvsmodules/mono )

push2: 
	scp -r deploy/* primates:public_html/xxx

$(OBJECTS): $(patsubst deploy/%.html,src/%.src, $(OBJECTS))

#.PRECIOUS: $(patsubst deploy/%.html,src/%.src, $(OBJECTS))

transform.exe: transform.cs
	$(CSC) transform.cs


.PRECIOUS: src/%.src
src/%.src: ../% commands template.html.in makefile
	perl htmlify $< > $@

.PRECIOUS: ../class-status-%
../class-status-%: src/%.html.in ../class-status.in
	cat ../class-status.in $< > $@


.PRECIOUS: src/%.html.in
src/%.html.in: src/%.xml transform.exe $(CORCOMPARE)/cormissing.xsl
	$(RUN_MONO) ./transform.exe $< $(CORCOMPARE)/cormissing.xsl > $@



.PRECIOUS: src/%.xml
src/%.xml: ../../../mcs/class/lib/%.dll $(CORCOMPARE)/CorCompare.exe
	$(RUN_MONO) $(CORCOMPARE)/CorCompare.exe -x $@ $<

src/corlib.xml: ../../../mcs/class/lib/corlib_cmp.dll $(CORCOMPARE)/CorCompare.exe
	$(RUN_MONO) $(CORCOMPARE)/CorCompare.exe -f corlib -ms mscorlib -x $@ $<

src/System.Security.xml: ../../../mcs/class/lib/System.Security_cmp.dll $(CORCOMPARE)/CorCompare.exe
	$(RUN_MONO) $(CORCOMPARE)/CorCompare.exe -f System.Security -ms System.Security -x $@ $<

../class-status: ../class-status.in
	cp -f $< $@

make-rss:
	$(CSC) make-rss.cs -o
