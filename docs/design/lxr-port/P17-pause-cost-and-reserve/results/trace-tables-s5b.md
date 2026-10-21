## Wall time, collections, pauses and peak RSS (medians of the runs that finished; ok = finished / all)

| workload | mode | build | ok | wall ms | gc ms | collections | full | pause ms (median of all pauses) | pause ms (max) | peak RSS MB |
|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | nursery | accepted | 3/3 | 5897 | 2552 | 18 | 0 | 110.0 | 381.4 | 231 |
| binarytrees | nursery | S4 | 3/3 | 2831 | 729 | 20 | 0 | 17.3 | 254.6 | 199 |
| binarytrees | nursery | S5b, overshoot off | 3/3 | 2332 | 397 | 19 | 0 | 17.7 | 189.9 | 113 |
| binarytrees | nursery | S5b | 3/3 | 2364 | 387 | 14 | 0 | 22.8 | 59.5 | 138 |
| binarytrees | mature | accepted | 3/3 | 5631 | 2365 | 17 | 0 | 109.9 | 320.9 | 231 |
| binarytrees | mature | S4 | 3/3 | 2743 | 784 | 20 | 0 | 17.4 | 277.1 | 188 |
| binarytrees | mature | S5b, overshoot off | 3/3 | 2264 | 375 | 18 | 0 | 17.2 | 183.2 | 113 |
| binarytrees | mature | S5b | 3/3 | 2382 | 384 | 14 | 0 | 21.9 | 59.3 | 139 |
| requests | nursery | accepted | 3/3 | 36331 | 23222 | 57 | 51 | 422.9 | 925.9 | 371 |
| requests | nursery | S4 | 3/3 | 9607 | 2194 | 71 | 0 | 25.4 | 134.6 | 284 |
| requests | nursery | S5b, overshoot off | 3/3 | 9376 | 2278 | 71 | 0 | 25.7 | 116.2 | 282 |
| requests | nursery | S5b | 3/3 | 8743 | 1877 | 44 | 0 | 35.9 | 166.5 | 354 |
| requests | mature | accepted | 3/3 | 40488 | 27679 | 58 | 52 | 510.9 | 995.4 | 373 |
| requests | mature | S4 | 3/3 | 10239 | 2749 | 67 | 0 | 26.4 | 309.1 | 308 |
| requests | mature | S5b, overshoot off | 3/3 | 10030 | 2702 | 68 | 0 | 26.2 | 297.7 | 311 |
| requests | mature | S5b | 3/3 | 9191 | 2098 | 42 | 0 | 35.8 | 314.7 | 369 |
| listsuffix | nursery | accepted | 3/3 | 53132 | 44191 | 64 | 0 | 698.3 | 1254.4 | 356 |
| listsuffix | nursery | S4 | 3/3 | 20581 | 13916 | 60 | 0 | 309.0 | 710.4 | 254 |
| listsuffix | nursery | S5b, overshoot off | 3/3 | 20168 | 13300 | 60 | 0 | 70.4 | 517.5 | 258 |
| listsuffix | nursery | S5b | 3/3 | 13661 | 7717 | 15 | 0 | 518.1 | 1121.2 | 538 |
| listsuffix | mature | accepted | 3/3 | 44353 | 35574 | 64 | 0 | 564.8 | 1625.1 | 311 |
| listsuffix | mature | S4 | 3/3 | 22615 | 14990 | 53 | 0 | 196.3 | 1028.8 | 410 |
| listsuffix | mature | S5b, overshoot off | 3/3 | 33954 | 27992 | 88 | 0 | 333.4 | 1128.3 | 319 |
| listsuffix | mature | S5b | 3/3 | 14241 | 8347 | 18 | 0 | 428.5 | 1221.6 | 589 |

## Borrowed capacity (S5b build; pooled over the pauses of all repetitions)

| workload | mode | pauses | pauses that borrow | borrow MiB median / max | request pages median | free pages median | refused | give-back attempts | succeeded | complete (back to the kept capacity) | borrowed after the pause MiB median / max | peak capacity MiB (max of runs) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | nursery | 42 | 42 | 3.1 / 63.9 | 816 | 24 | 0 | 42 | 42 | 0 | 4.2 / 8.1 | 96 |
| binarytrees | mature | 42 | 42 | 3.1 / 63.9 | 816 | 20 | 0 | 42 | 42 | 0 | 4.2 / 7.9 | 96 |
| requests | nursery | 132 | 126 | 4.7 / 7.4 | 1215 | 8 | 0 | 144 | 135 | 0 | 20.6 / 33.6 | 222 |
| requests | mature | 126 | 120 | 5.8 / 7.4 | 1487 | 8 | 0 | 138 | 130 | 0 | 21.8 / 37.5 | 222 |
| listsuffix | nursery | 45 | 37 | 14.3 / 27.0 | 4654 | 1488 | 0 | 54 | 43 | 0 | 52.9 / 70.8 | 113 |
| listsuffix | mature | 54 | 48 | 14.3 / 29.7 | 4346 | 1192 | 0 | 63 | 53 | 0 | 48.0 / 87.3 | 133 |

## Cost of admission and give-back per pause (S5b build, microseconds)

| workload | mode | borrow us median / max | give-back us median / max (pauses with an attempt) | borrow phase us median (timer) | give_back phase us median (timer) | stw pause us median |
|---|---|---|---|---|---|---|
| binarytrees | nursery | 67 / 141 | 97 / 407 | 67 | 97 | 22820 |
| binarytrees | mature | 67 / 111 | 99 / 324 | 67 | 99 | 21855 |
| requests | nursery | 72 / 136 | 112 / 487 | 72 | 112 | 35947 |
| requests | mature | 75 / 165 | 119 / 1902 | 75 | 119 | 35768 |
| listsuffix | nursery | 91 / 208 | 161 / 759 | 92 | 161 | 518054 |
| listsuffix | mature | 91 / 168 | 164 / 573 | 91 | 164 | 428471 |
