#! /bin/bash

pushd CSharp
./run -m 16  -d 120 -l 3 | tee ../Output/CSharp/m16d120.txt
./run -m 32  -d 120 -l 3 | tee ../Output/CSharp/m32d120.txt
./run -m 64  -d 120 -l 3 | tee ../Output/CSharp/m64d120.txt
./run -m 128 -d 120 -l 3 | tee ../Output/CSharp/m128d120.txt
./run -m 256 -d 120 -l 3 | tee ../Output/CSharp/m128d256.txt

./run -t 75pct -m 16  -d 120 -l 3 | tee ../Output/CSharp/t75m16d120.txt
./run -t 75pct -m 32  -d 120 -l 3 | tee ../Output/CSharp/t75m32d120.txt
./run -t 75pct -m 64  -d 120 -l 3 | tee ../Output/CSharp/t75m64d120.txt
./run -t 75pct -m 128 -d 120 -l 3 | tee ../Output/CSharp/t75m128d120.txt
./run -t 75pct -m 256 -d 120 -l 3 | tee ../Output/CSharp/t75m256d120.txt
popd

pushd Go
./run -m 16  -d 120 -l 3 | tee ../Output/Go/m16d120.txt
./run -m 32  -d 120 -l 3 | tee ../Output/Go/m32d120.txt
./run -m 64  -d 120 -l 3 | tee ../Output/Go/m64d120.txt
./run -m 128 -d 120 -l 3 | tee ../Output/Go/m128d120.txt
./run -m 256 -d 120 -l 3 | tee ../Output/Go/m128d256.txt

./run -t 75pct -m 16  -d 120 -l 3 | tee ../Output/Go/t75m16d120.txt
./run -t 75pct -m 32  -d 120 -l 3 | tee ../Output/Go/t75m32d120.txt
./run -t 75pct -m 64  -d 120 -l 3 | tee ../Output/Go/t75m64d120.txt
./run -t 75pct -m 128 -d 120 -l 3 | tee ../Output/Go/t75m128d120.txt
./run -t 75pct -m 256 -d 120 -l 3 | tee ../Output/Go/t75m256d120.txt
popd

