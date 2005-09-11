Most of the atomic_ops functionality is available under Win32 with
the Microsoft tools, but the build process currently is considerably more
primitive than on Linux/Unix platforms.

To build:

1) Go to the src directory in the distribution.
2) Make sure the Microsoft command-line tools (e.g. nmake) are available.
3) Run "nmake -f Makefile.msft".  This should run some tests, which
may print warnings about the types of the "Interlocked" functions.
I haven't been able to make all versions of VC++ happy.  If you know
how to, please send a patch.
4) To compile applications, you will need to retain or copy the following
pieces from the resulting src directory contents:
	"atomic_ops.h" - Header file defining low-level primitives.  This
			 includes files from:
	"atomic_ops"- Subdirectory containing implementation header files.
	"atomic_ops_stack.h" - Header file describing almost lock-free stack.
	"atomic_ops_malloc.h" - Header file describing almost lock-free malloc.
	"libatomic_ops_gpl.lib" - Library containing implementation of the
				  above two.  The atomic_ops.h implementation
				  is entirely in the header files in Win32.

Most clients of atomic_ops.h will need to define AO_ASSUME_WINDOWS98 before
including it.  Compare_and_swap is otherwise not available.

Note that the library is covered by the GNU General Public License, while
the top 2 of these pieces allow use in proprietary code.
