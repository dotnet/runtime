noinst_PROGRAMS = test 

test_SOURCES = \
	test.c      \
	driver.c	\
	hash.c		\
	str.c 		\
	slist.c

test_CFLAGS = -Wall -Werror -D_FORTIFY_SOURCE=2
INCLUDES = -I../src

test_LDADD = -L../src -leglib

MAINTAINERCLEANFILES = Makefile.in

