FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DogMeet.csproj .
RUN dotnet restore
COPY Components ./Components
COPY wwwroot ./wwwroot
COPY Program.cs .
RUN dotnet publish DogMeet.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "RotaryClubCentral.dll"]
