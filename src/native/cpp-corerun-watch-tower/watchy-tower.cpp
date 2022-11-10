// File: watchy-tower.cpp

// Will replace these includes with Microsoft .NET's ones if necessary afterwards :)
#include <cstdio>
#include <cstdlib>

static void display_usage();
static bool parse_args(const int, const char *[]);

int main(const int argc, const char *argv[])
{
    if (!parse_args(argc, argv))
        return EXIT_FAILURE;

    return EXIT_SUCCESS;
}

static void display_usage()
{
    fprintf(stderr, "Help will go here.\n");
    return ;
}

static bool parse_args(const int argc, const char *argv[])
{
    if (argc < 2)
    {
        display_usage();
        return false;
    }

    for (int i = 1; i < argc; i++)
    {
        printf("%s\n", argv[i]);
    }

    return true;
}
