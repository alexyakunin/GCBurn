@echo off
pushd "%~dp0"

pushd CSharp
call Clean.bat %*
popd

popd
