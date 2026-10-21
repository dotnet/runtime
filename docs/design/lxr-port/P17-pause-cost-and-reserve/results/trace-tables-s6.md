## Pause time per kind

| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | S5b: n, median, max ms | S6: n, median, max ms |
|---|---|---|---|---|---|
| binarytrees | non-moving | RefCount | 48, 85.3, 306 | 48, 18.1, 188 | 48, 3.8, 187 |
| binarytrees | non-moving | InitialMark | 3, 258.6, 274 | 3, 183.6, 187 | 3, 212.2, 216 |
| binarytrees | non-moving | FinalMark | 3, 47.6, 51 | 2, 40.3, 51 | 1, 18.1, 18 |
| binarytrees | nursery copy | RefCount | 48, 111.5, 351 | 54, 17.3, 117 | 54, 5.3, 44 |
| binarytrees | nursery copy | InitialMark | 3, 345.6, 349 | 2, 227.4, 256 | - |
| binarytrees | nursery copy | FinalMark | 3, 135.7, 175 | 1, 50.9, 51 | - |
| binarytrees | mature copy | RefCount | 46, 108.7, 339 | 54, 18.2, 112 | 54, 5.1, 122 |
| binarytrees | mature copy | InitialMark | 3, 345.4, 481 | 2, 264.6, 275 | 2, 196.1, 199 |
| binarytrees | mature copy | FinalMark | 3, 258.5, 274 | - | - |
| requests | non-moving | RefCount | 11, 107.1, 188 | 12, 21.1, 49 | 12, 6.7, 31 |
| requests | non-moving | InitialMark | 4, 192.7, 210 | 3, 72.3, 134 | 1, 56.0, 56 |
| requests | non-moving | FinalMark | 3, 196.4, 204 | 3, 139.6, 140 | 1, 100.2, 100 |
| requests | non-moving | Full | 155, 340.5, 663 | 156, 224.1, 303 | 160, 202.0, 460 |
| requests | nursery copy | RefCount | 11, 145.6, 294 | 189, 25.4, 82 | 189, 13.8, 75 |
| requests | nursery copy | InitialMark | 4, 225.6, 276 | 9, 76.2, 114 | 9, 79.5, 127 |
| requests | nursery copy | FinalMark | 3, 243.4, 267 | 9, 30.5, 49 | 9, 13.4, 27 |
| requests | nursery copy | Full | 156, 422.0, 918 | - | - |
| requests | mature copy | RefCount | 11, 143.6, 317 | 180, 25.8, 81 | 182, 14.1, 97 |
| requests | mature copy | InitialMark | 4, 278.0, 287 | 9, 121.5, 158 | 9, 90.0, 160 |
| requests | mature copy | FinalMark | 3, 281.7, 318 | 9, 237.3, 280 | 9, 193.8, 297 |
| requests | mature copy | Full | 154, 510.2, 1096 | - | - |
| listsuffix | non-moving | RefCount | 186, 363.1, 574 | 191, 48.2, 464 | 195, 38.0, 409 |
| listsuffix | non-moving | InitialMark | 4, 542.8, 568 | 3, 437.6, 484 | 3, 429.7, 443 |
| listsuffix | non-moving | FinalMark | 1, 276.7, 277 | 4, 118.6, 232 | 5, 163.1, 208 |
| listsuffix | nursery copy | RefCount | 186, 734.3, 1459 | 170, 372.8, 691 | 207, 325.5, 581 |
| listsuffix | nursery copy | InitialMark | 4, 686.5, 741 | 3, 436.5, 483 | 3, 442.7, 452 |
| listsuffix | nursery copy | FinalMark | - | 3, 253.1, 295 | 3, 207.8, 266 |
| listsuffix | mature copy | RefCount | 188, 608.0, 1146 | 206, 76.2, 736 | 195, 51.9, 328 |
| listsuffix | mature copy | InitialMark | 4, 696.6, 720 | 4, 367.9, 449 | 4, 409.4, 424 |
| listsuffix | mature copy | FinalMark | - | 4, 141.6, 158 | 6, 130.3, 159 |

## Median time of the phases that S6 changes, RefCount pauses (ms)

