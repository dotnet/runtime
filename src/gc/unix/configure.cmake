check_include_files(sys/time.h HAVE_SYS_TIME_H)
check_include_files(sys/mman.h HAVE_SYS_MMAN_H)
check_include_files(numa.h HAVE_NUMA_H)
check_function_exists(vm_allocate HAVE_VM_ALLOCATE)
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

configure_file(${CMAKE_CURRENT_LIST_DIR}/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)
