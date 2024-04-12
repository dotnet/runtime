typedef void * (*setup_func) (void);
typedef void (*measurement_func) (void *data);

typedef struct {
    const char *name;
    setup_func setup;
    measurement_func func, teardown;
} measurement_info;
