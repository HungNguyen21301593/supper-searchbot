FROM mcr.microsoft.com/dotnet/runtime:6.0
RUN apt-get update
# RUN apt-get upgrade
RUN apt-get --assume-yes install wget
RUN wget https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
RUN apt --assume-yes install ./google-chrome-stable_current_amd64.deb

WORKDIR /app
COPY ["./bin/Debug/net6.0","/app"]
ENTRYPOINT ["dotnet", "supper-searchbot.dll"]