@echo off
rem set GOGC=70

pushd "%~dp0"
go run app.go %*
popd
