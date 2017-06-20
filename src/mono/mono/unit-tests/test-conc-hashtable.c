/*
 * test-conc-hashtable.c: Unit test for the concurrent hashtable.
 *
 * Copyright (C) 2014 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#include "utils/mono-threads.h"
#include "utils/mono-conc-hashtable.h"
#include "utils/checked-build.h"
#include "metadata/w32handle.h"

#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <assert.h>

#include <pthread.h>

static int
single_writer_single_reader (void)
{
	mono_mutex_t mutex;
	MonoConcurrentHashTable *h;
	int res = 0;

	mono_os_mutex_init (&mutex);
	h = mono_conc_hashtable_new (NULL, NULL);

	mono_os_mutex_lock (&mutex);
	mono_conc_hashtable_insert (h, GUINT_TO_POINTER (10), GUINT_TO_POINTER (20));
	mono_os_mutex_unlock (&mutex);

	mono_os_mutex_lock (&mutex);
	mono_conc_hashtable_insert (h, GUINT_TO_POINTER (30), GUINT_TO_POINTER (40));
	mono_os_mutex_unlock (&mutex);

	mono_os_mutex_lock (&mutex);
	mono_conc_hashtable_insert (h, GUINT_TO_POINTER (50), GUINT_TO_POINTER (60));
	mono_os_mutex_unlock (&mutex);

	mono_os_mutex_lock (&mutex);
	mono_conc_hashtable_insert (h, GUINT_TO_POINTER (2), GUINT_TO_POINTER (3));
	mono_os_mutex_unlock (&mutex);

	if (mono_conc_hashtable_lookup (h, GUINT_TO_POINTER (30)) != GUINT_TO_POINTER (40))
		res = 1;
	if (mono_conc_hashtable_lookup (h, GUINT_TO_POINTER (10)) != GUINT_TO_POINTER (20))
		res = 2;
	if (mono_conc_hashtable_lookup (h, GUINT_TO_POINTER (2)) != GUINT_TO_POINTER (3))
		res = 3;
	if (mono_conc_hashtable_lookup (h, GUINT_TO_POINTER (50)) != GUINT_TO_POINTER (60))
		res = 4;

	mono_conc_hashtable_destroy (h);
	mono_os_mutex_destroy (&mutex);
	if (res)
		printf ("SERIAL TEST FAILED %d\n", res);
	return res;
}

static MonoConcurrentHashTable *hash;
static mono_mutex_t global_mutex;

static void*
pw_sr_thread (void *arg)
{
	int i, idx = 1000 * GPOINTER_TO_INT (arg);
	mono_thread_info_attach ();

	for (i = 0; i < 1000; ++i) {
		mono_os_mutex_lock (&global_mutex);
		mono_conc_hashtable_insert (hash, GINT_TO_POINTER (i + idx), GINT_TO_POINTER (i + 1));
		mono_os_mutex_unlock (&global_mutex);
	}
	return NULL;
}

static int
parallel_writer_single_reader (void)
{
	pthread_t a,b,c;
	int i, j, res = 0;

	mono_os_mutex_init (&global_mutex);
	hash = mono_conc_hashtable_new (NULL, NULL);

	pthread_create (&a, NULL, pw_sr_thread, GINT_TO_POINTER (1));
	pthread_create (&b, NULL, pw_sr_thread, GINT_TO_POINTER (2));
	pthread_create (&c, NULL, pw_sr_thread, GINT_TO_POINTER (3));

	pthread_join (a, NULL);
	pthread_join (b, NULL);
	pthread_join (c, NULL);

	for (i = 0; i < 1000; ++i) {
		for (j = 1; j < 4; ++j) {
			if (mono_conc_hashtable_lookup (hash, GINT_TO_POINTER (j * 1000 + i)) != GINT_TO_POINTER (i + 1)) {
				res = j + 1;
				goto done;
			}
		}
	}

done:
	mono_conc_hashtable_destroy (hash);
	mono_os_mutex_destroy (&global_mutex);
	if (res)
		printf ("PAR_WRITER_SINGLE_READER TEST FAILED %d\n", res);
	return res;
}


static void*
pr_sw_thread (void *arg)
{
	int i = 0, idx = 100 * GPOINTER_TO_INT (arg);
	mono_thread_info_attach ();

	while (i < 100) {
		gpointer res = mono_conc_hashtable_lookup (hash, GINT_TO_POINTER (i + idx + 1));
		if (!res)
			continue;
		if (res != GINT_TO_POINTER ((i + idx) * 2 + 1))
			return GINT_TO_POINTER (i);
		++i;
	}
	return NULL;
}

static int
single_writer_parallel_reader (void)
{
	pthread_t a,b,c;
	gpointer ra, rb, rc;
	int i, res = 0;
	ra = rb = rc = GINT_TO_POINTER (1);

	mono_os_mutex_init (&global_mutex);
	hash = mono_conc_hashtable_new (NULL, NULL);

	pthread_create (&a, NULL, pr_sw_thread, GINT_TO_POINTER (0));
	pthread_create (&b, NULL, pr_sw_thread, GINT_TO_POINTER (1));
	pthread_create (&c, NULL, pr_sw_thread, GINT_TO_POINTER (2));

	for (i = 0; i < 100; ++i) {
		mono_os_mutex_lock (&global_mutex);
		mono_conc_hashtable_insert (hash, GINT_TO_POINTER (i +   0 + 1), GINT_TO_POINTER ((i +   0) * 2 + 1));
		mono_os_mutex_unlock (&global_mutex);

		mono_os_mutex_lock (&global_mutex);
		mono_conc_hashtable_insert (hash, GINT_TO_POINTER (i + 100 + 1), GINT_TO_POINTER ((i + 100) * 2 + 1));
		mono_os_mutex_unlock (&global_mutex);

		mono_os_mutex_lock (&global_mutex);
		mono_conc_hashtable_insert (hash, GINT_TO_POINTER (i + 200 + 1), GINT_TO_POINTER ((i + 200) * 2 + 1));
		mono_os_mutex_unlock (&global_mutex);
	}

	pthread_join (a, &ra);
	pthread_join (b, &rb);
	pthread_join (c, &rc);
	res = GPOINTER_TO_INT (ra) + GPOINTER_TO_INT (rb) + GPOINTER_TO_INT (rc);

	mono_conc_hashtable_destroy (hash);
	mono_os_mutex_destroy (&global_mutex);
	if (res)
		printf ("SINGLE_WRITER_PAR_READER TEST FAILED %d\n", res);
	return res;
}

int running = 1;

static void*
pw_pr_r_thread (void *arg)
{
	int key, val, i;
	mono_thread_info_attach ();

	/* i will not be incremented as long as running is set to 1, this guarantee that
	   we loop over all the keys at least once after the writer threads have finished */
	for (i = 0; i < 2; i += 1 - running) {
		for (key = 1; key < 3 * 1000 + 1; key++) {
			val = GPOINTER_TO_INT (mono_conc_hashtable_lookup (hash, GINT_TO_POINTER (key)));

			if (!val)
				continue;
			if (key != val)
				return GINT_TO_POINTER (key);
		}
	}
	return NULL;
}

