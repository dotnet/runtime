# Update corerror.h and mscorurt.rc

Execute in repo root:

### Windows:

```
.\dotnet.cmd run --project .\src\coreclr\inc\genheaders\ -- .\src\coreclr\inc\corerror.xml .\src\coreclr\pal\prebuilt\inc\corerror.h .\src\coreclr\pal\prebuilt\corerror\mscorurt.rc
```

### Unix:

```
./dotnet.sh run --project ./src/coreclr/inc/genheaders/ -- ./src/coreclr/inc/corerror.xml ./src/coreclr/pal/prebuilt/inc/corerror.h ./src/coreclr/pal/prebuilt/corerror/mscorurt.rc
```
