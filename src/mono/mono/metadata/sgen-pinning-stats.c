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
enum {
	PIN_TYPE_STACK,
	PIN_TYPE_STATIC_DATA,
	PIN_TYPE_OTHER,
	PIN_TYPE_MAX
};

typedef struct _PinStatAddress PinStatAddress;
struct _PinStatAddress {
	char *addr;
	int pin_types;
	PinStatAddress *left;
	PinStatAddress *right;
};

typedef struct _ObjectList ObjectList;
struct _ObjectList {
	MonoObject *obj;
	ObjectList *next;
};

static PinStatAddress *pin_stat_addresses = NULL;
static size_t pinned_byte_counts [PIN_TYPE_MAX];

static ObjectList *pinned_objects = NULL;

static void
pin_stats_tree_free (PinStatAddress *node)
{
	if (!node)
		return;
	pin_stats_tree_free (node->left);
	pin_stats_tree_free (node->right);
	mono_sgen_free_internal_dynamic (node, sizeof (PinStatAddress), INTERNAL_MEM_STATISTICS);
}

static void
pin_stats_reset (void)
{
	int i;
	pin_stats_tree_free (pin_stat_addresses);
	pin_stat_addresses = NULL;
	for (i = 0; i < PIN_TYPE_MAX; ++i)
		pinned_byte_counts [i] = 0;
	while (pinned_objects) {
		ObjectList *next = pinned_objects->next;
		mono_sgen_free_internal_dynamic (pinned_objects, sizeof (ObjectList), INTERNAL_MEM_STATISTICS);
		pinned_objects = next;
	}
}

static void
pin_stats_register_address (char *addr, int pin_type)
{
	PinStatAddress **node_ptr = &pin_stat_addresses;
	PinStatAddress *node;
	int pin_type_bit = 1 << pin_type;

	while (*node_ptr) {
		node = *node_ptr;
		if (addr == node->addr) {
			node->pin_types |= pin_type_bit;
			return;
		}
		if (addr < node->addr)
			node_ptr = &node->left;
		else
			node_ptr = &node->right;
	}

	node = mono_sgen_alloc_internal_dynamic (sizeof (PinStatAddress), INTERNAL_MEM_STATISTICS);
	node->addr = addr;
	node->pin_types = pin_type_bit;
	node->left = node->right = NULL;

	*node_ptr = node;
}

static void
pin_stats_count_object_from_tree (char *obj, size_t size, PinStatAddress *node, int *pin_types)
{
	if (!node)
		return;
	if (node->addr >= obj && node->addr < obj + size) {
		int i;
		for (i = 0; i < PIN_TYPE_MAX; ++i) {
			int pin_bit = 1 << i;
			if (!(*pin_types & pin_bit) && (node->pin_types & pin_bit)) {
				pinned_byte_counts [i] += size;
				*pin_types |= pin_bit;
			}
		}
	}
	if (obj < node->addr)
		pin_stats_count_object_from_tree (obj, size, node->left, pin_types);
	if (obj + size - 1 > node->addr)
		pin_stats_count_object_from_tree (obj, size, node->right, pin_types);
}

void
mono_sgen_pin_stats_register_object (char *obj, size_t size)
{
	int pin_types = 0;
	ObjectList *list;

	if (!heap_dump_file)
		return;

	list = mono_sgen_alloc_internal_dynamic (sizeof (ObjectList), INTERNAL_MEM_STATISTICS);
	pin_stats_count_object_from_tree (obj, size, pin_stat_addresses, &pin_types);
	list->obj = (MonoObject*)obj;
	list->next = pinned_objects;
	pinned_objects = list;
}
