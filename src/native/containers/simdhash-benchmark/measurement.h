typedef void * (*setup_func) (void);
typedef void (*measurement_func) (void *data);

typedef struct {
    const char *name;
    setup_func setup;
    measurement_func func, teardown;
} measurement_info;

#define MEASUREMENT(name, data_type, setup, teardown, body) \
    static void DN_SIMDHASH_GLUE(measurement_, name) (void *_data) { \
        data_type data = (data_type)_data; \
        body; \
    }
