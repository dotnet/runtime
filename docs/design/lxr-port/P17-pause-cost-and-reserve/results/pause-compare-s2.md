| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | layer 1: n, median, max ms | S2: n, median, max ms |
|---|---|---|---|---|---|
| binarytrees | non-moving | RefCount | 45, 83, 119 | 48, 83, 252 | 48, 57, 243 |
| binarytrees | non-moving | InitialMark | 3, 265, 266 | 3, 256, 257 | 3, 210, 227 |
| binarytrees | non-moving | FinalMark | 3, 167, 168 | 3, 85, 90 | 3, 39, 40 |
| binarytrees | nursery copy | RefCount | 46, 107, 338 | 54, 96, 186 | 54, 73, 162 |
| binarytrees | nursery copy | InitialMark | 3, 322, 335 | 2, 287, 301 | 2, 280, 307 |
| binarytrees | nursery copy | FinalMark | 3, 227, 228 | 1, 110, 110 | 2, 140, 143 |
| requests | non-moving | RefCount | 12, 103, 178 | 12, 101, 127 | 12, 68, 89 |
| requests | non-moving | InitialMark | 3, 178, 194 | 2, 204, 213 | 2, 136, 160 |
| requests | non-moving | FinalMark | 2, 184, 184 | 2, 207, 208 | 2, 155, 158 |
| requests | non-moving | Full | 157, 308, 662 | 158, 301, 392 | 158, 257, 330 |
| requests | nursery copy | RefCount | 11, 137, 278 | 191, 119, 193 | 187, 95, 157 |
| requests | nursery copy | InitialMark | 4, 200, 270 | 9, 185, 243 | 9, 166, 193 |
| requests | nursery copy | FinalMark | 3, 238, 249 | 9, 136, 169 | 9, 121, 149 |
| requests | nursery copy | Full | 156, 387, 716 | - | - |
| listsuffix | non-moving | RefCount | 176, 323, 670 | 175, 104, 419 | 165, 65, 383 |
| listsuffix | non-moving | InitialMark | 4, 512, 522 | 3, 463, 600 | 3, 414, 486 |
| listsuffix | non-moving | FinalMark | 2, 260, 280 | 3, 215, 225 | 3, 186, 199 |
| listsuffix | nursery copy | RefCount | 187, 552, 1177 | 180, 457, 666 | 175, 435, 663 |
| listsuffix | nursery copy | InitialMark | 4, 680, 693 | 3, 467, 472 | 3, 415, 434 |
| listsuffix | nursery copy | FinalMark | - | 6, 276, 471 | 5, 161, 256 |
