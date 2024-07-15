# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p3rpc.AlternatingOpenings/*" -Force -Recurse
dotnet publish "./p3rpc.AlternatingOpenings.csproj" -c Release -o "$env:RELOADEDIIMODS/p3rpc.AlternatingOpenings" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location