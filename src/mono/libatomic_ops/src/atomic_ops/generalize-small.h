/* char_load */
#if defined(AO_HAVE_char_load_acquire) && !defined(AO_HAVE_char_load)
#  define AO_char_load(addr) AO_char_load_acquire(addr)
#  define AO_HAVE_char_load
#endif

#if defined(AO_HAVE_char_load_full) && !defined(AO_HAVE_char_load_acquire)
#  define AO_char_load_acquire(addr) AO_char_load_full(addr)
#  define AO_HAVE_char_load_acquire
#endif

#if defined(AO_HAVE_char_load_full) && !defined(AO_HAVE_char_load_read)
#  define AO_char_load_read(addr) AO_char_load_full(addr)
#  define AO_HAVE_char_load_read
#endif

#if !defined(AO_HAVE_char_load_acquire_read) && defined(AO_HAVE_char_load_acquire)
#  define AO_char_load_acquire_read(addr) AO_char_load_acquire(addr)
#  define AO_HAVE_char_load_acquire_read
#endif

#if defined(AO_HAVE_char_load) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_char_load_acquire)
   AO_INLINE unsigned char
   AO_char_load_acquire(volatile unsigned char *addr)
   {
     unsigned char result = AO_char_load(addr);
     /* Acquire barrier would be useless, since the load could be delayed  */
     /* beyond it.							   */
     AO_nop_full();
     return result;
   }
#  define AO_HAVE_char_load_acquire
#endif

#if defined(AO_HAVE_char_load) && defined(AO_HAVE_nop_read) && \
    !defined(AO_HAVE_char_load_read)
   AO_INLINE unsigned char
   AO_char_load_read(volatile unsigned char *addr)
   {
     unsigned char result = AO_char_load(addr);
     /* Acquire barrier would be useless, since the load could be delayed  */
     /* beyond it.							   */
     AO_nop_read();
     return result;
   }
#  define AO_HAVE_char_load_read
#endif

#if defined(AO_HAVE_char_load_acquire) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_char_load_full)
#  define AO_char_load_full(addr) (AO_nop_full(), AO_char_load_acquire(addr))
#  define AO_HAVE_char_load_full
#endif
 
#if !defined(AO_HAVE_char_load_acquire_read) && defined(AO_HAVE_char_load_read)
#  define AO_char_load_acquire_read(addr) AO_char_load_read(addr)
#  define AO_HAVE_char_load_acquire_read
#endif

#if defined(AO_HAVE_char_load_acquire_read) && !defined(AO_HAVE_char_load)
#  define AO_char_load(addr) AO_char_load_acquire_read(addr)
#  define AO_HAVE_char_load
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_char_load_acquire_read)
#    define AO_char_load_dd_acquire_read(addr) \
	AO_char_load_acquire_read(addr)
#    define AO_HAVE_char_load_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_char_load)
#    define AO_char_load_dd_acquire_read(addr) \
	AO_char_load(addr)
#    define AO_HAVE_char_load_dd_acquire_read
#  endif
#endif


/* char_store */

#if defined(AO_HAVE_char_store_release) && !defined(AO_HAVE_char_store)
#  define AO_char_store(addr, val) AO_char_store_release(addr,val)
#  define AO_HAVE_char_store
#endif

#if defined(AO_HAVE_char_store_full) && !defined(AO_HAVE_char_store_release)
#  define AO_char_store_release(addr,val) AO_char_store_full(addr,val)
#  define AO_HAVE_char_store_release
#endif

#if defined(AO_HAVE_char_store_full) && !defined(AO_HAVE_char_store_write)
#  define AO_char_store_write(addr,val) AO_char_store_full(addr,val)
#  define AO_HAVE_char_store_write
#endif

#if defined(AO_HAVE_char_store_release) && \
	!defined(AO_HAVE_char_store_release_write)
#  define AO_char_store_release_write(addr, val) \
	AO_char_store_release(addr,val)
#  define AO_HAVE_char_store_release_write
#endif

#if defined(AO_HAVE_char_store_write) && !defined(AO_HAVE_char_store)
#  define AO_char_store(addr, val) AO_char_store_write(addr,val)
#  define AO_HAVE_char_store
#endif

#if defined(AO_HAVE_char_store) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_char_store_release)
#  define AO_char_store_release(addr,val) \
	(AO_nop_full(), AO_char_store(addr,val))
#  define AO_HAVE_char_store_release
#endif

#if defined(AO_HAVE_nop_write) && defined(AO_HAVE_char_store) && \
     !defined(AO_HAVE_char_store_write)
#  define AO_char_store_write(addr, val) \
	(AO_nop_write(), AO_char_store(addr,val))
#  define AO_HAVE_char_store_write
#endif

#if defined(AO_HAVE_char_store_write) && \
     !defined(AO_HAVE_char_store_release_write)
#  define AO_char_store_release_write(addr, val) AO_char_store_write(addr,val)
#  define AO_HAVE_char_store_release_write
#endif

#if defined(AO_HAVE_char_store_release) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_char_store_full)
#  define AO_char_store_full(addr, val) \
	(AO_char_store_release(addr, val), AO_nop_full())
#  define AO_HAVE_char_store_full
#endif


