#! /bin/bash

./run -m 16  -d 120 -l 3 | tee m16d120.txt
./run -m 32  -d 120 -l 3 | tee m32d120.txt
./run -m 64  -d 120 -l 3 | tee m64d120.txt
./run -m 128 -d 120 -l 3 | tee m128d120.txt
./run -m 256 -d 120 -l 3 | tee m128d256.txt

./run -t 75pct -m 16  -d 120 -l 3 | tee t75m16d120.txt
./run -t 75pct -m 32  -d 120 -l 3 | tee t75m32d120.txt
./run -t 75pct -m 64  -d 120 -l 3 | tee t75m64d120.txt
./run -t 75pct -m 128 -d 120 -l 3 | tee t75m128d120.txt
./run -t 75pct -m 256 -d 120 -l 3 | tee t75m256d120.txt
