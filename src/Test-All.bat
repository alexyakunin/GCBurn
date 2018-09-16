@echo off
pushd "%~dp0"

call Clean.bat

set OUTPUT_SUFFIX=Default
set DURATION=120

:parse_loop
if "%1"=="" (
    goto :parsed
)
if "%1"=="-o" (
    set OUTPUT_SUFFIX=%2
    shift & shift
    goto :parse_loop
)
if "%1"=="-d" (
    set DURATION=%2
    shift & shift
    goto :parse_loop
    goto :loop
)
echo "Unknown option: %1."
exit 1
)
:parsed

call :run CSharp
call :run Go
goto :eof

:run
pushd %1
set OUTPUT='../../results/%1-%OUTPUT_SUFFIX%.txt'
echo Series: %1, writing output to %OUTPUT%
echo.

powershell -c "cmd /C run -l 3 -p f -r a -t 1 | tee %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r a -t 25pct | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r a -t 50pct | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r a -t 75pct | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r a | tee -a %OUTPUT%"

powershell -c "cmd /C run -l 3 -p m -r b -d %DURATION% -m 0 | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r b -d %DURATION% -m 1 | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r b -d %DURATION% -m 10pct | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r b -d %DURATION% -m 25pct | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r b -d %DURATION% -m 50pct | tee -a %OUTPUT%"

powershell -c "cmd /C run -l 3 -p m -r b -t 75pct -d %DURATION% -m 0 | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r b -t 75pct -d %DURATION% -m 1 | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r b -t 75pct -d %DURATION% -m 10pct | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r b -t 75pct -d %DURATION% -m 25pct | tee -a %OUTPUT%"
powershell -c "cmd /C run -l 3 -p m -r b -t 75pct -d %DURATION% -m 50pct | tee -a %OUTPUT%"

popd
goto :eof

