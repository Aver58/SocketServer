 @echo off
 set curdir=%cd%
 set PROTO_FILES_DIRECTORY=%curdir%\proto\
 set OUTPUT_DIRECTORY=%curdir%\generate\
 echo %curdir%
 echo %PROTO_FILES_DIRECTORY%
 echo %OUTPUT_DIRECTORY%
 
 protoc --csharp_out=%OUTPUT_DIRECTORY% --proto_path=%PROTO_FILES_DIRECTORY% %PROTO_FILES_DIRECTORY%/*.proto
 pause