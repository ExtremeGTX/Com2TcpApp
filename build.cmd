@echo off
dotnet build --configuration release /p:VersionPrefix=1.2.0.0
dotnet build --configuration debug  /p:VersionPrefix=1.2.0.0