WEB_FILES=														\
	ado-net anoncvs asp-net books ccvs c-sharp class-library contact contributing devel-faq documentation download	\
	embedded-api faq firebird gcc-frontend hackers ideas index							\
	java jit-debug languages mailing-lists monodoc-xml mysql odbc							\
	oledb papers													\
	passport plans porting postgresql ppc projects rationale							\
	resources roadmap runtime sqlclient sqlite sybase tdsclient							\
	tds-providers status testing thanks tools								\
	porting anoncvs monodoc-xml winforms delegate

OTHERS= pending resources-pending todo mono-build.sh mono-build-w32.sh print-stack

EXTRA_DIST = $(WEB_FILES) README $(OTHERS)

all-docs: $(WEB_FILES) 
	cat rationale roadmap c-sharp tools class-library \
	runtime documentation download faq contributing \
	resources status > all-docs

class-status: class-status.in Makefile.am
	cat class-status.in > class-status

pending-classes:

#pending-classes: pending-classes.in Makefile.am
#	echo "This is a list of pending classes, and links to their documentation on MSDN.Microsoft.com<p><p>" > pending-classes
#	for i in `cat pending-classes.in`; do x=`echo $$i | sed s/\\\.//g`; link=`echo http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrf@@@ClassTopic.asp | sed "s/@@@/$$x/"`; echo "	<a href=\"$$link\">$$i</a><br>" >> pending-classes; done


webit: dummy
	(cd web; make && make push)

push-notes:
	scp release-notes/mono* www@www:/web/cvsmodules/mono/archive

dummy:

