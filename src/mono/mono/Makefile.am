if SUPPORT_SGEN
sgen_dirs = sgen
endif

if CROSS_COMPILING
SUBDIRS = arch utils io-layer cil metadata $(sgen_dirs) mini dis profiler
else
if INSTALL_MONOTOUCH
SUBDIRS = arch utils io-layer metadata $(sgen_dirs) mini profiler

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
SUBDIRS = arch utils io-layer cil metadata $(sgen_dirs) mini dis tests unit-tests benchmark profiler
endif
endif
DIST_SUBDIRS = arch utils io-layer cil metadata $(sgen_dirs) mini dis tests unit-tests benchmark profiler
