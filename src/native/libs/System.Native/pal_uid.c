// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_safecrt.h"
#include "pal_uid.h"
#include "pal_utilities.h"

#include <assert.h>
#include <errno.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/types.h>
#include <grp.h>
#include <pwd.h>

// Linux c-libraries (glibc, musl) provide a thread-safe getgrouplist.
// OSX man page mentions explicitly the implementation is not thread safe,
// due to using getgrent.
#ifndef __linux__
#define USE_GROUPLIST_LOCK
#endif

#if defined(USE_GROUPLIST_LOCK) || !HAVE_GETGRGID_R
#include <pthread.h>
#endif

static int32_t ConvertNativePasswdToPalPasswd(int error, struct passwd* nativePwd, struct passwd* result, Passwd* pwd)
{
    // positive error number returned -> failure other than entry-not-found
    if (error != 0)
    {
        assert(error > 0);
        memset(pwd, 0, sizeof(Passwd)); // managed out param must be initialized
        return error;
    }

    // 0 returned with null result -> entry-not-found
    if (result == NULL)
    {
        memset(pwd, 0, sizeof(Passwd)); // managed out param must be initialized
        return -1; // shim convention for entry-not-found
    }

    // 0 returned with non-null result (guaranteed to be set to pwd arg) -> success
    assert(result == nativePwd);
    pwd->Name = nativePwd->pw_name;
    pwd->Password = nativePwd->pw_passwd;
    pwd->UserId = nativePwd->pw_uid;
    pwd->GroupId = nativePwd->pw_gid;
    pwd->UserInfo = nativePwd->pw_gecos;
    pwd->HomeDirectory = nativePwd->pw_dir;
    pwd->Shell = nativePwd->pw_shell;
    return 0;
}

int32_t SystemNative_GetPwUidR(uint32_t uid, Passwd* pwd, char* buf, int32_t buflen)
{
    assert(pwd != NULL);
    assert(buf != NULL);
    assert(buflen >= 0);

    if (buflen < 0)
        return EINVAL;

    struct passwd nativePwd;
    struct passwd* result;
    int error;
    while ((error = getpwuid_r(uid, &nativePwd, buf, Int32ToSizeT(buflen), &result)) == EINTR);

    return ConvertNativePasswdToPalPasswd(error, &nativePwd, result, pwd);
}

int32_t SystemNative_GetPwNamR(const char* name, Passwd* pwd, char* buf, int32_t buflen)
{
    assert(pwd != NULL);
    assert(buf != NULL);
    assert(buflen >= 0);

    if (buflen < 0)
        return EINVAL;

    struct passwd nativePwd;
    struct passwd* result;
    int error;
    while ((error = getpwnam_r(name, &nativePwd, buf, Int32ToSizeT(buflen), &result)) == EINTR);

    return ConvertNativePasswdToPalPasswd(error, &nativePwd, result, pwd);
}

uint32_t SystemNative_GetEUid(void)
{
    return geteuid();
}

uint32_t SystemNative_GetEGid(void)
{
    return getegid();
}

int32_t SystemNative_SetEUid(uint32_t euid)
{
    return seteuid(euid);
}

#ifdef USE_GROUPLIST_LOCK
static pthread_mutex_t s_groupLock = PTHREAD_MUTEX_INITIALIZER;
#endif

#if !HAVE_GETGROUPLIST
int getgrouplist(const char *uname, gid_t agroup, gid_t *groups, int *groupCount)
{
    int ngroups = 1;
    int maxgroups = *groupCount;

    if (groups)
    {
        groups[ngroups] = agroup;
    }
    if (maxgroups > 1)
    {
        if (groups)
            groups[ngroups++] = agroup;
        else
            ngroups++;
    }

    setgrent();

    int i;
    int result = 0;
    const struct group *group;

    while ((group = getgrent()) != NULL)
    {
        if (groups)
        {
            bool found = false;
            for (i = 0; i < ngroups; i++)
            {
                if (group->gr_gid == groups[i])
                {
                    found = true;
                    break;
                }
            }
            if (found)
                break;
        }

        for (i = 0; group->gr_mem[i]; i++)
        {
            if (!strcmp(group->gr_mem[i], uname))
            {
                if (ngroups >= maxgroups)
                {
                    result = -1;
                    break;
                }

                if (groups)
                    groups[ngroups++] = group->gr_gid;
                else
                    ngroups++;

                break;
            }
        }
    }

    endgrent();
    *groupCount = ngroups;
    return result;
}
#endif

