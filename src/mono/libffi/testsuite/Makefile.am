## Process this file with automake to produce Makefile.in.

AUTOMAKE_OPTIONS = foreign dejagnu

# Setup the testing framework, if you have one
EXPECT = `if [ -f $(top_builddir)/../expect/expect ] ; then \
            echo $(top_builddir)/../expect/expect ; \
          else echo expect ; fi`

RUNTEST = `if [ -f $(top_srcdir)/../dejagnu/runtest ] ; then \
	       echo $(top_srcdir)/../dejagnu/runtest ; \
	    else echo runtest; fi`

RUNTESTFLAGS = @AM_RUNTESTFLAGS@

