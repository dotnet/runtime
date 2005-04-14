# the handles dir doesn't apply to windows
if ! PLATFORM_WIN32
HANDLES = handles
endif

SUBDIRS = utils io-layer monoburg os cil metadata \
      arch interpreter mini dis monograph tests benchmark profiler

DIST_SUBDIRS = $(SUBDIRS) handles
