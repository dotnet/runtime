/**
 * \file
 * Metadata verfication support
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright (C) 2005-2008 Novell, Inc. (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/attrdefs.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/bsearch.h>
#include <string.h>
#include <ctype.h>

gboolean
mono_verifier_verify_table_data (MonoImage *image, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_cli_data (MonoImage *image, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_pe_data (MonoImage *image, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_full_table_data (MonoImage *image, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_field_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_method_header (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_method_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_standalone_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_typespec_signature (MonoImage *image, guint32 offset, guint32 token, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_methodspec_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_string_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_cattr_blob (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_cattr_content (MonoImage *image, MonoMethod *ctor, const guchar *data, guint32 size, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_is_sig_compatible (MonoImage *image, MonoMethod *method, MonoMethodSignature *signature)
{
	return TRUE;
}


gboolean
mono_verifier_verify_typeref_row (MonoImage *image, guint32 row, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_methodimpl_row (MonoImage *image, guint32 row, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_memberref_method_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

gboolean
mono_verifier_verify_memberref_field_signature (MonoImage *image, guint32 offset, MonoError *error)
{
	error_init (error);
	return TRUE;
}

