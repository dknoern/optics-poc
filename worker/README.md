Dotnet application
---

create new app

```
dotnet new console -o App -n Worker
cd App
dotnet add package RabbitMQ.Client
dotnet restore
```

Add code to Program.cs and other files

run with:

```
dotnet run
```

publish with:

```
dotnet publish -c Release
docker build . -t job-wq-1
docker tag job-wq-1 dknoern/job-wq-1
docker push dknoern/job-wq-1
```


Dockerfile (adjacent to Program.cs):

```
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
COPY bin/Release/netcoreapp3.1/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "Worker.dll"]
```

