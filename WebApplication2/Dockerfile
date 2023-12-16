FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
EXPOSE 80

COPY WebApplication2/WebApplication2.csproj WebApplication2/
RUN dotnet restore "WebApplication2/WebApplication2.csproj"

COPY . ./
RUN dotnet publish WebApplication2/WebApplication2.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app/WebApplication2
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "WebApplication2.dll"]