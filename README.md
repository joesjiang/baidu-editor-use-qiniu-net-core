### UEditor For ASP.Net Core Use Qiniu

- 此项目为UEditor提供文件管理;
- 后端服务使用 ASP.Net Core;
- 使用七牛提供的云存储;

### 运行示例
替换 QiniuFileController.cs 中的以下部分即可直接运行。

```csharp
  private readonly string accessKey = "xxx"; // 七牛提供的 AccessKey
  private readonly string secretKey = "xxx"; // 七牛提供的 SecretKey
  private readonly string bucket = "xx"; // 在七牛设置的空间名
  private readonly string suffix = "-s"; // 在七牛设置的缩略图后缀
  private readonly string domain = "http://xxx.com"; // 在七牛绑定的域名
```

### 在你自己的项目中使用步骤
1、在你的项目文件 project.json 中添加引用 DevZH.Qiniu。
```csharp
"dependencies": {
    "DevZH.Qiniu": "6.2.2.2",
    ...
  }
```
2、将示例项目的 wwwroot\lib\ueditor 整个文件夹复制到你项目的同样位置。
3、将示例项目的 QiniuFileController.cs 复制到你项目的同样位置并修改相关的设置信息。
4、新建任意测试视图，引入UEditor相关文件，即可正常使用。
```html
<div>
        <script type="text/plain" id="content" name="content" >
            UEditor 测试内容
        </script>
</div>
<script src="~/lib/ueditor/ueditor.config.js"></script>
<script src="~/lib/ueditor/ueditor.all.js"></script>
<script type="text/javascript">
    var ue = UE.getEditor('content'); 
</script>
```

### about
joes@live.com
