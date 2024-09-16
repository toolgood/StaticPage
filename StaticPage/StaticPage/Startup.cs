using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebMarkupMin.Core;
using WebMarkupMin.NUglify;

namespace StaticPage
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
   

            } else {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            // 注意 请先清一下浏览器缓存
            //开发模式 
            //HtmlStaticFileAttribute.IsDevelopmentMode = true;

            //设置缓存文件夹
            HtmlStaticFileAttribute.OutputFolder = @"D:\html\1";

            //使用压缩
            HtmlStaticFileAttribute.UseBrCompress = true;
            HtmlStaticFileAttribute.UseGzipCompress = true;

            //设置页面缓存时间 3分钟
            HtmlStaticFileAttribute.ExpireMinutes = 3;

            // 开启html压缩
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


            app.UseEndpoints(endpoints => {
                endpoints.MapRazorPages();
            });
        }

        private string HtmlStaticFileAttribute_MiniFunc(string arg)
        {
            throw new NotImplementedException();
        }
    }
}
