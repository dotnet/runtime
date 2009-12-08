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

static PinStatAddress *pin_stat_addresses = NULL;
static size_t pinned_byte_counts [PIN_TYPE_MAX];

static void
pin_stats_tree_free (PinStatAddress *node)
{
	if (!node)
		return;
	pin_stats_tree_free (node->left);
	pin_stats_tree_free (node->right);
	free_internal_mem (node, INTERNAL_MEM_STATISTICS);
}

static void
pin_stats_reset (void)
{
	int i;
	pin_stats_tree_free (pin_stat_addresses);
	pin_stat_addresses = NULL;
	for (i = 0; i < PIN_TYPE_MAX; ++i)
		pinned_byte_counts [i] = 0;
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

	node = get_internal_mem (sizeof (PinStatAddress), INTERNAL_MEM_STATISTICS);
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

static void
pin_stats_register_object (char *obj, size_t size)
{
	int pin_types = 0;
	pin_stats_count_object_from_tree (obj, size, pin_stat_addresses, &pin_types);
}
