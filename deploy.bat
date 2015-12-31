@echo off

set HOME=%~dp0

set BUILD_PATH=%HOME%\build\win2tiz\
del /q %BUILD_PATH%\*.exe
del /q %BUILD_PATH%\*.dll

cd win2tiz\bin\Release
copy win2tiz.exe %BUILD_PATH%
copy *.dll %BUILD_PATH%
cd %HOME%
cd mongcc\bin\Release
copy mongcc.exe %BUILD_PATH%
cd %HOME%
copy readme.txt %BUILD_PATH%

pause
