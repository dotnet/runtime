| workload | heap x | mode | wall ms s4 | wall ms s5b | s5b/s4 wall (repetitions) | coll. s4 | coll. s5b | gc ms s4 | gc ms s5b | RSS MB s4 | RSS MB s5b | hiccup max ms s4 | hiccup max ms s5b |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | 3835 (3/3) | 3339 (3/3) | 0.87 (0.87, 0.85, 1.06) | 68/0 | 68/0 | 1287 | 1151 | 133 | 134 | 228 | 225 |
| binarytrees | 2 | nursery | 4339 (3/3) | 4144 (3/3) | 0.96 (0.94, 0.99, 0.88) | 81/0 | 85/0 | 2315 | 2067 | 144 | 145 | 371 | 341 |
| binarytrees | 2 | mature | 3892 (3/3) | 4248 (3/3) | 1.09 (1.23, 0.89, 1.05) | 74/0 | 84/0 | 1893 | 2275 | 143 | 145 | 402 | 338 |
| binarytrees | 4 | nonmoving | 2777 (3/3) | 2859 (3/3) | 1.03 (0.96, 1.14, 1.12) | 18/0 | 18/0 | 891 | 982 | 190 | 196 | 274 | 336 |
| binarytrees | 4 | nursery | 2188 (3/3) | 2829 (3/3) | 1.29 (1.32, 0.88, 1.29) | 18/0 | 20/0 | 423 | 866 | 111 | 186 | 68 | 222 |
| binarytrees | 4 | mature | 2493 (3/3) | 2609 (3/3) | 1.05 (0.86, 1.12, 1.11) | 19/0 | 19/0 | 731 | 924 | 196 | 190 | 263 | 252 |
| binarytrees | 8 | nonmoving | 2232 (3/3) | 2186 (3/3) | 0.98 (1.08, 1.08, 0.86) | 7/0 | 7/0 | 310 | 311 | 243 | 242 | 58 | 65 |
| binarytrees | 8 | nursery | 2144 (3/3) | 2120 (3/3) | 0.99 (0.98, 0.82, 1.05) | 8/0 | 8/0 | 367 | 404 | 188 | 188 | 93 | 88 |
| binarytrees | 8 | mature | 2541 (3/3) | 2219 (3/3) | 0.87 (0.83, 1.14, 0.74) | 8/0 | 8/0 | 443 | 399 | 188 | 188 | 118 | 88 |
| requests | 2 | nonmoving | 19142 (3/3) | 19601 (3/3) | 1.02 (1.01, 1.01, 1.03) | 515/0 | 528/0 | 5521 | 5690 | 150 | 149 | 35 | 34 |
| requests | 2 | nursery | fail (0/3) | fail (0/3) | - | fail/fail | fail/fail | fail | fail | fail | fail | fail | fail |
| requests | 2 | mature | fail (0/3) | fail (0/3) | - | fail/fail | fail/fail | fail | fail | fail | fail | fail | fail |
| requests | 4 | nonmoving | 21927 (3/3) | 21987 (3/3) | 1.00 (1.04, 0.99, 0.97) | 57/51 | 57/51 | 11649 | 11717 | 294 | 296 | 415 | 456 |
| requests | 4 | nursery | 8877 (3/3) | 8787 (3/3) | 0.99 (1.01, 0.94, 1.00) | 69/0 | 69/0 | 2219 | 2177 | 280 | 283 | 101 | 83 |
| requests | 4 | mature | 8962 (3/3) | 9140 (3/3) | 1.02 (1.09, 1.02, 1.00) | 66/0 | 66/0 | 2682 | 2696 | 302 | 306 | 262 | 292 |
| requests | 8 | nonmoving | 14210 (3/3) | 14590 (3/3) | 1.03 (1.08, 1.00, 1.04) | 22/16 | 22/16 | 5950 | 5864 | 509 | 508 | 476 | 671 |
| requests | 8 | nursery | 8456 (3/3) | 7855 (3/3) | 0.93 (0.99, 0.87, 0.98) | 25/0 | 25/0 | 1968 | 1858 | 508 | 508 | 310 | 249 |
| requests | 8 | mature | 8369 (3/3) | 7559 (3/3) | 0.90 (1.01, 0.85, 0.90) | 26/0 | 26/0 | 1884 | 1824 | 501 | 501 | 233 | 283 |
| listsuffix | 2 | nonmoving | fail (0/3) | fail (0/3) | - | fail/fail | fail/fail | fail | fail | fail | fail | fail | fail |
| listsuffix | 2 | nursery | fail (0/3) | fail (0/3) | - | fail/fail | fail/fail | fail | fail | fail | fail | fail | fail |
| listsuffix | 2 | mature | fail (0/3) | fail (0/3) | - | fail/fail | fail/fail | fail | fail | fail | fail | fail | fail |
| listsuffix | 4 | nonmoving | 12715 (3/3) | 12442 (3/3) | 0.98 (0.89, 1.03, 0.94) | 52/0 | 52/0 | 6186 | 5208 | 272 | 271 | 557 | 595 |
| listsuffix | 4 | nursery | 14949 (3/3) | 14928 (3/3) | 1.00 (1.30, 1.03, 0.93) | 51/0 | 52/0 | 7994 | 7716 | 248 | 249 | 912 | 856 |
| listsuffix | 4 | mature | 36407 (3/3) | 34438 (3/3) | 0.95 (0.95, 0.92, 0.65) | 73/0 | 71/0 | 28688 | 27048 | 263 | 264 | 677 | 630 |
| listsuffix | 8 | nonmoving | 18864 (3/3) | 19344 (3/3) | 1.03 (1.11, 0.99, 1.03) | 15/10 | 15/10 | 12301 | 12492 | 460 | 462 | 1097 | 1164 |
| listsuffix | 8 | nursery | 19638 (3/3) | 17020 (3/3) | 0.87 (0.93, 0.84, 0.96) | 17/0 | 17/0 | 13224 | 11261 | 457 | 459 | 1193 | 1072 |
| listsuffix | 8 | mature | 21731 (3/3) | 21007 (3/3) | 0.97 (0.98, 1.00, 0.97) | 19/0 | 19/0 | 14833 | 14324 | 453 | 453 | 1078 | 1166 |

