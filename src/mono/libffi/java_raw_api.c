/* -----------------------------------------------------------------------
   java_raw_api.c - Copyright (c) 1999  Cygnus Solutions

   Cloned from raw_api.c

   Raw_api.c author: Kresten Krab Thorup <krab@gnu.org>
   Java_raw_api.c author: Hans-J. Boehm <hboehm@hpl.hp.com>

   Permission is hereby granted, free of charge, to any person obtaining
   a copy of this software and associated documentation files (the
   ``Software''), to deal in the Software without restriction, including
   without limitation the rights to use, copy, modify, merge, publish,
   distribute, sublicense, and/or sell copies of the Software, and to
   permit persons to whom the Software is furnished to do so, subject to
   the following conditions:

   The above copyright notice and this permission notice shall be included
   in all copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND, EXPRESS
   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
   IN NO EVENT SHALL RED HAT BE LIABLE FOR ANY CLAIM, DAMAGES OR
   OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
   ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
   OTHER DEALINGS IN THE SOFTWARE.
   ----------------------------------------------------------------------- */

/* This defines a Java- and 64-bit specific variant of the raw API.	*/
/* It assumes that "raw" argument blocks look like Java stacks on a 	*/
/* 64-bit machine.  Arguments that can be stored in a single stack	*/
/* stack slots (longs, doubles) occupy 128 bits, but only the first	*/
/* 64 bits are actually used.  						*/

#include <ffi.h>
#include <ffi_private.h>

#if !defined(NO_JAVA_RAW_API) && !defined(FFI_NO_RAW_API)

size_t
ffi_java_raw_size (ffi_cif *cif)
{
  size_t result = 0;
  int i;

  ffi_type **at = cif->arg_types;

  for (i = cif->nargs-1; i >= 0; i--, at++)
    {
      switch((*at) -> type) {
	case FFI_TYPE_UINT64:
	case FFI_TYPE_SINT64:
	  result += 2 * SIZEOF_ARG;
	  break;
	case FFI_TYPE_STRUCT:
	  /* No structure parameters in Java.	*/
	  abort();
	default:
	  result += SIZEOF_ARG;
      }
    }

  return result;
}


void
ffi_java_raw_to_ptrarray (ffi_cif *cif, ffi_raw *raw, void **args)
{
  unsigned i;
  ffi_type **tp = cif->arg_types;

#if WORDS_BIGENDIAN

  for (i = 0; i < cif->nargs; i++, tp++, args++)
    {	  
      switch ((*tp)->type)
	{
	case FFI_TYPE_UINT8:
	case FFI_TYPE_SINT8:
	  *args = (void*) ((char*)(raw++) + SIZEOF_ARG - 1);
	  break;
	  
	case FFI_TYPE_UINT16:
	case FFI_TYPE_SINT16:
	  *args = (void*) ((char*)(raw++) + SIZEOF_ARG - 2);
	  break;

#if SIZEOF_ARG >= 4	  
	case FFI_TYPE_UINT32:
	case FFI_TYPE_SINT32:
	  *args = (void*) ((char*)(raw++) + SIZEOF_ARG - 4);
	  break;
#endif
	
#if SIZEOF_ARG == 8	  
	case FFI_TYPE_UINT64:
	case FFI_TYPE_SINT64:
	case FFI_TYPE_DOUBLE:
	  *args = (void *)raw;
	  raw += 2;
	  break;
#endif

	case FFI_TYPE_POINTER:
	  *args = (void*) &(raw++)->ptr;
	  break;
	  
	default:
	  *args = raw;
	  raw += ALIGN ((*tp)->size, SIZEOF_ARG) / SIZEOF_ARG;
	}
    }

#else /* WORDS_BIGENDIAN */

#if !PDP

  /* then assume little endian */
  for (i = 0; i < cif->nargs; i++, tp++, args++)
    {
#if SIZEOF_ARG == 8
      switch((*tp)->type) {
	case FFI_TYPE_UINT64:
	case FFI_TYPE_SINT64:
	case FFI_TYPE_DOUBLE:
	  *args = (void*) raw;
	  raw += 2;
	  break;
	default:
	  *args = (void*) raw++;
      }
#else /* SIZEOF_ARG != 8 */
	*args = (void*) raw;
	raw += ALIGN ((*tp)->size, sizeof (void*)) / sizeof (void*);
#endif /* SIZEOF_ARG == 8 */
    }

#else
#error "pdp endian not supported"
#endif /* ! PDP */

#endif /* WORDS_BIGENDIAN */
}

