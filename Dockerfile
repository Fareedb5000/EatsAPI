FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EatsAPI.csproj", "./"]
RUN dotnet restore "./EatsAPI.csproj"

COPY . .
RUN dotnet publish "./EatsAPI.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:7860
EXPOSE 7860
USER $APP_UID

ENTRYPOINT ["dotnet", "EatsAPI.dll"]
