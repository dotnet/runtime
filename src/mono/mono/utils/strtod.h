#ifndef MONO_STRTOD_H
#define MONO_STRTOD_H 1

double bsd_strtod (const char *s00, char **se);
char *__bsd_dtoa  (double d, int mode, int ndigits, int *decpt, int *sign, char **rve, char **resultp);

#endif
