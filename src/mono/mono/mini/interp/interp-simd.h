#ifndef __MONO_MINI_INTERP_SIMD_H__
#define __MONO_MINI_INTERP_SIMD_H__

#include <glib.h>

typedef void (*PP_SIMD_Method) (gpointer, gpointer);
typedef void (*PPP_SIMD_Method) (gpointer, gpointer, gpointer);
typedef void (*PPPP_SIMD_Method) (gpointer, gpointer, gpointer, gpointer);

extern PP_SIMD_Method interp_simd_p_p_table [];
extern PPP_SIMD_Method interp_simd_p_pp_table [];
extern PPPP_SIMD_Method interp_simd_p_ppp_table [];

#endif /* __MONO_MINI_INTERP_SIMD_H__ */


