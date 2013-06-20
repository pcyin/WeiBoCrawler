using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeiBoCrawler
{
    class CommentCrawlJob
    {
        public String Url { get; set; }
        public int BeginPage { get; set; }
        public int EndPage { get; set; }
    }
}