static void*
pw_pr_w_add_thread (void *arg)
{
	int i, idx = 1000 * GPOINTER_TO_INT (arg);

	mono_thread_info_attach ();

	for (i = idx; i < idx + 1000; i++) {
		mono_os_mutex_lock (&global_mutex);
		mono_conc_hashtable_insert (hash, GINT_TO_POINTER (i + 1), GINT_TO_POINTER (i + 1));
		mono_os_mutex_unlock (&global_mutex);
	}
	return NULL;
}

static void*
pw_pr_w_del_thread (void *arg)
{
	int i, idx = 1000 * GPOINTER_TO_INT (arg);

	mono_thread_info_attach ();

	for (i = idx; i < idx + 1000; i++) {
		mono_os_mutex_lock (&global_mutex);
		mono_conc_hashtable_remove (hash, GINT_TO_POINTER (i + 1));
		mono_os_mutex_unlock (&global_mutex);
	}
	return NULL;
}

static int
parallel_writer_parallel_reader (void)
{
	pthread_t wa, wb, wc, ra, rb, rc;
	gpointer a, b, c;
	int res = 0, i;

	srand(time(NULL));

	mono_os_mutex_init (&global_mutex);
	hash = mono_conc_hashtable_new (NULL, NULL);

	for (i = 0; i < 2; i++) {
		running = 1;

		pthread_create (&ra, NULL, pw_pr_r_thread, NULL);
		pthread_create (&rb, NULL, pw_pr_r_thread, NULL);
		pthread_create (&rc, NULL, pw_pr_r_thread, NULL);

		switch (i) {
		case 0:
			pthread_create (&wa, NULL, pw_pr_w_add_thread, GINT_TO_POINTER (0));
			pthread_create (&wb, NULL, pw_pr_w_add_thread, GINT_TO_POINTER (1));
			pthread_create (&wc, NULL, pw_pr_w_add_thread, GINT_TO_POINTER (2));
			break;
		case 1:
			pthread_create (&wa, NULL, pw_pr_w_del_thread, GINT_TO_POINTER (0));
			pthread_create (&wb, NULL, pw_pr_w_del_thread, GINT_TO_POINTER (1));
			pthread_create (&wc, NULL, pw_pr_w_del_thread, GINT_TO_POINTER (2));
			break;
		}

		pthread_join (wa, NULL);
		pthread_join (wb, NULL);
		pthread_join (wc, NULL);

		running = 0;

		pthread_join (ra, &a);
		pthread_join (rb, &b);
		pthread_join (rc, &c);

		res += GPOINTER_TO_INT (a) + GPOINTER_TO_INT (b) + GPOINTER_TO_INT (c);
	}

	if (res)
		printf ("PAR_WRITER_PAR_READER TEST FAILED %d %d %d\n", GPOINTER_TO_INT (a), GPOINTER_TO_INT (b), GPOINTER_TO_INT (c));

	mono_conc_hashtable_destroy (hash);
	mono_os_mutex_destroy (&global_mutex);

	return res;
}

