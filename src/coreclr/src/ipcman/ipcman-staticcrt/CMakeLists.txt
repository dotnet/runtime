add_definitions(-D_CRTIMP=) #static link of crt
add_library(ipcmanager-staticcrt STATIC ${IPCMAN_SOURCES})