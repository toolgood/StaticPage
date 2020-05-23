using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// 生成Html静态文件
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class HtmlStaticFileAttribute : ActionFilterAttribute, IPageFilter
    {

        /// <summary>
        /// 页面更新参数，用于更新页面,更新文件 如 https://localhost:44345/?__update__
        /// </summary>
        public static string UpdateFileQueryString = "__update__";
        /// <summary>
        /// 页面测试参数，测试页面，不更新文件 如  https://localhost:44345/?__test__
        /// </summary>
        public static string TestQueryString = "__test__";

        /// <summary>
        /// 开发模式
        /// </summary>
        public static bool IsDevelopmentMode = false;

        /// <summary>
        /// 支持Url参数，不推荐使用
        /// </summary>
        public static bool UseQueryString = false;
        /// <summary>
        /// 页面缓存时间 单位：分钟
        /// </summary>
        public static int ExpireMinutes = 1;

        /// <summary>
        /// 静态文件保存路径, 如果为空，则默认放在 {dll文件夹}\html 文件夹下
        /// </summary>
        public static string OutputFolder;
        /// <summary>
        /// 使用GZIP压缩，会另生成一个单独的文件，以空间换时间，火狐、IE11 对于http://开头的地址不支持 br 压缩
        /// </summary>
        public static bool UseGzipCompress = false;

        /// <summary>
        /// 使用BR压缩，会另生成一个单独的文件，以空间换时间
        /// </summary>
        public static bool UseBrCompress = false;

        /// <summary>
        /// 指定方法，保存前会进行最小化处理, 推荐使用 WebMarkupMin
        /// </summary>
        public static event Func<string, string> MiniFunc;



        #region 用于 Pages
        public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            if (IsDevelopmentMode == false && IsTest(context) == false && IsUpdateOutputFile(context) == false) {
                var filePath = GetOutputFilePath(context);
                var response = context.HttpContext.Response;
                if (File.Exists(filePath)) {
                    var fi = new FileInfo(filePath);
                    var etag = fi.LastWriteTimeUtc.Ticks.ToString();
                    if (context.HttpContext.Request.Headers["If-None-Match"] == etag) {
                        context.Result = new StatusCodeResult(304);
                        return;
                    }
                    response.Headers["Cache-Control"] = "max-age=" + ExpireMinutes * 60;
                    response.Headers["Etag"] = etag;
                    response.Headers["Date"] = DateTime.Now.ToString("r");
                    response.Headers["Expires"] = DateTime.Now.AddMinutes(ExpireMinutes).ToString("r");

                    if (UseBrCompress || UseGzipCompress) {
                        var sp = context.HttpContext.Request.Headers["Accept-Encoding"].ToString().Replace(" ", "").ToLower().Split(',');
                        if (UseBrCompress && sp.Contains("br") && File.Exists(filePath + ".br")) {
                            response.Headers["Content-Encoding"] = "br";
                            context.Result = new FileContentResult(File.ReadAllBytes(filePath + ".br"), "text/html");
                            return;
                        } else if (UseGzipCompress && sp.Contains("gzip") && File.Exists(filePath + ".gzip")) {
                            response.Headers["Content-Encoding"] = "gzip";
                            context.Result = new FileContentResult(File.ReadAllBytes(filePath + ".gzip"), "text/html");
                            return;
                        }
                    }
                    var bytes = File.ReadAllBytes(filePath);
                    context.Result = new FileContentResult(bytes, "text/html");
                    return;
                }
            }
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
        #endregion

        #region 用于 Views

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (IsDevelopmentMode == false && IsTest(context) == false && IsUpdateOutputFile(context) == false) {
                var filePath = GetOutputFilePath(context);
                var response = context.HttpContext.Response;
                if (File.Exists(filePath)) {
                    var fi = new FileInfo(filePath);
                    var etag = fi.LastWriteTimeUtc.Ticks.ToString();
                    if (context.HttpContext.Request.Headers["If-None-Match"] == etag) {
                        context.Result = new StatusCodeResult(304);
                        return;
                    }
                    response.Headers["Cache-Control"] = "max-age=" + ExpireMinutes * 60;
                    response.Headers["Etag"] = etag;
                    response.Headers["Date"] = DateTime.Now.ToString("r");
                    response.Headers["Expires"] = DateTime.Now.AddMinutes(ExpireMinutes).ToString("r");

                    if (UseBrCompress || UseGzipCompress) {
                        var sp = context.HttpContext.Request.Headers["Accept-Encoding"].ToString().Replace(" ", "").ToLower().Split(',');
                        if (UseBrCompress && sp.Contains("br") && File.Exists(filePath + ".br")) {
                            response.Headers["Content-Encoding"] = "br";
                            context.Result = new FileContentResult(File.ReadAllBytes(filePath + ".br"), "text/html");
                            return;
                        } else if (UseGzipCompress && sp.Contains("gzip") && File.Exists(filePath + ".gzip")) {
                            response.Headers["Content-Encoding"] = "gzip";
                            context.Result = new FileContentResult(File.ReadAllBytes(filePath + ".gzip"), "text/html");
                            return;
                        }
                    }
                    var bytes = await File.ReadAllBytesAsync(filePath);
                    context.Result = new FileContentResult(bytes, "text/html");
                    return;
                }
            }
            await base.OnActionExecutionAsync(context, next);
        }
        #endregion

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // 开发模式，已处理的，测试，不用保存到本地目录
            if (IsDevelopmentMode || context.Result is StatusCodeResult || context.Result is FileContentResult || IsTest(context)) {
                await base.OnResultExecutionAsync(context, next);
                return;
            }

            var filePath = GetOutputFilePath(context);
            var response = context.HttpContext.Response;
            if (!response.Body.CanRead || !response.Body.CanSeek) {
                using (var ms = new MemoryStream()) {
                    var old = response.Body;
                    response.Body = ms;

                    await base.OnResultExecutionAsync(context, next);

                    if (response.StatusCode == 200) {
                        await SaveHtmlResult(response.Body, filePath);
                    }
                    ms.Position = 0;
                    await ms.CopyToAsync(old);
                    response.Body = old;
                }
            } else {
                await base.OnResultExecutionAsync(context, next);
                var old = response.Body.Position;
                if (response.StatusCode == 200) {
                    await SaveHtmlResult(response.Body, filePath);
                }
                response.Body.Position = old;
            }
            {
                var fi = new FileInfo(filePath);
                var etag = fi.LastWriteTimeUtc.Ticks.ToString();
                context.HttpContext.Response.Headers["Cache-Control"] = "max-age=" + ExpireMinutes * 60;
                context.HttpContext.Response.Headers["Etag"] = etag;
                context.HttpContext.Response.Headers["Date"] = DateTime.Now.ToString("r");
                context.HttpContext.Response.Headers["Expires"] = DateTime.Now.AddMinutes(ExpireMinutes).ToString("r");
            }
        }


        private async Task SaveHtmlResult(Stream stream, string filePath)
        {
            stream.Position = 0;
            var responseReader = new StreamReader(stream);
            var responseContent = await responseReader.ReadToEndAsync();
            if (MiniFunc != null) {//进行最小化处理
                responseContent = MiniFunc(responseContent);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            await File.WriteAllTextAsync(filePath, responseContent);

            if (UseGzipCompress || UseBrCompress) {
                var bytes = Encoding.UTF8.GetBytes(responseContent);
                if (UseGzipCompress) {
                    var bs = GzipCompress(bytes, false);
                    await File.WriteAllBytesAsync(filePath + ".gzip", bs);
                }
                if (UseBrCompress) {
                    var bs = BrCompress(bytes, false);
                    await File.WriteAllBytesAsync(filePath + ".br", bs);
                }
            }
        }


        private bool IsTest(FilterContext context)
        {
            return context.HttpContext.Request.Query.Keys.Contains(TestQueryString);
        }

        private bool IsUpdateOutputFile(FilterContext context)
        {
            return context.HttpContext.Request.Query.Keys.Contains(UpdateFileQueryString);
        }

        private string GetOutputFilePath(FilterContext context)
        {
            string dir = OutputFolder;
            if (string.IsNullOrEmpty(dir)) {
                dir = Path.Combine(Path.GetDirectoryName(typeof(HtmlStaticFileAttribute).Assembly.Location), "html");
                OutputFolder = dir;
            }

            var t = context.HttpContext.Request.Path.ToString().Replace("//", Path.DirectorySeparatorChar.ToString()).Replace("/", Path.DirectorySeparatorChar.ToString());
            if (t.EndsWith(Path.DirectorySeparatorChar)) {
                t += "index";
            }
            if (UseQueryString) {
                var list = new HashSet<string>();
                foreach (var item in context.HttpContext.Request.Query.Keys) {
                    if (item != UpdateFileQueryString) {
                        var value = context.HttpContext.Request.Query[item];
                        if (string.IsNullOrEmpty(value)) {
                            list.Add($"{list}_");
                        } else {
                            list.Add($"{list}_{value}");
                        }
                    }
                }
                t += Regex.Replace(string.Join(",", list), "[^0-9_a-zA-Z\u4E00-\u9FCB\u3400-\u4DB5\u3007]", "_");
            }

            t = t.TrimStart(Path.DirectorySeparatorChar);
            return Path.Combine(dir, t) + ".html";
        }

        /// <summary>
        /// Gzip压缩
        /// </summary>
        /// <param name="data">要压缩的字节数组</param>
        /// <param name="fastest">快速模式</param>
        /// <returns>压缩后的数组</returns>
        private byte[] GzipCompress(byte[] data, bool fastest = false)
        {
            if (data == null || data.Length == 0)
                return data;
            try {
                using (MemoryStream stream = new MemoryStream()) {
                    var level = fastest ? CompressionLevel.Fastest : CompressionLevel.Optimal;
                    using (GZipStream zStream = new GZipStream(stream, level)) {
                        zStream.Write(data, 0, data.Length);
                    }
                    return stream.ToArray();
                }
            } catch {
                return data;
            }
        }

        /// <summary>
        /// Br压缩
        /// </summary>
        /// <param name="data">要压缩的字节数组</param>
        /// <param name="fastest">快速模式</param>
        /// <returns>压缩后的数组</returns>
        private byte[] BrCompress(byte[] data, bool fastest = false)
        {
            if (data == null || data.Length == 0)
                return data;
            try {
                using (MemoryStream stream = new MemoryStream()) {
                    var level = fastest ? CompressionLevel.Fastest : CompressionLevel.Optimal;
                    using (BrotliStream zStream = new BrotliStream(stream, level)) {
                        zStream.Write(data, 0, data.Length);
                    }
                    return stream.ToArray();
                }
            } catch {
                return data;
            }
        }


    }
}
