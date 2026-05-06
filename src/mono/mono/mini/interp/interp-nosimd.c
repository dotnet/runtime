
#include "interp-internals.h"
#include "interp-simd.h"

#ifdef INTERP_ENABLE_SIMD

gboolean interp_simd_enabled = FALSE;

#ifdef HOST_BROWSER

int interp_simd_p_p_wasm_opcode_table [] = {
};

int interp_simd_p_pp_wasm_opcode_table [] = {
};

int interp_simd_p_ppp_wasm_opcode_table [] = {
};

#endif // HOST_BROWSER

PP_SIMD_Method interp_simd_p_p_table [] = {
};

PPP_SIMD_Method interp_simd_p_pp_table [] = {
};

PPPP_SIMD_Method interp_simd_p_ppp_table [] = {
};

#endif // INTERP_ENABLE_SIMD
