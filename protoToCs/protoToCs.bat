 @echo off
 @rem 对该目录下每个*.prot文件做转换
 set curdir=%cd%
 set protoPath=%curdir%\proto\
 set generate=%curdir%\generate\
 echo %curdir%
 echo %protoPath%

 for /r %%j in (*.proto) do ( 
	echo %%j
	protogen -i:"%%j" -o:%generate%%%~nj.cs 
 )
 pause