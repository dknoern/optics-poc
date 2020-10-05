FROM mcr.microsoft.com/dotnet/framework/sdk:4.8 AS build

WORKDIR /app

# copy everything else and build app
COPY . .

WORKDIR /app/
RUN dotnet build --configuration Release /p:Platform=x64 -o out









FROM dknoern/opticstudio:ZOS20.2.2_2020-07-06 AS opticstudio


SHELL ["powershell"]

RUN mkdir app
COPY --from=build /app/out ./app

COPY ZOSKubApp/resources/launchapp.ps1 ./app

WORKDIR /app
SHELL ["powershell"]
RUN ((Get-Content -Path 'C:\Program Files (x86)\Common Files\Aladdin Shared\HASP\hasplm.ini') -replace 'serversearchinterval = 30',"""serversearchinterval = 30`nserveraddr = x.x.x.x""") | Set-Content -Path 'C:\Program Files (x86)\Common Files\Aladdin Shared\HASP\hasplm.ini'

RUN powershell -Command Set-Executionpolicy Unrestricted -Force

#ENTRYPOINT [ "HPCKubApp.exe" ]









