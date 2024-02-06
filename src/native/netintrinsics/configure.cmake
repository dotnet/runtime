include(CheckCSourceRuns)
include(CheckIncludeFile)

if(CLR_CMAKE_TARGET_FREEBSD)
  set(CMAKE_REQUIRED_INCLUDES ${CROSS_ROOTFS}/usr/local/include)
elseif(CLR_CMAKE_TARGET_SUNOS)
  set(CMAKE_REQUIRED_INCLUDES /opt/local/include)
endif()

check_include_files(ieeefp.h HAVE_IEEEFP_H)

set(CMAKE_REQUIRED_LIBRARIES m)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  volatile double x = 10;
  if (!isnan(acos(x))) {
    exit(1);
  }
  exit(0);
}" HAVE_COMPATIBLE_ACOS)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  volatile double arg = 10;
  if (!isnan(asin(arg))) {
    exit(1);
  }
  exit(0);
}" HAVE_COMPATIBLE_ASIN)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  volatile double base = 1.0;
  volatile double infinity = 1.0 / 0.0;
  if (pow(base, infinity) != 1.0 || pow(base, -infinity) != 1.0) {
    exit(1);
  }
  if (pow(-base, infinity) != 1.0 || pow(-base, -infinity) != 1.0) {
    exit(1);
  }

  base = 0.0;
  if (pow(base, infinity) != 0.0) {
    exit(1);
  }
  if (pow(base, -infinity) != infinity) {
    exit(1);
  }

  base = 1.1;
  if (pow(-base, infinity) != infinity || pow(base, infinity) != infinity) {
    exit(1);
  }
  if (pow(-base, -infinity) != 0.0 || pow(base, -infinity) != 0.0) {
    exit(1);
  }

  base = 0.0;
  volatile int iexp = 1;
  if (pow(-base, -iexp) != -infinity) {
    exit(1);
  }
  if (pow(base, -iexp) != infinity) {
    exit(1);
  }
  exit(0);
}" HAVE_COMPATIBLE_POW)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(int argc, char **argv) {
  double result;
  volatile double base = 3.2e-10;
  volatile double exp = 1 - 5e14;

  result = pow(-base, exp);
  if (result != -1.0 / 0.0) {
    exit(1);
  }
  exit(0);
}" HAVE_VALID_NEGATIVE_INF_POW)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(int argc, char **argv) {
    double result;
    volatile double base = 3.5;
    volatile double exp = 3e100;

    result = pow(-base, exp);
    if (result != 1.0 / 0.0) {
        exit(1);
    }
    exit(0);
}" HAVE_VALID_POSITIVE_INF_POW)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  double pi = 3.14159265358979323846;
  double result;
  volatile double y = 0.0;
  volatile double x = 0.0;

  result = atan2(y, -x);
  if (fabs(pi - result) > 0.0000001) {
    exit(1);
  }

  result = atan2(-y, -x);
  if (fabs(-pi - result) > 0.0000001) {
    exit(1);
  }

  result = atan2 (-y, x);
  if (result != 0.0 || copysign (1.0, result) > 0) {
    exit(1);
  }

  result = atan2 (y, x);
  if (result != 0.0 || copysign (1.0, result) < 0) {
    exit(1);
  }

  exit (0);
}" HAVE_COMPATIBLE_ATAN2)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  double d = exp(1.0), e = M_E;

  /* Used memcmp rather than == to test that the doubles are equal to
   prevent gcc's optimizer from using its 80 bit internal long
   doubles. If you use ==, then on BSD you get a false negative since
   exp(1.0) == M_E to 64 bits, but not 80.
  */

  if (memcmp (&d, &e, sizeof (double)) == 0) {
    exit(0);
  }
  exit(1);
}" HAVE_COMPATIBLE_EXP)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  if (FP_ILOGB0 != -2147483648) {
    exit(1);
  }

  exit(0);
}" HAVE_COMPATIBLE_ILOGB0)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  if (FP_ILOGBNAN != 2147483647) {
    exit(1);
  }

  exit(0);
}" HAVE_COMPATIBLE_ILOGBNAN)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  volatile int arg = 10000;
  if (!isnan(log(-arg))) {
    exit(1);
  }
  exit(0);
}" HAVE_COMPATIBLE_LOG)
check_c_source_runs("
#include <math.h>
#include <stdlib.h>

int main(void) {
  volatile int arg = 10000;
  if (!isnan(log10(-arg))) {
    exit(1);
  }
  exit(0);
}" HAVE_COMPATIBLE_LOG10)

configure_file(${CMAKE_CURRENT_LIST_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)
