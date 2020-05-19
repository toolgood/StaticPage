using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StaticPage.Pages
{
    [HtmlStaticFile]
    public class CountModel : PageModel
    {
        private static int c = 0;

        public int C;

        public void OnGet()
        {
            c++;
            C = c;
        }
    }
}