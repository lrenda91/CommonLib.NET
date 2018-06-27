using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace org.commitworld.web.utility.mail
{

    public class MailDeliveryEventArgs : EventArgs
    {
        public MailMessage Message { get; internal set; }
        public Exception Error { get; internal set; }
    }

    public class MailSender : IMailSender
    {

        public event EventHandler<MailDeliveryEventArgs> Success, Fail;

        private void OnSuccess(MailMessage message)
        {
            if (Success != null) Success(this, new MailDeliveryEventArgs() { Message = message });
        }

        private void OnFail(Exception ex)
        {
            if (Fail != null) Fail(this, new MailDeliveryEventArgs() { Error = ex });
        }

        public void SendMail(MailParams config,
            ICollection<string> tos,
            ICollection<string> ccs,
            string subject,
            string body,
            ICollection<MailAttachment> attachments)
        {
            try
            {
                MailMessage message = new MailMessage();
                message.From = new MailAddress(config.FromAddress);

                if (tos != null)
                {
                    foreach (string to in tos)
                    {
                        message.To.Add(to);
                    }
                }
                if (ccs != null)
                {
                    foreach (string cc in ccs)
                    {
                        message.CC.Add(cc);
                    }
                }

                message.Subject = subject;
                message.Body = body;

                if (attachments != null)
                {
                    foreach (MailAttachment ma in attachments)
                    {
                        message.Attachments.Add(new Attachment(ma.ContentStream, ma.Name));
                    }
                }

                //Attachment Allegato = new Attachment(filename);
                //mail.Attachments.Add(Allegato);

                // If your smtp server requires TLS connection
                // smtpClient.ConnectType = SmtpConnectType.ConnectSSLAuto;

                // If your smtp server requires implicit SSL connection on 465 port, please add this line
                // smtpClient.Port = 465;
                // smtpClient.ConnectType = SmtpConnectType.ConnectSSLAuto;

                SmtpClient smtpClient = new SmtpClient()
                {
                    Host = config.Host,
                    Port = config.Port,
                    Credentials = new System.Net.NetworkCredential(config.User, config.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                using (smtpClient)
                {
                    using (message)
                    {
                        smtpClient.Send(message);
                    }
                }

                OnSuccess(message);
            }
            catch (Exception ex)
            {
                OnFail(ex);
                throw ex;
            }
        }
    }
}
