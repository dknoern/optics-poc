
dotnet publish -c Release
docker build . -t zos-worker
docker tag zos-worker dknoern/zos-worker
docker push dknoern/zos-worker
