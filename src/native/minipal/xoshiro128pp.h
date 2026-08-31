// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef HAVE_MINIPAL_XOSHIRO128PP_H
#define HAVE_MINIPAL_XOSHIRO128PP_H

#include <stdint.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

struct minipal_xoshiro128pp
{
    uint32_t s[4];
};

/**
 * Initialize the Xoshiro128++ random number generator state
 *
 * @param pState Pointer to the Xoshiro128++ state structure to be initialized.
 * @param seed Seed value to initialize the generator. Different seeds produce different random sequences.
 */
void minipal_xoshiro128pp_init(struct minipal_xoshiro128pp* pState, uint32_t seed);

/**
 * Generate the next random number in the Xoshiro128++ sequence
 *
 * @param pState Pointer to the Xoshiro128++ state structure.
 * @return The next 32-bit unsigned random number in the sequence.
 */
uint32_t minipal_xoshiro128pp_next(struct minipal_xoshiro128pp* pState);

#ifdef __cplusplus
}
#endif // __cplusplus
#endif /* HAVE_MINIPAL_XOSHIRO128PP_H */
