@echo off
pushd "%~dp0"
rmdir /S /Q App\bin 2>nul
rmdir /S /Q App\obj 2>nul
popd
