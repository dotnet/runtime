#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>

int vasprintf(char **ret, const char *fmt, va_list ap)
{
	char *buf;
	int len;
	size_t buflen;
	va_list ap2;
	
#if defined(_MSC_VER) || defined(__MINGW64_VERSION_MAJOR)
	ap2 = ap;
	len = _vscprintf(fmt, ap2); // NOTE MS specific extension ( :-( )
#else
	va_copy(ap2, ap);
	len = vsnprintf(NULL, 0, fmt, ap2);
#endif
	
	if (len >= 0 && (buf = malloc ((buflen = (size_t) (len + 1)))) != NULL) {
		len = vsnprintf(buf, buflen, fmt, ap);
		*ret = buf;
	} else {
		*ret = NULL;
		len = -1;
	}
	
	va_end(ap2);
	return len;
}

