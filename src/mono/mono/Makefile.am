if CROSS_COMPILING
SUBDIRS = utils io-layer cil metadata arch $(interpreter_dir) mini dis
else
if MOONLIGHT
SUBDIRS = utils io-layer     metadata arch $(interpreter_dir) mini
else
SUBDIRS = utils io-layer cil metadata arch $(interpreter_dir) mini dis monograph tests benchmark profiler
endif
endif
