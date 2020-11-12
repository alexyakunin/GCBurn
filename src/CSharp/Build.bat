@echo off
pushd "%~dp0"
dotnet build -c Release
popd
