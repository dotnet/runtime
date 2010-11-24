if CROSS_COMPILING
SUBDIRS = utils io-layer cil metadata arch $(interpreter_dir) mini dis
else
if MOONLIGHT
SUBDIRS = utils io-layer metadata arch mini

moon-do-build:
	@list='$(SUBDIRS)'; for subdir in $$list; do \
	  case "x$$subdir" in \
		xmetadata ) target="moon-do-build" ;; \
		xmini ) target="moon-do-build" ;; \
		* ) target="all" ;; \
	  esac; \
	  echo "Making $$target in $$subdir"; \
	  (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$target); \
    done;

moon-do-clean:
	@list='$(SUBDIRS)'; for subdir in $$list; do \
	  case "x$$subdir" in \
		xmetadata ) target="moon-do-clean" ;; \
		xmini ) target="moon-do-clean" ;; \
		* ) target="clean" ;; \
	  esac; \
	  echo "Making $$target in $$subdir"; \
	  (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$target); \
    done;

else
SUBDIRS = utils io-layer cil metadata arch $(interpreter_dir) mini dis monograph tests benchmark profiler
endif
endif
DIST_SUBDIRS = utils io-layer cil metadata arch interpreter mini dis monograph tests benchmark profiler
