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

for-tuomas:
	cp *.html ~/home-page/for-tuomas
	web-sync
