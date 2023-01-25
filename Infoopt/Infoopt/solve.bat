@echo off
for /l %%i in (1, 1, %1) do (start cmd.exe /C "dotnet run -v q") && timeout 1
