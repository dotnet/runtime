Configuring Huge Pages for loading composite binaries using CoreCLR on Linux
----

Huge pages can provide performance benefits to reduce the cost of TLB cache misses when
executing code. In general, the largest available wins may be achieved by enabling huge
pages for use by the GC, which will dominate the memory use in the process, but in some
circumstances, if the application is sufficiently large, there may be a benefit to using
huge pages to map in code.

It is expected that consumers who have these needs have very large applications, and are
able to tolerate somewhat complex solutions. CoreCLR supports loading composite R2R
images using the hugetlbfs. Doing some requires several steps.

1. The composite image must be created with a switch such as `--custom-pe-section-alignment=2097152`. This will align the PE sections in the R2R file on 2MB virtual address boundaries, and align the sections in the PE file itself on the same boundaries.
  - This will increase the size of the image by up to 5 * the specified alignment. Typical increases will be more similar to 3 * the specified alignment
2. The composite image must be copied into a hugetlbfs filesystem which is visible to the .NET process instead of the composite image being loaded from the normal path.
  - IMPORTANT: The composite image must NOT be located in the normal path next to the application binary, or that file will be used instead of the huge page version.
  - The environment variable `COMPlus_NativeImageSearchPaths` must be set to point at the location of the hugetlbfs in use. For instance, `COMPlus_NativeImageSearchPaths` might be set to `/var/lib/hugetlbfs/user/USER/pagesize-2MB`
  - As the cp command does not support copying into a hugetlbfs due to lack of support for the write syscall in that file system, a custom copy application must be used. A sample application that may be used to perform this task has a source listing in Appendix A.
3. The machine must be configured to have sufficient huge pages available in the appropriate huge page pool. The memory requirements of huge page PE loading are as follows.
  - Sufficient pages to hold the unmodified copy of the composite image in the hugetlbfs. These pages will be used by the initial copy which emplaces the composite image into huge pages.
  - By default the runtime will map each page of the composite image using a MAP_PRIVATE mapping. This will require that the maximum number of huge pages is large enough to hold a completely separate copy of the image as loaded.
  - To reduce that cost, launch the application with the PAL_MAP_READONLY_PE_HUGE_PAGE_AS_SHARED environment variable set to 1. This environment variable will change the way that the composite image R2R files are mapped into the process to create the mappings to read only sections as MAP_SHARED mappings. This will reduce the extra huge pages needed to only be the sections marked as RW in the PE file. On a Windows machine use the link tool (`link /dump /header compositeimage.dll` to determine the number of pages needed for the these `.data` section of the PE file.)
    - If the PAL_MAP_READONLY_PE_HUGE_PE_AS_SHARED is set, the number of huge pages needed is `<Count of huge pages for composite file> + <count of processes to run> * <count of huge pages needed for the .data section of the composite file>`

Appendix A - Source for a simple copy into hugetlbfs program.

```
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <sys/mman.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <string.h>
#include <unistd.h>

int main(int argc, char** argv)
{
    if (argc != 3)
    {
        printf("Incorrect number arguments specified. Arguments are <src> <dest>");
        return 1;
    }

    void *addrSrc, *addrDest;
    int fdSrc, fdDest, ret;

    fdSrc = open(argv[1], O_RDWR);
    if (fdSrc < 0)
    {
        printf("Open src failed\n");
        return 1;
    }

    struct stat st;
    if (fstat(fdSrc, &st) < 0)
    {
        printf("fdSrc fstat failed\n");
        return 1;
    }

    addrSrc = mmap(0, st.st_size, PROT_READ | PROT_WRITE, MAP_SHARED, fdSrc, 0);
    if (addrSrc == MAP_FAILED)
    {
        printf("fdSrc mmap failed\n");
        return 1;
    }

    fdDest = open(argv[2], O_CREAT | O_RDWR, 0755);
    if (fdDest < 0)
    {
        printf("Open dest failed\n");
        return 1;
    }

    if (ftruncate(fdDest, st.st_size) < 0)
    {
        printf("ftruncate failed\n");
        return 1;
    }

    addrDest = mmap(0, st.st_size, PROT_READ | PROT_WRITE, MAP_SHARED, fdDest, 0);
    if (addrDest == MAP_FAILED)
    {
        printf("fdDest mmap failed\n");
        return 1;
    }

    memcpy(addrDest, addrSrc, st.st_size);

    munmap(addrSrc, st.st_size);
    munmap(addrDest, st.st_size);
    close(fdSrc);
    close(fdDest);
    return 0;
}
```
