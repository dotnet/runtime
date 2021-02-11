/*
 * gmodule.c: dl* functions, glib style
 *
 * Author:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com)
 *   Jonathan Chambers (joncham@gmail.com)
 *   Robert Jordan (robertj@gmx.net)
 *   Calvin Buckley (calvin@cmpct.info)
 *
 * (C) 2006 Novell, Inc.
 * (C) 2006 Jonathan Chambers
 * (C) 2019 Calvin Buckley
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

// FIXME This does not work because it guards config.h include.
/* In case by some miracle, IBM implements this */
#if defined(_AIX) && !defined(HAVE_DLADDR)
#include <config.h>

#include <glib.h>
#include <gmodule.h>

#include <stdio.h>
#include <string.h>
#include <dlfcn.h>
#include <sys/errno.h>
#include <stdlib.h>

/* AIX specific headers for loadquery and traceback structure */
#include <sys/ldr.h>
#include <sys/debug.h>

/* library filename + ( + member file name + ) + NUL */
#define AIX_PRINTED_LIB_LEN ((PATH_MAX * 2) + 3)

/*
 * The structure that holds information for dladdr. Unfortunately, on AIX,
 * the information returned by loadquery lives in an allocated buffer, so it
 * should be freed when no longer needed. Note that sname /is/ still constant
 * (it points to the traceback info in the image), so don't free it.
 */
typedef struct _g_dl_info {
	char* dli_fname;
	void* dli_fbase;
	const char* dli_sname;
	void* dli_saddr;
} _g_Dl_info;

/**
 * Gets the base address and name of a symbol.
 *
 * This uses the traceback table at the function epilogue to get the base
 * address and the name of a symbol. As such, this means that the input must
 * be a word-aligned address within the text section.
 *
 * The way to support non-text (data/bss/whatever) would be to use an XCOFF
 * parser on the image loaded in memory and snarf its symbol table. However,
 * that is much more complex, and presumably, most addresses passed would be
 * code in the text section anyways (I hope so, anyways...) Unfortunately,
 * this does mean that function descriptors, which live in data, won't work.
 * The traceback approach actually works with JITted code too, provided it
 * could be emitted with XCOFF traceback...
 */
static void
_g_sym_from_tb(void **sbase, const char **sname, void *where) {
	unsigned int *s = (unsigned int*)where;
	while (*s) {
		/* look for zero word (invalid op) that begins epilogue */
		s++;
	}
	/* We're on a zero word now, seek after the traceback table. */
	struct tbtable_short *tb = (struct tbtable_short*)(s + 1);
	/* The extended traceback is variable length, so more seeking. */
	char *ext = (char*)(tb + 1);
	/* Skip a lot of cruft, in order according to the ext "structure". */
	if (tb->fixedparms || tb->floatparms) {
		ext += sizeof(unsigned int);
	}
	if (tb->has_tboff) {
		/* tb_offset */
		void *start = (char*)s - *((unsigned int*)ext);
		ext += sizeof (unsigned int);
		*sbase = (void*)start;
	} else {
		/*
		 * Can we go backwards instead until we hit a null word,
		 * that /precedes/ the block of code?
		 * Does the XCOFF/traceback format allow for that?
		 */
		*sbase = NULL; /* NULL base address as a sentinel */
	}
	if (tb->int_hndl) {
		ext += sizeof(int);
	}
	if (tb->has_ctl) {
		/* array */
		int ctlnum =  (*(int*)ext);
		ext += sizeof(int) + (sizeof(int) * ctlnum);
	}
	if (tb->name_present) {
		/*
		 * The 16-bit name length is here, but the name seems to
		 * include a NUL, so we don't reallocate it, and instead
		 * just point to its location in memory.
		 */
		ext += sizeof(short);
		*sname = ext;
	} else {
		*sname = NULL;
	}
}

/**
 * Look for the base address and name of both a symbol and the corresponding
 * executable in memory. This is a simplistic reimplementation for AIX.
 *
 * Returns 1 on failure and 0 on success. "s" is the address of the symbol,
 * and "i" points to a Dl_info structure to fill. Note that i.dli_fname is
 * not const, and should be freed.
 */
