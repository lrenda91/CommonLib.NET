using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.utility.mail
{
    public class MailAttachment
    {
        public Stream ContentStream { get; set; }
        public string Name { get; set; }

        public MailAttachment(Stream stream, string fileName)
        {
            ContentStream = stream;
            Name = fileName;
        }
    }
}
