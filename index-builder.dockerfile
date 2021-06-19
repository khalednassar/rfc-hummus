FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine

COPY . /src

RUN cd /src && dotnet publish -o /built && cp ./run.sh /built/

ENTRYPOINT [ "/built/run.sh" ]