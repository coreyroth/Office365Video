using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Office365Video.Models
{
    public class VideoChannel
    {
        public string Id { get; set; }
        public string HtmlColor { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ServerRelativeUrl { get; set; }
    }
}