| workload | mode | build | RefCount pauses | stw_pause median ms | root_capture median ms | scan0 median ms | scan_lease_finalizers median ms | scan_queue_finalizers median ms | scan_trace_finalizers median ms |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | S5b tip | 48 | 18.14 | 13.59 | 13.41 | 13.34 | 0.00 | 0.00 |
| binarytrees | non-moving | S6 | 48 | 3.80 | 0.21 | 0.07 | 0.00 | 0.00 | 0.00 |
| binarytrees | nursery copy | S5b tip | 54 | 17.33 | 12.22 | 12.03 | 11.95 | 0.00 | 0.00 |
| binarytrees | nursery copy | S6 | 54 | 5.26 | 0.21 | 0.07 | 0.00 | 0.00 | 0.00 |
| binarytrees | mature copy | S5b tip | 54 | 18.19 | 12.39 | 12.19 | 12.11 | 0.00 | 0.00 |
| binarytrees | mature copy | S6 | 54 | 5.08 | 0.21 | 0.07 | 0.00 | 0.00 | 0.00 |
| requests | non-moving | S5b tip | 12 | 21.06 | 14.47 | 14.30 | 14.22 | 0.00 | 0.00 |
| requests | non-moving | S6 | 12 | 6.66 | 0.19 | 0.06 | 0.00 | 0.00 | 0.00 |
| requests | nursery copy | S5b tip | 189 | 25.40 | 12.14 | 11.96 | 11.89 | 0.00 | 0.00 |
| requests | nursery copy | S6 | 189 | 13.85 | 0.20 | 0.07 | 0.00 | 0.00 | 0.00 |
| requests | mature copy | S5b tip | 180 | 25.81 | 12.31 | 12.13 | 12.06 | 0.00 | 0.00 |
| requests | mature copy | S6 | 182 | 14.10 | 0.20 | 0.07 | 0.00 | 0.00 | 0.00 |
| listsuffix | non-moving | S5b tip | 191 | 48.21 | 11.45 | 11.30 | 11.22 | 0.00 | 0.00 |
| listsuffix | non-moving | S6 | 195 | 37.98 | 0.22 | 0.07 | 0.00 | 0.00 | 0.00 |
| listsuffix | nursery copy | S5b tip | 170 | 372.85 | 11.38 | 11.19 | 11.10 | 0.00 | 0.00 |
| listsuffix | nursery copy | S6 | 207 | 325.53 | 0.26 | 0.08 | 0.00 | 0.00 | 0.00 |
| listsuffix | mature copy | S5b tip | 206 | 76.24 | 11.52 | 11.34 | 11.27 | 0.00 | 0.00 |
| listsuffix | mature copy | S6 | 195 | 51.88 | 0.21 | 0.07 | 0.00 | 0.00 | 0.00 |

## Share of the changed phases in all RefCount pause time (%)

| workload | mode | build | root_capture % of all RefCount pause time | scan0 % of all RefCount pause time | scan_lease_finalizers % of all RefCount pause time |
|---|---|---|---|---|---|
| binarytrees | non-moving | S5b tip | 48.3 | 47.5 | 47.2 |
| binarytrees | non-moving | S6 | 1.5 | 0.5 | 0.0 |
| binarytrees | nursery copy | S5b tip | 51.6 | 50.8 | 50.5 |
| binarytrees | nursery copy | S6 | 2.7 | 0.8 | 0.0 |
| binarytrees | mature copy | S5b tip | 51.4 | 50.7 | 50.3 |
| binarytrees | mature copy | S6 | 1.7 | 0.6 | 0.0 |
| requests | non-moving | S5b tip | 57.4 | 56.6 | 56.3 |
| requests | non-moving | S6 | 1.9 | 0.6 | 0.0 |
| requests | nursery copy | S5b tip | 44.2 | 43.6 | 43.3 |
| requests | nursery copy | S6 | 1.3 | 0.5 | 0.0 |
| requests | mature copy | S5b tip | 43.7 | 43.1 | 42.8 |
| requests | mature copy | S6 | 1.3 | 0.4 | 0.0 |
| listsuffix | non-moving | S5b tip | 9.4 | 9.2 | 9.2 |
| listsuffix | non-moving | S6 | 0.2 | 0.1 | 0.0 |
| listsuffix | nursery copy | S5b tip | 3.5 | 3.5 | 3.4 |
| listsuffix | nursery copy | S6 | 0.1 | 0.0 | 0.0 |
| listsuffix | mature copy | S5b tip | 5.9 | 5.7 | 5.7 |
| listsuffix | mature copy | S6 | 0.3 | 0.1 | 0.0 |

## The same phases in tracing pauses (Full and FinalMark), median ms

