ROOT=C:/WINNT
CSCRIPT = $(ROOT)/system32/cscript.exe
CSC=csc

CORCOMPARE=../../../mcs/tools/corcompare

OBJECTS=									\
	deploy/ado-net.html							\
	deploy/provider-factory.html							\
	deploy/mysql.html							\
	deploy/odbc.html							\
	deploy/oledb.html							\
	deploy/oracle.html							\
	deploy/postgresql.html							\
	deploy/sqlclient.html							\
	deploy/sqlite.html							\
	deploy/sybase.html							\
	deploy/tdsclient.html							\
	deploy/anoncvs.html							\
	deploy/asp-net.html							\
	deploy/books.html							\
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
	deploy/java.html							\
	deploy/jit-debug.html							\
	deploy/languages.html							\
	deploy/mailing-lists.html						\
	deploy/mbas.html							\
	deploy/mono-beginning.html						\
	deploy/mono-contribution-howto.html					\
	deploy/monodoc-xml.html							\
	deploy/other.html							\
	deploy/papers.html							\
	deploy/passport.html							\
	deploy/plans.html							\
	deploy/porting.html							\
	deploy/ppc.html								\
	deploy/rationale.html							\
	deploy/resources.html							\
	deploy/roadmap.html							\
	deploy/runtime.html							\
	deploy/screenshots.html							\
	deploy/status.html							\
	deploy/team.html							\
	deploy/testing.html							\
	deploy/tools.html							\
	deploy/winforms.html							\
	deploy/class-status.html						\
        deploy/class-status-corlib.html						\
        deploy/class-status-System.html						\
        deploy/class-status-System.Xml.html					\
        deploy/class-status-System.Drawing.html					\
        deploy/class-status-System.Data.html					\
        deploy/class-status-System.Web.html					\
        deploy/class-status-System.Web.Services.html				\
	deploy/class-status-Microsoft.VisualBasic.html				\
	deploy/class-status-System.EnterpriseServices.html			\
	deploy/class-status-System.Runtime.Serialization.Formatters.Soap.html	\
	deploy/class-status-System.Security.html				\
	deploy/class-status-System.Windows.Forms.html				\
	deploy/class-status-Cscompmgd.html					\
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
	./mono-rss.exe ../index deploy/index.rss

mono-rss.exe: mono-rss.cs rss.cs 
	$(CSC) mono-rss.cs rss.cs

../team: render-team-page.exe team.xml
	./render-team-page.exe team.xml "../team"

render-team-page.exe: render-team-page.cs
	$(CSC) render-team-page.cs

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

src/System.Security.xml: ../../../mcs/class/lib/System.Security_cmp.dll $(CORCOMPARE)/CorCompare.exe
	$(CORCOMPARE)/CorCompare.exe -f System.Security -ms System.Security -x $@ $<

../class-status: ../class-status.in
	cp -f $< $@

make-rss:
	$(CSC) make-rss.cs -o
