# Until we are ready:
#	../class-library	\

SOURCES=	\
	../anoncvs		\
	../contributing  	\
	../class-status		\
	../class-library	\
	../classlib-doc		\
	../contact		\
	../c-sharp		\
	../ccvs			\
	../documentation	\
	../download		\
	../faq			\
	../gcc-frontend		\
	../index		\
	../ideas		\
	../mailing-lists	\
	../passport		\
	../rationale		\
	../resources		\
	../roadmap		\
	../runtime		\
	../status		\
	../testing		\
	../porting		\
	../tools

NON_HTML_SOURCES=	\
	../mono-build.sh	\
	../mono-build-w32.sh


all:
	-mkdir site
	for i in $(SOURCES); do \
		perl htmlify $$i > src/`basename $$i`.src; \
	done;

	perl process.pl commands template.html.in deploy

clean:
	for i in $(SOURCES); do \
		rm -f src/`basename $$i`.src deploy/`basename $$i`.html; \
	done;

push:
	cp -f $(NON_HTML_SOURCES) deploy
	echo scp -P 2200 -r deploy/* www@www.ximian.com:/web/cvsmodules/mono
	(cd deploy; rsync --verbose --recursive --checksum --update --rsh='ssh -p 2200' . www@www.ximian.com:/web/cvsmodules/mono )

push2: 
	scp -r deploy/* primates:public_html/xxx

