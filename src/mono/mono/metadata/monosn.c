/*
 * monosn.c: Mono String Name Utility
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 *
 */
#include <mono/metadata/class.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include "mono/utils/mono-digest.h"
/* trim headers */

#include <string.h>
#include <ctype.h>

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

		mono_metadata_init ();
        mono_images_init ();
        mono_assemblies_init ();
        mono_loader_init ();

		image = mono_image_open (file, NULL);
		if (!image) {
			printf ("Cannot open image file: %s\n", file);
			return 2;
		}
		pubkey = mono_image_get_public_key (image, &len);
		if (!pubkey) {
			printf ("%s does not represent a strongly named assembly\n", mono_image_get_name(image));
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
		printf ("%s does not represent a strongly named assembly\n", mono_image_get_name(image));
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

const static guint8 asciitable [128] = {
	0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
	0xff, 0x3e, 0xff, 0xff, 0xff, 0x3f,
	0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
	0x3a, 0x3b, 0x3c, 0x3d, 0xff, 0xff,
	0xff, 0xff, 0xff, 0xff, 0xff, 0x00,
	0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
	0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c,
	0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12,
	0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
	0x19, 0xff, 0xff, 0xff, 0xff, 0xff,
	0xff, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
	0x1f, 0x20, 0x21, 0x22, 0x23, 0x24,
	0x25, 0x26, 0x27, 0x28, 0x29, 0x2a,
	0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30,
	0x31, 0x32, 0x33, 0xff, 0xff, 0xff,
	0xff, 0xff
};

/* data is changed in place */
static char*
pem_decode (guchar *data, int len, int *rlen) {
	guchar *p, *s;
	int b64len, i, rem = 0, full;
	int b0, b1, b2, b3, offset, dlen;

	p = strstr (data, "-----BEGIN");
	s = strstr (data, "\n-----END");
	if (!p || !s)
		return NULL;
	while (*p != '\n') p++;
	*s = 0;
	s = data = p;
	while (*p) {
		if (isalnum (*p) || *p == '+' || *p == '=' || *p == '/') {
			*s++ = *p++;
		} else {
			p++;
		}
	}
	*s = 0;
	b64len = s - data;

	full = b64len >> 2;
	if (data [b64len - 1] == '=') {
		full--;
		rem++;
	}
	if (data [b64len - 2] == '=')
		rem++;
	offset = 0;
	p = data;
	for (i = 0; i < full; ++i) {
		b0 = asciitable [data [offset++]];
		b1 = asciitable [data [offset++]];
		b2 = asciitable [data [offset++]];
		b3 = asciitable [data [offset++]];

		*p++ = (b0 << 2) | (b1 >> 4);
		*p++ = (b1 << 4) | (b2 >> 2);
		*p++ = (b2 << 6) | b3;
	}
	dlen = full * 3;
	switch (rem) {
	case 1:
		b0 = asciitable [data [offset++]];
		b1 = asciitable [data [offset++]];
		b2 = asciitable [data [offset++]];

		*p++ = (b0 << 2) | (b1 >> 4);
		*p++ = (b1 << 4) | (b2 >> 2);
		dlen += 2;
		break;
	case 2:
		b0 = asciitable [data [offset++]];
		b1 = asciitable [data [offset++]];

		*p++ = (b0 << 2) | (b1 >> 4);
		dlen++;
		break;
	}
	*rlen = dlen;
	return data;
}

enum {
	DER_INTEGER = 2,
	DER_BITSTRING = 3,
	DER_NULL = 5,
	DER_OBJID = 6,
	DER_SEQUENCE = 16,
	DER_INVALID = -1,
	DER_END = -2
};

static int
der_get_next (guchar *data, int dlen, int offset, int *len, guchar **rdata)
{
	int i, l, type, val;

	if (offset + 1 >= dlen)
		return DER_END;

	type = data [offset++] & 0x1f;
	if (data [offset] == 0x80) /* not supported */
		return DER_INVALID;
	l = 0;
	if (data [offset] & 0x80) {
		val = data [offset++] & 0x7f;
		for (i = 0; i < val; ++i) {
			l = (l << 8) | data [offset++];
		}
	} else {
		l = data [offset++];
	}
	*len = l;
	*rdata = data + offset;
	return type;
}

static void
dump_asn1 (guchar *key, int len) {
	int type, offset, elen;
	guchar *edata;

	offset = 0;
	while ((type = der_get_next (key, len, offset, &elen, &edata)) >= 0) {
		switch (type) {
		case DER_SEQUENCE:
			g_print ("seq (%d) at %d\n", elen, offset);
			dump_asn1 (edata, elen);
			offset = elen + edata - key;
			break;
		case DER_BITSTRING:
			g_print ("bits (%d) at %p + %d\n", elen, edata, offset);
			dump_asn1 (edata + 1, elen);
			offset = 1 + elen + edata - key;
			break;
		case DER_INTEGER:
			g_print ("int (%d) at %d\n", elen, offset);
			offset = elen + edata - key;
			break;
		case DER_NULL:
			g_print ("null (%d) at %d\n", elen, offset);
			offset = elen + edata - key;
			break;
		case DER_OBJID:
			g_print ("objid (%d) at %d\n", elen, offset);
			offset = elen + edata - key;
			break;
		default:
			return;
		}
	}
}

static guint32
get_der_int (guchar *data, int len)
{
	guint32 val = 0;
	int i;
	for (i = 0; i < len; ++i)
		val = (val << 8) | data [i];
	return val;
}

static void
mem_reverse (guchar *p, int len) {
	int i, t;

	for (i = 0; i < len/2; ++i) {
		t = p [i];
		p [i] = p [len - i - 1];
		p [len - i - 1] = t;
	}
}

static int
convert_der_key (guchar *key, int len, guchar **ret, int *retlen)
{
	int type, offset, val, elen;
	guchar *r, *edata;

	offset = 0;
	type = der_get_next (key, len, offset, &elen, &edata);
	if (type != DER_SEQUENCE)
		return 1;
	key = edata;
	len = elen;
	type = der_get_next (key, len, offset, &elen, &edata);
	if (type == DER_INTEGER) {
		int i;
		guchar *ints [6];
		int lengths [6];
		guchar *p;
		/* a private RSA key */
		val = get_der_int (edata, elen);
		if (val != 0)
			return 2;
		offset = elen + edata - key;
		/* the modulus */
		type = der_get_next (key, len, offset, &elen, &edata);
		if (type != DER_INTEGER)
			return 2;
		offset = elen + edata - key;
		if ((elen & 1) && *edata == 0) {
			edata ++;
			elen--;
		}
		r = g_new0 (guchar, elen*4 + elen/2 + 20);
		r [0] = 0x7; r [1] = 0x2; r [5] = 0x24;
		r [8] = 0x52; r [9] = 0x53; r [10] = 0x41; r [11] = 0x32;
		*(guint32*)(r + 12) = elen * 8;
		memcpy (r + 20, edata, elen);
		mem_reverse (r + 20, elen);
		p = r + 20 + elen;
		/* the exponent */
		type = der_get_next (key, len, offset, &elen, &edata);
		if (type != DER_INTEGER)
			return 2;
		offset = elen + edata - key;
		val = get_der_int (edata, elen);
		*(guint32*)(r + 16) = val;
		for (i = 0; i < 6; i++) {
			type = der_get_next (key, len, offset, &elen, &edata);
			if (type != DER_INTEGER)
				return 2;
			offset = elen + edata - key;
			if ((elen & 1) && *edata == 0) {
				edata++;
				elen--;
			}
			ints [i] = edata;
			lengths [i] = elen;
			g_print ("len: %d\n", elen);
		}
		/* prime1 */
		g_print ("prime1 at %d (%d)\n", p-r, lengths [1]);
		memcpy (p, ints [1], lengths [1]);
		mem_reverse (p, lengths [1]);
		p += lengths [1];
		/* prime2 */
		g_print ("prime2 at %d (%d)\n", p-r, lengths [2]);
		memcpy (p, ints [2], lengths [2]);
		mem_reverse (p, lengths [2]);
		p += lengths [2];
		/* exponent1 */
		g_print ("exp1 at %d (%d)\n", p-r, lengths [3]);
		memcpy (p, ints [3], lengths [3]);
		mem_reverse (p, lengths [3]);
		p += lengths [3];
		/* exponent2 */
		g_print ("exp2 at %d (%d)\n", p-r, lengths [4]);
		memcpy (p, ints [4], lengths [4]);
		mem_reverse (p, lengths [4]);
		p += lengths [4];
		/* coeff */
		g_print ("coeff at %d (%d)\n", p-r, lengths [5]);
		memcpy (p, ints [5], lengths [5]);
		mem_reverse (p, lengths [5]);
		p += lengths [5];
		/* private exponent */
		g_print ("prive at %d (%d)\n", p-r, lengths [0]);
		memcpy (p, ints [0], lengths [0]);
		mem_reverse (p, lengths [0]);
		p += lengths [0];
		*ret = r;
		*retlen = p-r;
		return 0;
	}
	return 1;
}

static int
convert_format (const char *from, const char *outfile) {
	guchar *key, *bindata, *keyout;
	gsize len;
	int binlen, ret, lenout;
	FILE *file;
	
	if (!g_file_get_contents (from, (gchar**) &key, &len, NULL)) {
		printf ("Cannot load file: %s\n", from);
		return 2;
	}

	if (*key == 0 || *key == 0x24) {
		g_free (key);
		printf ("Cannot convert to pem format yet\n");
		return 2;
	}
	bindata = pem_decode (key, len, &binlen);
	if (!(file = fopen (outfile, "wb"))) {
		g_free (key);
		printf ("Cannot open output file: %s\n", outfile);
		return 2;
	}
	dump_asn1 (bindata, binlen);
	ret = convert_der_key (bindata, binlen, &keyout, &lenout);
	if (!ret) {
		fwrite (keyout, lenout, 1, file);
		g_free (keyout);
	} else {
		printf ("Cannot convert key\n");
	}
	fclose (file);
	g_free (key);
	return ret;
}

static int
get_digest (const char *from, const char *outfile)
{
	guchar *ass;
	guchar digest [20];
	gsize len;
	guint32 snpos, snsize;
	FILE *file;
	MonoImage *image;
	MonoSHA1Context sha1;
	
	image = mono_image_open (from, NULL);
	if (!image) {
		printf ("Cannot open image file: %s\n", from);
		return 2;
	}
	snpos = mono_image_strong_name_position (image, &snsize);
	if (!snpos) {
		/*printf ("%s does not represent a strongly named assembly\n", from);
		mono_image_close (image);
		return 2;*/
		snsize = 0;
	}
	
	if (!g_file_get_contents (from, (gchar**) &ass, &len, NULL)) {
		printf ("Cannot load file: %s\n", from);
		mono_image_close (image);
		return 2;
	}
	/* 
	 * FIXME: we may need to set the STRONGNAMESIGNED flag in the cli header 
	 * before taking the sha1 digest of the image.
	 */
	mono_sha1_init (&sha1);
	mono_sha1_update (&sha1, ass, snpos);
	mono_sha1_update (&sha1, ass + snpos + snsize, len - snsize - snpos);
	mono_sha1_final (&sha1, digest);

	mono_image_close (image);
	g_free (ass);
	if (!(file = fopen (outfile, "wb"))) {
		printf ("Cannot open output file: %s\n", outfile);
		return 2;
	}
	fwrite (digest, 20, 1, file);
	fclose (file);
	return 0;
}

static void 
help (int err) {
	printf ("monosn: Mono Strong Name Utility\nUsage: monosn option [arguments]\n");
	printf ("Available options:\n");
	printf ("\t-C keyin keyout   Convert key file format from PEM to cryptoAPI (or the reverse).\n");
	printf ("\t-e assembly file  Extract the public key from assembly to file.\n");
	printf ("\t-E assembly file  Extract the strong name from assembly to file.\n");
	printf ("\t-r assembly file  Extract the sha1 digest from assembly to file.\n");
	printf ("\t-t[p] file        Display the public key token from file.\n");
	printf ("\t-T[p] assembly    Display the public key token from assembly.\n");
	exit (err);
}

int 
main (int argc, char *argv[]) {
	int opt;
	
	if (argc < 2 || argv [1] [0] != '-')
		help (1);

	opt = argv [1] [1];
	switch (opt) {
	case 'C':
		if (argc != 4)
			help (1);
		return convert_format (argv [2], argv [3]);
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
	case 'r':
		if (argc != 4)
			help (1);
		return get_digest (argv [2], argv [3]);
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

