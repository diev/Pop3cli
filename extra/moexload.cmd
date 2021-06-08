@echo off
rem 2021-06-08

set home=%~dp0
rem Входной буфер для очистки
set in=%home%..\IN
rem Выходное хранилище чистых файлов для АБС
set out=%home%..\LOAD
rem Справочник сертификатов
set spr=%home%..\SPR

%home%Pop3cli.exe

rem 866!
set profile=****

set dd=%date:~-10,2%
set mm=%date:~-7,2%
set yyyy=%date:~-4%

set lymd=%yyyy%%mm%%dd%

rem logs\2021\202106\20210608_this.log
set log=%home%logs\%lymd:~0,4%\%lymd:~0,6%
if not exist %log%\nul md %log% 2>nul
set log=%log%\%lymd%_%~n0.log

rem Справочник отозванных обновляется в GUI раз в месяц... - надо подумать над этим
rem if exist %AppData%\Roaming\Validata\xcs\local.gdbm copy /y %AppData%\Roaming\Validata\xcs\local.gdbm %spr%

if not exist temp\nul md temp 2>nul

rem BugFix to reload if certificates fail
for %%i in (%out%\*.p7?) do move/y "%%~i" %in%\

for %%i in (%in%\*.*) do call :in1 "%%~i"
goto :eof

:in1
copy /y "%~1" temp\
echo %date% %time:~0,8% %~nx1>>%log%

for %%f in (temp\*.p7a) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 xpki1utl.exe -profile %profile% -decrypt -in "%%~f" -out "temp\%%~nf.p7s"
 if exist "temp\%%~nf.p7s" del "%%~f"
)

for %%f in (temp\*.p7e) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 xpki1utl.exe -profile %profile% -decrypt -in "%%~f" -out "temp\%%~nf"
 if exist "temp\%%~nf" del "%%~f"
)

for %%f in (temp\*.zip) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 7z.exe e -y "%%~f" -otemp\
 if errorlevel 0 del "%%~f"
)

for %%f in (temp\*.p7s) do (
 rem echo %date% %time:~0,8% %%~nxf>>%log%
 xpki1utl.exe -profile %profile% -verify -in "%%~f" -out "temp\%%~nf" -delete 1
 if exist "temp\%%~nf" del "%%~f"
)

move /y temp\*.* %out%\
move /y "%~1" %in%\BAK\
goto :eof
