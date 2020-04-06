#include "pal_networking.h"

#include <arpa/inet.h>
#include <pthread.h>
#include <semaphore.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/socket.h>
#include <string.h>

struct state
{
    HostEntry entry;
    char* hostName;
    int errorCode;
    sem_t semaphore;
};

static void callback(HostEntry* entry, int errorCode)
{
    printf("(%lu) handler: enter, errorCode: %d\n", pthread_self(), errorCode);

    struct state* state = (struct state*)entry;

    if (errorCode == 0)
    {
        printf("(%lu) callback: # of addresses = %d\n", pthread_self(), entry->IPAddressCount);
        for (int i = 0; i < entry->IPAddressCount; ++i)
        {
            IPAddress ipAddress = entry->IPAddressList[i];

            char buffer[256];
            inet_ntop(ipAddress.IsIPv6 ? AF_INET6 : AF_INET, ipAddress.Address, buffer, sizeof(buffer));
            printf("(%lu) ip for %s: %s\n", pthread_self(), entry->CanonicalName, buffer);
        }
    }

    state->errorCode = errorCode;

    sem_post(&state->semaphore);
}

int main(int argc, char** argv)
{
    if (argc != 2)
    {
        printf("hostname must be given as argument\n");
        return EXIT_FAILURE;
    }

    if (!SystemNative_PlatformSupportsGetAddrInfoAsync())
    {
        printf("platform support not available\n");
        return EXIT_FAILURE;
    }

    printf("platform support available\n");

    char* hostName = argv[1];
    printf("(%lu) hostname: %s\n", pthread_self(), hostName);

    if (strcmp(hostName, "null")==0)
    {
        hostName = NULL;
    }

    struct state state;
    sem_init(&state.semaphore, 0, 0);

    int error = SystemNative_GetHostEntryForNameAsync((uint8_t*)hostName, &state.entry, callback);

    if (error != 0)
    {
        printf("(%lu) OS call failed with error %d\n", pthread_self(), error);
        return EXIT_FAILURE;
    }

    printf("(%lu) main: waiting for semaphore\n", pthread_self());

    sem_wait(&state.semaphore);
    sem_destroy(&state.semaphore);

    printf("(%lu) main: exit, errorCode: %d\n", pthread_self(), state.errorCode);

    return state.errorCode;
}
