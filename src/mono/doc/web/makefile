SOURCES=	\
	../contributing  	\
	../class-library	\
	../class-status		\
	../contact		\
	../c-sharp		\
	../ccvs			\
	../documentation	\
	../download		\
	../faq			\
	../gcc-frontend		\
	../index		\
	../ideas		\
	../passport		\
	../rationale		\
	../resources		\
	../roadmap		\
	../runtime		\
	../status		\
	../testing		\
	../porting		\
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
	scp *.html migueldeicaza@mononet.sf.net:/home/groups/m/mo/mononet/htdocs

push2: 
	scp *.html primates:public_html/xxx