/* char_fetch_and_add */
#if defined(AO_HAVE_char_compare_and_swap_full) && \
    !defined(AO_HAVE_char_fetch_and_add_full)
   AO_INLINE AO_t
   AO_char_fetch_and_add_full(volatile unsigned char *addr,
   			       unsigned char incr)
   {
     unsigned char old;
     do
       {
         old = *addr;
       }
     while (!AO_char_compare_and_swap_full(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_char_fetch_and_add_full
#endif

#if defined(AO_HAVE_char_compare_and_swap_acquire) && \
    !defined(AO_HAVE_char_fetch_and_add_acquire)
   AO_INLINE AO_t
   AO_char_fetch_and_add_acquire(volatile unsigned char *addr,
   				  unsigned char incr)
   {
     unsigned char old;
     do
       {
         old = *addr;
       }
     while (!AO_char_compare_and_swap_acquire(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_char_fetch_and_add_acquire
#endif

#if defined(AO_HAVE_char_compare_and_swap_release) && \
    !defined(AO_HAVE_char_fetch_and_add_release)
   AO_INLINE AO_t
   AO_char_fetch_and_add_release(volatile unsigned char *addr,
   				  unsigned char incr)
   {
     unsigned char old;
     do
       {
         old = *addr;
       }
     while (!AO_char_compare_and_swap_release(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_char_fetch_and_add_release
#endif

#if defined(AO_HAVE_char_fetch_and_add_full)
#  if !defined(AO_HAVE_char_fetch_and_add_release)
#    define AO_char_fetch_and_add_release(addr, val) \
  	 AO_char_fetch_and_add_full(addr, val)
#    define AO_HAVE_char_fetch_and_add_release
#  endif
#  if !defined(AO_HAVE_char_fetch_and_add_acquire)
#    define AO_char_fetch_and_add_acquire(addr, val) \
  	 AO_char_fetch_and_add_full(addr, val)
#    define AO_HAVE_char_fetch_and_add_acquire
#  endif
#  if !defined(AO_HAVE_char_fetch_and_add_write)
#    define AO_char_fetch_and_add_write(addr, val) \
  	 AO_char_fetch_and_add_full(addr, val)
#    define AO_HAVE_char_fetch_and_add_write
#  endif
#  if !defined(AO_HAVE_char_fetch_and_add_read)
#    define AO_char_fetch_and_add_read(addr, val) \
  	 AO_char_fetch_and_add_full(addr, val)
#    define AO_HAVE_char_fetch_and_add_read
#  endif
#endif /* AO_HAVE_char_fetch_and_add_full */

#if !defined(AO_HAVE_char_fetch_and_add) && \
    defined(AO_HAVE_char_fetch_and_add_release)
#  define AO_char_fetch_and_add(addr, val) \
  	AO_char_fetch_and_add_release(addr, val)
#  define AO_HAVE_char_fetch_and_add
#endif
#if !defined(AO_HAVE_char_fetch_and_add) && \
    defined(AO_HAVE_char_fetch_and_add_acquire)
#  define AO_char_fetch_and_add(addr, val) \
  	AO_char_fetch_and_add_acquire(addr, val)
#  define AO_HAVE_char_fetch_and_add
#endif
#if !defined(AO_HAVE_char_fetch_and_add) && \
    defined(AO_HAVE_char_fetch_and_add_write)
#  define AO_char_fetch_and_add(addr, val) \
  	AO_char_fetch_and_add_write(addr, val)
#  define AO_HAVE_char_fetch_and_add
#endif
#if !defined(AO_HAVE_char_fetch_and_add) && \
    defined(AO_HAVE_char_fetch_and_add_read)
#  define AO_char_fetch_and_add(addr, val) \
  	AO_char_fetch_and_add_read(addr, val)
#  define AO_HAVE_char_fetch_and_add
#endif

#if defined(AO_HAVE_char_fetch_and_add_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_char_fetch_and_add_full)
#  define AO_char_fetch_and_add_full(addr, val) \
  	(AO_nop_full(), AO_char_fetch_and_add_acquire(addr, val))
#endif

#if !defined(AO_HAVE_char_fetch_and_add_release_write) && \
    defined(AO_HAVE_char_fetch_and_add_write)
#  define AO_char_fetch_and_add_release_write(addr, val) \
  	AO_char_fetch_and_add_write(addr, val)
#  define AO_HAVE_char_fetch_and_add_release_write
#endif
#if !defined(AO_HAVE_char_fetch_and_add_release_write) && \
    defined(AO_HAVE_char_fetch_and_add_release)
#  define AO_char_fetch_and_add_release_write(addr, val) \
  	AO_char_fetch_and_add_release(addr, val)
#  define AO_HAVE_char_fetch_and_add_release_write
#endif
#if !defined(AO_HAVE_char_fetch_and_add_acquire_read) && \
    defined(AO_HAVE_char_fetch_and_add_read)
#  define AO_char_fetch_and_add_acquire_read(addr, val) \
  	AO_char_fetch_and_add_read(addr, val)
#  define AO_HAVE_char_fetch_and_add_acquire_read
#endif
#if !defined(AO_HAVE_char_fetch_and_add_acquire_read) && \
    defined(AO_HAVE_char_fetch_and_add_acquire)
#  define AO_char_fetch_and_add_acquire_read(addr, val) \
  	AO_char_fetch_and_add_acquire(addr, val)
#  define AO_HAVE_char_fetch_and_add_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_char_fetch_and_add_acquire_read)
#    define AO_char_fetch_and_add_dd_acquire_read(addr, val) \
	AO_char_fetch_and_add_acquire_read(addr, val)
#    define AO_HAVE_char_fetch_and_add_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_char_fetch_and_add)
#    define AO_char_fetch_and_add_dd_acquire_read(addr, val) \
	AO_char_fetch_and_add(addr, val)
#    define AO_HAVE_char_fetch_and_add_dd_acquire_read
#  endif
#endif
  
/* char_fetch_and_add1 */

#if defined(AO_HAVE_char_fetch_and_add_full) &&\
    !defined(AO_HAVE_char_fetch_and_add1_full)
#  define AO_char_fetch_and_add1_full(addr) \
	AO_char_fetch_and_add_full(addr,1)
#  define AO_HAVE_char_fetch_and_add1_full
#endif
#if defined(AO_HAVE_char_fetch_and_add_release) &&\
    !defined(AO_HAVE_char_fetch_and_add1_release)
#  define AO_char_fetch_and_add1_release(addr) \
	AO_char_fetch_and_add_release(addr,1)
#  define AO_HAVE_char_fetch_and_add1_release
#endif
#if defined(AO_HAVE_char_fetch_and_add_acquire) &&\
    !defined(AO_HAVE_char_fetch_and_add1_acquire)
#  define AO_char_fetch_and_add1_acquire(addr) \
	AO_char_fetch_and_add_acquire(addr,1)
#  define AO_HAVE_char_fetch_and_add1_acquire
#endif
#if defined(AO_HAVE_char_fetch_and_add_write) &&\
    !defined(AO_HAVE_char_fetch_and_add1_write)
#  define AO_char_fetch_and_add1_write(addr) \
	AO_char_fetch_and_add_write(addr,1)
#  define AO_HAVE_char_fetch_and_add1_write
#endif
#if defined(AO_HAVE_char_fetch_and_add_read) &&\
    !defined(AO_HAVE_char_fetch_and_add1_read)
#  define AO_char_fetch_and_add1_read(addr) \
	AO_char_fetch_and_add_read(addr,1)
#  define AO_HAVE_char_fetch_and_add1_read
#endif
#if defined(AO_HAVE_char_fetch_and_add_release_write) &&\
    !defined(AO_HAVE_char_fetch_and_add1_release_write)
#  define AO_char_fetch_and_add1_release_write(addr) \
	AO_char_fetch_and_add_release_write(addr,1)
#  define AO_HAVE_char_fetch_and_add1_release_write
#endif
#if defined(AO_HAVE_char_fetch_and_add_acquire_read) &&\
    !defined(AO_HAVE_char_fetch_and_add1_acquire_read)
#  define AO_char_fetch_and_add1_acquire_read(addr) \
	AO_char_fetch_and_add_acquire_read(addr,1)
#  define AO_HAVE_char_fetch_and_add1_acquire_read
#endif
#if defined(AO_HAVE_char_fetch_and_add) &&\
    !defined(AO_HAVE_char_fetch_and_add1)
#  define AO_char_fetch_and_add1(addr) \
	AO_char_fetch_and_add(addr,1)
#  define AO_HAVE_char_fetch_and_add1
#endif

#if defined(AO_HAVE_char_fetch_and_add1_full)
#  if !defined(AO_HAVE_char_fetch_and_add1_release)
#    define AO_char_fetch_and_add1_release(addr) \
  	 AO_char_fetch_and_add1_full(addr)
#    define AO_HAVE_char_fetch_and_add1_release
#  endif
#  if !defined(AO_HAVE_char_fetch_and_add1_acquire)
#    define AO_char_fetch_and_add1_acquire(addr) \
  	 AO_char_fetch_and_add1_full(addr)
#    define AO_HAVE_char_fetch_and_add1_acquire
#  endif
#  if !defined(AO_HAVE_char_fetch_and_add1_write)
#    define AO_char_fetch_and_add1_write(addr) \
  	 AO_char_fetch_and_add1_full(addr)
#    define AO_HAVE_char_fetch_and_add1_write
#  endif
#  if !defined(AO_HAVE_char_fetch_and_add1_read)
#    define AO_char_fetch_and_add1_read(addr) \
  	 AO_char_fetch_and_add1_full(addr)
#    define AO_HAVE_char_fetch_and_add1_read
#  endif
#endif /* AO_HAVE_char_fetch_and_add1_full */

#if !defined(AO_HAVE_char_fetch_and_add1) && \
    defined(AO_HAVE_char_fetch_and_add1_release)
#  define AO_char_fetch_and_add1(addr) \
  	AO_char_fetch_and_add1_release(addr)
#  define AO_HAVE_char_fetch_and_add1
#endif
#if !defined(AO_HAVE_char_fetch_and_add1) && \
    defined(AO_HAVE_char_fetch_and_add1_acquire)
#  define AO_char_fetch_and_add1(addr) \
  	AO_char_fetch_and_add1_acquire(addr)
#  define AO_HAVE_char_fetch_and_add1
#endif
#if !defined(AO_HAVE_char_fetch_and_add1) && \
    defined(AO_HAVE_char_fetch_and_add1_write)
#  define AO_char_fetch_and_add1(addr) \
  	AO_char_fetch_and_add1_write(addr)
#  define AO_HAVE_char_fetch_and_add1
#endif
#if !defined(AO_HAVE_char_fetch_and_add1) && \
    defined(AO_HAVE_char_fetch_and_add1_read)
#  define AO_char_fetch_and_add1(addr) \
  	AO_char_fetch_and_add1_read(addr)
#  define AO_HAVE_char_fetch_and_add1
#endif

#if defined(AO_HAVE_char_fetch_and_add1_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_char_fetch_and_add1_full)
#  define AO_char_fetch_and_add1_full(addr) \
  	(AO_nop_full(), AO_char_fetch_and_add1_acquire(addr))
#  define AO_HAVE_char_fetch_and_add1_full
#endif

#if !defined(AO_HAVE_char_fetch_and_add1_release_write) && \
    defined(AO_HAVE_char_fetch_and_add1_write)
#  define AO_char_fetch_and_add1_release_write(addr) \
  	AO_char_fetch_and_add1_write(addr)
#  define AO_HAVE_char_fetch_and_add1_release_write
#endif
#if !defined(AO_HAVE_char_fetch_and_add1_release_write) && \
    defined(AO_HAVE_char_fetch_and_add1_release)
#  define AO_char_fetch_and_add1_release_write(addr) \
  	AO_char_fetch_and_add1_release(addr)
#  define AO_HAVE_char_fetch_and_add1_release_write
#endif
#if !defined(AO_HAVE_char_fetch_and_add1_acquire_read) && \
    defined(AO_HAVE_char_fetch_and_add1_read)
#  define AO_char_fetch_and_add1_acquire_read(addr) \
  	AO_char_fetch_and_add1_read(addr)
#  define AO_HAVE_char_fetch_and_add1_acquire_read
#endif
#if !defined(AO_HAVE_char_fetch_and_add1_acquire_read) && \
    defined(AO_HAVE_char_fetch_and_add1_acquire)
#  define AO_char_fetch_and_add1_acquire_read(addr) \
  	AO_char_fetch_and_add1_acquire(addr)
#  define AO_HAVE_char_fetch_and_add1_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_char_fetch_and_add1_acquire_read)
#    define AO_char_fetch_and_add1_dd_acquire_read(addr) \
	AO_char_fetch_and_add1_acquire_read(addr)
#    define AO_HAVE_char_fetch_and_add1_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_char_fetch_and_add1)
#    define AO_char_fetch_and_add1_dd_acquire_read(addr) \
	AO_char_fetch_and_add1(addr)
#    define AO_HAVE_char_fetch_and_add1_dd_acquire_read
#  endif
#endif

/* char_fetch_and_sub1 */

#if defined(AO_HAVE_char_fetch_and_add_full) &&\
    !defined(AO_HAVE_char_fetch_and_sub1_full)
#  define AO_char_fetch_and_sub1_full(addr) \
	AO_char_fetch_and_add_full(addr,(unsigned char)(-1))
#  define AO_HAVE_char_fetch_and_sub1_full
#endif
#if defined(AO_HAVE_char_fetch_and_add_release) &&\
    !defined(AO_HAVE_char_fetch_and_sub1_release)
#  define AO_char_fetch_and_sub1_release(addr) \
	AO_char_fetch_and_add_release(addr,(unsigned char)(-1))
#  define AO_HAVE_char_fetch_and_sub1_release
#endif
#if defined(AO_HAVE_char_fetch_and_add_acquire) &&\
    !defined(AO_HAVE_char_fetch_and_sub1_acquire)
#  define AO_char_fetch_and_sub1_acquire(addr) \
	AO_char_fetch_and_add_acquire(addr,(unsigned char)(-1))
#  define AO_HAVE_char_fetch_and_sub1_acquire
#endif
#if defined(AO_HAVE_char_fetch_and_add_write) &&\
    !defined(AO_HAVE_char_fetch_and_sub1_write)
#  define AO_char_fetch_and_sub1_write(addr) \
	AO_char_fetch_and_add_write(addr,(unsigned char)(-1))
#  define AO_HAVE_char_fetch_and_sub1_write
#endif
#if defined(AO_HAVE_char_fetch_and_add_read) &&\
    !defined(AO_HAVE_char_fetch_and_sub1_read)
#  define AO_char_fetch_and_sub1_read(addr) \
	AO_char_fetch_and_add_read(addr,(unsigned char)(-1))
#  define AO_HAVE_char_fetch_and_sub1_read
#endif
#if defined(AO_HAVE_char_fetch_and_add_release_write) &&\
    !defined(AO_HAVE_char_fetch_and_sub1_release_write)
#  define AO_char_fetch_and_sub1_release_write(addr) \
	AO_char_fetch_and_add_release_write(addr,(unsigned char)(-1))
#  define AO_HAVE_char_fetch_and_sub1_release_write
#endif
#if defined(AO_HAVE_char_fetch_and_add_acquire_read) &&\
    !defined(AO_HAVE_char_fetch_and_sub1_acquire_read)
#  define AO_char_fetch_and_sub1_acquire_read(addr) \
	AO_char_fetch_and_add_acquire_read(addr,(unsigned char)(-1))
#  define AO_HAVE_char_fetch_and_sub1_acquire_read
#endif
#if defined(AO_HAVE_char_fetch_and_add) &&\
    !defined(AO_HAVE_char_fetch_and_sub1)
#  define AO_char_fetch_and_sub1(addr) \
	AO_char_fetch_and_add(addr,(unsigned char)(-1))
#  define AO_HAVE_char_fetch_and_sub1
#endif

#if defined(AO_HAVE_char_fetch_and_sub1_full)
#  if !defined(AO_HAVE_char_fetch_and_sub1_release)
#    define AO_char_fetch_and_sub1_release(addr) \
  	 AO_char_fetch_and_sub1_full(addr)
#    define AO_HAVE_char_fetch_and_sub1_release
#  endif
#  if !defined(AO_HAVE_char_fetch_and_sub1_acquire)
#    define AO_char_fetch_and_sub1_acquire(addr) \
  	 AO_char_fetch_and_sub1_full(addr)
#    define AO_HAVE_char_fetch_and_sub1_acquire
#  endif
#  if !defined(AO_HAVE_char_fetch_and_sub1_write)
#    define AO_char_fetch_and_sub1_write(addr) \
  	 AO_char_fetch_and_sub1_full(addr)
#    define AO_HAVE_char_fetch_and_sub1_write
#  endif
#  if !defined(AO_HAVE_char_fetch_and_sub1_read)
#    define AO_char_fetch_and_sub1_read(addr) \
  	 AO_char_fetch_and_sub1_full(addr)
#    define AO_HAVE_char_fetch_and_sub1_read
#  endif
#endif /* AO_HAVE_char_fetch_and_sub1_full */

#if !defined(AO_HAVE_char_fetch_and_sub1) && \
    defined(AO_HAVE_char_fetch_and_sub1_release)
#  define AO_char_fetch_and_sub1(addr) \
  	AO_char_fetch_and_sub1_release(addr)
#  define AO_HAVE_char_fetch_and_sub1
#endif
#if !defined(AO_HAVE_char_fetch_and_sub1) && \
    defined(AO_HAVE_char_fetch_and_sub1_acquire)
#  define AO_char_fetch_and_sub1(addr) \
  	AO_char_fetch_and_sub1_acquire(addr)
#  define AO_HAVE_char_fetch_and_sub1
#endif
#if !defined(AO_HAVE_char_fetch_and_sub1) && \
    defined(AO_HAVE_char_fetch_and_sub1_write)
#  define AO_char_fetch_and_sub1(addr) \
  	AO_char_fetch_and_sub1_write(addr)
#  define AO_HAVE_char_fetch_and_sub1
#endif
#if !defined(AO_HAVE_char_fetch_and_sub1) && \
    defined(AO_HAVE_char_fetch_and_sub1_read)
#  define AO_char_fetch_and_sub1(addr) \
  	AO_char_fetch_and_sub1_read(addr)
#  define AO_HAVE_char_fetch_and_sub1
#endif

#if defined(AO_HAVE_char_fetch_and_sub1_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_char_fetch_and_sub1_full)
#  define AO_char_fetch_and_sub1_full(addr) \
  	(AO_nop_full(), AO_char_fetch_and_sub1_acquire(addr))
#  define AO_HAVE_char_fetch_and_sub1_full
#endif

#if !defined(AO_HAVE_char_fetch_and_sub1_release_write) && \
    defined(AO_HAVE_char_fetch_and_sub1_write)
#  define AO_char_fetch_and_sub1_release_write(addr) \
  	AO_char_fetch_and_sub1_write(addr)
#  define AO_HAVE_char_fetch_and_sub1_release_write
#endif
#if !defined(AO_HAVE_char_fetch_and_sub1_release_write) && \
    defined(AO_HAVE_char_fetch_and_sub1_release)
#  define AO_char_fetch_and_sub1_release_write(addr) \
  	AO_char_fetch_and_sub1_release(addr)
#  define AO_HAVE_char_fetch_and_sub1_release_write
#endif
#if !defined(AO_HAVE_char_fetch_and_sub1_acquire_read) && \
    defined(AO_HAVE_char_fetch_and_sub1_read)
#  define AO_char_fetch_and_sub1_acquire_read(addr) \
  	AO_char_fetch_and_sub1_read(addr)
#  define AO_HAVE_char_fetch_and_sub1_acquire_read
#endif
#if !defined(AO_HAVE_char_fetch_and_sub1_acquire_read) && \
    defined(AO_HAVE_char_fetch_and_sub1_acquire)
#  define AO_char_fetch_and_sub1_acquire_read(addr) \
  	AO_char_fetch_and_sub1_acquire(addr)
#  define AO_HAVE_char_fetch_and_sub1_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_char_fetch_and_sub1_acquire_read)
#    define AO_char_fetch_and_sub1_dd_acquire_read(addr) \
	AO_char_fetch_and_sub1_acquire_read(addr)
#    define AO_HAVE_char_fetch_and_sub1_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_char_fetch_and_sub1)
#    define AO_char_fetch_and_sub1_dd_acquire_read(addr) \
	AO_char_fetch_and_sub1(addr)
#    define AO_HAVE_char_fetch_and_sub1_dd_acquire_read
#  endif
#endif

/* short_load */
#if defined(AO_HAVE_short_load_acquire) && !defined(AO_HAVE_short_load)
#  define AO_short_load(addr) AO_short_load_acquire(addr)
#  define AO_HAVE_short_load
#endif

#if defined(AO_HAVE_short_load_full) && !defined(AO_HAVE_short_load_acquire)
#  define AO_short_load_acquire(addr) AO_short_load_full(addr)
#  define AO_HAVE_short_load_acquire
#endif

#if defined(AO_HAVE_short_load_full) && !defined(AO_HAVE_short_load_read)
#  define AO_short_load_read(addr) AO_short_load_full(addr)
#  define AO_HAVE_short_load_read
#endif

#if !defined(AO_HAVE_short_load_acquire_read) && defined(AO_HAVE_short_load_acquire)
#  define AO_short_load_acquire_read(addr) AO_short_load_acquire(addr)
#  define AO_HAVE_short_load_acquire_read
#endif

#if defined(AO_HAVE_short_load) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_short_load_acquire)
   AO_INLINE unsigned short
   AO_short_load_acquire(volatile unsigned short *addr)
   {
     unsigned short result = AO_short_load(addr);
     /* Acquire barrier would be useless, since the load could be delayed  */
     /* beyond it.							   */
     AO_nop_full();
     return result;
   }
#  define AO_HAVE_short_load_acquire
#endif

#if defined(AO_HAVE_short_load) && defined(AO_HAVE_nop_read) && \
    !defined(AO_HAVE_short_load_read)
   AO_INLINE unsigned short
   AO_short_load_read(volatile unsigned short *addr)
   {
     unsigned short result = AO_short_load(addr);
     /* Acquire barrier would be useless, since the load could be delayed  */
     /* beyond it.							   */
     AO_nop_read();
     return result;
   }
#  define AO_HAVE_short_load_read
#endif

#if defined(AO_HAVE_short_load_acquire) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_short_load_full)
#  define AO_short_load_full(addr) (AO_nop_full(), AO_short_load_acquire(addr))
#  define AO_HAVE_short_load_full
#endif
 
#if !defined(AO_HAVE_short_load_acquire_read) && defined(AO_HAVE_short_load_read)
#  define AO_short_load_acquire_read(addr) AO_short_load_read(addr)
#  define AO_HAVE_short_load_acquire_read
#endif

#if defined(AO_HAVE_short_load_acquire_read) && !defined(AO_HAVE_short_load)
#  define AO_short_load(addr) AO_short_load_acquire_read(addr)
#  define AO_HAVE_short_load
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_short_load_acquire_read)
#    define AO_short_load_dd_acquire_read(addr) \
	AO_short_load_acquire_read(addr)
#    define AO_HAVE_short_load_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_short_load)
#    define AO_short_load_dd_acquire_read(addr) \
	AO_short_load(addr)
#    define AO_HAVE_short_load_dd_acquire_read
#  endif
#endif


/* short_store */

#if defined(AO_HAVE_short_store_release) && !defined(AO_HAVE_short_store)
#  define AO_short_store(addr, val) AO_short_store_release(addr,val)
#  define AO_HAVE_short_store
#endif

#if defined(AO_HAVE_short_store_full) && !defined(AO_HAVE_short_store_release)
#  define AO_short_store_release(addr,val) AO_short_store_full(addr,val)
#  define AO_HAVE_short_store_release
#endif

#if defined(AO_HAVE_short_store_full) && !defined(AO_HAVE_short_store_write)
#  define AO_short_store_write(addr,val) AO_short_store_full(addr,val)
#  define AO_HAVE_short_store_write
#endif

#if defined(AO_HAVE_short_store_release) && \
	!defined(AO_HAVE_short_store_release_write)
#  define AO_short_store_release_write(addr, val) \
	AO_short_store_release(addr,val)
#  define AO_HAVE_short_store_release_write
#endif

#if defined(AO_HAVE_short_store_write) && !defined(AO_HAVE_short_store)
#  define AO_short_store(addr, val) AO_short_store_write(addr,val)
#  define AO_HAVE_short_store
#endif

#if defined(AO_HAVE_short_store) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_short_store_release)
#  define AO_short_store_release(addr,val) \
	(AO_nop_full(), AO_short_store(addr,val))
#  define AO_HAVE_short_store_release
#endif

#if defined(AO_HAVE_nop_write) && defined(AO_HAVE_short_store) && \
     !defined(AO_HAVE_short_store_write)
#  define AO_short_store_write(addr, val) \
	(AO_nop_write(), AO_short_store(addr,val))
#  define AO_HAVE_short_store_write
#endif

#if defined(AO_HAVE_short_store_write) && \
     !defined(AO_HAVE_short_store_release_write)
#  define AO_short_store_release_write(addr, val) AO_short_store_write(addr,val)
#  define AO_HAVE_short_store_release_write
#endif

#if defined(AO_HAVE_short_store_release) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_short_store_full)
#  define AO_short_store_full(addr, val) \
	(AO_short_store_release(addr, val), AO_nop_full())
#  define AO_HAVE_short_store_full
#endif


/* short_fetch_and_add */
#if defined(AO_HAVE_short_compare_and_swap_full) && \
    !defined(AO_HAVE_short_fetch_and_add_full)
   AO_INLINE AO_t
   AO_short_fetch_and_add_full(volatile unsigned short *addr,
   			       unsigned short incr)
   {
     unsigned short old;
     do
       {
         old = *addr;
       }
     while (!AO_short_compare_and_swap_full(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_short_fetch_and_add_full
#endif

#if defined(AO_HAVE_short_compare_and_swap_acquire) && \
    !defined(AO_HAVE_short_fetch_and_add_acquire)
   AO_INLINE AO_t
   AO_short_fetch_and_add_acquire(volatile unsigned short *addr,
   				  unsigned short incr)
   {
     unsigned short old;
     do
       {
         old = *addr;
       }
     while (!AO_short_compare_and_swap_acquire(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_short_fetch_and_add_acquire
#endif

#if defined(AO_HAVE_short_compare_and_swap_release) && \
    !defined(AO_HAVE_short_fetch_and_add_release)
   AO_INLINE AO_t
   AO_short_fetch_and_add_release(volatile unsigned short *addr,
   				  unsigned short incr)
   {
     unsigned short old;
     do
       {
         old = *addr;
       }
     while (!AO_short_compare_and_swap_release(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_short_fetch_and_add_release
#endif

#if defined(AO_HAVE_short_fetch_and_add_full)
#  if !defined(AO_HAVE_short_fetch_and_add_release)
#    define AO_short_fetch_and_add_release(addr, val) \
  	 AO_short_fetch_and_add_full(addr, val)
#    define AO_HAVE_short_fetch_and_add_release
#  endif
#  if !defined(AO_HAVE_short_fetch_and_add_acquire)
#    define AO_short_fetch_and_add_acquire(addr, val) \
  	 AO_short_fetch_and_add_full(addr, val)
#    define AO_HAVE_short_fetch_and_add_acquire
#  endif
#  if !defined(AO_HAVE_short_fetch_and_add_write)
#    define AO_short_fetch_and_add_write(addr, val) \
  	 AO_short_fetch_and_add_full(addr, val)
#    define AO_HAVE_short_fetch_and_add_write
#  endif
#  if !defined(AO_HAVE_short_fetch_and_add_read)
#    define AO_short_fetch_and_add_read(addr, val) \
  	 AO_short_fetch_and_add_full(addr, val)
#    define AO_HAVE_short_fetch_and_add_read
#  endif
#endif /* AO_HAVE_short_fetch_and_add_full */

#if !defined(AO_HAVE_short_fetch_and_add) && \
    defined(AO_HAVE_short_fetch_and_add_release)
#  define AO_short_fetch_and_add(addr, val) \
  	AO_short_fetch_and_add_release(addr, val)
#  define AO_HAVE_short_fetch_and_add
#endif
#if !defined(AO_HAVE_short_fetch_and_add) && \
    defined(AO_HAVE_short_fetch_and_add_acquire)
#  define AO_short_fetch_and_add(addr, val) \
  	AO_short_fetch_and_add_acquire(addr, val)
#  define AO_HAVE_short_fetch_and_add
#endif
#if !defined(AO_HAVE_short_fetch_and_add) && \
    defined(AO_HAVE_short_fetch_and_add_write)
#  define AO_short_fetch_and_add(addr, val) \
  	AO_short_fetch_and_add_write(addr, val)
#  define AO_HAVE_short_fetch_and_add
#endif
#if !defined(AO_HAVE_short_fetch_and_add) && \
    defined(AO_HAVE_short_fetch_and_add_read)
#  define AO_short_fetch_and_add(addr, val) \
  	AO_short_fetch_and_add_read(addr, val)
#  define AO_HAVE_short_fetch_and_add
#endif

#if defined(AO_HAVE_short_fetch_and_add_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_short_fetch_and_add_full)
#  define AO_short_fetch_and_add_full(addr, val) \
  	(AO_nop_full(), AO_short_fetch_and_add_acquire(addr, val))
#endif

#if !defined(AO_HAVE_short_fetch_and_add_release_write) && \
    defined(AO_HAVE_short_fetch_and_add_write)
#  define AO_short_fetch_and_add_release_write(addr, val) \
  	AO_short_fetch_and_add_write(addr, val)
#  define AO_HAVE_short_fetch_and_add_release_write
#endif
#if !defined(AO_HAVE_short_fetch_and_add_release_write) && \
    defined(AO_HAVE_short_fetch_and_add_release)
#  define AO_short_fetch_and_add_release_write(addr, val) \
  	AO_short_fetch_and_add_release(addr, val)
#  define AO_HAVE_short_fetch_and_add_release_write
#endif
#if !defined(AO_HAVE_short_fetch_and_add_acquire_read) && \
    defined(AO_HAVE_short_fetch_and_add_read)
#  define AO_short_fetch_and_add_acquire_read(addr, val) \
  	AO_short_fetch_and_add_read(addr, val)
#  define AO_HAVE_short_fetch_and_add_acquire_read
#endif
#if !defined(AO_HAVE_short_fetch_and_add_acquire_read) && \
    defined(AO_HAVE_short_fetch_and_add_acquire)
#  define AO_short_fetch_and_add_acquire_read(addr, val) \
  	AO_short_fetch_and_add_acquire(addr, val)
#  define AO_HAVE_short_fetch_and_add_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_short_fetch_and_add_acquire_read)
#    define AO_short_fetch_and_add_dd_acquire_read(addr, val) \
	AO_short_fetch_and_add_acquire_read(addr, val)
#    define AO_HAVE_short_fetch_and_add_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_short_fetch_and_add)
#    define AO_short_fetch_and_add_dd_acquire_read(addr, val) \
	AO_short_fetch_and_add(addr, val)
#    define AO_HAVE_short_fetch_and_add_dd_acquire_read
#  endif
#endif
  
/* short_fetch_and_add1 */

#if defined(AO_HAVE_short_fetch_and_add_full) &&\
    !defined(AO_HAVE_short_fetch_and_add1_full)
#  define AO_short_fetch_and_add1_full(addr) \
	AO_short_fetch_and_add_full(addr,1)
#  define AO_HAVE_short_fetch_and_add1_full
#endif
#if defined(AO_HAVE_short_fetch_and_add_release) &&\
    !defined(AO_HAVE_short_fetch_and_add1_release)
#  define AO_short_fetch_and_add1_release(addr) \
	AO_short_fetch_and_add_release(addr,1)
#  define AO_HAVE_short_fetch_and_add1_release
#endif
#if defined(AO_HAVE_short_fetch_and_add_acquire) &&\
    !defined(AO_HAVE_short_fetch_and_add1_acquire)
#  define AO_short_fetch_and_add1_acquire(addr) \
	AO_short_fetch_and_add_acquire(addr,1)
#  define AO_HAVE_short_fetch_and_add1_acquire
#endif
#if defined(AO_HAVE_short_fetch_and_add_write) &&\
    !defined(AO_HAVE_short_fetch_and_add1_write)
#  define AO_short_fetch_and_add1_write(addr) \
	AO_short_fetch_and_add_write(addr,1)
#  define AO_HAVE_short_fetch_and_add1_write
#endif
#if defined(AO_HAVE_short_fetch_and_add_read) &&\
    !defined(AO_HAVE_short_fetch_and_add1_read)
#  define AO_short_fetch_and_add1_read(addr) \
	AO_short_fetch_and_add_read(addr,1)
#  define AO_HAVE_short_fetch_and_add1_read
#endif
#if defined(AO_HAVE_short_fetch_and_add_release_write) &&\
    !defined(AO_HAVE_short_fetch_and_add1_release_write)
#  define AO_short_fetch_and_add1_release_write(addr) \
	AO_short_fetch_and_add_release_write(addr,1)
#  define AO_HAVE_short_fetch_and_add1_release_write
#endif
#if defined(AO_HAVE_short_fetch_and_add_acquire_read) &&\
    !defined(AO_HAVE_short_fetch_and_add1_acquire_read)
#  define AO_short_fetch_and_add1_acquire_read(addr) \
	AO_short_fetch_and_add_acquire_read(addr,1)
#  define AO_HAVE_short_fetch_and_add1_acquire_read
#endif
#if defined(AO_HAVE_short_fetch_and_add) &&\
    !defined(AO_HAVE_short_fetch_and_add1)
#  define AO_short_fetch_and_add1(addr) \
	AO_short_fetch_and_add(addr,1)
#  define AO_HAVE_short_fetch_and_add1
#endif

#if defined(AO_HAVE_short_fetch_and_add1_full)
#  if !defined(AO_HAVE_short_fetch_and_add1_release)
#    define AO_short_fetch_and_add1_release(addr) \
  	 AO_short_fetch_and_add1_full(addr)
#    define AO_HAVE_short_fetch_and_add1_release
#  endif
#  if !defined(AO_HAVE_short_fetch_and_add1_acquire)
#    define AO_short_fetch_and_add1_acquire(addr) \
  	 AO_short_fetch_and_add1_full(addr)
#    define AO_HAVE_short_fetch_and_add1_acquire
#  endif
#  if !defined(AO_HAVE_short_fetch_and_add1_write)
#    define AO_short_fetch_and_add1_write(addr) \
  	 AO_short_fetch_and_add1_full(addr)
#    define AO_HAVE_short_fetch_and_add1_write
#  endif
#  if !defined(AO_HAVE_short_fetch_and_add1_read)
#    define AO_short_fetch_and_add1_read(addr) \
  	 AO_short_fetch_and_add1_full(addr)
#    define AO_HAVE_short_fetch_and_add1_read
#  endif
#endif /* AO_HAVE_short_fetch_and_add1_full */

#if !defined(AO_HAVE_short_fetch_and_add1) && \
    defined(AO_HAVE_short_fetch_and_add1_release)
#  define AO_short_fetch_and_add1(addr) \
  	AO_short_fetch_and_add1_release(addr)
#  define AO_HAVE_short_fetch_and_add1
#endif
#if !defined(AO_HAVE_short_fetch_and_add1) && \
    defined(AO_HAVE_short_fetch_and_add1_acquire)
#  define AO_short_fetch_and_add1(addr) \
  	AO_short_fetch_and_add1_acquire(addr)
#  define AO_HAVE_short_fetch_and_add1
#endif
#if !defined(AO_HAVE_short_fetch_and_add1) && \
    defined(AO_HAVE_short_fetch_and_add1_write)
#  define AO_short_fetch_and_add1(addr) \
  	AO_short_fetch_and_add1_write(addr)
#  define AO_HAVE_short_fetch_and_add1
#endif
#if !defined(AO_HAVE_short_fetch_and_add1) && \
    defined(AO_HAVE_short_fetch_and_add1_read)
#  define AO_short_fetch_and_add1(addr) \
  	AO_short_fetch_and_add1_read(addr)
#  define AO_HAVE_short_fetch_and_add1
#endif

#if defined(AO_HAVE_short_fetch_and_add1_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_short_fetch_and_add1_full)
#  define AO_short_fetch_and_add1_full(addr) \
  	(AO_nop_full(), AO_short_fetch_and_add1_acquire(addr))
#  define AO_HAVE_short_fetch_and_add1_full
#endif

#if !defined(AO_HAVE_short_fetch_and_add1_release_write) && \
    defined(AO_HAVE_short_fetch_and_add1_write)
#  define AO_short_fetch_and_add1_release_write(addr) \
  	AO_short_fetch_and_add1_write(addr)
#  define AO_HAVE_short_fetch_and_add1_release_write
#endif
#if !defined(AO_HAVE_short_fetch_and_add1_release_write) && \
    defined(AO_HAVE_short_fetch_and_add1_release)
#  define AO_short_fetch_and_add1_release_write(addr) \
  	AO_short_fetch_and_add1_release(addr)
#  define AO_HAVE_short_fetch_and_add1_release_write
#endif
#if !defined(AO_HAVE_short_fetch_and_add1_acquire_read) && \
    defined(AO_HAVE_short_fetch_and_add1_read)
#  define AO_short_fetch_and_add1_acquire_read(addr) \
  	AO_short_fetch_and_add1_read(addr)
#  define AO_HAVE_short_fetch_and_add1_acquire_read
#endif
#if !defined(AO_HAVE_short_fetch_and_add1_acquire_read) && \
    defined(AO_HAVE_short_fetch_and_add1_acquire)
#  define AO_short_fetch_and_add1_acquire_read(addr) \
  	AO_short_fetch_and_add1_acquire(addr)
#  define AO_HAVE_short_fetch_and_add1_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_short_fetch_and_add1_acquire_read)
#    define AO_short_fetch_and_add1_dd_acquire_read(addr) \
	AO_short_fetch_and_add1_acquire_read(addr)
#    define AO_HAVE_short_fetch_and_add1_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_short_fetch_and_add1)
#    define AO_short_fetch_and_add1_dd_acquire_read(addr) \
	AO_short_fetch_and_add1(addr)
#    define AO_HAVE_short_fetch_and_add1_dd_acquire_read
#  endif
#endif

/* short_fetch_and_sub1 */

#if defined(AO_HAVE_short_fetch_and_add_full) &&\
    !defined(AO_HAVE_short_fetch_and_sub1_full)
#  define AO_short_fetch_and_sub1_full(addr) \
	AO_short_fetch_and_add_full(addr,(unsigned short)(-1))
#  define AO_HAVE_short_fetch_and_sub1_full
#endif
#if defined(AO_HAVE_short_fetch_and_add_release) &&\
    !defined(AO_HAVE_short_fetch_and_sub1_release)
#  define AO_short_fetch_and_sub1_release(addr) \
	AO_short_fetch_and_add_release(addr,(unsigned short)(-1))
#  define AO_HAVE_short_fetch_and_sub1_release
#endif
#if defined(AO_HAVE_short_fetch_and_add_acquire) &&\
    !defined(AO_HAVE_short_fetch_and_sub1_acquire)
#  define AO_short_fetch_and_sub1_acquire(addr) \
	AO_short_fetch_and_add_acquire(addr,(unsigned short)(-1))
#  define AO_HAVE_short_fetch_and_sub1_acquire
#endif
#if defined(AO_HAVE_short_fetch_and_add_write) &&\
    !defined(AO_HAVE_short_fetch_and_sub1_write)
#  define AO_short_fetch_and_sub1_write(addr) \
	AO_short_fetch_and_add_write(addr,(unsigned short)(-1))
#  define AO_HAVE_short_fetch_and_sub1_write
#endif
#if defined(AO_HAVE_short_fetch_and_add_read) &&\
    !defined(AO_HAVE_short_fetch_and_sub1_read)
#  define AO_short_fetch_and_sub1_read(addr) \
	AO_short_fetch_and_add_read(addr,(unsigned short)(-1))
#  define AO_HAVE_short_fetch_and_sub1_read
#endif
#if defined(AO_HAVE_short_fetch_and_add_release_write) &&\
    !defined(AO_HAVE_short_fetch_and_sub1_release_write)
#  define AO_short_fetch_and_sub1_release_write(addr) \
	AO_short_fetch_and_add_release_write(addr,(unsigned short)(-1))
#  define AO_HAVE_short_fetch_and_sub1_release_write
#endif
#if defined(AO_HAVE_short_fetch_and_add_acquire_read) &&\
    !defined(AO_HAVE_short_fetch_and_sub1_acquire_read)
#  define AO_short_fetch_and_sub1_acquire_read(addr) \
	AO_short_fetch_and_add_acquire_read(addr,(unsigned short)(-1))
#  define AO_HAVE_short_fetch_and_sub1_acquire_read
#endif
#if defined(AO_HAVE_short_fetch_and_add) &&\
    !defined(AO_HAVE_short_fetch_and_sub1)
#  define AO_short_fetch_and_sub1(addr) \
	AO_short_fetch_and_add(addr,(unsigned short)(-1))
#  define AO_HAVE_short_fetch_and_sub1
#endif

#if defined(AO_HAVE_short_fetch_and_sub1_full)
#  if !defined(AO_HAVE_short_fetch_and_sub1_release)
#    define AO_short_fetch_and_sub1_release(addr) \
  	 AO_short_fetch_and_sub1_full(addr)
#    define AO_HAVE_short_fetch_and_sub1_release
#  endif
#  if !defined(AO_HAVE_short_fetch_and_sub1_acquire)
#    define AO_short_fetch_and_sub1_acquire(addr) \
  	 AO_short_fetch_and_sub1_full(addr)
#    define AO_HAVE_short_fetch_and_sub1_acquire
#  endif
#  if !defined(AO_HAVE_short_fetch_and_sub1_write)
#    define AO_short_fetch_and_sub1_write(addr) \
  	 AO_short_fetch_and_sub1_full(addr)
#    define AO_HAVE_short_fetch_and_sub1_write
#  endif
#  if !defined(AO_HAVE_short_fetch_and_sub1_read)
#    define AO_short_fetch_and_sub1_read(addr) \
  	 AO_short_fetch_and_sub1_full(addr)
#    define AO_HAVE_short_fetch_and_sub1_read
#  endif
#endif /* AO_HAVE_short_fetch_and_sub1_full */

#if !defined(AO_HAVE_short_fetch_and_sub1) && \
    defined(AO_HAVE_short_fetch_and_sub1_release)
#  define AO_short_fetch_and_sub1(addr) \
  	AO_short_fetch_and_sub1_release(addr)
#  define AO_HAVE_short_fetch_and_sub1
#endif
#if !defined(AO_HAVE_short_fetch_and_sub1) && \
    defined(AO_HAVE_short_fetch_and_sub1_acquire)
#  define AO_short_fetch_and_sub1(addr) \
  	AO_short_fetch_and_sub1_acquire(addr)
#  define AO_HAVE_short_fetch_and_sub1
#endif
#if !defined(AO_HAVE_short_fetch_and_sub1) && \
    defined(AO_HAVE_short_fetch_and_sub1_write)
#  define AO_short_fetch_and_sub1(addr) \
  	AO_short_fetch_and_sub1_write(addr)
#  define AO_HAVE_short_fetch_and_sub1
#endif
#if !defined(AO_HAVE_short_fetch_and_sub1) && \
    defined(AO_HAVE_short_fetch_and_sub1_read)
#  define AO_short_fetch_and_sub1(addr) \
  	AO_short_fetch_and_sub1_read(addr)
#  define AO_HAVE_short_fetch_and_sub1
#endif

#if defined(AO_HAVE_short_fetch_and_sub1_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_short_fetch_and_sub1_full)
#  define AO_short_fetch_and_sub1_full(addr) \
  	(AO_nop_full(), AO_short_fetch_and_sub1_acquire(addr))
#  define AO_HAVE_short_fetch_and_sub1_full
#endif

#if !defined(AO_HAVE_short_fetch_and_sub1_release_write) && \
    defined(AO_HAVE_short_fetch_and_sub1_write)
#  define AO_short_fetch_and_sub1_release_write(addr) \
  	AO_short_fetch_and_sub1_write(addr)
#  define AO_HAVE_short_fetch_and_sub1_release_write
#endif
#if !defined(AO_HAVE_short_fetch_and_sub1_release_write) && \
    defined(AO_HAVE_short_fetch_and_sub1_release)
#  define AO_short_fetch_and_sub1_release_write(addr) \
  	AO_short_fetch_and_sub1_release(addr)
#  define AO_HAVE_short_fetch_and_sub1_release_write
#endif
#if !defined(AO_HAVE_short_fetch_and_sub1_acquire_read) && \
    defined(AO_HAVE_short_fetch_and_sub1_read)
#  define AO_short_fetch_and_sub1_acquire_read(addr) \
  	AO_short_fetch_and_sub1_read(addr)
#  define AO_HAVE_short_fetch_and_sub1_acquire_read
#endif
#if !defined(AO_HAVE_short_fetch_and_sub1_acquire_read) && \
    defined(AO_HAVE_short_fetch_and_sub1_acquire)
#  define AO_short_fetch_and_sub1_acquire_read(addr) \
  	AO_short_fetch_and_sub1_acquire(addr)
#  define AO_HAVE_short_fetch_and_sub1_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_short_fetch_and_sub1_acquire_read)
#    define AO_short_fetch_and_sub1_dd_acquire_read(addr) \
	AO_short_fetch_and_sub1_acquire_read(addr)
#    define AO_HAVE_short_fetch_and_sub1_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_short_fetch_and_sub1)
#    define AO_short_fetch_and_sub1_dd_acquire_read(addr) \
	AO_short_fetch_and_sub1(addr)
#    define AO_HAVE_short_fetch_and_sub1_dd_acquire_read
#  endif
#endif

/* int_load */
#if defined(AO_HAVE_int_load_acquire) && !defined(AO_HAVE_int_load)
#  define AO_int_load(addr) AO_int_load_acquire(addr)
#  define AO_HAVE_int_load
#endif

#if defined(AO_HAVE_int_load_full) && !defined(AO_HAVE_int_load_acquire)
#  define AO_int_load_acquire(addr) AO_int_load_full(addr)
#  define AO_HAVE_int_load_acquire
#endif

#if defined(AO_HAVE_int_load_full) && !defined(AO_HAVE_int_load_read)
#  define AO_int_load_read(addr) AO_int_load_full(addr)
#  define AO_HAVE_int_load_read
#endif

#if !defined(AO_HAVE_int_load_acquire_read) && defined(AO_HAVE_int_load_acquire)
#  define AO_int_load_acquire_read(addr) AO_int_load_acquire(addr)
#  define AO_HAVE_int_load_acquire_read
#endif

#if defined(AO_HAVE_int_load) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_int_load_acquire)
   AO_INLINE unsigned int
   AO_int_load_acquire(volatile unsigned int *addr)
   {
     unsigned int result = AO_int_load(addr);
     /* Acquire barrier would be useless, since the load could be delayed  */
     /* beyond it.							   */
     AO_nop_full();
     return result;
   }
#  define AO_HAVE_int_load_acquire
#endif

#if defined(AO_HAVE_int_load) && defined(AO_HAVE_nop_read) && \
    !defined(AO_HAVE_int_load_read)
   AO_INLINE unsigned int
   AO_int_load_read(volatile unsigned int *addr)
   {
     unsigned int result = AO_int_load(addr);
     /* Acquire barrier would be useless, since the load could be delayed  */
     /* beyond it.							   */
     AO_nop_read();
     return result;
   }
#  define AO_HAVE_int_load_read
#endif

#if defined(AO_HAVE_int_load_acquire) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_int_load_full)
#  define AO_int_load_full(addr) (AO_nop_full(), AO_int_load_acquire(addr))
#  define AO_HAVE_int_load_full
#endif
 
#if !defined(AO_HAVE_int_load_acquire_read) && defined(AO_HAVE_int_load_read)
#  define AO_int_load_acquire_read(addr) AO_int_load_read(addr)
#  define AO_HAVE_int_load_acquire_read
#endif

#if defined(AO_HAVE_int_load_acquire_read) && !defined(AO_HAVE_int_load)
#  define AO_int_load(addr) AO_int_load_acquire_read(addr)
#  define AO_HAVE_int_load
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_int_load_acquire_read)
#    define AO_int_load_dd_acquire_read(addr) \
	AO_int_load_acquire_read(addr)
#    define AO_HAVE_int_load_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_int_load)
#    define AO_int_load_dd_acquire_read(addr) \
	AO_int_load(addr)
#    define AO_HAVE_int_load_dd_acquire_read
#  endif
#endif


/* int_store */

#if defined(AO_HAVE_int_store_release) && !defined(AO_HAVE_int_store)
#  define AO_int_store(addr, val) AO_int_store_release(addr,val)
#  define AO_HAVE_int_store
#endif

#if defined(AO_HAVE_int_store_full) && !defined(AO_HAVE_int_store_release)
#  define AO_int_store_release(addr,val) AO_int_store_full(addr,val)
#  define AO_HAVE_int_store_release
#endif

#if defined(AO_HAVE_int_store_full) && !defined(AO_HAVE_int_store_write)
#  define AO_int_store_write(addr,val) AO_int_store_full(addr,val)
#  define AO_HAVE_int_store_write
#endif

#if defined(AO_HAVE_int_store_release) && \
	!defined(AO_HAVE_int_store_release_write)
#  define AO_int_store_release_write(addr, val) \
	AO_int_store_release(addr,val)
#  define AO_HAVE_int_store_release_write
#endif

#if defined(AO_HAVE_int_store_write) && !defined(AO_HAVE_int_store)
#  define AO_int_store(addr, val) AO_int_store_write(addr,val)
#  define AO_HAVE_int_store
#endif

#if defined(AO_HAVE_int_store) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_int_store_release)
#  define AO_int_store_release(addr,val) \
	(AO_nop_full(), AO_int_store(addr,val))
#  define AO_HAVE_int_store_release
#endif

#if defined(AO_HAVE_nop_write) && defined(AO_HAVE_int_store) && \
     !defined(AO_HAVE_int_store_write)
#  define AO_int_store_write(addr, val) \
	(AO_nop_write(), AO_int_store(addr,val))
#  define AO_HAVE_int_store_write
#endif

#if defined(AO_HAVE_int_store_write) && \
     !defined(AO_HAVE_int_store_release_write)
#  define AO_int_store_release_write(addr, val) AO_int_store_write(addr,val)
#  define AO_HAVE_int_store_release_write
#endif

#if defined(AO_HAVE_int_store_release) && defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_int_store_full)
#  define AO_int_store_full(addr, val) \
	(AO_int_store_release(addr, val), AO_nop_full())
#  define AO_HAVE_int_store_full
#endif


/* int_fetch_and_add */
#if defined(AO_HAVE_int_compare_and_swap_full) && \
    !defined(AO_HAVE_int_fetch_and_add_full)
   AO_INLINE AO_t
   AO_int_fetch_and_add_full(volatile unsigned int *addr,
   			       unsigned int incr)
   {
     unsigned int old;
     do
       {
         old = *addr;
       }
     while (!AO_int_compare_and_swap_full(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_int_fetch_and_add_full
#endif

#if defined(AO_HAVE_int_compare_and_swap_acquire) && \
    !defined(AO_HAVE_int_fetch_and_add_acquire)
   AO_INLINE AO_t
   AO_int_fetch_and_add_acquire(volatile unsigned int *addr,
   				  unsigned int incr)
   {
     unsigned int old;
     do
       {
         old = *addr;
       }
     while (!AO_int_compare_and_swap_acquire(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_int_fetch_and_add_acquire
#endif

#if defined(AO_HAVE_int_compare_and_swap_release) && \
    !defined(AO_HAVE_int_fetch_and_add_release)
   AO_INLINE AO_t
   AO_int_fetch_and_add_release(volatile unsigned int *addr,
   				  unsigned int incr)
   {
     unsigned int old;
     do
       {
         old = *addr;
       }
     while (!AO_int_compare_and_swap_release(addr, old, old+incr));
     return old;
   }
#  define AO_HAVE_int_fetch_and_add_release
#endif

#if defined(AO_HAVE_int_fetch_and_add_full)
#  if !defined(AO_HAVE_int_fetch_and_add_release)
#    define AO_int_fetch_and_add_release(addr, val) \
  	 AO_int_fetch_and_add_full(addr, val)
#    define AO_HAVE_int_fetch_and_add_release
#  endif
#  if !defined(AO_HAVE_int_fetch_and_add_acquire)
#    define AO_int_fetch_and_add_acquire(addr, val) \
  	 AO_int_fetch_and_add_full(addr, val)
#    define AO_HAVE_int_fetch_and_add_acquire
#  endif
#  if !defined(AO_HAVE_int_fetch_and_add_write)
#    define AO_int_fetch_and_add_write(addr, val) \
  	 AO_int_fetch_and_add_full(addr, val)
#    define AO_HAVE_int_fetch_and_add_write
#  endif
#  if !defined(AO_HAVE_int_fetch_and_add_read)
#    define AO_int_fetch_and_add_read(addr, val) \
  	 AO_int_fetch_and_add_full(addr, val)
#    define AO_HAVE_int_fetch_and_add_read
#  endif
#endif /* AO_HAVE_int_fetch_and_add_full */

#if !defined(AO_HAVE_int_fetch_and_add) && \
    defined(AO_HAVE_int_fetch_and_add_release)
#  define AO_int_fetch_and_add(addr, val) \
  	AO_int_fetch_and_add_release(addr, val)
#  define AO_HAVE_int_fetch_and_add
#endif
#if !defined(AO_HAVE_int_fetch_and_add) && \
    defined(AO_HAVE_int_fetch_and_add_acquire)
#  define AO_int_fetch_and_add(addr, val) \
  	AO_int_fetch_and_add_acquire(addr, val)
#  define AO_HAVE_int_fetch_and_add
#endif
#if !defined(AO_HAVE_int_fetch_and_add) && \
    defined(AO_HAVE_int_fetch_and_add_write)
#  define AO_int_fetch_and_add(addr, val) \
  	AO_int_fetch_and_add_write(addr, val)
#  define AO_HAVE_int_fetch_and_add
#endif
#if !defined(AO_HAVE_int_fetch_and_add) && \
    defined(AO_HAVE_int_fetch_and_add_read)
#  define AO_int_fetch_and_add(addr, val) \
  	AO_int_fetch_and_add_read(addr, val)
#  define AO_HAVE_int_fetch_and_add
#endif

#if defined(AO_HAVE_int_fetch_and_add_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_int_fetch_and_add_full)
#  define AO_int_fetch_and_add_full(addr, val) \
  	(AO_nop_full(), AO_int_fetch_and_add_acquire(addr, val))
#endif

#if !defined(AO_HAVE_int_fetch_and_add_release_write) && \
    defined(AO_HAVE_int_fetch_and_add_write)
#  define AO_int_fetch_and_add_release_write(addr, val) \
  	AO_int_fetch_and_add_write(addr, val)
#  define AO_HAVE_int_fetch_and_add_release_write
#endif
#if !defined(AO_HAVE_int_fetch_and_add_release_write) && \
    defined(AO_HAVE_int_fetch_and_add_release)
#  define AO_int_fetch_and_add_release_write(addr, val) \
  	AO_int_fetch_and_add_release(addr, val)
#  define AO_HAVE_int_fetch_and_add_release_write
#endif
#if !defined(AO_HAVE_int_fetch_and_add_acquire_read) && \
    defined(AO_HAVE_int_fetch_and_add_read)
#  define AO_int_fetch_and_add_acquire_read(addr, val) \
  	AO_int_fetch_and_add_read(addr, val)
#  define AO_HAVE_int_fetch_and_add_acquire_read
#endif
#if !defined(AO_HAVE_int_fetch_and_add_acquire_read) && \
    defined(AO_HAVE_int_fetch_and_add_acquire)
#  define AO_int_fetch_and_add_acquire_read(addr, val) \
  	AO_int_fetch_and_add_acquire(addr, val)
#  define AO_HAVE_int_fetch_and_add_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_int_fetch_and_add_acquire_read)
#    define AO_int_fetch_and_add_dd_acquire_read(addr, val) \
	AO_int_fetch_and_add_acquire_read(addr, val)
#    define AO_HAVE_int_fetch_and_add_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_int_fetch_and_add)
#    define AO_int_fetch_and_add_dd_acquire_read(addr, val) \
	AO_int_fetch_and_add(addr, val)
#    define AO_HAVE_int_fetch_and_add_dd_acquire_read
#  endif
#endif
  
/* int_fetch_and_add1 */

#if defined(AO_HAVE_int_fetch_and_add_full) &&\
    !defined(AO_HAVE_int_fetch_and_add1_full)
#  define AO_int_fetch_and_add1_full(addr) \
	AO_int_fetch_and_add_full(addr,1)
#  define AO_HAVE_int_fetch_and_add1_full
#endif
#if defined(AO_HAVE_int_fetch_and_add_release) &&\
    !defined(AO_HAVE_int_fetch_and_add1_release)
#  define AO_int_fetch_and_add1_release(addr) \
	AO_int_fetch_and_add_release(addr,1)
#  define AO_HAVE_int_fetch_and_add1_release
#endif
#if defined(AO_HAVE_int_fetch_and_add_acquire) &&\
    !defined(AO_HAVE_int_fetch_and_add1_acquire)
#  define AO_int_fetch_and_add1_acquire(addr) \
	AO_int_fetch_and_add_acquire(addr,1)
#  define AO_HAVE_int_fetch_and_add1_acquire
#endif
#if defined(AO_HAVE_int_fetch_and_add_write) &&\
    !defined(AO_HAVE_int_fetch_and_add1_write)
#  define AO_int_fetch_and_add1_write(addr) \
	AO_int_fetch_and_add_write(addr,1)
#  define AO_HAVE_int_fetch_and_add1_write
#endif
#if defined(AO_HAVE_int_fetch_and_add_read) &&\
    !defined(AO_HAVE_int_fetch_and_add1_read)
#  define AO_int_fetch_and_add1_read(addr) \
	AO_int_fetch_and_add_read(addr,1)
#  define AO_HAVE_int_fetch_and_add1_read
#endif
#if defined(AO_HAVE_int_fetch_and_add_release_write) &&\
    !defined(AO_HAVE_int_fetch_and_add1_release_write)
#  define AO_int_fetch_and_add1_release_write(addr) \
	AO_int_fetch_and_add_release_write(addr,1)
#  define AO_HAVE_int_fetch_and_add1_release_write
#endif
#if defined(AO_HAVE_int_fetch_and_add_acquire_read) &&\
    !defined(AO_HAVE_int_fetch_and_add1_acquire_read)
#  define AO_int_fetch_and_add1_acquire_read(addr) \
	AO_int_fetch_and_add_acquire_read(addr,1)
#  define AO_HAVE_int_fetch_and_add1_acquire_read
#endif
#if defined(AO_HAVE_int_fetch_and_add) &&\
    !defined(AO_HAVE_int_fetch_and_add1)
#  define AO_int_fetch_and_add1(addr) \
	AO_int_fetch_and_add(addr,1)
#  define AO_HAVE_int_fetch_and_add1
#endif

#if defined(AO_HAVE_int_fetch_and_add1_full)
#  if !defined(AO_HAVE_int_fetch_and_add1_release)
#    define AO_int_fetch_and_add1_release(addr) \
  	 AO_int_fetch_and_add1_full(addr)
#    define AO_HAVE_int_fetch_and_add1_release
#  endif
#  if !defined(AO_HAVE_int_fetch_and_add1_acquire)
#    define AO_int_fetch_and_add1_acquire(addr) \
  	 AO_int_fetch_and_add1_full(addr)
#    define AO_HAVE_int_fetch_and_add1_acquire
#  endif
#  if !defined(AO_HAVE_int_fetch_and_add1_write)
#    define AO_int_fetch_and_add1_write(addr) \
  	 AO_int_fetch_and_add1_full(addr)
#    define AO_HAVE_int_fetch_and_add1_write
#  endif
#  if !defined(AO_HAVE_int_fetch_and_add1_read)
#    define AO_int_fetch_and_add1_read(addr) \
  	 AO_int_fetch_and_add1_full(addr)
#    define AO_HAVE_int_fetch_and_add1_read
#  endif
#endif /* AO_HAVE_int_fetch_and_add1_full */

#if !defined(AO_HAVE_int_fetch_and_add1) && \
    defined(AO_HAVE_int_fetch_and_add1_release)
#  define AO_int_fetch_and_add1(addr) \
  	AO_int_fetch_and_add1_release(addr)
#  define AO_HAVE_int_fetch_and_add1
#endif
#if !defined(AO_HAVE_int_fetch_and_add1) && \
    defined(AO_HAVE_int_fetch_and_add1_acquire)
#  define AO_int_fetch_and_add1(addr) \
  	AO_int_fetch_and_add1_acquire(addr)
#  define AO_HAVE_int_fetch_and_add1
#endif
#if !defined(AO_HAVE_int_fetch_and_add1) && \
    defined(AO_HAVE_int_fetch_and_add1_write)
#  define AO_int_fetch_and_add1(addr) \
  	AO_int_fetch_and_add1_write(addr)
#  define AO_HAVE_int_fetch_and_add1
#endif
#if !defined(AO_HAVE_int_fetch_and_add1) && \
    defined(AO_HAVE_int_fetch_and_add1_read)
#  define AO_int_fetch_and_add1(addr) \
  	AO_int_fetch_and_add1_read(addr)
#  define AO_HAVE_int_fetch_and_add1
#endif

#if defined(AO_HAVE_int_fetch_and_add1_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_int_fetch_and_add1_full)
#  define AO_int_fetch_and_add1_full(addr) \
  	(AO_nop_full(), AO_int_fetch_and_add1_acquire(addr))
#  define AO_HAVE_int_fetch_and_add1_full
#endif

#if !defined(AO_HAVE_int_fetch_and_add1_release_write) && \
    defined(AO_HAVE_int_fetch_and_add1_write)
#  define AO_int_fetch_and_add1_release_write(addr) \
  	AO_int_fetch_and_add1_write(addr)
#  define AO_HAVE_int_fetch_and_add1_release_write
#endif
#if !defined(AO_HAVE_int_fetch_and_add1_release_write) && \
    defined(AO_HAVE_int_fetch_and_add1_release)
#  define AO_int_fetch_and_add1_release_write(addr) \
  	AO_int_fetch_and_add1_release(addr)
#  define AO_HAVE_int_fetch_and_add1_release_write
#endif
#if !defined(AO_HAVE_int_fetch_and_add1_acquire_read) && \
    defined(AO_HAVE_int_fetch_and_add1_read)
#  define AO_int_fetch_and_add1_acquire_read(addr) \
  	AO_int_fetch_and_add1_read(addr)
#  define AO_HAVE_int_fetch_and_add1_acquire_read
#endif
#if !defined(AO_HAVE_int_fetch_and_add1_acquire_read) && \
    defined(AO_HAVE_int_fetch_and_add1_acquire)
#  define AO_int_fetch_and_add1_acquire_read(addr) \
  	AO_int_fetch_and_add1_acquire(addr)
#  define AO_HAVE_int_fetch_and_add1_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_int_fetch_and_add1_acquire_read)
#    define AO_int_fetch_and_add1_dd_acquire_read(addr) \
	AO_int_fetch_and_add1_acquire_read(addr)
#    define AO_HAVE_int_fetch_and_add1_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_int_fetch_and_add1)
#    define AO_int_fetch_and_add1_dd_acquire_read(addr) \
	AO_int_fetch_and_add1(addr)
#    define AO_HAVE_int_fetch_and_add1_dd_acquire_read
#  endif
#endif

/* int_fetch_and_sub1 */

#if defined(AO_HAVE_int_fetch_and_add_full) &&\
    !defined(AO_HAVE_int_fetch_and_sub1_full)
#  define AO_int_fetch_and_sub1_full(addr) \
	AO_int_fetch_and_add_full(addr,(unsigned int)(-1))
#  define AO_HAVE_int_fetch_and_sub1_full
#endif
#if defined(AO_HAVE_int_fetch_and_add_release) &&\
    !defined(AO_HAVE_int_fetch_and_sub1_release)
#  define AO_int_fetch_and_sub1_release(addr) \
	AO_int_fetch_and_add_release(addr,(unsigned int)(-1))
#  define AO_HAVE_int_fetch_and_sub1_release
#endif
#if defined(AO_HAVE_int_fetch_and_add_acquire) &&\
    !defined(AO_HAVE_int_fetch_and_sub1_acquire)
#  define AO_int_fetch_and_sub1_acquire(addr) \
	AO_int_fetch_and_add_acquire(addr,(unsigned int)(-1))
#  define AO_HAVE_int_fetch_and_sub1_acquire
#endif
#if defined(AO_HAVE_int_fetch_and_add_write) &&\
    !defined(AO_HAVE_int_fetch_and_sub1_write)
#  define AO_int_fetch_and_sub1_write(addr) \
	AO_int_fetch_and_add_write(addr,(unsigned int)(-1))
#  define AO_HAVE_int_fetch_and_sub1_write
#endif
#if defined(AO_HAVE_int_fetch_and_add_read) &&\
    !defined(AO_HAVE_int_fetch_and_sub1_read)
#  define AO_int_fetch_and_sub1_read(addr) \
	AO_int_fetch_and_add_read(addr,(unsigned int)(-1))
#  define AO_HAVE_int_fetch_and_sub1_read
#endif
#if defined(AO_HAVE_int_fetch_and_add_release_write) &&\
    !defined(AO_HAVE_int_fetch_and_sub1_release_write)
#  define AO_int_fetch_and_sub1_release_write(addr) \
	AO_int_fetch_and_add_release_write(addr,(unsigned int)(-1))
#  define AO_HAVE_int_fetch_and_sub1_release_write
#endif
#if defined(AO_HAVE_int_fetch_and_add_acquire_read) &&\
    !defined(AO_HAVE_int_fetch_and_sub1_acquire_read)
#  define AO_int_fetch_and_sub1_acquire_read(addr) \
	AO_int_fetch_and_add_acquire_read(addr,(unsigned int)(-1))
#  define AO_HAVE_int_fetch_and_sub1_acquire_read
#endif
#if defined(AO_HAVE_int_fetch_and_add) &&\
    !defined(AO_HAVE_int_fetch_and_sub1)
#  define AO_int_fetch_and_sub1(addr) \
	AO_int_fetch_and_add(addr,(unsigned int)(-1))
#  define AO_HAVE_int_fetch_and_sub1
#endif

#if defined(AO_HAVE_int_fetch_and_sub1_full)
#  if !defined(AO_HAVE_int_fetch_and_sub1_release)
#    define AO_int_fetch_and_sub1_release(addr) \
  	 AO_int_fetch_and_sub1_full(addr)
#    define AO_HAVE_int_fetch_and_sub1_release
#  endif
#  if !defined(AO_HAVE_int_fetch_and_sub1_acquire)
#    define AO_int_fetch_and_sub1_acquire(addr) \
  	 AO_int_fetch_and_sub1_full(addr)
#    define AO_HAVE_int_fetch_and_sub1_acquire
#  endif
#  if !defined(AO_HAVE_int_fetch_and_sub1_write)
#    define AO_int_fetch_and_sub1_write(addr) \
  	 AO_int_fetch_and_sub1_full(addr)
#    define AO_HAVE_int_fetch_and_sub1_write
#  endif
#  if !defined(AO_HAVE_int_fetch_and_sub1_read)
#    define AO_int_fetch_and_sub1_read(addr) \
  	 AO_int_fetch_and_sub1_full(addr)
#    define AO_HAVE_int_fetch_and_sub1_read
#  endif
#endif /* AO_HAVE_int_fetch_and_sub1_full */

#if !defined(AO_HAVE_int_fetch_and_sub1) && \
    defined(AO_HAVE_int_fetch_and_sub1_release)
#  define AO_int_fetch_and_sub1(addr) \
  	AO_int_fetch_and_sub1_release(addr)
#  define AO_HAVE_int_fetch_and_sub1
#endif
#if !defined(AO_HAVE_int_fetch_and_sub1) && \
    defined(AO_HAVE_int_fetch_and_sub1_acquire)
#  define AO_int_fetch_and_sub1(addr) \
  	AO_int_fetch_and_sub1_acquire(addr)
#  define AO_HAVE_int_fetch_and_sub1
#endif
#if !defined(AO_HAVE_int_fetch_and_sub1) && \
    defined(AO_HAVE_int_fetch_and_sub1_write)
#  define AO_int_fetch_and_sub1(addr) \
  	AO_int_fetch_and_sub1_write(addr)
#  define AO_HAVE_int_fetch_and_sub1
#endif
#if !defined(AO_HAVE_int_fetch_and_sub1) && \
    defined(AO_HAVE_int_fetch_and_sub1_read)
#  define AO_int_fetch_and_sub1(addr) \
  	AO_int_fetch_and_sub1_read(addr)
#  define AO_HAVE_int_fetch_and_sub1
#endif

#if defined(AO_HAVE_int_fetch_and_sub1_acquire) &&\
    defined(AO_HAVE_nop_full) && \
    !defined(AO_HAVE_int_fetch_and_sub1_full)
#  define AO_int_fetch_and_sub1_full(addr) \
  	(AO_nop_full(), AO_int_fetch_and_sub1_acquire(addr))
#  define AO_HAVE_int_fetch_and_sub1_full
#endif

#if !defined(AO_HAVE_int_fetch_and_sub1_release_write) && \
    defined(AO_HAVE_int_fetch_and_sub1_write)
#  define AO_int_fetch_and_sub1_release_write(addr) \
  	AO_int_fetch_and_sub1_write(addr)
#  define AO_HAVE_int_fetch_and_sub1_release_write
#endif
#if !defined(AO_HAVE_int_fetch_and_sub1_release_write) && \
    defined(AO_HAVE_int_fetch_and_sub1_release)
#  define AO_int_fetch_and_sub1_release_write(addr) \
  	AO_int_fetch_and_sub1_release(addr)
#  define AO_HAVE_int_fetch_and_sub1_release_write
#endif
#if !defined(AO_HAVE_int_fetch_and_sub1_acquire_read) && \
    defined(AO_HAVE_int_fetch_and_sub1_read)
#  define AO_int_fetch_and_sub1_acquire_read(addr) \
  	AO_int_fetch_and_sub1_read(addr)
#  define AO_HAVE_int_fetch_and_sub1_acquire_read
#endif
#if !defined(AO_HAVE_int_fetch_and_sub1_acquire_read) && \
    defined(AO_HAVE_int_fetch_and_sub1_acquire)
#  define AO_int_fetch_and_sub1_acquire_read(addr) \
  	AO_int_fetch_and_sub1_acquire(addr)
#  define AO_HAVE_int_fetch_and_sub1_acquire_read
#endif

#ifdef AO_NO_DD_ORDERING
#  if defined(AO_HAVE_int_fetch_and_sub1_acquire_read)
#    define AO_int_fetch_and_sub1_dd_acquire_read(addr) \
	AO_int_fetch_and_sub1_acquire_read(addr)
#    define AO_HAVE_int_fetch_and_sub1_dd_acquire_read
#  endif
#else
#  if defined(AO_HAVE_int_fetch_and_sub1)
#    define AO_int_fetch_and_sub1_dd_acquire_read(addr) \
	AO_int_fetch_and_sub1(addr)
#    define AO_HAVE_int_fetch_and_sub1_dd_acquire_read
#  endif
#endif

