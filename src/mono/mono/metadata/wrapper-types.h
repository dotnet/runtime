/**
 * \file
 * NOTE NOTE NOTE
 * No additional wrapper types should be added.
 * If a new wrapper is asolutely necessary, an existing one needs
 * to be removed first (with all the change that implies).
 */
WRAPPER(NONE, "none")
WRAPPER(DELEGATE_INVOKE, "delegate-invoke")
WRAPPER(DELEGATE_BEGIN_INVOKE, "delegate-begin-invoke")
WRAPPER(DELEGATE_END_INVOKE, "delegate-end-invoke")
WRAPPER(RUNTIME_INVOKE, "runtime-invoke")
WRAPPER(NATIVE_TO_MANAGED, "native-to-managed")
WRAPPER(MANAGED_TO_NATIVE, "managed-to-native")
WRAPPER(MANAGED_TO_MANAGED, "managed-to-managed")
WRAPPER(SYNCHRONIZED, "synchronized")
WRAPPER(DYNAMIC_METHOD, "dynamic-method")
WRAPPER(CASTCLASS, "castclass")
WRAPPER(STELEMREF, "stelemref")
WRAPPER(UNBOX, "unbox")
WRAPPER(WRITE_BARRIER, "write-barrier")
WRAPPER(OTHER, "other")
WRAPPER(ALLOC, "alloc")