int32_t SystemNative_GetGroupList(const char* name, uint32_t group, uint32_t* groups, int32_t* ngroups)
{
    assert(name != NULL);
    assert(groups != NULL);
    assert(ngroups != NULL);
    assert(*ngroups >= 0);

    // The man page of getgrouplist doesn't explicitly mention the use of errno on error.
    // glibc and musl implementation look like they return -1 for functions that fail with errno set.
    // To be safe, we'll assume errno is set on error and handle EINTR.
    int rv;
    int groupsAvailable;
    do
    {
        errno = 0;
        groupsAvailable = *ngroups;

#ifdef USE_GROUPLIST_LOCK
        rv = pthread_mutex_lock(&s_groupLock);
        if (rv != 0)
        {
            errno = rv;
            rv = -1;
            break;
        }
#endif

#ifdef __APPLE__
        // On OSX groups are passed as a signed int.
        rv = getgrouplist(name, (int)group, (int*)groups, &groupsAvailable);
#else
        rv = getgrouplist(name, group, groups, &groupsAvailable);
#endif

#ifdef USE_GROUPLIST_LOCK
        pthread_mutex_unlock(&s_groupLock);
#endif

        // Check if the buffer is too small.
        if (rv == -1 &&
                (// if ngroups was updated to a higher value, the buffer was too small.
                 (groupsAvailable > *ngroups) ||
                 // OSX doesn't update ngroups when the buffer is too small.
                 // musl doesn't update ngroups when it encounters an error.
                 (groupsAvailable == *ngroups && errno == 0)))
        {
            // return an increased value for ngroups.
            *ngroups = groupsAvailable > *ngroups ? groupsAvailable : *ngroups * 2;
            return rv;
        }
    } while (rv == -1 && errno == EINTR);

    *ngroups = rv >= 0 ? groupsAvailable : -1;

    return rv;
}

int32_t SystemNative_GetGroups(int32_t ngroups, uint32_t* groups)
{
    assert(ngroups >= 0);
    assert(groups != NULL);

    return getgroups(ngroups, groups);
}

#if !HAVE_GETGRGID_R
// Need to call getgrgid which is not thread-safe, and protect it with a mutex
static pthread_mutex_t s_getgrgid_lock = PTHREAD_MUTEX_INITIALIZER;
#endif

char* SystemNative_GetGroupName(uint32_t gid)
{
#if HAVE_GETGRGID_R
    size_t bufferLength = 512;
    while (1)
    {
        char *buffer = (char*)malloc(bufferLength);
        if (buffer == NULL)
        {
            return NULL;
        }

        struct group* result;
        struct group gr;
        if (getgrgid_r(gid, &gr, buffer, bufferLength, &result) == 0)
        {
            if (result == NULL)
            {
                errno = ENOENT;
                free(buffer);
                return NULL;
            }
            else
            {
                char* name = strdup(gr.gr_name);
                free(buffer);
                return name;
            }
        }

        free(buffer);
        size_t tmpBufferLength;
        if (errno != ERANGE || !multiply_s(bufferLength, (size_t)2, &tmpBufferLength))
        {
            return NULL;
        }
        bufferLength = tmpBufferLength;
    }
#else
    // Platforms like Android API level < 24 do not have getgrgid_r available
    int rv = pthread_mutex_lock(&s_getgrgid_lock);
    if (rv != 0)
    {
        errno = rv;
        return NULL;
    }

    struct group* result = getgrgid(gid);
    if (result == NULL)
    {
        pthread_mutex_unlock(&s_getgrgid_lock);
        return NULL;
    }
    char* name = strdup(result->gr_name);
    pthread_mutex_unlock(&s_getgrgid_lock);
    return name;
#endif
}
