WEB_FILES=  \
	c-sharp class-library contact contributing documentation download \
	faq gcc-frontend ideas index  passport rationale resources \
	roadmap runtime status team testing thanks tools porting anoncvs

OTHERS= pending resources-pending todo

EXTRA_DIST = $(WEB_FILES) README $(OTHERS)

all-docs: $(WEB_FILES) 
	cat rationale roadmap c-sharp tools class-library \
	runtime documentation download faq contributing \
	resources status > all-docs

webit:
	(cd web; make && make push)

push-notes:
	scp release-notes/mono* www@www:/web/cvsmodules/mono/archive
