/* Copyright 2016 Google Inc. All Rights Reserved.

   Distributed under MIT license.
   See file LICENSE for detail or copy at https://opensource.org/licenses/MIT
*/

/* Version definition. */

#ifndef BROTLI_COMMON_VERSION_H_
#define BROTLI_COMMON_VERSION_H_

/* This macro should only be used when library is compiled together with client.
   If library is dynamically linked, use BrotliDecoderVersion and
   BrotliEncoderVersion methods. */

/* Semantic version, calculated as (MAJOR << 24) | (MINOR << 12) | PATCH */
#define BROTLI_VERSION 0x1000001

#endif  /* BROTLI_COMMON_VERSION_H_ */
