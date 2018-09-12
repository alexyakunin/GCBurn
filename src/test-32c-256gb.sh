#! /bin/bash

pushd CSharp
./run -m 16  -d 120 -l 3 | tee ../out/csharp/m16d120.txt
./run -m 32  -d 120 -l 3 | tee ../out/csharp/m32d120.txt
./run -m 64  -d 120 -l 3 | tee ../out/csharp/m64d120.txt
./run -m 128 -d 120 -l 3 | tee ../out/csharp/m128d120.txt
./run -m 256 -d 120 -l 3 | tee ../out/csharp/m128d256.txt

./run -t 75pct -m 16  -d 120 -l 3 | tee ../out/csharp/t75m16d120.txt
./run -t 75pct -m 32  -d 120 -l 3 | tee ../out/csharp/t75m32d120.txt
./run -t 75pct -m 64  -d 120 -l 3 | tee ../out/csharp/t75m64d120.txt
./run -t 75pct -m 128 -d 120 -l 3 | tee ../out/csharp/t75m128d120.txt
./run -t 75pct -m 256 -d 120 -l 3 | tee ../out/csharp/t75m256d120.txt
popd

pushd Go
./run -m 16  -d 120 -l 3 | tee ../out/go/m16d120.txt
./run -m 32  -d 120 -l 3 | tee ../out/go/m32d120.txt
./run -m 64  -d 120 -l 3 | tee ../out/go/m64d120.txt
./run -m 128 -d 120 -l 3 | tee ../out/go/m128d120.txt
./run -m 256 -d 120 -l 3 | tee ../out/go/m128d256.txt

./run -t 75pct -m 16  -d 120 -l 3 | tee ../out/go/t75m16d120.txt
./run -t 75pct -m 32  -d 120 -l 3 | tee ../out/go/t75m32d120.txt
./run -t 75pct -m 64  -d 120 -l 3 | tee ../out/go/t75m64d120.txt
./run -t 75pct -m 128 -d 120 -l 3 | tee ../out/go/t75m128d120.txt
./run -t 75pct -m 256 -d 120 -l 3 | tee ../out/go/t75m256d120.txt
popd

