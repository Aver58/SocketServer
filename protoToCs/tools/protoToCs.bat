
 @rem �Ը�Ŀ¼��ÿ��*.prot�ļ���ת��
 for %%j in (*.proto) do ( 
  echo %%j
 

protogen -i:"%%j" -o:%%~nj.cs 

)