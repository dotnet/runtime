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
WRAPPER(REMOTING_INVOKE, "remoting-invoke")
WRAPPER(REMOTING_INVOKE_WITH_CHECK, "remoting-invoke-with-check")
WRAPPER(XDOMAIN_INVOKE, "xdomain-invoke")
WRAPPER(XDOMAIN_DISPATCH, "xdomain-dispatch")
WRAPPER(LDFLD, "ldfld")
WRAPPER(STFLD, "stfld")
WRAPPER(SYNCHRONIZED, "synchronized")
WRAPPER(DYNAMIC_METHOD, "dynamic-method")
WRAPPER(CASTCLASS, "castclass")
WRAPPER(PROXY_ISINST, "proxy_isinst")
WRAPPER(STELEMREF, "stelemref")
WRAPPER(UNBOX, "unbox")
WRAPPER(LDFLDA, "ldflda")
WRAPPER(WRITE_BARRIER, "write-barrier")
WRAPPER(UNKNOWN, "unknown")
WRAPPER(COMINTEROP_INVOKE, "cominterop-invoke")
WRAPPER(COMINTEROP, "cominterop")
WRAPPER(ALLOC, "alloc")


