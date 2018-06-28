using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.utility.mail
{
    public interface IMailSender
    {
        void SendMail(
            MailParams config, 
            ICollection<string> to, 
            ICollection<string> cc,
            ICollection<string> bcc, 
            string subject, 
            string body,
            ICollection<MailAttachment> attachments
        );
    }

    interface MailParamsBuilder
    {
        MailParams BuildDeliveryParams();
    }

    public class MailParams
    {
        public string Host { get; set; }
        public short Port { get; set; }
        public string FromAddress { get; set; }
        public string User { get; set; }
        public string Password { get; set; }

        public override string ToString()
        {
            return string.Format("Host: {0}:{1} From [{2}] User '{3}' Passwd '{4}'", Host, Port, FromAddress, User, Password);
        }
    }

}