void
ffi_java_ptrarray_to_raw (ffi_cif *cif, void **args, ffi_raw *raw)
{
  unsigned i;
  ffi_type **tp = cif->arg_types;

  for (i = 0; i < cif->nargs; i++, tp++, args++)
    {	  
      switch ((*tp)->type)
	{
	case FFI_TYPE_UINT8:
	  (raw++)->uint = *(UINT8*) (*args);
	  break;

	case FFI_TYPE_SINT8:
	  (raw++)->sint = *(SINT8*) (*args);
	  break;

	case FFI_TYPE_UINT16:
	  (raw++)->uint = *(UINT16*) (*args);
	  break;

	case FFI_TYPE_SINT16:
	  (raw++)->sint = *(SINT16*) (*args);
	  break;

#if SIZEOF_ARG >= 4
	case FFI_TYPE_UINT32:
	  (raw++)->uint = *(UINT32*) (*args);
	  break;

	case FFI_TYPE_SINT32:
	  (raw++)->sint = *(SINT32*) (*args);
	  break;
#endif
        case FFI_TYPE_FLOAT:
	  (raw++)->flt = *(FLOAT32*) (*args);
	  break;

#if SIZEOF_ARG == 8
	case FFI_TYPE_UINT64:
	case FFI_TYPE_SINT64:
	case FFI_TYPE_DOUBLE:
	  raw->uint = *(UINT64*) (*args);
	  raw += 2;
	  break;
#endif

	case FFI_TYPE_POINTER:
	  (raw++)->ptr = **(void***) args;
	  break;

	default:
#if SIZEOF_ARG == 8
	  FFI_ASSERT(FALSE);	/* Should have covered all cases */
#else	
	  memcpy ((void*) raw->data, (void*)*args, (*tp)->size);
	  raw += ALIGN ((*tp)->size, SIZEOF_ARG) / SIZEOF_ARG;
#endif
	}
    }
}

#if !FFI_NATIVE_RAW_API


/* This is a generic definition of ffi_raw_call, to be used if the
 * native system does not provide a machine-specific implementation.
 * Having this, allows code to be written for the raw API, without
 * the need for system-specific code to handle input in that format;
 * these following couple of functions will handle the translation forth
 * and back automatically. */

void ffi_java_raw_call (/*@dependent@*/ ffi_cif *cif, 
		   void (*fn)(), 
		   /*@out@*/ void *rvalue, 
		   /*@dependent@*/ ffi_raw *raw)
{
  void **avalue = (void**) alloca (cif->nargs * sizeof (void*));
  ffi_java_raw_to_ptrarray (cif, raw, avalue);
  ffi_call (cif, fn, rvalue, avalue);
}

#if FFI_CLOSURES		/* base system provides closures */

static void 
ffi_java_translate_args (ffi_cif *cif, void *rvalue,
		    void **avalue, void *user_data)
{
  ffi_raw *raw = (ffi_raw*)alloca (ffi_java_raw_size (cif));
  ffi_raw_closure *cl = (ffi_raw_closure*)user_data;

  ffi_java_ptrarray_to_raw (cif, avalue, raw);
  (*cl->fun) (cif, rvalue, raw, cl->user_data);
}

/* Again, here is the generic version of ffi_prep_raw_closure, which
 * will install an intermediate "hub" for translation of arguments from
 * the pointer-array format, to the raw format */

ffi_status
ffi_prep_java_raw_closure (ffi_raw_closure* cl,
		      ffi_cif *cif,
		      void (*fun)(ffi_cif*,void*,ffi_raw*,void*),
		      void *user_data)
{
  ffi_status status;

  status = ffi_prep_closure ((ffi_closure*) cl, 
			     cif,
			     &ffi_java_translate_args,
			     (void*)cl);
  if (status == FFI_OK)
    {
      cl->fun       = fun;
      cl->user_data = user_data;
    }

  return status;
}

#endif /* FFI_CLOSURES */
#endif /* !FFI_NATIVE_RAW_API */
#endif /* !FFI_NO_RAW_API */
