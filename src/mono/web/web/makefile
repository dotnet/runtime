SOURCES=	\
	../contributing  	\
	../class-library	\
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

push:
	scp *.html www:/web/cvsmodules/mono

push2: 
	scp *.html primates:public_html/xxx