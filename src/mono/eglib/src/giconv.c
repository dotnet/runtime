/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 *  Copyright (C) 2011 Jeffrey Stedfast
 *
 *  Permission is hereby granted, free of charge, to any person
 *  obtaining a copy of this software and associated documentation
 *  files (the "Software"), to deal in the Software without
 *  restriction, including without limitation the rights to use, copy,
 *  modify, merge, publish, distribute, sublicense, and/or sell copies
 *  of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be
 *  included in all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 *  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 *  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 *  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 *  HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 *  WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <glib.h>
#include <stdio.h>
#include <string.h>
#include <locale.h>
#include <iconv.h>
#include <errno.h>

#ifdef HAVE_CODESET
#include <langinfo.h>
#endif

#define ICONV_ISO_INT_FORMAT "iso-%u-%u"
#define ICONV_ISO_STR_FORMAT "iso-%u-%s"
#define ICONV_10646 "iso-10646"

#define ICONV_CACHE_MAX_SIZE   (16)

typedef struct _ListNode {
	struct _ListNode *next;
	struct _ListNode *prev;
} ListNode;

typedef struct {
	ListNode *head;
	ListNode *tail;
	ListNode *tailpred;
} List;

typedef struct {
	GHashTable *hash;
	size_t size;
	List list;
} GIConvCache;

typedef struct {
	ListNode node;
	GIConvCache *cache;
	guint32 refcount : 31;
	guint32 used : 1;
	iconv_t cd;
	char *key;
} GIConvCacheNode;


static GIConvCache *iconv_cache = NULL;
static GHashTable *iconv_open_hash = NULL;
static GHashTable *iconv_charsets = NULL;
static char *locale_charset = NULL;

#ifdef G_THREADS_ENABLED
static pthread_mutex_t iconv_cache_lock = PTHREAD_MUTEX_INITIALIZER;
#define ICONV_CACHE_LOCK()   pthread_mutex_lock (&iconv_cache_lock)
#define ICONV_CACHE_UNLOCK() pthread_mutex_unlock (&iconv_cache_lock)
#else
#define ICONV_CACHE_LOCK()
#define ICONV_CACHE_UNLOCK()
#endif /* G_THREADS_ENABLED */


/* a useful website on charset alaises:
 * http://www.li18nux.org/subgroups/sa/locnameguide/v1.1draft/CodesetAliasTable-V11.html */

static struct {
	const char *charset;     /* Note: expected to be lowercase */
	const char *iconv_name;  /* Note: expected to be properly cased for iconv_open() */
} known_iconv_charsets[] = {
	/* charset name, iconv-friendly name (sometimes case sensitive) */
	{ "utf-8",           "UTF-8"      },
	{ "utf8",            "UTF-8"      },
	
	/* ANSI_X3.4-1968 is used on some systems and should be
	   treated the same as US-ASCII */
	{ "ansi_x3.4-1968",  NULL         },
	
	/* 10646 is a special case, its usually UCS-2 big endian */
	/* This might need some checking but should be ok for
           solaris/linux */
	{ "iso-10646-1",     "UCS-2BE"    },
	{ "iso_10646-1",     "UCS-2BE"    },
	{ "iso10646-1",      "UCS-2BE"    },
	{ "iso-10646",       "UCS-2BE"    },
	{ "iso_10646",       "UCS-2BE"    },
	{ "iso10646",        "UCS-2BE"    },
	
	/* Korean charsets */
	/* Note: according to http://www.iana.org/assignments/character-sets,
	 * ks_c_5601-1987 should really map to ISO-2022-KR, but the EUC-KR
	 * mapping was given to me via a native Korean user, so I'm not sure
	 * if I should change this... perhaps they are compatable? */
	{ "ks_c_5601-1987",  "EUC-KR"     },
	{ "5601",            "EUC-KR"     },
	{ "ksc-5601",        "EUC-KR"     },
	{ "ksc-5601-1987",   "EUC-KR"     },
	{ "ksc-5601_1987",   "EUC-KR"     },
	{ "ks_c_5861-1992",  "EUC-KR"     },
	{ "euckr-0",         "EUC-KR"     },
	
	/* Chinese charsets */
	{ "big5-0",          "BIG5"       },
	{ "big5.eten-0",     "BIG5"       },
	{ "big5hkscs-0",     "BIG5HKSCS"  },
	/* Note: GBK is a superset of gb2312 (see
	 * http://en.wikipedia.org/wiki/GBK for details), so 'upgrade'
	 * gb2312 to GBK so that we can completely convert GBK text
	 * that is incorrectly tagged as gb2312 to UTF-8. */
	{ "gb2312",          "GBK"        },
	{ "gb-2312",         "GBK"        },
	{ "gb2312-0",        "GBK"        },
	{ "gb2312-80",       "GBK"        },
	{ "gb2312.1980-0",   "GBK"        },
	/* euc-cn is an alias for gb2312 */
	{ "euc-cn",          "GBK"        },
	{ "gb18030-0",       "gb18030"    },
	{ "gbk-0",           "GBK"        },
	
	/* Japanese charsets */
	{ "eucjp-0",         "eucJP"  	  },  /* should this map to "EUC-JP" instead? */
	{ "ujis-0",          "ujis"  	  },  /* we might want to map this to EUC-JP */
	{ "jisx0208.1983-0", "SJIS"       },
	{ "jisx0212.1990-0", "SJIS"       },
	{ "pck",	     "SJIS"       },
	{ NULL,              NULL         }
};


