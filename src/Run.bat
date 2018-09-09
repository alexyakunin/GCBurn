@echo off
pushd "%~dp0"

pushd Go
call Run.bat %*
popd

pushd CSharp
call Run.bat %*
popd

popd
