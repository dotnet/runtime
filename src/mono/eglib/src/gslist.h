#include <glib.h>

typedef struct _GSList GSList;
struct _GSList {
	gpointer data;
	GSList *next;
};

GSList *g_slist_alloc (void);
GSList *g_slist_append (GSList* list, gpointer data);
GSList *g_slist_prepend (GSList* list, gpointer data);
void    g_slist_free (GSList* list);
void    g_slist_free_1 (GSList* list);
GSList *g_slist_copy (GSList* list);
GSList *g_slist_concat (GSList* list1, GSList* list2);
void    g_slist_foreach (GSList* list, GFunc func, gpointer user_data);
GSList *g_slist_last (GSList *list);
#define g_slist_next (slist) ((slist) ? (((GSList *) slist)->next) : NULL)
