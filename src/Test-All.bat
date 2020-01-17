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

call :run CSharp CSharp
call :run Go Go
rem call :run CSharp-Batch CSharp run 0
rem call :run CSharp-WorkstationGC CSharp run-wgc
goto :eof

:run
set variant=%1
set language=%2
set runner=%3
if "%runner%"=="" set runner=run
set lmode=%4
if "%lmode%"=="" set lmode=3
set output='../../results/%variant%-%OUTPUT_SUFFIX%.txt'

pushd %language%
echo Series: %variant%, writing output to %output%
echo.

powershell -c "cmd /C %runner% -l %lmode% -p f -r a -t 1 2>&1 | tee %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r a -t 25pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r a -t 50pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r a -t 75pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r a 2>&1 | tee -a %output%"

powershell -c "cmd /C %runner% -l %lmode% -p m -r b -d %DURATION% -m 0 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -d %DURATION% -m 1 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -d %DURATION% -m 10pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -d %DURATION% -m 25pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -d %DURATION% -m 50pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -d %DURATION% -m 75pct 2>&1 | tee -a %output%"

powershell -c "cmd /C %runner% -l %lmode% -p m -r b -t 75pct -d %DURATION% -m 0 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -t 75pct -d %DURATION% -m 1 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -t 75pct -d %DURATION% -m 10pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -t 75pct -d %DURATION% -m 25pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -t 75pct -d %DURATION% -m 50pct 2>&1 | tee -a %output%"
powershell -c "cmd /C %runner% -l %lmode% -p m -r b -t 75pct -d %DURATION% -m 75pct 2>&1 | tee -a %output%"

popd
goto :eof

