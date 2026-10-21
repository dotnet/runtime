## Pause time per kind

| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | S3: n, median, max ms | S4: n, median, max ms |
|---|---|---|---|---|---|
| binarytrees | non-moving | RefCount | 46, 83.2, 327 | 48, 31.7, 232 | 48, 17.5, 201 |
| binarytrees | non-moving | InitialMark | 3, 252.2, 268 | 3, 198.1, 199 | 3, 175.9, 182 |
| binarytrees | non-moving | FinalMark | 3, 153.5, 177 | 3, 54.0, 88 | 3, 25.2, 53 |
| binarytrees | nursery copy | RefCount | 46, 105.5, 345 | 54, 31.3, 156 | 55, 16.9, 85 |
| binarytrees | nursery copy | InitialMark | 3, 315.4, 326 | 3, 210.5, 218 | 1, 241.6, 242 |
| binarytrees | nursery copy | FinalMark | 3, 233.0, 253 | 3, 88.5, 115 | 1, 34.3, 34 |
| requests | non-moving | RefCount | 11, 106.2, 182 | 12, 45.0, 68 | 12, 19.5, 42 |
| requests | non-moving | InitialMark | 4, 191.4, 200 | 3, 91.8, 101 | - |
| requests | non-moving | FinalMark | 3, 191.0, 193 | 3, 137.2, 143 | - |
| requests | non-moving | Full | 155, 340.7, 666 | 156, 246.1, 280 | 162, 199.5, 249 |
| requests | nursery copy | RefCount | 11, 142.4, 282 | 191, 53.4, 106 | 192, 25.2, 78 |
| requests | nursery copy | InitialMark | 3, 220.5, 273 | 9, 103.5, 113 | 9, 76.8, 125 |
| requests | nursery copy | FinalMark | 2, 253.6, 264 | 9, 78.7, 90 | 9, 23.2, 48 |
| requests | nursery copy | Full | 158, 405.1, 803 | - | - |
| listsuffix | non-moving | RefCount | 172, 334.0, 841 | 162, 60.1, 395 | 172, 47.2, 405 |
| listsuffix | non-moving | InitialMark | 4, 499.6, 520 | 4, 422.4, 430 | 3, 425.1, 475 |
| listsuffix | non-moving | FinalMark | 3, 97.9, 231 | 4, 159.8, 209 | 3, 70.7, 160 |
| listsuffix | nursery copy | RefCount | 189, 560.3, 1141 | 160, 324.6, 492 | 171, 64.3, 455 |
| listsuffix | nursery copy | InitialMark | 4, 662.8, 681 | 3, 399.9, 401 | 4, 426.2, 448 |
| listsuffix | nursery copy | FinalMark | - | 5, 140.4, 319 | 4, 102.1, 274 |

## Median time of the phases that S4 changes, RefCount pauses (ms)

| workload | mode | build | RefCount pauses | stw_pause median ms | release_epoch median ms | reclaim_nursery median ms | sweep_blocks median ms | scan_heap_statistics median ms | post_drain median ms |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | s3 | 48 | 31.73 | 12.93 | 0.01 | 12.92 | 1.86 | 1.26 |
| binarytrees | non-moving | s4 | 48 | 17.51 | 0.53 | 0.52 | 0.00 | 0.00 | 1.03 |
| binarytrees | nursery copy | s3 | 54 | 31.27 | 11.61 | 0.01 | 11.42 | 2.60 | 1.33 |
| binarytrees | nursery copy | s4 | 55 | 16.94 | 0.53 | 0.43 | 0.00 | 0.00 | 1.54 |
| requests | non-moving | s3 | 12 | 45.04 | 14.17 | 0.01 | 14.16 | 10.42 | 5.00 |
| requests | non-moving | s4 | 12 | 19.53 | 0.61 | 0.61 | 0.00 | 0.00 | 14.73 |
| requests | nursery copy | s3 | 191 | 53.44 | 19.24 | 0.01 | 18.72 | 9.57 | 3.17 |
| requests | nursery copy | s4 | 192 | 25.21 | 1.35 | 0.80 | 0.00 | 0.00 | 6.19 |
| listsuffix | non-moving | s3 | 162 | 60.08 | 2.75 | 0.01 | 2.74 | 10.77 | 25.34 |
| listsuffix | non-moving | s4 | 172 | 47.15 | 0.20 | 0.19 | 0.00 | 0.00 | 28.79 |
| listsuffix | nursery copy | s3 | 160 | 324.61 | 2.76 | 0.01 | 2.75 | 13.16 | 0.00 |
| listsuffix | nursery copy | s4 | 171 | 64.27 | 0.21 | 0.20 | 0.00 | 0.00 | 26.21 |

