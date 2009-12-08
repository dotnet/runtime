#define GRAY_QUEUE_SECTION_SIZE	125
#define GRAY_QUEUE_LENGTH_LIMIT	64

typedef struct _GrayQueueSection GrayQueueSection;
struct _GrayQueueSection {
	int start;
	int end;
	GrayQueueSection *next;
	char *objects [GRAY_QUEUE_SECTION_SIZE];
};

static GrayQueueSection *gray_queue_start = NULL;
static GrayQueueSection *gray_queue_end = NULL;

static GrayQueueSection *gray_queue_section_free_list = NULL;
static int gray_queue_free_list_length = 0;

static int gray_queue_balance = 0;
static int num_gray_queue_sections = 0;

static void
gray_object_alloc_queue_section (void)
{
	GrayQueueSection *section;

	if (gray_queue_section_free_list) {
		section = gray_queue_section_free_list;
		gray_queue_section_free_list = gray_queue_section_free_list->next;
		--gray_queue_free_list_length;
	} else {
		section = get_internal_mem (sizeof (GrayQueueSection), INTERNAL_MEM_GRAY_QUEUE);
		++num_gray_queue_sections;
	}

	section->start = section->end = 0;
	section->next = NULL;

	if (gray_queue_end) {
		g_assert (gray_queue_start);
		gray_queue_end = gray_queue_end->next = section;
	} else {
		g_assert (!gray_queue_start);
		gray_queue_start = gray_queue_end = section;
	}
}

static void
gray_object_enqueue (char *obj)
{
	g_assert (obj);
	if (!gray_queue_end || gray_queue_end->end == GRAY_QUEUE_SECTION_SIZE)
		gray_object_alloc_queue_section ();
	g_assert (gray_queue_end && gray_queue_end->end < GRAY_QUEUE_SECTION_SIZE);
	gray_queue_end->objects [gray_queue_end->end++] = obj;

	++gray_queue_balance;
}

static gboolean
gray_object_queue_is_empty (void)
{
	if (!gray_queue_start) {
		g_assert (!gray_queue_end);
		return TRUE;
	} else {
		g_assert (gray_queue_end);
		return FALSE;
	}
}

static char*
gray_object_dequeue (void)
{
	char *obj;

	if (gray_object_queue_is_empty ())
		return NULL;

	g_assert (gray_queue_start->start < gray_queue_start->end);

	obj = gray_queue_start->objects [gray_queue_start->start++];

	if (gray_queue_start->start == gray_queue_start->end) {
		GrayQueueSection *section = gray_queue_start->next;
		if (gray_queue_free_list_length >= GRAY_QUEUE_LENGTH_LIMIT) {
			free_internal_mem (gray_queue_start, INTERNAL_MEM_GRAY_QUEUE);
		} else {
			gray_queue_start->next = gray_queue_section_free_list;
			gray_queue_section_free_list = gray_queue_start;
			++gray_queue_free_list_length;
		}
		if (section)
			gray_queue_start = section;
		else
			gray_queue_start = gray_queue_end = NULL;
	}

	--gray_queue_balance;

	return obj;
}

static void
gray_object_queue_init (void)
{
	g_assert (gray_object_queue_is_empty ());
	g_assert (sizeof (GrayQueueSection) < MAX_FREELIST_SIZE);
	g_assert (gray_queue_balance == 0);
}
