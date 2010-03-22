/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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
#define PIN_STAGING_AREA_SIZE	1024

static void* pin_staging_area [PIN_STAGING_AREA_SIZE];
static int pin_staging_area_index;

static void
init_pinning (void)
{
	pin_staging_area_index = 0;
}

static void
evacuate_pin_staging_area (void)
{
	int i;

	g_assert (pin_staging_area_index >= 0 && pin_staging_area_index <= PIN_STAGING_AREA_SIZE);

	if (pin_staging_area_index == 0)
		return;

	/*
	 * The pinning addresses might come from undefined memory, this is normal. Since they
	 * are used in lots of functions, we make the memory defined here instead of having
	 * to add a supression for those functions.
	 */
	VALGRIND_MAKE_MEM_DEFINED (pin_staging_area, pin_staging_area_index * sizeof (void*));

	sort_addresses (pin_staging_area, pin_staging_area_index);

	while (next_pin_slot + pin_staging_area_index > pin_queue_size)
		realloc_pin_queue ();

	pin_queue [next_pin_slot++] = pin_staging_area [0];
	for (i = 1; i < pin_staging_area_index; ++i) {
		void *p = pin_staging_area [i];
		if (p != pin_queue [next_pin_slot - 1])
			pin_queue [next_pin_slot++] = p;
	}

	g_assert (next_pin_slot <= pin_queue_size);

	pin_staging_area_index = 0;
}

static void
pin_stage_ptr (void *ptr)
{
	if (pin_staging_area_index >= PIN_STAGING_AREA_SIZE)
		evacuate_pin_staging_area ();

	pin_staging_area [pin_staging_area_index++] = ptr;
}
