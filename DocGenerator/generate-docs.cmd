setlocal
set libName=Phaeyz.Jfif
set repoUrl=https://github.com/Phaeyz/Jfif
dotnet run ..\%libName%\bin\Debug\net9.0\%libName%.dll ..\docs --source %repoUrl%/blob/main/%libName% --namespace %libName% --clean
endlocal