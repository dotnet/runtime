# Add the Merge flag here is needed. Not needed for RyuJIT if building as a DLL.
add_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)
if(WIN32)
  add_subdirectory(hosts)
endif(WIN32)

