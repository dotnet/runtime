include $(top_srcdir)/mk/common.mk

if SUPPORT_SGEN
sgen_dirs = sgen
endif

if BTLS
btls_dirs = btls
endif

if CROSS_COMPILING
SUBDIRS = $(btls_dirs) eglib arch utils cil $(sgen_dirs) metadata mini dis profiler
else
if INSTALL_MONOTOUCH
SUBDIRS = $(btls_dirs) eglib arch utils $(sgen_dirs) metadata mini profiler

monotouch-do-build:
	@list='$(SUBDIRS)'; for subdir in $$list; do \
	  case "x$$subdir" in \
		xmetadata ) target="monotouch-do-build" ;; \
		xmini ) target="monotouch-do-build" ;; \
		* ) target="all" ;; \
	  esac; \
	  echo "Making $$target in $$subdir"; \
	  (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$target); \
    done;

monotouch-do-clean:
	@list='$(SUBDIRS)'; for subdir in $$list; do \
	  case "x$$subdir" in \
		xmetadata ) target="monotouch-do-clean" ;; \
		xmini ) target="monotouch-do-clean" ;; \
		* ) target="clean" ;; \
	  esac; \
	  echo "Making $$target in $$subdir"; \
	  (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$target); \
    done;
else
SUBDIRS = $(btls_dirs) eglib arch utils cil $(sgen_dirs) metadata mini dis tests unit-tests benchmark profiler
endif
endif
DIST_SUBDIRS = btls eglib arch utils cil $(sgen_dirs) metadata mini dis tests unit-tests benchmark profiler
