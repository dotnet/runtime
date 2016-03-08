#ifndef __MONO_MINI_ARM_TLS_H__
#define __MONO_MINI_ARM_TLS_H__

/* Fast inlined tls getters/setters */

int mono_fast_get_tls_key (int);
void mono_fast_set_tls_key (int, int);
int mono_fast_get_tls_key2 (int);
void mono_fast_set_tls_key2 (int, int);

/* Fallback tls getters/setters */

int mono_fallback_get_tls_key (int);
void mono_fallback_set_tls_key (int, int);

/* End of thunks */

void mono_fast_get_tls_key_end (void);
void mono_fast_set_tls_key_end (void);
void mono_fast_get_tls_key2_end (void);
void mono_fast_set_tls_key2_end (void);

#if defined(__linux__) && defined(HAVE_KW_THREAD) && defined(__ARM_EABI__)
void* __aeabi_read_tp (void);
#endif

/* Structure that maps a possible  tls implementation to the corresponding thunks */
typedef struct {
	guint32 *expected_code;
	int expected_code_length;
	gboolean check_kernel_helper;
	gpointer get_tls_thunk;
	gpointer get_tls_thunk_end;
	gpointer set_tls_thunk;
	gpointer set_tls_thunk_end;
} MonoTlsImplementation;


static MonoTlsImplementation known_tls_implementations [] = {
#if defined(HAVE_KW_THREAD) && defined(__linux__) && defined(__ARM_EABI__)
	{ (guint32[]) {0x0f70ee1d, 0xbf004770}, 8, FALSE, mono_fast_get_tls_key, mono_fast_get_tls_key_end, mono_fast_set_tls_key, mono_fast_set_tls_key_end },
	{ (guint32[]) {0xe3e00a0f, 0xe240f01f}, 8, TRUE, mono_fast_get_tls_key, mono_fast_get_tls_key_end, mono_fast_set_tls_key, mono_fast_set_tls_key_end }
#elif defined(TARGET_IOS)
	{ (guint32[]) {0x1f70ee1d, 0x0103f021, 0x0020f851, 0xbf004770}, 16, FALSE, mono_fast_get_tls_key, mono_fast_get_tls_key_end, mono_fast_set_tls_key, mono_fast_set_tls_key_end }
#elif defined(TARGET_ANDROID)
	{ (guint32[]) {0xe2403003, 0xe353003c, 0xe92d4010, 0xe1a04000, 0x9a000001, 0xe3a00000, 0xe8bd8010, 0xe3e00a0f, 0xe240101f, 0xe12fff31, 0xe7900104, 0xe8bd8010}, 48, TRUE, mono_fast_get_tls_key, mono_fast_get_tls_key_end, mono_fast_set_tls_key, mono_fast_set_tls_key_end}, /* 1.5 */
	{ (guint32[]) {0xe2402003, 0xe1a03000, 0xe352003c, 0x8a000002, 0xee1d0f70, 0xe7900103, 0xe12fff1e}, 28, FALSE, mono_fast_get_tls_key, mono_fast_get_tls_key_end, mono_fast_set_tls_key, mono_fast_set_tls_key_end}, /* 4.2 */
	{ (guint32[]) {0xe2403007, 0xe3530084, 0x8a000002, 0xee1d1f70, 0xe7910100, 0xe12fff1e, 0xe3a00000, 0xe12fff1e}, 32, FALSE, mono_fast_get_tls_key, mono_fast_get_tls_key_end, mono_fast_set_tls_key, mono_fast_set_tls_key_end}, /* 4.4 */
	{ (guint32[]) {0x2b8c1fc3, 0xee1dd804, 0xf8511f70, 0x47700020, 0x47702000}, 20, FALSE, mono_fast_get_tls_key, mono_fast_get_tls_key_end, mono_fast_set_tls_key, mono_fast_set_tls_key_end}, /* 5.0 */
	{ (guint32[]) {0xb5104b0f, 0xda114298, 0xf020490e, 0xee1d4000, 0x00c24f70, 0xf8514479, 0x68631030, 0xd50707cc, 0x6e54441a, 0xd103428c, 0xbd106e90}, 44, FALSE, mono_fast_get_tls_key2, mono_fast_get_tls_key2_end, mono_fast_set_tls_key2, mono_fast_set_tls_key2_end} /* 6.0 */
#endif
};

static gboolean
known_kernel_helper (void)
{
#ifdef __linux__
	const guint32* kuser_get_tls = (void*)0xffff0fe0; /* linux kernel user helper on arm */
	guint32 expected [] = {0xee1d0f70, 0xe12fff1e};

	/* Expecting mrc + bx lr in the kuser_get_tls kernel helper */
	return memcmp (kuser_get_tls, expected, 8) == 0;
#else
	g_error ("Trying to check linux kernel helper on non linux platform"); 
	return FALSE;
#endif
}

static void
dump_code (guint32 *ptr)
{
	char current_impl [256];
	char hex [16];
	int i;
	guint32 page_mask = ~((guint32)mono_pagesize () - 1);

	current_impl [0] = 0;
	for (i = 0; i < 16; i++) {
		/* Don't risk page fault since we don't know where the code ends */
		if (((guint32)&ptr [i] & page_mask) != ((guint32)ptr & page_mask))
			break;
		sprintf (hex, "0x%x ", ptr [i]);
		strcat (current_impl, hex);
	}

	g_warning (current_impl);
}

static MonoTlsImplementation
mono_arm_get_tls_implementation (void)
{
#ifdef MONO_CROSS_COMPILE
	g_assert_not_reached ();
#else
	/* Discard thumb bit */
#if defined(__linux__) && defined(HAVE_KW_THREAD) && defined(__ARM_EABI__)
	guint32* check_addr = (guint32*) ((guint32)__aeabi_read_tp & 0xfffffffe);
#else
	guint32* check_addr = (guint32*) ((guint32)pthread_getspecific & 0xfffffffe);
#endif
	int i;

	if (!mini_get_debug_options ()->arm_use_fallback_tls) {
		for (i = 0; i < sizeof (known_tls_implementations) / sizeof (MonoTlsImplementation); i++) {
			if (memcmp (check_addr, known_tls_implementations [i].expected_code, known_tls_implementations [i].expected_code_length) == 0) {
				if ((known_tls_implementations [i].check_kernel_helper && known_kernel_helper ()) ||
						!known_tls_implementations [i].check_kernel_helper)
					return known_tls_implementations [i];
			}
		}
	}

	g_warning ("No fast tls on device. Using fallbacks. Current implementation : ");
	dump_code (check_addr);

	return (MonoTlsImplementation) { NULL, 0, FALSE, mono_fallback_get_tls_key, NULL, mono_fallback_set_tls_key, NULL };
#endif
}
#endif
