@echo off
pushd "%~dp0"
dotnet run -p App\GCBurn.App.csproj -c Release -- %*
popd
