# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p3rpc.classconstructor/*" -Force -Recurse
dotnet publish "./p3rpc.classconstructor.csproj" -c Release -o "$env:RELOADEDIIMODS/p3rpc.classconstructor" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location