static int
_g_dladdr(void* s, _g_Dl_info* i) {
	/*
	 * Use stack instead of heap because malloc may be messed up.
	 * Init returned structure members to clear out any garbage.
	 */
	char *buf = (char*)g_alloca(10000);
	i->dli_fbase = NULL;
	i->dli_fname = NULL;
	i->dli_saddr = NULL;
	i->dli_sname = NULL;
	int r = loadquery (L_GETINFO, buf, 10000);
	if (r == -1) {
		return 0;
	}
	/* The loader info structures are also a linked list. */
	struct ld_info *cur = (struct ld_info*) buf;
	while (1) {
		/*
		 * Check in text and data sections. Function descriptors are
		 * stored in the data section.
		 */
		char *db = (char*)cur->ldinfo_dataorg;
		char *tb = (char*)cur->ldinfo_textorg;
		char *de = db + cur->ldinfo_datasize;
		char *te = tb + cur->ldinfo_textsize;
		/* Just casting for comparisons. */
		char *cs = (char*)s;

		/*
		 * Find the symbol's name and base address. To make it
		 * easier, we use the traceback in the text section.
		 * See the function's comments above as to why.
		 * (Perhaps we could deref if a descriptor though...)
		 */
		if (cs >= tb && cs <= te) {
			_g_sym_from_tb(&i->dli_saddr, &i->dli_sname, s);
		}

		if ((cs >= db && cs <= de) || (cs >= tb && cs <= te)) {
			/* Look for file name and base address. */
			i->dli_fbase = tb; /* Includes XCOFF header */
			/* library filename + ( + member + ) + NUL */
			char *libname = (char*)g_alloca (AIX_PRINTED_LIB_LEN);
			char *file_part = cur->ldinfo_filename;
			char *member_part = file_part + strlen(file_part) + 1;
			/*
			 * This can't be a const char*, because it exists from
			 * a stack allocated buffer. Also append the member.
			 *
			 * XXX: See if we can't frob usla's memory ranges for
			 * const strings; but is quite difficult.
			 */
			if (member_part[0] == '\0') {
				/* Not an archive, just copy the file name. */
				g_strlcpy(libname, file_part, AIX_PRINTED_LIB_LEN);
			} else {
				/* It's an archive with member. */
				sprintf(libname, "%s(%s)", file_part, member_part);
			}
			i->dli_fname = libname;

			return 1;
		} else if (cur->ldinfo_next == 0) {
			/* Nothing. */
			return 0;
		} else {
			/* Try the next image in memory. */
			cur = (struct ld_info*)((char*)cur + cur->ldinfo_next);
		}
	}
}

gboolean
g_module_address (void *addr, char *file_name, size_t file_name_len,
                  void **file_base, char *sym_name, size_t sym_name_len,
                  void **sym_addr)
{
	_g_Dl_info dli;
	int ret = _g_dladdr(addr, &dli);
	/* This zero-on-failure is unlike other Unix APIs. */
	if (ret == 0)
		return FALSE;
	if (file_name != NULL && file_name_len >= 1) {
		if (dli.dli_fname != NULL)
			g_strlcpy(file_name, dli.dli_fname, file_name_len);
		else
			file_name [0] = '\0';
	}
	if (file_base != NULL)
		*file_base = dli.dli_fbase;
	if (sym_name != NULL && sym_name_len >= 1) {
		if (dli.dli_sname != NULL)
			g_strlcpy(sym_name, dli.dli_sname, sym_name_len);
		else
			sym_name [0] = '\0';
	}
	if (sym_addr != NULL)
		*sym_addr = dli.dli_saddr;
	return TRUE;
}

#else

#define MONO_EMPTY_SOURCE_FILE(x) extern const char mono_quash_linker_empty_file_warning_ ## x; \
				  const char mono_quash_linker_empty_file_warning_ ## x = 0;

MONO_EMPTY_SOURCE_FILE (gmodule_aix);

#endif

