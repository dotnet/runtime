/*
 * gqueue.c: Queue
 *
 * Author:
 *   Duncan Mak (duncan@novell.com)
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
 *
 * (C) 2006 Novell, Inc.
 *
 */

#include <stdio.h>
#include <glib.h>

gpointer
g_queue_pop_head (GQueue *queue)
{
	gpointer head = NULL;

	if (queue->length != 0){
		GList *old_head;

		old_head = queue->head;
		head = old_head->data;
		queue->head = old_head->next;
		queue->length--;
		g_list_free_1 (old_head);
	}

	return head;
}

gboolean
g_queue_is_empty (GQueue *queue)
{
	if (!queue)
		return TRUE;
	
	return queue->length == 0;
}

void
g_queue_push_head (GQueue *queue, gpointer head)
{
	if (!queue)
		return;
	
	queue->head = g_list_prepend (queue->head, head);
	
	if (!queue->tail)
		queue->tail = queue->head;

	queue->length ++;
}

GQueue *
g_queue_new (void)
{
	GQueue *queue = g_new0 (GQueue, 1);
	queue->length = 0;

	return queue;
}

void
g_queue_free (GQueue *queue)
{
	if (!queue)
		return;
	
	g_list_free (queue->head);
	g_free (queue);
}
