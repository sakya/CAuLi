rd /s /q .\dist\win-x64

dotnet clean CAuLi.sln -c Release
dotnet publish CAuLi.sln -c Release --runtime win-x64 -p:PublishReadyToRun=true --self-contained --output .\dist\win-x64