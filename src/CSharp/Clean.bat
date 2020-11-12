@echo off
pushd "%~dp0"
rmdir /S /Q App\bin 2>nul
rmdir /S /Q App\obj 2>nul
rmdir /S /Q Main\bin 2>nul
rmdir /S /Q Main\obj 2>nul
rmdir /S /Q Benchmarking.Common\bin 2>nul
rmdir /S /Q Benchmarking.Common\obj 2>nul
rmdir /S /Q Tests\bin 2>nul
rmdir /S /Q Tests\obj 2>nul
popd
