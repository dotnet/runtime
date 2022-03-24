/**
 * \file
 */

#ifndef __MONO_METADATA_VERIFY_H__
#define __MONO_METADATA_VERIFY_H__

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>
#include <glib.h> /* GSList dep */

MONO_BEGIN_DECLS

typedef enum {
	MONO_VERIFY_OK,
	MONO_VERIFY_ERROR,
	MONO_VERIFY_WARNING,
	MONO_VERIFY_CLS = 4,
	MONO_VERIFY_ALL = 7,

	/* Status signaling code that is not verifiable.*/
	MONO_VERIFY_NOT_VERIFIABLE = 8,

	/*OR it with other flags*/

	/* Abort the verification if the code is not verifiable.
	 * The standard behavior is to abort if the code is not valid.
	 * */
	MONO_VERIFY_FAIL_FAST = 16,


	/* Perform less verification of the code. This flag should be used
	 * if one wants the verifier to be more compatible to the MS runtime.
	 * Mind that this is not to be more compatible with MS peverify, but
	 * with the runtime itself, that has a less strict verifier.
	 */
	MONO_VERIFY_NON_STRICT = 32,

	/*Skip all visibility related checks*/
	MONO_VERIFY_SKIP_VISIBILITY = 64,

	/*Skip all visibility related checks*/
	MONO_VERIFY_REPORT_ALL_ERRORS = 128

} MonoVerifyStatus;

typedef struct {
	char            *message;
	MonoVerifyStatus status;
} MonoVerifyInfo;

typedef struct {
	MonoVerifyInfo info;
	int8_t exception_type; /*should be one of MONO_EXCEPTION_* */
} MonoVerifyInfoExtended;


MONO_API MONO_RT_EXTERNAL_ONLY GSList* mono_method_verify       (MonoMethod *method, int level);
MONO_API MONO_RT_EXTERNAL_ONLY void    mono_free_verify_list    (GSList *list);
MONO_API MONO_RT_EXTERNAL_ONLY char*   mono_verify_corlib       (void);

MONO_END_DECLS

#endif  /* __MONO_METADATA_VERIFY_H__ */

