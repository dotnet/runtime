add_definitions(-D_CRTIMP=) #static link of crt
add_precompiled_header(stdafx.h ../stdafx.cpp IPCMAN_SOURCES)
add_library_clr(ipcmanager-staticcrt STATIC ${IPCMAN_SOURCES})