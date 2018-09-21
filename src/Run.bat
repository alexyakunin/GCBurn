@echo off
pushd "%~dp0"

pushd CSharp
call Run.bat %*
popd

echo.

pushd Go
call Run.bat %*
popd

popd