static void G_GNUC_UNUSED
benchmark_conc (void)
{
	MonoConcurrentHashTable *h;
	int i, j;

	h = mono_conc_hashtable_new (NULL, NULL);

	for (i = 1; i < 10 * 1000; ++i) {
		mono_conc_hashtable_insert (h, GUINT_TO_POINTER (i), GUINT_TO_POINTER (i));
	}


	for (j = 0; j < 100000; ++j)
		for (i = 1; i < 10 * 105; ++i)
			mono_conc_hashtable_lookup (h, GUINT_TO_POINTER (i));

	mono_conc_hashtable_destroy (h);
}

static void G_GNUC_UNUSED
benchmark_glib (void)
{
	GHashTable *h;
	int i, j;

	h = g_hash_table_new (NULL, NULL);

	for (i = 1; i < 10 * 1000; ++i)
		g_hash_table_insert (h, GUINT_TO_POINTER (i), GUINT_TO_POINTER (i));


	for (j = 0; j < 100000; ++j)
		for (i = 1; i < 10 * 105; ++i)
			g_hash_table_lookup (h, GUINT_TO_POINTER (i));

	g_hash_table_destroy (h);
}

static void
thread_state_init (MonoThreadUnwindState *ctx)
{
}


int
main (void)
{
	MonoThreadInfoRuntimeCallbacks ticallbacks;
	int res = 0;

	CHECKED_MONO_INIT ();
	mono_thread_info_init (sizeof (MonoThreadInfo));
	memset (&ticallbacks, 0, sizeof (ticallbacks));
	ticallbacks.thread_state_init = thread_state_init;
	mono_thread_info_runtime_init (&ticallbacks);
#ifndef HOST_WIN32
	mono_w32handle_init ();
#endif

	mono_thread_info_attach ();

	// benchmark_conc ();
	// benchmark_glib ();

	res += single_writer_single_reader ();
	res += parallel_writer_single_reader ();
	res += single_writer_parallel_reader ();
	res += parallel_writer_parallel_reader ();

	return res;
}
