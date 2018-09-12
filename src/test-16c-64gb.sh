#! /bin/bash

./clean

pushd CSharp
./run -m 8   -d 120 -l 3 | tee ../Output/CSharp/m8d120.txt
./run -m 16  -d 120 -l 3 | tee ../Output/CSharp/m16d120.txt
./run -m 32  -d 120 -l 3 | tee ../Output/CSharp/m32d120.txt
./run -m 64  -d 120 -l 3 | tee ../Output/CSharp/m64d120.txt

./run -t 75pct -m 8   -d 120 -l 3 | tee ../Output/CSharp/t75m8d120.txt
./run -t 75pct -m 16  -d 120 -l 3 | tee ../Output/CSharp/t75m16d120.txt
./run -t 75pct -m 32  -d 120 -l 3 | tee ../Output/CSharp/t75m32d120.txt
./run -t 75pct -m 64  -d 120 -l 3 | tee ../Output/CSharp/t75m64d120.txt
popd

pushd Go
./run -m 8   -d 120 -l 3 | tee ../Output/Go/m8d120.txt
./run -m 16  -d 120 -l 3 | tee ../Output/Go/m16d120.txt
./run -m 32  -d 120 -l 3 | tee ../Output/Go/m32d120.txt
./run -m 64  -d 120 -l 3 | tee ../Output/Go/m64d120.txt

./run -t 75pct -m 8   -d 120 -l 3 | tee ../Output/Go/t75m8d120.txt
./run -t 75pct -m 16  -d 120 -l 3 | tee ../Output/Go/t75m16d120.txt
./run -t 75pct -m 32  -d 120 -l 3 | tee ../Output/Go/t75m32d120.txt
./run -t 75pct -m 64  -d 120 -l 3 | tee ../Output/Go/t75m64d120.txt
popd

