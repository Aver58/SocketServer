 @echo off
 @rem �Ը�Ŀ¼��ÿ��*.prot�ļ���ת��
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