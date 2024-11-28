
class introsort
{

private:
    static const int size_threshold = 64;
    static const int max_depth = 100;


inline static void swap_elements(uint8_t** i,uint8_t** j)
    {
        uint8_t* t=*i;
        *i=*j;
        *j=t;
    }

public:
    static void sort (uint8_t** begin, uint8_t** end, int ignored);

private:

    static void introsort_loop (uint8_t** lo, uint8_t** hi, int depth_limit);

    static uint8_t** median_partition (uint8_t** low, uint8_t** high);

    static void insertionsort (uint8_t** lo, uint8_t** hi);

    static void heapsort (uint8_t** lo, uint8_t** hi);

    static void downheap (size_t i, size_t n, uint8_t** lo);

};