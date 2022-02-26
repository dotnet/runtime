/**
 * \file
 */

#ifndef _MONONET_METADATA_IMAGE_H_
#define _MONONET_METADATA_IMAGE_H_

#include <mono/metadata/details/image-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/image-functions.h>
#undef MONO_API_FUNCTION

mono_bool mono_has_pdb_checksum (char *raw_data, uint32_t raw_data_len);

MONO_END_DECLS

#endif
