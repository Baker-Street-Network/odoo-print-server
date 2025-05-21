# Odoo Print Server
This is the server that runs on the Windows computer to do the printing.

## Publish
To publish this project use this command:

`dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`

Just a note for future projects, to enable building this Windows only project on Linux, you will need to set this in the csproj file:

```
<EnableWindowsTargeting>true</EnableWindowsTargeting>
```