| workload | mode | build | tracing pauses | stw_pause median ms | root_capture median ms | scan0 median ms | scan_lease_finalizers median ms | scan_queue_finalizers median ms | scan_trace_finalizers median ms |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | S5b tip | 2 | 40.34 | 5.22 | 5.08 | 4.98 | 4.39 | 0.00 |
| binarytrees | non-moving | S6 | 1 | 18.11 | 0.28 | 0.11 | 0.00 | 0.03 | 0.03 |
| binarytrees | nursery copy | S5b tip | 1 | 50.87 | 5.78 | 5.59 | 5.52 | 5.37 | 0.00 |
| requests | non-moving | S5b tip | 159 | 223.46 | 14.39 | 14.17 | 14.08 | 11.84 | 0.00 |
| requests | non-moving | S6 | 161 | 201.83 | 0.26 | 0.08 | 0.01 | 0.02 | 0.02 |
| requests | nursery copy | S5b tip | 9 | 30.52 | 8.58 | 8.40 | 8.33 | 7.97 | 0.00 |
| requests | nursery copy | S6 | 9 | 13.40 | 0.22 | 0.07 | 0.00 | 0.01 | 0.01 |
| requests | mature copy | S5b tip | 9 | 237.34 | 11.45 | 11.30 | 11.23 | 9.54 | 0.00 |
| requests | mature copy | S6 | 9 | 193.83 | 0.22 | 0.07 | 0.00 | 0.02 | 0.02 |
| listsuffix | non-moving | S5b tip | 4 | 118.61 | 9.89 | 9.69 | 9.62 | 8.90 | 0.00 |
| listsuffix | non-moving | S6 | 5 | 163.08 | 0.24 | 0.08 | 0.00 | 0.04 | 0.03 |
| listsuffix | nursery copy | S5b tip | 3 | 253.10 | 10.70 | 10.47 | 10.39 | 8.56 | 0.00 |
| listsuffix | nursery copy | S6 | 3 | 207.77 | 0.24 | 0.08 | 0.00 | 0.03 | 0.03 |
| listsuffix | mature copy | S5b tip | 4 | 141.56 | 7.01 | 6.79 | 6.70 | 5.59 | 0.00 |
| listsuffix | mature copy | S6 | 6 | 130.29 | 0.23 | 0.07 | 0.00 | 0.02 | 0.02 |

## Per-run totals

| workload | mode | build | runs | wall ms (median) | gc ms (median) | pauses (median) | tracing pauses (median) | checksums |
|---|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | accepted | 3 | 5148 | 1941 | 18 | 1 | 1 |
| binarytrees | non-moving | S5b tip | 3 | 2756 | 698 | 18 | 1 | 1 |
| binarytrees | non-moving | S6 | 3 | 2584 | 492 | 18 | 1 | 1 |
| binarytrees | nursery copy | accepted | 3 | 5903 | 2616 | 18 | 1 | 1 |
| binarytrees | nursery copy | S5b tip | 3 | 2645 | 684 | 19 | 1 | 1 |
| binarytrees | nursery copy | S6 | 3 | 2244 | 167 | 18 | 0 | 1 |
| binarytrees | mature copy | accepted | 3 | 5659 | 2387 | 17 | 1 | 1 |
| binarytrees | mature copy | S5b tip | 3 | 2799 | 863 | 20 | 1 | 1 |
| binarytrees | mature copy | S6 | 3 | 2638 | 557 | 20 | 1 | 1 |
| requests | non-moving | accepted | 3 | 30819 | 18268 | 58 | 54 | 1 |
| requests | non-moving | S5b tip | 3 | 23089 | 12135 | 58 | 53 | 1 |
| requests | non-moving | S6 | 3 | 22260 | 10981 | 58 | 54 | 1 |
| requests | nursery copy | accepted | 3 | 36594 | 23446 | 58 | 54 | 1 |
| requests | nursery copy | S5b tip | 3 | 9661 | 2205 | 70 | 3 | 1 |
| requests | nursery copy | S6 | 3 | 9089 | 1417 | 70 | 3 | 1 |
| requests | mature copy | accepted | 3 | 40714 | 27560 | 57 | 53 | 1 |
| requests | mature copy | S5b tip | 3 | 9625 | 2696 | 67 | 3 | 1 |
| requests | mature copy | S6 | 3 | 9418 | 1942 | 67 | 3 | 1 |
| listsuffix | non-moving | accepted | 3 | 31982 | 23087 | 64 | 2 | 1 |
| listsuffix | non-moving | S5b tip | 3 | 13001 | 5671 | 67 | 1 | 1 |
| listsuffix | non-moving | S6 | 3 | 16501 | 9117 | 71 | 1 | 1 |
| listsuffix | nursery copy | accepted | 3 | 54535 | 45992 | 63 | 2 | 1 |
| listsuffix | nursery copy | S5b tip | 3 | 28789 | 22349 | 59 | 1 | 1 |
| listsuffix | nursery copy | S6 | 3 | 24403 | 17622 | 65 | 1 | 1 |
| listsuffix | mature copy | accepted | 3 | 46424 | 37977 | 64 | 2 | 1 |
| listsuffix | mature copy | S5b tip | 3 | 12996 | 5738 | 67 | 1 | 1 |
| listsuffix | mature copy | S6 | 3 | 13025 | 5430 | 71 | 1 | 1 |
