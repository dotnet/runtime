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
	deploy/beta1-press.html							\
	deploy/blogs.html							\
	deploy/books.html							\
	deploy/bugs.html							\
	deploy/contributing.html						\
	deploy/contact.html							\
	deploy/c-sharp.html							\
	deploy/ccvs.html							\
	deploy/delegate.html							\
	deploy/documentation.html						\
	deploy/download.html							\
	deploy/drawing.html							\
	deploy/compiling.html							\
	deploy/crypto.html							\
	deploy/embedded-api.html						\
	deploy/faq.html								\
	deploy/firebird.html							\
	deploy/forge.html							\
	deploy/gcc-frontend.html						\
	deploy/gtk-sharp.html							\
	deploy/hackers.html							\
	deploy/ideas.html							\
	deploy/ibmdb2.html							\
	deploy/index.html							\
	deploy/issues.html							\
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
	deploy/performance.html							\
	deploy/plans.html							\
	deploy/porting.html							\
	deploy/postgresql.html							\
	deploy/ppc.html								\
	deploy/provider-factory.html						\
	deploy/rationale.html							\
	deploy/resources.html							\
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
	deploy/remap.html							\
	deploy/winforms.html							\
	deploy/xml-classes.html							\
	deploy/class-library.html						\
	deploy/classlib-doc.html 						\
	deploy/class-status.html	 					\
	deploy/class-status-corlib.html 					\
	deploy/class-status-System.html 					\
	deploy/class-status-System.Xml.html 					\
	deploy/class-status-System.Configuration.Install.html			\
	deploy/class-status-System.Data.html 					\
	deploy/class-status-System.Data.OracleClient.html			\
	deploy/class-status-System.Design.html					\
	deploy/class-status-System.Management.html				\
	deploy/class-status-System.Messaging.html				\
	deploy/class-status-System.Security.html				\
	deploy/class-status-System.Web.html 					\
	deploy/class-status-System.Web.Services.html				\
	deploy/class-status-System.Drawing.html					\
	deploy/class-status-System.Drawing.Design.html				\
	deploy/class-status-Microsoft.VisualBasic.html				\
	deploy/class-status-System.Windows.Forms.html				\
	deploy/class-status-System.DirectoryServices.html			\
	deploy/class-status-System.EnterpriseServices.html			\
	deploy/class-status-System.Runtime.Remoting.html			\
	deploy/class-status-cscompmgd.html					\
	deploy/class-status-System.Runtime.Serialization.Formatters.Soap.html

HTML_SOURCES = ../mono-roadmap.html

#2,Configuration.Install,class-status-System.Configuration.Install.html,class-status-System.Configuration.Install.src,cm/cormissing.css,cm/cormissing.js
#2,Runtime.Remoting,class-status-System.Runtime.Remoting.html,class-status-System.Runtime.Remoting.src,cm/cormissing.css,cm/cormissing.js

#	deploy/class-status-System.Configuration.Install.html	\
#	deploy/class-status-System.Runtime.Remoting.html	\

NON_HTML_SOURCES=		\
	../mono-build.sh	\
	../mono-build-w32.sh

all: $(OBJECTS) deploy/index.rss ../team
	perl process.pl commands template.html.in deploy
	cp $(HTML_SOURCES) deploy
	test -d deploy/team || mkdir deploy/team
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
	rm -f $(OBJECTS) $(patsubst deploy/%.html,src/%.src, $(OBJECTS)) src/*.xml src/*.html.in ../class-status-* ../class-status infos/*.xml

push:
	cp -f $(NON_HTML_SOURCES) deploy
	cp ../mono-roadmap.html deploy
	#echo scp -P 2200 -r deploy/* www@www.go-mono.com:/web/cvsmodules/mono
	(cd deploy; rsync --verbose --recursive --checksum --update --rsh='ssh' . mono-web@www.go-mono.com:go-mono)


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
src/%.html.in: src/%.xml transform.exe $(CORCOMPARE)/mono-api.xsl
	$(RUN_MONO) ./transform.exe $< $(CORCOMPARE)/mono-api.xsl > $@ || (rm -f $@ && exit 1)


.PRECIOUS: infos/%.xml
infos/%.xml: ../../../mcs/class/lib/default/%.dll $(CORCOMPARE)/mono-api-info.exe
	$(RUN_MONO) $(CORCOMPARE)/mono-api-info.exe $< > $@ || (rm -f $@ && exit 1)

.PRECIOUS: src/%.xml
src/%.xml: infos/%.xml $(CORCOMPARE)/mono-api-diff.exe 
	$(RUN_MONO) $(CORCOMPARE)/mono-api-diff.exe masterinfos/$(notdir $<) $< > $@ || (rm -f $@ && exit 1)

# corlib, corlib...
infos/mscorlib.xml: ../../../mcs/class/lib/default/mscorlib.dll $(CORCOMPARE)/mono-api-info.exe
	$(RUN_MONO) $(CORCOMPARE)/mono-api-info.exe $(notdir $(basename $<)) > $@ || (rm -f $@ && exit 1)

src/corlib.xml: infos/mscorlib.xml $(CORCOMPARE)/mono-api-diff.exe 
	$(RUN_MONO) $(CORCOMPARE)/mono-api-diff.exe masterinfos/mscorlib.xml $< > $@ || (rm -f $@ && exit 1)

# System
infos/System.xml: ../../../mcs/class/lib/default/System.dll $(CORCOMPARE)/mono-api-info.exe
	$(RUN_MONO) $(CORCOMPARE)/mono-api-info.exe $(notdir $(basename $<)) > $@ || (rm -f $@ && exit 1)

src/System.xml: infos/System.xml $(CORCOMPARE)/mono-api-diff.exe 
	$(RUN_MONO) $(CORCOMPARE)/mono-api-diff.exe masterinfos/System.xml $< > $@ || (rm -f $@ && exit 1)

# System.Xml
infos/System.Xml.xml: ../../../mcs/class/lib/default/System.Xml.dll $(CORCOMPARE)/mono-api-info.exe
	$(RUN_MONO) $(CORCOMPARE)/mono-api-info.exe $(notdir $(basename $<)) > $@ || (rm -f $@ && exit 1)

src/System.Xml.xml: infos/System.Xml.xml $(CORCOMPARE)/mono-api-diff.exe 
	$(RUN_MONO) $(CORCOMPARE)/mono-api-diff.exe masterinfos/System.Xml.xml $< > $@ || (rm -f $@ && exit 1)

src/System.DirectoryServices.xml: infos/System.DirectoryServices.xml $(CORCOMPARE)/mono-api-diff.exe 
	$(RUN_MONO) $(CORCOMPARE)/mono-api-diff.exe masterinfos/System.DirectoryServices.xml $< > $@ || (rm -f $@ && exit 1)

../class-status: ../class-status.in
	cp -f $< $@

make-rss:
	$(CSC) make-rss.cs -o
