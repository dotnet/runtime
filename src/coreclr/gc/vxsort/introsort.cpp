
#include "common.h"
#include "introsort.h"


 void introsort::sort (uint8_t** begin, uint8_t** end, int ignored)
    {
        ignored = 0;
        introsort_loop (begin, end, max_depth);
        insertionsort (begin, end);
    }


 void introsort::introsort_loop (uint8_t** lo, uint8_t** hi, int depth_limit)
    {
        while (hi-lo >= size_threshold)
        {
            if (depth_limit == 0)
            {
                heapsort (lo, hi);
                return;
            }
            uint8_t** p=median_partition (lo, hi);
            depth_limit=depth_limit-1;
            introsort_loop (p, hi, depth_limit);
            hi=p-1;
        }
    }

 uint8_t** introsort::median_partition (uint8_t** low, uint8_t** high)
    {
        uint8_t *pivot, **left, **right;

        //sort low middle and high
        if (*(low+((high-low)/2)) < *low)
            swap_elements ((low+((high-low)/2)), low);
        if (*high < *low)
            swap_elements (low, high);
        if (*high < *(low+((high-low)/2)))
            swap_elements ((low+((high-low)/2)), high);

        swap_elements ((low+((high-low)/2)), (high-1));
        pivot =  *(high-1);
        left = low; right = high-1;
        while (1) {
            while (*(--right) > pivot);
            while (*(++left)  < pivot);
            if (left < right)
            {
                swap_elements(left, right);
            }
            else
                break;
        }
        swap_elements (left, (high-1));
        return left;
    }


 void introsort::insertionsort (uint8_t** lo, uint8_t** hi)
    {
        for (uint8_t** i=lo+1; i <= hi; i++)
        {
            uint8_t** j = i;
            uint8_t* t = *i;
            while((j > lo) && (t <*(j-1)))
            {
                *j = *(j-1);
                j--;
            }
            *j = t;
        }
    }

 void introsort::heapsort (uint8_t** lo, uint8_t** hi)
    {
        size_t n = hi - lo + 1;
        for (size_t i=n / 2; i >= 1; i--)
        {
            downheap (i,n,lo);
        }
        for (size_t i = n; i > 1; i--)
        {
            swap_elements (lo, lo + i - 1);
            downheap(1, i - 1,  lo);
        }
    }

 void introsort::downheap (size_t i, size_t n, uint8_t** lo)
    {
        uint8_t* d = *(lo + i - 1);
        size_t child;
        while (i <= n / 2)
        {
            child = 2*i;
            if (child < n && *(lo + child - 1)<(*(lo + child)))
            {
                child++;
            }
            if (!(d<*(lo + child - 1)))
            {
                break;
            }
            *(lo + i - 1) = *(lo + child - 1);
            i = child;
        }
        *(lo + i - 1) = d;
    }
