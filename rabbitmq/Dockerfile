FROM mcr.microsoft.com/windows/servercore:2004

####### PORTS ########
#Main rabbitmq port
EXPOSE 5672
#port mapper daemon (epmd)
EXPOSE 4369
#inet_dist_listen
EXPOSE 35197
#rabbitmq management console
EXPOSE 15672

#set the home directory for erlang so rabbit can find it easily
ENV ERLANG_HOME "c:\program files\erl10.7\erts-10.7"
ENV ERLANG_SERVICE_MANAGER_PATH "c:\program files\erl10.7\erts-10.7"

#install chocolatey
RUN @powershell -NoProfile -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"

#install rabbitmq
RUN choco install -y rabbitmq

#copy a config file over
COPY "rabbitmq.config" "C:\Users\ContainerAdministrator\AppData\Roaming\RabbitMQ\advanced.config"

#set the startup command to be rabbit
CMD ["C:/Program Files/RabbitMQ Server/rabbitmq_server-3.8.5/sbin/rabbitmq-server.bat"]