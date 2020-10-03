FROM mcr.microsoft.com/windows:1809 AS opticstudio_base

RUN mkdir resources
COPY ./resources ./resources

# download OpticStudio installer
ADD http://downloads.zemax.com/Downloads/ZOS20.1_2020-01-21.exe ./resources

# download .Net Framework
ADD http://go.microsoft.com/fwlink/?linkid=863265 ./resources/NDP472-KB4054530-x86-x64-AllOS-ENU.exe

# install .Net Framework
RUN powershell ".\resources\NDP472-KB4054530-x86-x64-AllOS-ENU.exe /q /norestart"