static void
list_init (List *list)
{
	list->head = (ListNode *) &list->tail;
	list->tail = NULL;
	list->tailpred = (ListNode *) &list->head;
}

static ListNode *
list_prepend (List *list, ListNode *node)
{
	node->next = list->head;
	node->prev = (ListNode *) &list->head;
	list->head->prev = node;
	list->head = node;
	
	return node;
}

static ListNode *
list_unlink (ListNode *node)
{
	node->next->prev = node->prev;
        node->prev->next = node->next;
	
	return node;
}


static GIConvCacheNode *
g_iconv_cache_node_new (const char *key, iconv_t cd)
{
	GIConvCacheNode *node;
	
	node = g_malloc (sizeof (GIConvCacheNode));
	node->key = g_strdup (key);
	node->refcount = 1;
	node->used = TRUE;
	node->cd = cd;
	
	return node;
}

static void
g_iconv_cache_node_free (GIConvCacheNode *node)
{
	iconv_close (node->cd);
	g_free (node->key);
}

static GIConvCache *
g_iconv_cache_new (void)
{
	GIConvCache *cache;
	
	cache = g_malloc (sizeof (GIConvCache));
	cache->hash = g_hash_table_new_full (g_str_hash, g_str_equal, NULL, (GDestroyNotify) g_iconv_cache_node_free);
	list_init (&cache->list);
	
	return cache;
}

static void
g_iconv_cache_free (GIConvCache *cache)
{
	g_hash_table_destroy (cache->hash);
	g_free (cache);
}

static void
g_iconv_cache_expire_unused (GIConvCache *cache)
{
	ListNode *node, *prev;
	GIConvCacheNode *inode;
	
	node = cache->list.tailpred;
	while (node->prev && cache->size > ICONV_CACHE_MAX_SIZE) {
		inode = (GIConvCacheNode *) node;
		prev = node->prev;
		if (inode->refcount == 0) {
			list_unlink (node);
			g_hash_table_remove (cache->hash, inode->key);
			cache->size--;
		}
		node = prev;
	}
}

static GIConvCacheNode *
g_iconv_cache_insert (GIConvCache *cache, const char *key, iconv_t cd)
{
	GIConvCacheNode *node;
	
	cache->size++;
	
	if (cache->size > ICONV_CACHE_MAX_SIZE)
		g_iconv_cache_expire_unused (cache);
	
	node = g_iconv_cache_node_new (key, cd);
	node->cache = cache;
	
	g_hash_table_insert (cache->hash, node->key, node);
	list_prepend (&cache->list, (ListNode *) node);
	
	return node;
}

static GIConvCacheNode *
g_iconv_cache_lookup (GIConvCache *cache, const char *key, gboolean use)
{
	GIConvCacheNode *node;
	
	node = g_hash_table_lookup (cache->hash, key);
	if (node && use) {
		list_unlink ((ListNode *) node);
		list_prepend (&cache->list, (ListNode *) node);
	}
	
	return node;
}

static const char *
strdown (char *str)
{
	register char *s = str;
	
	while (*s) {
		if (*s >= 'A' && *s <= 'Z')
			*s += 0x20;
		s++;
	}
	
	return str;
}

