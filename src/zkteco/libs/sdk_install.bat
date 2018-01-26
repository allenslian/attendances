@echo off
REM set x86/x64
set Platform=%1
set CurDir=%~dp0

if [%Platform%]==[] goto failure 
if [%Platform%]==[x86] goto runx86 
if [%Platform%]==[x64] goto runx64

goto failure

:runx86
copy "%CurDir%x86\*.dll" %windir%\syswow64\
%windir%\syswow64\regsvr32.exe /i /s %windir%\syswow64\zkemkeeper.dll
goto success

:runx64
copy "%CurDir%x64\*.dll" %windir%\system32\
%windir%\system32\regsvr32.exe /i /s %windir%\system32\zkemkeeper.dll
goto success

:failure
echo Not supported argument value.
echo Fail to install sdk.
goto end

:success
echo Install sdk succssfully.
goto end

:end