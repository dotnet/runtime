#### binarytrees-40 mode 00
| run | kind | n | median ms | p95 ms | max ms | total ms |
|---|---|---|---|---|---|---|
| base | RefCount | 16 | 83 | 118 | 118 | 1378 |
| base | InitialMark | 1 | 300 | 300 | 300 | 300 |
| base | FinalMark | 1 | 331 | 331 | 331 | 331 |
| s0b | RefCount | 16 | 84 | 248 | 248 | 1577 |
| s0b | InitialMark | 1 | 302 | 302 | 302 | 302 |
| s0b | FinalMark | 1 | 99 | 99 | 99 | 99 |
| s5e | RefCount | 16 | 83 | 256 | 256 | 1587 |
| s5e | InitialMark | 1 | 282 | 282 | 282 | 282 |
| s1d | RefCount | 16 | 87 | 226 | 226 | 1555 |
| s1d | InitialMark | 1 | 239 | 239 | 239 | 239 |
base: "checksum":14985972 "elapsed_ms":5289.286 "gc_count":18 "gc_ms":2009 "lxr_full":0 "peak_rss_kb":235084
s5e: "checksum":14985972 "elapsed_ms":4239.410 "gc_count":18 "gc_ms":1941 "lxr_full":0 "peak_rss_kb":229140
s1d: "checksum":14985972 "elapsed_ms":4165.145 "gc_count":18 "gc_ms":1867 "lxr_full":0 "peak_rss_kb":228164
#### binarytrees-40 mode 10
| run | kind | n | median ms | p95 ms | max ms | total ms |
|---|---|---|---|---|---|---|
| base | RefCount | 15 | 111 | 166 | 166 | 1777 |
| base | InitialMark | 1 | 327 | 327 | 327 | 327 |
| base | FinalMark | 1 | 240 | 240 | 240 | 240 |
| s0b | RefCount | 16 | 109 | 316 | 316 | 2077 |
| s0b | InitialMark | 1 | 345 | 345 | 345 | 345 |
| s0b | FinalMark | 1 | 109 | 109 | 109 | 109 |
| s5e | RefCount | 18 | 99 | 198 | 198 | 2028 |
| s5e | InitialMark | 1 | 155 | 155 | 155 | 155 |
| s1d | RefCount | 18 | 100 | 145 | 145 | 1894 |
| s1d | InitialMark | 1 | 146 | 146 | 146 | 146 |
base: "checksum":14985972 "elapsed_ms":5538.967 "gc_count":17 "gc_ms":2344 "lxr_full":0 "peak_rss_kb":235244
s5e: "checksum":14985972 "elapsed_ms":4577.917 "gc_count":19 "gc_ms":2182 "lxr_full":0 "peak_rss_kb":204292
s1d: "checksum":14985972 "elapsed_ms":4485.206 "gc_count":19 "gc_ms":2039 "lxr_full":0 "peak_rss_kb":203556
#### requests-84 mode 00
| run | kind | n | median ms | p95 ms | max ms | total ms |
|---|---|---|---|---|---|---|
| base | RefCount | 3 | 108 | 112 | 112 | 322 |
| base | InitialMark | 2 | 189 | 199 | 199 | 378 |
| base | FinalMark | 1 | 174 | 174 | 174 | 174 |
| base | Full | 52 | 311 | 351 | 628 | 16304 |
| s0b | RefCount | 4 | 104 | 180 | 180 | 489 |
| s0b | InitialMark | 1 | 179 | 179 | 179 | 179 |
| s0b | FinalMark | 1 | 176 | 176 | 176 | 176 |
| s0b | Full | 52 | 315 | 364 | 381 | 16546 |
| s5e | RefCount | 4 | 105 | 185 | 185 | 498 |
| s5e | InitialMark | 1 | 203 | 203 | 203 | 203 |
| s5e | FinalMark | 1 | 207 | 207 | 207 | 207 |
| s5e | Full | 52 | 311 | 345 | 373 | 16218 |
| s1d | RefCount | 4 | 103 | 129 | 129 | 437 |
| s1d | InitialMark | 1 | 211 | 211 | 211 | 211 |
| s1d | FinalMark | 1 | 213 | 213 | 213 | 213 |
| s1d | Full | 52 | 312 | 352 | 373 | 16466 |
base: "checksum":317308156512 "elapsed_ms":29226.658 "gc_count":58 "gc_ms":17177 "lxr_full":52 "peak_rss_kb":379024
s5e: "checksum":317308156512 "elapsed_ms":27978.683 "gc_count":58 "gc_ms":17125 "lxr_full":52 "peak_rss_kb":379364
s1d: "checksum":317308156512 "elapsed_ms":28232.343 "gc_count":58 "gc_ms":17326 "lxr_full":52 "peak_rss_kb":379692
#### requests-84 mode 10
| run | kind | n | median ms | p95 ms | max ms | total ms |
|---|---|---|---|---|---|---|
| base | RefCount | 3 | 137 | 142 | 142 | 413 |
| base | InitialMark | 2 | 233 | 268 | 268 | 466 |
| base | FinalMark | 1 | 238 | 238 | 238 | 238 |
| base | Full | 52 | 399 | 460 | 546 | 21065 |
| s0b | RefCount | 5 | 142 | 285 | 285 | 852 |
| s0b | Full | 53 | 402 | 445 | 486 | 21341 |
| s5e | RefCount | 63 | 122 | 141 | 268 | 8042 |
| s5e | InitialMark | 3 | 198 | 199 | 199 | 575 |
| s5e | FinalMark | 3 | 161 | 174 | 174 | 466 |
| s1d | RefCount | 64 | 120 | 135 | 202 | 7852 |
| s1d | InitialMark | 3 | 181 | 195 | 195 | 531 |
| s1d | FinalMark | 3 | 146 | 169 | 169 | 444 |
base: "checksum":317308156512 "elapsed_ms":34602.843 "gc_count":58 "gc_ms":22182 "lxr_full":52 "peak_rss_kb":378416
s5e: "checksum":317308156512 "elapsed_ms":17720.033 "gc_count":69 "gc_ms":9083 "lxr_full":0 "peak_rss_kb":368164
s1d: "checksum":317308156512 "elapsed_ms":17281.192 "gc_count":70 "gc_ms":8826 "lxr_full":0 "peak_rss_kb":368440
#### listsuffix-40 mode 00
| run | kind | n | median ms | p95 ms | max ms | total ms |
|---|---|---|---|---|---|---|
| base | RefCount | 60 | 337 | 400 | 461 | 19648 |
| base | InitialMark | 2 | 354 | 514 | 514 | 708 |
| base | FinalMark | 1 | 253 | 253 | 253 | 253 |
| s0b | RefCount | 65 | 375 | 411 | 443 | 23989 |
| s0b | InitialMark | 1 | 519 | 519 | 519 | 519 |
| s5e | RefCount | 59 | 152 | 414 | 419 | 12844 |
| s5e | InitialMark | 1 | 502 | 502 | 502 | 502 |
| s5e | FinalMark | 2 | 227 | 238 | 238 | 454 |
| s1d | RefCount | 66 | 117 | 351 | 367 | 12863 |
| s1d | InitialMark | 1 | 465 | 465 | 465 | 465 |
| s1d | FinalMark | 2 | 235 | 236 | 236 | 469 |
base: "checksum":301262627042 "elapsed_ms":29078.355 "gc_count":63 "gc_ms":20609 "lxr_full":0 "peak_rss_kb":318284
s5e: "checksum":301262627042 "elapsed_ms":23931.481 "gc_count":65 "gc_ms":14035 "lxr_full":0 "peak_rss_kb":351348
s1d: "checksum":301262627042 "elapsed_ms":23071.762 "gc_count":72 "gc_ms":14058 "lxr_full":0 "peak_rss_kb":316068
#### listsuffix-40 mode 10
| run | kind | n | median ms | p95 ms | max ms | total ms |
|---|---|---|---|---|---|---|
| base | RefCount | 62 | 571 | 605 | 1074 | 35228 |
| base | InitialMark | 2 | 493 | 685 | 685 | 986 |
| s0b | RefCount | 81 | 586 | 713 | 782 | 41010 |
| s0b | InitialMark | 1 | 734 | 734 | 734 | 734 |
| s0b | FinalMark | 1 | 710 | 710 | 710 | 710 |
| s5e | RefCount | 247 | 85 | 190 | 338 | 23469 |
| s5e | InitialMark | 1 | 406 | 406 | 406 | 406 |
| s5e | FinalMark | 2 | 217 | 342 | 342 | 435 |
| s1d | RefCount | 234 | 60 | 176 | 288 | 17679 |
| s1d | InitialMark | 1 | 332 | 332 | 332 | 332 |
| s1d | FinalMark | 2 | 191 | 313 | 313 | 382 |
base: "checksum":301262627042 "elapsed_ms":44560.247 "gc_count":64 "gc_ms":36213 "lxr_full":0 "peak_rss_kb":315128
s5e: "checksum":301262627042 "elapsed_ms":38725.167 "gc_count":252 "gc_ms":24826 "lxr_full":0 "peak_rss_kb":271460
s1d: "checksum":301262627042 "elapsed_ms":31305.039 "gc_count":239 "gc_ms":18823 "lxr_full":0 "peak_rss_kb":253284
