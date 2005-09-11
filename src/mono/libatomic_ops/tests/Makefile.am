EXTRA_DIST=test_atomic.template list_atomic.template run_parallel.inc \
	   test_atomic_include.h
# We distribute test_atomic_include.h, since it's hard to regenerate
# on Windows without sed.

BUILT_SOURCES = test_atomic_include.h list_atomic.i
CLEANFILES = test_atomic_include.h list_atomic.c list_atomic.i

AM_CPPFLAGS=-I$(srcdir)/../src

TESTS=test_atomic test_atomic_pthreads test_stack test_malloc

#create the test_atomic test program
check_PROGRAMS=test_atomic test_atomic_pthreads test_stack test_malloc

test_atomic_SOURCES=test_atomic.c
test_atomic_LDADD=-lpthread ../src/libatomic_ops.a

test_atomic_pthreads_SOURCES=test_atomic.c 
test_atomic_pthreads_CPPFLAGS=-DAO_USE_PTHREAD_DEFS $(AM_CPPFLAGS)
test_atomic_pthreads_LDADD=-lpthread ../src/libatomic_ops.a

test_stack_SOURCES=test_stack.c 
test_stack_LDADD=-lpthread ../src/libatomic_ops_gpl.a ../src/libatomic_ops.a 

test_malloc_SOURCES=test_malloc.c 
test_malloc_LDADD=-lpthread ../src/libatomic_ops_gpl.a ../src/libatomic_ops.a 

test_atomic_include.h: test_atomic.template
	sed -e s/XX// $? > $@
	sed -e s/XX/_release/ $? >> $@
	sed -e s/XX/_acquire/ $? >> $@
	sed -e s/XX/_read/ $? >> $@
	sed -e s/XX/_write/ $? >> $@
	sed -e s/XX/_full/ $? >> $@
	sed -e s/XX/_release_write/ $? >> $@
	sed -e s/XX/_acquire_read/ $? >> $@

list_atomic.c: list_atomic.template
	echo "#include \"atomic_ops.h\" " > $@
	sed -e s/XX// $? >> $@
	sed -e s/XX/_release/ $? >> $@
	sed -e s/XX/_acquire/ $? >> $@
	sed -e s/XX/_read/ $? >> $@
	sed -e s/XX/_write/ $? >> $@
	sed -e s/XX/_full/ $? >> $@
	sed -e s/XX/_release_write/ $? >> $@
	sed -e s/XX/_acquire_read/ $? >> $@

list_atomic.i: list_atomic.c
	$(COMPILE) $? -E > list_atomic.i
