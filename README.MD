# 动态页面转静态页面 （Dynamic page to static page）

一个文件搞定动态页面转静态页面，支持.NET CORE 3.1 ，支持MVC和PageModel。

One file gets dynamic pages to static pages, supports .NET CORE 3.1, and supports MVC and PageModel.


### 快速入门 MVC（Quick Start MVC）
1、将`HtmlStaticFileAttribute.cs`放到项目下；

Put `HtmlStaticFileAttribute.cs` under the project;

2、在控制器文件中，添加命名空间`Microsoft.AspNetCore.Mvc`；

In the controller file, add the namespace `Microsoft.AspNetCore.Mvc`;

3、在`类名`或`Action`方法上添加`[HtmlStaticFile]`。

Add `[HtmlStaticFile]` to the `class name` or` Action` method.

相关如下代码（Related code）：
``` csharp
using Microsoft.AspNetCore.Mvc;

namespace StaticPage.Mvc.Controllers
{
    public class HomeController : Controller
    {
    
        [HtmlStaticFile]
        [HttpGet("/Count")]
        public IActionResult Count()
        {
            c++;
            ViewBag.C = c;
            return View();
        }

	}
}
```

### 快速入门 PageModel（Quick Start PageModel）
1、将`HtmlStaticFileAttribute.cs`放到项目下；

Put `HtmlStaticFileAttribute.cs` under the project;

2、在PageModel文件中，添加命名空间`Microsoft.AspNetCore.Mvc`。

In the PageModel file, add the namespace `Microsoft.AspNetCore.Mvc`.

3、在类名上添加`[HtmlStaticFile]`，注：方法上添加`[HtmlStaticFile]`是无效的。

Add `[HtmlStaticFile]` to the class name. Note: Adding [[HtmlStaticFile] `to the method is invalid.

相关如下代码（Related code）：
``` csharp
using Microsoft.AspNetCore.Mvc;

namespace StaticPage.Pages
{
    [HtmlStaticFile]
    public class CountModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
```

### 设置缓存文件夹 （Set cache folder）
``` csharp
            HtmlStaticFileAttribute.OutputFolder = @"D:\html";
```

### 使用压缩 （Use compression）
``` csharp
            HtmlStaticFileAttribute.UseBrCompress = true;
            HtmlStaticFileAttribute.UseGzipCompress = true;
```

### 设置页面缓存时间 （Set page cache time）
``` csharp
            HtmlStaticFileAttribute.ExpireMinutes = 3;
```


### 使用开发模式 （Use development mode）
在开发模式，页面不会被缓存，便于开发调试。

In the development mode, the page will not be cached, which is convenient for development and debugging.

``` csharp
            HtmlStaticFileAttribute.IsDevelopmentMode = true;
```

### 支持Url参数，不推荐使用 （Support Url parameter, not recommended）

``` csharp
            HtmlStaticFileAttribute.UseQueryString = true;
```

### 使用Html压缩 （Use Html compression）

推荐使用`WebMarkupMin`来压缩Html。

It is recommended to use `WebMarkupMin` to compress Html.

``` csharp
            HtmlStaticFileAttribute.MiniFunc += (string html) => {
                var js = new NUglifyJsMinifier();
                var css = new NUglifyCssMinifier();

                XhtmlMinifier htmlMinifier = new XhtmlMinifier(null, css, js, null);
                var result = htmlMinifier.Minify(html);
                if (result.Errors.Count == 0) {
                    return result.MinifiedContent;
                }
                return html;
            };
```


### 更新文件缓存 （Update file cache）
在Url地址后面添加参数“__update__”，访问一下就可以生成新的静态页面。

Add the parameter "__update__" after the Url address, and you can generate a new static page after visiting it.

```
    https://localhost:44304/Count?__update__
```

### 测试页面，不更新文件缓存 （Test page without updating file cache）

在Url地址后面添加参数“__test__”，访问一下就可以看到最新页面。

Add the parameter "__test__" after the Url address, you can see the latest page after visiting.

```
    https://localhost:44304/Count?__test__
```




## 捐赠 （Donate）

如果这个类库有帮助到您，请 Star 这个仓库。

If this class library helps you, please ask Star this repository.

你也可以选择使用支付宝或微信给我捐赠：

You can also choose to use Alipay or WeChat to donate to me:

![Alipay, WeChat](https://toolgood.github.io/image/toolgood.png)

