// HKTN-TODO: cull the #includes
#include <config.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/icall-decl.h>
#include "mini.h"
#include "mini-runtime.h"
#include "ir-emit.h"
#include "llvm-intrinsics-types.h"
#ifdef ENABLE_LLVM
#include "mini-llvm.h"
#include "mini-llvm-cpp.h"
#endif
#include "mono/utils/bsearch.h"
#include <mono/metadata/abi-details.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/mono-hwcap.h>

static NamedIntrinsic 
lookup_named_intrinsic (const char* class_ns, const char* class_name, MonoMethod* method)
{
  // We should be able to get class_ns and class_name for free - emit_intrinsics generates that.

  NamedIntrinsic ret = NamedIntrinsic.NI_Illegal;
  // HKTN-TODO: https://github.com/dotnet/runtime/blob/559470195bec88d9c74e70ea440c8394a0a6cfdc/src/coreclr/jit/importercalls.cpp#L8487
  // HKTN-TODO: Are we interested in automatically generating this search code? We could use some C# code to generate that perhaps.

  return ret;
}