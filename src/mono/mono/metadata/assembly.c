/**
 * \file
 * Routines for loading assemblies.
 * 
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include "assembly.h"
#include "assembly-internals.h"
#include "image.h"
#include "image-internals.h"
#include "object-internals.h"
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/custom-attrs-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-uri.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/mono-config-dirs.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-path.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/coree.h>
#include <mono/metadata/cil-coff.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-os-mutex.h>

#ifndef HOST_WIN32
#include <sys/types.h>
#include <unistd.h>
#include <sys/stat.h>
#endif

#ifdef HOST_DARWIN
#include <mach-o/dyld.h>
#endif

/* AssemblyVersionMap: an assembly name, the assembly version set on which it is based, the assembly name it is replaced with and whether only versions lower than the current runtime version should be remapped */
typedef struct  {
	const char* assembly_name;
	guint8 version_set_index;
	const char* new_assembly_name;
	gboolean only_lower_versions;
	gboolean framework_facade_assembly;
} AssemblyVersionMap;

/* the default search path is empty, the first slot is replaced with the computed value */
static char*
default_path [] = {
	NULL,
	NULL,
	NULL
};

/* Contains the list of directories to be searched for assemblies (MONO_PATH) */
static char **assemblies_path = NULL;

/* Contains the list of directories that point to auxiliary GACs */
static char **extra_gac_paths = NULL;

#ifndef DISABLE_DESKTOP_LOADER

#define FACADE_ASSEMBLY(str) {str, 0, NULL, FALSE, TRUE}

static GHashTable* assembly_remapping_table;
/* The list of system assemblies what will be remapped to the running
 * runtime version.
 * This list is stored in @assembly_remapping_table during initialization.
 * Keep it sorted just to make maintenance easier.
 *
 * The integer number is an index in the MonoRuntimeInfo structure, whose
 * values can be found in domain.c - supported_runtimes. Look there
 * to understand what remapping will be made.
 *
 * .NET version can be found at https://github.com/dotnet/coreclr/blob/master/src/inc/fxretarget.h#L99
 *
 */
static const AssemblyVersionMap framework_assemblies [] = {
	{"Accessibility", 0},
	{"Commons.Xml.Relaxng", 0},
	{"CustomMarshalers", 0},
	{"I18N", 0},
	{"I18N.CJK", 0},
	{"I18N.MidEast", 0},
	{"I18N.Other", 0},
	{"I18N.Rare", 0},
	{"I18N.West", 0},
	{"Microsoft.Build.Engine", 2, NULL, TRUE},
	{"Microsoft.Build.Framework", 2, NULL, TRUE},
	{"Microsoft.Build.Tasks", 2, "Microsoft.Build.Tasks.v4.0"},
	{"Microsoft.Build.Tasks.v3.5", 2, "Microsoft.Build.Tasks.v4.0"},
	{"Microsoft.Build.Utilities", 2, "Microsoft.Build.Utilities.v4.0"},
	{"Microsoft.Build.Utilities.v3.5", 2, "Microsoft.Build.Utilities.v4.0"},
	{"Microsoft.CSharp", 0},
	{"Microsoft.VisualBasic", 1},
	{"Microsoft.VisualC", 1},
	FACADE_ASSEMBLY ("Microsoft.Win32.Primitives"),
	FACADE_ASSEMBLY ("Microsoft.Win32.Registry"),
	FACADE_ASSEMBLY ("Microsoft.Win32.Registry.AccessControl"),
	{"Mono.Cairo", 0},
	{"Mono.CompilerServices.SymbolWriter", 0},
	{"Mono.Data", 0},
	{"Mono.Data.SybaseClient", 0},
	{"Mono.Data.Tds", 0},
	{"Mono.Data.TdsClient", 0},
	{"Mono.GetOptions", 0},
	{"Mono.Http", 0},
	{"Mono.Posix", 0},
	{"Mono.Security", 0},
	{"Mono.Security.Win32", 0},
	{"Mono.Xml.Ext", 0},
	{"Novell.Directory.Ldap", 0},
	{"PEAPI", 0},
	{"System", 0},
	FACADE_ASSEMBLY ("System.AppContext"),
	FACADE_ASSEMBLY ("System.Buffers"),
	FACADE_ASSEMBLY ("System.Collections"),
	FACADE_ASSEMBLY ("System.Collections.Concurrent"),
	FACADE_ASSEMBLY ("System.Collections.NonGeneric"),
	FACADE_ASSEMBLY ("System.Collections.Specialized"),
	FACADE_ASSEMBLY ("System.ComponentModel"),
	FACADE_ASSEMBLY ("System.ComponentModel.Annotations"),
	{"System.ComponentModel.Composition", 2},
	{"System.ComponentModel.DataAnnotations", 2},
	FACADE_ASSEMBLY ("System.ComponentModel.EventBasedAsync"),
	FACADE_ASSEMBLY ("System.ComponentModel.Primitives"),
	FACADE_ASSEMBLY ("System.ComponentModel.TypeConverter"),
	{"System.Configuration", 0},
	{"System.Configuration.Install", 0},
	FACADE_ASSEMBLY ("System.Console"),
	{"System.Core", 2},
	{"System.Data", 0},
	FACADE_ASSEMBLY ("System.Data.Common"),
	{"System.Data.DataSetExtensions", 0},
	{"System.Data.Entity", 0},
	{"System.Data.Linq", 2},
	{"System.Data.OracleClient", 0},
	{"System.Data.Services", 2},
	{"System.Data.Services.Client", 2},
	FACADE_ASSEMBLY ("System.Data.SqlClient"),
	{"System.Data.SqlXml", 0},
	{"System.Deployment", 0},
	{"System.Design", 0},
	FACADE_ASSEMBLY ("System.Diagnostics.Contracts"),
	FACADE_ASSEMBLY ("System.Diagnostics.Debug"),
	FACADE_ASSEMBLY ("System.Diagnostics.FileVersionInfo"),
	FACADE_ASSEMBLY ("System.Diagnostics.Process"),
	FACADE_ASSEMBLY ("System.Diagnostics.StackTrace"),
	FACADE_ASSEMBLY ("System.Diagnostics.TextWriterTraceListener"),
	FACADE_ASSEMBLY ("System.Diagnostics.Tools"),
	FACADE_ASSEMBLY ("System.Diagnostics.TraceEvent"),
	FACADE_ASSEMBLY ("System.Diagnostics.TraceSource"),
	FACADE_ASSEMBLY ("System.Diagnostics.Tracing"),
	{"System.DirectoryServices", 0},
	{"System.DirectoryServices.Protocols", 0},
	{"System.Drawing", 0},
	FACADE_ASSEMBLY ("System.Drawing.Common"),
	{"System.Drawing.Design", 0},
	FACADE_ASSEMBLY ("System.Drawing.Primitives"),
	{"System.Dynamic", 0},
	FACADE_ASSEMBLY ("System.Dynamic.Runtime"),
	{"System.EnterpriseServices", 0},
	FACADE_ASSEMBLY ("System.Globalization"),
	FACADE_ASSEMBLY ("System.Globalization.Calendars"),
	FACADE_ASSEMBLY ("System.Globalization.Extensions"),
	{"System.IdentityModel", 3},
	{"System.IdentityModel.Selectors", 3},
	FACADE_ASSEMBLY ("System.IO"),
	{"System.IO.Compression", 2},
	{"System.IO.Compression.FileSystem", 0},
	FACADE_ASSEMBLY ("System.IO.Compression.ZipFile"),
	FACADE_ASSEMBLY ("System.IO.FileSystem"),
	FACADE_ASSEMBLY ("System.IO.FileSystem.AccessControl"),
	FACADE_ASSEMBLY ("System.IO.FileSystem.DriveInfo"),
	FACADE_ASSEMBLY ("System.IO.FileSystem.Primitives"),
	FACADE_ASSEMBLY ("System.IO.FileSystem.Watcher"),
	FACADE_ASSEMBLY ("System.IO.IsolatedStorage"),
	FACADE_ASSEMBLY ("System.IO.MemoryMappedFiles"),
	FACADE_ASSEMBLY ("System.IO.Packaging"),
	FACADE_ASSEMBLY ("System.IO.Pipes"),
	FACADE_ASSEMBLY ("System.IO.UnmanagedMemoryStream"),
	FACADE_ASSEMBLY ("System.Linq"),
	FACADE_ASSEMBLY ("System.Linq.Expressions"),
	FACADE_ASSEMBLY ("System.Linq.Parallel"),
	FACADE_ASSEMBLY ("System.Linq.Queryable"),
	{"System.Management", 0},
	FACADE_ASSEMBLY ("System.Memory"),
	{"System.Messaging", 0},
	{"System.Net", 2},
	FACADE_ASSEMBLY ("System.Net.AuthenticationManager"),
	FACADE_ASSEMBLY ("System.Net.Cache"),
	{"System.Net.Http", 4},
	{"System.Net.Http.Rtc", 0},
	{"System.Net.Http.WebRequest", 0},
	FACADE_ASSEMBLY ("System.Net.HttpListener"),
	FACADE_ASSEMBLY ("System.Net.Mail"),
	FACADE_ASSEMBLY ("System.Net.NameResolution"),
	FACADE_ASSEMBLY ("System.Net.NetworkInformation"),
	FACADE_ASSEMBLY ("System.Net.Ping"),
	FACADE_ASSEMBLY ("System.Net.Primitives"),
	FACADE_ASSEMBLY ("System.Net.Requests"),
	FACADE_ASSEMBLY ("System.Net.Security"),
	FACADE_ASSEMBLY ("System.Net.ServicePoint"),
	FACADE_ASSEMBLY ("System.Net.Sockets"),
	FACADE_ASSEMBLY ("System.Net.Utilities"),
	FACADE_ASSEMBLY ("System.Net.WebHeaderCollection"),
	FACADE_ASSEMBLY ("System.Net.WebSockets"),
	FACADE_ASSEMBLY ("System.Net.WebSockets.Client"),
	{"System.Numerics", 3},
	{"System.Numerics.Vectors", 3},
	FACADE_ASSEMBLY ("System.ObjectModel"),
	FACADE_ASSEMBLY ("System.Reflection"),
	{"System.Reflection.Context", 0},
	FACADE_ASSEMBLY ("System.Reflection.DispatchProxy"),
	FACADE_ASSEMBLY ("System.Reflection.Emit"),
	FACADE_ASSEMBLY ("System.Reflection.Emit.ILGeneration"),
	FACADE_ASSEMBLY ("System.Reflection.Emit.Lightweight"),
	FACADE_ASSEMBLY ("System.Reflection.Extensions"),
	FACADE_ASSEMBLY ("System.Reflection.Primitives"),
	FACADE_ASSEMBLY ("System.Reflection.TypeExtensions"),
	FACADE_ASSEMBLY ("System.Resources.Reader"),
	FACADE_ASSEMBLY ("System.Resources.ReaderWriter"),
	FACADE_ASSEMBLY ("System.Resources.ResourceManager"),
	FACADE_ASSEMBLY ("System.Resources.Writer"),
	FACADE_ASSEMBLY ("System.Runtime"),
	{"System.Runtime.Caching", 0},
	FACADE_ASSEMBLY ("System.Runtime.CompilerServices.VisualC"),
	{"System.Runtime.DurableInstancing", 0},
	FACADE_ASSEMBLY ("System.Runtime.Extensions"),
	FACADE_ASSEMBLY ("System.Runtime.Handles"),
	FACADE_ASSEMBLY ("System.Runtime.InteropServices"),
	FACADE_ASSEMBLY ("System.Runtime.InteropServices.RuntimeInformation"),
	FACADE_ASSEMBLY ("System.Runtime.InteropServices.WindowsRuntime"),
	FACADE_ASSEMBLY ("System.Runtime.Loader"),
	FACADE_ASSEMBLY ("System.Runtime.Numerics"),
	{"System.Runtime.Remoting", 0},
	{"System.Runtime.Serialization", 3},
	FACADE_ASSEMBLY ("System.Runtime.Serialization.Formatters"),
	{"System.Runtime.Serialization.Formatters.Soap", 0},
	FACADE_ASSEMBLY ("System.Runtime.Serialization.Json"),
	FACADE_ASSEMBLY ("System.Runtime.Serialization.Primitives"),
	FACADE_ASSEMBLY ("System.Runtime.Serialization.Xml"),
	{"System.Security", 0},
	FACADE_ASSEMBLY ("System.Security.AccessControl"),
	FACADE_ASSEMBLY ("System.Security.Claims"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Algorithms"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Cng"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Csp"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.DeriveBytes"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Encoding"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Encryption"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Encryption.Aes"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Encryption.ECDiffieHellman"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Encryption.ECDsa"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Hashing"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Hashing.Algorithms"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.OpenSsl"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Pkcs"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.Primitives"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.ProtectedData"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.RSA"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.RandomNumberGenerator"),
	FACADE_ASSEMBLY ("System.Security.Cryptography.X509Certificates"),
	FACADE_ASSEMBLY ("System.Security.Principal"),
	FACADE_ASSEMBLY ("System.Security.Principal.Windows"),
	FACADE_ASSEMBLY ("System.Security.SecureString"),
	{"System.ServiceModel", 3},
	{"System.ServiceModel.Activation", 0},
	{"System.ServiceModel.Discovery", 0},
	FACADE_ASSEMBLY ("System.ServiceModel.Duplex"),
	FACADE_ASSEMBLY ("System.ServiceModel.Http"),
	FACADE_ASSEMBLY ("System.ServiceModel.NetTcp"),
	FACADE_ASSEMBLY ("System.ServiceModel.Primitives"),
	{"System.ServiceModel.Routing", 0},
	FACADE_ASSEMBLY ("System.ServiceModel.Security"),
	{"System.ServiceModel.Web", 2},
	{"System.ServiceProcess", 0},
	FACADE_ASSEMBLY ("System.ServiceProcess.ServiceController"),
	FACADE_ASSEMBLY ("System.Text.Encoding"),
	FACADE_ASSEMBLY ("System.Text.Encoding.CodePages"),
	FACADE_ASSEMBLY ("System.Text.Encoding.Extensions"),
	FACADE_ASSEMBLY ("System.Text.RegularExpressions"),
	FACADE_ASSEMBLY ("System.Threading"),
	FACADE_ASSEMBLY ("System.Threading.AccessControl"),
	FACADE_ASSEMBLY ("System.Threading.Overlapped"),
	FACADE_ASSEMBLY ("System.Threading.Tasks"),
	{"System.Threading.Tasks.Dataflow", 0},
	FACADE_ASSEMBLY ("System.Threading.Tasks.Extensions"),
	FACADE_ASSEMBLY ("System.Threading.Tasks.Parallel"),
	FACADE_ASSEMBLY ("System.Threading.Thread"),
	FACADE_ASSEMBLY ("System.Threading.ThreadPool"),
	FACADE_ASSEMBLY ("System.Threading.Timer"),
	{"System.Transactions", 0},
	FACADE_ASSEMBLY ("System.ValueTuple"),
	{"System.Web", 0},
	{"System.Web.Abstractions", 2},
	{"System.Web.ApplicationServices", 0},
	{"System.Web.DynamicData", 2},
	{"System.Web.Extensions", 2},
	{"System.Web.Extensions.Design", 0},
	{"System.Web.Mobile", 0},
	{"System.Web.RegularExpressions", 0},
	{"System.Web.Routing", 2},
	{"System.Web.Services", 0},
	{"System.Windows", 0},
	{"System.Windows.Forms", 0},
	{"System.Windows.Forms.DataVisualization", 0},
	{"System.Workflow.Activities", 0},
	{"System.Workflow.ComponentModel", 0},
	{"System.Workflow.Runtime", 0},
	{"System.Xaml", 0},
	{"System.Xml", 0},
	{"System.Xml.Linq", 2},
	FACADE_ASSEMBLY ("System.Xml.ReaderWriter"),
	{"System.Xml.Serialization", 0},
	FACADE_ASSEMBLY ("System.Xml.XDocument"),
	FACADE_ASSEMBLY ("System.Xml.XPath"),
	FACADE_ASSEMBLY ("System.Xml.XPath.XmlDocument"),
	FACADE_ASSEMBLY ("System.Xml.XPath.XDocument"),
	FACADE_ASSEMBLY ("System.Xml.XmlDocument"),
	FACADE_ASSEMBLY ("System.Xml.XmlSerializer"),
	FACADE_ASSEMBLY ("System.Xml.Xsl.Primitives"),
	{"WindowsBase", 3},
	{"cscompmgd", 0},
	{"mscorlib", 0},
	FACADE_ASSEMBLY ("netstandard"),
};
#endif

/*
 * keeps track of loaded assemblies
 */
static GList *loaded_assemblies = NULL;
static MonoAssembly *corlib;

static char* unquote (const char *str);

/* This protects loaded_assemblies and image->references */
#define mono_assemblies_lock() mono_os_mutex_lock (&assemblies_mutex)
#define mono_assemblies_unlock() mono_os_mutex_unlock (&assemblies_mutex)
static mono_mutex_t assemblies_mutex;

/* If defined, points to the bundled assembly information */
static const MonoBundledAssembly **bundles;

static mono_mutex_t assembly_binding_mutex;

/* Loaded assembly binding info */
static GSList *loaded_assembly_bindings = NULL;

/* Class lazy loading functions */
static GENERATE_TRY_GET_CLASS_WITH_CACHE (internals_visible, "System.Runtime.CompilerServices", "InternalsVisibleToAttribute")
static MonoAssembly*
mono_assembly_invoke_search_hook_internal (MonoAssemblyName *aname, MonoAssembly *requesting, gboolean refonly, gboolean postload);
static MonoAssembly*
mono_assembly_request_byname_nosearch (MonoAssemblyName *aname, const MonoAssemblyByNameRequest *req, MonoImageOpenStatus *status);
static MonoAssembly*
mono_assembly_load_full_gac_base_default (MonoAssemblyName *aname, const char *basedir, MonoAssemblyContextKind asmctx, MonoImageOpenStatus *status);
static MonoAssembly*
chain_redirections_loadfrom (MonoImage *image, MonoImageOpenStatus *status);
static MonoAssembly*
mono_problematic_image_reprobe (MonoImage *image, MonoImageOpenStatus *status);

static MonoBoolean
mono_assembly_is_in_gac (const gchar *filanem);
static MonoAssemblyName*
mono_assembly_apply_binding (MonoAssemblyName *aname, MonoAssemblyName *dest_name);

static MonoAssembly*
prevent_reference_assembly_from_running (MonoAssembly* candidate, gboolean refonly);

/* Assembly name matching */
static gboolean
exact_sn_match (MonoAssemblyName *wanted_name, MonoAssemblyName *candidate_name);
static gboolean
framework_assembly_sn_match (MonoAssemblyName *wanted_name, MonoAssemblyName *candidate_name);

static const char *
mono_asmctx_get_name (const MonoAssemblyContext *asmctx);

static gboolean
assembly_loadfrom_asmctx_from_path (const char *filename, MonoAssembly *requesting_assembly, gpointer user_data, MonoAssemblyContextKind *out_asmctx);

static gchar*
encode_public_tok (const guchar *token, gint32 len)
{
	const static gchar allowed [] = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
	gchar *res;
	int i;

	res = (gchar *)g_malloc (len * 2 + 1);
	for (i = 0; i < len; i++) {
		res [i * 2] = allowed [token [i] >> 4];
		res [i * 2 + 1] = allowed [token [i] & 0xF];
	}
	res [len * 2] = 0;
	return res;
}

/**
 * mono_public_tokens_are_equal:
 * \param pubt1 first public key token
 * \param pubt2 second public key token
 *
 * Compare two public key tokens and return TRUE is they are equal and FALSE
 * otherwise.
 */
gboolean
mono_public_tokens_are_equal (const unsigned char *pubt1, const unsigned char *pubt2)
{
	return memcmp (pubt1, pubt2, 16) == 0;
}

/**
 * mono_set_assemblies_path:
 * \param path list of paths that contain directories where Mono will look for assemblies
 *
 * Use this method to override the standard assembly lookup system and
 * override any assemblies coming from the GAC.  This is the method
 * that supports the \c MONO_PATH variable.
 *
 * Notice that \c MONO_PATH and this method are really a very bad idea as
 * it prevents the GAC from working and it prevents the standard
 * resolution mechanisms from working.  Nonetheless, for some debugging
 * situations and bootstrapping setups, this is useful to have. 
 */
void
mono_set_assemblies_path (const char* path)
{
	char **splitted, **dest;

	splitted = g_strsplit (path, G_SEARCHPATH_SEPARATOR_S, 1000);
	if (assemblies_path)
		g_strfreev (assemblies_path);
	assemblies_path = dest = splitted;
	while (*splitted) {
		char *tmp = *splitted;
		if (*tmp)
			*dest++ = mono_path_canonicalize (tmp);
		g_free (tmp);
		splitted++;
	}
	*dest = *splitted;

	if (g_hasenv ("MONO_DEBUG"))
		return;

	splitted = assemblies_path;
	while (*splitted) {
		if (**splitted && !g_file_test (*splitted, G_FILE_TEST_IS_DIR))
			g_warning ("'%s' in MONO_PATH doesn't exist or has wrong permissions.", *splitted);

		splitted++;
	}
}

static void
check_path_env (void)
{
	if (assemblies_path != NULL)
		return;

	char* path = g_getenv ("MONO_PATH");
	if (!path)
		return;

	mono_set_assemblies_path(path);
	g_free (path);
}

static void
check_extra_gac_path_env (void) 
{
	char *path;
	char **splitted, **dest;
	
	path = g_getenv ("MONO_GAC_PREFIX");
	if (!path)
		return;

	splitted = g_strsplit (path, G_SEARCHPATH_SEPARATOR_S, 1000);
	g_free (path);

	if (extra_gac_paths)
		g_strfreev (extra_gac_paths);
	extra_gac_paths = dest = splitted;
	while (*splitted){
		if (**splitted)
			*dest++ = *splitted;
		splitted++;
	}
	*dest = *splitted;
	
	if (!g_hasenv ("MONO_DEBUG"))
		return;

	while (*splitted) {
		if (**splitted && !g_file_test (*splitted, G_FILE_TEST_IS_DIR))
			g_warning ("'%s' in MONO_GAC_PREFIX doesn't exist or has wrong permissions.", *splitted);

		splitted++;
	}
}

static gboolean
assembly_binding_maps_name (MonoAssemblyBindingInfo *info, MonoAssemblyName *aname)
{
	if (!info || !info->name)
		return FALSE;

	if (strcmp (info->name, aname->name))
		return FALSE;

	if (info->major != aname->major || info->minor != aname->minor)
		return FALSE;

	if ((info->culture != NULL && info->culture [0]) != (aname->culture != NULL && aname->culture [0])) 
		return FALSE;
	
	if (info->culture && aname->culture && strcmp (info->culture, aname->culture))
		return FALSE;
	
	if (!mono_public_tokens_are_equal (info->public_key_token, aname->public_key_token))
		return FALSE;

	return TRUE;
}

static void
mono_assembly_binding_info_free (MonoAssemblyBindingInfo *info)
{
	if (!info)
		return;

	g_free (info->name);
	g_free (info->culture);
}

static void
get_publisher_policy_info (MonoImage *image, MonoAssemblyName *aname, MonoAssemblyBindingInfo *binding_info)
{
	MonoTableInfo *t;
	guint32 cols [MONO_MANIFEST_SIZE];
	const gchar *filename;
	gchar *subpath, *fullpath;

	t = &image->tables [MONO_TABLE_MANIFESTRESOURCE];
	/* MS Impl. accepts policy assemblies with more than
	 * one manifest resource, and only takes the first one */
	if (t->rows < 1) {
		binding_info->is_valid = FALSE;
		return;
	}
	
	mono_metadata_decode_row (t, 0, cols, MONO_MANIFEST_SIZE);
	if ((cols [MONO_MANIFEST_IMPLEMENTATION] & MONO_IMPLEMENTATION_MASK) != MONO_IMPLEMENTATION_FILE) {
		binding_info->is_valid = FALSE;
		return;
	}
	
	filename = mono_metadata_string_heap (image, cols [MONO_MANIFEST_NAME]);
	g_assert (filename != NULL);
	
	subpath = g_path_get_dirname (image->name);
	fullpath = g_build_path (G_DIR_SEPARATOR_S, subpath, filename, NULL);
	mono_config_parse_publisher_policy (fullpath, binding_info);
	g_free (subpath);
	g_free (fullpath);
	
	/* Define the optional elements/attributes before checking */
	if (!binding_info->culture)
		binding_info->culture = g_strdup ("");
	
	/* Check that the most important elements/attributes exist */
	if (!binding_info->name || !binding_info->public_key_token [0] || !binding_info->has_old_version_bottom ||
			!binding_info->has_new_version || !assembly_binding_maps_name (binding_info, aname)) {
		mono_assembly_binding_info_free (binding_info);
		binding_info->is_valid = FALSE;
		return;
	}

	binding_info->is_valid = TRUE;
}

static int
compare_versions (AssemblyVersionSet *v, MonoAssemblyName *aname)
{
	if (v->major > aname->major)
		return 1;
	else if (v->major < aname->major)
		return -1;

	if (v->minor > aname->minor)
		return 1;
	else if (v->minor < aname->minor)
		return -1;

	if (v->build > aname->build)
		return 1;
	else if (v->build < aname->build)
		return -1;

	if (v->revision > aname->revision)
		return 1;
	else if (v->revision < aname->revision)
		return -1;

	return 0;
}

static gboolean
check_policy_versions (MonoAssemblyBindingInfo *info, MonoAssemblyName *name)
{
	if (!info->is_valid)
		return FALSE;
	
	/* If has_old_version_top doesn't exist, we don't have an interval */
	if (!info->has_old_version_top) {
		if (compare_versions (&info->old_version_bottom, name) == 0)
			return TRUE;

		return FALSE;
	}

	/* Check that the version defined by name is valid for the interval */
	if (compare_versions (&info->old_version_top, name) < 0)
		return FALSE;

	/* We should be greater or equal than the small version */
	if (compare_versions (&info->old_version_bottom, name) > 0)
		return FALSE;

	return TRUE;
}

/**
 * mono_assembly_names_equal:
 * \param l first assembly
 * \param r second assembly.
 *
 * Compares two \c MonoAssemblyName instances and returns whether they are equal.
 *
 * This compares the names, the cultures, the release version and their
 * public tokens.
 *
 * \returns TRUE if both assembly names are equal.
 */
gboolean
mono_assembly_names_equal (MonoAssemblyName *l, MonoAssemblyName *r)
{
	return mono_assembly_names_equal_flags (l, r, MONO_ANAME_EQ_NONE);
}

/**
 * mono_assembly_names_equal_flags:
 * \param l first assembly name
 * \param r second assembly name
 * \param flags flags that affect what is compared.
 *
 * Compares two \c MonoAssemblyName instances and returns whether they are equal.
 *
 * This compares the simple names and cultures and optionally the versions and
 * public key tokens, depending on the \c flags.
 *
 * \returns TRUE if both assembly names are equal.
 */
gboolean
mono_assembly_names_equal_flags (MonoAssemblyName *l, MonoAssemblyName *r, MonoAssemblyNameEqFlags flags)
{
	g_assert (l != NULL);
	g_assert (r != NULL);

	if (!l->name || !r->name)
		return FALSE;

	if ((flags & MONO_ANAME_EQ_IGNORE_CASE) != 0 && g_strcasecmp (l->name, r->name))
		return FALSE;

	if ((flags & MONO_ANAME_EQ_IGNORE_CASE) == 0 && strcmp (l->name, r->name))
		return FALSE;

	if (l->culture && r->culture && strcmp (l->culture, r->culture))
		return FALSE;

	if ((l->major != r->major || l->minor != r->minor ||
	     l->build != r->build || l->revision != r->revision) &&
	    (flags & MONO_ANAME_EQ_IGNORE_VERSION) == 0)
		if (! ((l->major == 0 && l->minor == 0 && l->build == 0 && l->revision == 0) || (r->major == 0 && r->minor == 0 && r->build == 0 && r->revision == 0)))
			return FALSE;

	if (!l->public_key_token [0] || !r->public_key_token [0] || (flags & MONO_ANAME_EQ_IGNORE_PUBKEY) != 0)
		return TRUE;

	if (!mono_public_tokens_are_equal (l->public_key_token, r->public_key_token))
		return FALSE;

	return TRUE;
}

/**
 * assembly_names_compare_versions:
 * \param l left assembly name
 * \param r right assembly name
 * \param maxcomps how many version components to compare, or -1 to compare all.
 *
 * \returns a negative if \p l is a lower version than \p r; a positive value
 * if \p r is a lower version than \p l, or zero if \p l and \p r are equal
 * versions (comparing upto \p maxcomps components).
 *
 * Components are \c major, \c minor, \c revision, and \c build. \p maxcomps 1 means just compare
 * majors. 2 means majors then minors. etc.
 */
static int
assembly_names_compare_versions (MonoAssemblyName *l, MonoAssemblyName *r, int maxcomps)
{
	int i = 0;
	if (maxcomps < 0) maxcomps = 4;
#define CMP(field) do {				\
		if (l-> field < r-> field && i < maxcomps) return -1;	\
		if (l-> field > r-> field && i < maxcomps) return 1;	\
	} while (0)
	CMP (major);
	++i;
	CMP (minor);
	++i;
	CMP (revision);
	++i;
	CMP (build);
#undef CMP
	return 0;
}

/**
 * mono_assembly_request_prepare:
 * \param req the request to be initialized
 * \param req_size the size of the request structure
 * \param asmctx the assembly load context kind
 *
 * Initialize an assembly loader request.  The passed structure \p req must be
 * of size \p req_size.  Its state will be reset and the assembly context kind will be prefilled with \p asmctx.
 */
void
mono_assembly_request_prepare (MonoAssemblyLoadRequest *req, size_t req_size, MonoAssemblyContextKind asmctx)
{
	memset (req, 0, req_size);
	req->asmctx = asmctx;
}

static MonoAssembly *
load_in_path (const char *basename, const char** search_path, const MonoAssemblyOpenRequest *req, MonoImageOpenStatus *status)
{
	int i;
	char *fullpath;
	MonoAssembly *result;

	for (i = 0; search_path [i]; ++i) {
		fullpath = g_build_filename (search_path [i], basename, NULL);
		result = mono_assembly_request_open (fullpath, req, status);
		g_free (fullpath);
		if (result)
			return result;
	}
	return NULL;
}

/**
 * mono_assembly_setrootdir:
 * \param root_dir The pathname of the root directory where we will locate assemblies
 *
 * This routine sets the internal default root directory for looking up
 * assemblies.
 *
 * This is used by Windows installations to compute dynamically the
 * place where the Mono assemblies are located.
 *
 */
void
mono_assembly_setrootdir (const char *root_dir)
{
	/*
	 * Override the MONO_ASSEMBLIES directory configured at compile time.
	 */
	if (default_path [0])
		g_free (default_path [0]);
	default_path [0] = g_strdup (root_dir);
}

/**
 * mono_assembly_getrootdir:
 * 
 * Obtains the root directory used for looking up assemblies.
 *
 * Returns: a string with the directory, this string should not be freed.
 */
G_CONST_RETURN gchar *
mono_assembly_getrootdir (void)
{
	return default_path [0];
}

/**
 * mono_native_getrootdir:
 * 
 * Obtains the root directory used for looking up native libs (.so, .dylib).
 *
 * Returns: a string with the directory, this string should be freed by
 * the caller.
 */
gchar *
mono_native_getrootdir (void)
{
	gchar* fullpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (), mono_config_get_reloc_lib_dir(), NULL);
	return fullpath;
}

/**
 * mono_set_dirs:
 * \param assembly_dir the base directory for assemblies
 * \param config_dir the base directory for configuration files
 *
 * This routine is used internally and by developers embedding
 * the runtime into their own applications.
 *
 * There are a number of cases to consider: Mono as a system-installed
 * package that is available on the location preconfigured or Mono in
 * a relocated location.
 *
 * If you are using a system-installed Mono, you can pass NULL
 * to both parameters.  If you are not, you should compute both
 * directory values and call this routine.
 *
 * The values for a given PREFIX are:
 *
 *    assembly_dir: PREFIX/lib
 *    config_dir:   PREFIX/etc
 *
 * Notice that embedders that use Mono in a relocated way must
 * compute the location at runtime, as they will be in control
 * of where Mono is installed.
 */
void
mono_set_dirs (const char *assembly_dir, const char *config_dir)
{
	if (assembly_dir == NULL)
		assembly_dir = mono_config_get_assemblies_dir ();
	if (config_dir == NULL)
		config_dir = mono_config_get_cfg_dir ();
	mono_assembly_setrootdir (assembly_dir);
	mono_set_config_dir (config_dir);
}

#ifndef HOST_WIN32

static char *
compute_base (char *path)
{
	char *p = strrchr (path, '/');
	if (p == NULL)
		return NULL;

	/* Not a well known Mono executable, we are embedded, cant guess the base  */
	if (strcmp (p, "/mono") && strcmp (p, "/mono-boehm") && strcmp (p, "/mono-sgen") && strcmp (p, "/pedump") && strcmp (p, "/monodis"))
		return NULL;
	    
	*p = 0;
	p = strrchr (path, '/');
	if (p == NULL)
		return NULL;
	
	if (strcmp (p, "/bin") != 0)
		return NULL;
	*p = 0;
	return path;
}

static void
fallback (void)
{
	mono_set_dirs (mono_config_get_assemblies_dir (), mono_config_get_cfg_dir ());
}

static G_GNUC_UNUSED void
set_dirs (char *exe)
{
	char *base;
	char *config, *lib, *mono;
	struct stat buf;
	const char *bindir;
	
	/*
	 * Only /usr prefix is treated specially
	 */
	bindir = mono_config_get_bin_dir ();
	g_assert (bindir);
	if (strncmp (exe, bindir, strlen (bindir)) == 0 || (base = compute_base (exe)) == NULL){
		fallback ();
		return;
	}

	config = g_build_filename (base, "etc", NULL);
	lib = g_build_filename (base, "lib", NULL);
	mono = g_build_filename (lib, "mono/4.5", NULL);  // FIXME: stop hardcoding 4.5 here
	if (stat (mono, &buf) == -1)
		fallback ();
	else {
		mono_set_dirs (lib, config);
	}
	
	g_free (config);
	g_free (lib);
	g_free (mono);
}

#endif /* HOST_WIN32 */

/**
 * mono_set_rootdir:
 *
 * Registers the root directory for the Mono runtime, for Linux and Solaris 10,
 * this auto-detects the prefix where Mono was installed. 
 */
void
mono_set_rootdir (void)
{
#if defined(HOST_WIN32) || (defined(HOST_DARWIN) && !defined(TARGET_ARM))
	gchar *bindir, *installdir, *root, *name, *resolvedname, *config;

#ifdef HOST_WIN32
	name = mono_get_module_file_name ((HMODULE) &__ImageBase);
#else
 	{
		/* 
		 * _NSGetExecutablePath may return -1 to indicate buf is not large
		 *  enough, but we ignore that case to avoid having to do extra dynamic
		 *  allocation for the path and hope that 4096 is enough - this is 
		 *  ok in the Linux/Solaris case below at least...
		 */
 		
		gchar buf[4096];
 		guint buf_size = sizeof (buf);
 
		name = NULL;
 		if (_NSGetExecutablePath (buf, &buf_size) == 0)
 			name = g_strdup (buf);
 
 		if (name == NULL) {
 			fallback ();
 			return;
 		}
 	}
#endif

	resolvedname = mono_path_resolve_symlinks (name);

	bindir = g_path_get_dirname (resolvedname);
	installdir = g_path_get_dirname (bindir);
	root = g_build_path (G_DIR_SEPARATOR_S, installdir, "lib", NULL);

	config = g_build_filename (root, "..", "etc", NULL);
#ifdef HOST_WIN32
	mono_set_dirs (root, config);
#else
	if (g_file_test (root, G_FILE_TEST_EXISTS) && g_file_test (config, G_FILE_TEST_EXISTS))
		mono_set_dirs (root, config);
	else
		fallback ();
#endif

	g_free (config);
	g_free (root);
	g_free (installdir);
	g_free (bindir);
	g_free (name);
	g_free (resolvedname);
#elif defined(DISABLE_MONO_AUTODETECTION)
	fallback ();
#else
	char buf [4096];
	int  s;
	char *str;

#if defined(HAVE_READLINK)
	/* Linux style */
	s = readlink ("/proc/self/exe", buf, sizeof (buf)-1);
#else
	s = -1;
#endif

	if (s != -1){
		buf [s] = 0;
		set_dirs (buf);
		return;
	}

	/* Solaris 10 style */
	str = g_strdup_printf ("/proc/%d/path/a.out", getpid ());

#if defined(HAVE_READLINK)
	s = readlink (str, buf, sizeof (buf)-1);
#else
	s = -1;
#endif

	g_free (str);
	if (s != -1){
		buf [s] = 0;
		set_dirs (buf);
		return;
	} 
	fallback ();
#endif
}

/**
 * mono_assemblies_init:
 *
 *  Initialize global variables used by this module.
 */
void
mono_assemblies_init (void)
{
	/*
	 * Initialize our internal paths if we have not been initialized yet.
	 * This happens when embedders use Mono.
	 */
	if (mono_assembly_getrootdir () == NULL)
		mono_set_rootdir ();

	check_path_env ();
	check_extra_gac_path_env ();

	mono_os_mutex_init_recursive (&assemblies_mutex);
	mono_os_mutex_init (&assembly_binding_mutex);

#ifndef DISABLE_DESKTOP_LOADER
	assembly_remapping_table = g_hash_table_new (g_str_hash, g_str_equal);

	int i;
	for (i = 0; i < G_N_ELEMENTS (framework_assemblies); ++i)
		g_hash_table_insert (assembly_remapping_table, (void*)framework_assemblies [i].assembly_name, (void*)&framework_assemblies [i]);

#endif
	mono_install_assembly_asmctx_from_path_hook (assembly_loadfrom_asmctx_from_path, NULL);

}

static void
mono_assembly_binding_lock (void)
{
	mono_locks_os_acquire (&assembly_binding_mutex, AssemblyBindingLock);
}

static void
mono_assembly_binding_unlock (void)
{
	mono_locks_os_release (&assembly_binding_mutex, AssemblyBindingLock);
}

gboolean
mono_assembly_fill_assembly_name_full (MonoImage *image, MonoAssemblyName *aname, gboolean copyBlobs)
{
	MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLY];
	guint32 cols [MONO_ASSEMBLY_SIZE];
	gint32 machine, flags;

	if (!t->rows)
		return FALSE;

	mono_metadata_decode_row (t, 0, cols, MONO_ASSEMBLY_SIZE);

	aname->hash_len = 0;
	aname->hash_value = NULL;
	aname->name = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_NAME]);
	if (copyBlobs)
		aname->name = g_strdup (aname->name);
	aname->culture = mono_metadata_string_heap (image, cols [MONO_ASSEMBLY_CULTURE]);
	if (copyBlobs)
		aname->culture = g_strdup (aname->culture);
	aname->flags = cols [MONO_ASSEMBLY_FLAGS];
	aname->major = cols [MONO_ASSEMBLY_MAJOR_VERSION];
	aname->minor = cols [MONO_ASSEMBLY_MINOR_VERSION];
	aname->build = cols [MONO_ASSEMBLY_BUILD_NUMBER];
	aname->revision = cols [MONO_ASSEMBLY_REV_NUMBER];
	aname->hash_alg = cols [MONO_ASSEMBLY_HASH_ALG];
	if (cols [MONO_ASSEMBLY_PUBLIC_KEY]) {
		guchar* token = (guchar *)g_malloc (8);
		gchar* encoded;
		const gchar* pkey;
		int len;

		pkey = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
		len = mono_metadata_decode_blob_size (pkey, &pkey);
		aname->public_key = (guchar*)pkey;

		mono_digest_get_public_token (token, aname->public_key, len);
		encoded = encode_public_tok (token, 8);
		g_strlcpy ((char*)aname->public_key_token, encoded, MONO_PUBLIC_KEY_TOKEN_LENGTH);

		g_free (encoded);
		g_free (token);
	}
	else {
		aname->public_key = NULL;
		memset (aname->public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}

	if (cols [MONO_ASSEMBLY_PUBLIC_KEY]) {
		aname->public_key = (guchar*)mono_metadata_blob_heap (image, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
		if (copyBlobs) {
			const gchar *pkey_end;
			int len = mono_metadata_decode_blob_size ((const gchar*) aname->public_key, &pkey_end);
			pkey_end += len; /* move to end */
			size_t size = pkey_end - (const gchar*)aname->public_key;
			guchar *tmp = g_new (guchar, size);
			memcpy (tmp, aname->public_key, size);
			aname->public_key = tmp;
		}

	}
	else
		aname->public_key = 0;

	machine = image->image_info->cli_header.coff.coff_machine;
	flags = image->image_info->cli_cli_header.ch_flags;
	switch (machine) {
	case COFF_MACHINE_I386:
		/* https://bugzilla.xamarin.com/show_bug.cgi?id=17632 */
		if (flags & (CLI_FLAGS_32BITREQUIRED|CLI_FLAGS_PREFERRED32BIT))
			aname->arch = MONO_PROCESSOR_ARCHITECTURE_X86;
		else if ((flags & 0x70) == 0x70)
			aname->arch = MONO_PROCESSOR_ARCHITECTURE_NONE;
		else
			aname->arch = MONO_PROCESSOR_ARCHITECTURE_MSIL;
		break;
	case COFF_MACHINE_IA64:
		aname->arch = MONO_PROCESSOR_ARCHITECTURE_IA64;
		break;
	case COFF_MACHINE_AMD64:
		aname->arch = MONO_PROCESSOR_ARCHITECTURE_AMD64;
		break;
	case COFF_MACHINE_ARM:
		aname->arch = MONO_PROCESSOR_ARCHITECTURE_ARM;
		break;
	default:
		break;
	}

	return TRUE;
}

/**
 * mono_assembly_fill_assembly_name:
 * \param image Image
 * \param aname Name
 * \returns TRUE if successful
 */
gboolean
mono_assembly_fill_assembly_name (MonoImage *image, MonoAssemblyName *aname)
{
	return mono_assembly_fill_assembly_name_full (image, aname, FALSE);
}

/**
 * mono_stringify_assembly_name:
 * \param aname the assembly name.
 *
 * Convert \p aname into its string format. The returned string is dynamically
 * allocated and should be freed by the caller.
 *
 * \returns a newly allocated string with a string representation of
 * the assembly name.
 */
char*
mono_stringify_assembly_name (MonoAssemblyName *aname)
{
	const char *quote = (aname->name && g_ascii_isspace (aname->name [0])) ? "\"" : "";

	return g_strdup_printf (
		"%s%s%s, Version=%d.%d.%d.%d, Culture=%s, PublicKeyToken=%s%s",
		quote, aname->name, quote,
		aname->major, aname->minor, aname->build, aname->revision,
		aname->culture && *aname->culture? aname->culture: "neutral",
		aname->public_key_token [0] ? (char *)aname->public_key_token : "null",
		(aname->flags & ASSEMBLYREF_RETARGETABLE_FLAG) ? ", Retargetable=Yes" : "");
}

static gchar*
assemblyref_public_tok (MonoImage *image, guint32 key_index, guint32 flags)
{
	const gchar *public_tok;
	int len;

	public_tok = mono_metadata_blob_heap (image, key_index);
	len = mono_metadata_decode_blob_size (public_tok, &public_tok);

	if (flags & ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG) {
		guchar token [8];
		mono_digest_get_public_token (token, (guchar*)public_tok, len);
		return encode_public_tok (token, 8);
	}

	return encode_public_tok ((guchar*)public_tok, len);
}

static gchar*
assemblyref_public_tok_checked (MonoImage *image, guint32 key_index, guint32 flags, MonoError *error)
{
	const gchar *public_tok;
	int len;

	public_tok = mono_metadata_blob_heap_checked (image, key_index, error);
	return_val_if_nok (error, NULL);
	len = mono_metadata_decode_blob_size (public_tok, &public_tok);

	if (flags & ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG) {
		guchar token [8];
		mono_digest_get_public_token (token, (guchar*)public_tok, len);
		return encode_public_tok (token, 8);
	}
	return encode_public_tok ((guchar*)public_tok, len);
}

/**
 * mono_assembly_addref:
 * \param assembly the assembly to reference
 *
 * This routine increments the reference count on a MonoAssembly.
 * The reference count is reduced every time the method mono_assembly_close() is
 * invoked.
 */
void
mono_assembly_addref (MonoAssembly *assembly)
{
	mono_atomic_inc_i32 (&assembly->ref_count);
}

/*
 * CAUTION: This table must be kept in sync with
 *          ivkm/reflect/Fusion.cs
 */

#define SILVERLIGHT_KEY "7cec85d7bea7798e"
#define WINFX_KEY "31bf3856ad364e35"
#define ECMA_KEY "b77a5c561934e089"
#define MSFINAL_KEY "b03f5f7f11d50a3a"
#define COMPACTFRAMEWORK_KEY "969db8053d3322ac"

typedef struct {
	const char *name;
	const char *from;
	const char *to;
} KeyRemapEntry;

static KeyRemapEntry key_remap_table[] = {
	{ "CustomMarshalers", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	{ "Microsoft.CSharp", WINFX_KEY, MSFINAL_KEY },
	{ "Microsoft.VisualBasic", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	{ "System", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.ComponentModel.Composition", WINFX_KEY, ECMA_KEY },
	{ "System.ComponentModel.DataAnnotations", "ddd0da4d3e678217", WINFX_KEY },
	{ "System.Core", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.Core", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Data", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Data.DataSetExtensions", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Drawing", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	{ "System.Messaging", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	// FIXME: MS uses MSFINAL_KEY for .NET 4.5
	{ "System.Net", SILVERLIGHT_KEY, MSFINAL_KEY },
	{ "System.Numerics", WINFX_KEY, ECMA_KEY },
	{ "System.Runtime.Serialization", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.Runtime.Serialization", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.ServiceModel", WINFX_KEY, ECMA_KEY },
	{ "System.ServiceModel", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.ServiceModel.Web", SILVERLIGHT_KEY, WINFX_KEY },
	{ "System.Web.Services", COMPACTFRAMEWORK_KEY, MSFINAL_KEY },
	{ "System.Windows", SILVERLIGHT_KEY, MSFINAL_KEY },
	{ "System.Windows.Forms", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Xml", SILVERLIGHT_KEY, ECMA_KEY },
	{ "System.Xml", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Xml.Linq", WINFX_KEY, ECMA_KEY },
	{ "System.Xml.Linq", COMPACTFRAMEWORK_KEY, ECMA_KEY },
	{ "System.Xml.Serialization", WINFX_KEY, ECMA_KEY }
};

static void
remap_keys (MonoAssemblyName *aname)
{
	int i;
	for (i = 0; i < G_N_ELEMENTS (key_remap_table); i++) {
		const KeyRemapEntry *entry = &key_remap_table [i];

		if (strcmp (aname->name, entry->name) ||
		    !mono_public_tokens_are_equal (aname->public_key_token, (const unsigned char*) entry->from))
			continue;

		memcpy (aname->public_key_token, entry->to, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		     
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
			    "Remapped public key token of retargetable assembly %s from %s to %s",
			    aname->name, entry->from, entry->to);
		return;
	}
}

static MonoAssemblyName *
mono_assembly_remap_version (MonoAssemblyName *aname, MonoAssemblyName *dest_aname)
{
	const MonoRuntimeInfo *current_runtime;

	if (aname->name == NULL) return aname;

	current_runtime = mono_get_runtime_info ();

	if (aname->flags & ASSEMBLYREF_RETARGETABLE_FLAG) {
		const AssemblyVersionSet* vset;

		/* Remap to current runtime */
		vset = &current_runtime->version_sets [0];

		memcpy (dest_aname, aname, sizeof(MonoAssemblyName));
		dest_aname->major = vset->major;
		dest_aname->minor = vset->minor;
		dest_aname->build = vset->build;
		dest_aname->revision = vset->revision;
		dest_aname->flags &= ~ASSEMBLYREF_RETARGETABLE_FLAG;

		/* Remap assembly name */
		if (!strcmp (aname->name, "System.Net"))
			dest_aname->name = g_strdup ("System");
		
		remap_keys (dest_aname);

		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
					"The request to load the retargetable assembly %s v%d.%d.%d.%d was remapped to %s v%d.%d.%d.%d",
					aname->name,
					aname->major, aname->minor, aname->build, aname->revision,
					dest_aname->name,
					vset->major, vset->minor, vset->build, vset->revision
					);

		return dest_aname;
	}
	
#ifndef DISABLE_DESKTOP_LOADER
	const AssemblyVersionMap *vmap = (AssemblyVersionMap *)g_hash_table_lookup (assembly_remapping_table, aname->name);
	if (vmap) {
		const AssemblyVersionSet* vset;
		int index = vmap->version_set_index;
		g_assert (index < G_N_ELEMENTS (current_runtime->version_sets));
		vset = &current_runtime->version_sets [index];

		if (vmap->framework_facade_assembly) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly %s is a framework Facade asseembly",
				    aname->name);
			return aname;
		}

		if (aname->major == vset->major && aname->minor == vset->minor &&
			aname->build == vset->build && aname->revision == vset->revision) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Found assembly remapping for %s and was for the same version %d.%d.%d.%d",
				aname->name,
				aname->major, aname->minor, aname->build, aname->revision);
			return aname;
		}

		if (vmap->only_lower_versions && compare_versions ((AssemblyVersionSet*)vset, aname) < 0) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY,
				"Found lower-versions-only assembly remaping to load %s %d.%d.%d.%d but mapping has %d.%d.%d.%d",
						aname->name,
						aname->major, aname->minor, aname->build, aname->revision,
						vset->major, vset->minor, vset->build, vset->revision
						);
			return aname;
		}

		if ((aname->major | aname->minor | aname->build | aname->revision) != 0)
			mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY,
				"The request to load the assembly %s v%d.%d.%d.%d was remapped to v%d.%d.%d.%d",
						aname->name,
						aname->major, aname->minor, aname->build, aname->revision,
						vset->major, vset->minor, vset->build, vset->revision
						);

		memcpy (dest_aname, aname, sizeof(MonoAssemblyName));
		dest_aname->major = vset->major;
		dest_aname->minor = vset->minor;
		dest_aname->build = vset->build;
		dest_aname->revision = vset->revision;
		if (vmap->new_assembly_name != NULL) {
			dest_aname->name = vmap->new_assembly_name;
			mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY,
						"The assembly name %s was remapped to %s",
						aname->name,
						dest_aname->name);
		}
		return dest_aname;
	}
#endif

	return aname;
}

/**
 * mono_assembly_get_assemblyref:
 * \param image pointer to the \c MonoImage to extract the information from.
 * \param index index to the assembly reference in the image.
 * \param aname pointer to a \c MonoAssemblyName that will hold the returned value.
 *
 * Fills out the \p aname with the assembly name of the \p index assembly reference in \p image.
 */
void
mono_assembly_get_assemblyref (MonoImage *image, int index, MonoAssemblyName *aname)
{
	MonoTableInfo *t;
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	const char *hash;

	t = &image->tables [MONO_TABLE_ASSEMBLYREF];

	mono_metadata_decode_row (t, index, cols, MONO_ASSEMBLYREF_SIZE);
		
	hash = mono_metadata_blob_heap (image, cols [MONO_ASSEMBLYREF_HASH_VALUE]);
	aname->hash_len = mono_metadata_decode_blob_size (hash, &hash);
	aname->hash_value = hash;
	aname->name = mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_NAME]);
	aname->culture = mono_metadata_string_heap (image, cols [MONO_ASSEMBLYREF_CULTURE]);
	aname->flags = cols [MONO_ASSEMBLYREF_FLAGS];
	aname->major = cols [MONO_ASSEMBLYREF_MAJOR_VERSION];
	aname->minor = cols [MONO_ASSEMBLYREF_MINOR_VERSION];
	aname->build = cols [MONO_ASSEMBLYREF_BUILD_NUMBER];
	aname->revision = cols [MONO_ASSEMBLYREF_REV_NUMBER];

	if (cols [MONO_ASSEMBLYREF_PUBLIC_KEY]) {
		gchar *token = assemblyref_public_tok (image, cols [MONO_ASSEMBLYREF_PUBLIC_KEY], aname->flags);
		g_strlcpy ((char*)aname->public_key_token, token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (token);
	} else {
		memset (aname->public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}
}

static MonoAssembly*
load_reference_by_aname_refonly_asmctx (MonoAssemblyName *aname, MonoAssembly *assm, MonoImageOpenStatus *status)
{
	MonoAssembly *reference = NULL;
	g_assert (assm != NULL);
	g_assert (status != NULL);
	*status = MONO_IMAGE_OK;
	{
		/* We use the loaded corlib */
		if (!strcmp (aname->name, MONO_ASSEMBLY_CORLIB_NAME)) {
			MonoAssemblyByNameRequest req;
			mono_assembly_request_prepare (&req.request, sizeof (req), MONO_ASMCTX_DEFAULT);
			req.requesting_assembly = assm;
			req.basedir = assm->basedir;
			reference = mono_assembly_request_byname (aname, &req, status);
		} else {
			reference = mono_assembly_loaded_full (aname, TRUE);
			if (!reference)
				/* Try a postload search hook */
				reference = mono_assembly_invoke_search_hook_internal (aname, assm, TRUE, TRUE);
		}

		/*
		 * Here we must advice that the error was due to
		 * a non loaded reference using the ReflectionOnly api
		*/
		if (!reference)
			reference = (MonoAssembly *)REFERENCE_MISSING;
	}
	return reference;
}

static MonoAssembly*
load_reference_by_aname_default_asmctx (MonoAssemblyName *aname, MonoAssembly *assm, MonoImageOpenStatus *status)
{
	MonoAssembly *reference = NULL;
	g_assert (status != NULL);
	*status = MONO_IMAGE_OK;
	{
		/* we first try without setting the basedir: this can eventually result in a ResolveAssembly
		 * event which is the MS .net compatible behaviour (the assemblyresolve_event3.cs test has been fixed
		 * accordingly, it would fail on the MS runtime before).
		 * The second load attempt has the basedir set to keep compatibility with the old mono behavior, for
		 * example bug-349190.2.cs and who knows how much more code in the wild.
		 */
		MonoAssemblyByNameRequest req;
		mono_assembly_request_prepare (&req.request, sizeof (req), MONO_ASMCTX_DEFAULT);
		req.requesting_assembly = assm;
		reference = mono_assembly_request_byname (aname, &req, status);
		if (!reference && assm) {
			memset (&req, 0, sizeof (req));
			req.request.asmctx = MONO_ASMCTX_DEFAULT;
			req.requesting_assembly = assm;
			req.basedir = assm->basedir;
			reference = mono_assembly_request_byname (aname, &req, status);
		}
	}
	return reference;
}

static MonoAssembly*
load_reference_by_aname_loadfrom_asmctx (MonoAssemblyName *aname, MonoAssembly *requesting, MonoImageOpenStatus *status)
{
	MonoAssembly *reference = NULL;
	MonoAssemblyByNameRequest req;
	mono_assembly_request_prepare (&req.request, sizeof (req), MONO_ASMCTX_LOADFROM);
	req.requesting_assembly = requesting;
	req.basedir = requesting->basedir;
	/* Just like default search, but look in the requesting assembly basedir right away */
	reference = mono_assembly_request_byname (aname, &req, status);
	return reference;

}

static MonoAssembly*
load_reference_by_aname_individual_asmctx (MonoAssemblyName *aname, MonoAssembly *requesting, MonoImageOpenStatus *status)
{
	/* For an individual assembly, all references must already be loaded or
	 * else we fire the assembly resolve event - similar to refonly - but
	 * subject to remaping and binding.
	 */

	g_assert (status != NULL);

	MonoAssembly *reference = NULL;
	*status = MONO_IMAGE_OK;
	MonoAssemblyName maped_aname;
	MonoAssemblyName maped_name_pp;

	aname = mono_assembly_remap_version (aname, &maped_aname);
	aname = mono_assembly_apply_binding (aname, &maped_name_pp);

	reference = mono_assembly_loaded_full (aname, FALSE);
	/* Still try to load from application base directory, MONO_PATH or the
	 * GAC.  This is consistent with what .NET Framework (4.7) actually
	 * does, rather than what the documentation implies: If `LoadFile` is
	 * used to load an assembly into "no context"/individual assembly
	 * context, the runtime will still load assemblies from the GAC or the
	 * application base directory (e.g. `System.Runtime` will be loaded if
	 * it wasn't already).
	 * Moreover, those referenced assemblies are loaded in the default context.
	 */
	if (!reference) {
		MonoAssemblyByNameRequest req;
		mono_assembly_request_prepare (&req.request, sizeof (req), MONO_ASMCTX_DEFAULT);
		req.requesting_assembly = requesting;
		reference = mono_assembly_request_byname (aname, &req, status);
	}
	if (!reference)
		reference = (MonoAssembly*)REFERENCE_MISSING;
	return reference;
}

/**
 * mono_assembly_get_assemblyref:
 * \param image pointer to the \c MonoImage to extract the information from.
 * \param index index to the assembly reference in the image.
 * \param aname pointer to a \c MonoAssemblyName that will hold the returned value.
 * \param error set on error
 *
 * Fills out the \p aname with the assembly name of the \p index assembly reference in \p image.
 *
 * \returns TRUE on success, otherwise sets \p error and returns FALSE
 */
gboolean
mono_assembly_get_assemblyref_checked (MonoImage *image, int index, MonoAssemblyName *aname, MonoError *error)
{
	MonoTableInfo *t;
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	const char *hash;

	t = &image->tables [MONO_TABLE_ASSEMBLYREF];

	if (!mono_metadata_decode_row_checked (image, t, index, cols, MONO_ASSEMBLYREF_SIZE, error))
		return FALSE;

	hash = mono_metadata_blob_heap_checked (image, cols [MONO_ASSEMBLYREF_HASH_VALUE], error);
	return_val_if_nok (error, FALSE);
	aname->hash_len = mono_metadata_decode_blob_size (hash, &hash);
	aname->hash_value = hash;
	aname->name = mono_metadata_string_heap_checked (image, cols [MONO_ASSEMBLYREF_NAME], error);
	return_val_if_nok (error, FALSE);
	aname->culture = mono_metadata_string_heap_checked (image, cols [MONO_ASSEMBLYREF_CULTURE], error);
	return_val_if_nok (error, FALSE);
	aname->flags = cols [MONO_ASSEMBLYREF_FLAGS];
	aname->major = cols [MONO_ASSEMBLYREF_MAJOR_VERSION];
	aname->minor = cols [MONO_ASSEMBLYREF_MINOR_VERSION];
	aname->build = cols [MONO_ASSEMBLYREF_BUILD_NUMBER];
	aname->revision = cols [MONO_ASSEMBLYREF_REV_NUMBER];
	if (cols [MONO_ASSEMBLYREF_PUBLIC_KEY]) {
		gchar *token = assemblyref_public_tok_checked (image, cols [MONO_ASSEMBLYREF_PUBLIC_KEY], aname->flags, error);
		return_val_if_nok (error, FALSE);
		g_strlcpy ((char*)aname->public_key_token, token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (token);
	} else {
		memset (aname->public_key_token, 0, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}
	return TRUE;
}

/**
 * mono_assembly_load_reference:
 */
void
mono_assembly_load_reference (MonoImage *image, int index)
{
	MonoAssembly *reference;
	MonoAssemblyName aname;
	MonoImageOpenStatus status;

	/*
	 * image->references is shared between threads, so we need to access
	 * it inside a critical section.
	 */
	mono_assemblies_lock ();
	if (!image->references) {
		MonoTableInfo *t = &image->tables [MONO_TABLE_ASSEMBLYREF];
	
		image->references = g_new0 (MonoAssembly *, t->rows + 1);
		image->nreferences = t->rows;
	}
	reference = image->references [index];
	mono_assemblies_unlock ();
	if (reference)
		return;

	mono_assembly_get_assemblyref (image, index, &aname);

	if (image->assembly) {
		if (mono_trace_is_traced (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY)) {
			char *aname_str = mono_stringify_assembly_name (&aname);
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Loading reference %d of %s asmctx %s, looking for %s",
				    index, image->name, mono_asmctx_get_name (&image->assembly->context),
				    aname_str);
			g_free (aname_str);
		}
		switch (mono_asmctx_get_kind (&image->assembly->context)) {
		case MONO_ASMCTX_DEFAULT:
			reference = load_reference_by_aname_default_asmctx (&aname, image->assembly, &status);
			break;
		case MONO_ASMCTX_REFONLY:
			reference = load_reference_by_aname_refonly_asmctx (&aname, image->assembly, &status);
			break;
		case MONO_ASMCTX_LOADFROM:
			reference = load_reference_by_aname_loadfrom_asmctx (&aname, image->assembly, &status);
			break;
		case MONO_ASMCTX_INDIVIDUAL:
			reference = load_reference_by_aname_individual_asmctx (&aname, image->assembly, &status);
			break;
		default:
			g_error ("Unexpected assembly load context kind %d for image %s.", mono_asmctx_get_kind (&image->assembly->context), image->name);
			break;
		}
	} else {
		/* FIXME: can we establish that image->assembly is never NULL and this code is dead? */
		reference = load_reference_by_aname_default_asmctx (&aname, image->assembly, &status);
	}

	if (reference == NULL){
		char *extra_msg;

		if (status == MONO_IMAGE_ERROR_ERRNO && errno == ENOENT) {
			extra_msg = g_strdup_printf ("The assembly was not found in the Global Assembly Cache, a path listed in the MONO_PATH environment variable, or in the location of the executing assembly (%s).\n", image->assembly != NULL ? image->assembly->basedir : "" );
		} else if (status == MONO_IMAGE_ERROR_ERRNO) {
			extra_msg = g_strdup_printf ("System error: %s\n", strerror (errno));
		} else if (status == MONO_IMAGE_MISSING_ASSEMBLYREF) {
			extra_msg = g_strdup ("Cannot find an assembly referenced from this one.\n");
		} else if (status == MONO_IMAGE_IMAGE_INVALID) {
			extra_msg = g_strdup ("The file exists but is not a valid assembly.\n");
		} else {
			extra_msg = g_strdup ("");
		}
		
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_ASSEMBLY, "The following assembly referenced from %s could not be loaded:\n"
				   "     Assembly:   %s    (assemblyref_index=%d)\n"
				   "     Version:    %d.%d.%d.%d\n"
				   "     Public Key: %s\n%s",
				   image->name, aname.name, index,
				   aname.major, aname.minor, aname.build, aname.revision,
				   strlen ((char*)aname.public_key_token) == 0 ? "(none)" : (char*)aname.public_key_token, extra_msg);
		g_free (extra_msg);

	}

	mono_assemblies_lock ();
	if (reference == NULL) {
		/* Flag as not found */
		reference = (MonoAssembly *)REFERENCE_MISSING;
	}	

	if (!image->references [index]) {
		if (reference != REFERENCE_MISSING){
			mono_assembly_addref (reference);
			if (image->assembly)
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Assembly Ref addref %s[%p] -> %s[%p]: %d",
				    image->assembly->aname.name, image->assembly, reference->aname.name, reference, reference->ref_count);
		} else {
			if (image->assembly)
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Failed to load assembly %s[%p].",
				    image->assembly->aname.name, image->assembly);
		}
		
		image->references [index] = reference;
	}
	mono_assemblies_unlock ();

	if (image->references [index] != reference) {
		/* Somebody loaded it before us */
		mono_assembly_close (reference);
	}
}

/**
 * mono_assembly_load_references:
 * \param image
 * \param status
 * \deprecated There is no reason to use this method anymore, it does nothing
 *
 * This method is now a no-op, it does nothing other than setting the \p status to \c MONO_IMAGE_OK
 */
void
mono_assembly_load_references (MonoImage *image, MonoImageOpenStatus *status)
{
	/* This is a no-op now but it is part of the embedding API so we can't remove it */
	if (status)
		*status = MONO_IMAGE_OK;
}

typedef struct AssemblyLoadHook AssemblyLoadHook;
struct AssemblyLoadHook {
	AssemblyLoadHook *next;
	MonoAssemblyLoadFunc func;
	gpointer user_data;
};

static AssemblyLoadHook *assembly_load_hook = NULL;

/**
 * mono_assembly_invoke_load_hook:
 */
void
mono_assembly_invoke_load_hook (MonoAssembly *ass)
{
	AssemblyLoadHook *hook;

	for (hook = assembly_load_hook; hook; hook = hook->next) {
		hook->func (ass, hook->user_data);
	}
}

/**
 * mono_install_assembly_load_hook:
 */
void
mono_install_assembly_load_hook (MonoAssemblyLoadFunc func, gpointer user_data)
{
	AssemblyLoadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyLoadHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_load_hook;
	assembly_load_hook = hook;
}

static void
free_assembly_load_hooks (void)
{
	AssemblyLoadHook *hook, *next;

	for (hook = assembly_load_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}
}

typedef struct AssemblySearchHook AssemblySearchHook;
struct AssemblySearchHook {
	AssemblySearchHook *next;
	MonoAssemblySearchFunc func;
	gboolean refonly;
	gboolean postload;
	gpointer user_data;
};

static AssemblySearchHook *assembly_search_hook = NULL;

static MonoAssembly*
mono_assembly_invoke_search_hook_internal (MonoAssemblyName *aname, MonoAssembly *requesting, gboolean refonly, gboolean postload)
{
	AssemblySearchHook *hook;

	for (hook = assembly_search_hook; hook; hook = hook->next) {
		if ((hook->refonly == refonly) && (hook->postload == postload)) {
			MonoAssembly *ass;
			/**
			  * A little explanation is in order here.
			  *
			  * The default postload search hook needs to know the requesting assembly to report it to managed code.
			  * The embedding API exposes a search hook that doesn't take such argument.
			  *
			  * The original fix would call the default search hook before all the registered ones and pass
			  * the requesting assembly to it. It works but broke a very suddle embedding API aspect that some users
			  * rely on. Which is the ordering between user hooks and the default runtime hook.
			  *
			  * Registering the hook after mono_jit_init would let your hook run before the default one and
			  * when using it to handle non standard app layouts this could save your app from a massive amount
			  * of syscalls that the default hook does when probing all sorts of places. Slow targets with horrible IO
			  * are all using this trick and if we broke this assumption they would be very disapointed at us.
			  *
			  * So what's the fix? We register the default hook using regular means and special case it when iterating
			  * over the registered hooks. This preserves ordering and enables managed resolve hooks to get the requesting
			  * assembly.
			  */
			if (hook->func == (void*)mono_domain_assembly_postload_search)
				ass = mono_domain_assembly_postload_search (aname, requesting, refonly);
			else
				ass = hook->func (aname, hook->user_data);
			if (ass)
				return ass;
		}
	}

	return NULL;
}

/**
 * mono_assembly_invoke_search_hook:
 */
MonoAssembly*
mono_assembly_invoke_search_hook (MonoAssemblyName *aname)
{
	return mono_assembly_invoke_search_hook_internal (aname, NULL, FALSE, FALSE);
}

static void
mono_install_assembly_search_hook_internal (MonoAssemblySearchFunc func, gpointer user_data, gboolean refonly, gboolean postload)
{
	AssemblySearchHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblySearchHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->refonly = refonly;
	hook->postload = postload;
	hook->next = assembly_search_hook;
	assembly_search_hook = hook;
}

/**
 * mono_install_assembly_search_hook:
 */
void          
mono_install_assembly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal (func, user_data, FALSE, FALSE);
}	

static void
free_assembly_search_hooks (void)
{
	AssemblySearchHook *hook, *next;

	for (hook = assembly_search_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}
}

/**
 * mono_install_assembly_refonly_search_hook:
 */
void
mono_install_assembly_refonly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal (func, user_data, TRUE, FALSE);
}

/**
 * mono_install_assembly_postload_search_hook:
 */
void          
mono_install_assembly_postload_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal (func, user_data, FALSE, TRUE);
}	

void
mono_install_assembly_postload_refonly_search_hook (MonoAssemblySearchFunc func, gpointer user_data)
{
	mono_install_assembly_search_hook_internal (func, user_data, TRUE, TRUE);
}

typedef struct AssemblyPreLoadHook AssemblyPreLoadHook;
struct AssemblyPreLoadHook {
	AssemblyPreLoadHook *next;
	MonoAssemblyPreLoadFunc func;
	gpointer user_data;
};

static AssemblyPreLoadHook *assembly_preload_hook = NULL;
static AssemblyPreLoadHook *assembly_refonly_preload_hook = NULL;

static MonoAssembly *
invoke_assembly_preload_hook (MonoAssemblyName *aname, gchar **apath)
{
	AssemblyPreLoadHook *hook;
	MonoAssembly *assembly;

	for (hook = assembly_preload_hook; hook; hook = hook->next) {
		assembly = hook->func (aname, apath, hook->user_data);
		if (assembly != NULL)
			return assembly;
	}

	return NULL;
}

static MonoAssembly *
invoke_assembly_refonly_preload_hook (MonoAssemblyName *aname, gchar **apath)
{
	AssemblyPreLoadHook *hook;
	MonoAssembly *assembly;

	for (hook = assembly_refonly_preload_hook; hook; hook = hook->next) {
		assembly = hook->func (aname, apath, hook->user_data);
		if (assembly != NULL)
			return assembly;
	}

	return NULL;
}

/**
 * mono_install_assembly_preload_hook:
 */
void
mono_install_assembly_preload_hook (MonoAssemblyPreLoadFunc func, gpointer user_data)
{
	AssemblyPreLoadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyPreLoadHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_preload_hook;
	assembly_preload_hook = hook;
}

/**
 * mono_install_assembly_refonly_preload_hook:
 */
void
mono_install_assembly_refonly_preload_hook (MonoAssemblyPreLoadFunc func, gpointer user_data)
{
	AssemblyPreLoadHook *hook;
	
	g_return_if_fail (func != NULL);

	hook = g_new0 (AssemblyPreLoadHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_refonly_preload_hook;
	assembly_refonly_preload_hook = hook;
}

static void
free_assembly_preload_hooks (void)
{
	AssemblyPreLoadHook *hook, *next;

	for (hook = assembly_preload_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}

	for (hook = assembly_refonly_preload_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}
}

typedef struct AssemblyAsmCtxFromPathHook AssemblyAsmCtxFromPathHook;
struct AssemblyAsmCtxFromPathHook {
	AssemblyAsmCtxFromPathHook *next;
	MonoAssemblyAsmCtxFromPathFunc func;
	gpointer user_data;
};

static AssemblyAsmCtxFromPathHook *assembly_asmctx_from_path_hook = NULL;

/**
 * mono_install_assembly_asmctx_from_path_hook:
 *
 * \param func Hook function
 * \param user_data User data
 *
 * Installs a hook function \p func that when called with an absolute path name
 * returns \c TRUE and writes to \c out_asmctx if an assembly that name would
 * be found by that asmctx.  The hooks are called in the order from most
 * recently added to oldest.
 *
 */
void
mono_install_assembly_asmctx_from_path_hook (MonoAssemblyAsmCtxFromPathFunc func, gpointer user_data)
{
	g_return_if_fail (func != NULL);

	AssemblyAsmCtxFromPathHook *hook = g_new0 (AssemblyAsmCtxFromPathHook, 1);
	hook->func = func;
	hook->user_data = user_data;
	hook->next = assembly_asmctx_from_path_hook;
	assembly_asmctx_from_path_hook = hook;
}

/**
 * mono_assembly_invoke_asmctx_from_path_hook:
 *
 * \param absfname absolute path name
 * \param requesting_assembly the \c MonoAssembly that requested the load, may be \c NULL
 * \param out_asmctx assembly context kind, written on output
 *
 * Invokes hooks to find the assembly context that would have searched for the
 * given assembly name.  Writes to \p out_asmctx the assembly context kind from
 * the first hook to return \c TRUE.  \returns \c TRUE if any hook wrote to \p
 * out_asmctx, or \c FALSE otherwise.
 */
static gboolean
assembly_invoke_asmctx_from_path_hook (const char *absfname, MonoAssembly *requesting_assembly, MonoAssemblyContextKind *out_asmctx)
{
	g_assert (absfname);
	g_assert (out_asmctx);
	AssemblyAsmCtxFromPathHook *hook;

	for (hook = assembly_asmctx_from_path_hook; hook; hook = hook->next) {
		*out_asmctx = MONO_ASMCTX_INDIVIDUAL;
		if (hook->func (absfname, requesting_assembly, hook->user_data, out_asmctx))
			return TRUE;
	}
	return FALSE;
}


static void
free_assembly_asmctx_from_path_hooks (void)
{
	AssemblyAsmCtxFromPathHook *hook, *next;

	for (hook = assembly_asmctx_from_path_hook; hook; hook = next) {
		next = hook->next;
		g_free (hook);
	}
}

static gchar *
absolute_dir (const gchar *filename)
{
	gchar *cwd;
	gchar *mixed;
	gchar **parts;
	gchar *part;
	GList *list, *tmp;
	GString *result;
	gchar *res;
	gint i;

	if (g_path_is_absolute (filename)) {
		part = g_path_get_dirname (filename);
		res = g_strconcat (part, G_DIR_SEPARATOR_S, NULL);
		g_free (part);
		return res;
	}

	cwd = g_get_current_dir ();
	mixed = g_build_filename (cwd, filename, NULL);
	parts = g_strsplit (mixed, G_DIR_SEPARATOR_S, 0);
	g_free (mixed);
	g_free (cwd);

	list = NULL;
	for (i = 0; (part = parts [i]) != NULL; i++) {
		if (!strcmp (part, "."))
			continue;

		if (!strcmp (part, "..")) {
			if (list && list->next) /* Don't remove root */
				list = g_list_delete_link (list, list);
		} else {
			list = g_list_prepend (list, part);
		}
	}

	result = g_string_new ("");
	list = g_list_reverse (list);

	/* Ignores last data pointer, which should be the filename */
	for (tmp = list; tmp && tmp->next != NULL; tmp = tmp->next){
		if (tmp->data)
			g_string_append_printf (result, "%s%c", (char *) tmp->data,
								G_DIR_SEPARATOR);
	}

	res = result->str;
	g_string_free (result, FALSE);
	g_list_free (list);
	g_strfreev (parts);
	if (*res == '\0') {
		g_free (res);
		return g_strdup (".");
	}

	return res;
}

/** 
 * mono_assembly_open_from_bundle:
 * \param filename Filename requested
 * \param status return status code
 *
 * This routine tries to open the assembly specified by \p filename from the
 * defined bundles, if found, returns the MonoImage for it, if not found
 * returns NULL
 */
MonoImage *
mono_assembly_open_from_bundle (const char *filename, MonoImageOpenStatus *status, gboolean refonly)
{
	int i;
	char *name;
	gchar *lowercase_filename;
	MonoImage *image = NULL;
	gboolean is_satellite = FALSE;
	/*
	 * we do a very simple search for bundled assemblies: it's not a general 
	 * purpose assembly loading mechanism.
	 */

	if (!bundles)
		return NULL;

	lowercase_filename = g_utf8_strdown (filename, -1);
	is_satellite = g_str_has_suffix (lowercase_filename, ".resources.dll");
	g_free (lowercase_filename);
	name = g_path_get_basename (filename);
	mono_assemblies_lock ();
	for (i = 0; !image && bundles [i]; ++i) {
		if (strcmp (bundles [i]->name, is_satellite ? filename : name) == 0) {
			image = mono_image_open_from_data_internal ((char*)bundles [i]->data, bundles [i]->size, FALSE, status, refonly, FALSE, name);
			break;
		}
	}
	mono_assemblies_unlock ();
	if (image) {
		mono_image_addref (image);
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Assembly Loader loaded assembly from bundle: '%s'.", is_satellite ? filename : name);
		g_free (name);
		return image;
	}
	g_free (name);
	return NULL;
}

/**
 * mono_assembly_open_full:
 * \param filename the file to load
 * \param status return status code 
 * \param refonly Whether this assembly is being opened in "reflection-only" mode.
 *
 * This loads an assembly from the specified \p filename. The \p filename allows
 * a local URL (starting with a \c file:// prefix).  If a file prefix is used, the
 * filename is interpreted as a URL, and the filename is URL-decoded.   Otherwise the file
 * is treated as a local path.
 *
 * First, an attempt is made to load the assembly from the bundled executable (for those
 * deployments that have been done with the \c mkbundle tool or for scenarios where the
 * assembly has been registered as an embedded assembly).   If this is not the case, then
 * the assembly is loaded from disk using `api:mono_image_open_full`.
 *
 * If the pointed assembly does not live in the Global Assembly Cache, a shadow copy of
 * the assembly is made.
 *
 * If \p refonly is set to true, then the assembly is loaded purely for inspection with
 * the \c System.Reflection API.
 *
 * \returns NULL on error, with the \p status set to an error code, or a pointer
 * to the assembly.
 */
MonoAssembly *
mono_assembly_open_full (const char *filename, MonoImageOpenStatus *status, gboolean refonly)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare (&req.request, sizeof (req), refonly ? MONO_ASMCTX_REFONLY : MONO_ASMCTX_DEFAULT);
	res = mono_assembly_request_open (filename, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

static gboolean
assembly_loadfrom_asmctx_from_path (const char *filename, MonoAssembly *requesting_assembly,
				    gpointer user_data, MonoAssemblyContextKind *out_asmctx) {
	if (requesting_assembly && mono_asmctx_get_kind (&requesting_assembly->context) == MONO_ASMCTX_LOADFROM) {
		if (mono_path_filename_in_basedir (filename, requesting_assembly->basedir)) {
			*out_asmctx = MONO_ASMCTX_LOADFROM;
			return TRUE;
		}
	}
	return FALSE;
}

MonoAssembly *
mono_assembly_request_open (const char *filename, const MonoAssemblyOpenRequest *open_req,
			    MonoImageOpenStatus *status)
{
	MonoImage *image;
	MonoAssembly *ass;
	MonoImageOpenStatus def_status;
	gchar *fname;
	gchar *new_fname;
	gboolean loaded_from_bundle;

	MonoAssemblyLoadRequest load_req;
	/* we will be overwriting the load request's asmctx.*/
	memcpy (&load_req, &open_req->request, sizeof (load_req));
	
	g_return_val_if_fail (filename != NULL, NULL);

	if (!status)
		status = &def_status;
	*status = MONO_IMAGE_OK;

	if (strncmp (filename, "file://", 7) == 0) {
		GError *gerror = NULL;
		gchar *uri = (gchar *) filename;
		gchar *tmpuri;

		/*
		 * MS allows file://c:/... and fails on file://localhost/c:/... 
		 * They also throw an IndexOutOfRangeException if "file://"
		 */
		if (uri [7] != '/')
			uri = g_strdup_printf ("file:///%s", uri + 7);
	
		tmpuri = uri;
		uri = mono_escape_uri_string (tmpuri);
		fname = g_filename_from_uri (uri, NULL, &gerror);
		g_free (uri);

		if (tmpuri != filename)
			g_free (tmpuri);

		if (gerror != NULL) {
			g_warning ("%s\n", gerror->message);
			g_error_free (gerror);
			fname = g_strdup (filename);
		}
	} else {
		fname = g_strdup (filename);
	}

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
			"Assembly Loader probing location: '%s'.", fname);

	new_fname = NULL;
	if (!mono_assembly_is_in_gac (fname)) {
		ERROR_DECL (error);
		new_fname = mono_make_shadow_copy (fname, error);
		if (!is_ok (error)) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
				    "Assembly Loader shadow copy error: %s.", mono_error_get_message (error));
			mono_error_cleanup (error);
			*status = MONO_IMAGE_IMAGE_INVALID;
			g_free (fname);
			return NULL;
		}

		if (load_req.asmctx != MONO_ASMCTX_REFONLY) {
			MonoAssemblyContextKind out_asmctx;
			/* If the path belongs to the appdomain base dir or the
			 * base dir of the requesting assembly, load the
			 * assembly in the corresponding asmctx.
			 */
			if (assembly_invoke_asmctx_from_path_hook (fname, open_req->requesting_assembly, &out_asmctx))
				load_req.asmctx = out_asmctx;
		}
	} else {
		if (load_req.asmctx != MONO_ASMCTX_REFONLY) {
			/* GAC assemblies always in default context or refonly context. */
			load_req.asmctx = MONO_ASMCTX_DEFAULT;
		}
	}
	if (new_fname && new_fname != fname) {
		g_free (fname);
		fname = new_fname;
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
			    "Assembly Loader shadow-copied assembly to: '%s'.", fname);
	}
	
	image = NULL;

	const gboolean refonly = load_req.asmctx == MONO_ASMCTX_REFONLY;
	/* for LoadFrom(string), LoadFile(string) and Load(byte[]), allow them
	 * to load problematic images.  Not sure if ReflectionOnlyLoad(string)
	 * and ReflectionOnlyLoadFrom(string) should also be allowed - let's
	 * say, yes.
	 */
	const gboolean load_from_context = load_req.asmctx == MONO_ASMCTX_LOADFROM || load_req.asmctx == MONO_ASMCTX_INDIVIDUAL || load_req.asmctx == MONO_ASMCTX_REFONLY;

	// If VM built with mkbundle
	loaded_from_bundle = FALSE;
	if (bundles != NULL) {
		image = mono_assembly_open_from_bundle (fname, status, refonly);
		loaded_from_bundle = image != NULL;
	}

	if (!image)
		image = mono_image_open_a_lot (fname, status, refonly, load_from_context);

	if (!image){
		if (*status == MONO_IMAGE_OK)
			*status = MONO_IMAGE_ERROR_ERRNO;
		g_free (fname);
		return NULL;
	}

	if (load_req.asmctx == MONO_ASMCTX_LOADFROM || load_req.asmctx == MONO_ASMCTX_INDIVIDUAL) {
		MonoAssembly *redirected_asm = NULL;
		MonoImageOpenStatus new_status = MONO_IMAGE_OK;
		if ((redirected_asm = chain_redirections_loadfrom (image, &new_status))) {
			mono_image_close (image);
			image = redirected_asm->image;
			mono_image_addref (image); /* so that mono_image_close, below, has something to do */
			/* fall thru to if (image->assembly) below */
		} else if (new_status != MONO_IMAGE_OK) {
			*status = new_status;
			mono_image_close (image);
			g_free (fname);
			return NULL;
		}
	}

	if (image->assembly) {
		/* We want to return the MonoAssembly that's already loaded,
		 * but if we're using the strict assembly loader, we also need
		 * to check that the previously loaded assembly matches the
		 * predicate.  It could be that we previously loaded a
		 * different version that happens to have the filename that
		 * we're currently probing. */
		if (mono_loader_get_strict_strong_names () &&
		    load_req.predicate && !load_req.predicate (image->assembly, load_req.predicate_ud)) {
			mono_image_close (image);
			g_free (fname);
			return NULL;
		} else {
			/* Already loaded by another appdomain */
			mono_assembly_invoke_load_hook (image->assembly);
			mono_image_close (image);
			g_free (fname);
			return image->assembly;
		}
	}

	ass = mono_assembly_request_load_from (image, fname, &load_req, status);

	if (ass) {
		if (!loaded_from_bundle)
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY,
				"Assembly Loader loaded assembly from location: '%s'.", filename);
		if (!refonly)
			mono_config_for_assembly (ass->image);
	}

	/* Clear the reference added by mono_image_open */
	mono_image_close (image);
	
	g_free (fname);

	return ass;
}

static void
free_item (gpointer val, gpointer user_data)
{
	g_free (val);
}

/**
 * mono_assembly_load_friends:
 * \param ass an assembly
 *
 * Load the list of friend assemblies that are allowed to access
 * the assembly's internal types and members. They are stored as assembly
 * names in custom attributes.
 *
 * This is an internal method, we need this because when we load mscorlib
 * we do not have the internals visible cattr loaded yet,
 * so we need to load these after we initialize the runtime. 
 *
 * LOCKING: Acquires the assemblies lock plus the loader lock.
 */
void
mono_assembly_load_friends (MonoAssembly* ass)
{
	ERROR_DECL (error);
	int i;
	MonoCustomAttrInfo* attrs;
	GSList *list;

	if (ass->friend_assembly_names_inited)
		return;

	attrs = mono_custom_attrs_from_assembly_checked (ass, FALSE, error);
	mono_error_assert_ok (error);
	if (!attrs) {
		mono_assemblies_lock ();
		ass->friend_assembly_names_inited = TRUE;
		mono_assemblies_unlock ();
		return;
	}

	mono_assemblies_lock ();
	if (ass->friend_assembly_names_inited) {
		mono_assemblies_unlock ();
		return;
	}
	mono_assemblies_unlock ();

	list = NULL;
	/* 
	 * We build the list outside the assemblies lock, the worse that can happen
	 * is that we'll need to free the allocated list.
	 */
	for (i = 0; i < attrs->num_attrs; ++i) {
		MonoCustomAttrEntry *attr = &attrs->attrs [i];
		MonoAssemblyName *aname;
		const gchar *data;
		uint32_t data_length;
		gchar *data_with_terminator;
		/* Do some sanity checking */
		if (!attr->ctor || attr->ctor->klass != mono_class_try_get_internals_visible_class ())
			continue;
		if (attr->data_size < 4)
			continue;
		data = (const char*)attr->data;
		/* 0xFF means null string, see custom attr format */
		if (data [0] != 1 || data [1] != 0 || (data [2] & 0xFF) == 0xFF)
			continue;
		data_length = mono_metadata_decode_value (data + 2, &data);
		data_with_terminator = (char *)g_memdup (data, data_length + 1);
		data_with_terminator[data_length] = 0;
		aname = g_new0 (MonoAssemblyName, 1);
		/*g_print ("friend ass: %s\n", data);*/
		if (mono_assembly_name_parse_full (data_with_terminator, aname, TRUE, NULL, NULL)) {
			list = g_slist_prepend (list, aname);
		} else {
			g_free (aname);
		}
		g_free (data_with_terminator);
	}
	mono_custom_attrs_free (attrs);

	mono_assemblies_lock ();
	if (ass->friend_assembly_names_inited) {
		mono_assemblies_unlock ();
		g_slist_foreach (list, free_item, NULL);
		g_slist_free (list);
		return;
	}
	ass->friend_assembly_names = list;

	/* Because of the double checked locking pattern above */
	mono_memory_barrier ();
	ass->friend_assembly_names_inited = TRUE;
	mono_assemblies_unlock ();
}

struct HasReferenceAssemblyAttributeIterData {
	gboolean has_attr;
};

static gboolean
has_reference_assembly_attribute_iterator (MonoImage *image, guint32 typeref_scope_token, const char *nspace, const char *name, guint32 method_token, gpointer user_data)
{
	gboolean stop_scanning = FALSE;
	struct HasReferenceAssemblyAttributeIterData *iter_data = (struct HasReferenceAssemblyAttributeIterData*)user_data;

	if (!strcmp (name, "ReferenceAssemblyAttribute") && !strcmp (nspace, "System.Runtime.CompilerServices")) {
		/* Note we don't check the assembly name, same as coreCLR. */
		iter_data->has_attr = TRUE;
		stop_scanning = TRUE;
	}

	return stop_scanning;
}

/**
 * mono_assembly_has_reference_assembly_attribute:
 * \param assembly a MonoAssembly
 * \param error set on error.
 *
 * \returns TRUE if \p assembly has the \c System.Runtime.CompilerServices.ReferenceAssemblyAttribute set.
 * On error returns FALSE and sets \p error.
 */
gboolean
mono_assembly_has_reference_assembly_attribute (MonoAssembly *assembly, MonoError *error)
{
	g_assert (assembly && assembly->image);
	/* .NET Framework appears to ignore the attribute on dynamic
	 * assemblies, so don't call this function for dynamic assemblies. */
	g_assert (!image_is_dynamic (assembly->image));
	error_init (error);

	/*
	 * This might be called during assembly loading, so do everything using the low-level
	 * metadata APIs.
	 */

	struct HasReferenceAssemblyAttributeIterData iter_data = { FALSE };

	mono_assembly_metadata_foreach_custom_attr (assembly, &has_reference_assembly_attribute_iterator, &iter_data);

	return iter_data.has_attr;
}

/**
 * chain_redirections_loadfrom:
 * \param image a MonoImage that we wanted to load using LoadFrom context
 * \param status set if there was an error opening the redirected image
 *
 * Check if we need to open a different image instead of the given one for some reason.
 * Returns NULL and sets status to \c MONO_IMAGE_OK if the given image was good.
 *
 * Otherwise returns the assembly that we opened instead or sets status if
 * there was a problem opening the redirected image.
 *
 */
MonoAssembly*
chain_redirections_loadfrom (MonoImage *image, MonoImageOpenStatus *out_status)
{
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssembly *redirected = NULL;

	redirected = mono_assembly_binding_applies_to_image (image, &status);
	if (redirected || status != MONO_IMAGE_OK) {
		*out_status = status;
		return redirected;
	}

	redirected = mono_problematic_image_reprobe (image, &status);
	if (redirected || status != MONO_IMAGE_OK) {
		*out_status = status;
		return redirected;
	}

	*out_status = MONO_IMAGE_OK;
	return NULL;
}

/**
 * mono_assembly_binding_applies_to_image:
 * \param image The image whose assembly name we should check
 * \param status sets on error;
 *
 * Get the \c MonoAssemblyName from the given \p image metadata and apply binding redirects to it.
 * If the resulting name is different from the name in the image, load that \c MonoAssembly instead
 *
 * \returns the loaded \c MonoAssembly, or NULL if no binding redirection applied.
 *
 */
MonoAssembly*
mono_assembly_binding_applies_to_image (MonoImage* image, MonoImageOpenStatus *status)
{
	g_assert (status != NULL);

	/* This is a "fun" one now.
	 * For LoadFrom ("/basedir/some.dll") or LoadFile("/basedir/some.dll") or Load(byte[])),
	 * apparently what we're meant to do is:
	 *   1. probe the assembly name from some.dll (or the byte array)
	 *   2. apply binding redirects
	 *   3. If we get some other different name, drop this image and use
	 *      the binding redirected name to probe.
	 *   4. Return the new assembly.
	 */
	MonoAssemblyName probed_aname, dest_name;
	if (!mono_assembly_fill_assembly_name_full (image, &probed_aname, TRUE)) {
		if (*status == MONO_IMAGE_OK)
			*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}
	MonoAssembly *result_ass = NULL;
	MonoAssemblyName *result_name = &probed_aname;
	result_name = mono_assembly_apply_binding (result_name, &dest_name);
	if (result_name != &probed_aname && !mono_assembly_names_equal (result_name, &probed_aname)) {
		if (mono_trace_is_traced (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY)) {
			char *probed_fullname = mono_stringify_assembly_name (&probed_aname);
			char *result_fullname = mono_stringify_assembly_name (result_name);
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Request to load from %s in (%s) remapped to %s", probed_fullname, image->name, result_fullname);
			g_free (probed_fullname);
			g_free (result_fullname);
		}
		const char *new_basedir = NULL; /* FIXME: null? - do a test of this */
		MonoAssemblyContextKind new_asmctx = MONO_ASMCTX_DEFAULT; /* FIXME: default? or? */
		MonoAssembly *new_requesting = NULL; /* this seems okay */
		MonoImageOpenStatus new_status = MONO_IMAGE_OK;

		MonoAssemblyByNameRequest new_req;
		mono_assembly_request_prepare (&new_req.request, sizeof (new_req), new_asmctx);
		new_req.requesting_assembly = new_requesting;
		new_req.basedir = new_basedir;
		result_ass = mono_assembly_request_byname (result_name, &new_req, &new_status);

		if (result_ass && new_status == MONO_IMAGE_OK) {
			g_assert (result_ass->image->assembly != NULL);
		} else {
			*status = new_status;
		}
	}
	mono_assembly_name_free (&probed_aname);
	return result_ass;
}

/**
 * mono_problematic_image_reprobe:
 * \param image A MonoImage
 * \param status set on error
 *
 * If the given image is problematic for mono (see image.c), then try to load
 * by assembly name in the default context (which should succeed with Mono's
 * own implementations of those assemblies).
 *
 * Returns NULL and sets status to MONO_IMAGE_OK if no redirect is needed.
 *
 * Otherwise returns the assembly we were redirected to, or NULL and sets a
 * non-ok status on failure.
 *
 * IMPORTANT NOTE: Don't call this if \c image was found by probing the search
 * path, you will end up in a loop and a stack overflow.
 */
MonoAssembly*
mono_problematic_image_reprobe (MonoImage *image, MonoImageOpenStatus *status)
{
	g_assert (status != NULL);

	if (G_LIKELY (!mono_is_problematic_image (image))) {
		*status = MONO_IMAGE_OK;
		return NULL;
	}
	MonoAssemblyName probed_aname;
	if (!mono_assembly_fill_assembly_name_full (image, &probed_aname, TRUE)) {
		*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}
	if (mono_trace_is_traced (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY)) {
		char *probed_fullname = mono_stringify_assembly_name (&probed_aname);
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Requested to load from problematic image %s, probing instead for assembly with name %s", image->name, probed_fullname);
		g_free (probed_fullname);
	}
	const char *new_basedir = NULL;
	MonoAssemblyContextKind new_asmctx = MONO_ASMCTX_DEFAULT;
	MonoAssembly *new_requesting = NULL;
	MonoImageOpenStatus new_status = MONO_IMAGE_OK;
	MonoAssemblyByNameRequest new_req;
	mono_assembly_request_prepare (&new_req.request, sizeof (new_req), new_asmctx);
	new_req.requesting_assembly = new_requesting;
	new_req.basedir = new_basedir;
	// Note: this interacts with mono_image_open_a_lot (). If the path from
	// which we tried to load the problematic image is among the probing
	// paths, the MonoImage will be in the hash of loaded images and we
	// would just get it back again here, except for the code there that
	// mitigates the situation.  Instead
	MonoAssembly *result_ass = mono_assembly_request_byname (&probed_aname, &new_req, &new_status);

	if (! (result_ass && new_status == MONO_IMAGE_OK)) {
		*status = new_status;
	}
	mono_assembly_name_free (&probed_aname);
	return result_ass;
}
/**
 * mono_assembly_open:
 * \param filename Opens the assembly pointed out by this name
 * \param status return status code
 *
 * This loads an assembly from the specified \p filename. The \p filename allows
 * a local URL (starting with a \c file:// prefix).  If a file prefix is used, the
 * filename is interpreted as a URL, and the filename is URL-decoded.   Otherwise the file
 * is treated as a local path.
 *
 * First, an attempt is made to load the assembly from the bundled executable (for those
 * deployments that have been done with the \c mkbundle tool or for scenarios where the
 * assembly has been registered as an embedded assembly).   If this is not the case, then
 * the assembly is loaded from disk using `api:mono_image_open_full`.
 *
 * If the pointed assembly does not live in the Global Assembly Cache, a shadow copy of
 * the assembly is made.
 *
 * \returns a pointer to the \c MonoAssembly if \p filename contains a valid
 * assembly or NULL on error.  Details about the error are stored in the
 * \p status variable.
 */
MonoAssembly *
mono_assembly_open (const char *filename, MonoImageOpenStatus *status)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare (&req.request, sizeof (req), MONO_ASMCTX_DEFAULT);
	res = mono_assembly_request_open (filename, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_assembly_load_from_full:
 * \param image Image to load the assembly from
 * \param fname assembly name to associate with the assembly
 * \param status returns the status condition
 * \param refonly Whether this assembly is being opened in "reflection-only" mode.
 *
 * If the provided \p image has an assembly reference, it will process the given
 * image as an assembly with the given name.
 *
 * Most likely you want to use the `api:mono_assembly_load_full` method instead.
 *
 * Returns: A valid pointer to a \c MonoAssembly* on success and the \p status will be
 * set to \c MONO_IMAGE_OK;  or NULL on error.
 *
 * If there is an error loading the assembly the \p status will indicate the
 * reason with \p status being set to \c MONO_IMAGE_INVALID if the
 * image did not contain an assembly reference table.
 */
MonoAssembly *
mono_assembly_load_from_full (MonoImage *image, const char*fname, 
			      MonoImageOpenStatus *status, gboolean refonly)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyLoadRequest req;
	MonoImageOpenStatus def_status;
	if (!status)
		status = &def_status;
	mono_assembly_request_prepare (&req, sizeof (req), refonly ? MONO_ASMCTX_REFONLY : MONO_ASMCTX_DEFAULT);
	res = mono_assembly_request_load_from (image, fname, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

MonoAssembly *
mono_assembly_request_load_from (MonoImage *image, const char *fname,
				 const MonoAssemblyLoadRequest *req,
				   MonoImageOpenStatus *status)
{
	MonoAssemblyContextKind asmctx;
	MonoAssemblyCandidatePredicate predicate;
	gpointer user_data;

	MonoAssembly *ass, *ass2;
	char *base_dir;

	g_assert (status != NULL);

	asmctx = req->asmctx;
	predicate = req->predicate;
	user_data = req->predicate_ud;

	if (!image->tables [MONO_TABLE_ASSEMBLY].rows) {
		/* 'image' doesn't have a manifest -- maybe someone is trying to Assembly.Load a .netmodule */
		*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}

#if defined (HOST_WIN32)
	{
		gchar *tmp_fn;
		int i;

		tmp_fn = g_strdup (fname);
		for (i = strlen (tmp_fn) - 1; i >= 0; i--) {
			if (tmp_fn [i] == '/')
				tmp_fn [i] = '\\';
		}

		base_dir = absolute_dir (tmp_fn);
		g_free (tmp_fn);
	}
#else
	base_dir = absolute_dir (fname);
#endif

	/*
	 * Create assembly struct, and enter it into the assembly cache
	 */
	ass = g_new0 (MonoAssembly, 1);
	ass->basedir = base_dir;
	ass->context.kind = asmctx;
	ass->image = image;

	MONO_PROFILER_RAISE (assembly_loading, (ass));

	mono_assembly_fill_assembly_name (image, &ass->aname);

	if (mono_defaults.corlib && strcmp (ass->aname.name, MONO_ASSEMBLY_CORLIB_NAME) == 0) {
		// MS.NET doesn't support loading other mscorlibs
		g_free (ass);
		g_free (base_dir);
		mono_image_addref (mono_defaults.corlib);
		*status = MONO_IMAGE_OK;
		return mono_defaults.corlib->assembly;
	}

	/* Add a non-temporary reference because of ass->image */
	mono_image_addref (image);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Image addref %s[%p] (asmctx %s) -> %s[%p]: %d", ass->aname.name, ass, mono_asmctx_get_name (&ass->context), image->name, image, image->ref_count);

	/* 
	 * The load hooks might take locks so we can't call them while holding the
	 * assemblies lock.
	 */
	if (ass->aname.name && asmctx != MONO_ASMCTX_INDIVIDUAL) {
		/* FIXME: I think individual context should probably also look for an existing MonoAssembly here, we just need to pass the asmctx to the search hook so that it does a filename match (I guess?) */
		ass2 = mono_assembly_invoke_search_hook_internal (&ass->aname, NULL, asmctx == MONO_ASMCTX_REFONLY, FALSE);
		if (ass2) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Image %s[%p] reusing existing assembly %s[%p]", ass->aname.name, ass, ass2->aname.name, ass2);
			g_free (ass);
			g_free (base_dir);
			mono_image_close (image);
			*status = MONO_IMAGE_OK;
			return ass2;
		}
	}

	/* We need to check for ReferenceAssmeblyAttribute before we
	 * mark the assembly as loaded and before we fire the load
	 * hook. Otherwise mono_domain_fire_assembly_load () in
	 * appdomain.c will cache a mapping from the assembly name to
	 * this image and we won't be able to look for a different
	 * candidate. */

	if (asmctx != MONO_ASMCTX_REFONLY) {
		ERROR_DECL (refasm_error);
		if (mono_assembly_has_reference_assembly_attribute (ass, refasm_error)) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Image for assembly '%s' (%s) has ReferenceAssemblyAttribute, skipping", ass->aname.name, image->name);
			g_free (ass);
			g_free (base_dir);
			mono_image_close (image);
			*status = MONO_IMAGE_IMAGE_INVALID;
			return NULL;
		}
		mono_error_cleanup (refasm_error);
	}

	if (predicate && !predicate (ass, user_data)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate returned FALSE, skipping '%s' (%s)\n", ass->aname.name, image->name);
		g_free (ass);
		g_free (base_dir);
		mono_image_close (image);
		*status = MONO_IMAGE_IMAGE_INVALID;
		return NULL;
	}

	mono_assemblies_lock ();

	/* If an assembly is loaded into an individual context, always return a
	 * new MonoAssembly, even if another assembly with the same name has
	 * already been loaded.
	 */
	if (image->assembly && asmctx != MONO_ASMCTX_INDIVIDUAL) {
		/*
		 * This means another thread has already loaded the assembly, but not yet
		 * called the load hooks so the search hook can't find the assembly.
		 */
		mono_assemblies_unlock ();
		ass2 = image->assembly;
		g_free (ass);
		g_free (base_dir);
		mono_image_close (image);
		*status = MONO_IMAGE_OK;
		return ass2;
	}

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Prepared to set up assembly '%s' (%s)", ass->aname.name, image->name);

	/* If asmctx is INDIVIDUAL, image->assembly might not be NULL, so don't
	 * overwrite it. */
	if (image->assembly == NULL)
		image->assembly = ass;

	loaded_assemblies = g_list_prepend (loaded_assemblies, ass);
	mono_assemblies_unlock ();

#ifdef HOST_WIN32
	if (m_image_is_module_handle (image))
		mono_image_fixup_vtable (image);
#endif

	mono_assembly_invoke_load_hook (ass);

	MONO_PROFILER_RAISE (assembly_loaded, (ass));
	
	return ass;
}

/**
 * mono_assembly_load_from:
 * \param image Image to load the assembly from
 * \param fname assembly name to associate with the assembly
 * \param status return status code
 *
 * If the provided \p image has an assembly reference, it will process the given
 * image as an assembly with the given name.
 *
 * Most likely you want to use the `api:mono_assembly_load_full` method instead.
 *
 * This is equivalent to calling `api:mono_assembly_load_from_full` with the
 * \p refonly parameter set to FALSE.
 * \returns A valid pointer to a \c MonoAssembly* on success and then \p status will be
 * set to \c MONO_IMAGE_OK; or NULL on error.
 *
 * If there is an error loading the assembly the \p status will indicate the
 * reason with \p status being set to \c MONO_IMAGE_INVALID if the
 * image did not contain an assembly reference table.
 
 */
MonoAssembly *
mono_assembly_load_from (MonoImage *image, const char *fname,
			 MonoImageOpenStatus *status)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyLoadRequest req;
	MonoImageOpenStatus def_status;
	if (!status)
		status = &def_status;
	mono_assembly_request_prepare (&req, sizeof (req), MONO_ASMCTX_DEFAULT);
	res = mono_assembly_request_load_from (image, fname, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_assembly_name_free:
 * \param aname assembly name to free
 * 
 * Frees the provided assembly name object.
 * (it does not frees the object itself, only the name members).
 */
void
mono_assembly_name_free (MonoAssemblyName *aname)
{
	MONO_ENTER_GC_UNSAFE;
	mono_assembly_name_free_internal (aname);
	MONO_EXIT_GC_UNSAFE;
}

void
mono_assembly_name_free_internal (MonoAssemblyName *aname)
{
	MONO_REQ_GC_UNSAFE_MODE;

	if (aname == NULL)
		return;

	g_free ((void *) aname->name);
	g_free ((void *) aname->culture);
	g_free ((void *) aname->hash_value);
	g_free ((guint8*) aname->public_key);
}

static gboolean
parse_public_key (const gchar *key, gchar** pubkey, gboolean *is_ecma)
{
	const gchar *pkey;
	gchar header [16], val, *arr, *endp;
	gint i, j, offset, bitlen, keylen, pkeylen;

	//both pubkey and is_ecma are required arguments
	g_assert (pubkey && is_ecma);

	keylen = strlen (key) >> 1;
	if (keylen < 1)
		return FALSE;

	/* allow the ECMA standard key */
	if (strcmp (key, "00000000000000000400000000000000") == 0) {
		*pubkey = NULL;
		*is_ecma = TRUE;
		return TRUE;
	}
	*is_ecma = FALSE;
	val = g_ascii_xdigit_value (key [0]) << 4;
	val |= g_ascii_xdigit_value (key [1]);
	switch (val) {
		case 0x00:
			if (keylen < 13)
				return FALSE;
			val = g_ascii_xdigit_value (key [24]);
			val |= g_ascii_xdigit_value (key [25]);
			if (val != 0x06)
				return FALSE;
			pkey = key + 24;
			break;
		case 0x06:
			pkey = key;
			break;
		default:
			return FALSE;
	}
		
	/* We need the first 16 bytes
	* to check whether this key is valid or not */
	pkeylen = strlen (pkey) >> 1;
	if (pkeylen < 16)
		return FALSE;
		
	for (i = 0, j = 0; i < 16; i++) {
		header [i] = g_ascii_xdigit_value (pkey [j++]) << 4;
		header [i] |= g_ascii_xdigit_value (pkey [j++]);
	}

	if (header [0] != 0x06 || /* PUBLICKEYBLOB (0x06) */
			header [1] != 0x02 || /* Version (0x02) */
			header [2] != 0x00 || /* Reserved (word) */
			header [3] != 0x00 ||
			(guint)(read32 (header + 8)) != 0x31415352) /* DWORD magic = RSA1 */
		return FALSE;

	/* Based on this length, we _should_ be able to know if the length is right */
	bitlen = read32 (header + 12) >> 3;
	if ((bitlen + 16 + 4) != pkeylen)
		return FALSE;
		
	arr = (gchar *)g_malloc (keylen + 4);
	/* Encode the size of the blob */
	mono_metadata_encode_value (keylen, &arr[0], &endp);
	offset = (gint)(endp-arr);
		
	for (i = offset, j = 0; i < keylen + offset; i++) {
		arr [i] = g_ascii_xdigit_value (key [j++]) << 4;
		arr [i] |= g_ascii_xdigit_value (key [j++]);
	}

	*pubkey = arr;

	return TRUE;
}

static gboolean
build_assembly_name (const char *name, const char *version, const char *culture, const char *token, const char *key, guint32 flags, guint32 arch, MonoAssemblyName *aname, gboolean save_public_key)
{
	gint len;
	gint version_parts;
	gchar *pkeyptr, *encoded, tok [8];

	memset (aname, 0, sizeof (MonoAssemblyName));

	if (version) {
#ifdef ENABLE_NETCORE
		int parts [4];
		int i;
		int part_len;

		parts [2] = -1;
		parts [3] = -1;
		const char *s = version;
		version_parts = 0;
		for (i = 0; i < 4; ++i) {
			int n = sscanf (s, "%u%n", &parts [i], &part_len);
			if (n != 1)
				return FALSE;
			if (parts [i] < 0 || parts [i] > 65535)
				return FALSE;
			if (i < 2 && parts [i] == 65535)
				return FALSE;
			version_parts ++;
			s += part_len;
			if (s [0] == '\0')
				break;
			if (i < 3) {
				if (s [0] != '.')
					return FALSE;
				s ++;
			}
		}
		if (s [0] != '\0')
			return FALSE;
		if (version_parts < 2 || version_parts > 4)
			return FALSE;
		aname->major = parts [0];
		aname->minor = parts [1];
		if (version_parts >= 3)
			aname->build = parts [2];
		else
			aname->build = -1;
		if (version_parts == 4)
			aname->revision = parts [3];
		else
			aname->revision = -1;
#else
		gint major, minor, build, revision;

		version_parts = sscanf (version, "%u.%u.%u.%u", &major, &minor, &build, &revision);
		if (version_parts < 2 || version_parts > 4)
			return FALSE;

		/* FIXME: we should set build & revision to -1 (instead of 0)
		if these are not set in the version string. That way, later on,
		we can still determine if these were specified.	*/
		aname->major = major;
		aname->minor = minor;
		if (version_parts >= 3)
			aname->build = build;
		else
			aname->build = 0;
		if (version_parts == 4)
			aname->revision = revision;
		else
			aname->revision = 0;
#endif
	}
	
	aname->flags = flags;
	aname->arch = arch;
	aname->name = g_strdup (name);
	
	if (culture) {
		if (g_ascii_strcasecmp (culture, "neutral") == 0)
			aname->culture = g_strdup ("");
		else
			aname->culture = g_strdup (culture);
	}
	
	if (token && strncmp (token, "null", 4) != 0) {
		char *lower;

		/* the constant includes the ending NULL, hence the -1 */
		if (strlen (token) != (MONO_PUBLIC_KEY_TOKEN_LENGTH - 1)) {
			mono_assembly_name_free (aname);
			return FALSE;
		}
		lower = g_ascii_strdown (token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_strlcpy ((char*)aname->public_key_token, lower, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (lower);
	}

	if (key) {
		gboolean is_ecma = FALSE;
		gchar *pkey = NULL;
		if (strcmp (key, "null") == 0 || !parse_public_key (key, &pkey, &is_ecma)) {
			mono_assembly_name_free (aname);
			return FALSE;
		}

		if (is_ecma) {
			g_assert (pkey == NULL);
			aname->public_key = NULL;
			g_strlcpy ((gchar*)aname->public_key_token, "b77a5c561934e089", MONO_PUBLIC_KEY_TOKEN_LENGTH);
			return TRUE;
		}
		
		len = mono_metadata_decode_blob_size ((const gchar *) pkey, (const gchar **) &pkeyptr);
		// We also need to generate the key token
		mono_digest_get_public_token ((guchar*) tok, (guint8*) pkeyptr, len);
		encoded = encode_public_tok ((guchar*) tok, 8);
		g_strlcpy ((gchar*)aname->public_key_token, encoded, MONO_PUBLIC_KEY_TOKEN_LENGTH);
		g_free (encoded);

		if (save_public_key)
			aname->public_key = (guint8*) pkey;
		else
			g_free (pkey);
	}

	return TRUE;
}

static gboolean
parse_assembly_directory_name (const char *name, const char *dirname, MonoAssemblyName *aname)
{
	gchar **parts;
	gboolean res;
	
	parts = g_strsplit (dirname, "_", 3);
	if (!parts || !parts[0] || !parts[1] || !parts[2]) {
		g_strfreev (parts);
		return FALSE;
	}
	
	res = build_assembly_name (name, parts[0], parts[1], parts[2], NULL, 0, 0, aname, FALSE);
	g_strfreev (parts);
	return res;
}

static gboolean
split_key_value (const gchar *pair, gchar **key, guint32 *keylen, gchar **value)
{
	char *eqsign = (char*)strchr (pair, '=');
	if (!eqsign) {
		*key = NULL;
		*keylen = 0;
		*value = NULL;
		return FALSE;
	}

	*key = (gchar*)pair;
	*keylen = eqsign - *key;
	while (*keylen > 0 && g_ascii_isspace ((*key) [*keylen - 1]))
		(*keylen)--;
	*value = g_strstrip (eqsign + 1);
	return TRUE;
}

gboolean
mono_assembly_name_parse_full (const char *name, MonoAssemblyName *aname, gboolean save_public_key, gboolean *is_version_defined, gboolean *is_token_defined)
{
	gchar *dllname;
	gchar *dllname_uq;
	gchar *version = NULL;
	gchar *version_uq;
	gchar *culture = NULL;
	gchar *culture_uq;
	gchar *token = NULL;
	gchar *token_uq;
	gchar *key = NULL;
	gchar *key_uq;
	gchar *retargetable = NULL;
	gchar *retargetable_uq;
	gchar *procarch;
	gchar *procarch_uq;
	gboolean res;
	gchar *value, *part_name;
	guint32 part_name_len;
	gchar **parts;
	gchar **tmp;
	gboolean version_defined;
	gboolean token_defined;
	guint32 flags = 0;
	guint32 arch = MONO_PROCESSOR_ARCHITECTURE_NONE;

	if (!is_version_defined)
		is_version_defined = &version_defined;
	*is_version_defined = FALSE;
	if (!is_token_defined)
		is_token_defined = &token_defined;
	*is_token_defined = FALSE;
	
	parts = tmp = g_strsplit (name, ",", 6);
	if (!tmp || !*tmp) {
		g_strfreev (tmp);
		return FALSE;
	}

	dllname = g_strstrip (*tmp);
	
	tmp++;

	while (*tmp) {
		if (!split_key_value (g_strstrip (*tmp), &part_name, &part_name_len, &value))
			goto cleanup_and_fail;

		if (part_name_len == 7 && !g_ascii_strncasecmp (part_name, "Version", part_name_len)) {
			*is_version_defined = TRUE;
			version = value;
			if (strlen (version) == 0) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 7 && !g_ascii_strncasecmp (part_name, "Culture", part_name_len)) {
			culture = value;
			if (strlen (culture) == 0) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 14 && !g_ascii_strncasecmp (part_name, "PublicKeyToken", part_name_len)) {
			*is_token_defined = TRUE;
			token = value;
			if (strlen (token) == 0) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 9 && !g_ascii_strncasecmp (part_name, "PublicKey", part_name_len)) {
			key = value;
			if (strlen (key) == 0) {
				goto cleanup_and_fail;
			}
			tmp++;
			continue;
		}

		if (part_name_len == 12 && !g_ascii_strncasecmp (part_name, "Retargetable", part_name_len)) {
			retargetable = value;
			retargetable_uq = unquote (retargetable);
			if (retargetable_uq != NULL)
				retargetable = retargetable_uq;

			if (!g_ascii_strcasecmp (retargetable, "yes")) {
				flags |= ASSEMBLYREF_RETARGETABLE_FLAG;
			} else if (g_ascii_strcasecmp (retargetable, "no")) {
				g_free (retargetable_uq);
				goto cleanup_and_fail;
			}

			g_free (retargetable_uq);
			tmp++;
			continue;
		}

		if (part_name_len == 21 && !g_ascii_strncasecmp (part_name, "ProcessorArchitecture", part_name_len)) {
			procarch = value;
			procarch_uq = unquote (procarch);
			if (procarch_uq != NULL)
				procarch = procarch_uq;

			if (!g_ascii_strcasecmp (procarch, "MSIL"))
				arch = MONO_PROCESSOR_ARCHITECTURE_MSIL;
			else if (!g_ascii_strcasecmp (procarch, "X86"))
				arch = MONO_PROCESSOR_ARCHITECTURE_X86;
			else if (!g_ascii_strcasecmp (procarch, "IA64"))
				arch = MONO_PROCESSOR_ARCHITECTURE_IA64;
			else if (!g_ascii_strcasecmp (procarch, "AMD64"))
				arch = MONO_PROCESSOR_ARCHITECTURE_AMD64;
			else if (!g_ascii_strcasecmp (procarch, "ARM"))
				arch = MONO_PROCESSOR_ARCHITECTURE_ARM;
			else {
				g_free (procarch_uq);
				goto cleanup_and_fail;
			}

			g_free (procarch_uq);
			tmp++;
			continue;
		}

		g_strfreev (parts);
		return FALSE;
	}

	/* if retargetable flag is set, then we must have a fully qualified name */
	if (retargetable != NULL && (version == NULL || culture == NULL || (key == NULL && token == NULL))) {
		goto cleanup_and_fail;
	}

	dllname_uq = unquote (dllname);
	version_uq = unquote (version);
	culture_uq = unquote (culture);
	token_uq = unquote (token);
	key_uq = unquote (key);

	res = build_assembly_name (
		dllname_uq == NULL ? dllname : dllname_uq,
		version_uq == NULL ? version : version_uq,
		culture_uq == NULL ? culture : culture_uq,
		token_uq == NULL ? token : token_uq,
		key_uq == NULL ? key : key_uq,
		flags, arch, aname, save_public_key);

	g_free (dllname_uq);
	g_free (version_uq);
	g_free (culture_uq);
	g_free (token_uq);
	g_free (key_uq);

	g_strfreev (parts);
	return res;

cleanup_and_fail:
	g_strfreev (parts);
	return FALSE;
}

static char*
unquote (const char *str)
{
	gint slen;
	const char *end;

	if (str == NULL)
		return NULL;

	slen = strlen (str);
	if (slen < 2)
		return NULL;

	if (*str != '\'' && *str != '\"')
		return NULL;

	end = str + slen - 1;
	if (*str != *end)
		return NULL;

	return g_strndup (str + 1, slen - 2);
}

/**
 * mono_assembly_name_parse:
 * \param name name to parse
 * \param aname the destination assembly name
 * 
 * Parses an assembly qualified type name and assigns the name,
 * version, culture and token to the provided assembly name object.
 *
 * \returns TRUE if the name could be parsed.
 */
gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname)
{
	return mono_assembly_name_parse_full (name, aname, FALSE, NULL, NULL);
}

/**
 * mono_assembly_name_new:
 * \param name name to parse
 *
 * Allocate a new \c MonoAssemblyName and fill its values from the
 * passed \p name.
 *
 * \returns a newly allocated structure or NULL if there was any failure.
 */
MonoAssemblyName*
mono_assembly_name_new (const char *name)
{
	MonoAssemblyName *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyName *aname = g_new0 (MonoAssemblyName, 1);
	if (mono_assembly_name_parse (name, aname))
		result = aname;
	else
		g_free (aname);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_assembly_name_get_name:
 */
const char*
mono_assembly_name_get_name (MonoAssemblyName *aname)
{
	const char *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	result = aname->name;
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_assembly_name_get_culture:
 */
const char*
mono_assembly_name_get_culture (MonoAssemblyName *aname)
{
	const char *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	result = aname->culture;
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/**
 * mono_assembly_name_get_pubkeytoken:
 */
mono_byte*
mono_assembly_name_get_pubkeytoken (MonoAssemblyName *aname)
{
	if (aname->public_key_token [0])
		return aname->public_key_token;
	return NULL;
}

/**
 * mono_assembly_name_get_version:
 */
uint16_t
mono_assembly_name_get_version (MonoAssemblyName *aname, uint16_t *minor, uint16_t *build, uint16_t *revision)
{
	if (minor)
		*minor = aname->minor;
	if (build)
		*build = aname->build;
	if (revision)
		*revision = aname->revision;
	return aname->major;
}

static MonoAssembly*
probe_for_partial_name (const char *basepath, const char *fullname, MonoAssemblyName *aname, MonoImageOpenStatus *status)
{
	gchar *fullpath = NULL;
	GDir *dirhandle;
	const char* direntry;
	MonoAssemblyName gac_aname;
	gint major=-1, minor=0, build=0, revision=0;
	gboolean exact_version;
	
	dirhandle = g_dir_open (basepath, 0, NULL);
	if (!dirhandle)
		return NULL;
		
	exact_version = (aname->major | aname->minor | aname->build | aname->revision) != 0;

	while ((direntry = g_dir_read_name (dirhandle))) {
		gboolean match = TRUE;
		
		if(!parse_assembly_directory_name (aname->name, direntry, &gac_aname))
			continue;
		
		if (aname->culture != NULL && strcmp (aname->culture, gac_aname.culture) != 0)
			match = FALSE;
			
		if (match && strlen ((char*)aname->public_key_token) > 0 && 
				!mono_public_tokens_are_equal (aname->public_key_token, gac_aname.public_key_token))
			match = FALSE;
		
		if (match) {
			if (exact_version) {
				match = (aname->major == gac_aname.major && aname->minor == gac_aname.minor &&
						 aname->build == gac_aname.build && aname->revision == gac_aname.revision); 
			}
			else if (gac_aname.major < major)
				match = FALSE;
			else if (gac_aname.major == major) {
				if (gac_aname.minor < minor)
					match = FALSE;
				else if (gac_aname.minor == minor) {
					if (gac_aname.build < build)
						match = FALSE;
					else if (gac_aname.build == build && gac_aname.revision <= revision)
						match = FALSE; 
				}
			}
		}
		
		if (match) {
			major = gac_aname.major;
			minor = gac_aname.minor;
			build = gac_aname.build;
			revision = gac_aname.revision;
			g_free (fullpath);
			fullpath = g_build_path (G_DIR_SEPARATOR_S, basepath, direntry, fullname, NULL);
		}

		mono_assembly_name_free (&gac_aname);
	}
	
	g_dir_close (dirhandle);
	
	if (fullpath == NULL)
		return NULL;
	else {
		MonoAssemblyOpenRequest req;
		mono_assembly_request_prepare (&req.request, sizeof (req), MONO_ASMCTX_DEFAULT);
		MonoAssembly *res = mono_assembly_request_open (fullpath, &req, status);
		g_free (fullpath);
		return res;
	}
}

/**
 * mono_assembly_load_with_partial_name:
 * \param name an assembly name that is then parsed by `api:mono_assembly_name_parse`.
 * \param status return status code
 *
 * Loads a \c MonoAssembly from a name.  The name is parsed using `api:mono_assembly_name_parse`,
 * so it might contain a qualified type name, version, culture and token.
 *
 * This will load the assembly from the file whose name is derived from the assembly name
 * by appending the \c .dll extension.
 *
 * The assembly is loaded from either one of the extra Global Assembly Caches specified
 * by the extra GAC paths (specified by the \c MONO_GAC_PREFIX environment variable) or
 * if that fails from the GAC.
 *
 * \returns NULL on failure, or a pointer to a \c MonoAssembly on success.
 */
MonoAssembly*
mono_assembly_load_with_partial_name (const char *name, MonoImageOpenStatus *status)
{
	MonoAssembly *result;
	MONO_ENTER_GC_UNSAFE;
	MonoImageOpenStatus def_status;
	if (!status)
		status = &def_status;
	result = mono_assembly_load_with_partial_name_internal (name, status);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

MonoAssembly*
mono_assembly_load_with_partial_name_internal (const char *name, MonoImageOpenStatus *status)
{
	ERROR_DECL (error);
	MonoAssembly *res;
	MonoAssemblyName *aname, base_name;
	MonoAssemblyName mapped_aname;
	gchar *fullname, *gacpath;
	gchar **paths;

	MONO_REQ_GC_UNSAFE_MODE;

	g_assert (status != NULL);

	memset (&base_name, 0, sizeof (MonoAssemblyName));
	aname = &base_name;

	if (!mono_assembly_name_parse (name, aname))
		return NULL;

	/* 
	 * If no specific version has been requested, make sure we load the
	 * correct version for system assemblies.
	 */ 
	if ((aname->major | aname->minor | aname->build | aname->revision) == 0)
		aname = mono_assembly_remap_version (aname, &mapped_aname);
	
	res = mono_assembly_loaded_full (aname, FALSE);
	if (res) {
		mono_assembly_name_free (aname);
		return res;
	}

	res = invoke_assembly_preload_hook (aname, assemblies_path);
	if (res) {
		res->in_gac = FALSE;
		mono_assembly_name_free (aname);
		return res;
	}

	fullname = g_strdup_printf ("%s.dll", aname->name);

	if (extra_gac_paths) {
		paths = extra_gac_paths;
		while (!res && *paths) {
			gacpath = g_build_path (G_DIR_SEPARATOR_S, *paths, "lib", "mono", "gac", aname->name, NULL);
			res = probe_for_partial_name (gacpath, fullname, aname, status);
			g_free (gacpath);
			paths++;
		}
	}

	if (res) {
		res->in_gac = TRUE;
		g_free (fullname);
		mono_assembly_name_free (aname);
		return res;
	}

	gacpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (), "mono", "gac", aname->name, NULL);
	res = probe_for_partial_name (gacpath, fullname, aname, status);
	g_free (gacpath);

	g_free (fullname);
	mono_assembly_name_free (aname);

	if (res)
		res->in_gac = TRUE;
	else {
		MonoDomain *domain = mono_domain_get ();

		res = mono_try_assembly_resolve (domain, name, NULL, FALSE, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error);
			if (*status == MONO_IMAGE_OK)
				*status = MONO_IMAGE_IMAGE_INVALID;
		}
	}

	return res;
}

static MonoBoolean
mono_assembly_is_in_gac (const gchar *filename)
{
	const gchar *rootdir;
	gchar *gp;
	gchar **paths;

	if (filename == NULL)
		return FALSE;

	for (paths = extra_gac_paths; paths && *paths; paths++) {
		if (strstr (*paths, filename) != *paths)
			continue;

		gp = (gchar *) (filename + strlen (*paths));
		if (*gp != G_DIR_SEPARATOR)
			continue;
		gp++;
		if (strncmp (gp, "lib", 3))
			continue;
		gp += 3;
		if (*gp != G_DIR_SEPARATOR)
			continue;
		gp++;
		if (strncmp (gp, "mono", 4))
			continue;
		gp += 4;
		if (*gp != G_DIR_SEPARATOR)
			continue;
		gp++;
		if (strncmp (gp, "gac", 3))
			continue;
		gp += 3;
		if (*gp != G_DIR_SEPARATOR)
			continue;

		return TRUE;
	}

	rootdir = mono_assembly_getrootdir ();
	if (strstr (filename, rootdir) != filename)
		return FALSE;

	gp = (gchar *) (filename + strlen (rootdir));
	if (*gp != G_DIR_SEPARATOR)
		return FALSE;
	gp++;
	if (strncmp (gp, "mono", 4))
		return FALSE;
	gp += 4;
	if (*gp != G_DIR_SEPARATOR)
		return FALSE;
	gp++;
	if (strncmp (gp, "gac", 3))
		return FALSE;
	gp += 3;
	if (*gp != G_DIR_SEPARATOR)
		return FALSE;
	return TRUE;
}

static MonoImage*
mono_assembly_load_publisher_policy (MonoAssemblyName *aname)
{
	MonoImage *image;
	gchar *filename, *pname, *name, *culture, *version, *fullpath, *subpath;
	gchar **paths;
	gint32 len;

	if (strstr (aname->name, ".dll")) {
		len = strlen (aname->name) - 4;
		name = (gchar *)g_malloc (len + 1);
		memcpy (name, aname->name, len);
		name[len] = 0;
	} else
		name = g_strdup (aname->name);
	
	if (aname->culture)
		culture = g_utf8_strdown (aname->culture, -1);
	else
		culture = g_strdup ("");
	
	pname = g_strdup_printf ("policy.%d.%d.%s", aname->major, aname->minor, name);
	version = g_strdup_printf ("0.0.0.0_%s_%s", culture, aname->public_key_token);
	g_free (name);
	g_free (culture);
	
	filename = g_strconcat (pname, ".dll", NULL);
	subpath = g_build_path (G_DIR_SEPARATOR_S, pname, version, filename, NULL);
	g_free (pname);
	g_free (version);
	g_free (filename);

	image = NULL;
	if (extra_gac_paths) {
		paths = extra_gac_paths;
		while (!image && *paths) {
			fullpath = g_build_path (G_DIR_SEPARATOR_S, *paths,
					"lib", "mono", "gac", subpath, NULL);
			image = mono_image_open (fullpath, NULL);
			g_free (fullpath);
			paths++;
		}
	}

	if (image) {
		g_free (subpath);
		return image;
	}

	fullpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (), 
			"mono", "gac", subpath, NULL);
	image = mono_image_open (fullpath, NULL);
	g_free (subpath);
	g_free (fullpath);
	
	return image;
}

static MonoAssemblyName*
mono_assembly_bind_version (MonoAssemblyBindingInfo *info, MonoAssemblyName *aname, MonoAssemblyName *dest_name)
{
	memcpy (dest_name, aname, sizeof (MonoAssemblyName));
	dest_name->major = info->new_version.major;
	dest_name->minor = info->new_version.minor;
	dest_name->build = info->new_version.build;
	dest_name->revision = info->new_version.revision;
	
	return dest_name;
}

/* LOCKING: assembly_binding lock must be held */
static MonoAssemblyBindingInfo*
search_binding_loaded (MonoAssemblyName *aname)
{
	GSList *tmp;

	for (tmp = loaded_assembly_bindings; tmp; tmp = tmp->next) {
		MonoAssemblyBindingInfo *info = (MonoAssemblyBindingInfo *)tmp->data;
		if (assembly_binding_maps_name (info, aname))
			return info;
	}

	return NULL;
}

static inline gboolean
info_compare_versions (AssemblyVersionSet *left, AssemblyVersionSet *right)
{
	if (left->major != right->major || left->minor != right->minor ||
	    left->build != right->build || left->revision != right->revision)
		return FALSE;

	return TRUE;
}

static inline gboolean
info_versions_equal (MonoAssemblyBindingInfo *left, MonoAssemblyBindingInfo *right)
{
	if (left->has_old_version_bottom != right->has_old_version_bottom)
		return FALSE;

	if (left->has_old_version_top != right->has_old_version_top)
		return FALSE;

	if (left->has_new_version != right->has_new_version)
		return FALSE;

	if (left->has_old_version_bottom && !info_compare_versions (&left->old_version_bottom, &right->old_version_bottom))
		return FALSE;

	if (left->has_old_version_top && !info_compare_versions (&left->old_version_top, &right->old_version_top))
		return FALSE;

	if (left->has_new_version && !info_compare_versions (&left->new_version, &right->new_version))
		return FALSE;

	return TRUE;
}

/* LOCKING: assumes all the necessary locks are held */
static void
assembly_binding_info_parsed (MonoAssemblyBindingInfo *info, void *user_data)
{
	MonoAssemblyBindingInfo *info_copy;
	GSList *tmp;
	MonoAssemblyBindingInfo *info_tmp;
	MonoDomain *domain = (MonoDomain*)user_data;

	if (!domain)
		return;

	if (info->has_new_version && mono_assembly_is_problematic_version (info->name, info->new_version.major, info->new_version.minor, info->new_version.build, info->new_version.revision)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Discarding assembly binding to problematic version %s v%d.%d.%d.%d",
			info->name, info->new_version.major, info->new_version.minor, info->new_version.build, info->new_version.revision);
		return;
	}

	for (tmp = domain->assembly_bindings; tmp; tmp = tmp->next) {
		info_tmp = (MonoAssemblyBindingInfo *)tmp->data;
		if (strcmp (info->name, info_tmp->name) == 0 && info_versions_equal (info, info_tmp))
			return;
	}

	info_copy = (MonoAssemblyBindingInfo *)mono_mempool_alloc0 (domain->mp, sizeof (MonoAssemblyBindingInfo));
	memcpy (info_copy, info, sizeof (MonoAssemblyBindingInfo));
	if (info->name)
		info_copy->name = mono_mempool_strdup (domain->mp, info->name);
	if (info->culture)
		info_copy->culture = mono_mempool_strdup (domain->mp, info->culture);

	domain->assembly_bindings = g_slist_append_mempool (domain->mp, domain->assembly_bindings, info_copy);
}

static int
get_version_number (int major, int minor)
{
	return major * 256 + minor;
}

static inline gboolean
info_major_minor_in_range (MonoAssemblyBindingInfo *info, MonoAssemblyName *aname)
{
	int aname_version_number = get_version_number (aname->major, aname->minor);
	if (!info->has_old_version_bottom)
		return FALSE;

	if (get_version_number (info->old_version_bottom.major, info->old_version_bottom.minor) > aname_version_number)
		return FALSE;

	if (info->has_old_version_top && get_version_number (info->old_version_top.major, info->old_version_top.minor) < aname_version_number)
		return FALSE;

	/* This is not the nicest way to do it, but it's a by-product of the way parsing is done */
	info->major = aname->major;
	info->minor = aname->minor;

	return TRUE;
}

/* LOCKING: Assumes that we are already locked - both loader and domain locks */
static MonoAssemblyBindingInfo*
get_per_domain_assembly_binding_info (MonoDomain *domain, MonoAssemblyName *aname)
{
	MonoAssemblyBindingInfo *info;
	GSList *list;

	if (!domain->assembly_bindings)
		return NULL;

	info = NULL;
	for (list = domain->assembly_bindings; list; list = list->next) {
		info = (MonoAssemblyBindingInfo *)list->data;
		if (info && !strcmp (aname->name, info->name) && info_major_minor_in_range (info, aname))
			break;
		info = NULL;
	}

	if (info) {
		if (info->name && info->public_key_token [0] && info->has_old_version_bottom &&
		    info->has_new_version && assembly_binding_maps_name (info, aname))
			info->is_valid = TRUE;
		else
			info->is_valid = FALSE;
	}

	return info;
}

void
mono_domain_parse_assembly_bindings (MonoDomain *domain, int amajor, int aminor, gchar *domain_config_file_name)
{
	if (domain->assembly_bindings_parsed)
		return;
	mono_domain_lock (domain);
	if (!domain->assembly_bindings_parsed) {

		gchar *domain_config_file_path = mono_portability_find_file (domain_config_file_name, TRUE);

		if (!domain_config_file_path)
			domain_config_file_path = domain_config_file_name;

		mono_config_parse_assembly_bindings (domain_config_file_path, amajor, aminor, domain, assembly_binding_info_parsed);
		domain->assembly_bindings_parsed = TRUE;
		if (domain_config_file_name != domain_config_file_path)
			g_free (domain_config_file_path);
	}

	mono_domain_unlock (domain);
}

static MonoAssemblyName*
mono_assembly_apply_binding (MonoAssemblyName *aname, MonoAssemblyName *dest_name)
{
	HANDLE_FUNCTION_ENTER ();

	ERROR_DECL (error);
	MonoAssemblyBindingInfo *info, *info2;
	MonoImage *ppimage;
	MonoDomain *domain;

	if (aname->public_key_token [0] == 0)
		goto return_aname;

	domain = mono_domain_get ();

	mono_assembly_binding_lock ();
	info = search_binding_loaded (aname);
	mono_assembly_binding_unlock ();

	if (!info) {
		mono_domain_lock (domain);
		info = get_per_domain_assembly_binding_info (domain, aname);
		mono_domain_unlock (domain);
	}

	if (info) {
		if (!check_policy_versions (info, aname))
			goto return_aname;
		
		mono_assembly_bind_version (info, aname, dest_name);
		goto return_dest_name;
	}

	MonoAppDomainSetupHandle setup;
	MonoStringHandle configuration_file;

	if (domain
			&& !MONO_HANDLE_IS_NULL (setup = MONO_HANDLE_NEW (MonoAppDomainSetup, domain->setup))
			&& !MONO_HANDLE_IS_NULL (configuration_file = MONO_HANDLE_NEW_GET (MonoString, setup, configuration_file))) {
		char *domain_config_file_name = mono_string_handle_to_utf8 (configuration_file, error);
		/* expect this to succeed because mono_domain_set_options_from_config () did
		 * the same thing when the domain was created. */
		mono_error_assert_ok (error);
		mono_domain_parse_assembly_bindings (domain, aname->major, aname->minor, domain_config_file_name);
		g_free (domain_config_file_name);

		mono_domain_lock (domain);
		info2 = get_per_domain_assembly_binding_info (domain, aname);

		if (info2) {
			info = (MonoAssemblyBindingInfo *)g_memdup (info2, sizeof (MonoAssemblyBindingInfo));
			info->name = g_strdup (info2->name);
			info->culture = g_strdup (info2->culture);
			info->domain_id = domain->domain_id;
		}

		mono_domain_unlock (domain);
	}

	if (!info) {
		info = g_new0 (MonoAssemblyBindingInfo, 1);
		info->major = aname->major;
		info->minor = aname->minor;
	}

	if (!info->is_valid) {
		ppimage = mono_assembly_load_publisher_policy (aname);
		if (ppimage) {
			get_publisher_policy_info (ppimage, aname, info);
			mono_image_close (ppimage);
		}
	}

	/* Define default error value if needed */
	if (!info->is_valid) {
		info->name = g_strdup (aname->name);
		info->culture = g_strdup (aname->culture);
		g_strlcpy ((char *)info->public_key_token, (const char *)aname->public_key_token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	}
	
	mono_assembly_binding_lock ();
	info2 = search_binding_loaded (aname);
	if (info2) {
		/* This binding was added by another thread 
		 * before us */
		mono_assembly_binding_info_free (info);
		g_free (info);
		
		info = info2;
	} else
		loaded_assembly_bindings = g_slist_prepend (loaded_assembly_bindings, info);
		
	mono_assembly_binding_unlock ();
	
	if (!info->is_valid || !check_policy_versions (info, aname))
		goto return_aname;

	mono_assembly_bind_version (info, aname, dest_name);
	goto return_dest_name;

	MonoAssemblyName* result;

return_dest_name:
	result = dest_name;
	goto exit;

return_aname:
	result = aname;
	goto exit;
exit:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

/**
 * mono_assembly_load_from_gac
 *
 * \param aname The assembly name object
 */
static MonoAssembly*
mono_assembly_load_from_gac (MonoAssemblyName *aname,  gchar *filename, MonoImageOpenStatus *status, MonoBoolean refonly)
{
	MonoAssembly *result = NULL;
	gchar *name, *version, *culture, *fullpath, *subpath;
	gint32 len;
	gchar **paths;
	char *pubtok;

	if (aname->public_key_token [0] == 0) {
		return NULL;
	}

	if (strstr (aname->name, ".dll")) {
		len = strlen (filename) - 4;
		name = (gchar *)g_malloc (len + 1);
		memcpy (name, aname->name, len);
		name[len] = 0;
	} else {
		name = g_strdup (aname->name);
	}

	if (aname->culture) {
		culture = g_utf8_strdown (aname->culture, -1);
	} else {
		culture = g_strdup ("");
	}

	pubtok = g_ascii_strdown ((char*)aname->public_key_token, MONO_PUBLIC_KEY_TOKEN_LENGTH);
	version = g_strdup_printf ("%d.%d.%d.%d_%s_%s", aname->major,
			aname->minor, aname->build, aname->revision,
			culture, pubtok);
	g_free (pubtok);
	
	subpath = g_build_path (G_DIR_SEPARATOR_S, name, version, filename, NULL);
	g_free (name);
	g_free (version);
	g_free (culture);

	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare (&req.request, sizeof (req), refonly ? MONO_ASMCTX_REFONLY : MONO_ASMCTX_DEFAULT);

	if (extra_gac_paths) {
		paths = extra_gac_paths;
		while (!result && *paths) {
			fullpath = g_build_path (G_DIR_SEPARATOR_S, *paths, "lib", "mono", "gac", subpath, NULL);
			result = mono_assembly_request_open (fullpath, &req, status);
			g_free (fullpath);
			paths++;
		}
	}

	if (result) {
		result->in_gac = TRUE;
		g_free (subpath);
		return result;
	}

	fullpath = g_build_path (G_DIR_SEPARATOR_S, mono_assembly_getrootdir (),
			"mono", "gac", subpath, NULL);
	result = mono_assembly_request_open (fullpath, &req, status);
	g_free (fullpath);

	if (result)
		result->in_gac = TRUE;
	
	g_free (subpath);

	return result;
}

MonoAssembly*
mono_assembly_load_corlib (const MonoRuntimeInfo *runtime, MonoImageOpenStatus *status)
{
	MonoAssemblyName *aname;
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare (&req.request, sizeof (req), MONO_ASMCTX_DEFAULT);

	if (corlib) {
		/* g_print ("corlib already loaded\n"); */
		return corlib;
	}

#ifdef ENABLE_NETCORE
	aname = mono_assembly_name_new (MONO_ASSEMBLY_CORLIB_NAME);
	corlib = invoke_assembly_preload_hook (aname, NULL);
	/* MonoCore preload hook should know how to find it */
	/* FIXME: AOT compiler comes here without an installed hook. */
	if (!corlib) {
		if (assemblies_path) { // Custom assemblies path set via MONO_PATH or mono_set_assemblies_path
			char *corlib_name = g_strdup_printf ("%s.dll", MONO_ASSEMBLY_CORLIB_NAME);
			corlib = load_in_path (corlib_name, (const char**)assemblies_path, &req, status);
		}
	}
	g_assert (corlib);
#else
	// A nonstandard preload hook may provide a special mscorlib assembly
	aname = mono_assembly_name_new ("mscorlib.dll");
	corlib = invoke_assembly_preload_hook (aname, assemblies_path);
	mono_assembly_name_free (aname);
	g_free (aname);
	if (corlib != NULL)
		goto return_corlib_and_facades;

	// This unusual directory layout can occur if mono is being built and run out of its own source repo
	if (assemblies_path) { // Custom assemblies path set via MONO_PATH or mono_set_assemblies_path
		corlib = load_in_path ("mscorlib.dll", (const char**)assemblies_path, &req, status);
		if (corlib)
			goto return_corlib_and_facades;
	}

	/* Normal case: Load corlib from mono/<version> */
	char *corlib_file;
	corlib_file = g_build_filename ("mono", runtime->framework_version, "mscorlib.dll", NULL);
	if (assemblies_path) { // Custom assemblies path
		corlib = load_in_path (corlib_file, (const char**)assemblies_path, &req, status);
		if (corlib) {
			g_free (corlib_file);
			goto return_corlib_and_facades;
		}
	}
	corlib = load_in_path (corlib_file, (const char**) default_path, &req, status);
	g_free (corlib_file);

return_corlib_and_facades:
	if (corlib)  // FIXME: stop hardcoding 4.5 here
		default_path [1] = g_strdup_printf ("%s/Facades", corlib->basedir);
#endif /*!ENABLE_NETCORE*/
		
	return corlib;
}

static MonoAssembly*
prevent_reference_assembly_from_running (MonoAssembly* candidate, gboolean refonly)
{
	ERROR_DECL (refasm_error);
	if (candidate && !refonly) {
		/* .NET Framework seems to not check for ReferenceAssemblyAttribute on dynamic assemblies */
		if (!image_is_dynamic (candidate->image) &&
		    mono_assembly_has_reference_assembly_attribute (candidate, refasm_error))
			candidate = NULL;
	}
	mono_error_cleanup (refasm_error);
	return candidate;
}

gboolean
mono_assembly_candidate_predicate_sn_same_name (MonoAssembly *candidate, gpointer ud)
{
	MonoAssemblyName *wanted_name = (MonoAssemblyName*)ud;
	MonoAssemblyName *candidate_name = &candidate->aname;

	g_assert (wanted_name != NULL);
	g_assert (candidate_name != NULL);

	if (mono_trace_is_traced (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY)) {
		char * s = mono_stringify_assembly_name (wanted_name);
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate: wanted = %s\n", s);
		g_free (s);
		s = mono_stringify_assembly_name (candidate_name);
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate: candidate = %s\n", s);
		g_free (s);
	}


	/* Wanted name has no token, not strongly named: always matches. */
	if (0 == wanted_name->public_key_token [0]) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate: wanted has no token, returning TRUE\n");
		return TRUE;
	}

	/* Candidate name has no token, not strongly named: never matches */
	if (0 == candidate_name->public_key_token [0]) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate: candidate has no token, returning FALSE\n");
		return FALSE;
	}

	return exact_sn_match (wanted_name, candidate_name) ||
		framework_assembly_sn_match (wanted_name, candidate_name);
}

gboolean
exact_sn_match (MonoAssemblyName *wanted_name, MonoAssemblyName *candidate_name)
{
#if ENABLE_NETCORE
	gboolean result = mono_assembly_names_equal_flags (wanted_name, candidate_name, MONO_ANAME_EQ_IGNORE_VERSION);
	if (result && assembly_names_compare_versions (wanted_name, candidate_name, -1) > 0)
		result = FALSE;
#else
	gboolean result = mono_assembly_names_equal (wanted_name, candidate_name);
#endif

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate: candidate and wanted names %s\n",
		    result ? "match, returning TRUE" : "don't match, returning FALSE");
	return result;

}

gboolean
framework_assembly_sn_match (MonoAssemblyName *wanted_name, MonoAssemblyName *candidate_name)
{
#ifndef DISABLE_DESKTOP_LOADER
	g_assert (wanted_name != NULL);
	g_assert (candidate_name != NULL);
	const AssemblyVersionMap *vmap = (AssemblyVersionMap *)g_hash_table_lookup (assembly_remapping_table, wanted_name->name);
	if (vmap) {
		if (!vmap->framework_facade_assembly) {
			/* If the wanted name is a framework assembly, it's enough for the name/version/culture to match.  If the assembly was remapped, the public key token is likely unrelated. */
			gboolean result = mono_assembly_names_equal_flags (wanted_name, candidate_name, MONO_ANAME_EQ_IGNORE_PUBKEY);
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate: candidate and wanted names %s (ignoring the public key token)", result ? "match, returning TRUE" : "don't match, returning FALSE");
			return result;
		} else {
			/* For facades, the name and public key token should
			 * match, but the version doesn't matter as long as the
			 * candidate is not older. */
			gboolean result = mono_assembly_names_equal_flags (wanted_name, candidate_name, MONO_ANAME_EQ_IGNORE_VERSION);
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate: candidate and wanted names %s (ignoring version)", result ? "match" : "don't match, returning FALSE");
			if (result) {
				// compare major of candidate and wanted
				int c = assembly_names_compare_versions (candidate_name, wanted_name, 1);
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Predicate: candidate major version is %s wanted major version, returning %s\n", c == 0 ? "the same as" : (c < 0 ? "lower than" : "greater than"),
					    (c >= 0) ? "TRUE" : "FALSE");
				return (c >= 0);  // don't accept a candidate that's older than wanted.
			} else {
				return FALSE;
			}
		}
	}
#endif
	return FALSE;
}

static MonoAssembly*
mono_assembly_request_byname_nosearch (MonoAssemblyName *aname,
				       const MonoAssemblyByNameRequest *req,
				       MonoImageOpenStatus *status)
{
	MonoAssembly *result;
	MonoAssemblyName maped_aname;
	MonoAssemblyName maped_name_pp;

	aname = mono_assembly_remap_version (aname, &maped_aname);

	const gboolean refonly = req->request.asmctx == MONO_ASMCTX_REFONLY;

	/* Reflection only assemblies don't get assembly binding */
	if (!refonly)
		aname = mono_assembly_apply_binding (aname, &maped_name_pp);

	result = mono_assembly_loaded_full (aname, refonly);
	if (result)
		return result;

	result = refonly ? invoke_assembly_refonly_preload_hook (aname, assemblies_path) : invoke_assembly_preload_hook (aname, assemblies_path);
	if (result) {
		result->in_gac = FALSE;
		return result;
	}

	return mono_assembly_load_full_gac_base_default (aname, req->basedir, req->request.asmctx, status);
}

/* Like mono_assembly_request_byname_nosearch, but don't ask the preload look (ie,
 * the appdomain) to run.  Just looks in the gac, the specified base dir or the
 * default_path.  Does NOT look in the appdomain application base or in the
 * MONO_PATH.
 */
MonoAssembly*
mono_assembly_load_full_gac_base_default (MonoAssemblyName *aname,
					  const char *basedir,
					  MonoAssemblyContextKind asmctx,
					  MonoImageOpenStatus *status)
{
	MonoAssembly *result;
	MonoAssemblyName maped_aname;
	char *fullpath, *filename;
	int ext_index;
	const char *ext;
	int len;

	/* If we remap e.g. 4.1.3.0 to 4.0.0.0, look in the 4.0.0.0
	 * GAC directory, not 4.1.3.0 */
	aname = mono_assembly_remap_version (aname, &maped_aname);

	/* Currently we retrieve the loaded corlib for reflection 
	 * only requests, like a common reflection only assembly 
	 */
	gboolean name_is_corlib = strcmp (aname->name, MONO_ASSEMBLY_CORLIB_NAME) == 0;
	/* Assembly.Load (new AssemblyName ("mscorlib.dll")) (respectively,
	 * "System.Private.CoreLib.dll" for netcore) is treated the same as
	 * "mscorlib" (resp "System.Private.CoreLib"). */
	name_is_corlib = name_is_corlib || strcmp (aname->name, MONO_ASSEMBLY_CORLIB_NAME ".dll") == 0;
	if (name_is_corlib) {
		return mono_assembly_load_corlib (mono_get_runtime_info (), status);
	}

	MonoAssemblyCandidatePredicate predicate = NULL;
	void* predicate_ud = NULL;
#if !defined(DISABLE_DESKTOP_LOADER)
	if (G_LIKELY (mono_loader_get_strict_strong_names ())) {
		predicate = &mono_assembly_candidate_predicate_sn_same_name;
		predicate_ud = aname;
	}
#endif

	const gboolean refonly = asmctx == MONO_ASMCTX_REFONLY;

	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare (&req.request, sizeof (req), asmctx);
	req.request.predicate = predicate;
	req.request.predicate_ud = predicate_ud;

	len = strlen (aname->name);
	for (ext_index = 0; ext_index < 2; ext_index ++) {
		ext = ext_index == 0 ? ".dll" : ".exe";
		if (len > 4 && (!strcmp (aname->name + len - 4, ".dll") || !strcmp (aname->name + len - 4, ".exe"))) {
			filename = g_strdup (aname->name);
			/* Don't try appending .dll/.exe if it already has one of those extensions */
			ext_index++;
		} else {
			filename = g_strconcat (aname->name, ext, NULL);
		}

		result = mono_assembly_load_from_gac (aname, filename, status, refonly);
		if (result) {
			g_free (filename);
			return result;
		}

		if (basedir) {
			fullpath = g_build_filename (basedir, filename, NULL);
			result = mono_assembly_request_open (fullpath, &req, status);
			g_free (fullpath);
			if (result) {
				result->in_gac = FALSE;
				g_free (filename);
				return result;
			}
		}

		result = load_in_path (filename, (const char**) default_path, &req, status);
		if (result)
			result->in_gac = FALSE;
		g_free (filename);
		if (result)
			return result;
	}

	return result;
}

MonoAssembly*
mono_assembly_request_byname (MonoAssemblyName *aname, const MonoAssemblyByNameRequest *req, MonoImageOpenStatus *status)
{
	MonoAssembly *result = mono_assembly_request_byname_nosearch (aname, req, status);
	const gboolean refonly = req->request.asmctx == MONO_ASMCTX_REFONLY;

	if (!result && !req->no_postload_search) {
		/* Try a postload search hook */
		result = mono_assembly_invoke_search_hook_internal (aname, req->requesting_assembly, refonly, TRUE);
		result = prevent_reference_assembly_from_running (result, refonly);
	}
	return result;
}

/**
 * mono_assembly_load_full:
 * \param aname A MonoAssemblyName with the assembly name to load.
 * \param basedir A directory to look up the assembly at.
 * \param status a pointer to a MonoImageOpenStatus to return the status of the load operation
 * \param refonly Whether this assembly is being opened in "reflection-only" mode.
 *
 * Loads the assembly referenced by \p aname, if the value of \p basedir is not NULL, it
 * attempts to load the assembly from that directory before probing the standard locations.
 *
 * If the assembly is being opened in reflection-only mode (\p refonly set to TRUE) then no 
 * assembly binding takes place.
 *
 * \returns the assembly referenced by \p aname loaded or NULL on error. On error the
 * value pointed by \p status is updated with an error code.
 */
MonoAssembly*
mono_assembly_load_full (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status, gboolean refonly)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyByNameRequest req;
	mono_assembly_request_prepare (&req.request, sizeof (req), refonly ? MONO_ASMCTX_REFONLY : MONO_ASMCTX_DEFAULT);
	req.requesting_assembly = NULL;
	req.basedir = basedir;
	res = mono_assembly_request_byname (aname, &req, status);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

/**
 * mono_assembly_load:
 * \param aname A MonoAssemblyName with the assembly name to load.
 * \param basedir A directory to look up the assembly at.
 * \param status a pointer to a MonoImageOpenStatus to return the status of the load operation
 *
 * Loads the assembly referenced by \p aname, if the value of \p basedir is not NULL, it
 * attempts to load the assembly from that directory before probing the standard locations.
 *
 * \returns the assembly referenced by \p aname loaded or NULL on error. On error the
 * value pointed by \p status is updated with an error code.
 */
MonoAssembly*
mono_assembly_load (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status)
{
	MonoAssemblyByNameRequest req;
	mono_assembly_request_prepare (&req.request, sizeof (req), MONO_ASMCTX_DEFAULT);
	req.requesting_assembly = NULL;
	req.basedir = basedir;
	return mono_assembly_request_byname (aname, &req, status);
}

/**
 * mono_assembly_loaded_full:
 * \param aname an assembly to look for.
 * \param refonly Whether this assembly is being opened in "reflection-only" mode.
 *
 * This is used to determine if the specified assembly has been loaded
 * \returns NULL If the given \p aname assembly has not been loaded, or a pointer to
 * a \c MonoAssembly that matches the \c MonoAssemblyName specified.
 */
MonoAssembly*
mono_assembly_loaded_full (MonoAssemblyName *aname, gboolean refonly)
{
	MonoAssembly *res;
	MonoAssemblyName maped_aname;

	aname = mono_assembly_remap_version (aname, &maped_aname);

	res = mono_assembly_invoke_search_hook_internal (aname, NULL, refonly, FALSE);

	return res;
}

/**
 * mono_assembly_loaded:
 * \param aname an assembly to look for.
 *
 * This is used to determine if the specified assembly has been loaded
 
 * \returns NULL If the given \p aname assembly has not been loaded, or a pointer to
 * a \c MonoAssembly that matches the \c MonoAssemblyName specified.
 */
MonoAssembly*
mono_assembly_loaded (MonoAssemblyName *aname)
{
	MonoAssembly *res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_assembly_loaded_full (aname, FALSE);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

void
mono_assembly_release_gc_roots (MonoAssembly *assembly)
{
	if (assembly == NULL || assembly == REFERENCE_MISSING)
		return;

	if (assembly_is_dynamic (assembly)) {
		int i;
		MonoDynamicImage *dynimg = (MonoDynamicImage *)assembly->image;
		for (i = 0; i < dynimg->image.module_count; ++i)
			mono_dynamic_image_release_gc_roots ((MonoDynamicImage *)dynimg->image.modules [i]);
		mono_dynamic_image_release_gc_roots (dynimg);
	}
}

/*
 * Returns whether mono_assembly_close_finish() must be called as
 * well.  See comment for mono_image_close_except_pools() for why we
 * unload in two steps.
 */
gboolean
mono_assembly_close_except_image_pools (MonoAssembly *assembly)
{
	GSList *tmp;
	g_return_val_if_fail (assembly != NULL, FALSE);

	if (assembly == REFERENCE_MISSING)
		return FALSE;

	/* Might be 0 already */
	if (mono_atomic_dec_i32 (&assembly->ref_count) > 0)
		return FALSE;

	MONO_PROFILER_RAISE (assembly_unloading, (assembly));

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_ASSEMBLY, "Unloading assembly %s [%p].", assembly->aname.name, assembly);

	mono_debug_close_image (assembly->image);

	mono_assemblies_lock ();
	loaded_assemblies = g_list_remove (loaded_assemblies, assembly);
	mono_assemblies_unlock ();

	assembly->image->assembly = NULL;

	if (!mono_image_close_except_pools (assembly->image))
		assembly->image = NULL;

	for (tmp = assembly->friend_assembly_names; tmp; tmp = tmp->next) {
		MonoAssemblyName *fname = (MonoAssemblyName *)tmp->data;
		mono_assembly_name_free (fname);
		g_free (fname);
	}
	g_slist_free (assembly->friend_assembly_names);
	g_free (assembly->basedir);

	MONO_PROFILER_RAISE (assembly_unloaded, (assembly));

	return TRUE;
}

void
mono_assembly_close_finish (MonoAssembly *assembly)
{
	g_assert (assembly && assembly != REFERENCE_MISSING);

	if (assembly->image)
		mono_image_close_finish (assembly->image);

	if (assembly_is_dynamic (assembly)) {
		g_free ((char*)assembly->aname.culture);
	} else {
		g_free (assembly);
	}
}

/**
 * mono_assembly_close:
 * \param assembly the assembly to release.
 *
 * This method releases a reference to the \p assembly.  The assembly is
 * only released when all the outstanding references to it are released.
 */
void
mono_assembly_close (MonoAssembly *assembly)
{
	if (mono_assembly_close_except_image_pools (assembly))
		mono_assembly_close_finish (assembly);
}

/**
 * mono_assembly_load_module:
 */
MonoImage*
mono_assembly_load_module (MonoAssembly *assembly, guint32 idx)
{
	ERROR_DECL (error);
	MonoImage *result = mono_assembly_load_module_checked (assembly, idx, error);
	mono_error_assert_ok (error);
	return result;
}

MONO_API MonoImage*
mono_assembly_load_module_checked (MonoAssembly *assembly, uint32_t idx, MonoError *error)
{
	return mono_image_load_file_for_image_checked (assembly->image, idx, error);
}


/**
 * mono_assembly_foreach:
 * \param func function to invoke for each assembly loaded
 * \param user_data data passed to the callback
 *
 * Invokes the provided \p func callback for each assembly loaded into
 * the runtime.   The first parameter passed to the callback  is the
 * \c MonoAssembly*, and the second parameter is the \p user_data.
 *
 * This is done for all assemblies loaded in the runtime, not just
 * those loaded in the current application domain.
 */
void
mono_assembly_foreach (GFunc func, gpointer user_data)
{
	GList *copy;

	/*
	 * We make a copy of the list to avoid calling the callback inside the 
	 * lock, which could lead to deadlocks.
	 */
	mono_assemblies_lock ();
	copy = g_list_copy (loaded_assemblies);
	mono_assemblies_unlock ();

	g_list_foreach (loaded_assemblies, func, user_data);

	g_list_free (copy);
}

/**
 * mono_assemblies_cleanup:
 *
 * Free all resources used by this module.
 */
void
mono_assemblies_cleanup (void)
{
	GSList *l;

	mono_os_mutex_destroy (&assemblies_mutex);
	mono_os_mutex_destroy (&assembly_binding_mutex);

	for (l = loaded_assembly_bindings; l; l = l->next) {
		MonoAssemblyBindingInfo *info = (MonoAssemblyBindingInfo *)l->data;

		mono_assembly_binding_info_free (info);
		g_free (info);
	}
	g_slist_free (loaded_assembly_bindings);

	free_assembly_asmctx_from_path_hooks ();
	free_assembly_load_hooks ();
	free_assembly_search_hooks ();
	free_assembly_preload_hooks ();
}

/*LOCKING takes the assembly_binding lock*/
void
mono_assembly_cleanup_domain_bindings (guint32 domain_id)
{
	GSList **iter;

	mono_assembly_binding_lock ();
	iter = &loaded_assembly_bindings;
	while (*iter) {
		GSList *l = *iter;
		MonoAssemblyBindingInfo *info = (MonoAssemblyBindingInfo *)l->data;

		if (info->domain_id == domain_id) {
			*iter = l->next;
			mono_assembly_binding_info_free (info);
			g_free (info);
			g_slist_free_1 (l);
		} else {
			iter = &l->next;
		}
	}
	mono_assembly_binding_unlock ();
}

/*
 * Holds the assembly of the application, for
 * System.Diagnostics.Process::MainModule
 */
static MonoAssembly *main_assembly=NULL;

/**
 * mono_assembly_set_main:
 */
void
mono_assembly_set_main (MonoAssembly *assembly)
{
	main_assembly = assembly;
}

/**
 * mono_assembly_get_main:
 *
 * Returns: the assembly for the application, the first assembly that is loaded by the VM
 */
MonoAssembly *
mono_assembly_get_main (void)
{
	return (main_assembly);
}

/**
 * mono_assembly_get_image:
 * \param assembly The assembly to retrieve the image from
 *
 * \returns the \c MonoImage associated with this assembly.
 */
MonoImage*
mono_assembly_get_image (MonoAssembly *assembly)
{
	MonoImage *res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_assembly_get_image_internal (assembly);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

MonoImage*
mono_assembly_get_image_internal (MonoAssembly *assembly)
{
	MONO_REQ_GC_UNSAFE_MODE;
	return assembly->image;
}

/**
 * mono_assembly_get_name:
 * \param assembly The assembly to retrieve the name from
 *
 * The returned name's lifetime is the same as \p assembly's.
 *
 * \returns the \c MonoAssemblyName associated with this assembly.
 */
MonoAssemblyName *
mono_assembly_get_name (MonoAssembly *assembly)
{
	MonoAssemblyName *res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_assembly_get_name_internal (assembly);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

MonoAssemblyName *
mono_assembly_get_name_internal (MonoAssembly *assembly)
{
	MONO_REQ_GC_UNSAFE_MODE;
	return &assembly->aname;
}

/**
 * mono_register_bundled_assemblies:
 */
void
mono_register_bundled_assemblies (const MonoBundledAssembly **assemblies)
{
	bundles = assemblies;
}

#define MONO_DECLSEC_FORMAT_10		0x3C
#define MONO_DECLSEC_FORMAT_20		0x2E
#define MONO_DECLSEC_FIELD		0x53
#define MONO_DECLSEC_PROPERTY		0x54

#define SKIP_VISIBILITY_XML_ATTRIBUTE ("\"SkipVerification\"")
#define SKIP_VISIBILITY_ATTRIBUTE_NAME ("System.Security.Permissions.SecurityPermissionAttribute")
#define SKIP_VISIBILITY_ATTRIBUTE_SIZE (sizeof (SKIP_VISIBILITY_ATTRIBUTE_NAME) - 1)
#define SKIP_VISIBILITY_PROPERTY_NAME ("SkipVerification")
#define SKIP_VISIBILITY_PROPERTY_SIZE (sizeof (SKIP_VISIBILITY_PROPERTY_NAME) - 1)

static gboolean
mono_assembly_try_decode_skip_verification_param (const char *p, const char **resp, gboolean *abort_decoding)
{
	int len;
	switch (*p++) {
	case MONO_DECLSEC_PROPERTY:
		break;
	case MONO_DECLSEC_FIELD:
	default:
		*abort_decoding = TRUE;
		return FALSE;
		break;
	}

	if (*p++ != MONO_TYPE_BOOLEAN) {
		*abort_decoding = TRUE;
		return FALSE;
	}
		
	/* property name length */
	len = mono_metadata_decode_value (p, &p);

	if (len >= SKIP_VISIBILITY_PROPERTY_SIZE && !memcmp (p, SKIP_VISIBILITY_PROPERTY_NAME, SKIP_VISIBILITY_PROPERTY_SIZE)) {
		p += len;
		return *p;
	}
	p += len + 1;

	*resp = p;
	return FALSE;
}

static gboolean
mono_assembly_try_decode_skip_verification (const char *p, const char *endn)
{
	int i, j, num, len, params_len;

	if (*p == MONO_DECLSEC_FORMAT_10) {
		gsize read, written;
		char *res = g_convert (p, endn - p, "UTF-8", "UTF-16LE", &read, &written, NULL);
		if (res) {
			gboolean found = strstr (res, SKIP_VISIBILITY_XML_ATTRIBUTE) != NULL;
			g_free (res);
			return found;
		}
		return FALSE;
	}
	if (*p++ != MONO_DECLSEC_FORMAT_20)
		return FALSE;

	/* number of encoded permission attributes */
	num = mono_metadata_decode_value (p, &p);
	for (i = 0; i < num; ++i) {
		gboolean is_valid = FALSE;
		gboolean abort_decoding = FALSE;

		/* attribute name length */
		len =  mono_metadata_decode_value (p, &p);

		/* We don't really need to fully decode the type. Comparing the name is enough */
		is_valid = len >= SKIP_VISIBILITY_ATTRIBUTE_SIZE && !memcmp (p, SKIP_VISIBILITY_ATTRIBUTE_NAME, SKIP_VISIBILITY_ATTRIBUTE_SIZE);

		p += len;

		/*size of the params table*/
		params_len =  mono_metadata_decode_value (p, &p);
		if (is_valid) {
			const char *params_end = p + params_len;
			
			/* number of parameters */
			len = mono_metadata_decode_value (p, &p);
	
			for (j = 0; j < len; ++j) {
				if (mono_assembly_try_decode_skip_verification_param (p, &p, &abort_decoding))
					return TRUE;
				if (abort_decoding)
					break;
			}
			p = params_end;
		} else {
			p += params_len;
		}
	}
	
	return FALSE;
}


gboolean
mono_assembly_has_skip_verification (MonoAssembly *assembly)
{
	MonoTableInfo *t;	
	guint32 cols [MONO_DECL_SECURITY_SIZE];
	const char *blob;
	int i, len;

	if (MONO_SECMAN_FLAG_INIT (assembly->skipverification))
		return MONO_SECMAN_FLAG_GET_VALUE (assembly->skipverification);

	t = &assembly->image->tables [MONO_TABLE_DECLSECURITY];

	for (i = 0; i < t->rows; ++i) {
		mono_metadata_decode_row (t, i, cols, MONO_DECL_SECURITY_SIZE);
		if ((cols [MONO_DECL_SECURITY_PARENT] & MONO_HAS_DECL_SECURITY_MASK) != MONO_HAS_DECL_SECURITY_ASSEMBLY)
			continue;
		if (cols [MONO_DECL_SECURITY_ACTION] != SECURITY_ACTION_REQMIN)
			continue;

		blob = mono_metadata_blob_heap (assembly->image, cols [MONO_DECL_SECURITY_PERMISSIONSET]);
		len = mono_metadata_decode_blob_size (blob, &blob);
		if (!len)
			continue;

		if (mono_assembly_try_decode_skip_verification (blob, blob + len)) {
			MONO_SECMAN_FLAG_SET_VALUE (assembly->skipverification, TRUE);
			return TRUE;
		}
	}

	MONO_SECMAN_FLAG_SET_VALUE (assembly->skipverification, FALSE);
	return FALSE;
}

MonoAssemblyContextKind
mono_asmctx_get_kind (const MonoAssemblyContext *ctx)
{
	return ctx->kind;
}

static const char *
mono_asmctx_get_name (const MonoAssemblyContext *asmctx)
{
	static const char* names [] = {
		"DEFAULT",
		"REFONLY",
		"LOADFROM",
		"INDIVIDIUAL",
	};
	g_assert (asmctx->kind >= 0 && asmctx->kind <= MONO_ASMCTX_LAST);
	return names [asmctx->kind];
}
