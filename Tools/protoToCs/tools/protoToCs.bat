
 @rem 对该目录下每个*.prot文件做转换
 for %%j in (*.proto) do ( 
  echo %%j
 

protogen -i:"%%j" -o:%%~nj.cs 

)