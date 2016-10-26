using Microsoft.AspNetCore.Mvc;

namespace BaiduEditorUseQiniu.NetCore.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Picture = "http://files.weistores.com/image/2016/10/26/223059363031,http://files.weistores.com/image/2016/10/26/223112375065";
            ViewBag.Content = "<p>欢迎使用UEditor，这是一段默认的测试文本。内容是后端初始化的。</p>";

            return View();
        }

        [HttpPost]
        public IActionResult Index(string content, string picture)
        {
            return Json(new { picture = picture, content = content });
        }
    }
}