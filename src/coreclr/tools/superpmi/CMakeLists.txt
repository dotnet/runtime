# The native parts of the managed SuperPMI tools (managed/), which the managed projects publish next to them.
add_subdirectory(jitinterface)

# They implement and forward ICorJitInfo, so they must see it as the JIT does.
set_target_properties(superpmi-jitinterface superpmi-shim-jitinterface
  PROPERTIES IGNORE_FEATURE_INTERPRETER_DIRECT_CALLS TRUE)
