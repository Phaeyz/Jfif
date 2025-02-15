setlocal
set lib=Phaeyz.Jfif
set repoUrl=https://github.com/Phaeyz/Jfif
dotnet run ..\%lib%\bin\Debug\net9.0\%lib%.dll ..\docs --source %repoUrl%/blob/main/%lib% --namespace %lib% --clean
endlocal