#include <assert.h>
#include <stdio.h>

#include "zbuild.h"

extern int LLVMFuzzerTestOneInput(const unsigned char *data, size_t size);

int main(int argc, char **argv) {
    int i;
    fprintf(stderr, "StandaloneFuzzTargetMain: running %d inputs\n", argc - 1);

    for (i = 1; i < argc; i++) {
        size_t len, n_read, err;
        unsigned char *buf;
        FILE *f = fopen(argv[i], "rb+");
        if (!f) {
            /* Failed to open this file: it may be a directory. */
            fprintf(stderr, "Skipping: %s\n", argv[i]);
            continue;
        }
        fprintf(stderr, "Running: %s %s\n", argv[0], argv[i]);
        fseek(f, 0, SEEK_END);
        len = ftell(f);
        fseek(f, 0, SEEK_SET);
        buf = (unsigned char *)malloc(len);
        n_read = fread(buf, 1, len, f);
        assert(n_read == len);
        LLVMFuzzerTestOneInput(buf, len);
        free(buf);
        err = fclose(f);
        assert(err == 0);
        Z_UNUSED(err);
        fprintf(stderr, "Done:    %s: (%d bytes)\n", argv[i], (int)n_read);
    }

    return 0;
}
