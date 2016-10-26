using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Qiniu.Conf;
using Qiniu.IO;
using Qiniu.RS;
using Qiniu.RSF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BaiduEditorUseQiniu.NetCore.Controllers
{
    public class QiniuFileController : Controller
    {
        #region Config

        private readonly string accessKey = "AccessKey";     // 七牛提供的 AccessKey
        private readonly string secretKey = "SecretKey";     // 七牛提供的 SecretKey
        private readonly string bucket = "bucket";           // 在七牛设置的空间名
        private readonly string suffix = "";                 // 在七牛设置的缩略图后缀
        private readonly string domain = "http://xxx.com";   // 在七牛绑定的域名

        #endregion

        #region Private

        private JObject setting { set; get; }

        #endregion

        #region Init

        public QiniuFileController(IHostingEnvironment env)
        {
            Config.ACCESS_KEY = accessKey;
            Config.SECRET_KEY = secretKey;
            Config.RS_HOST = domain;

            setting = JObject.Parse(System.IO.File.ReadAllText(Path.Combine(env.WebRootPath, "lib", "ueditor", "config.json")));
        }

        #endregion

        public IActionResult GetToken(string fileType)
        {
            if (string.IsNullOrEmpty(fileType)) fileType = "file";

            var fileName = GetFileName(fileType);

            var token = new PutPolicy(bucket).Token();

            return Json(new
            {
                updomain = Config.UP_HOST,
                fsdomain = Config.RS_HOST,
                filename = fileName,
                token = token,
                suffix = suffix
            });
        }

        public async Task<IActionResult> Editor()
        {
            string action = Request.Query["action"];

            switch (action)
            {
                case "config":
                    return Json(setting);

                case "uploadimage":
                    return Json(await Upload("image"));

                case "uploadfile":
                    return Json(await Upload("file"));

                case "uploadvideo":
                    return Json(await Upload("video"));

                case "uploadscrawl":
                    return Json(await UploadScrawl("scrawl"));

                case "catchimage":
                    return Json(await CatchImage());

                case "listimage":
                    return Json(await ListFile("image"));

                case "listfile":
                    return Json(await ListFile("file"));

                default:
                    return Content("action 参数为空或者 action 不被支持。");
            }
        }

        #region Private

        private async Task<object> Upload(string uploadType = "image")
        {
            var filedName = setting[uploadType + "FieldName"].ToString();
            var maxSize = long.Parse(setting[uploadType + "MaxSize"].ToString());
            var allowTypes = setting[uploadType + "AllowFiles"].Select(p => p.ToString()).ToList();
            var file = Request.Form.Files.FirstOrDefault(p => p.Name == filedName);
            var ext = Path.GetExtension(file.FileName);

            if (file.Length > maxSize) return new { state = "文件大小超出限制。" };
            if (!allowTypes.Contains(ext)) return new { state = "不支持的文件类型。" };

            var fileName = await UploadToQiniu(file.OpenReadStream(), uploadType, uploadType != "image" ? ext : "");    // 图片文件没有加扩展名

            return new
            {
                state = "SUCCESS",
                url = string.Format("{0}/{1}", domain, fileName),
                title = Path.GetFileNameWithoutExtension(file.FileName),
                original = Path.GetFileNameWithoutExtension(file.FileName)
            };
        }

        private async Task<object> UploadScrawl(string uploadType = "scrawl")
        {
            var filedName = setting["scrawlFieldName"].ToString();
            var maxSize = long.Parse(setting["scrawlMaxSize"].ToString());
            var bytes = Convert.FromBase64String(Request.Form[filedName].ToString());

            if (bytes.Length > maxSize) return new { state = "文件大小超出限制。" };

            var fileName = await UploadToQiniu(new MemoryStream(bytes), uploadType);

            return new
            {
                state = "SUCCESS",
                url = string.Format("{0}/{1}", domain, fileName)
            };
        }

        private async Task<object> CatchImage()
        {
            var sources = Request.Form["source[]"];
            var list = new List<object>();

            foreach (var item in sources)
            {
                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, item))
                    {
                        using (var client = new HttpClient())
                        {
                            var response = await client.SendAsync(request);
                            var stream = await response.Content.ReadAsStreamAsync();
                            var fileName = await UploadToQiniu(stream, "image");

                            list.Add(new
                            {
                                state = "SUCCESS",
                                source = item,
                                url = string.Format("{0}/{1}", domain, fileName)
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    list.Add(new
                    {
                        state = e.Message,
                        source = item
                    });
                }
            }

            return new
            {
                state = "SUCCESS",
                list = list
            };
        }

        private async Task<object> ListFile(string prefix = "image")
        {
            var start = int.Parse(Request.Query["start"].ToString());
            var size = int.Parse(Request.Query["size"].ToString());

            var rsf = new RSFClient(bucket);
            var files = await rsf.ListPrefixAsync(bucket, prefix);

            var list = files.Items.Select(p => string.Format("{0}/{1}{2}", domain, p.Key, prefix == "image" ? suffix : "")).OrderByDescending(p => p).Skip(start).Take(size).ToList();

            return new
            {
                state = "SUCCESS",
                list = list == null ? null : list.Select(x => new { url = x }),
                start = start,
                size = size,
                total = files.Items.Count
            };
        }

        private async Task<string> UploadToQiniu(Stream stream, string fileType = "image", string ext = "")
        {
            var target = new IOClient();

            var key = GetFileName(fileType, ext);

            var result = await target.PutAsync(new PutPolicy(bucket).Token(), key, stream, null);

            return result.key;
        }

        private string GetFileName(string fileType, string ext = "")
        {
            var random = new Random(Guid.NewGuid().GetHashCode());

            return string.Format("{0}/{1}{2}{3}", fileType, DateTime.Now.ToString("yyyy/MM/dd/HHmmss"), random.Next(100000, 1000000), ext);
        }

        #endregion
    }
}