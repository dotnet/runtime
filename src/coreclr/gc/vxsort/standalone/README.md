
mkdir build
cd build
cmake -DCMAKE_BUILD_TYPE=Release ..
make -j12

./demo/Project_demo 250
./simple_bench/Project_demo 250
