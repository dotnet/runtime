// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

int ManagedSum8(int, int, int, int, int, int, int, int);
int ManagedSum9(int, int, int, int, int, int, int, int, int);
long long ManagedSum16(long long, long long, long long, long long, long long, long long, long long, long long,
                       long long, long long, long long, long long, long long, long long, long long, long long);
void ManagedVoid12(int, int, int, int, int, int, int, int, int, int, int, int);

int call_sum8(void) { return ManagedSum8(1, 2, 3, 4, 5, 6, 7, 8); }
int call_sum9(void) { return ManagedSum9(1, 2, 3, 4, 5, 6, 7, 8, 9); }
long long call_sum16(void) { return ManagedSum16(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16); }
void call_void12(void) { ManagedVoid12(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12); }