cells compared=22; s5b/s4 geometric mean: wall 0.987 (min 0.867, max 1.293), gc ms 1.022, peak RSS 1.026 (min 0.972, max 1.671); all repetitions faster in 5 cells, all slower in 2, mixed (noise) in 15

failed or timed-out runs: 30
- s4 requests x2 nursery rep0: exit=-6 'Out of memory.'
- s4 requests x2 nursery rep1: exit=-6 'Out of memory.'
- s4 requests x2 nursery rep2: exit=-6 'Out of memory.'
- s5b requests x2 nursery rep0: exit=-6 'Out of memory.'
- s5b requests x2 nursery rep1: exit=-6 'Out of memory.'
- s5b requests x2 nursery rep2: exit=-6 'Out of memory.'
- s4 requests x2 mature rep0: exit=-6 'Out of memory.'
- s4 requests x2 mature rep1: exit=-6 'Out of memory.'
- s4 requests x2 mature rep2: exit=-6 'Out of memory.'
- s5b requests x2 mature rep0: exit=-6 'Out of memory.'
- s5b requests x2 mature rep1: exit=-6 'Out of memory.'
- s5b requests x2 mature rep2: exit=-6 'Out of memory.'
- s4 listsuffix x2 nonmoving rep0: exit=-6 'Out of memory.'
- s4 listsuffix x2 nonmoving rep1: exit=-6 'Out of memory.'
- s4 listsuffix x2 nonmoving rep2: exit=-6 'Out of memory.'
- s5b listsuffix x2 nonmoving rep0: exit=-6 'Out of memory.'
- s5b listsuffix x2 nonmoving rep1: exit=-6 'Out of memory.'
- s5b listsuffix x2 nonmoving rep2: exit=-6 'Out of memory.'
- s4 listsuffix x2 nursery rep0: exit=-6 'Out of memory.'
- s4 listsuffix x2 nursery rep1: exit=-6 'Out of memory.'
- s4 listsuffix x2 nursery rep2: exit=-6 'Out of memory.'
- s5b listsuffix x2 nursery rep0: exit=-6 'Out of memory.'
- s5b listsuffix x2 nursery rep1: exit=-6 'Out of memory.'
- s5b listsuffix x2 nursery rep2: exit=-6 'Out of memory.'
- s4 listsuffix x2 mature rep0: exit=-6 'Out of memory.'
- s4 listsuffix x2 mature rep1: exit=-6 'Out of memory.'
- s4 listsuffix x2 mature rep2: exit=-6 'Out of memory.'
- s5b listsuffix x2 mature rep0: exit=-6 'Out of memory.'
- s5b listsuffix x2 mature rep1: exit=-6 'Out of memory.'
- s5b listsuffix x2 mature rep2: exit=-6 'Out of memory.'
