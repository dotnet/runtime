/* This tests g_assertf.

f is for format, like printf

Previously one would say like:
	if (!expr)
	  g_error(...)
  
now:
	g_assertf(expr, ...);
*/

#include "../glib.h"

int main(int argc, char** argv)
{
	g_assertf(1, "", *(volatile char*)0);
	g_assertf(0, "argc:%d, argv0:%s", argc, argv[0]);
	return 0;
}
