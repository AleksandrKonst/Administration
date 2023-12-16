# docker-compose.yml 
1. [**Postgresql**](#01)
2. [**MySql**](#02)
3. [**Команды**](#03)

## 01. Postgresql<a name="01"></a>

### 1.1 Два сервиса
Данный пример включает два сервиса в одной сети, бд, ui для бд, и реверс прокси:
![image](https://github.com/AleksandrKonst/Administration/assets/40522320/8688d920-df78-4672-8210-a24c98d3c025)

![image](https://github.com/AleksandrKonst/Administration/assets/40522320/658427bd-333d-444c-aaff-cafbad8b8afb)

#### Dockerfile
##### Service 1
Файл для вашего первого API
- создаем докер файл для сервиса
```Dockerfile
# базовый образ для сборки проекта
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
# дирректиория в которой будут выполняться последующие команды в контейнере
WORKDIR /app
# внутренний порт
EXPOSE 80

# Копируем в контейнер csproj
COPY WebApplication1/WebApplication1.csproj WebApplication1/
# Востанавливаем зависимости с помощью него
RUN dotnet restore "WebApplication1/WebApplication1.csproj" 

# Копируем весь проект
COPY . ./
# Билдим проект в релиз
RUN dotnet publish WebApplication1/WebApplication1.csproj -c Release -o out

# Используем базовый образ ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
# дирректиория в которой будут выполняться последующие команды в контейнере
WORKDIR /app/WebApplication1
# Коппируем
COPY --from=build /app/out .
# Точка старта
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
```
##### Service 2
Файл для вашего второго API. 
- Все тоже самое.
```Dockerfile
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
```

#### init.sql
SQL файл для создания таблиц
- Если нам необходима база данных, то мы создаем init файл в `корне проекта (см. проект)` для того чтобы создать необходимые нам таблицы и заполнить их данными
```sql
CREATE TABLE weather (
    id SERIAL NOT NULL,
    name character varying NOT NULL
);

INSERT INTO weather (id, name) VALUES (1, 'Пасмурно'), (2, 'Облачно');
```

#### init.sql
Файл конфигурации Nginx
- Если нам необходим реверс прокси, создаем файл с настройками в `корне проекта (см. проект)`
```sql
CREATE TABLE weather (
    id SERIAL NOT NULL,
    name character varying NOT NULL
);

INSERT INTO weather (id, name) VALUES (1, 'Пасмурно'), (2, 'Облачно');
```

#### docker-compose.yml

```docker-compose.yml
services:
  proxy:
    container_name: nginx_container
    image: nginx:latest
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    ports:
      - "80:80"
    networks:
      - services-network
  
  api:
    container_name: api_container
    image: webapplication1
    build:
      context: .
      dockerfile: WebApplication1/Dockerfile
    ports:
      - "8080:80"
    networks:
      - services-network
    depends_on:
      - "db"
      - "apitwo"
    environment:
      DATABASE_CONNECT: Server=postgres_container;Port=5432;Database=DataBaseName;User Id=postgres;Password=1243

  apitwo:
    container_name: apitwo_container
    image: webapplication2
    build:
      context: .
      dockerfile: WebApplication2/Dockerfile
    networks:
      - services-network

  db:
    container_name: postgres_container
    image: postgres
    restart: always
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: 1243
      POSTGRES_DB: DataBaseName
      PGDATA: "/var/lib/postgresql/data/pgdata"
    volumes:
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
      - test-data-db:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    networks:
      - services-network
  
  pgadmin:
    container_name: pgadmin_container
    image: dpage/pgadmin4:7.2
    restart: always
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@miit.com
      PGADMIN_DEFAULT_PASSWORD: 1243
    volumes:
      - pgadmin-data:/var/lib/pgadmin
    ports:
      - "5050:80"
    networks:
      - services-network
    depends_on:
      - "db"

networks:
  services-network:
    driver: bridge

volumes:
  test-data-db:
  pgadmin-data:
```

#### ASP.NET
```c#
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataBaseNameContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("DATABASE_CONNECT")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
```
