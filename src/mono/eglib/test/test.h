#ifndef _TEST_H
#define _TEST_H

#include <stdarg.h>

typedef struct _Test Test;

typedef char * (* RunTestHandler)();
typedef Test * (* LoadGroupHandler)();

struct _Test {
	const char *name;
	RunTestHandler handler;
};

void run_test(Test *test);
void run_group(const char *name, LoadGroupHandler group_handler);
void run_groups(const char *first_name, LoadGroupHandler first_group_handler, ...);

#define DEFINE_TEST_GROUP_INIT(name, table) \
	Test * (name)() { return table; }

#define DEFINE_TEST_GROUP_INIT_H(name) \
	Test * (name)();

#define RESULT(x) g_strdup_printf(x);

#endif /* _TEST_H */

