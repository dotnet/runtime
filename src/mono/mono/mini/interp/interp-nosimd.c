
#include "interp-internals.h"
#include "interp-simd.h"

#ifdef INTERP_ENABLE_SIMD

gboolean interp_simd_enabled = FALSE;

#if HOST_BROWSER || HOST_WASI

int interp_simd_p_p_wasm_opcode_table [] = {
};

int interp_simd_p_pp_wasm_opcode_table [] = {
};

int interp_simd_p_ppp_wasm_opcode_table [] = {
};

// Always 0 in the nosimd library: when WasmEnableSIMD is false the app links this
// variant instead of either libmono-wasm-simd.a or libmono-wasm-relaxed-simd.a, and
// emit_sri_relaxedsimd in transform-simd.c reads this to gate RelaxedSimd.IsSupported.
const int mono_interp_relaxed_simd_supported = 0;

#endif // HOST_BROWSER || HOST_WASI

PP_SIMD_Method interp_simd_p_p_table [] = {
};

PPP_SIMD_Method interp_simd_p_pp_table [] = {
};

PPPP_SIMD_Method interp_simd_p_ppp_table [] = {
};

#endif // INTERP_ENABLE_SIMD
