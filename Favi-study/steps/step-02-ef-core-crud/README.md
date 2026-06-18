# Шаг 2 — EF Core и база данных: CRUD над заметками

> **Это ЗАДАНИЕ, а не образец.** Код пишешь ты. Я даю цель, объяснение «зачем»,
> пошаговые подсказки и чек-лист. Где застрянешь — спрашивай, разберём.
> **Не копируй бездумно** — на каждом шаге проговаривай, что делает строка.

---

## Цель

Сделать API, который хранит **заметки** в настоящей базе данных и умеет: создать,
прочитать список, прочитать одну, обновить, удалить (это и есть **CRUD**).

К концу ты поймёшь, как C#-класс превращается в таблицу, как `_db.Notes.Where(...)`
становится SQL-запросом, и что такое миграция — то есть весь слой данных Favilonia.

## Зачем именно это

Работа бэкендера на 90% — «достать и сохранить данные». В Favilonia это оценки,
посещаемость, пользователи. Всё это живёт через **EF Core** (ORM): он переводит объекты
в строки таблиц, а LINQ — в SQL, чтобы ты не писал SQL руками. Поймёшь здесь на заметках —
поймёшь там на оценках.

> Базу возьмём **SQLite** — это файл на диске, ставить сервер не надо (в отличие от
> PostgreSQL в большой Favilonia). **Код EF Core абсолютно тот же** — отличается одна
> строка в настройке (`UseSqlite` вместо `UseNpgsql`). Это специально: учимся концепции,
> а не борьбе с установкой Postgres.

---

## Подготовка: создай проект

Из папки `steps/step-02-ef-core-crud/` выполни:

```bash
dotnet new web -f net8.0 -n NotesApi -o .
```

Добавь NuGet-пакеты EF Core (это библиотеки — они пропишутся в `.csproj`):

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.*
```

Установи инструмент для миграций (один раз на машину; если уже есть — пропусти):

```bash
dotnet tool install --global dotnet-ef
```

Проверь: `dotnet ef --version` должно что-то вывести.

---

## Шаги (пиши сам, подсказки — рядом)

### 2.1. Сущность `Note`
Создай файл `Entities/Note.cs` — обычный C#-класс. Он станет таблицей.
Поля-ориентиры (типы подбери сам): `Id` (Guid), `Title` (string), `Content` (string),
`CreatedAt` (DateTime).

> 💡 EF по соглашению считает свойство с именем `Id` первичным ключом автоматически.
> 💡 В Favilonia у сущностей есть общий базовый класс `BaseEntity` с `Id/CreatedAt/UpdatedAt` —
> у нас пока упростим и впишем поля прямо в `Note`.

### 2.2. `DbContext`
Создай `Data/AppDbContext.cs`. Класс наследуется от `DbContext`, принимает
`DbContextOptions<AppDbContext>` в конструкторе (передай в `base(...)`) и объявляет:

```csharp
public DbSet<Note> Notes => Set<Note>();
```

> 💡 `DbSet<Note>` = «таблица заметок». Через него идут все запросы: `_db.Notes...`.
> 💡 Сравни с Favilonia `AppDbContext` — там просто много `DbSet`-ов вместо одного.

### 2.3. Подключи базу в `Program.cs`
В ФАЗЕ 1 (помнишь две фазы из Шага 1?) зарегистрируй контекст и контроллеры:

```csharp
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=notes.db"));   // в Favilonia здесь UseNpgsql(...)
```

В ФАЗЕ 2 не забудь `app.MapControllers();` (как на Шаге 1).

> 💡 `"Data Source=notes.db"` — строка подключения. Для SQLite это просто имя файла,
> который EF создаст рядом с проектом.

### 2.4. Первая миграция
Миграция — это код, описывающий «как привести базу к нужной структуре». Сгенерируй и примени:

```bash
dotnet ef migrations add InitialCreate   # создаст папку Migrations/ с кодом схемы
dotnet ef database update                # применит её -> появится файл notes.db с таблицей
```

> 💡 Открой сгенерированный файл в `Migrations/` — увидишь метод `Up()` с `CreateTable`.
> Это ровно то, что лежит в `Backend/Favilonia.Infrastructure/Migrations/`.
> 💡 Поменяешь сущность (добавишь поле) → надо новая миграция `add` + `database update`.

### 2.5. CRUD-контроллер
Создай `Controllers/NotesController.cs`. Возьми за основу структуру `PingController`
из Шага 1, но добавь в конструктор зависимость (вспомни DI!):

```csharp
private readonly AppDbContext _db;
public NotesController(AppDbContext db) => _db = db;
```

Реализуй пять методов (сигнатуры — ориентир, тело пишешь сам):

| Метод | Маршрут | Что делает | EF-подсказка |
|---|---|---|---|
| `GetAll` | `GET /api/notes` | список всех заметок | `await _db.Notes.ToListAsync()` |
| `GetById` | `GET /api/notes/{id}` | одна по id или `404` | `await _db.Notes.FindAsync(id)` → если null, `NotFound()` |
| `Create` | `POST /api/notes` | создать, вернуть `201` | `_db.Notes.Add(note); await _db.SaveChangesAsync();` |
| `Update` | `PUT /api/notes/{id}` | изменить или `404` | найти, поменять поля, `SaveChangesAsync()` |
| `Delete` | `DELETE /api/notes/{id}` | удалить или `404` | `_db.Notes.Remove(note); SaveChangesAsync()` |

> 💡 Тело запроса (для POST/PUT) приходит JSON-ом и привязывается к параметру метода —
> просто прими `Note note` аргументом (на Шаге 3 заменим на отдельный DTO — поймёшь, зачем).
> 💡 `SaveChangesAsync()` — момент, когда изменения РЕАЛЬНО уходят в базу одним разом.
> Без него ничего не сохранится.
> 💡 `async/await` и `...Async()` методы: запрос к базе — операция «с ожиданием», поэтому
> методы делают `async`, а вызовы пишут с `await`. Если непонятно — это отдельная тема, спроси.

---

## Проверь руками

```bash
dotnet run
```

```bash
# создать
curl -X POST http://localhost:5xxx/api/notes -H "Content-Type: application/json" -d "{\"title\":\"Первая\",\"content\":\"привет\"}"
# список
curl http://localhost:5xxx/api/notes
# одна (подставь id из списка)
curl http://localhost:5xxx/api/notes/<id>
```

---

## Чек-лист «я понял этот шаг»

- [ ] Что такое ORM и что конкретно делает EF Core (что во что превращает)?
- [ ] Что такое `DbContext` и `DbSet<T>`?
- [ ] Что такое миграция и зачем она, если можно «просто создать таблицу руками»?
- [ ] Что произойдёт, если убрать `await _db.SaveChangesAsync()` из `Create`?
- [ ] Почему методы `async` и где здесь `await`?
- [ ] Какой код статуса уместен для каждой из 5 операций и почему?

Когда отвечаешь на всё — открой `Backend/Favilonia.Infrastructure/Data/AppDbContext.cs`
и любой метод в `GradesController` с `_db.Grades...`. Узнаёшь? Шаг закрыт. ✅

Заполни секцию Шага 2 в [../../LEARN.md](../../LEARN.md).

→ Дальше: Шаг 3 — DTO и проекция (открою, когда закроешь этот).