const char *
charset_to_iconv_name (const char *charset)
{
	char *name, *iconv_name, *buf;
	
	if (charset == NULL)
		return NULL;
	
	name = g_alloca (strlen (charset) + 1);
	strcpy (name, charset);
	strdown (name);
	
	if ((iconv_name = g_hash_table_lookup (iconv_charsets, name)))
		return iconv_name;
	
	if (!strncmp (name, "iso", 3)) {
		int iso, codepage;
		char *p;
		
		buf = name + 3;
		if (*buf == '-' || *buf == '_')
			buf++;
		
		iso = strtoul (buf, &p, 10);
		
		if (iso == 10646) {
			/* they all become ICONV_10646 */
			iconv_name = g_strdup (ICONV_10646);
		} else if (p > buf) {
			buf = p;
			if (*buf == '-' || *buf == '_')
				buf++;
			
			codepage = strtoul (buf, &p, 10);
			
			if (p > buf) {
				/* codepage is numeric */
#ifdef __aix__
				if (codepage == 13)
					iconv_name = g_strdup ("IBM-921");
				else
#endif /* __aix__ */
					iconv_name = g_strdup_printf (ICONV_ISO_INT_FORMAT,
								      iso, codepage);
			} else {
				/* codepage is a string - probably iso-2022-jp or something */
				iconv_name = g_strdup_printf (ICONV_ISO_STR_FORMAT,
							      iso, p);
			}
		} else {
			/* p == buf, which probably means we've
			   encountered an invalid iso charset name */
			iconv_name = g_strdup (name);
		}
	} else if (!strncmp (name, "windows-", 8)) {
		buf = name + 8;
		if (!strncmp (buf, "cp", 2))
			buf += 2;
		
		iconv_name = g_strdup_printf ("CP%s", buf);
	} else if (!strncmp (name, "microsoft-", 10)) {
		buf = name + 10;
		if (!strncmp (buf, "cp", 2))
			buf += 2;
		
		iconv_name = g_strdup_printf ("CP%s", buf);
	} else {
		/* assume charset name is ok as is? */
		iconv_name = g_strdup (charset);
	}
	
	g_hash_table_insert (iconv_charsets, g_strdup (name), iconv_name);
	
	return iconv_name;
}


static void
iconv_open_node_free (gpointer key, gpointer value, gpointer user_data)
{
	iconv_t cd = (iconv_t) key;
	GIConvCacheNode *node;
	
	node = (GIConvCacheNode *) g_iconv_cache_lookup (iconv_cache, value, FALSE);
	g_assert (node);
	
	if (cd != node->cd) {
		node->refcount--;
		iconv_close (cd);
	}
}

static void
g_iconv_shutdown (void)
{
	if (!iconv_cache)
		return;
	
	g_hash_table_foreach (iconv_open_hash, iconv_open_node_free, NULL);
	g_hash_table_destroy (iconv_open_hash);
	iconv_open_hash = NULL;
	
	g_iconv_cache_free (iconv_cache);
	iconv_cache = NULL;
	
	g_hash_table_destroy (iconv_charsets);
	iconv_charsets = NULL;
}

static void
g_iconv_init (void)
{
	char *charset, *iconv_name;
	int i;
	
	if (iconv_cache)
		return;
	
	iconv_charsets = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, g_free);
	iconv_open_hash = g_hash_table_new (g_direct_hash, g_direct_equal);
	iconv_cache = g_iconv_cache_new ();
	
	for (i = 0; known_iconv_charsets[i].charset != NULL; i++) {
		iconv_name = g_strdup (known_iconv_charsets[i].iconv_name);
		charset = g_strdup (known_iconv_charsets[i].charset);
		
		g_hash_table_insert (iconv_charsets, charset, iconv_name);
	}
	
	if (!((locale_charset = getenv ("CHARSET")) && *locale_charset)) {
#ifdef HAVE_CODESET
		if ((locale_charset = nl_langinfo (CODESET)) && locale_charset[0])
			locale_charset = g_ascii_strdown (locale_charset, -1);
		else
			locale_charset = NULL;
#endif
		
		if (!locale_charset) {
			char *locale = setlocale (LC_ALL, NULL);
			
			if (!locale || !strcmp (locale, "C") || !strcmp (locale, "POSIX")) {
				/* The locale "C"  or  "POSIX"  is  a  portable  locale;  its
				 * LC_CTYPE  part  corresponds  to  the 7-bit ASCII character
				 * set.  */
			} else {
				/* A locale name is typically of  the  form  language[_terri-
				 * tory][.codeset][@modifier],  where  language is an ISO 639
				 * language code, territory is an ISO 3166 country code,  and
				 * codeset  is  a  character  set or encoding identifier like
				 * ISO-8859-1 or UTF-8.
				 */
				char *codeset, *p;
				
				if (!locale_charset) {
					codeset = strchr (locale, '.');
					if (codeset) {
						codeset++;
						
						/* ; is a hack for debian systems and / is a hack for Solaris systems */
						p = codeset;
						while (*p && !strchr ("@;/", *p))
							p++;
						
						locale_charset = g_ascii_strdown (codeset, (size_t)(p - codeset));
					} else {
						/* charset unknown */
						locale_charset = NULL;
					}
				}
			}
		}
	}
}

