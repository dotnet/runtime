SOURCES=	\
	../contributing  	\
	../class-library	\
	../contact		\
	../c-sharp		\
	../documentation	\
	../download		\
	../faq			\
	../gcc-frontend		\
	../index		\
	../ideas		\
	../rationale		\
	../resources		\
	../roadmap		\
	../runtime		\
	../status		\
	../testing		\
	../tools

all:
	-mkdir site
	for i in $(SOURCES); do \
		perl htmlify $$i > `basename $$i`.src; \
	done;

	perl process.pl commands template.html.in .

clean:
	for i in $(SOURCES); do \
		rm -f `basename $$i`.src `basename $$i`.html; \
	done;

push:
	scp *.html www@www.ximian.com:/web/cvsmodules/mono

push2: 
	scp *.html primates:public_html/xxx
