/* math.c - these are based on bob smith's csharp routines */

#define __USE_ISOC99
#include <math.h>
#include <mono/metadata/sysmath.h>

gdouble 
ves_icall_System_Math_Sin (gdouble x)
{
	return sin (x);
}

gdouble 
ves_icall_System_Math_Cos (gdouble x)
{
	return cos (x);
}

gdouble 
ves_icall_System_Math_Tan (gdouble x)
{
	return tan (x);
}

gdouble 
ves_icall_System_Math_Sinh (gdouble x)
{
	return sinh (x);
}

gdouble 
ves_icall_System_Math_Cosh (gdouble x)
{
	return cosh (x);
}

gdouble 
ves_icall_System_Math_Tanh (gdouble x)
{
	return tanh (x);
}

gdouble 
ves_icall_System_Math_Acos (gdouble x)
{
	if (x < -1 || x > 1)
		return NAN;

	return acos (x);
}

gdouble 
ves_icall_System_Math_Asin (gdouble x)
{
	if (x < -1 || x > 1)
		return NAN;

	return asin (x);
}

gdouble 
ves_icall_System_Math_Atan (gdouble x)
{
	return atan (x);
}

gdouble 
ves_icall_System_Math_Atan2 (gdouble y, gdouble x)
{
	return atan2 (y, x);
}

gdouble 
ves_icall_System_Math_Exp (gdouble x)
{
	return exp (x);
}

gdouble 
ves_icall_System_Math_Log (gdouble x)
{
	if (x == 0)
		return -HUGE_VAL;
	else if (x < 0)
		return NAN;

	return log (x);
}

gdouble 
ves_icall_System_Math_Log10 (gdouble x)
{
	if (x == 0)
		return -HUGE_VAL;
	else if (x < 0)
		return NAN;

	return log10 (x);
}

gdouble 
ves_icall_System_Math_Pow (gdouble x, gdouble y)
{
	return pow (x, y);
}

gdouble 
ves_icall_System_Math_Sqrt (gdouble x)
{
	if (x < 0)
		return NAN;

	return sqrt (x);
}
