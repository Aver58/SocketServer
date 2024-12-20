@REM author:Manistein
@REM since:2019.04.26
@REM desc:dump sproto 2 cs

set RPCProtoCS=%~dp0..\\..\\Proto\\ProtoCS\\
set RPCProtoSchema=%~dp0..\\..\\Proto\\ProtoFile\\

set WorkingPath=.\\lib\\
pushd %WorkingPath%

lua.exe ..\\sproto2cs.lua %RPCProtoSchema% %RPCProtoCS%

pause