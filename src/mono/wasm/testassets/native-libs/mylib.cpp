#include <stdio.h>

extern "C" {
    int cpp_add(int a, int b) {
        return a + b;
    }
}