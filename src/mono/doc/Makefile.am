WEB_FILES=  \
	c-sharp class-library contact contributing documentation download \
	faq gcc-frontend ideas index  passport rationale resources \
	roadmap runtime status team testing thanks tools porting anoncvs

OTHERS= pending resources-pending todo mono-build.sh mono-build-w32.sh

EXTRA_DIST = $(WEB_FILES) README $(OTHERS)

all-docs: $(WEB_FILES) 
	cat rationale roadmap c-sharp tools class-library \
	runtime documentation download faq contributing \
	resources status > all-docs

class-status: class-status.in pending-classes Makefile.am
	cat class-status.in pending-classes > class-status

pending-classes:

#pending-classes: pending-classes.in Makefile.am
#	echo "This is a list of pending classes, and links to their documentation on MSDN.Microsoft.com<p><p>" > pending-classes
#	for i in `cat pending-classes.in`; do x=`echo $$i | sed s/\\\.//g`; link=`echo http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrf@@@ClassTopic.asp | sed "s/@@@/$$x/"`; echo "	<a href=\"$$link\">$$i</a><br>" >> pending-classes; done


webit: dummy
	(cd web; make && make push)

push-notes:
	scp release-notes/mono* www@www:/web/cvsmodules/mono/archive

dummy:

