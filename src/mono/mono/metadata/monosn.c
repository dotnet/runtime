/*
 * monosn.c: Mono String Name Utility
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 *
 */
#include <mono/metadata/class.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include "mono/utils/mono-digest.h"
/* trim headers */

#define RSA1_MAGIC     0x32415351
#define RSA2_MAGIC     0x32415352
#define PRIVKEY_MAGIC  0x00000207
#define PUBKEY_MAGIC   0x00008004

typedef struct {
	guchar type, version;
	guint16 reserved1;
	guint32 algid;
} MonoKeyHeader;

typedef struct {
	MonoKeyHeader header;
	guint32 bitlen;
	guint32 exponent;
	guchar  modulus [MONO_ZERO_LEN_ARRAY];
} MonoRSAPubHeader;

static void
print_data (const char *data, int len)
{
	int i;
	for (i = 0; i < len; ++i) {
		if (i && !(i % 32))
			printf ("\n");
		printf ("%02x", data [i] & 0xff);
	}
	printf ("\n");
}

static int
show_token (const char *file, int is_assembly, int show_pubkey) {
	char token [20];
	if (!is_assembly) {
		char *pubkey;
		gsize len;
		if (!g_file_get_contents (file, &pubkey, &len, NULL)) {
			printf ("Cannot load file: %s\n", file);
			return 2;
		}
		mono_digest_get_public_token (token, pubkey, len);
		if (show_pubkey) {
			printf ("Public key is\n");
			print_data (pubkey, len);
		}
		g_free (pubkey);
	} else {
		MonoImage *image;
		const char *pubkey;
		guint32 len;
		image = mono_image_open (file, NULL);
		if (!image) {
			printf ("Cannot open image file: %s\n", file);
			return 2;
		}
		pubkey = mono_image_get_public_key (image, &len);
		if (!pubkey) {
			printf ("%s does not represent a strongly named assembly\n", image->name);
			mono_image_close (image);
			return 2;
		}
		if (show_pubkey) {
			printf ("Public key is\n");
			print_data (pubkey, len);
		}
		mono_digest_get_public_token (token, pubkey, len);
		mono_image_close (image);
	}
	printf ("Public key token is ");
	print_data (token, 8);
	return 0;
}

static int
extract_data_to_file (int pubk, const char *assembly, const char *outfile) {
	MonoImage *image;
	FILE *file;
	const char *pubkey;
	guint32 len;
	
	image = mono_image_open (assembly, NULL);
	if (!image) {
		printf ("Cannot open image file: %s\n", assembly);
		return 2;
	}
	if (pubk)
		pubkey = mono_image_get_public_key (image, &len);
	else
		pubkey = mono_image_get_strong_name (image, &len);
	if (!pubkey) {
		printf ("%s does not represent a strongly named assembly\n", image->name);
		mono_image_close (image);
		return 2;
	}
	if (!(file = fopen (outfile, "wb"))) {
		printf ("Cannot open output file: %s\n", outfile);
		return 2;
	}
	fwrite (pubkey, len, 1, file);
	fclose (file);
	mono_image_close (image);
	return 0;
}

static void 
help (int err) {
	printf ("monosn: Mono Strong Name Utility\nUsage: monosn option [arguments]\n");
	printf ("Available options:\n");
	printf ("\t-e assembly file  Extract the public key from assembly to file.");
	printf ("\t-E assembly file  Extract the strong name from assembly to file.");
	printf ("\t-t[p] file        Display the public key token from file.");
	printf ("\t-T[p] assembly    Display the public key token from assembly.");
	exit (err);
}

int 
main (int argc, char *argv[]) {
	int opt;
	
	if (argc < 2 || argv [1] [0] != '-')
		help (1);

	opt = argv [1] [1];
	switch (opt) {
	case 'e':
		if (argc != 4)
			help (1);
		return extract_data_to_file (1, argv [2], argv [3]);
	case 'E':
		if (argc != 4)
			help (1);
		return extract_data_to_file (0, argv [2], argv [3]);
	case 'h':
	case '?':
		help (0);
		return 0;
	case 't':
		if (argc != 3)
			help (1);
		return show_token (argv [2], 0, argv [1] [2] == 'p');
	case 'T':
		if (argc != 3)
			help (1);
		return show_token (argv [2], 1, argv [1] [2] == 'p');
	default:
		help (1);
	}
	return 0;
}

