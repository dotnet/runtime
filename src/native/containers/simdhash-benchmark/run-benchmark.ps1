cl /GS- /O2 /std:c17 ./*.c ../dn-simdhash-u32-ptr.c ../dn-simdhash.c ../dn-vector.c ../dn-simdhash-string-ptr.c /DNO_CONFIG_H /DSIZEOF_VOID_P=8 /DNDEBUG /Fe:all-measurements.exe
./all-measurements.exe
