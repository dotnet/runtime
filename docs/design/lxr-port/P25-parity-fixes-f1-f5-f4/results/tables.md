### Wall time (s, median of 3), with the ratio to WKS of the same session

| program | arm | P23 | P24 baseline (c16710a) | after F1 | after F5 | after F4 (final) | final x WKS | final CPU s | final CPU x WKS |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | wks | 0.32 | 0.34 | 0.35 | 0.38 | 0.33 | 1.0 | 0.25 | 1.0 |
| binarytrees | server |  |  | 0.37 | 0.36 | 0.33 | 1.0 | 0.27 | 1.1 |
| binarytrees | lxr-nonmoving | 0.75 | 0.76 | 0.52 | 0.53 | 0.49 | 1.5 | 0.65 | 2.7 |
| binarytrees | lxr-nursery | 0.86 | 0.82 | 0.64 | 0.63 | 0.55 | 1.6 | 0.87 | 3.6 |
| requests | wks | 1.71 | 1.72 | 1.92 | 1.73 | 1.71 | 1.0 | 1.58 | 1.0 |
| requests | server |  |  | 1.92 | 1.82 | 1.73 | 1.0 | 1.82 | 1.2 |
| requests | lxr-nonmoving | 10.57 | 10.72 | 10.92 | 3.99 | 4.04 | 2.4 | 5.91 | 3.8 |
| requests | lxr-nursery | 4.2 | 4.32 | 4.33 | 4.07 | 4.16 | 2.4 | 8.82 | 5.6 |
| listsuffix | wks | 3.41 | 3.57 | 4.05 | 3.59 | 3.60 | 1.0 | 3.63 | 1.0 |
| listsuffix | server |  |  | 1.85 | 1.64 | 1.53 | 0.4 | 1.91 | 0.5 |
| listsuffix | lxr-nonmoving | 21.62 | 20.39 | 5.80 | 5.57 | 3.68 | 1.0 | 10.71 | 3.0 |
| listsuffix | lxr-nursery | 30.42 | 28.8 | 6.92 | 6.38 | 4.56 | 1.3 | 13.49 | 3.7 |

### Final build (F4): all columns

| program | arm | wall s | cpu s | gc s | pauses | full | RSS MiB | RefCount pause median ms (traced run) |
|---|---|---|---|---|---|---|---|---|
| binarytrees | wks | 0.33 | 0.25 | 0.08 | 87 | 0 | 54 |  |
| binarytrees | server | 0.33 | 0.27 | 0.05 | 68 | 0 | 67 |  |
| binarytrees | lxr-nonmoving | 0.49 | 0.65 | 0.08 | 17 | 0 | 92 | 5.18 |
| binarytrees | lxr-nursery | 0.55 | 0.87 | 0.13 | 18 | 0 | 92 | 5.64 |
| requests | wks | 1.71 | 1.58 | 0.23 | 277 | 0 | 83 |  |
| requests | server | 1.73 | 1.82 | 0.27 | 274 | 0 | 87 |  |
| requests | lxr-nonmoving | 4.04 | 5.91 | 0.37 | 58 | 0 | 155 | 15.98 |
| requests | lxr-nursery | 4.16 | 8.82 | 0.97 | 68 | 0 | 155 | 18.84 |
| listsuffix | wks | 3.60 | 3.63 | 2.01 | 191 | 0 | 66 |  |
| listsuffix | server | 1.53 | 1.91 | 0.62 | 51 | 0 | 74 |  |
| listsuffix | lxr-nonmoving | 3.68 | 10.71 | 0.91 | 53 | 0 | 107 | 33.5 |
| listsuffix | lxr-nursery | 4.56 | 13.49 | 1.53 | 51 | 0 | 103 | 40.91 |

### Number of Full pauses and median RefCount pause (ms), per fix

| program | arm | Full F1 | Full F5 | Full F4 | RefCount ms F1 | RefCount ms F5 | RefCount ms F4 |
|---|---|---|---|---|---|---|---|
| binarytrees | lxr-nonmoving | 0 | 0 | 0 | 5.72 | 6.25 | 5.18 |
| binarytrees | lxr-nursery | 0 | 0 | 0 | 6.4 | 7.49 | 5.64 |
| requests | lxr-nonmoving | 52 | 0 | 0 | 10.43 | 17.06 | 15.98 |
| requests | lxr-nursery | 0 | 0 | 0 | 19.74 | 19.54 | 18.84 |
| listsuffix | lxr-nonmoving | 0 | 0 | 0 | 69.53 | 69.92 | 33.5 |
| listsuffix | lxr-nursery | 0 | 0 | 0 | 85.01 | 84.63 | 40.91 |
