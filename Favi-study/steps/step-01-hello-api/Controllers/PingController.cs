// Подключаем типы из ASP.NET Core MVC: [ApiController], ControllerBase, IActionResult и т.д.
using Microsoft.AspNetCore.Mvc;

// namespace — логический "адрес" класса внутри проекта. На поведение не влияет,
// но помогает не путать классы с одинаковыми именами. В Favilonia такая же схема:
// Favilonia.API.Controllers.
namespace HelloApi.Controllers;

// [ApiController] — атрибут, который говорит: "это контроллер REST API".
// Он включает удобства: автоматическую валидацию модели, привязку параметров и т.п.
// (разберём детальнее на следующих шагах, когда появятся входные данные).
[ApiController]
// [Route("...")] задаёт URL, по которому доступен этот контроллер.
// "[controller]" — это шаблон: ASP.NET подставит имя класса без слова "Controller".
// PingController -> "ping". Итог: базовый адрес контроллера = /api/ping.
[Route("api/[controller]")]
// Контроллер — это обычный C#-класс, который наследуется от ControllerBase.
// ControllerBase даёт готовые методы-помощники: Ok(), NotFound(), BadRequest() и др.
public class PingController : ControllerBase
{
    // [HttpGet] — этот метод отвечает на HTTP-метод GET по адресу контроллера (/api/ping).
    // Запрос "GET /api/ping" -> ASP.NET вызовет именно этот метод.
    [HttpGet]
    public IActionResult Get()
    {
        // Ok(...) формирует HTTP-ответ со статусом 200 (успех) и телом в формате JSON.
        // Анонимный объект { message = ... } сериализуется в {"message":"..."}.
        return Ok(new { message = "pong", time = DateTime.UtcNow });
    }

    // Второй эндпоинт с ДОБАВКОЙ к адресу: [HttpGet("hello/{name}")]
    // "{name}" — параметр прямо в URL. Запрос "GET /api/ping/hello/Kenik"
    // -> ASP.NET вытащит "Kenik" из URL и положит в аргумент name.
    [HttpGet("hello/{name}")]
    public IActionResult Hello(string name)
    {
        return Ok(new { message = $"Привет, {name}!" });
    }
}
