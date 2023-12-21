cd  src/tests
./build.sh -mono -debug riscv64 -ninja /p:KeepNativeSymbols=true -tree:JIT/ 

cd ../..