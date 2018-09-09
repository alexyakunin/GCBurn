@echo off
pushd "%~dp0"
rmdir /S /Q App\bin
rmdir /S /Q App\obj
popd
