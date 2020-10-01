FROM mcr.microsoft.com/windows:1809 AS opticstudio_base

RUN mkdir resources
COPY ./resources ./resources

# download OpticStudio installer
ADD http://downloads.zemax.com/Downloads/ZOS20.2.2_2020-07-06.exe ./resources
ADD http://go.microsoft.com/fwlink/?linkid=863265 ./resources/NDP472-KB4054530-x86-x64-AllOS-ENU.exe

# install .Net Framework
RUN powershell ".\resources\NDP472-KB4054530-x86-x64-AllOS-ENU.exe /q /norestart"
RUN powershell "echo hey"
RUN powershell ".\resources\ZOS20.2.2_2020-07-06.exe /SP- /VERYSILENT /SUPPRESSMSGBOXES /LOADINF=ZOS20.2.2_2020-07-06.exe.inf /LOG=ZOS20.2.2_2020-07-06.exe.log"