## Share of the changed phases in all RefCount pause time (%)

| workload | mode | build | release_epoch % of all RefCount pause time | reclaim_nursery % of all RefCount pause time | sweep_blocks % of all RefCount pause time | scan_heap_statistics % of all RefCount pause time |
|---|---|---|---|---|---|---|
| binarytrees | non-moving | s3 | 29.1 | 0.0 | 29.1 | 4.9 |
| binarytrees | non-moving | s4 | 1.9 | 1.9 | 0.0 | 0.0 |
| binarytrees | nursery copy | s3 | 29.5 | 0.0 | 27.8 | 7.1 |
| binarytrees | nursery copy | s4 | 4.6 | 2.1 | 0.0 | 0.0 |
| requests | non-moving | s3 | 30.1 | 0.0 | 30.1 | 20.8 |
| requests | non-moving | s4 | 2.5 | 2.5 | 0.0 | 0.0 |
| requests | nursery copy | s3 | 35.7 | 0.0 | 34.4 | 17.1 |
| requests | nursery copy | s4 | 5.4 | 2.9 | 0.0 | 0.0 |
| listsuffix | non-moving | s3 | 2.2 | 0.0 | 2.2 | 8.5 |
| listsuffix | non-moving | s4 | 0.3 | 0.3 | 0.0 | 0.0 |
| listsuffix | nursery copy | s3 | 1.4 | 0.0 | 1.2 | 5.3 |
| listsuffix | nursery copy | s4 | 0.4 | 0.1 | 0.0 | 0.0 |

## Per-run totals

| workload | mode | build | runs | wall ms (median) | gc ms (median) | pauses (median) | checksums |
|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | acc | 3 | 4934 | 1750 | 17 | 1 |
| binarytrees | non-moving | s3 | 3 | 3157 | 968 | 18 | 1 |
| binarytrees | non-moving | s4 | 3 | 2914 | 679 | 18 | 1 |
| binarytrees | nursery copy | acc | 3 | 5526 | 2311 | 17 | 1 |
| binarytrees | nursery copy | s3 | 3 | 3123 | 1039 | 20 | 1 |
| binarytrees | nursery copy | s4 | 3 | 2621 | 384 | 19 | 1 |
| requests | non-moving | acc | 3 | 31225 | 18542 | 58 | 1 |
| requests | non-moving | s3 | 3 | 24004 | 13276 | 58 | 1 |
| requests | non-moving | s4 | 3 | 21689 | 10526 | 58 | 1 |
| requests | nursery copy | acc | 3 | 34947 | 22349 | 58 | 1 |
| requests | nursery copy | s3 | 3 | 11341 | 4122 | 70 | 1 |
| requests | nursery copy | s4 | 3 | 9799 | 2174 | 71 | 1 |
| listsuffix | non-moving | acc | 3 | 25815 | 17166 | 61 | 1 |
| listsuffix | non-moving | s3 | 3 | 13226 | 6590 | 59 | 1 |
| listsuffix | non-moving | s4 | 3 | 11937 | 4775 | 59 | 1 |
| listsuffix | nursery copy | acc | 3 | 43570 | 35557 | 64 | 1 |
| listsuffix | nursery copy | s3 | 3 | 19555 | 12974 | 57 | 1 |
| listsuffix | nursery copy | s4 | 3 | 17620 | 10925 | 63 | 1 |