gsize
g_iconv (GIConv converter, gchar **inbuf, gsize *inleft, gchar **outbuf, gsize *outleft)
{
	return iconv ((iconv_t) converter, inbuf, inleft, outbuf, outleft);
}

GIConv
g_iconv_open (const gchar *to, const gchar *from)
{
	GIConvCacheNode *node;
	iconv_t cd;
	char *key;
	
	if (from == NULL || to == NULL) {
		errno = EINVAL;
		return (GIConv) -1;
	}
	
	ICONV_CACHE_LOCK ();
	
	g_iconv_init ();
	
	if (!g_ascii_strcasecmp (from, "x-unknown"))
		from = locale_charset;
	
	from = charset_to_iconv_name (from);
	to = charset_to_iconv_name (to);
	key = g_alloca (strlen (from) + strlen (to) + 2);
	sprintf (key, "%s:%s", from, to);
	
	if ((node = g_iconv_cache_lookup (iconv_cache, key, TRUE))) {
		if (node->used) {
			if ((cd = iconv_open (to, from)) == (iconv_t) -1)
				goto exception;
		} else {
			/* Apparently iconv on Solaris <= 7 segfaults if you pass in
			 * NULL for anything but inbuf; work around that. (NULL outbuf
			 * or NULL *outbuf is allowed by Unix98.)
			 */
			size_t inleft = 0, outleft = 0;
			char *outbuf = NULL;
			
			cd = node->cd;
			node->used = TRUE;
			
			/* reset the descriptor */
			iconv (cd, NULL, &inleft, &outbuf, &outleft);
		}
		
		node->refcount++;
	} else {
		if ((cd = iconv_open (to, from)) == (iconv_t) -1)
			goto exception;
		
		node = g_iconv_cache_insert (iconv_cache, key, cd);
	}
	
	g_hash_table_insert (iconv_open_hash, cd, node->key);
	
	ICONV_CACHE_UNLOCK ();
	
	return (GIConv) cd;
	
 exception:
	
	ICONV_CACHE_UNLOCK ();
	
	return (GIConv) -1;
}

int
g_iconv_close (GIConv converter)
{
	GIConvCacheNode *node;
	const char *key;
	iconv_t cd;
	
	if (converter == (GIConv) -1)
		return 0;
	
	cd = (iconv_t) converter;
	
	ICONV_CACHE_LOCK ();
	
	g_iconv_init ();
	
	if ((key = g_hash_table_lookup (iconv_open_hash, cd))) {
		g_hash_table_remove (iconv_open_hash, cd);
		
		node = (GIConvCacheNode *) g_iconv_cache_lookup (iconv_cache, key, FALSE);
		g_assert (node);
		
		if (iconv_cache->size > ICONV_CACHE_MAX_SIZE) {
			/* expire before unreffing this node so that it wont get uncached */
			g_iconv_cache_expire_unused (iconv_cache);
		}
		
		node->refcount--;
		
		if (cd == node->cd)
			node->used = FALSE;
		else
			iconv_close (cd);
	} else {
		ICONV_CACHE_UNLOCK ();
		
		/* really this is an error... someone is trying to close an
		 * iconv_t descriptor that wasn't opened by us. */
		
		return iconv_close (cd);
	}
	
	ICONV_CACHE_UNLOCK ();
	
	return 0;
}
