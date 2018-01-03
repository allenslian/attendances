@echo off
REM set x86/x64
set Platform=%1

if [%Platform%]==[] goto runx86 
if [%Platform%]==[x86] goto runx86 
if [%Platform%]==[x64] goto runx64

goto err

:runx86
%windir%\syswow64\regsvr32 /u /s %windir%\syswow64\zkemkeeper.dll 
del %windir%\syswow64\commpro.dll
del %windir%\syswow64\comms.dll
del %windir%\syswow64\rscagent.dll
del %windir%\syswow64\rscomm.dll
del %windir%\syswow64\tcpcomm.dll
del %windir%\syswow64\usbcomm.dll
del %windir%\syswow64\zkemkeeper.dll
del %windir%\syswow64\zkemsdk.dll
del %windir%\syswow64\plcommpro.dll
del %windir%\syswow64\plcomms.dll
del %windir%\syswow64\plrscagent.dll
del %windir%\syswow64\plrscomm.dll
del %windir%\syswow64\pltcpcomm.dll
goto end

:runx64
%windir%\system32\regsvr32 /u /s %windir%\system32\zkemkeeper.dll -u
del %windir%\system32\commpro.dll
del %windir%\system32\comms.dll
del %windir%\system32\rscagent.dll
del %windir%\system32\rscomm.dll
del %windir%\system32\tcpcomm.dll
del %windir%\system32\usbcomm.dll
del %windir%\system32\zkemkeeper.dll
del %windir%\system32\zkemsdk.dll
del %windir%\system32\plcommpro.dll
del %windir%\system32\plcomms.dll
del %windir%\system32\plrscagent.dll
del %windir%\system32\plrscomm.dll
del %windir%\system32\pltcpcomm.dll
goto end

:err
echo Not supported argument value.

:end
echo run complete.

