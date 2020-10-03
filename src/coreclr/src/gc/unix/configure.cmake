check_include_files(sys/time.h HAVE_SYS_TIME_H)
check_include_files(sys/mman.h HAVE_SYS_MMAN_H)
check_include_files(numa.h HAVE_NUMA_H)
check_include_files(pthread_np.h HAVE_PTHREAD_NP_H)

check_function_exists(vm_allocate HAVE_VM_ALLOCATE)
check_function_exists(sysctlbyname HAVE_SYSCTLBYNAME)

check_cxx_source_compiles("
    #include <pthread.h>
    #include <stdint.h>

    int main()
    {
        uint64_t tid;
        pthread_threadid_np(pthread_self(), &tid);
        return (int)tid;
    }
    " HAVE_PTHREAD_THREADID_NP)

check_cxx_source_compiles("
    #include <pthread.h>
    #include <stdint.h>

    int main()
    {
        return (int)pthread_getthreadid_np();
    }
    " HAVE_PTHREAD_GETTHREADID_NP)

check_cxx_source_compiles("
    #include <sys/mman.h>

    int main()
    {
        return VM_FLAGS_SUPERPAGE_SIZE_ANY;
    }
    " HAVE_VM_FLAGS_SUPERPAGE_SIZE_ANY)

check_cxx_source_compiles("
    #include <sys/mman.h>

    int main()
    {
        return MAP_HUGETLB;
    }
    " HAVE_MAP_HUGETLB)

check_cxx_source_compiles("
#include <pthread_np.h>
int main(int argc, char **argv) {
  cpuset_t cpuSet;

  return 0;
}" HAVE_CPUSET_T)

check_cxx_source_runs("
    #include <sched.h>

    int main()
    {
        int result = sched_getcpu();
        if (result == -1)
        {
            return 1;
        }

        return 0;
    }
    " HAVE_SCHED_GETCPU)

check_library_exists(pthread pthread_condattr_setclock "" HAVE_PTHREAD_CONDATTR_SETCLOCK)

check_cxx_source_runs("
    #include <stdlib.h>
    #include <mach/mach_time.h>
    int main()
    {
        int ret;
        mach_timebase_info_data_t timebaseInfo;
        ret = mach_timebase_info(&timebaseInfo);
        mach_absolute_time();
        exit(ret);
    }
    " HAVE_MACH_ABSOLUTE_TIME)


check_library_exists(c sched_getaffinity "" HAVE_SCHED_GETAFFINITY)
check_library_exists(c sched_setaffinity "" HAVE_SCHED_SETAFFINITY)
check_library_exists(pthread pthread_create "" HAVE_LIBPTHREAD)

if (HAVE_LIBPTHREAD)
  set(PTHREAD_LIBRARY pthread)
elseif (HAVE_PTHREAD_IN_LIBC)
  set(PTHREAD_LIBRARY c)
endif()

check_library_exists(${PTHREAD_LIBRARY} pthread_setaffinity_np "" HAVE_PTHREAD_SETAFFINITY_NP)

check_cxx_symbol_exists(_SC_PHYS_PAGES unistd.h HAVE__SC_PHYS_PAGES)
check_cxx_symbol_exists(_SC_AVPHYS_PAGES unistd.h HAVE__SC_AVPHYS_PAGES)
check_cxx_symbol_exists(swapctl sys/swap.h HAVE_SWAPCTL)
check_function_exists(sysctl HAVE_SYSCTL)
check_function_exists(sysinfo HAVE_SYSINFO)
check_function_exists(sysconf HAVE_SYSCONF)
check_struct_has_member ("struct sysinfo" mem_unit "sys/sysinfo.h" HAVE_SYSINFO_WITH_MEM_UNIT)

check_cxx_source_compiles("
#include <sys/param.h>
#include <sys/sysctl.h>
#include <vm/vm_param.h>

int main(int argc, char **argv)
{
    struct xswdev xsw;

    return 0;
}" HAVE_XSWDEV)

check_cxx_source_compiles("
#include <sys/param.h>
#include <sys/sysctl.h>

int main(int argc, char **argv)
{
    struct xsw_usage xsu;

    return 0;
}" HAVE_XSW_USAGE)

configure_file(${CMAKE_CURRENT_LIST_DIR}/config.gc.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.gc.h)
