project(ryujit)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
remove_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

if(FEATURE_READYTORUN)
  add_definitions(-DFEATURE_READYTORUN_COMPILER)
endif(FEATURE_READYTORUN)

add_jit(clrjit)

# add the install targets
install_clr(TARGETS clrjit DESTINATION sharedFramework SKIP_STRIP)

# Enable profile guided optimization
add_pgo(clrjit)
