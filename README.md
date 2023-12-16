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

#### nginx.conf
Файл конфигурации Nginx
- Если нам необходим реверс прокси, создаем файл с настройками в `корне проекта (см. проект)`
```nginx
worker_processes 4;

events { worker_connections 1024; }

http {  
    server {
        listen 80;
 
        location /firstapi/ {
            proxy_pass http://api_container:80/;
        }
        
        location /secondapi/ {
            proxy_pass http://apitwo_container:80/;
        }
    }
}
```

#### docker-compose.yml

```docker-compose.yml
services:
  # Если надо сделать с реверс прокси (если нет удаляем)
  proxy:
    # Имя контейнера
    container_name: nginx_container
    # Образ
    image: nginx:latest
    # Маунтим нам конфигурационный файл
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    # Внешний порт
    ports:
      - "80:80"
    # Одна сеть между конйнерами
    networks:
      - services-network

  # Если надо развернуть asp.net сервис (если нет удаляем)
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
    # Не запуститься пока не стартанут это контейнеры
    depends_on:
      - "db"
      - "apitwo"
    # Передаем переменую окружения со строкой подколючения к бд (если нет удаляем)
    environment:
      DATABASE_CONNECT: Server=postgres_container;Port=5432;Database=DataBaseName;User Id=postgres;Password=1243

  # Если надо развернуть второй asp.net сервис (если нет удаляем)
  apitwo:
    container_name: apitwo_container
    image: webapplication2
    build:
      context: .
      dockerfile: WebApplication2/Dockerfile
    networks:
      - services-network

  # База данных (если нет удаляем)
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
      # Маунтим init файл бд
      - ./init.sql:/docker-entrypoint-initdb.d/init.sql
      # Для сохранения базы на вашем пк
      - test-data-db:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    networks:
      - services-network

  # UI для бд (если нет удаляем)
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

# Сети для связи с контейнерами
networks:
  services-network:
    driver: bridge

# volumes верхнего уровня (именнованные), храняться на вашем пк для сохранения данных
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
// Строка которую мы передали в наш сервис получаеться тут (см. проект)
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
#### ASP.NET Controller
Пример получения данных со второго API (ЛАБА №5)
```c#
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly DataBaseNameContext _context;
    private HttpClient _client;
    
    public WeatherController(DataBaseNameContext context)
    {
        //Создаем HTTP клиент
        _context = context;
        _client = new HttpClient();
        _client.BaseAddress = new Uri("http://apitwo_container:80/");
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

    }
    
    [HttpGet]
    [Produces("application/hal+json")]
    public async Task<IActionResult> Get()
    {
        return Ok(await _context.Weathers.ToListAsync());
    }

    [HttpGet("{id}")]
    [Produces("application/hal+json")]
    public async Task<IActionResult> Get(int id)
    {
        return Ok(await _context.Weathers.FindAsync(id));
    }
    
    [HttpGet("network")]
    public IActionResult NetworkRequest()
    {
        //Запрос данных
        var response = _client.GetAsync("WeatherForecast").Result;
        return Ok(response.Content.ReadAsStringAsync().Result);
    } 
}
```

## 02. MySql<a name="02"></a>
С MySql буквально все тоже самое, меняем провайдер в .NET сервисе и заменяем постгрес на MySql в docker-compose.yml
```docker-compose.yml
mysql:
  container_name: mysql_container
  image: mysql
  restart: always
  ports:
   - "3306:3306"
  environment:
    MYSQL_ROOT_PASSWORD: 1243
    MYSQL_USER: root
    MYSQL_PASSWORD: 1243
    MYSQL_DATABASE: DataBaseName
  volumes:
   - ./init.sql:/docker-entrypoint-initdb.d/init.sql

# UI если надо
adminer:
    image: adminer
    restart: always
    ports:
      - 8080:8080
```

## 03. Команды<a name="03"></a>
### Команды необходимы для запуска
в директори с файлом (можно пропустить и сразу выполнить run)
```cmd
docker-compose build
```
Стартуем
```cmd
docker-compose up
```
### Краткий справочник
